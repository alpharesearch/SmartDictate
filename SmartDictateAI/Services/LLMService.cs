using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLama;
using LLama.Common;

namespace SmartDictateAI.Services
{
    public class LLMService : ILLMService
    {
        private readonly object _llmInitLock = new object();
        private LLamaWeights? _llmModelWeights;
        private StatelessExecutor? _llmExecutor;
        private string? _currentModelPath;

        public bool IsInitialized => _llmExecutor != null;

        public bool Initialize(string modelPath, int contextSize, bool useGpu, Action<string>? onDebugMessage = null)
        {
            lock (_llmInitLock)
            {
                if (_llmExecutor != null && _currentModelPath == modelPath)
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
                {
                    onDebugMessage?.Invoke($"[Settings] LLM Initialize: LocalLLMModelPath invalid: {modelPath}");
                    return false;
                }

                onDebugMessage?.Invoke($"[Settings] Initializing LLamaSharp with model: {modelPath}");
                try
                {
                    DisposeResourcesInternal(onDebugMessage);

                    var parameters = new ModelParams(modelPath)
                    {
                        ContextSize = (uint)contextSize,
                        GpuLayerCount = useGpu ? 99 : 0
                    };
                    _llmModelWeights = LLamaWeights.LoadFromFile(parameters);
                    _llmExecutor = new StatelessExecutor(_llmModelWeights, parameters);
                    _currentModelPath = modelPath;

                    onDebugMessage?.Invoke($"[LLM] Backend request | UseGpu={useGpu} | GpuLayerCount={parameters.GpuLayerCount} | Arch={System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
                    onDebugMessage?.Invoke("[LLM] Initialized successfully.");
                    return true;
                }
                catch (Exception ex)
                {
                    onDebugMessage?.Invoke($"[LLM] FATAL: [LLM] Could not initialize LLamaSharp: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                    DisposeResourcesInternal(onDebugMessage);
                    return false;
                }
            }
        }

        public async Task<string> RefineTextAsync(
            string inputText, 
            AppSettings settings, 
            string systemPromptOverride = "", 
            string userPromptOverride = "", 
            Action<string>? onDebugMessage = null)
        {
            if (string.IsNullOrWhiteSpace(inputText))
            {
                return inputText;
            }

            var systemPrompt = string.IsNullOrEmpty(systemPromptOverride) ? settings.LLMSystemPrompt : systemPromptOverride;
            var userPrompt = string.IsNullOrEmpty(userPromptOverride) ? settings.LLMUserPrompt : userPromptOverride;

            if (!string.IsNullOrWhiteSpace(settings.CustomVocabulary))
            {
                systemPrompt += $"\n\nCRITICAL: The user has specified the following custom vocabulary terms that must be preserved exactly with their correct spelling and capitalization if they appear phonetically or as similar misrecognized words in the transcription (do not change or 'correct' them to general words): {settings.CustomVocabulary}";
            }

            if (_llmExecutor == null)
            {
                if (!Initialize(settings.LocalLLMModelPath, settings.LLMContextSize, settings.UseGpu, onDebugMessage))
                {
                    onDebugMessage?.Invoke("[LLM] LLM could not be initialized. Skipping LLM processing.");
                    return inputText;
                }
                if (_llmExecutor == null)
                {
                    onDebugMessage?.Invoke("[LLM] LLM Executor still null after init attempt. Skipping.");
                    return inputText;
                }
            }

            onDebugMessage?.Invoke("[LLM] Sending text to LLamaSharp for processing..." + inputText);
            var outputBuffer = new StringBuilder();

            try
            {
                string templateToUse;
                var autoAntiPrompts = new List<string>();
                string modelFileLower = settings.LocalLLMModelPath.ToLowerInvariant();

                if (!string.IsNullOrWhiteSpace(settings.LLMPromptTemplate))
                {
                    onDebugMessage?.Invoke("[LLM] Using manual LLMPromptTemplate override.");
                    templateToUse = settings.LLMPromptTemplate;
                }
                else if (modelFileLower.Contains("gemma"))
                {
                    templateToUse = "<start_of_turn>user\n{0}\n\n{1}\n\n{2}<end_of_turn>\n<start_of_turn>model\n";
                    autoAntiPrompts.AddRange(new[] { "<end_of_turn>", "<eos>" });
                }
                else if (modelFileLower.Contains("llama-3") || modelFileLower.Contains("llama3"))
                {
                    templateToUse = "<|begin_of_text|><|start_header_id|>system<|end_header_id|>\n{0}<|eot_id|><|start_header_id|>user<|end_header_id|>\n{1}\n\n{2}<|eot_id|><|start_header_id|>assistant<|end_header_id|>\n";
                    autoAntiPrompts.AddRange(new[] { "<|eot_id|>", "<|end_of_text|>" });
                }
                else if (modelFileLower.Contains("llama-2") || modelFileLower.Contains("llama2"))
                {
                    templateToUse = "[INST] <<SYS>>\n{0}\n<</SYS>>\n\n{1}\n\n{2} [/INST]";
                    autoAntiPrompts.AddRange(new[] { "[/INST]", "<<SYS>>" });
                }
                else // Default fallback to ChatML (Qwen, etc)
                {
                    templateToUse = "<|im_start|>system\n{0}<|im_end|>\n<|im_start|>user\n{1}\n\n{2}<|im_end|>\n<|im_start|>assistant\n";
                    autoAntiPrompts.AddRange(new[] { "<|im_end|>", "<|im_start|>" });
                }

                string fullPrompt = string.Format(templateToUse, systemPrompt, userPrompt, inputText);
                uint actualSeedToUse;
                if (settings.LLMSeed == 0)
                {
                    actualSeedToUse = (uint)Random.Shared.Next();
                    onDebugMessage?.Invoke($"[LLM] LLMSeed was 0, generated random seed for this inference: {actualSeedToUse}");
                }
                else
                {
                    actualSeedToUse = (uint)settings.LLMSeed;
                    onDebugMessage?.Invoke($"[Settings] Using LLMSeed from settings: {actualSeedToUse}");
                }

                var combinedAntiPrompts = autoAntiPrompts.Concat(settings.LLMAntiPrompts ?? new List<string>()).Distinct().ToList();

                var inferenceParams = new InferenceParams()
                {
                    AntiPrompts = combinedAntiPrompts,
                    MaxTokens = settings.LLMMaxOutputTokens,
                    SamplingPipeline = new LLama.Sampling.DefaultSamplingPipeline()
                    {
                        Seed = actualSeedToUse,
                        Temperature = settings.LLMTemperature,
                    }
                };

                onDebugMessage?.Invoke("[LLM] \nfullPrompt " + fullPrompt);
                onDebugMessage?.Invoke("[App] \ninferenceParams" + inferenceParams.ToString());

                var swLlm = System.Diagnostics.Stopwatch.StartNew();
                int tokCount = 0;

                onDebugMessage?.Invoke($"[LLM] Begin | ctx={settings.LLMContextSize} | maxOut={settings.LLMMaxOutputTokens} | gpuLayers={(settings.UseGpu ? 99 : 0)}");

                await foreach (var textPart in _llmExecutor.InferAsync(fullPrompt, inferenceParams))
                {
                    outputBuffer.Append(textPart);
                    tokCount++;

                    if (tokCount % 32 == 0)
                    {
                        double liveTps = tokCount / Math.Max(swLlm.Elapsed.TotalSeconds, 0.001);
                        onDebugMessage?.Invoke($"[LLM] ... {tokCount} tokens | {liveTps:F1} tok/s");
                    }
                }

                swLlm.Stop();

                var outText = outputBuffer.ToString();
                double tps = tokCount / Math.Max(swLlm.Elapsed.TotalSeconds, 0.001);
                onDebugMessage?.Invoke($"[LLM] Done | streamParts={tokCount} | sec={swLlm.Elapsed.TotalSeconds:F2} | {tps:F1} tok/s-ish");
                double cps = outText.Length / Math.Max(swLlm.Elapsed.TotalSeconds, 0.001);
                onDebugMessage?.Invoke($"[LLM] Done | outChars={outText.Length} | sec={swLlm.Elapsed.TotalSeconds:F2} | {cps:F0} chars/s");

                string finalResult = outputBuffer.ToString().Trim();
                foreach (var tag in combinedAntiPrompts)
                {
                    finalResult = finalResult.Replace(tag, string.Empty).Trim();
                }

                onDebugMessage?.Invoke("[LLM] LLamaSharp processing successful. " + finalResult);
                return finalResult;
            }
            catch (Exception ex)
            {
                onDebugMessage?.Invoke($"[LLM] Generic error during LLamaSharp processing: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                return inputText;
            }
        }

        private void DisposeResourcesInternal(Action<string>? onDebugMessage = null)
        {
            lock (_llmInitLock)
            {
                onDebugMessage?.Invoke("[LLM] Disposing LLamaSharp internal resources.");
                if (_llmExecutor is IDisposable disposableExecutor)
                {
                    try { disposableExecutor.Dispose(); } catch { }
                }
                _llmModelWeights?.Dispose();
                _llmExecutor = null;
                _llmModelWeights = null;
                _currentModelPath = null;
            }
        }

        public async Task DisposeResourcesAsync(Action<string>? onDebugMessage = null)
        {
            onDebugMessage?.Invoke("[LLM] Disposing LLamaSharp resources (synchronously within async method for test)...");
            DisposeResourcesInternal(onDebugMessage);
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            DisposeResourcesInternal();
        }
    }
}
