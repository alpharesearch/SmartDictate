using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using SmartDictateAI.Services;
using System.Threading;

namespace SmartDictateAI.PerformanceTests
{
    internal class TestPrompt
    {
        public string Name { get; set; } = "";
        public string Input { get; set; } = "";
        public string ExpectedOutput { get; set; } = "";
        public string[] ExpectedSubstrings { get; set; } = Array.Empty<string>();
        public string[] ForbiddenSubstrings { get; set; } = Array.Empty<string>();
    }

    public partial class ModelPerformanceTests
    {
        private static readonly List<TestPrompt> TestCases = new()
        {
            new TestPrompt
            {
                Name = "Grammar & Spelling Correction",
                Input = "she do not likes the new apple phone because it are too big",
                ExpectedOutput = "She does not like the new Apple phone because it is too big.",
                ExpectedSubstrings = new[] { "does not", "like", "Apple", "is too big" },
                ForbiddenSubstrings = new[] { "do not likes", "it are" }
            },
            new TestPrompt
            {
                Name = "Filler Word Removal",
                Input = "uh so basically like I went to the store and um bought some milk",
                ExpectedOutput = "I went to the store and bought some milk.",
                ExpectedSubstrings = new[] { "went to the store", "bought some milk" },
                ForbiddenSubstrings = new[] { "uh", "um", "basically like" }
            },
            new TestPrompt
            {
                Name = "Custom Vocabulary Formatting",
                Input = "we configured the siemetic step 7 s7 1500 plc using tia portal",
                ExpectedOutput = "We configured the SIMATIC STEP 7 S7-1500 PLC using TIA Portal.",
                ExpectedSubstrings = new[] { "SIMATIC", "STEP 7", "S7-1500", "TIA Portal" },
                ForbiddenSubstrings = new[] { "siemetic", "step 7", "s7 1500", "tia portal" }
            },
            new TestPrompt
            {
                Name = "Advanced Custom Vocabulary Formatting",
                Input = "the wincc unified project uses opc ua and profinet to talk to the s7 1200 and et 200sp modules",
                ExpectedOutput = "The WinCC Unified project uses OPC UA and PROFINET to communicate with the S7-1200 and ET 200SP modules.",
                ExpectedSubstrings = new[] { "WinCC Unified", "OPC UA", "PROFINET", "S7-1200", "ET 200SP" },
                ForbiddenSubstrings = new[] { "wincc unified", "opc ua", "profinet", "s7 1200", "et 200sp" }
            },
            new TestPrompt
            {
                Name = "Homophone & Dictation Gotchas",
                Input = "there dogs are barking over their because its a shame that the dog lost its collar",
                ExpectedOutput = "Their dogs are barking over there because it's a shame that the dog lost its collar.",
                ExpectedSubstrings = new[] { "eir dogs", "barking over there", "t's a shame", "lost its collar" },
                ForbiddenSubstrings = new[] { "there dogs", "over their", "its a shame", "lost it's collar" }
            },
            new TestPrompt
            {
                Name = "Punctuation & Capitalization",
                Input = "the project was delayed however we managed to finish it on time did you check the final build",
                ExpectedOutput = "The project was delayed; however, we managed to finish it on time. Did you check the final build?",
                ExpectedSubstrings = new[] { "The project was delayed", "however,", "on time.", "Did you check", "final build?" },
                ForbiddenSubstrings = new[] { "however we", "on time did", "the project" }
            },
            new TestPrompt
            {
                Name = "Numbers & Measurements",
                Input = "the motor runs at three thousand two hundred rpm with a temperature of eighty five degrees celsius at twelve thirty pm",
                ExpectedOutput = "The motor runs at 3,200 RPM with a temperature of 85 degrees Celsius at 12:30 PM.",
                ExpectedSubstrings = new[] { "200", "RPM", "85", "C", "12:30", "PM" },
                ForbiddenSubstrings = new[] { "three thousand", "eighty five", "twelve thirty" }
            },
            new TestPrompt
            {
                Name = "Contractions & Pronoun Corrections",
                Input = "we should of verified the logic and it would of saved us a lot of time",
                ExpectedOutput = "We should have verified the logic, and it would have saved us a lot of time.",
                ExpectedSubstrings = new[] { "should have verified", "would have saved" },
                ForbiddenSubstrings = new[] { "should of", "would of" }
            },
            new TestPrompt
            {
                Name = "Double Words & Slurs",
                Input = "we need to to update the the firmware as soon as possible",
                ExpectedOutput = "We need to update the firmware as soon as possible.",
                ExpectedSubstrings = new[] { "need to update", "update the firmware" },
                ForbiddenSubstrings = new[] { "to to", "the the" }
            },
            new TestPrompt
            {
                Name = "Professional Email Tone",
                Input = "dear john i wanted to let you know that the plc code is ready let me know when you can jump on a call thanks mark",
                ExpectedOutput = "Dear John, I wanted to let you know that the PLC code is ready. Let me know when you can jump on a call. Thanks, Mark.",
                ExpectedSubstrings = new[] { "Dear John,", "PLC", "PLC code", "on a call.", "Thanks, Mark" },
                ForbiddenSubstrings = new[] { "dear john", "plc", "thanks mark" }
            },
            new TestPrompt
            {
                Name = "Easy Siemens Cleanup",
                Input = "during the commisioning of a new automation sell the engineering team configured a simatic s71500 plc in tia portal and intergrated it with several ET200 SP distributed io stations over profi net the control architechture was designed provide deterministic comunication between the controller operator interfaces and plant network infrastucture a scalence managed switch was use to segment trafic and improve network diagnostic's\n\nthe hmi application was developed using win cc unifide and deployed to a comfort pannel located on the main opperator station the interface provides real time monitoring of production data alarm management equipment status and maintanance information um operators can review process trends acknowledge alarms and perform authorized control actions directly from the panel you know remote access for trouble shooting was enabled through smart server allowing qualified support personal too securely view operating screens when required\n\nthe automation system exchanges production metrics with a manufacturing execution system threw O P C U A data includes machine status batch information quality measurements and production counters communication was validated during factory acceptance testing too insure reliable data transfer under normal and fault condtions\n\na redundant power architechture was implemented useing sitop power supplys to increase system availibility critical components were also specified in syplus versions because the installation enviroment is subject too elevated temperatures and airborn contaminates network diagnostics and device monitoring where configured through cinema to provide centralized visibility into infrastructure health\n\nthe project also included integration of legacy equipment connected through profibus existing controllers remained in service during the phase migration reducing production down time engineering documentation hardware configuration software back ups and commissioning reports was archived in accordance with plant standards final testing confirmed stable operation of the symatic platform including the s7 1500 controller wincc unifide runtime environment ET200SP stations and all connected field devices",
                ExpectedOutput = "During the commissioning of a new automation cell, the engineering team configured a SIMATIC S7-1500 PLC in TIA Portal and integrated it with several ET 200SP distributed I/O stations over PROFINET. The control architecture was designed to provide deterministic communication between the controller, operator interfaces, and plant network infrastructure. A SCALANCE managed switch was used to segment traffic and improve network diagnostics. The HMI application was developed using WinCC Unified and deployed to a Comfort Panel located on the main operator station. The interface provides real-time monitoring of production data, alarm management, equipment status, and maintenance information. Operators can review process trends, acknowledge alarms, and perform authorized control actions directly from the panel. Remote access for troubleshooting was enabled through Sm@rtServer, allowing qualified support personnel to securely view operating screens when required. The automation system exchanges production metrics with a manufacturing execution system through OPC UA. Data includes machine status, batch information, quality measurements, and production counters. Communication was validated during factory acceptance testing to ensure reliable data transfer under normal and fault conditions. A redundant power architecture was implemented using SITOP power supplies to increase system availability. Critical components were also specified in SIPLUS versions because the installation environment is subject to elevated temperatures and airborne contaminants. Network diagnostics and device monitoring were configured through SINEMA to provide centralized visibility into infrastructure health. The project also included integration of legacy equipment connected through PROFIBUS. Existing controllers remained in service during the phased migration, reducing production downtime. Engineering documentation, hardware configuration, software backups, and commissioning reports were archived in accordance with plant standards. Final testing confirmed stable operation of the SIMATIC platform, including the S7-1500 controller, WinCC Unified runtime environment, ET 200SP stations, and all connected field devices.",
                ExpectedSubstrings = new[] { "SIMATIC S7-1500 PLC", "TIA Portal", "ET 200SP", "PROFINET", "SCALANCE", "WinCC Unified", "Comfort Panel", "Sm@rtServer", "OPC UA", "SITOP", "SIPLUS", "SINEMA", "PROFIBUS", "cell", "diagnostics", "personnel", "redundant" },
                ForbiddenSubstrings = new[] { "s71500", "profi net", "ET200 SP", "comunication", "scalence", "win cc", "unifide", "comfort pannel", "smart server", "threw O P C U A", "syplus", "cinema", "symatic" }
            },
            new TestPrompt
            {
                Name = "Harsh Siemens Migration",
                Input = "uh okay so during commissioning of the packaging line up grade the engineering team migrated the existing symatic s seven three hundred controller over to a simatic s fifteen hundred platform using tea portal several ee tee two hundred sp remote io stations were connected over profi net while legacy drives remained on profi bus during the transition phrase network communication was monitored through scaleance switches to verify stable industrial internet performance across the production area\n\nthe hmi application was upgraded from win cc flexable to win cc unified and deployed to a comfort pannel located at the operator work station operators use the interface to review alarms monitor process values and acknowledge system events you know remote diagnostics were enabled through smart server allowing maintenance personal to securely access runtime screens whenever necessary\n\npower redundancy was implemented using site top power supplies several field devices were specified in sy plus variants because the installation environment experiences elevated temperatures and dust exposure device diagnostics were aggregated through cinema and presented to maintenance teams for centralized monitoring\n\nproduction data is exchanged with higher level systems through op c u a the engineering team verified communications during factory exceptance testing and sight acceptance testing to ensure reliable operation under both normal and fault conditions\n\nthere testing confirmed proper operation of the somatic system including the s fifteen hundred controller ET200SP stations wincc unified runtime scaleance infrastructure and all connected field devices",
                ExpectedOutput = "During commissioning of the packaging line upgrade, the engineering team migrated the existing SIMATIC S7-300 controller to a SIMATIC S7-1500 platform using TIA Portal. Several ET 200SP remote I/O stations were connected over PROFINET, while legacy drives remained on PROFIBUS during the transition phase. Network communication was monitored through SCALANCE switches to verify stable Industrial Ethernet performance across the production area. The HMI application was upgraded from WinCC flexible to WinCC Unified and deployed to a Comfort Panel located at the operator workstation. Operators use the interface to review alarms, monitor process values, and acknowledge system events. Remote diagnostics were enabled through Sm@rtServer, allowing maintenance personnel to securely access the runtime screens when necessary. Power redundancy was implemented using SITOP power supplies. Several field devices were specified in SIPLUS variants because the installation environment experiences elevated temperatures and dust exposure. Device diagnostics were aggregated through SINEMA and presented to maintenance teams for centralized monitoring. Production data is exchanged with higher-level systems through OPC UA. The engineering team verified communications during factory acceptance testing and site acceptance testing to ensure reliable operation under both normal and fault conditions. Final validation confirmed proper operation of the SIMATIC system, including the S7-1500 controller, ET 200SP stations, WinCC Unified runtime, SCALANCE infrastructure, and all connected field devices.",
                ExpectedSubstrings = new[] { "SIMATIC S7-300", "SIMATIC S7-1500", "TIA Portal", "ET 200SP", "PROFINET", "PROFIBUS", "SCALANCE", "WinCC Unified", "Comfort Panel", "Sm@rtServer", "SITOP", "SIPLUS", "SINEMA", "OPC UA", "factory acceptance testing", "site acceptance testing", "Industrial Ethernet" },
                ForbiddenSubstrings = new[] { "symatic", "s seven three hundred", "s fifteen hundred", "tea portal", "ee tee two hundred sp", "profi net", "profi bus", "scaleance", "win cc", "flexable", "comfort pannel", "smart server", "maintenance personal", "site top", "sy plus", "cinema", "op c u a", "exceptance", "sight acceptance" }
            },
            new TestPrompt
            {
                Name = "Extreme Model Killer",
                Input = "okay so the customer reported that there s7 fifteen hundred cpu was intermittently entering stop mode after a firmware update however there maintenance team believed the issue was related too network congestion on the profi net backbone and not the controller itself during troubleshooting engineering discovered that multiple scaleance switches had duplicate ip addresses and one switch had accidentally been configured as the time master creating synchronization issues across the line\n\nthe original project was developed in tea portal version seventeen but portions of the software had been migrated from step seven classic several hardware modules where incorrectly identified in the documentation including an ee tee two hundred mp rack that was documented as an ee tee two hundred sp station this discrepancy resulted in confusion during replacement procedures\n\noperators reported that win cc unified alarms where not matching alarm descriptions displayed on the comfort panel additionally several maintenance technicians referred too smart server as smart serve or smart saver in service reports making historical records difficult too search\n\nduring testing approximately seventy three percent of communication faults were traced too improperly terminated profibus segments while the remaining failures were linked too intermittent industrial ethernet connectivity production metrics continued to be transmitted through op c ua despite the communication disturbances although timestamps occasionally differed between systems\n\nonce corrections were made the simatic network achieved stable operation and there were no further reports of unexpected cpu stop events",
                ExpectedOutput = "The customer reported that the S7-1500 CPU was intermittently entering stop mode after a firmware update. However, the maintenance team believed the issue was related to network congestion on the PROFINET backbone, not the controller itself. During troubleshooting, engineering discovered that multiple SCALANCE switches had duplicate IP addresses, and one switch had accidentally been configured as the time master, creating synchronization issues across the line. The original project was developed in TIA Portal version 17, but portions of the software had been migrated from STEP 7 Classic. Several hardware modules were incorrectly identified in the documentation, including an ET 200MP rack that was documented as an ET 200SP station. This discrepancy resulted in confusion during replacement procedures. Operators reported that WinCC Unified alarms were not matching alarm descriptions displayed on the Comfort Panel. Additionally, several maintenance technicians referred to Sm@rtServer as \"Smart Serve\" or \"Smart Saver\" in service reports, making historical records difficult to search. During testing, approximately 73% of communication faults were traced to improperly terminated PROFIBUS segments, while the remaining failures were linked to intermittent Industrial Ethernet connectivity. Production metrics continued to be transmitted through OPC UA despite the communication disturbances, although timestamps occasionally differed between systems. Once corrections were made, the SIMATIC network achieved stable operation, and there were no further reports of unexpected CPU stop events.",
                ExpectedSubstrings = new[] { "S7-1500", "PROFINET", "SCALANCE", "TIA Portal", "STEP 7 Classic", "ET 200MP", "ET 200SP", "WinCC Unified", "Comfort Panel", "Sm@rtServer", "PROFIBUS", "Industrial Ethernet", "OPC UA", "SIMATIC", "their" },
                ForbiddenSubstrings = new[] { "there s7", "s7 fifteen hundred", "profi net", "scaleance", "tea portal", "step seven", "ee tee two hundred mp", "ee tee two hundred sp", "win cc", "smart serve", "smart saver", "seventy three percent", "op c ua" }
            },
            new TestPrompt
            {
                Name = "Ambiguity & Inference",
                Input = "the customer upgraded there s seven three hundred controller too a fifteen hundred and replaced the old panels with unified panels because the old flexible project would no longer support there requirements after the update engineering found that the server was connected too the wrong switch and alarms where not being acknowledged correctly",
                ExpectedOutput = "The customer upgraded their S7-300 controller to an S7-1500 and replaced the old panels with WinCC Unified panels because the old WinCC flexible project would no longer support their requirements after the update. Engineering found that the server was connected to the wrong switch and alarms were not being acknowledged correctly.",
                ExpectedSubstrings = new[] { "S7-300", "S7-1500", "WinCC Unified", "WinCC flexible", "their requirements", "connected to", "alarms were" },
                ForbiddenSubstrings = new[] { "there s", "s seven three hundred", "too a fifteen hundred", "flexible project", "there requirements", "connected too", "alarms where" }
            },
            new TestPrompt
            {
                Name = "ASR Nightmare",
                Input = "customer call regarding site top alarm on the ee tee two hundred sp rack tea portal project version nineteen win cc unified runtime not updating values over profi net smart server connection failed after scaleance replacement operators reported the s seven fifteen hundred cpu entered stop mode after power cycle",
                ExpectedOutput = "Customer call regarding a SITOP alarm on the ET 200SP rack. The TIA Portal project version 19 showed that the WinCC Unified runtime was not updating values over PROFINET. The Sm@rtServer connection failed after the SCALANCE replacement. Operators reported that the S7-1500 CPU entered stop mode after a power cycle.",
                ExpectedSubstrings = new[] { "SITOP", "ET 200SP", "TIA Portal", "WinCC Unified", "PROFINET", "Sm@rtServer", "SCALANCE", "S7-1500" },
                ForbiddenSubstrings = new[] { "site top", "ee tee two hundred sp", "tea portal", "win cc", "profi net", "smart server", "scaleance", "s seven fifteen hundred" }
            },
            new TestPrompt
            {
                Name = "Vocabulary Preservation Stress",
                Input = "uh customer said the logo controller was okay but the basic pannel was not showing alarms after the profy net reconnect the tech checked the simatic net settings and found the rugged com switch was powered from a site top supply instead of the documented source the mobile pannel also lost connection to smart server and the key pannel buttons were mapped wrong in tea portal",
                ExpectedOutput = "The customer said the LOGO! controller was okay, but the Basic Panel was not showing alarms after the PROFINET reconnect. The technician checked the SIMATIC NET settings and found that the RUGGEDCOM switch was powered from a SITOP supply instead of the documented source. The Mobile Panel also lost connection to Sm@rtServer, and the Key Panel buttons were mapped incorrectly in TIA Portal.",
                ExpectedSubstrings = new[] { "LOGO!", "Basic Panel", "PROFINET", "SIMATIC NET", "RUGGEDCOM", "SITOP", "Mobile Panel", "Sm@rtServer", "Key Panel", "TIA Portal" },
                ForbiddenSubstrings = new[] { "logo controller", "basic pannel", "profy net", "rugged com", "site top", "mobile pannel", "smart server", "key pannel", "tea portal" }
            },
            new TestPrompt
            {
                Name = "Long Run-On Service Report",
                Input = "so we got onsite and the customer said the machine had stopped three times this week but nobody had a clear fault history because the win cc unified alarm view was filtered wrong and the operator thought the comfort panel was frozen after checking the s seven fifteen hundred online diagnostics in tea portal we found intermittent profy net device failures on two ee tee two hundred sp stations and one scaleance switch showed link flapping on port five the maintenance team had already replaced a cable but the fault came back after the line restarted the site top power supply was stable but the sip plus module cabinet temperature was higher than expected because the fan filter was blocked engineering updated the documentation corrected the device names in simatic net and verified that op c ua data was reaching the mes system again",
                ExpectedOutput = "We arrived onsite, and the customer said the machine had stopped three times this week. However, nobody had a clear fault history because the WinCC Unified alarm view was filtered incorrectly, and the operator thought the Comfort Panel was frozen. After checking the S7-1500 online diagnostics in TIA Portal, we found intermittent PROFINET device failures on two ET 200SP stations, and one SCALANCE switch showed link flapping on port 5. The maintenance team had already replaced a cable, but the fault returned after the line restarted. The SITOP power supply was stable, but the SIPLUS module cabinet temperature was higher than expected because the fan filter was blocked. Engineering updated the documentation, corrected the device names in SIMATIC NET, and verified that OPC UA data was reaching the MES system again.",
                ExpectedSubstrings = new[] { "WinCC Unified", "Comfort Panel", "S7-1500", "TIA Portal", "PROFINET", "ET 200SP", "SCALANCE", "SITOP", "SIPLUS", "SIMATIC NET", "OPC UA" },
                ForbiddenSubstrings = new[] { "win cc", "comfort pannel", "s seven fifteen hundred", "tea portal", "profy net", "ee tee two hundred sp", "scaleance", "site top", "sip plus", "simatic net", "op c ua" }
            }
        };

