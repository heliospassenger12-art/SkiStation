using System;
using UnityEngine;

namespace SkiStation
{
    public enum ManagementCommandAction
    {
        None, Transport, Slopes, Services, Finance, Visitors, Infrastructure
    }

    public sealed class ManagementCommandActionRegistry : MonoBehaviour
    {
        public static ManagementCommandActionRegistry I { get; private set; }
        public event Action<ManagementCommandAction> ActionRequested;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        public void Request(ManagementCommandAction action)
        {
            if (action != ManagementCommandAction.None) ActionRequested?.Invoke(action);
        }
    }
}
