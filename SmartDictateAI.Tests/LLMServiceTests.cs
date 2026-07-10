using Xunit;
using Xunit.Abstractions;
using SmartDictateAI.Services;
using System;
using System.IO;

namespace SmartDictateAI.Tests
{
    public class LLMServiceTests
    {
        private readonly ITestOutputHelper _output;

        public LLMServiceTests(ITestOutputHelper output)
        {
            _output = output;
        }
        [Fact]
        public void StripThinkingBlocks_ShouldRemoveChannelThoughtTagsAndContent()
        {
            // Arrange
            string input = "(<|channel>thought Here is the model's thinking process. <channel|>) This is the final output text.";

            // Act
            string result = LLMService.StripThinkingBlocks(input);

            // Assert
            Assert.Equal("This is the final output text.", result);
        }

        [Fact]
        public void StripThinkingBlocks_ShouldRemoveStandardThoughtTagsAndContent()
        {
            // Arrange
            string input = "<thought>Calculating spelling correction...</thought>This is the corrected text.";

            // Act
            string result = LLMService.StripThinkingBlocks(input);

            // Assert
            Assert.Equal("This is the corrected text.", result);
        }

        [Fact]
        public void StripThinkingBlocks_ShouldRemoveBarThoughtTagsAndContent()
        {
            // Arrange
            string input = "(<|thought|>Thinking hard about grammar rules...</|thought|>) Correct text here.";

            // Act
            string result = LLMService.StripThinkingBlocks(input);

            // Assert
            Assert.Equal("Correct text here.", result);
        }

        [Fact]
        public void StripThinkingBlocks_ShouldHandleWhitespaceAndNewlines()
        {
            // Arrange
            string input = "\n(<|channel>thought\nThinking\n<channel|>)\n\nCorrect text.";

            // Act
            string result = LLMService.StripThinkingBlocks(input);

            // Assert
            Assert.Equal("Correct text.", result);
        }

        [Fact]
        public void StripThinkingBlocks_ShouldRemoveQwenThinkTagsAndContent()
        {
            // Arrange
            string input = "<think> Analyzing spelling correction. </think> Correct text.";

            // Act
            string result = LLMService.StripThinkingBlocks(input);

            // Assert
            Assert.Equal("Correct text.", result);
        }

        [Fact]
        public void StripThinkingBlocks_ShouldRemoveUnclosedThinkTagsAtEnd()
        {
            // Arrange
            string input = "Correct text. <think> Thinking about spelling correction but got cut off...";

            // Act
            string result = LLMService.StripThinkingBlocks(input);

            // Assert
            Assert.Equal("Correct text.", result);
        }

        [Fact]
        public void StripThinkingBlocks_ShouldRemoveUnclosedChannelThoughtTagsAtEnd()
        {
            // Arrange
            string input = "Correct text. (<|channel>thought Thinking about spelling correction...";

            // Act
            string result = LLMService.StripThinkingBlocks(input);

            // Assert
            Assert.Equal("Correct text.", result);
        }

        [Theory]
        [InlineData("gemma-2-9b-it.gguf", "<start_of_turn>user", "<end_of_turn>", "<eos>")]
        [InlineData("Llama-3.2-3B-Instruct-Q8_0.gguf", "<|start_header_id|>system", "<|eot_id|>", "<|end_of_text|>")]
        [InlineData("llama-2-7b-chat.gguf", "[INST]", "[/INST]", "<<SYS>>")]
        [InlineData("qwen2-0_5b-instruct-q8_0.gguf", "<|im_start|>system", "<|im_end|>", "<|im_start|>")]
        [InlineData("unknown_model.gguf", "<|im_start|>system", "<|im_end|>", "<|im_start|>")] // Default fallback
        public void ResolvePromptTemplate_ShouldDetectCorrectFormatFromFilename(string filename, string expectedSnippet, string antiPrompt1, string antiPrompt2)
        {
            // Act
            var (template, antiPrompts) = LLMService.ResolvePromptTemplate(filename);

            // Assert
            Assert.Contains(expectedSnippet, template);
            Assert.Contains(antiPrompt1, antiPrompts);
            Assert.Contains(antiPrompt2, antiPrompts);
        }

        [Fact]
        public void ResolvePromptTemplate_ShouldUseManualOverrideIfProvided()
        {
            // Arrange
            string filename = "gemma-2-9b-it.gguf"; // Would normally resolve to Gemma
            string overrideTemplate = "MANUAL_OVERRIDE_TEMPLATE";

            // Act
            var (template, antiPrompts) = LLMService.ResolvePromptTemplate(filename, overrideTemplate);

            // Assert
            Assert.Equal(overrideTemplate, template);
            Assert.Empty(antiPrompts);
        }

