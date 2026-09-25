"""Regression check against the supplied 2.54 ZIP; does not replace Unity Play tests."""
import sys,zipfile
from pathlib import Path
from tree_sitter import Language,Parser
import tree_sitter_c_sharp
root=Path(__file__).resolve().parents[1]
parser=Parser(Language(tree_sitter_c_sharp.language()))
files=list((root/'Assets').rglob('*.cs'))
errors=[str(f) for f in files if parser.parse(f.read_bytes()).root_node.has_error]
assert not errors,errors
print('PASS C# syntax:',len(files),'files; not compilation')
def methods(data):
 tree=parser.parse(data)
 result=[]
 def visit(n):
  if n.type=='method_declaration':result.append(data[n.start_byte:n.end_byte])
  for c in n.children:visit(c)
 visit(tree.root_node);return result
with zipfile.ZipFile(sys.argv[1]) as z:
 protected=['SkierRouteAgent.cs','StationVisitorRoadArrival248.cs','StationRoadConstruction249.cs','ManagedZoneConstructionMode.cs','PiouPiouConstructionMode.cs']
 for n in z.namelist():
  if n.endswith('/'):continue
  p=n.split('/',1)[1];b=z.read(n);actual=(root/p).read_bytes()
  if Path(p).name in protected:assert methods(b)==methods(actual),p
  if '/Arcade/' in p or p.startswith('Assets/Scenes/') or Path(p).name in ['LiftConstructionPlanner.cs','LiftVehicleRuntime.cs','VisibleSkiLoopAuthorityRuntime.cs','VisitorSkiAdmission.cs','ArrivalTicketOffice250.cs']:
   assert b==actual,p
 print('PASS simulation methods unchanged; arcade, scenes, lift placement, vehicles and admission unchanged')
print('Unity rendering, compilation, mouse input and Play still require testing.')
