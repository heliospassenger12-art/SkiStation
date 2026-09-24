using System.Collections.Generic;
using UnityEngine;

namespace SkiStation.PisteOperations
{
    public enum GroomingSectorPriority { Low, Normal, High, Critical }

    /// <summary>Lightweight sector-management layer: teams can be assigned to mountain sectors.</summary>
    public class GroomingSectorManagementRuntime : MonoBehaviour
    {
        public static GroomingSectorManagementRuntime I { get; private set; }
        public bool assignmentMode = true;
        readonly Dictionary<string, int> assignedTeams = new Dictionary<string, int>();
        readonly Dictionary<string, GroomingSectorPriority> priorities = new Dictionary<string, GroomingSectorPriority>();
        float rebuildTimer;

        void Awake(){ if(I!=null&&I!=this){Destroy(gameObject);return;} I=this; }
        void Update(){ rebuildTimer += Time.deltaTime; if(rebuildTimer>=8f){rebuildTimer=0f; GroomingSectorDispatchRuntime.I?.Rebuild();} }

        public int GetAssignedTeams(GroomingSectorRuntime sector)
        {
            if(sector==null) return 0;
            int value; return assignedTeams.TryGetValue(sector.sectorId,out value)?Mathf.Max(0,value):0;
        }
        public void SetAssignedTeams(GroomingSectorRuntime sector,int count)
        {
            if(sector==null)return;
            assignedTeams[sector.sectorId]=Mathf.Max(0,count);
        }
        public void AddTeam(GroomingSectorRuntime sector,int maxTeams)
        {
            SetAssignedTeams(sector,Mathf.Min(maxTeams,GetAssignedTeams(sector)+1));
        }
        public void RemoveTeam(GroomingSectorRuntime sector)
        {
            SetAssignedTeams(sector,Mathf.Max(0,GetAssignedTeams(sector)-1));
        }
        public GroomingSectorPriority GetPriority(GroomingSectorRuntime sector)
        {
            if(sector==null)return GroomingSectorPriority.Normal;
            GroomingSectorPriority p; return priorities.TryGetValue(sector.sectorId,out p)?p:GroomingSectorPriority.Normal;
        }
        public void CyclePriority(GroomingSectorRuntime sector)
        {
            if(sector==null)return;
            var next=(int)GetPriority(sector)+1; if(next>3)next=0;
            priorities[sector.sectorId]=(GroomingSectorPriority)next;
        }
        public float PriorityBonus(GroomingSectorRuntime sector)
        {
            switch(GetPriority(sector))
            {
                case GroomingSectorPriority.Critical:return 45f;
                case GroomingSectorPriority.High:return 25f;
                case GroomingSectorPriority.Low:return -12f;
                default:return 0f;
            }
        }
        public bool HasAnyAssignment()
        {
            foreach(var kv in assignedTeams) if(kv.Value>0)return true;
            return false;
        }
        public bool IsSectorAuthorized(GroomingSectorRuntime sector)
        {
            if(!assignmentMode || !HasAnyAssignment()) return true;
            return GetAssignedTeams(sector)>0;
        }
    }
}