        public static IEnumerable<object[]> GetLLMModels()
        {
            try
            {
                var llmDir = ModelPathHelper.GetLLMModelsDirectory();
                var ggufFiles = Directory.GetFiles(llmDir, "*.gguf");

                if (ggufFiles.Length == 0)
                {
                    var rootDir = ModelPathHelper.GetModelsDirectory();
                    ggufFiles = Directory.GetFiles(rootDir, "*.gguf");
                }

                if (ggufFiles.Length > 0)
                {
                    return ggufFiles.Select(f => new object[] { Path.GetFileName(f), f });
                }
            }
            catch
            {
                // Directory scanning failed or directory not found during discovery
            }

            // Fallback list of models so they are ALWAYS listed in the Test Explorer
            return new List<object[]>
            {
                new object[] { "gemma-4-E4B-it-Q4_K_M.gguf", "" },
                new object[] { "Llama-3.2-3B-Instruct-Q8_0.gguf", "" },
                new object[] { "qwen2-0_5b-instruct-q8_0.gguf", "" },
                new object[] { "gemma-4-E2B-it-Q4_0.gguf", "" }
            };
        }

        public static float[] GetConfiguredTemperatures()
        {
            // 1. Try Environment Variable (dynamic overrides)
            string? envValue = Environment.GetEnvironmentVariable("BENCHMARK_TEMPERATURES");
            if (!string.IsNullOrWhiteSpace(envValue))
            {
                try
                {
                    var temps = envValue.Split(',')
                        .Select(s => float.Parse(s.Trim(), System.Globalization.CultureInfo.InvariantCulture))
                        .ToArray();
                    if (temps.Length > 0) return temps;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Benchmark Config] Failed to parse BENCHMARK_TEMPERATURES env var: {ex.Message}");
                }
            }

