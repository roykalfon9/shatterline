using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Shatterline
{
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] GameObject mainMenuPanel;
        [SerializeField] GameObject hudPanel;
        [SerializeField] GameObject pausePanel;
        [SerializeField] GameObject levelClearPanel;
        [SerializeField] GameObject gameOverPanel;

        [Header("HUD")]
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text serveCountdownText;
        [SerializeField] Image[] lifeIcons;
        [SerializeField] GameObject launchHint;
        [SerializeField] TMP_Text muteLabel;

        [Header("Main Menu")]
        [SerializeField] TMP_Text bestScoreMenuText;
        [SerializeField] UnityEngine.UI.Button quitButton;

        [Header("Level Clear")]
        [SerializeField] TMP_Text levelClearText;

        [Header("Game Over")]
        [SerializeField] TMP_Text finalScoreText;
        [SerializeField] TMP_Text bestScoreGameOverText;
        [SerializeField] GameObject newBestTag;

        void Awake()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            quitButton.gameObject.SetActive(true);
#else
            quitButton.gameObject.SetActive(false);
#endif
        }

        public void ShowLaunchHint(bool show)
        {
            if (launchHint != null) launchHint.SetActive(show);
        }

        void Start() => UpdateMuteLabel();

        void UpdateMuteLabel()
        {
            if (muteLabel != null) muteLabel.text = AudioManager.Instance.IsMuted ? "SOUND OFF" : "SOUND ON";
        }

        public void ShowScreenFor(GameState state)
        {
            ShowLaunchHint(false);
            mainMenuPanel.SetActive(state == GameState.MainMenu);
            hudPanel.SetActive(state != GameState.MainMenu);
            levelClearPanel.SetActive(state == GameState.LevelClear);
            gameOverPanel.SetActive(state == GameState.GameOver);
            pausePanel.SetActive(false);
            SelectFirstButton(state == GameState.MainMenu ? mainMenuPanel : state == GameState.GameOver ? gameOverPanel : null);

            if (state != GameState.Serve)
                serveCountdownText.gameObject.SetActive(false);

            switch (state)
            {
                case GameState.MainMenu:
                    bestScoreMenuText.text = $"Best Score: {GameManager.Instance.BestScore}";
                    break;
                case GameState.LevelClear:
                    levelClearText.text = $"{GameManager.Instance.CurrentLevelName.ToUpperInvariant()}\nCLEAR";
                    break;
                case GameState.GameOver:
                    finalScoreText.text = $"Score: {GameManager.Instance.Score}";
                    bestScoreGameOverText.text = $"Best: {GameManager.Instance.BestScore}";
                    newBestTag.SetActive(GameManager.Instance.NewBestThisRun);
                    break;
            }
        }

        public void UpdateScore(int score)
        {
            scoreText.text = score.ToString();
        }

        public void UpdateLives(int lives)
        {
            for (int i = 0; i < lifeIcons.Length; i++)
                lifeIcons[i].gameObject.SetActive(i < lives);
        }

        public void UpdateLevel(int levelNumber)
        {
            levelText.text = $"LV {levelNumber}";
        }

        public void ShowServeCountdown(int count)
        {
            if (count <= 0)
            {
                serveCountdownText.gameObject.SetActive(false);
                return;
            }
            serveCountdownText.gameObject.SetActive(true);
            serveCountdownText.text = $"<size=23>{GameManager.Instance.CurrentLevelName.ToUpperInvariant()}</size>\n{count}";
        }

        public void ShowPauseOverlay(bool show)
        {
            pausePanel.SetActive(show);
            SelectFirstButton(show ? pausePanel : null);
        }

        static void SelectFirstButton(GameObject panel)
        {
            if (EventSystem.current == null) return;
            var button = panel != null ? panel.GetComponentInChildren<UnityEngine.UI.Button>() : null;
            EventSystem.current.SetSelectedGameObject(button != null ? button.gameObject : null);
        }

        // UI Button targets, wired in the scene via GameSetup.
        public void OnPlayClicked()
        {
            AudioManager.Instance.PlayClick();
            GameManager.Instance.StartRun();
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
#endif
        }

        public void OnRetryClicked()
        {
            AudioManager.Instance.PlayClick();
            if (!GameManager.Instance.InputLocked)
                GameManager.Instance.StartRun();
        }

        public void OnMenuClicked()
        {
            AudioManager.Instance.PlayClick();
            if (!GameManager.Instance.InputLocked)
                GameManager.Instance.ReturnToMenu();
        }

        public void OnResumeClicked()
        {
            AudioManager.Instance.PlayClick();
            GameManager.Instance.ResumeFromPause();
        }

        public void OnQuitToMenuClicked()
        {
            AudioManager.Instance.PlayClick();
            GameManager.Instance.QuitToMenuFromPause();
        }

        public void OnPauseIconClicked()
        {
            AudioManager.Instance.PlayClick();
            GameManager.Instance.TogglePause();
        }

        public void OnMuteToggleClicked()
        {
            AudioManager.Instance.PlayClick();
            AudioManager.Instance.ToggleMute();
            UpdateMuteLabel();
        }
    }
}
