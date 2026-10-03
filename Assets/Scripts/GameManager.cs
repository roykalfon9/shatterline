using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shatterline
{
    public enum GameState
    {
        MainMenu,
        Serve,
        Playing,
        BallLost,
        LevelClear,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Config & Data")]
        [SerializeField] GameConfig config;
        [SerializeField] LevelData[] levels;

        [Header("Scene References")]
        [SerializeField] BrickGrid brickGrid;
        [SerializeField] UIManager ui;
        [SerializeField] BallController ballPrefab;
        [SerializeField] Transform ballSpawnPoint;
        [SerializeField] Transform paddleTransform;
        [SerializeField] int ballPoolSize = 3;
        [SerializeField] InputActionAsset controls;

        [Header("Timings")]
        [SerializeField] float serveCountdownStepSeconds = .3f;
        [SerializeField] float ballLostFreezeSeconds = 0.5f;
        [SerializeField] float levelClearPauseSeconds = 1.2f;
        [SerializeField] float gameOverInputLockoutSeconds = 0.5f;

        public GameState State { get; private set; } = GameState.MainMenu;
        public int Score { get; private set; }
        public int Lives { get; private set; }
        public int BestScore { get; private set; }
        public int CurrentLevelIndex { get; private set; }
        public int CurrentLevelNumber { get; private set; }
        public bool NewBestThisRun { get; private set; }
        public string CurrentLevelName { get; private set; }

        const string BestScoreKey = "Shatterline.BestScore";

        ObjectPool<BallController> ballPool;
        readonly HashSet<BallController> activeBalls = new HashSet<BallController>();
        float slowBallMultiplier = 1f;
        bool isPaused;
        int launchBlockedThroughFrame;
        Coroutine stateRoutine;
        PaddleController paddle;
        PowerUpSpawner powerUpSpawner;
        InputAction launchAction;
        InputAction pauseAction;

        public bool InputLocked { get; private set; }
        public bool AcceptsPaddleInput => !isPaused && (State == GameState.Serve || State == GameState.Playing);

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            paddle = paddleTransform.GetComponent<PaddleController>();
            powerUpSpawner = FindFirstObjectByType<PowerUpSpawner>();

            BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
            ballPool = new ObjectPool<BallController>(ballPrefab, ballPoolSize, transform);

            InputActionMap gameplay = controls.FindActionMap("Gameplay", true);
            launchAction = gameplay.FindAction("Launch", true);
            pauseAction = gameplay.FindAction("Pause", true);
        }

        void OnEnable()
        {
            pauseAction?.Enable();
            launchAction?.Enable();
            if (pauseAction != null)
                pauseAction.performed += OnPausePerformed;
        }

        void OnDisable()
        {
            if (pauseAction != null)
                pauseAction.performed -= OnPausePerformed;
        }

        void OnPausePerformed(InputAction.CallbackContext ctx)
        {
            if (State == GameState.Playing || State == GameState.Serve)
                TogglePause();
        }

        void Start()
        {
            SetState(GameState.MainMenu);
        }

        public void StartRun()
        {
            ResetRunState();
            Score = 0;
            Lives = config.livesStart;
            CurrentLevelIndex = 0;
            slowBallMultiplier = 1f;
            NewBestThisRun = false;
            ui.UpdateScore(Score);
            ui.UpdateLives(Lives);
            LoadLevel(CurrentLevelIndex);
        }

        void LoadLevel(int index)
        {
            CurrentLevelIndex = index;
            LevelData level = levels[index % levels.Length];
            CurrentLevelNumber = index + 1;
            CurrentLevelName = level.displayName;
            brickGrid.BuildLevel(level);
            ui.UpdateLevel(CurrentLevelNumber);
            SetState(GameState.Serve);
        }

        void SetState(GameState next)
        {
            if (stateRoutine != null) StopCoroutine(stateRoutine);
            stateRoutine = null;
            State = next;
            ui.ShowScreenFor(next);

            switch (next)
            {
                case GameState.Serve:
                    stateRoutine = StartCoroutine(ServeCountdownRoutine());
                    break;
                case GameState.BallLost:
                    stateRoutine = StartCoroutine(BallLostFreezeRoutine());
                    break;
                case GameState.LevelClear:
                    AudioManager.Instance.PlayLevelClear();
                    stateRoutine = StartCoroutine(LevelClearRoutine());
                    break;
                case GameState.GameOver:
                    AudioManager.Instance.PlayGameOver();
                    stateRoutine = StartCoroutine(GameOverLockoutRoutine());
                    break;
            }
        }

        IEnumerator ServeCountdownRoutine()
        {
            for (int count = 3; count >= 1; count--)
            {
                ui.ShowServeCountdown(count);
                yield return new WaitForSeconds(serveCountdownStepSeconds);
            }
            ui.ShowServeCountdown(0);

            SpawnBall();
            SetState(GameState.Playing);
            ui.ShowLaunchHint(true);
        }

        void Update()
        {
            if (State != GameState.Playing || isPaused || Time.frameCount <= launchBlockedThroughFrame
                || !launchAction.WasPressedThisFrame()
                || PointerInput.IsLaunchOverControl(launchAction))
                return;
            foreach (var ball in activeBalls)
                if (!ball.HasLaunched) ball.Launch();
            ui.ShowLaunchHint(false);
        }

        BallController SpawnBall()
        {
            BallController ball = ballPool.Get();
            ball.SetPaddleReference(paddleTransform);
            ball.transform.position = ballSpawnPoint.position;
            ball.ResetBall(config.ballBaseSpeed, slowBallMultiplier);
            activeBalls.Add(ball);
            return ball;
        }

        public void SpawnMultiBalls()
        {
            if (State != GameState.Playing || activeBalls.Count == 0)
                return;

            BallController source = null;
            foreach (var b in activeBalls) { source = b; break; }
            if (!source.HasLaunched) return;
            SpawnExtraBall(source, 20f);
            SpawnExtraBall(source, -20f);
        }

        void SpawnExtraBall(BallController source, float angleOffsetDeg)
        {
            if (!ballPool.TryGet(out BallController ball)) return;
            ball.SetPaddleReference(paddleTransform);
            ball.transform.position = source.transform.position;
            ball.ResetBall(source.CurrentSpeed, slowBallMultiplier);
            ball.LaunchInDirection(Quaternion.Euler(0, 0, angleOffsetDeg) * source.Direction);
            activeBalls.Add(ball);
        }

        public void ReportBallLost(BallController ball)
        {
            if (State != GameState.Playing || !activeBalls.Remove(ball)) return;
            ballPool.Return(ball);

            if (activeBalls.Count > 0)
                return; // other balls (multi-ball) still in play

            Lives--;
            ui.UpdateLives(Lives);
            SetState(GameState.BallLost);
        }

        public void AddScore(int points)
        {
            Score += points;
            ui.UpdateScore(Score);

            if (Score > BestScore)
            {
                BestScore = Score;
                NewBestThisRun = true;
                PlayerPrefs.SetInt(BestScoreKey, BestScore);
            }
        }

        public void ReportLevelClear()
        {
            if (State != GameState.Playing) return;
            ClearBalls();
            ResetPowerUps();
            SetState(GameState.LevelClear);
        }

        Coroutine slowBallRoutine;

        public void ApplySlowBall(float multiplier, float duration)
        {
            if (slowBallRoutine != null)
                StopCoroutine(slowBallRoutine);
            slowBallRoutine = StartCoroutine(SlowBallRoutine(multiplier, duration));
        }

        IEnumerator SlowBallRoutine(float multiplier, float duration)
        {
            SetSlowBallMultiplier(multiplier);
            yield return new WaitForSeconds(duration);
            SetSlowBallMultiplier(1f);
            slowBallRoutine = null;
        }

        void SetSlowBallMultiplier(float multiplier)
        {
            slowBallMultiplier = multiplier;
            paddle.SetSlowVisual(multiplier < 1f);
            foreach (var ball in activeBalls)
                ball.SetSpeedMultiplier(multiplier);
        }

        IEnumerator BallLostFreezeRoutine()
        {
            yield return new WaitForSeconds(ballLostFreezeSeconds);
            if (Lives > 0)
                SetState(GameState.Serve);
            else
                SetState(GameState.GameOver);
        }

        IEnumerator LevelClearRoutine()
        {
            yield return new WaitForSeconds(levelClearPauseSeconds);
            LoadLevel(CurrentLevelIndex + 1);
        }

        IEnumerator GameOverLockoutRoutine()
        {
            InputLocked = true;
            yield return new WaitForSeconds(gameOverInputLockoutSeconds);
            InputLocked = false;
        }

        public void ReturnToMenu()
        {
            ResetRunState();
            brickGrid.ClearLevel();
            PlayerPrefs.Save();
            SetState(GameState.MainMenu);
        }

        void ClearBalls()
        {
            ballPool.ReturnAll();
            activeBalls.Clear();
        }

        void ResetPowerUps()
        {
            if (slowBallRoutine != null) StopCoroutine(slowBallRoutine);
            slowBallRoutine = null;
            SetSlowBallMultiplier(1f);
            paddle.ResetEffects();
            powerUpSpawner.ClearCapsules();
        }

        // A fresh run/menu owns no timers or pooled objects from the previous run.
        void ResetRunState()
        {
            StopAllCoroutines();
            stateRoutine = null;
            slowBallRoutine = null;
            InputLocked = false;
            isPaused = false;
            Time.timeScale = 1f;
            ClearBalls();
            ResetPowerUps();
            paddleTransform.position = new Vector3(0f, paddleTransform.position.y, paddleTransform.position.z);
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            Time.timeScale = 1f;
        }

        public void TogglePause()
        {
            if (isPaused)
                ResumeFromPause();
            else
                Pause();
        }

        void Pause()
        {
            isPaused = true;
            Time.timeScale = 0f;
            ui.ShowPauseOverlay(true);
        }

        public void ResumeFromPause()
        {
            // The release that dismisses the overlay also raises the touch tap action.
            // Consume it even when the UI has already disappeared before Update runs.
            launchBlockedThroughFrame = Time.frameCount + 1;
            isPaused = false;
            Time.timeScale = 1f;
            ui.ShowPauseOverlay(false);
        }

        public void QuitToMenuFromPause()
        {
            isPaused = false;
            Time.timeScale = 1f;
            ui.ShowPauseOverlay(false);
            ReturnToMenu();
        }
    }
}
