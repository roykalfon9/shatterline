using UnityEngine;

namespace Shatterline
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Shatterline/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Paddle")]
        [Tooltip("How fast the paddle tracks keyboard/touch input, in units/sec.")]
        public float paddleSpeed = 12f;

        [Header("Ball")]
        [Tooltip("Ball speed at the start of every serve, in units/sec.")]
        public float ballBaseSpeed = 7f;
        [Tooltip("Speed added to the ball on each paddle hit, in units/sec.")]
        public float speedStepPerHit = 0.15f;
        [Tooltip("Hard cap on ball speed regardless of hits, in units/sec.")]
        public float maxBallSpeed = 12f;
        [Tooltip("Steepest angle a paddle-edge hit can send the ball, in degrees.")]
        public float paddleMaxDeflectionDeg = 60f;

        [Header("Power-ups")]
        [Tooltip("Odds a destroyed brick spawns a falling capsule.")]
        [Range(0f, 1f)]
        public float powerUpDropChance = 0.12f;

        [Header("Lives")]
        [Tooltip("Lives the player starts each run with.")]
        public int livesStart = 3;
    }
}
