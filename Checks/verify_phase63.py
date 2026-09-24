"""Static checks only; not Unity compilation or Play. Pass original phase63 ZIP as argument."""
from pathlib import Path
import sys, zipfile
from tree_sitter import Language, Parser
import tree_sitter_c_sharp
root=Path(__file__).resolve().parents[1]
parser=Parser(Language(tree_sitter_c_sharp.language()))
files=list((root/'Assets').rglob('*.cs'))
errors=[str(p.relative_to(root)) for p in files if parser.parse(p.read_bytes()).root_node.has_error]
assert not errors, errors
print(f'PASS: C# syntax, {len(files)} files. Not a Unity compilation.')
if len(sys.argv)>1:
 with zipfile.ZipFile(sys.argv[1]) as z:
  changed=[]
  for f in z.namelist():
   if f.endswith('/'): continue
   p=root/f
   assert p.is_file(), f'Missing baseline file: {f}'
   if p.read_bytes()!=z.read(f): changed.append(f)
  allowed={'StationVisitorDemandRuntime.cs','StationVisitorRoadArrival248.cs','SkierRouteAgent.cs','LiftQueueRuntime.cs','LiftStationLogic.cs','LiftVehicleRuntime.cs','LiftPhysicalPassengerFlowRuntime.cs','VisibleSkiLoopAuthorityRuntime.cs','VisitorLifecycleRuntime.cs','SkierMountainTransferRuntime.cs','SkierDiagnosticTrackerRuntime.cs','SkierRuntime.cs','LiftPostArrivalRoutingRuntime.cs','SkierTerminalDecisionRuntime.cs'}
  assert all(Path(f).name in allowed for f in changed),changed
  print('PASS: baseline preserved outside reviewed runtime files; arcade, station placement, terrain and scenes byte-identical.')
  name='Assets/SkiStation/Scripts/Playable/VisibleSkiLoopAuthorityRuntime.cs'
  old=z.read(name).decode();new=(root/name).read_text()
  # Phase63 physical trajectory implementations must not be rewritten.
  def method(s,n):
   start=s.index('private void '+n+'(');brace=s.index('{',start);depth=1;i=brace+1
   while depth:
    depth+=(s[i]=='{')-(s[i]=='}');i+=1
   return s[start:i]
  for n in ('MoveToBoarding','MoveLinear','MoveWaypointByWaypoint'):
   assert method(old,n)==method(new,n),n
  print('PASS: phase63 boarding approach and lift/slope motion methods unchanged.')
  print('Modified original files:')
  print('\n'.join(changed))
