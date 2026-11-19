using System;
using System.Collections.Generic;
using System.Linq;

namespace SkiStation.AlpineEmpires
{
    public enum AlpineTeamMode { Individuel, DeuxEquipes, EmpirePartage }
    public enum AlpineActionKind { Discover, Buy, TestExpense, TestRevenue, ServiceBuilt, ServiceRevenue, ServiceOperatingCost }
    public enum AlpineActionResult
    {
        Applied, AlreadyApplied, Invalid, Conflict, Forbidden, Undiscovered, Occupied,
        InsufficientFunds, AmountOutOfRange, JournalFull
    }

    [Serializable]
    public struct AlpineCommand
    {
        public AlpineActionKind kind;
        public string operationId, managerId, companyId, subjectId;
        public float amount;
        public int cents, day;
        public float hour;
        public FinanceCategory category;
        public CustomerProfile customer;
        public string sectorId;
        public AlpineFootprint footprint;

        internal bool SameAs(AlpineCommand other)
        {
            return kind == other.kind && operationId == other.operationId && managerId == other.managerId
                && companyId == other.companyId && subjectId == other.subjectId && amount == other.amount
                && cents == other.cents && day == other.day && hour == other.hour && category == other.category
                && customer == other.customer && (sectorId ?? "") == (other.sectorId ?? "") && footprint.Equals(other.footprint);
        }
    }

    public sealed class AlpineCompany
    {
        internal readonly StationState State = new StationState();
        internal readonly FinanceLedger Ledger = new FinanceLedger();
        internal readonly HashSet<string> Discovered = new HashSet<string>(StringComparer.Ordinal);

        public string Id { get; }
        public string ManagerId { get; }
        public string StationId { get; }
        public string TeamId { get; }
        public string InitialSectorId { get; }
        public bool IsHuman { get; }
        internal decimal WorldDelta { get; private set; }
        internal decimal ExactBalance => (decimal)State.money + WorldDelta;
        internal void ApplyWorldDelta(decimal amount) => WorldDelta += amount;
        public float Balance => WorldDelta == 0m ? State.money : (float)ExactBalance;
        public float Revenue => Ledger.TotalRevenue;
        public float Expenses => Ledger.TotalExpenses;
        public int EntryCount => Ledger.Transactions.Count;

        internal AlpineCompany(int slot, bool human, string teamId)
        {
            Id = $"company-{slot}";
            ManagerId = $"manager-{slot}";
            StationId = $"station-{slot}";
            TeamId = teamId;
            InitialSectorId = $"start-{slot}";
            IsHuman = human;
            Discovered.Add(InitialSectorId);
        }
    }

    [Serializable]
    public sealed class AlpineSessionSave
    {
        public int schema;
        public int humanCount;
        public AlpineTeamMode teamMode;
        public AlpineCommand[] commands;
        public AlpineLandSeed261[] layout;
    }

    [Serializable]
    public sealed class AlpineLandSeed261
    {
        public string id, ownerId, regionId;
        public AlpineFootprint bounds;
        public float price;
        public bool buildable;

        internal AlpineTerritory ToTerritory()
            => new AlpineTerritory(id, bounds, price, buildable, string.IsNullOrEmpty(ownerId) ? null : ownerId, regionId);
    }

    // Opt-in lab model. Existing scene systems keep their historical entry points.
    public sealed class AlpineSession
    {
        public const int Schema = 1;
        public const int MaxOperations = 10000;
        private const float MaxAmount = 20000000f;
        private const int WorldSchema = 3;
        private readonly bool usesWorldLayout;
        private readonly AlpineLandSeed261[] layout;
        private readonly object gate = new object();
        private readonly Dictionary<string, AlpineCompany> companies = new Dictionary<string, AlpineCompany>(StringComparer.Ordinal);
        private readonly Dictionary<string, AlpineTerritory> territories = new Dictionary<string, AlpineTerritory>(StringComparer.Ordinal);
        private readonly Dictionary<string, AlpineCommand> operations = new Dictionary<string, AlpineCommand>(StringComparer.Ordinal);
        private readonly List<AlpineCommand> journal = new List<AlpineCommand>();
        private readonly Dictionary<string, AlpineCommand> services = new Dictionary<string, AlpineCommand>(StringComparer.Ordinal);

        public int HumanCount { get; }
        public AlpineTeamMode TeamMode { get; }
        public IReadOnlyList<AlpineCompany> Companies { get; }

