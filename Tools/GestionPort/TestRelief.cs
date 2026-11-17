using System; using System.IO; using SkiStation.Gestion;
class T { static void Main(string[] a) {
  var sw = System.Diagnostics.Stopwatch.StartNew(); var r = new Relief(); r.Init(int.Parse(a[0])); var ms = sw.ElapsedMilliseconds; r.GenTrees();
  var js = File.ReadAllBytes(a[1]); double maxd = 0; int at = 0;
  for (int i = 0; i < r.H.Length; i++) { float v = BitConverter.ToSingle(js, i * 4); double d = Math.Abs(v - r.H[i]); if (d > maxd) { maxd = d; at = i; } }
  var rng = new Rng(1337); Console.WriteLine("rng " + rng.Next().ToString("R") + " " + rng.Next().ToString("R"));
  Console.WriteLine("seed " + a[0] + " init " + ms + " ms | min " + r.MinH.ToString("0.00") + " max " + r.MaxH.ToString("0.00") + " | écart max avec le web " + maxd.ToString("0.000000") + " m (cellule " + at + ") | sapins " + r.Trees.Count + " | H(0,1000)=" + r.GetH(0,1000).ToString("0.000") + " pente " + r.Slope(0,1000).ToString("0.0000"));
  var m = r.ReachMask(); int n = 0; foreach (var b in m) n += b; Console.WriteLine("zones desservables " + n);
} }
