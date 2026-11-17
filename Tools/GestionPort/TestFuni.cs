using System; using System.Linq; using SkiStation.Gestion;
class TF { static void Main() {
  Game.NewGame(new NewGameOptions{mode="sandbox", allUnlocked=true, unlimited=true});
  var an=Game.AnalyzeLift("funicular",new P2(0,2330),new P2(1300,-1400));
  Console.WriteLine("funi "+an.ok+" "+an.err+" long "+(int)an.len+" dénivelé "+(int)an.vert+" coût "+an.cost+" points "+an.path.Count+" sous le relief "+an.path.Skip(1).Take(an.path.Count-2).All(p=>p.y<p.g-25));
  var l=Game.CommitLift(an); var b=Game.AnalyzeBuilding("restaurant",650,465); Console.WriteLine("bâtiment au-dessus : "+b.ok+" "+b.err);
}}
