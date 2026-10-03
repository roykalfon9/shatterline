using System.Collections.Generic;
using Shatterline;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShatterlineEditor
{
    /// <summary>Explicit authoring command; never runs automatically over hand-edited levels.</summary>
    public static class SubmissionContent
    {
        [MenuItem("Tools/Shatterline/Apply Five Level Set")]
        public static void ApplyLevels()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before authoring levels.");
            var levels = new[]
            {
                WriteLevel(1, "First Light", new Color(.18f,.85f,1f),
                    "00200200", "01111110", "01111110", "01111110"),
                WriteLevel(2, "Split Signal", new Color(.63f,.42f,1f),
                    "22200222", "11100111", "11100111", "01111110", "00111100"),
                WriteLevel(3, "Prism", new Color(1f,.35f,.66f),
                    "00022000", "00222200", "02111120", "21111112", "01111110", "00111100"),
                WriteLevel(4, "Crosscurrent", new Color(.22f,1f,.65f),
                    "22022022", "11011011", "01100110", "00111100", "01100110", "11011011"),
                WriteLevel(5, "Shatterline", new Color(1f,.68f,.24f),
                    "22222222", "21111112", "21011012", "11011011", "01111110", "11100111")
            };
            var game = Object.FindFirstObjectByType<GameManager>();
            if (game == null) throw new System.InvalidOperationException("Open Game.unity before applying levels.");
            Undo.RecordObject(game, "Assign five levels");
            var data = new SerializedObject(game);
            var list = data.FindProperty("levels");
            list.arraySize = levels.Length;
            for (int i = 0; i < levels.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            data.ApplyModifiedProperties();
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            EditorSceneManager.SaveScene(game.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("SHATTERLINE: five authored levels assigned and saved.");
        }

        static LevelData WriteLevel(int number, string title, Color accent, params string[] rows)
        {
            string path = $"Assets/ScriptableObjects/Level{number}.asset";
            var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(level, path);
            }
            Undo.RecordObject(level, "Author level");
            level.levelNumber = number;
            level.displayName = title;
            level.accentColor = accent;
            level.rows = new List<BrickRow>();
            foreach (string line in rows)
            {
                var row = new BrickRow { hp = new int[line.Length] };
                for (int i = 0; i < line.Length; i++) row.hp[i] = line[i] - '0';
                level.rows.Add(row);
            }
            EditorUtility.SetDirty(level);
            return level;
        }
    }
}
