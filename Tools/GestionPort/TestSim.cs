using System; using System.Linq; using System.Collections.Generic; using SkiStation.Gestion;
class TestSim {
  static void Main(string[] a) {
    var t0=DateTime.Now; int days=a.Length>0?int.Parse(a[0]):6;
    var G=Game.NewGame(new NewGameOptions{mode="sandbox"});
    Console.WriteLine("init ms "+(DateTime.Now-t0).TotalMilliseconds);
    Func<string,P2,P2,Lift> tryLift=(type,pa,pb)=>{var an=Game.AnalyzeLift(type,pa,pb);Console.WriteLine(type+" "+(an.ok?"OK":"ERR "+an.err)+" len "+(int)an.len+" vert "+(int)an.vert+" cost "+an.cost+" cap "+an.capacity+" ride "+an.rideMin.ToString("0.0"));return an.ok?Game.CommitLift(an):null;};
    tryLift("chair_fixed_2",new P2(20,2450),new P2(60,1150));
    tryLift("tow_classic",new P2(-70,2440),new P2(-200,1700));
    Action<P2[]> tryPiste=ctrl=>{var an=Game.AnalyzePiste(ctrl,30);Console.WriteLine("piste "+(an.ok?"OK":"ERR "+an.err)+" len "+(int)an.len+" drop "+(int)an.drop+" avg "+(int)(an.avg*100)+"% p90 "+(int)(an.p90*100)+"% diff "+an.diff+" cost "+an.cost+" trees "+an.trees);if(an.ok)Game.CommitPiste(ctrl,an);};
    tryPiste(new[]{new P2(80,1150),new P2(350,1500),new P2(250,2000),new P2(40,2440)});
    tryPiste(new[]{new P2(40,1150),new P2(-80,1400),new P2(-190,1690)});
    tryPiste(new[]{new P2(-210,1700),new P2(-380,2000),new P2(-200,2300),new P2(-30,2460)});
    Console.WriteLine("nodes "+Game.Net.nodes.Count+" edges "+string.Join(" ",Game.Net.edges.Select(e=>e.kind+":"+e.from.id+">"+e.to.id)));
    foreach(var t in new[]{Tuple.Create("restaurant",-120.0,2600.0),Tuple.Create("rental",60.0,2600.0),Tuple.Create("garage",200.0,2700.0),Tuple.Create("rescue",-180.0,2520.0),Tuple.Create("school",140.0,2560.0),Tuple.Create("pioupiou",-200.0,2700.0),Tuple.Create("bar",0.0,2680.0)}){var an=Game.AnalyzeBuilding(t.Item1,t.Item2,t.Item3);Console.WriteLine(t.Item1+" "+(an.ok?"OK":an.err)+" "+(an.warn??"")+" "+an.cost);if(an.ok)Game.CommitBuilding(an,0);}
    var ap=Game.AutoPiste(new P2(60,1150),null,30,1); Console.WriteLine("auto "+(ap.err??("ctrl "+ap.ctrl.Count+" len "+(int)ap.an.len+" diff "+ap.an.diff+" ok "+ap.an.ok)));
    Console.WriteLine("money "+(long)G.money);
    G.paused=false;G.speed=8;
    Game.Bus+=(ev,x,y)=>{ if(ev=="day:end"){var r=(DayReport)x;Console.WriteLine("DAY "+r.date+" vis "+r.visitors+" sat "+r.sat+" inc "+(int)r.inc+" exp "+(int)r.exp+" money "+(long)G.money+" rep "+(int)G.reputation+" prest "+G.prestige+" fame "+G.fame.ToString("0.0")+" fb "+string.Join(",",r.fb.Select(k=>k.Key+":"+k.Value))+" acc "+r.incidents+" "+string.Join(" | ",r.notes));} };
    t0=DateTime.Now; long frames=0;
    while(G.day<days){Sim.Tick(1.0/30);frames++;}
    Console.WriteLine("ms "+(DateTime.Now-t0).TotalMilliseconds+" frames "+frames+" pistes "+string.Join(", ",G.pistes.Select(p=>p.name+" g"+p.groomed.ToString("0.00")+" d"+(int)p.depth))+" medals "+string.Join(",",G.medals.Select(k=>k.Key+":"+k.Value)));
    var js=SaveLoad.Serialize(G); Console.WriteLine("save bytes "+js.Length);
    var g2=SaveLoad.Restore(SaveLoad.Deserialize(js)); Console.WriteLine("restored pistes "+g2.pistes.Count+" lifts "+g2.lifts.Count+" bld "+g2.buildings.Count+" money "+(long)g2.money+" demand "+Sim.Today.total);
  }
}