        public AlpineSession(int humanCount, AlpineTeamMode teamMode)
            : this(humanCount, teamMode, null) { }

        internal AlpineSession(int humanCount, AlpineTeamMode teamMode, IReadOnlyList<AlpineTerritory> worldLayout)
        {
            if (humanCount < 1 || humanCount > 4 || !Enum.IsDefined(typeof(AlpineTeamMode), teamMode))
                throw new ArgumentException("Composition invalide.");
            HumanCount = humanCount;
            TeamMode = teamMode;
            usesWorldLayout = worldLayout != null;
            for (int i = 1; i <= 4; i++)
            {
                var company = new AlpineCompany(i, i <= humanCount, TeamForSlot(teamMode, i));
                companies.Add(company.Id, company);
                if (!usesWorldLayout)
                    territories.Add(company.InitialSectorId, new AlpineTerritory(company.InitialSectorId,
                        new AlpineFootprint((i - 1) * 100, i * 100, 0, 100, 1350, 4000), 0, true, company.Id));
            }
            Companies = Array.AsReadOnly(companies.Values.OrderBy(c => c.Id, StringComparer.Ordinal).ToArray());
            if (usesWorldLayout)
            {
                if (worldLayout.Count != 7) throw new ArgumentException("Sept concessions attendues.");
                layout = new AlpineLandSeed261[worldLayout.Count];
                for (int i = 0; i < worldLayout.Count; i++)
                {
                    var sector = worldLayout[i];
                    if (sector == null || !sector.Bounds.Valid || string.IsNullOrWhiteSpace(sector.Id)
                        || sector.RegionId != "bootstrap-front-de-neige" || !Finite(sector.Price)
                        || sector.Price < 0 || sector.Price > MaxAmount
                        || sector.Bounds.highAltitude > 4050f || sector.Bounds.lowAltitude < 1300f
                        || (sector.OwnerId != null && (!companies.TryGetValue(sector.OwnerId, out var owner)
                            || sector.Id != owner.InitialSectorId || !sector.Buildable || sector.Price != 0f)))
                        throw new ArgumentException("Concession de départ invalide.");
                    for (int j = 0; j < i; j++)
                    {
                        var other = layout[j].bounds;
                        if (sector.Bounds.minX < other.maxX && sector.Bounds.maxX > other.minX
                            && sector.Bounds.minZ < other.maxZ && sector.Bounds.maxZ > other.minZ)
                            throw new ArgumentException("Concessions superposées.");
                    }
                    layout[i] = new AlpineLandSeed261 { id = sector.Id, ownerId = sector.OwnerId, regionId = sector.RegionId,
                        bounds = sector.Bounds, price = sector.Price, buildable = sector.Buildable };
                    territories.Add(sector.Id, new AlpineTerritory(sector.Id, sector.Bounds, sector.Price,
                        sector.Buildable, sector.OwnerId, sector.RegionId));
                }
                foreach (var company in Companies)
                    if (!territories.TryGetValue(company.InitialSectorId, out var start) || start.OwnerId != company.Id)
                        throw new ArgumentException("Concession de départ manquante.");
                return;
            }
            territories.Add("expansion-1", new AlpineTerritory("expansion-1", new AlpineFootprint(0, 100, 100, 200, 1350, 4000), 50000, true));
            territories.Add("expansion-2", new AlpineTerritory("expansion-2", new AlpineFootprint(100, 200, 100, 200, 1350, 4000), 100000, true));
            territories.Add("protected-1", new AlpineTerritory("protected-1", new AlpineFootprint(200, 300, 100, 200, 3300, 4050), 0, false));
        }

        private static string TeamForSlot(AlpineTeamMode mode, int slot)
        {
            switch (mode)
            {
                case AlpineTeamMode.EmpirePartage: return "team-1";
                case AlpineTeamMode.DeuxEquipes: return slot <= 2 ? "team-1" : "team-2";
                default: return $"team-{slot}";
            }
        }

        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public AlpineTerritory Inspect(string managerId, string companyId, string sectorId)
        {
            lock (gate)
            {
                if (!Resolve(managerId, companyId, out var company) || sectorId == null
                    || !company.Discovered.Contains(sectorId) || !territories.TryGetValue(sectorId, out var sector))
                    return null;
                return new AlpineTerritory(sector.Id, sector.Bounds, sector.Price, sector.Buildable, sector.OwnerId, sector.RegionId);
            }
        }

