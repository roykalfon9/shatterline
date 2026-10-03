using Shatterline;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShatterlineEditor
{
    public static class PresentationSetup
    {
        [MenuItem("Tools/Shatterline/Apply Neon Arena")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before applying presentation.");
            var camera = Camera.main;
            var paddle = Object.FindFirstObjectByType<PaddleController>();
            if (camera == null || paddle == null) throw new System.InvalidOperationException("Open Game.unity first.");
            System.IO.Directory.CreateDirectory("Assets/Art/Materials");
            var glass = MaterialAt("BrickGlass", "Shatterline/NeonGlass");
            var paddleGlass = MaterialAt("PaddleGlass", "Shatterline/NeonGlass");
            paddleGlass.SetFloat("_Aspect", 5f);
            EditorUtility.SetDirty(paddleGlass);
            var background = MaterialAt("Arena", "Shatterline/Arena");
            var trailMaterial = MaterialAt("BallTrail", "Sprites/Default");

            EditBrick(glass);
            EditBall(trailMaterial);
            ApplyAudio();
            var game = Object.FindFirstObjectByType<GameManager>();
            var gameData = new SerializedObject(game);
            gameData.FindProperty("serveCountdownStepSeconds").floatValue = .3f;
            gameData.ApplyModifiedProperties();
            Undo.RecordObject(paddle.GetComponent<SpriteRenderer>(), "Style paddle");
            paddle.GetComponent<SpriteRenderer>().sharedMaterial = paddleGlass;
            paddle.GetComponent<SpriteRenderer>().color = new Color(.3f, .9f, 1f);

            var arena = GameObject.Find("ArenaBackground");
            if (arena == null)
            {
                arena = GameObject.CreatePrimitive(PrimitiveType.Quad);
                arena.name = "ArenaBackground";
                Undo.RegisterCreatedObjectUndo(arena, "Create arena backdrop");
                Undo.DestroyObjectImmediate(arena.GetComponent<Collider>());
            }
            Undo.RecordObject(arena.transform, "Size arena");
            arena.transform.position = new Vector3(0, 0, 2);
            arena.transform.localScale = new Vector3(9, 16, 1);
            var renderer = arena.GetComponent<MeshRenderer>();
            Undo.RecordObject(renderer, "Style arena");
            renderer.sharedMaterial = background;
            renderer.sortingLayerName = "Background";
            renderer.sortingOrder = -100;

            Undo.RecordObject(camera, "Dark camera surround");
            camera.backgroundColor = new Color(.012f, .021f, .047f);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
            EditorSceneManager.SaveScene(camera.gameObject.scene);
            Debug.Log("SHATTERLINE: neon arena, glass bricks, paddle and ball trail saved.");
        }

        static void ApplyAudio()
        {
            var audio = Object.FindFirstObjectByType<AudioManager>();
            if (audio == null) throw new System.InvalidOperationException("Missing AudioManager.");
            Undo.RecordObject(audio, "Assign original audio");
            var data = new SerializedObject(audio);
            data.FindProperty("musicClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/neon_orbit.wav");
            data.FindProperty("levelClearClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_levelclear.wav");
            data.FindProperty("toughHitClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_toughhit.wav");
            var source = (AudioSource)data.FindProperty("musicSource").objectReferenceValue;
            data.ApplyModifiedProperties();
            Undo.RecordObject(source, "Balance music volume");
            source.volume = .16f;
            EditorUtility.SetDirty(source);
        }

        static Material MaterialAt(string name, string shaderName)
        {
            string path = $"Assets/Art/Materials/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new System.InvalidOperationException($"Missing shader: {shaderName}");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void EditBrick(Material glass)
        {
            const string path = "Assets/Prefabs/Brick.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                prefab.GetComponent<SpriteRenderer>().sharedMaterial = glass;
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }

        static void EditBall(Material material)
        {
            const string path = "Assets/Prefabs/Ball.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                // Match the visible ball to its existing physics diameter.
                var sprite = prefab.GetComponent<SpriteRenderer>();
                var collider = prefab.GetComponent<CircleCollider2D>();
                float diameter = collider.radius * prefab.transform.localScale.x * 2f;
                float nativeDiameter = sprite.sprite.bounds.size.x;
                prefab.transform.localScale = Vector3.one * (diameter / nativeDiameter);
                collider.radius = nativeDiameter * .5f;
                var trail = prefab.GetComponent<TrailRenderer>();
                if (trail == null) trail = prefab.AddComponent<TrailRenderer>();
                trail.sharedMaterial = material;
                trail.time = .14f;
                trail.startWidth = .16f;
                trail.endWidth = .01f;
                trail.minVertexDistance = .035f;
                trail.numCapVertices = 4;
                trail.sortingLayerName = "Ball";
                trail.sortingOrder = -1;
                trail.emitting = false;
                trail.startColor = new Color(.35f,.9f,1f,.65f);
                trail.endColor = new Color(.12f,.4f,1f,0f);
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
    }
}
