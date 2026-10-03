using System.IO;
using Shatterline;
using UnityEditor;
using UnityEngine;

namespace ShatterlineEditor
{
    public static class SubmissionCapture
    {
        [MenuItem("Tools/Shatterline/Capture Game Screenshot")]
        public static void Capture()
        {
            if (!EditorApplication.isPlaying || GameManager.Instance == null)
                throw new System.InvalidOperationException("Enter Play Mode before capturing the game.");
            Directory.CreateDirectory("Docs/Screenshots");
            string name = GameManager.Instance.State == GameState.MainMenu ? "menu" : "gameplay";
            ScreenCapture.CaptureScreenshot($"Docs/Screenshots/{name}.png");
            Debug.Log($"SHATTERLINE screenshot queued: Docs/Screenshots/{name}.png");
        }
    }
}