        public AlpineActionResult CheckConstruction(string managerId, string companyId, string sectorId, AlpineFootprint footprint)
        {
            var sector = Inspect(managerId, companyId, sectorId);
            if (sector == null)
                return AlpineActionResult.Undiscovered;
            if (sector.OwnerId != companyId || !sector.Buildable || !footprint.Inside(sector.Bounds)
                || footprint.highAltitude > 4050f)
                return AlpineActionResult.Forbidden;
            return AlpineActionResult.Applied;
        }

        internal AlpineActionResult CheckWorldConstruction(string managerId, string companyId,
            AlpineFootprint footprint, out string sectorId)
        {
            sectorId = null;
            lock (gate)
            {
                if (!usesWorldLayout || !footprint.Valid || !Resolve(managerId, companyId, out _))
                    return AlpineActionResult.Invalid;
                foreach (var sector in territories.Values)
                {
                    if (!footprint.Inside(sector.Bounds)) continue;
                    var result = CheckConstruction(managerId, companyId, sector.Id, footprint);
                    if (result == AlpineActionResult.Applied) sectorId = sector.Id;
                    return result;
                }
            }
            return AlpineActionResult.Forbidden;
        }

        public float TeamBalance(string teamId)
        {
            lock (gate)
                return companies.Values.Where(company => company.TeamId == teamId).Sum(company => company.Balance);
        }

        private bool Resolve(string managerId, string companyId, out AlpineCompany company)
        {
            company = null;
            return companyId != null && companies.TryGetValue(companyId, out company) && company.ManagerId == managerId;
        }

        public AlpineActionResult Apply(AlpineCommand command) => Apply(command, false, null);

        internal bool TryGetManager(string companyId, out AlpineCompany company)
        {
            company = null;
            return companyId != null && companies.TryGetValue(companyId, out company);
        }

        internal bool HasBuiltService(string operationId)
        {
            lock (gate)
                return operations.TryGetValue(operationId, out var previous) && previous.kind == AlpineActionKind.ServiceBuilt;
        }

        internal bool TryBuildRetry(string operationId, string managerId, string companyId,
            AlpineFootprint footprint, out AlpineActionResult result)
        {
            result = AlpineActionResult.Invalid;
            lock (gate)
            {
                if (!operations.TryGetValue(operationId, out var previous)) return false;
                result = previous.kind == AlpineActionKind.ServiceBuilt && previous.managerId == managerId
                    && previous.companyId == companyId && previous.footprint.Equals(footprint)
                    ? AlpineActionResult.AlreadyApplied : AlpineActionResult.Conflict;
                return true;
            }
        }

        internal bool OwnsService(string companyId, string sectorId, string serviceId)
        {
            lock (gate)
                return serviceId != null && services.TryGetValue(serviceId, out var receipt)
                    && receipt.companyId == companyId && receipt.sectorId == sectorId;
        }

        internal bool MatchesServiceSite(string companyId, string sectorId, string serviceId, float x, float z)
        {
            lock (gate)
            {
                if (!OwnsService(companyId, sectorId, serviceId)) return false;
                var bounds = services[serviceId].footprint;
                return Math.Abs(x - (bounds.minX + bounds.maxX) * .5f) <= .05f
                    && Math.Abs(z - (bounds.minZ + bounds.maxZ) * .5f) <= .05f;
            }
        }

        internal int BuiltServiceCount { get { lock (gate) return services.Count; } }

        internal AlpineActionResult BuildService(AlpineCommand command, Func<bool> materialize)
            => command.kind == AlpineActionKind.ServiceBuilt && materialize != null
                ? Apply(command, true, materialize) : AlpineActionResult.Invalid;

        internal AlpineActionResult PostServiceReceipt(AlpineCommand command)
            => command.kind == AlpineActionKind.ServiceRevenue || command.kind == AlpineActionKind.ServiceOperatingCost
                ? Apply(command, true, null) : AlpineActionResult.Invalid;

