using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Shatterline;

namespace ShatterlineEditor
{
    public static class GameSetup
    {
        const float HalfHeight = 8f;
        const float TargetAspect = 9f / 16f;
        static readonly float HalfWidth = HalfHeight * TargetAspect;
        const float PaddleY = -6.8f;

        static readonly Color GoldAccent = new Color(1f, 0.788f, 0.235f);
        static readonly Color CreamText = new Color(0.94f, 0.93f, 0.90f);
        static readonly Color ButtonTextDark = new Color(0.17f, 0.17f, 0.23f);
        static readonly Color WarnText = new Color(0.95f, 0.4f, 0.32f);
        static readonly Color DimOverlay = new Color(0.04f, 0.04f, 0.07f, 0.82f);
        static readonly Color TextShadowColor = new Color(0f, 0f, 0f, 0.55f);

        [MenuItem("Tools/Shatterline/Build Game Scene")]
        public static void BuildGameScene()
        {
            // Create the fresh scene FIRST: switching scenes can invalidate
            // AssetDatabase-loaded object references fetched beforehand.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Scene scene = EditorSceneManager.GetActiveScene();

            ProjectSetup.ConfigurePlayerSettings();
            ProjectSetup.EnsureTagsAndLayers();
            ProjectSetup.ConfigureQualityPresets();
            ProjectSetup.ConfigureSpriteImportSettings();

            var powerUpSprites = ProjectSetup.EnsurePowerUpPlaceholderSprites();
            TMP_FontAsset font = ProjectSetup.EnsureTmpFontAsset();
            InputActionAsset controls = ProjectSetup.EnsureInputActions();
            GameConfig config = ProjectSetup.EnsureGameConfig();
            LevelData[] levels = ProjectSetup.EnsureLevelData();
            BallController ballPrefab = ProjectSetup.EnsureBallPrefab();
            Brick brickPrefab = ProjectSetup.EnsureBrickPrefab();
            PowerUpCapsule capsulePrefab = ProjectSetup.EnsureCapsulePrefab();
            ParticleSystem breakParticlePrefab = ProjectSetup.EnsureBreakParticlePrefab();

            Sprite[] brickSprites =
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/brick_red.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/brick_yellow.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/brick_green.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/brick_blue.png"),
            };

            AudioClip bounceClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_bounce.ogg");
            AudioClip breakClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_break.ogg");
            AudioClip powerUpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_powerup.ogg");
            AudioClip gameOverClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_gameover.ogg");
            AudioClip clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_click.ogg");

            Camera camera = BuildCamera();
            BuildEventSystem(controls);

            GameObject paddleGo = BuildPaddle();
            Transform ballSpawnPoint = new GameObject("BallSpawnPoint").transform;
            ballSpawnPoint.position = new Vector3(0f, PaddleY + 0.4f, 0f);

            BuildWalls();

            GameObject brickGridGo = new GameObject("BrickGrid");
            BrickGrid brickGrid = brickGridGo.AddComponent<BrickGrid>();

            GameObject powerUpSpawnerGo = new GameObject("PowerUpSpawner");
            PowerUpSpawner powerUpSpawner = powerUpSpawnerGo.AddComponent<PowerUpSpawner>();

            GameObject audioManagerGo = new GameObject("AudioManager");
            AudioManager audioManager = audioManagerGo.AddComponent<AudioManager>();
            AudioSource sfxSource = audioManagerGo.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            AudioSource musicSource = audioManagerGo.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;

            (UIManager ui, GameObject canvasGo) = BuildCanvas(font);

            GameObject gmGo = new GameObject("GameManager");
            GameManager gameManager = gmGo.AddComponent<GameManager>();

            // --- wire serialized references ---
            PaddleController paddle = paddleGo.GetComponent<PaddleController>();
            Set(paddle, "config", config);
            Set(paddle, "controls", controls);
            Set(paddle, "paddleCollider", paddleGo.GetComponents<BoxCollider2D>()[0]);
            Set(paddle, "playCamera", camera);

            Set(ballPrefab, "config", config);

