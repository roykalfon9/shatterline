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
        [SerializeField] GameObject gameWinPanel;

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

        [Header("Game Win")]
        [SerializeField] TMP_Text winText;

        void Awake()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (quitButton != null) quitButton.gameObject.SetActive(true);
#else
            if (quitButton != null) quitButton.gameObject.SetActive(false);
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
            // Null-safe panel toggling
            if (mainMenuPanel != null) mainMenuPanel.SetActive(state == GameState.MainMenu);
            if (hudPanel != null) hudPanel.SetActive(state != GameState.MainMenu);
            if (levelClearPanel != null) levelClearPanel.SetActive(state == GameState.LevelClear);
            if (gameOverPanel != null) gameOverPanel.SetActive(state == GameState.GameOver);
            if (gameWinPanel != null) gameWinPanel.SetActive(state == GameState.GameWin);
            if (pausePanel != null) pausePanel.SetActive(false);
            
            ShowLaunchHint(false);

            // Focus first button
            GameObject targetPanel = null;
            if (state == GameState.MainMenu) targetPanel = mainMenuPanel;
            else if (state == GameState.GameOver) targetPanel = gameOverPanel;
            else if (state == GameState.GameWin) targetPanel = gameWinPanel;
            SelectFirstButton(targetPanel);

            // Serve countdown visibility
            if (serveCountdownText != null)
            {
                serveCountdownText.gameObject.SetActive(state == GameState.Serve);
            }

            // State-specific text updates
            switch (state)
            {
                case GameState.MainMenu:
                    if (bestScoreMenuText != null) bestScoreMenuText.text = $"Best Score: {GameManager.Instance.BestScore}";
                    break;
                case GameState.LevelClear:
                    if (levelClearText != null) levelClearText.text = $"{GameManager.Instance.CurrentLevelName.ToUpperInvariant()}\nCLEAR";
                    break;
                case GameState.GameOver:
                    if (finalScoreText != null) finalScoreText.text = $"Score: {GameManager.Instance.Score}";
                    if (bestScoreGameOverText != null) bestScoreGameOverText.text = $"Best: {GameManager.Instance.BestScore}";
                    if (newBestTag != null) newBestTag.SetActive(GameManager.Instance.NewBestThisRun);
                    break;
                case GameState.GameWin:
                    if (winText != null) winText.text = "GAME COMPLETE!";
                    break;
            }
        }

        public void UpdateScore(int score)
        {
            if (scoreText != null) scoreText.text = score.ToString();
        }

        public void UpdateLives(int lives)
        {
            if (lifeIcons == null) return;
            for (int i = 0; i < lifeIcons.Length; i++)
                if (lifeIcons[i] != null) lifeIcons[i].gameObject.SetActive(i < lives);
        }

        public void UpdateLevel(int levelNumber)
        {
            if (levelText != null) levelText.text = $"LV {levelNumber}";
        }

        public void ShowServeCountdown(int count)
        {
            if (serveCountdownText == null) return;
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
            if (pausePanel != null)
            {
                pausePanel.SetActive(show);
                SelectFirstButton(show ? pausePanel : null);
            }
        }

        static void SelectFirstButton(GameObject panel)
        {
            if (panel == null || EventSystem.current == null) return;
            var button = panel.GetComponentInChildren<UnityEngine.UI.Button>();
            EventSystem.current.SetSelectedGameObject(button != null ? button.gameObject : null);
        }

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