        private AlpineActionResult Apply(AlpineCommand command, bool native, Func<bool> materialize)
        {
            lock (gate)
            {
                if (string.IsNullOrWhiteSpace(command.operationId) || command.operationId.Length > 128
                    || !Resolve(command.managerId, command.companyId, out var company)
                    || !Finite(command.amount) || string.IsNullOrWhiteSpace(command.subjectId)
                    || command.subjectId.Length > 128)
                    return AlpineActionResult.Invalid;
                bool serviceAction = command.kind == AlpineActionKind.ServiceBuilt || command.kind == AlpineActionKind.ServiceRevenue
                    || command.kind == AlpineActionKind.ServiceOperatingCost;
                if (serviceAction && (!native || !usesWorldLayout)) return AlpineActionResult.Forbidden;
                if (operations.TryGetValue(command.operationId, out var previous))
                    return previous.SameAs(command) ? AlpineActionResult.AlreadyApplied : AlpineActionResult.Conflict;
                if (journal.Count >= MaxOperations)
                    return AlpineActionResult.JournalFull;

                var result = serviceAction ? ApplyServiceReceipt(company, command, materialize) : ApplyToCompany(company, command);
                if (result == AlpineActionResult.Applied)
                {
                    operations.Add(command.operationId, command);
                    journal.Add(command);
                }
                return result;
            }
        }

        private AlpineActionResult ApplyServiceReceipt(AlpineCompany company, AlpineCommand command, Func<bool> materialize)
        {
            if (company.IsHuman || command.cents <= 0 || command.day < 1 || !Finite(command.hour)
                || command.hour < 0 || command.hour > 24 || !Enum.IsDefined(typeof(FinanceCategory), command.category)
                || !Enum.IsDefined(typeof(CustomerProfile), command.customer))
                return AlpineActionResult.Invalid;

            bool build = command.kind == AlpineActionKind.ServiceBuilt;
            bool revenue = command.kind == AlpineActionKind.ServiceRevenue;
            if (build)
            {
                if (command.category != FinanceCategory.Construction || services.ContainsKey(command.subjectId))
                    return AlpineActionResult.Conflict;
                var permission = CheckWorldConstruction(company.ManagerId, company.Id, command.footprint, out var sector);
                if (permission != AlpineActionResult.Applied || sector != command.sectorId)
                    return AlpineActionResult.Forbidden;
            }
            else
            {
                if (!OwnsService(company.Id, command.sectorId, command.subjectId)
                    || command.day < services[command.subjectId].day
                    || (revenue ? command.category != FinanceCategory.Food : command.category != FinanceCategory.Maintenance))
                    return AlpineActionResult.Forbidden;
            }

            decimal amount = command.cents / 100m;
            if (!revenue && company.ExactBalance < amount)
                return AlpineActionResult.InsufficientFunds;
            // Only the live construction path supplies a factory. Replay rebuilds the journal
            // and never invokes the factory: the native domain checkpoint owns the geometry.
            if (build && materialize != null && !materialize())
                return AlpineActionResult.Invalid;

            company.Ledger.Record(new FinanceTransaction(revenue ? FinanceTransactionType.Revenue : FinanceTransactionType.Expense,
                command.category, command.customer, (float)amount, command.day, command.hour, command.operationId, command.subjectId));
            company.ApplyWorldDelta(revenue ? amount : -amount);
            if (build) services.Add(command.subjectId, command);
            return AlpineActionResult.Applied;
        }

        private AlpineActionResult ApplyToCompany(AlpineCompany company, AlpineCommand command)
        {
            switch (command.kind)
            {
                case AlpineActionKind.Discover:
                case AlpineActionKind.Buy:
                    return ApplyTerritory(company, command);
                case AlpineActionKind.TestExpense:
                case AlpineActionKind.TestRevenue:
                    if (usesWorldLayout || command.subjectId != company.StationId)
                        return AlpineActionResult.Forbidden;
                    return RecordPayment(company, command, FinanceCategory.Other,
                        command.kind == AlpineActionKind.TestRevenue);
                default:
                    return AlpineActionResult.Invalid;
            }
        }

        private AlpineActionResult ApplyTerritory(AlpineCompany company, AlpineCommand command)
        {
            if (!territories.TryGetValue(command.subjectId, out var sector))
                return AlpineActionResult.Invalid;
            if (command.kind == AlpineActionKind.Discover)
            {
                if (command.amount != 0f)
                    return AlpineActionResult.Invalid;
                return company.Discovered.Add(sector.Id) ? AlpineActionResult.Applied : AlpineActionResult.AlreadyApplied;
            }
            if (!company.Discovered.Contains(sector.Id))
                return AlpineActionResult.Undiscovered;
            if (!sector.Buildable)
                return AlpineActionResult.Forbidden;
            if (sector.OwnerId != null)
                return AlpineActionResult.Occupied;
            if (command.amount != sector.Price)
                return AlpineActionResult.Conflict;

            var payment = RecordPayment(company, command, FinanceCategory.Construction, false);
            if (payment == AlpineActionResult.Applied)
                sector.OwnerId = company.Id;
            return payment;
        }

