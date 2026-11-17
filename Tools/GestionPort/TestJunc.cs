using System; using System.Linq; using SkiStation.Gestion;
class TestJunc { static void Main() {
  var G=Game.NewGame(new NewGameOptions{mode="sandbox"});
  Game.CommitLift(Game.AnalyzeLift("chair_fixed_2",new P2(20,2450),new P2(60,1150)));
  var P1=new[]{new P2(80,1150),new P2(350,1500),new P2(250,2000),new P2(40,2440)}; var a=Game.AnalyzePiste(P1,30); Game.CommitPiste(P1,a); Console.WriteLine("P1 "+a.ok+" len "+(int)a.len);
  var P2_=new[]{new P2(40,1150),new P2(-60,1500),new P2(200,1800),new P2(480,2100)}; a=Game.AnalyzePiste(P2_,30); Console.WriteLine("P2 croise "+a.ok+" "+a.err+" len "+(int)a.len+" joins "+a.joins+" ctrl "+string.Join(" ",a.ctrl.Select(c=>"("+c.x+","+c.z+")"))); if(a.ok) Game.CommitPiste(P2_,a);
  var P3=new[]{new P2(70,1160),new P2(340,1510),new P2(260,1990),new P2(60,2430)}; a=Game.AnalyzePiste(P3,30); Console.WriteLine("P3 longe "+a.ok+" "+a.err+" len "+(int)a.len);
  for(int e=0;e<3;e++){ var r=Game.AutoPiste(new P2(60,1150),null,30,e); Console.WriteLine("auto "+e+" "+(r.err??("ok "+r.an.ok+" len "+(int)r.an.len+" joins "+(r.an.joins??"-")+" diff "+r.an.diff))); }
  Game.Net.Rebuild(); Console.WriteLine("edges "+string.Join(" | ",Game.Net.edges.Select(x=>x.kind+(x.piste!=null?":"+x.piste.name:"")+" "+x.from.id+">"+x.to.id)));
  var ra=Game.AnalyzeRoad(new[]{new P2(-200,2900),new P2(100,3300),new P2(300,3500)}); Console.WriteLine("route "+ra.ok+" joins "+ra.joins+" pts "+string.Join(" ",ra.pts.Select(c=>"("+c.x+","+c.z+")")));
}}
