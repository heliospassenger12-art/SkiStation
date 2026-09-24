using System;
using UnityEngine;

namespace SkiStation
{
    public enum FundingMode { Standard, Unlimited }

    /// <summary>Controls whether management construction is constrained by cash.</summary>
    public sealed class EconomyModeRuntime : MonoBehaviour
    {
        public static EconomyModeRuntime I { get; private set; }
        [SerializeField] private FundingMode fundingMode = FundingMode.Standard;
        public FundingMode Mode => fundingMode;
        public bool UnlimitedFunds => fundingMode == FundingMode.Unlimited;
        public event Action<FundingMode> ModeChanged;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
        }

        public void SetFundingMode(FundingMode mode)
        {
            if (fundingMode == mode) return;
            fundingMode = mode;
            ModeChanged?.Invoke(mode);
        }
    }
}