        private AlpineActionResult RecordPayment(AlpineCompany company, AlpineCommand command,
            FinanceCategory category, bool revenue)
        {
            var amount = command.amount;
            if (amount <= 0f || amount > MaxAmount)
                return AlpineActionResult.AmountOutOfRange;
            if (usesWorldLayout)
            {
                decimal exact = (decimal)amount;
                if (exact != decimal.Round(exact, 2)) return AlpineActionResult.AmountOutOfRange;
                if (!revenue && company.ExactBalance < exact) return AlpineActionResult.InsufficientFunds;
                company.Ledger.Record(new FinanceTransaction(revenue ? FinanceTransactionType.Revenue : FinanceTransactionType.Expense,
                    category, CustomerProfile.Tourist, amount, company.State.day, 0f, command.operationId, command.subjectId));
                company.ApplyWorldDelta(revenue ? exact : -exact);
                return AlpineActionResult.Applied;
            }
            if (!revenue && company.Balance < amount)
                return AlpineActionResult.InsufficientFunds;

            float total = revenue ? company.Revenue : company.Expenses;
            float next = revenue ? company.Balance + amount : company.Balance - amount;
            var totals = revenue ? company.Ledger.Revenue : company.Ledger.Expenses;
            totals.TryGetValue(category, out var categoryTotal);
            float balanceDelta = revenue ? next - company.Balance : company.Balance - next;
            if (!Finite(next) || balanceDelta != amount
                || !Finite(total + amount) || total + amount - total != amount
                || !Finite(categoryTotal + amount) || categoryTotal + amount - categoryTotal != amount)
                return AlpineActionResult.AmountOutOfRange;

            var type = revenue ? FinanceTransactionType.Revenue : FinanceTransactionType.Expense;
            company.Ledger.Record(new FinanceTransaction(type, category, CustomerProfile.Tourist,
                amount, company.State.day, 0f, command.operationId, command.subjectId));
            company.State.money = next;
            return AlpineActionResult.Applied;
        }

        public AlpineSessionSave Capture()
        {
            if (usesWorldLayout)
                throw new InvalidOperationException("Le schéma du laboratoire ne sauvegarde pas une partie 3D.");
            lock (gate)
                return new AlpineSessionSave
                {
                    schema = Schema, humanCount = HumanCount, teamMode = TeamMode, commands = journal.ToArray()
                };
        }

        internal AlpineSessionSave CaptureWorld()
        {
            if (!usesWorldLayout) throw new InvalidOperationException("Session 3D requise.");
            lock (gate)
                return new AlpineSessionSave
                {
                    schema = WorldSchema, humanCount = HumanCount, teamMode = TeamMode, commands = journal.ToArray(),
                    layout = layout.Select(sector => new AlpineLandSeed261 { id = sector.id, ownerId = sector.ownerId,
                        regionId = sector.regionId, bounds = sector.bounds, price = sector.price, buildable = sector.buildable }).ToArray()
                };
        }

        public static AlpineSession Restore(AlpineSessionSave save)
            => Restore(save, false);

        internal static AlpineSession RestoreWorld(AlpineSessionSave save)
            => Restore(save, true);

        private static AlpineSession Restore(AlpineSessionSave save, bool world)
        {
            if (save == null || (world ? save.schema != 2 && save.schema != WorldSchema : save.schema != Schema)
                || save.commands == null || save.commands.Length > MaxOperations)
                throw new ArgumentException("Sauvegarde Alpine Empires invalide ou incompatible.");
            AlpineTerritory[] land = null;
            if (world)
            {
                if (save.humanCount != 1 || save.teamMode != AlpineTeamMode.EmpirePartage || save.layout == null
                    || save.layout.Length != 7 || save.layout.Any(seed => seed == null))
                    throw new ArgumentException("Carte Alpine Empires incompatible.");
                land = save.layout.Select(seed => seed.ToTerritory()).ToArray();
            }
            var restored = new AlpineSession(save.humanCount, save.teamMode, land);
            foreach (var command in save.commands)
                if (restored.Apply(command, world && save.schema == WorldSchema, null) != AlpineActionResult.Applied)
                    throw new ArgumentException("Journal Alpine Empires incohérent.");
            return restored;
        }
    }
}
