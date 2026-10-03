using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace ShatterlineEditor
{
    [InitializeOnLoad]
    public static class SubmissionTests
    {
        static SubmissionTests()
        {
            TestRunnerApi.RegisterTestCallback(new Results());
        }

        [MenuItem("Tools/Shatterline/Run Playability Tests")]
        public static void Run()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.PlayMode,
                assemblyNames = new[] { "Shatterline.PlayModeTests" }
            }));
        }

        class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                Directory.CreateDirectory("Logs");
                TestRunnerApi.SaveResultToFile(result, "Logs/PlayabilityTests.xml");
                Debug.Log($"SHATTERLINE tests: {result.PassCount} passed, {result.FailCount} failed. Logs/PlayabilityTests.xml");
            }
        }
    }
}
