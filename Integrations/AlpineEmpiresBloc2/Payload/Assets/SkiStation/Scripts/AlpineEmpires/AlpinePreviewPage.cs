using System;
using System.Collections.Generic;
using SkiStation.Arcade;
using UnityEngine;

namespace SkiStation.AlpineEmpires
{
    internal sealed class AlpinePreviewPage
    {
        private AlpineSession session;
        private string feedback;
        private StationRuntime managementStation;

        public bool IsOpen { get; private set; }
        public bool HasStation => managementStation != null;
        internal AlpineSession ActiveWorld => HasStation && managementStation == StationRuntime.I ? session : null;

        public void Open() => IsOpen = true;
        public void Close() => IsOpen = false;

        public void AttachManagement(StationRuntime station, FinanceRuntime finance)
        {
            if (HasStation || session == null || station == null || finance == null)
                throw new InvalidOperationException("Passage à la station 3D impossible.");

            var company = session.Companies[0];
            if (company.Expenses > 0f)
            {
                // Import purchase history once, without replaying payments against the station.
                var checkpoint = finance.CaptureCheckpoint261();
                var transactions = new List<FinanceTransaction>(checkpoint.transactions);
                transactions.AddRange(company.Ledger.Transactions);
                checkpoint.transactions = transactions.ToArray();
                checkpoint.totalExpenses += company.Expenses;

                int index = checkpoint.expenses.FindIndex(value => value.category == FinanceCategory.Construction);
                var category = new FinanceCategoryValue261 { category = FinanceCategory.Construction, value = company.Expenses };
                if (index >= 0)
                {
                    category.value += checkpoint.expenses[index].value;
                    checkpoint.expenses[index] = category;
                }
                else checkpoint.expenses.Add(category);

                finance.RestoreCheckpoint261(checkpoint);
            }
            station.State.money = company.Balance;
            managementStation = station;
        }

        internal AlpineSessionSave CaptureWorld()
        {
            if (!HasStation || managementStation != StationRuntime.I)
                throw new InvalidOperationException("Station Alpine Empires indisponible.");
            return session.CaptureWorld();
        }

        internal void BindRestoredWorld(AlpineSession restored, StationRuntime station)
        {
            // The native checkpoint already restored the balance and ledger.
            session = restored;
            managementStation = station;
            EconomyBalanceRuntime.I?.SyncAlpineRestoredDay();
        }

        internal bool TryAuthorizeService(Vector3 point, Vector2 size, out string companyId,
            out string sectorId, out string reason)
        {
            companyId = sectorId = null;
            if (!HasStation || managementStation != StationRuntime.I || session == null)
            {
                reason = "Compte de l’entreprise Alpine Empires indisponible.";
                return false;
            }

            var company = session.Companies[0];
            var result = session.CheckWorldConstruction(company.ManagerId, company.Id,
                AlpineManagementLand.ServiceFootprint(point, size), out sectorId);
            if (result != AlpineActionResult.Applied)
            {
                reason = result == AlpineActionResult.Undiscovered
                    ? "Concession inconnue. Explorez-la et achetez-la avant de construire."
                    : "Construisez sur votre concession de départ ou sur une extension acquise.";
                return false;
            }

            companyId = company.Id;
            reason = "";
            return true;
        }

        private void CreateSession()
        {
            if (AlpineManagementLand.TryCreateSession(out var created, out feedback))
                session = created;
        }

