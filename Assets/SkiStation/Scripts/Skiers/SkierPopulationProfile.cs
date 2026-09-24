using UnityEngine;

namespace SkiStation
{
    public enum SkierPopulationRole { Adult, Child, EliteAthlete }
    public enum SkierPopulationArchetype { Family, Couple, Solo, Friends, EliteTeam }

    /// <summary>Presentation metadata for a management visitor. It does not alter economy or AI.</summary>
    public sealed class SkierPopulationProfile : MonoBehaviour
    {
        public string DisplayName { get; private set; }
        public SkierPopulationRole Role { get; private set; }
        public SkierPopulationArchetype Archetype { get; private set; }
        public string FamilyId { get; private set; }
        public string GroupId { get; private set; }
        public bool IsEliteAthlete => Role == SkierPopulationRole.EliteAthlete;
        public bool IsStationStar { get; private set; }
        public string StarDiscipline { get; private set; }
        public bool IsChild => Role == SkierPopulationRole.Child;
        public string ShortProfile { get; private set; }
        public int AgeYears { get; private set; } = 30;

        public CustomerProfile ToCustomerProfile()
        {
            if (IsStationStar || IsEliteAthlete) return CustomerProfile.Athlete;
            if (IsChild) return CustomerProfile.Youth;
            if (Archetype == SkierPopulationArchetype.Family) return CustomerProfile.Family;
            return CustomerProfile.Tourist;
        }

        public void Configure(string displayName, SkierPopulationRole role, SkierPopulationArchetype archetype, string familyId = null, bool isStationStar = false, string starDiscipline = null, string groupId = null, int ageYears = 30)
        {
            DisplayName = string.IsNullOrEmpty(displayName) ? "Client" : displayName;
            Role = role;
            Archetype = archetype;
            FamilyId = familyId;
            GroupId = string.IsNullOrEmpty(groupId) ? familyId : groupId;
            IsStationStar = isStationStar && IsEliteAthlete;
            StarDiscipline = IsStationStar ? (string.IsNullOrEmpty(starDiscipline) ? "Ski" : starDiscipline) : null;
            AgeYears = role == SkierPopulationRole.Child && ageYears == 30 ? 7 : Mathf.Clamp(ageYears, 3, 100);
            ShortProfile = BuildProfile();
        }

        private string BuildProfile()
        {
            if (IsStationStar) return "Star sportive — " + StarDiscipline;
            if (IsEliteAthlete) return "Sportif de haut niveau";
            if (IsChild) return "Enfant — en famille";
            switch (Archetype)
            {
                case SkierPopulationArchetype.Family: return "Famille";
                case SkierPopulationArchetype.Couple: return "Couple";
                case SkierPopulationArchetype.Friends: return "Groupe d'amis";
                case SkierPopulationArchetype.EliteTeam: return "Équipe sportive";
                default: return "Vacancier";
            }
        }
    }
}
