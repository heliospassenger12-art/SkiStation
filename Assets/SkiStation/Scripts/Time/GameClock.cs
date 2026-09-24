using System;
using UnityEngine;

namespace SkiStation
{
    public enum GameTimePhase { Night = 0, EarlyMorning = 1, Day = 2, Evening = 3 }

    public class GameClock : MonoBehaviour
    {
        public static GameClock I { get; private set; }
        public static GameClock Instance => I;

        public static GameClock Find() => I != null ? I : FindObjectOfType<GameClock>();
        [SerializeField] private float realSecondsPerGameDay = 180f;
        [SerializeField] private float dayStartHour = 6f;
        [SerializeField] private float dayEndHour = 24f;

        public float GameDayProgress { get; private set; }
        public float GameHour { get; private set; } = 6f;
        // Compatibility alias used by management runtimes.
        public float CurrentHour => GameHour;
        public float RealSecondsPerGameHour => realSecondsPerGameDay / 18f;
        public int CurrentDay { get; private set; } = 1;
        public bool IsNight => GameHour < dayStartHour || GameHour >= dayEndHour;
        public GameTimePhase Phase => GetPhase(GameHour);
        public event Action<float> TimeChanged;
        public event Action<int> DayStarted;
        public event Action NightStarted;
        public event Action MorningStarted;
        private bool wasNight;

        private void Awake() { I = this; GameHour = dayStartHour; wasNight = false; }

        private void OnDestroy() { if (I == this) I = null; }

        private void Update()
        {
            if (GameSpeedController.I != null && GameSpeedController.I.Current == GameSpeed.Paused) return;
            GameDayProgress += Time.deltaTime / Mathf.Max(1f, realSecondsPerGameDay);
            GameHour = dayStartHour + GameDayProgress * (24f - dayStartHour);
            if (GameHour >= dayEndHour)
            {
                CurrentDay++;
                GameDayProgress = 0f;
                GameHour = dayStartHour;
                GameEvent.RaiseDayChanged();
                DayStarted?.Invoke(CurrentDay);
                wasNight = false;
                MorningStarted?.Invoke();
            }
            bool night = IsNight;
            if (night && !wasNight) NightStarted?.Invoke();
            wasNight = night;
            TimeChanged?.Invoke(GameHour);
        }

        public float MinutesUntil(float targetHour)
        {
            targetHour = Mathf.Repeat(targetHour, 24f);
            return (targetHour >= GameHour ? targetHour - GameHour : 24f - GameHour + targetHour) * 60f;
        }

        public static GameTimePhase GetPhase(float hour)
        {
            if (hour < 6f) return GameTimePhase.Night;
            if (hour < 9f) return GameTimePhase.EarlyMorning;
            if (hour < 18f) return GameTimePhase.Day;
            if (hour < 24f) return GameTimePhase.Evening;
            return GameTimePhase.Night;
        }

        public string GetTimeLabel()
        {
            int h = Mathf.Clamp(Mathf.FloorToInt(GameHour), 0, 23);
            int m = Mathf.Clamp(Mathf.FloorToInt((GameHour - h) * 60f), 0, 59);
            return string.Format("{0:00}:{1:00}", h, m);
        }
    }
}
