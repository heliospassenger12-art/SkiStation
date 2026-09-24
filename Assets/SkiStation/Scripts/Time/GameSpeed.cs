using System;
using UnityEngine;

namespace SkiStation
{
    public enum GameSpeed
    {
        Paused = 0,
        Normal = 1,
        Fast = 2,
        VeryFast = 3
    }

    public class GameSpeedController : MonoBehaviour
    {
        public static GameSpeedController I { get; private set; }

        [SerializeField] private float normalScale = 1f;
        [SerializeField] private float fastScale = 3f;
        [SerializeField] private float veryFastScale = 8f;

        public GameSpeed Current { get; private set; } = GameSpeed.Normal;
        public float CurrentScale { get; private set; } = 1f;

        public event Action<GameSpeed, float> SpeedChanged;

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
            DontDestroyOnLoad(gameObject);
            Apply(GameSpeed.Normal);
        }

        public void SetPaused(bool paused)
        {
            Apply(paused ? GameSpeed.Paused : GameSpeed.Normal);
        }

        public void SetSpeed(GameSpeed speed)
        {
            Apply(speed);
        }

        public void TogglePause()
        {
            Apply(Current == GameSpeed.Paused ? GameSpeed.Normal : GameSpeed.Paused);
        }

        public void Faster()
        {
            if (Current == GameSpeed.Paused) Apply(GameSpeed.Normal);
            else if (Current == GameSpeed.Normal) Apply(GameSpeed.Fast);
            else if (Current == GameSpeed.Fast) Apply(GameSpeed.VeryFast);
        }

        public void Slower()
        {
            if (Current == GameSpeed.VeryFast) Apply(GameSpeed.Fast);
            else if (Current == GameSpeed.Fast) Apply(GameSpeed.Normal);
            else if (Current == GameSpeed.Normal) Apply(GameSpeed.Paused);
        }

        private void Apply(GameSpeed speed)
        {
            Current = speed;

            CurrentScale =
                speed == GameSpeed.Paused ? 0f :
                speed == GameSpeed.Fast ? fastScale :
                speed == GameSpeed.VeryFast ? veryFastScale :
                normalScale;

            Time.timeScale = CurrentScale;
            SpeedChanged?.Invoke(Current, CurrentScale);
        }

        private void OnApplicationQuit()
        {
            Time.timeScale = 1f;
        }
    }
}