            Set(brickGrid, "brickPrefab", brickPrefab);
            SetArray(brickGrid, "brickSprites", brickSprites);
            Set(brickGrid, "powerUpSpawner", powerUpSpawner);
            Set(brickGrid, "breakParticlePrefab", breakParticlePrefab);
            Set(brickGrid, "playCamera", camera);

            Set(powerUpSpawner, "config", config);
            Set(powerUpSpawner, "paddle", paddle);
            Set(powerUpSpawner, "playCamera", camera);
            Set(powerUpSpawner, "capsulePrefab", capsulePrefab);
            Set(powerUpSpawner, "widenPaddleSprite", powerUpSprites[PowerUpType.WidenPaddle]);
            Set(powerUpSpawner, "slowBallSprite", powerUpSprites[PowerUpType.SlowBall]);
            Set(powerUpSpawner, "multiBallSprite", powerUpSprites[PowerUpType.MultiBall]);

            Set(audioManager, "sfxSource", sfxSource);
            Set(audioManager, "musicSource", musicSource);
            Set(audioManager, "bounceClip", bounceClip);
            Set(audioManager, "breakClip", breakClip);
            Set(audioManager, "powerUpClip", powerUpClip);
            Set(audioManager, "gameOverClip", gameOverClip);
            Set(audioManager, "clickClip", clickClip);

            Set(gameManager, "config", config);
            SetArray(gameManager, "levels", levels);
            Set(gameManager, "brickGrid", brickGrid);
            Set(gameManager, "ui", ui);
            Set(gameManager, "ballPrefab", ballPrefab);
            Set(gameManager, "ballSpawnPoint", ballSpawnPoint);
            Set(gameManager, "paddleTransform", paddleGo.transform);
            Set(gameManager, "controls", controls);

            AssetDatabase.SaveAssets();

