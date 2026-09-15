using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        [Header("Main Menu")]
        [SerializeField] TMP_Text bestScoreMenuText;

        [Header("Level Clear")]
        [SerializeField] TMP_Text levelClearText;

        [Header("Game Over")]
        [SerializeField] TMP_Text finalScoreText;
        [SerializeField] TMP_Text bestScoreGameOverText;
        [SerializeField] GameObject newBestTag;

        public void ShowScreenFor(GameState state)
        {
            mainMenuPanel.SetActive(state == GameState.MainMenu);
            hudPanel.SetActive(state != GameState.MainMenu);
            levelClearPanel.SetActive(state == GameState.LevelClear);
            gameOverPanel.SetActive(state == GameState.GameOver);
            pausePanel.SetActive(false);

            if (state != GameState.Serve)
                serveCountdownText.gameObject.SetActive(false);

            switch (state)
            {
                case GameState.MainMenu:
                    bestScoreMenuText.text = $"Best Score: {GameManager.Instance.BestScore}";
                    break;
                case GameState.LevelClear:
                    levelClearText.text = $"LEVEL {GameManager.Instance.CurrentLevelNumber} CLEAR";
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
            serveCountdownText.text = count.ToString();
        }

        public void ShowPauseOverlay(bool show)
        {
            pausePanel.SetActive(show);
        }

        // UI Button targets, wired in the scene via GameSetup.
        public void OnPlayClicked() => GameManager.Instance.StartRun();

        public void OnRetryClicked()
        {
            if (!GameManager.Instance.InputLocked)
                GameManager.Instance.StartRun();
        }

        public void OnMenuClicked()
        {
            if (!GameManager.Instance.InputLocked)
                GameManager.Instance.ReturnToMenu();
        }

        public void OnResumeClicked() => GameManager.Instance.ResumeFromPause();

        public void OnQuitToMenuClicked() => GameManager.Instance.QuitToMenuFromPause();

        public void OnPauseIconClicked() => GameManager.Instance.TogglePause();

        public void OnMuteToggleClicked() => AudioManager.Instance.ToggleMute();
    }
}