        [Fact]
        public void ResolvePromptTemplate_ShouldResolveAllDiscoveredLocalLLMModels()
        {
            // Resolve the local models directory
            string llmDir = "";
            try
            {
                var currentDir = AppDomain.CurrentDomain.BaseDirectory;
                while (currentDir != null)
                {
                    var testPath = Path.Combine(currentDir, "models", "llm");
                    if (Directory.Exists(testPath))
                    {
                        llmDir = testPath;
                        break;
                    }
                    
                    // Fallback check for root models directory if organized subfolder doesn't exist
                    var rootModelsPath = Path.Combine(currentDir, "models");
                    if (Directory.Exists(rootModelsPath))
                    {
                        llmDir = rootModelsPath;
                        break;
                    }
                    currentDir = Directory.GetParent(currentDir)?.FullName;
                }
            }
            catch
            {
                // Directory scanning failed
            }

            if (!string.IsNullOrEmpty(llmDir) && Directory.Exists(llmDir))
            {
                var ggufFiles = Directory.GetFiles(llmDir, "*.gguf");
                _output.WriteLine($"[Local Models Verification] Found {ggufFiles.Length} GGUF models in: {llmDir}");
                
                foreach (var file in ggufFiles)
                {
                    string filename = Path.GetFileName(file);
                    
                    // Act
                    var (template, antiPrompts) = LLMService.ResolvePromptTemplate(filename);

                    // Assert
                    Assert.NotNull(template);
                    Assert.NotEmpty(template);
                    Assert.NotNull(antiPrompts);
                    
                    _output.WriteLine($"  - Model: {filename}");
                    _output.WriteLine($"    Template: {template.Replace("\n", "\\n")}");
                    _output.WriteLine($"    Anti-Prompts: {string.Join(", ", antiPrompts)}");
                }
            }
            else
            {
                _output.WriteLine("[Local Models Verification] No local models folder found, skipping verification.");
            }
        }

        [Fact]
        public void ApplyVocabularyReplacements_ShouldReplaceConfiguredTerms()
        {
            // Arrange
            var settings = new AppSettings();
            settings.VocabularyReplacements = new List<VocabularyReplacement>
            {
                new() { Target = "SmartServer", Replacement = "Sm@rtServer" },
                new() { Target = "Smart Server", Replacement = "Sm@rtServer" },
                new() { Target = "site top", Replacement = "SITOP" },
                new() { Target = "Site top", Replacement = "SITOP" },
                new() { Target = "SIMATIC Net", Replacement = "SIMATIC NET" },
                new() { Target = "ET200SP", Replacement = "ET 200SP" },
                new() { Target = "ET200 SP", Replacement = "ET 200SP" },
                new() { Target = "S1500", Replacement = "S7-1500" },
                new() { Target = "S 1500", Replacement = "S7-1500" }
            };

            // Act & Assert
            Assert.Equal("We need to configure the Sm@rtServer.", LLMService.ApplyVocabularyReplacements("We need to configure the SmartServer.", settings));
            Assert.Equal("The Sm@rtServer is running.", LLMService.ApplyVocabularyReplacements("The Smart Server is running.", settings));
            Assert.Equal("Use SITOP power supplies.", LLMService.ApplyVocabularyReplacements("Use site top power supplies.", settings));
            Assert.Equal("Use SITOP power supplies.", LLMService.ApplyVocabularyReplacements("Use Site top power supplies.", settings));
            Assert.Equal("Connect via SIMATIC NET.", LLMService.ApplyVocabularyReplacements("Connect via SIMATIC Net.", settings));
            Assert.Equal("Configure ET 200SP modules.", LLMService.ApplyVocabularyReplacements("Configure ET200SP modules.", settings));
            Assert.Equal("Configure ET 200SP modules.", LLMService.ApplyVocabularyReplacements("Configure ET200 SP modules.", settings));
            Assert.Equal("This runs on the S7-1500 PLC.", LLMService.ApplyVocabularyReplacements("This runs on the S1500 PLC.", settings));
            Assert.Equal("This runs on the S7-1500 PLC.", LLMService.ApplyVocabularyReplacements("This runs on the S 1500 PLC.", settings));
            
            // Boundary checks: shouldn't replace substrings within other words
            Assert.Equal("The asset name is AS15000.", LLMService.ApplyVocabularyReplacements("The asset name is AS15000.", settings));
            Assert.Equal("This is site topography.", LLMService.ApplyVocabularyReplacements("This is site topography.", settings));
        }
    }
}