        public void Draw(GUIStyle label, GUIStyle button, Action<Action> enqueue, Action openStation, Action back)
        {
            GUILayout.Label("ALPINE EMPIRES — AVANT-PREMIÈRE", label);
            GUILayout.Label("Empire partagé solo · 1 gestionnaire humain et 3 places alliées.", label);
            GUILayout.Label("Gère ta station dans le monde 3D. Les trois places alliées sont réservées. Enregistre le domaine matériel depuis le menu Pause, à un point calme.", label);
            GUILayout.Space(12);

            if (session == null)
            {
                if (DemoPadUi263.Button("Nouvelle partie solo", button, GUILayout.Height(50)))
                    enqueue(CreateSession);
                if (!string.IsNullOrEmpty(feedback)) GUILayout.Label(feedback, label);
            }
            else
            {
                GUILayout.Label("Stations de départ et trésoreries individuelles", label);
                foreach (var company in session.Companies)
                {
                    string manager = company.IsHuman ? "Toi" : "Place alliée";
                    float balance = company.IsHuman && HasStation ? managementStation.State.money : company.Balance;
                    GUILayout.Label($"{company.StationId} · {manager} · {balance:N0} €", label);
                }

                GUILayout.Space(12);
                if (DemoPadUi263.Button(HasStation ? "Reprendre ma station 3D" : "Jouer ma station en 3D", button, GUILayout.Height(50)))
                    enqueue(openStation);

                if (HasStation)
                {
                    GUILayout.Label("La trésorerie est celle de ta station 3D. Les bâtiments de service sont autorisés sur ta concession de départ et sur les extensions achetées avant le lancement.", label);
#if UNITY_EDITOR
                    if (DemoPadUi263.Button("Essai Bloc 2 · chantier de l’entreprise alliée B", button, GUILayout.Height(45)))
                        enqueue(TestAlliedConstruction);
                    if (!string.IsNullOrEmpty(feedback)) GUILayout.Label(feedback, label);
#endif
                }
                else
                {
                    GUILayout.Label("Les extensions bordent la concession du front de neige, à l’ouest et à l’est. Achète-les avant de lancer la 3D pour y construire des bâtiments de service.", label);
                    DrawSector("expansion-1", "Extension ouest", label, button, enqueue);
                    DrawSector("expansion-2", "Extension est", label, button, enqueue);
                    DrawSector("protected-1", "Secteur protégé", label, button, enqueue);
                    if (!string.IsNullOrEmpty(feedback))
                        GUILayout.Label(feedback, label);
                }
            }

            if (DemoPadUi263.Button("Retour à l’accueil", button, GUILayout.Height(40)))
                enqueue(back);
        }

#if UNITY_EDITOR
        private void TestAlliedConstruction()
        {
            var builder = StationServiceConstructionRuntime.I;
            if (builder == null)
            {
                feedback = "Construction Gestion indisponible.";
                return;
            }
            feedback = builder.TestAlpineAlliedRestaurant(ActiveWorld);
        }
#endif

        private void DrawSector(string sectorId, string name, GUIStyle label, GUIStyle button, Action<Action> enqueue)
        {
            var company = session.Companies[0];
            var sector = session.Inspect(company.ManagerId, company.Id, sectorId);
            if (sector == null)
            {
                if (DemoPadUi263.Button($"Explorer · {name}", button, GUILayout.Height(40)))
                    enqueue(() => Apply(company, AlpineActionKind.Discover, sectorId, name, 0f));
                return;
            }

            if (sector.OwnerId != null)
            {
                GUILayout.Label($"{name} · concession acquise", label);
                return;
            }
            if (!sector.Buildable)
            {
                GUILayout.Label($"{name} · zone protégée, construction interdite", label);
                return;
            }

            if (DemoPadUi263.Button($"Acheter · {name} · {sector.Price:N0} €", button, GUILayout.Height(40)))
                enqueue(() => Apply(company, AlpineActionKind.Buy, sectorId, name, sector.Price));
        }

        private void Apply(AlpineCompany company, AlpineActionKind kind, string sectorId, string name, float amount)
        {
            var result = session.Apply(new AlpineCommand
            {
                kind = kind,
                operationId = Guid.NewGuid().ToString("N"),
                managerId = company.ManagerId,
                companyId = company.Id,
                subjectId = sectorId,
                amount = amount
            });

            switch (result)
            {
                case AlpineActionResult.Applied:
                    feedback = kind == AlpineActionKind.Discover ? $"{name} exploré." : $"{name} acquis.";
                    break;
                case AlpineActionResult.InsufficientFunds:
                    feedback = "Fonds insuffisants.";
                    break;
                default:
                    feedback = $"Opération refusée ({result}).";
                    break;
            }
        }
    }
}
