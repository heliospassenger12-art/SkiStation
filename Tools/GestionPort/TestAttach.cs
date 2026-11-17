using System; using System.Linq; using SkiStation.Gestion;
class TA { static void Main() {
  Game.NewGame(new NewGameOptions{mode="sandbox"});
  var L1=Game.CommitLift(Game.AnalyzeLift("chair_fixed_2",new P2(20,2450),new P2(60,1150)));
  var L2=Game.CommitLift(Game.AnalyzeLift("chair_fixed_2",new P2(420,1900),new P2(520,1100)));
  var an=Game.AnalyzePiste(new[]{new P2(130,1180),new P2(300,1500),new P2(390,1810)},30); Console.WriteLine("main "+an.ok+" départ "+an.startLift+" arrivée "+an.endLift+" ctrl "+string.Join(" ",an.ctrl.Select(c=>"("+c.x+","+c.z+")")));
  var P=Game.CommitPiste(an.ctrl,an);
  var P2c=new[]{new P2(60,1150),new P2(-150,1500),new P2(-260,1880)}; an=Game.AnalyzePiste(P2c,30); var P2=Game.CommitPiste(an.ctrl,an);
  var L3=Game.CommitLift(Game.AnalyzeLift("chair_fixed_2",new P2(-300,1990),new P2(-500,1300)));
  Console.WriteLine("P2 après L3 "+string.Join(" ",P2.ctrl.Select(c=>"("+c.x+","+c.z+")")));
  Game.Net.Rebuild(); Console.WriteLine("P2 fin = gare L3 ? "+(P2.nb==L3.na)+" | départ=L1 "+(P.na==L1.nb)+" arrivée=L2 "+(P.nb==L2.na));
  foreach (var d in new[]{"any","self","hub"}) { Game.AutoDest=d; var r=Game.AutoPiste(new P2(520,1100),null,30,1); Console.WriteLine("auto "+d+" "+(r.err??("dest "+r.dest+" len "+(int)r.an.len+" ok "+r.an.ok))); }
}}
