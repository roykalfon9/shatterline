using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;
using Shatterline;

namespace ShatterlineEditor
{
    /// <summary>
    /// Non-scene project setup: player settings, quality presets, sprite import
    /// settings, procedurally-built placeholder art, the input actions asset,
    /// TMP font asset, and the ScriptableObject/prefab assets the scene needs.
    /// Called by GameSetup's single "Build Game Scene" menu command.
    /// </summary>
    public static class ProjectSetup
    {
        const string ArtDir = "Assets/Art/Sprites";
        const string PowerUpArtDir = "Assets/Art/Sprites/PowerUps";
        const string ScriptableObjectsDir = "Assets/ScriptableObjects";
        const string PrefabsDir = "Assets/Prefabs";

        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Roy Kalfon";
            PlayerSettings.productName = "SHATTERLINE";

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.roykalfon.shatterline");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.roykalfon.shatterline");
        }

        public static void EnsureTagsAndLayers()
        {
            EnsureTag("Paddle");
            EnsureTag("Ball");
            EnsureSortingLayer("Background");
            EnsureSortingLayer("Bricks");
            EnsureSortingLayer("PowerUps");
            EnsureSortingLayer("Ball");
            EnsureSortingLayer("Paddle");
            EnsureSortingLayer("VFX");
        }

        static void EnsureTag(string tag)
        {
            var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var so = new SerializedObject(tagManager);
            SerializedProperty tags = so.FindProperty("tags");

            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                    return;
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureSortingLayer(string layerName)
        {
            var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var so = new SerializedObject(tagManager);
            SerializedProperty layers = so.FindProperty("m_SortingLayers");

            for (int i = 0; i < layers.arraySize; i++)
            {
                SerializedProperty nameProp = layers.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                if (nameProp != null && nameProp.stringValue == layerName)
                    return;
            }

            int newIndex = layers.arraySize;
            layers.InsertArrayElementAtIndex(newIndex);
            SerializedProperty newLayer = layers.GetArrayElementAtIndex(newIndex);
            newLayer.FindPropertyRelative("name").stringValue = layerName;
            SerializedProperty uniqueId = newLayer.FindPropertyRelative("uniqueID");
            if (uniqueId != null)
                uniqueId.intValue = 1000 + newIndex;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ConfigureQualityPresets()
        {
            string pcPath = "Assets/Settings/PC_RPAsset.asset";
            string mobilePath = "Assets/Settings/Mobile_RPAsset.asset";
            string sourcePath = "Assets/Settings/UniversalRP.asset";

            UniversalRenderPipelineAsset pcAsset = LoadOrCloneUrpAsset(sourcePath, pcPath);
            pcAsset.msaaSampleCount = 4;
            pcAsset.renderScale = 1f;
            pcAsset.shadowDistance = 30f;
            EditorUtility.SetDirty(pcAsset);

            UniversalRenderPipelineAsset mobileAsset = LoadOrCloneUrpAsset(sourcePath, mobilePath);
            mobileAsset.msaaSampleCount = 2;
            mobileAsset.renderScale = 0.9f;
            mobileAsset.shadowDistance = 15f;
            EditorUtility.SetDirty(mobileAsset);

            AssetDatabase.SaveAssets();

            var qualitySettingsObjects = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (qualitySettingsObjects.Length > 0)
            {
                var so = new SerializedObject(qualitySettingsObjects[0]);
                SerializedProperty levels = so.FindProperty("m_QualitySettings");

                // Re-purpose two rungs of Unity's default quality ladder as our
                // named PC/Mobile presets (tries both the original and already-
                // renamed name, so re-running this is idempotent).
                RenameLevelAndAssignRenderPipeline(levels, new[] { "Very High", "PC" }, "PC", pcAsset);
                RenameLevelAndAssignRenderPipeline(levels, new[] { "Low", "Mobile" }, "Mobile", mobileAsset);

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            QualitySettings.renderPipeline = pcAsset;
        }

        static UniversalRenderPipelineAsset LoadOrCloneUrpAsset(string sourcePath, string targetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(targetPath);
            if (existing != null)
                return existing;

            AssetDatabase.CopyAsset(sourcePath, targetPath);
            AssetDatabase.ImportAsset(targetPath);
            return AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(targetPath);
        }

        static void RenameLevelAndAssignRenderPipeline(SerializedProperty levels, string[] candidateCurrentNames, string newName, RenderPipelineAsset asset)
        {
            for (int i = 0; i < levels.arraySize; i++)
            {
                SerializedProperty level = levels.GetArrayElementAtIndex(i);
                SerializedProperty nameProp = level.FindPropertyRelative("name");
                if (nameProp == null || System.Array.IndexOf(candidateCurrentNames, nameProp.stringValue) < 0)
                    continue;

                nameProp.stringValue = newName;
                SerializedProperty rpProp = level.FindPropertyRelative("renderPipeline");
                if (rpProp != null)
                    rpProp.objectReferenceValue = asset;
                return;
            }
            Debug.LogWarning($"SHATTERLINE setup: no quality level named any of [{string.Join(", ", candidateCurrentNames)}] found; " +
                $"assign {asset.name} to a quality level manually via Project Settings > Quality.");
        }

        public static void ConfigureSpriteImportSettings()
        {
            string[] paths =
            {
                $"{ArtDir}/paddle.png",
                $"{ArtDir}/ball.png",
                $"{ArtDir}/brick_red.png",
                $"{ArtDir}/brick_yellow.png",
                $"{ArtDir}/brick_green.png",
                $"{ArtDir}/brick_blue.png",
            };

            foreach (string path in paths)
                ApplySpriteImportSettings(path);
        }

        static void ApplySpriteImportSettings(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        public static Dictionary<PowerUpType, Sprite> EnsurePowerUpPlaceholderSprites()
        {
            Directory.CreateDirectory(PowerUpArtDir);

            var result = new Dictionary<PowerUpType, Sprite>
            {
                [PowerUpType.WidenPaddle] = EnsurePlaceholderSprite("powerup_widen.png", new Color(0.25f, 0.65f, 1f)),
                [PowerUpType.SlowBall] = EnsurePlaceholderSprite("powerup_slow.png", new Color(0.65f, 0.35f, 0.95f)),
                [PowerUpType.MultiBall] = EnsurePlaceholderSprite("powerup_multi.png", new Color(1f, 0.6f, 0.15f)),
            };
            return result;
        }

        static Sprite EnsurePlaceholderSprite(string fileName, Color color)
        {
            string path = $"{PowerUpArtDir}/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
                return existing;

            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ApplySpriteImportSettings(path);

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void EnsureTmpSettings()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
                return;

            // AssetDatabase.ImportPackage() is asynchronous and never finishes
            // within a single -executeMethod batchmode call before -quit, so
            // the TMP Essential Resources are committed to the repo directly
            // (Assets/TextMesh Pro/...) instead of being imported here.
            Debug.LogWarning("SHATTERLINE setup: Assets/TextMesh Pro (TMP Essential Resources) is missing. " +
                "In the Editor, run Window > TextMeshPro > Import TMP Essential Resources, then re-run this command.");
        }

        public static TMP_FontAsset EnsureTmpFontAsset()
        {
            string path = "Assets/Art/Fonts/KenneyFuture SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null)
                return existing;

            EnsureTmpSettings();

            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/KenneyFuture.ttf");
            if (font == null)
                return null;

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fontAsset.name = "KenneyFuture SDF";

            AssetDatabase.CreateAsset(fontAsset, path);
            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            if (fontAsset.material != null)
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        public static InputActionAsset EnsureInputActions()
        {
            string path = "Assets/Scripts/ShatterlineControls.inputactions";
            var existing = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            if (existing != null)
                return existing;

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();

            InputActionMap gameplay = asset.AddActionMap("Gameplay");

            InputAction move = gameplay.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick/x");
            move.AddBinding("<Gamepad>/dpad/x");
            move.AddBinding("<Touchscreen>/primaryTouch/delta/x").WithProcessor("scale(factor=0.08)");

            // Mouse is absolute cursor-tracking (GDD §4), not a relative drag
            // like touch, so it gets its own action rather than sharing Move's
            // rate-based semantics. Raw screen-space X; PaddleController
            // converts to world space and only trusts it while it's moving.
            InputAction mouseX = gameplay.AddAction("MouseX", InputActionType.Value);
            mouseX.AddBinding("<Mouse>/position/x");

            InputAction launch = gameplay.AddAction("Launch", InputActionType.Button);
            launch.AddBinding("<Keyboard>/space");
            launch.AddBinding("<Mouse>/leftButton");
            launch.AddBinding("<Gamepad>/buttonSouth");
            launch.AddBinding("<Touchscreen>/primaryTouch/tap");

            InputAction pause = gameplay.AddAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape");
            pause.AddBinding("<Gamepad>/start");

            InputActionMap ui = asset.AddActionMap("UI");

            InputAction point = ui.AddAction("Point", InputActionType.PassThrough);
            point.AddBinding("<Mouse>/position");
            point.AddBinding("<Pen>/position");
            point.AddBinding("<Touchscreen>/position");

            InputAction click = ui.AddAction("Click", InputActionType.PassThrough);
            click.AddBinding("<Mouse>/leftButton");
            click.AddBinding("<Touchscreen>/primaryTouch/tap");

            InputAction submit = ui.AddAction("Submit", InputActionType.Button);
            submit.AddBinding("<Keyboard>/enter");
            submit.AddBinding("<Keyboard>/space");
            submit.AddBinding("<Gamepad>/buttonSouth");

            InputAction cancel = ui.AddAction("Cancel", InputActionType.Button);
            cancel.AddBinding("<Keyboard>/escape");
            cancel.AddBinding("<Gamepad>/buttonEast");

            InputAction navigate = ui.AddAction("Navigate", InputActionType.PassThrough);
            navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            navigate.AddBinding("<Gamepad>/leftStick");

            string json = asset.ToJson();
            File.WriteAllText(path, json);
            Object.DestroyImmediate(asset);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }

        public static GameConfig EnsureGameConfig()
        {
            string path = $"{ScriptableObjectsDir}/GameConfig.asset";
            var existing = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
            if (existing != null)
                return existing;

            var config = ScriptableObject.CreateInstance<GameConfig>();
            Directory.CreateDirectory(ScriptableObjectsDir);
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            return config;
        }

        public static LevelData[] EnsureLevelData()
        {
            Directory.CreateDirectory(ScriptableObjectsDir);
            var level1 = EnsureLevel(1, BuildSolidLayout(8, 6));
            return new[] { level1 };
        }

        static List<BrickRow> BuildSolidLayout(int columns, int rows)
        {
            var list = new List<BrickRow>();
            for (int r = 0; r < rows; r++)
            {
                var row = new BrickRow { hp = new int[columns] };
                int hp = r < 2 ? 2 : 1; // top two rows are tough
                for (int c = 0; c < columns; c++)
                    row.hp[c] = hp;
                list.Add(row);
            }
            return list;
        }

        static LevelData EnsureLevel(int levelNumber, List<BrickRow> rows)
        {
            string path = $"{ScriptableObjectsDir}/Level{levelNumber}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (existing != null)
                return existing;

            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelNumber = levelNumber;
            level.rows = rows;
            AssetDatabase.CreateAsset(level, path);
            AssetDatabase.SaveAssets();
            return level;
        }

        public static BallController EnsureBallPrefab()
        {
            string path = $"{PrefabsDir}/Ball.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing.GetComponent<BallController>();

            var physMat = EnsureBouncyMaterial();
            var go = new GameObject("Ball");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/ball.png");
            sr.sortingLayerName = "Ball";

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.25f;
            col.sharedMaterial = physMat;
            go.tag = "Ball";
            go.transform.localScale = Vector3.one * 0.5f;

            go.AddComponent<BallController>();

            Directory.CreateDirectory(PrefabsDir);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<BallController>();
        }

        public static Brick EnsureBrickPrefab()
        {
            string path = $"{PrefabsDir}/Brick.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing.GetComponent<Brick>();

            var physMat = EnsureBouncyMaterial();
            var go = new GameObject("Brick");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Bricks";

            var col = go.AddComponent<BoxCollider2D>();
            col.sharedMaterial = physMat;

            go.AddComponent<Brick>();
            go.transform.localScale = new Vector3(0.9f, 0.4f, 1f);

            Directory.CreateDirectory(PrefabsDir);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Brick>();
        }

        public static PowerUpCapsule EnsureCapsulePrefab()
        {
            string path = $"{PrefabsDir}/PowerUpCapsule.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing.GetComponent<PowerUpCapsule>();

            var go = new GameObject("PowerUpCapsule");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "PowerUps";

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            go.transform.localScale = Vector3.one * 0.4f;

            go.AddComponent<PowerUpCapsule>();

            Directory.CreateDirectory(PrefabsDir);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<PowerUpCapsule>();
        }

        public static ParticleSystem EnsureBreakParticlePrefab()
        {
            string path = $"{PrefabsDir}/BrickBreakParticle.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing.GetComponent<ParticleSystem>();

            var go = new GameObject("BrickBreakParticle");
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.startLifetime = 0.35f;
            main.startSpeed = 2.5f;
            main.startSize = 0.12f;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)10) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sortingLayerName = "VFX";
            var mat = new Material(Shader.Find("Sprites/Default"));
            renderer.material = mat;

            Directory.CreateDirectory(PrefabsDir);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<ParticleSystem>();
        }

        static PhysicsMaterial2D EnsureBouncyMaterial()
        {
            string path = "Assets/Prefabs/BallPhysics.physicsMaterial2D";
            var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (existing != null)
                return existing;

            var mat = new PhysicsMaterial2D("BallPhysics")
            {
                bounciness = 1f,
                friction = 0f
            };
            Directory.CreateDirectory(PrefabsDir);
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            return mat;
        }
    }
}
