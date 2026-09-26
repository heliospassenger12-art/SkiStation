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
        [SerializeField, Min(600f)] private float realSecondsPerGameDay = 14400f;
        [SerializeField, HideInInspector] private int clockBalanceVersion;
        private const int CurrentClockBalanceVersion = 327;
        private const float BalancedRealSecondsPerGameDay = 14400f; // 4 h réelles à x1 pour 06:00 -> 24:00.
        [SerializeField] private float dayStartHour = 6f;
        [SerializeField] private float dayEndHour = 24f;

        public float GameDayProgress { get; private set; }
        public float GameHour { get; private set; } = 6f;
        // Compatibility alias used by management runtimes.
        public float CurrentHour => GameHour;
        public float RealSecondsPerGameHour => realSecondsPerGameDay / Mathf.Max(1f, dayEndHour - dayStartHour);
        public float RealSecondsPerGameMinute => RealSecondsPerGameHour / 60f;
        public float GameMinutesPerRealSecond => 60f / Mathf.Max(.01f, RealSecondsPerGameHour);
        public int CurrentDay { get; private set; } = 1;
        public float ElapsedGameMinutes => ((CurrentDay - 1) * (dayEndHour - dayStartHour) + GameHour - dayStartHour) * 60f;
        public bool IsNight => GameHour < dayStartHour || GameHour >= dayEndHour;
        public GameTimePhase Phase => GetPhase(GameHour);
        public event Action<float> TimeChanged;
        public event Action<int> DayStarted;
        public event Action NightStarted;
        public event Action MorningStarted;
        private bool wasNight;

        private void Awake()
        {
            I = this;
            // 3.27 migration: old scenes serialized a 180 s day, which made a normal lift ride
            // consume several in-game hours. Move legacy clocks to the new observation-friendly scale.
            if (clockBalanceVersion < CurrentClockBalanceVersion)
            {
                realSecondsPerGameDay = BalancedRealSecondsPerGameDay;
                clockBalanceVersion = CurrentClockBalanceVersion;
            }
            GameHour = dayStartHour;
            wasNight = false;
        }

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

        public float RealSecondsToGameMinutes(float realSeconds)
        {
            return Mathf.Max(0f, realSeconds) * GameMinutesPerRealSecond;
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
