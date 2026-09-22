using UnityEngine;

namespace Shatterline
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioClip bounceClip;
        [SerializeField] AudioClip breakClip;
        [SerializeField] AudioClip powerUpClip;
        [SerializeField] AudioClip gameOverClip;
        [SerializeField] AudioClip clickClip;
        [SerializeField] AudioClip musicClip;

        const string MuteKey = "Shatterline.Muted";
        public bool IsMuted { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            IsMuted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
            ApplyMuteState();
        }

        void Start()
        {
            if (musicClip == null)
                return;

            musicSource.clip = musicClip;
            musicSource.loop = true;
            musicSource.Play();
        }

        public void PlayBounce() => sfxSource.PlayOneShot(bounceClip);
        public void PlayBreak() => sfxSource.PlayOneShot(breakClip);
        public void PlayPowerUp() => sfxSource.PlayOneShot(powerUpClip);
        public void PlayGameOver() => sfxSource.PlayOneShot(gameOverClip);
        public void PlayClick() => sfxSource.PlayOneShot(clickClip);

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
            PlayerPrefs.SetInt(MuteKey, IsMuted ? 1 : 0);
            ApplyMuteState();
        }

        void ApplyMuteState()
        {
            sfxSource.mute = IsMuted;
            musicSource.mute = IsMuted;
        }
    }
}
