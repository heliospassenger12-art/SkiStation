using System.Collections.Generic;
using UnityEngine;

namespace SkiStation
{
    /// <summary>Lightweight instructor pool. Instructors are resources, not individually micromanaged staff.</summary>
    public sealed class SkiInstructorRuntime : MonoBehaviour
    {
        public static readonly List<SkiInstructorRuntime> All = new();
        public SkiSchoolRuntime School { get; private set; }
        public bool Busy { get; private set; }
        public string Assignment { get; private set; }

        public void Bind(SkiSchoolRuntime school) { School = school; }
        public bool TryAssign(string assignment)
        {
            if (Busy) return false;
            Busy = true; Assignment = assignment ?? "Cours de ski"; return true;
        }
        public void Release() { Busy = false; Assignment = null; }

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }
    }
}