            // 2. Try benchmark_config.json in Solution or Project folder (persistent settings)
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var dir = new DirectoryInfo(baseDir);
                while (dir != null)
                {
                    var configPath = Path.Combine(dir.FullName, "benchmark_config.json");
                    if (File.Exists(configPath))
                    {
                        var json = File.ReadAllText(configPath);
                        BenchmarkConfig? config = System.Text.Json.JsonSerializer.Deserialize<BenchmarkConfig>(json);
                        if (config != null && config.Temperatures != null && config.Temperatures.Length > 0)
                        {
                            return config.Temperatures;
                        }
                    }

                    var nestedConfigPath = Path.Combine(dir.FullName, "SmartDictateAI.PerformanceTests", "benchmark_config.json");
                    if (File.Exists(nestedConfigPath))
                    {
                        var json = File.ReadAllText(nestedConfigPath);
                        BenchmarkConfig? config = System.Text.Json.JsonSerializer.Deserialize<BenchmarkConfig>(json);
                        if (config != null && config.Temperatures != null && config.Temperatures.Length > 0)
                        {
                            return config.Temperatures;
                        }
                    }

                    dir = dir.Parent;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Benchmark Config] Failed to read benchmark_config.json: {ex.Message}");
            }

            // 3. Default Fallback
            return new float[] { 0.0f, 0.7f };
        }

        private class BenchmarkConfig
        {
            public float[] Temperatures { get; set; } = Array.Empty<float>();
        }

        [Fact]
        [Trait("Category", "Performance")]
        public async Task Benchmark_Stage2_LLM_Models()
        {
            if (!PerformanceTestHelper.ShouldRun())
            {
                Console.WriteLine("Bypassing LLM benchmarks (performance runs are not enabled).");
                return;
            }

            var models = GetLLMModels().ToList();
            foreach (var model in models)
            {
                var modelName = (string)model[0];
                var modelPath = (string)model[1];
                await RunSingleLLMModelBenchmark(modelName, modelPath);
            }
        }

        private async Task RunSingleLLMModelBenchmark(string modelName, string modelPath)
        {

            // Resolve path if using the fallback model list
            if (string.IsNullOrEmpty(modelPath))
            {
                try
                {
                    var llmDir = ModelPathHelper.GetLLMModelsDirectory();
                    modelPath = Path.Combine(llmDir, modelName);
                    if (!File.Exists(modelPath))
                    {
                        var rootDir = ModelPathHelper.GetModelsDirectory();
                        modelPath = Path.Combine(rootDir, modelName);
                    }
                }
                catch
                {
                    modelPath = modelName; // Fallback
                }
            }

            Assert.True(File.Exists(modelPath), $"Model file '{modelName}' was not found on disk at '{modelPath}'. Place it in 'models/llm/' to run the benchmark.");

            var temperatures = GetConfiguredTemperatures();
            foreach (var temperature in temperatures)
            {
                var fileSizeGb = new FileInfo(modelPath).Length / (1024.0 * 1024.0 * 1024.0);

                int totalSentences = 0;
                foreach (var tc in TestCases)
                {
                    var expectedSentences = tc.ExpectedOutput.Split(new[] { ". ", "? ", "! ", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => s.Length > 0);
                    totalSentences += expectedSentences.Count();
                }

                var benchmarkResult = new ModelBenchmarkResult
                {
                    ModelName = modelName,
                    Temperature = temperature,
                    ModelType = "LLM",
                    FileSizeGb = fileSizeGb,
                    TotalCases = totalSentences
                };

                // Setup AppSettings specifically for this model benchmark
                var settings = new AppSettings
                {
                    LocalLLMModelPath = modelPath,
                    LLMContextSize = 2048, // Use a conservative context size for benchmark compatibility
                    LLMSeed = 42,          // Fix the seed for deterministic outputs
                    LLMTemperature = temperature, // Set the temperature under test
                    LLMMaxOutputTokens = 2048,
                    UseGpu = true          // Attempt GPU usage
                };

                using var llmService = new LLMService();

                // Memory peak tracking
                double peakRamMb = 0;
                double peakVramMb = 0;
                var cts = new CancellationTokenSource();

                // Setup Windows performance counters for VRAM if available
                var vramCounters = new List<PerformanceCounter>();
                try
                {
                    var pid = Process.GetCurrentProcess().Id;
                    var category = new PerformanceCounterCategory("GPU Process Memory");
                    var instances = category.GetInstanceNames();
                    var pidPrefix = $"pid_{pid}_";
                    foreach (var instance in instances)
                    {
                        if (instance.StartsWith(pidPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            vramCounters.Add(new PerformanceCounter("GPU Process Memory", "Dedicated Usage", instance, true));
                        }
                    }
                }
                catch { /* Headless or OS non-supported */ }

                // Start memory polling task
                var memoryMonitorTask = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        long currentRamBytes = Process.GetCurrentProcess().WorkingSet64;
                        double ramMb = currentRamBytes / (1024.0 * 1024.0);
                        if (ramMb > peakRamMb) peakRamMb = ramMb;

                        if (vramCounters.Count > 0)
                        {
                            float currentVramBytes = 0;
                            foreach (var c in vramCounters)
                            {
                                try { currentVramBytes += c.NextValue(); } catch { }
                            }
                            double vramMb = currentVramBytes / (1024.0 * 1024.0);
                            if (vramMb > peakVramMb) peakVramMb = vramMb;
                        }

                        await Task.Delay(50, cts.Token);
                    }
                });

                // Measure initialization / model loading time
                var loadSw = Stopwatch.StartNew();
                bool initSuccess = llmService.Initialize(settings.LocalLLMModelPath, settings.LLMContextSize, settings.UseGpu, msg => Console.WriteLine(msg));
                loadSw.Stop();

                benchmarkResult.LoadTimeSec = loadSw.Elapsed.TotalSeconds;

                if (!initSuccess)
                {
                    cts.Cancel();
                    try { await memoryMonitorTask; } catch { }
                    foreach (var c in vramCounters) c.Dispose();

                    benchmarkResult.PassedCases = 0;
                    benchmarkResult.TestCases = TestCases.Select(tc => new TestCaseResult
                    {
                        TestCaseName = tc.Name,
                        InputText = tc.Input,
                        OutputText = "INIT_FAILED",
                        Passed = false,
                        AssertionSummary = "Model could not be initialized."
                    }).ToList();

                    PerformanceReportGenerator.AddResult(benchmarkResult);
                    continue;
                }

                double totalSpeedTps = 0;
                double totalDuration = 0;

                foreach (var testCase in TestCases)
                {
                    double currentTps = 0;
                    var debugLogs = new List<string>();

                    var testCaseSw = Stopwatch.StartNew();
                    var refinedOutput = await llmService.RefineTextAsync(
                        testCase.Input, 
                        settings, 
                        onDebugMessage: msg =>
                        {
                            debugLogs.Add(msg);
                            Console.WriteLine(msg);

                            if (msg.Contains("[LLM] Done | streamParts="))
                            {
                                // Parse speed: "[LLM] Done | streamParts=25 | sec=1.23 | 20.3 tok/s-ish"
                                var parts = msg.Split('|');
                                foreach (var part in parts)
                                {
                                    if (part.Contains("tok/s-ish"))
                                    {
                                        var valStr = part.Replace("tok/s-ish", "").Trim();
                                        if (double.TryParse(valStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedTps))
                                        {
                                            currentTps = parsedTps;
                                        }
                                    }
                                }
                            }
                        });
                    testCaseSw.Stop();

                    double caseDurationSec = testCaseSw.Elapsed.TotalSeconds;
                    totalDuration += caseDurationSec;

                    // Fallback speed calculation if debug log parsing failed
                    if (currentTps == 0 && caseDurationSec > 0)
                    {
                        // Estimate token count as character count / 4
                        int estimatedTokens = Math.Max(1, refinedOutput.Length / 4);
                        currentTps = estimatedTokens / caseDurationSec;
                    }
                    totalSpeedTps += currentTps;

                    // Normalize the refined output (strip punctuation, collapse whitespace) for substring checking
                    var normalizedOutput = NormalizeText(refinedOutput);

                    // Split ExpectedOutput into sentences
                    var expectedSentences = testCase.ExpectedOutput.Split(new[] { ". ", "? ", "! ", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => s.Length > 0)
                        .ToList();

                    int passedSentences = 0;
                    var failedSentences = new List<string>();

                    foreach (var sentence in expectedSentences)
                    {
                        var normalizedSentence = NormalizeText(sentence);
                        if (normalizedSentence.Length > 0 && normalizedOutput.Contains(normalizedSentence, StringComparison.Ordinal))
                        {
                            passedSentences++;
                        }
                        else
                        {
                            failedSentences.Add(sentence);
                        }
                    }

                    bool passed = failedSentences.Count == 0;
                    benchmarkResult.PassedCases += passedSentences;

                    var notes = new List<string>();
                    notes.Add($"Sentences: {passedSentences}/{expectedSentences.Count} passed");
                    if (failedSentences.Count > 0)
                    {
                        notes.Add($"Failed: {string.Join(" | ", failedSentences.Select(s => $"\"{s}\""))}");
                    }
                    else
                    {
                        notes.Add("Correct spelling & grammar applied successfully.");
                    }

                    benchmarkResult.TestCases.Add(new TestCaseResult
                    {
                        TestCaseName = testCase.Name,
                        InputText = testCase.Input,
                        OutputText = refinedOutput,
                        DurationSec = caseDurationSec,
                        SpeedTpsOrRtf = currentTps,
                        Passed = passed,
                        AssertionSummary = string.Join(" | ", notes)
                    });
                }

                // Stop memory monitoring
                cts.Cancel();
                try { await memoryMonitorTask; } catch { }
                foreach (var c in vramCounters) c.Dispose();

                benchmarkResult.AvgDurationSec = totalDuration / TestCases.Count;
                benchmarkResult.AvgSpeedTpsOrRtf = totalSpeedTps / TestCases.Count;
                benchmarkResult.PeakRamMb = peakRamMb;
                benchmarkResult.PeakVramMb = peakVramMb;

                benchmarkResult.AccuracyScore = benchmarkResult.TotalCases > 0 ? (double)benchmarkResult.PassedCases / benchmarkResult.TotalCases : 0;

                // Add result & trigger report update
                PerformanceReportGenerator.AddResult(benchmarkResult);

                // Clean up resources explicitly
                await llmService.DisposeResourcesAsync(msg => Console.WriteLine(msg));

                // Force Garbage Collection and wait for cooldown to let GPU driver free VRAM
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(1500);
            }
        }

        private static string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
                else if (char.IsWhiteSpace(c))
                {
                    sb.Append(' ');
                }
            }
            return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }
    }
}
