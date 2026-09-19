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
        [SerializeField] float serveCountdownStepSeconds = 1f;
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

        const string BestScoreKey = "Shatterline.BestScore";

        ObjectPool<BallController> ballPool;
        readonly HashSet<BallController> activeBalls = new HashSet<BallController>();
        float slowBallMultiplier = 1f;
        bool isPaused;
        InputAction launchAction;
        InputAction pauseAction;

        public bool InputLocked { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

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
            CurrentLevelNumber = level.levelNumber;
            brickGrid.BuildLevel(level);
            ui.UpdateLevel(level.levelNumber);
            SetState(GameState.Serve);
        }

        void SetState(GameState next)
        {
            State = next;
            ui.ShowScreenFor(next);

            switch (next)
            {
                case GameState.Serve:
                    StartCoroutine(ServeCountdownRoutine());
                    break;
                case GameState.BallLost:
                    StartCoroutine(BallLostFreezeRoutine());
                    break;
                case GameState.LevelClear:
                    StartCoroutine(LevelClearRoutine());
                    break;
                case GameState.GameOver:
                    AudioManager.Instance.PlayGameOver();
                    StartCoroutine(GameOverLockoutRoutine());
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

            BallController ball = SpawnBall();
            SetState(GameState.Playing);

            yield return null;
            while (State == GameState.Playing && !ball.HasLaunched)
            {
                if (launchAction.WasPressedThisFrame())
                {
                    ball.Launch();
                }
                yield return null;
            }
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
            if (activeBalls.Count == 0)
                return;

            BallController source = null;
            foreach (var b in activeBalls) { source = b; break; }
            SpawnExtraBall(source, 20f);
            SpawnExtraBall(source, -20f);
        }

        void SpawnExtraBall(BallController source, float angleOffsetDeg)
        {
            BallController ball = ballPool.Get();
            ball.SetPaddleReference(paddleTransform);
            ball.transform.position = source.transform.position;
            ball.ResetBall(source.CurrentSpeed, slowBallMultiplier);
            ball.LaunchInDirection(Quaternion.Euler(0, 0, angleOffsetDeg) * source.Direction);
            activeBalls.Add(ball);
        }

        public void ReportBallLost(BallController ball)
        {
            activeBalls.Remove(ball);
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
            foreach (var ball in new List<BallController>(activeBalls))
            {
                activeBalls.Remove(ball);
                ballPool.Return(ball);
            }
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
            foreach (var ball in new List<BallController>(activeBalls))
            {
                activeBalls.Remove(ball);
                ballPool.Return(ball);
            }
            SetState(GameState.MainMenu);
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