            EditorSceneManager.MarkSceneDirty(scene);
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true) };

            Debug.Log("SHATTERLINE: Game.unity built successfully.");
        }

        static Camera BuildCamera()
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 0f, -10f);
            Camera cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = HalfHeight;
            cam.backgroundColor = new Color(0.06f, 0.06f, 0.09f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            go.AddComponent<CameraAspectFitter>();
            go.AddComponent<AudioListener>();
            return cam;
        }

        static void BuildEventSystem(InputActionAsset controls)
        {
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();

            module.point = FindActionReference(controls, "Point");
            module.leftClick = FindActionReference(controls, "Click");
            module.submit = FindActionReference(controls, "Submit");
            module.cancel = FindActionReference(controls, "Cancel");
            module.move = FindActionReference(controls, "Navigate");
        }

        static InputActionReference FindActionReference(InputActionAsset asset, string actionName)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (obj is InputActionReference iar && iar.action != null && iar.action.name == actionName)
                    return iar;
            }
            Debug.LogError($"SHATTERLINE setup: no InputActionReference sub-asset found for action '{actionName}'");
            return null;
        }

        static GameObject BuildPaddle()
        {
            GameObject go = new GameObject("Paddle");
            go.tag = "Paddle";
            go.transform.position = new Vector3(0f, PaddleY, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/paddle.png");
            sr.sortingLayerName = "Paddle";

            // transform.localScale multiplies the sprite's own pixels-per-unit
            // size, it isn't an absolute world size - scale/size colliders
            // relative to the sprite's native bounds for the desired dimensions.
            Vector2 nativeSize = sr.sprite.bounds.size;
            Vector2 desiredSize = new Vector2(1.6f, 0.4f);
            go.transform.localScale = new Vector3(desiredSize.x / nativeSize.x, desiredSize.y / nativeSize.y, 1f);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var physicalCollider = go.AddComponent<BoxCollider2D>();
            physicalCollider.size = nativeSize;

            var triggerCollider = go.AddComponent<BoxCollider2D>();
            triggerCollider.size = nativeSize * 1.1f;
            triggerCollider.isTrigger = true;

            go.AddComponent<PaddleController>();
            return go;
        }

        static void BuildWalls()
        {
            CreateWall("LeftWall", new Vector3(-HalfWidth - 0.25f, 0f, 0f), new Vector2(0.5f, HalfHeight * 2f + 2f));
            CreateWall("RightWall", new Vector3(HalfWidth + 0.25f, 0f, 0f), new Vector2(0.5f, HalfHeight * 2f + 2f));
            CreateWall("TopWall", new Vector3(0f, HalfHeight + 0.25f, 0f), new Vector2(HalfWidth * 2f + 1f, 0.5f));
        }

        static void CreateWall(string name, Vector3 position, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        static (UIManager, GameObject) BuildCanvas(TMP_FontAsset font)
        {
            GameObject canvasGo = new GameObject("Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject safeAreaGo = new GameObject("SafeArea", typeof(RectTransform));
            safeAreaGo.transform.SetParent(canvasGo.transform, false);
            RectTransform safeRect = safeAreaGo.GetComponent<RectTransform>();
            Stretch(safeRect);
            safeAreaGo.AddComponent<SafeAreaFitter>();

            UIManager ui = canvasGo.AddComponent<UIManager>();

            GameObject mainMenu = BuildMainMenuPanel(safeRect, font, ui);
            GameObject hud = BuildHudPanel(safeRect, font, out TMP_Text scoreText, out TMP_Text levelText,
                out TMP_Text countdownText, out Image[] lifeIcons, ui);
            GameObject pause = BuildPausePanel(safeRect, font, ui);
            GameObject levelClear = BuildLevelClearPanel(safeRect, font, out TMP_Text levelClearText);
            GameObject gameOver = BuildGameOverPanel(safeRect, font, ui, out TMP_Text finalScoreText,
                out TMP_Text bestScoreGameOverText, out GameObject newBestTag);

            TMP_Text bestScoreMenuText = mainMenu.transform.Find("BestScoreText").GetComponent<TMP_Text>();

            Set(ui, "mainMenuPanel", mainMenu);
            Set(ui, "hudPanel", hud);
            Set(ui, "pausePanel", pause);
            Set(ui, "levelClearPanel", levelClear);
            Set(ui, "gameOverPanel", gameOver);
            Set(ui, "scoreText", scoreText);
            Set(ui, "levelText", levelText);
            Set(ui, "serveCountdownText", countdownText);
            SetArray(ui, "lifeIcons", lifeIcons);
            Set(ui, "bestScoreMenuText", bestScoreMenuText);
            Set(ui, "levelClearText", levelClearText);
            Set(ui, "finalScoreText", finalScoreText);
            Set(ui, "bestScoreGameOverText", bestScoreGameOverText);
            Set(ui, "newBestTag", newBestTag);

            pause.SetActive(false);
            levelClear.SetActive(false);
            gameOver.SetActive(false);

            return (ui, canvasGo);
        }

        static GameObject BuildMainMenuPanel(RectTransform parent, TMP_FontAsset font, UIManager ui)
        {
            GameObject panel = CreatePanel("MainMenuPanel", parent);
            TMP_Text title = CreateText(panel.transform, "Title", "SHATTERLINE", 64, font,
                Anchors(0f, 0.65f, 1f, 0.85f), GoldAccent);
            title.fontStyle = FontStyles.Bold;
            title.GetComponent<Shadow>().effectDistance = new Vector2(2.5f, -2.5f);

            Button playButton = CreateButton(panel.transform, "PlayButton", "PLAY", font,
                Anchors(0.25f, 0.45f, 0.75f, 0.55f));
            UnityEventTools.AddPersistentListener(playButton.onClick, ui.OnPlayClicked);

            CreateText(panel.transform, "BestScoreText", "Best Score: 0", 28, font,
                Anchors(0f, 0.35f, 1f, 0.42f));

            Button muteButton = CreateButton(panel.transform, "MuteButton", "MUTE", font,
                Anchors(0.75f, 0.9f, 0.98f, 0.98f));
            UnityEventTools.AddPersistentListener(muteButton.onClick, ui.OnMuteToggleClicked);

            return panel;
        }

        static GameObject BuildHudPanel(RectTransform parent, TMP_FontAsset font,
            out TMP_Text scoreText, out TMP_Text levelText, out TMP_Text countdownText,
            out Image[] lifeIcons, UIManager ui)
        {
            GameObject panel = CreatePanel("HUDPanel", parent);

            scoreText = CreateText(panel.transform, "ScoreText", "0", 36, font, Anchors(0.02f, 0.93f, 0.4f, 0.99f));
            scoreText.alignment = TextAlignmentOptions.TopLeft;

            levelText = CreateText(panel.transform, "LevelText", "LV 1", 24, font, Anchors(0.4f, 0.93f, 0.6f, 0.99f));
            levelText.alignment = TextAlignmentOptions.Top;

            countdownText = CreateText(panel.transform, "ServeCountdownText", "3", 96, font,
                Anchors(0.3f, 0.45f, 0.7f, 0.6f), GoldAccent);
            countdownText.fontStyle = FontStyles.Bold;
            countdownText.GetComponent<Shadow>().effectDistance = new Vector2(2.5f, -2.5f);
            countdownText.gameObject.SetActive(false);

            GameObject livesGroup = new GameObject("LivesGroup", typeof(RectTransform));
            livesGroup.transform.SetParent(panel.transform, false);
            SetAnchors(livesGroup.GetComponent<RectTransform>(), Anchors(0.62f, 0.93f, 0.98f, 0.99f));

            Sprite paddleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/paddle.png");
            lifeIcons = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject icon = new GameObject($"Life{i}", typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(livesGroup.transform, false);
                RectTransform rt = icon.GetComponent<RectTransform>();
                float w = 1f / 3f;
                SetAnchors(rt, Anchors(i * w, 0f, i * w + w * 0.85f, 1f));
                lifeIcons[i] = icon.GetComponent<Image>();
                lifeIcons[i].sprite = paddleSprite;
                lifeIcons[i].preserveAspect = true;
                lifeIcons[i].color = Color.white;
            }

            Button pauseIcon = CreateButton(panel.transform, "PauseIcon", "II", font, Anchors(0.02f, 0.02f, 0.12f, 0.08f));
            UnityEventTools.AddPersistentListener(pauseIcon.onClick, ui.OnPauseIconClicked);

            return panel;
        }

        static GameObject BuildPausePanel(RectTransform parent, TMP_FontAsset font, UIManager ui)
        {
            GameObject panel = CreatePanel("PausePanel", parent);
            Image dim = panel.GetComponent<Image>();
            dim.color = DimOverlay;

            TMP_Text pausedText = CreateText(panel.transform, "PausedText", "PAUSED", 56, font, Anchors(0f, 0.6f, 1f, 0.75f), GoldAccent);
            pausedText.fontStyle = FontStyles.Bold;

            Button resume = CreateButton(panel.transform, "ResumeButton", "RESUME", font, Anchors(0.25f, 0.45f, 0.75f, 0.55f));
            UnityEventTools.AddPersistentListener(resume.onClick, ui.OnResumeClicked);

            Button quit = CreateButton(panel.transform, "QuitButton", "QUIT TO MENU", font, Anchors(0.2f, 0.32f, 0.8f, 0.42f));
            UnityEventTools.AddPersistentListener(quit.onClick, ui.OnQuitToMenuClicked);

            return panel;
        }

        static GameObject BuildLevelClearPanel(RectTransform parent, TMP_FontAsset font, out TMP_Text levelClearText)
        {
            GameObject panel = CreatePanel("LevelClearPanel", parent);
            Image dim = panel.GetComponent<Image>();
            dim.color = DimOverlay;

            levelClearText = CreateText(panel.transform, "LevelClearText", "LEVEL 1 CLEAR", 48, font,
                Anchors(0f, 0.5f, 1f, 0.6f), GoldAccent);
            levelClearText.fontStyle = FontStyles.Bold;
            return panel;
        }

        static GameObject BuildGameOverPanel(RectTransform parent, TMP_FontAsset font, UIManager ui,
            out TMP_Text finalScoreText, out TMP_Text bestScoreGameOverText, out GameObject newBestTag)
        {
            GameObject panel = CreatePanel("GameOverPanel", parent);
            Image dim = panel.GetComponent<Image>();
            dim.color = DimOverlay;

            TMP_Text gameOverText = CreateText(panel.transform, "GameOverText", "GAME OVER", 56, font, Anchors(0f, 0.7f, 1f, 0.82f), WarnText);
            gameOverText.fontStyle = FontStyles.Bold;
            finalScoreText = CreateText(panel.transform, "FinalScoreText", "Score: 0", 32, font, Anchors(0f, 0.6f, 1f, 0.68f));
            bestScoreGameOverText = CreateText(panel.transform, "BestScoreText", "Best: 0", 28, font, Anchors(0f, 0.53f, 1f, 0.6f));

            TMP_Text newBestText = CreateText(panel.transform, "NewBestTag", "NEW BEST!", 26, font, Anchors(0f, 0.46f, 1f, 0.52f), GoldAccent);
            newBestText.fontStyle = FontStyles.Bold;
            newBestTag = newBestText.gameObject;

            Button retry = CreateButton(panel.transform, "RetryButton", "RETRY", font, Anchors(0.25f, 0.3f, 0.75f, 0.4f));
            UnityEventTools.AddPersistentListener(retry.onClick, ui.OnRetryClicked);

            Button menu = CreateButton(panel.transform, "MenuButton", "MENU", font, Anchors(0.25f, 0.18f, 0.75f, 0.28f));
            UnityEventTools.AddPersistentListener(menu.onClick, ui.OnMenuClicked);

            return panel;
        }

        // --- small UI construction helpers ---

        static GameObject CreatePanel(string name, RectTransform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            Image img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            return go;
        }

        static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, TMP_FontAsset font, Rect anchors, Color? color = null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            SetAnchors(go.GetComponent<RectTransform>(), anchors);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color ?? CreamText;
            if (font != null)
                tmp.font = font;

            Shadow shadow = go.AddComponent<Shadow>();
            shadow.effectColor = TextShadowColor;
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return tmp;
        }

        static Sprite buttonDefaultSprite;
        static Sprite buttonSelectedSprite;

        static Button CreateButton(Transform parent, string name, string label, TMP_FontAsset font, Rect anchors)
        {
            if (buttonDefaultSprite == null)
                buttonDefaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/button_default.png");
            if (buttonSelectedSprite == null)
                buttonSelectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/button_selected.png");

            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            SetAnchors(go.GetComponent<RectTransform>(), anchors);

            Image image = go.GetComponent<Image>();
            image.sprite = buttonDefaultSprite;
            image.type = Image.Type.Sliced;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            SpriteState state = button.spriteState;
            state.highlightedSprite = buttonSelectedSprite;
            state.pressedSprite = buttonSelectedSprite;
            state.selectedSprite = buttonSelectedSprite;
            button.spriteState = state;

            TMP_Text labelText = CreateText(go.transform, "Label", label, 28, font, new Rect(0f, 0f, 1f, 1f), ButtonTextDark);
            labelText.fontStyle = FontStyles.Bold;
            Object.DestroyImmediate(labelText.GetComponent<Shadow>());
            return button;
        }

        static Rect Anchors(float xMin, float yMin, float xMax, float yMax) => new Rect(xMin, yMin, xMax - xMin, yMax - yMin);

        static void SetAnchors(RectTransform rt, Rect anchors)
        {
            rt.anchorMin = new Vector2(anchors.xMin, anchors.yMin);
            rt.anchorMax = new Vector2(anchors.xMin + anchors.width, anchors.yMin + anchors.height);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // --- serialized-field wiring helpers ---

        static void Set(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"SHATTERLINE setup: field '{fieldName}' not found on {target.GetType().Name}");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string fieldName, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"SHATTERLINE setup: array field '{fieldName}' not found on {target.GetType().Name}");
                return;
            }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
