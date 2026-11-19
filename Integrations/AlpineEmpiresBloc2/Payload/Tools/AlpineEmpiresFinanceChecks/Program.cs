using System;
using System.Linq;
using System.Text.Json;
using SkiStation;
using SkiStation.AlpineEmpires;

var layout = Enumerable.Range(1, 7).Select(i =>
{
    var bounds = new AlpineFootprint(i * 100, i * 100 + 100, 0, 100, 1350, 4000);
    return new AlpineTerritory(i <= 4 ? $"start-{i}" : $"extension-{i}", bounds,
        i <= 4 ? 0 : 50000, true, i <= 4 ? $"company-{i}" : null, "bootstrap-front-de-neige");
}).ToArray();
var session = new AlpineSession(1, AlpineTeamMode.EmpirePartage, layout);
var older = session.CaptureWorld();
older.schema = 2;
Check(AlpineSession.RestoreWorld(older).Companies[1].ExactBalance == 650000m, "Reprise ancienne");
var build = new AlpineCommand
{
    kind = AlpineActionKind.ServiceBuilt,
    companyId = "company-2",
    managerId = "manager-2",
    sectorId = "start-2",
    subjectId = "restaurant-2",
    operationId = "build.2",
    footprint = new AlpineFootprint(212, 240, 20, 48, 1360, 1360),
    cents = 7500000,
    category = FinanceCategory.Construction,
    customer = CustomerProfile.Tourist,
    day = 1,
    hour = 7f
};
var spoof = build;
spoof.managerId = "manager-1";
Check(session.BuildService(spoof, () => throw new Exception("Fabrication non autorisée")) == AlpineActionResult.Invalid, "Identité");
var crossing = build;
crossing.footprint.maxX = 301;
Check(session.BuildService(crossing, () => throw new Exception("Fabrication hors secteur")) == AlpineActionResult.Forbidden, "Emprise");
Check(session.Apply(build) == AlpineActionResult.Forbidden, "Canal explicite");
Check(session.BuildService(build, () => false) == AlpineActionResult.Invalid, "Abandon de chantier");
Check(session.Companies[1].ExactBalance == 650000m, "Échec sans débit");
int created = 0;
Check(session.BuildService(build, () => { created++; return true; }) == AlpineActionResult.Applied, "Chantier accepté");
Check(session.BuildService(build, () => { created++; return true; }) == AlpineActionResult.AlreadyApplied && created == 1, "Retry idempotent");
var sale = build;
sale.kind = AlpineActionKind.ServiceRevenue;
sale.operationId = "sale.2";
sale.category = FinanceCategory.Food;
sale.cents = 4501;
Check(session.PostServiceReceipt(sale) == AlpineActionResult.Applied, "Recette");
Check(session.PostServiceReceipt(sale) == AlpineActionResult.AlreadyApplied, "Pas de double recette");
var charges = sale;
charges.kind = AlpineActionKind.ServiceOperatingCost;
charges.operationId = "opex.2.1";
charges.cents = 29500;
charges.category = FinanceCategory.Maintenance;
Check(session.PostServiceReceipt(charges) == AlpineActionResult.Applied, "Charges");
Check(session.Companies[1].ExactBalance == 574750.01m, "Solde B exact");
Check(session.Companies.Where(x => x.Id != "company-2").All(x => x.ExactBalance == 650000m), "Comptes voisins");
var options = new JsonSerializerOptions { IncludeFields = true };
var restored = AlpineSession.RestoreWorld(JsonSerializer.Deserialize<AlpineSessionSave>(
    JsonSerializer.Serialize(session.CaptureWorld(), options), options));
Check(restored.Companies[1].ExactBalance == session.Companies[1].ExactBalance, "Solde restauré");
Check(restored.MatchesServiceSite("company-2", "start-2", "restaurant-2", 226f, 34f), "Bâtiment et reçu");
Check(restored.BuildService(build, () => throw new Exception("Fabrication après reprise")) == AlpineActionResult.AlreadyApplied, "Aucun nouveau chantier");
Console.WriteLine("Comptes Alpine Empires : assertions réussies.");

static void Check(bool ok, string context)
{
    if (!ok) throw new InvalidOperationException(context);
}

namespace SkiStation
{
    // Data-only mirror of the Unity class, for these checks without Unity.
    public class StationState { public int day = 1; public float money = 650000f; }
    public enum CustomerProfile { Beginner, Intermediate, Advanced, Expert, Family, Youth, Athlete, Premium, Tourist }
}
