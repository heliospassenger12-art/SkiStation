using System; using System.Linq; using SkiStation.Gestion;
class TestScn { static void Main(string[] a) {
  foreach (var id in new[]{"rescue","olympic","first"}) {
    var sc=Catalog.Scenario(id); var G=Game.NewGame(new NewGameOptions{mode="career",scn=id,seed=sc.start.seed,level=1});
    Console.WriteLine(id+": lifts "+G.lifts.Count+" pistes "+G.pistes.Count+" bld "+G.buildings.Count+" money "+(long)G.money+" loan "+G.loan+" prestige "+G.prestige+" hint: "+Scenarios.Hint());
    G.paused=false; G.speed=8; int d0=G.day; while(G.day<d0+3) Sim.Tick(1.0/30);
    Console.WriteLine("  after 3 days: vis "+G.visitorsToday+" best "+G.bestDay+" sat "+G.sat+" money "+(long)G.money+" fame "+G.fame.ToString("0.0")+" stage "+G.scn.stage+" xg "+Scenarios.CritShare("xgames").ToString("0.00")+" bid: "+Scenarios.BidCan().why.First());
    var e=MorningEvents.Current; if(e!=null){Console.WriteLine("  matin: "+e.title); MorningEvents.Choose(0);}
  }
}}
