using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace PaperFootball.Editor
{
    public static class PaperFootballValidationRunner
    {
        public static void RunEditModeTests()
        {
            RunTests(TestMode.EditMode);
        }

        public static void RunPlayModeTests()
        {
            RunTests(TestMode.PlayMode);
        }

        private static void RunTests(TestMode testMode)
        {
            string resultPath = GetArgumentValue("-paperFootballTestResults");
            if (string.IsNullOrWhiteSpace(resultPath))
            {
                resultPath = Path.Combine(Path.GetTempPath(), $"paperfootball-{testMode.ToString().ToLowerInvariant()}-results.txt");
            }

            TestRunnerApi api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new ExitOnCompletionCallbacks(testMode, resultPath));
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = testMode
            }));
        }

        private static string GetArgumentValue(string name)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return string.Empty;
        }

        private sealed class ExitOnCompletionCallbacks : ICallbacks
        {
            private readonly TestMode testMode;
            private readonly string resultPath;
            private readonly List<string> failures = new();

            public ExitOnCompletionCallbacks(TestMode testMode, string resultPath)
            {
                this.testMode = testMode;
                this.resultPath = resultPath;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log($"PaperFootballValidationRunner: {testMode} tests started.");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                StringBuilder builder = new();
                builder.AppendLine($"Mode: {testMode}");
                builder.AppendLine($"Result: {result.ResultState}");
                builder.AppendLine($"Passed: {result.PassCount}");
                builder.AppendLine($"Failed: {result.FailCount}");
                builder.AppendLine($"Skipped: {result.SkipCount}");
                builder.AppendLine($"Inconclusive: {result.InconclusiveCount}");
                builder.AppendLine($"Duration: {result.Duration:F3}s");

                if (failures.Count > 0)
                {
                    builder.AppendLine("Failures:");
                    foreach (string failure in failures)
                    {
                        builder.AppendLine(failure);
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(resultPath) ?? Path.GetTempPath());
                File.WriteAllText(resultPath, builder.ToString());
                Debug.Log(builder.ToString());
                EditorApplication.Exit(result.FailCount > 0 ? 1 : 0);
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.FailCount <= 0)
                {
                    return;
                }

                failures.Add($"{result.FullName}: {result.Message}\n{result.StackTrace}");
            }
        }
    }
}
