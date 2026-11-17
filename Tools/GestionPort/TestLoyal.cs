using System; using System.Linq; using SkiStation.Gestion;
class TL { static void Main(string[] a) {
  int days=a.Length>0?int.Parse(a[0]):170;
  var G=Game.NewGame(new NewGameOptions{mode="sandbox", unlimited=true});
  Game.CommitLift(Game.AnalyzeLift("chair_fixed_2",new P2(20,2450),new P2(60,1150)));
  foreach (var e in new[]{1,0,2}) { Game.AutoDest="self"; var r=Game.AutoPiste(new P2(60,1150),null,30,e); Game.AutoDest="any"; if(r.err==null&&r.an.ok) Game.CommitPiste(r.an.ctrl,r.an); }
  foreach (var t in new[]{Tuple.Create("restaurant",-150.0,2600.0),Tuple.Create("rental",90.0,2620.0),Tuple.Create("garage",260.0,2700.0),Tuple.Create("rescue",-180.0,2500.0),Tuple.Create("school",170.0,2540.0),Tuple.Create("bar",20.0,2700.0),Tuple.Create("parking",300.0,2950.0),Tuple.Create("parking",-300.0,2980.0),Tuple.Create("snack",-60.0,2700.0)}) { var an=Game.AnalyzeBuilding(t.Item1,t.Item2,t.Item3); if(an.ok) Game.CommitBuilding(an,0); }
  G.paused=false; G.speed=8; var t0=DateTime.Now;
  Game.Bus+=(ev,x,y)=>{ if(ev!="day:end") return; var r=(DayReport)x; var ls=Loyalty.Stats(); bool pass=r.notes.Any(n=>n.Contains("forfait saison"));
    if(G.elapsed%20==0||pass) Console.WriteLine("J"+G.elapsed+" an "+G.year+" vis "+r.visitors+" sat "+r.sat+" | habitués "+ls.clients+" fidèles "+ls.loyal+" pass "+ls.pass+" b-à-o x"+Loyalty.DemandMul().ToString("0.00")+" fame "+G.fame.ToString("0")+" "+string.Join(" ",r.notes.Where(n=>n.Contains("forfait saison")))); };
  while(G.elapsed<days) Sim.Tick(1.0/30);
  Console.WriteLine("top "+string.Join(" | ",Loyalty.Stats().top.Take(4).Select(c=>Loyalty.Label(c)+" "+c.visits+" venues fid "+(int)(c.loy*100)+"% fav "+c.fav)));
  var js=SaveLoad.Serialize(G); var g2=SaveLoad.Restore(SaveLoad.Deserialize(js)); Console.WriteLine("sauvegarde : habitués "+g2.clients.Count+" ms "+(DateTime.Now-t0).TotalMilliseconds);
}}
