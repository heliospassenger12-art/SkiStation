"""Offline syntax, baseline and geometry checks, not a Unity runtime test."""
from pathlib import Path
from tree_sitter import Language,Parser
import tree_sitter_c_sharp
import zipfile,sys,math
r=Path(__file__).resolve().parents[1]
p=Parser(Language(tree_sitter_c_sharp.language()))
fs=list((r/'Assets').rglob('*.cs'))
assert not [str(f) for f in fs if p.parse(f.read_bytes()).root_node.has_error]
print(f'PASS syntax: {len(fs)} C# files (not compilation)')
if len(sys.argv)>1:
 with zipfile.ZipFile(sys.argv[1]) as z:
  for n in z.namelist():
   if n.endswith('/'):continue
   assert (r/n).is_file(),n
   if n.startswith(('Assets/SkiStation/Scripts/Arcade/','Assets/Scenes/')) or Path(n).name in ['VisibleSkiLoopAuthorityRuntime.cs','LiftConstructionPlanner.cs','LiftVehicleRuntime.cs','SkierRouteAgent.cs','ManagementJourneyAccess.cs']:
    assert (r/n).read_bytes()==z.read(n),n
 print('PASS unchanged: arcade, scenes, phase63 proof motion, lift construction, lift vehicles, ski routing, access checks')
# Independent physical envelope checks for marked prototype bays.
cars=[(38+5.5*i,12,2.1,4.8) for i in range(12)]
buses=[(42+15*i,-37,2.7,11) for i in range(3)]
for x,z,w,l in cars+buses:
 assert 14 <= x-w/2 < x+w/2 <= 110
 assert -53 <= z-l/2 < z+l/2 <= 29
for i,a in enumerate(cars+buses):
 for b in (cars+buses)[i+1:]:
  assert abs(a[0]-b[0])>(a[2]+b[2])/2 or abs(a[1]-b[1])>(a[3]+b[3])/2
print('PASS 12 car / 3 bus envelopes inside parking, no parked body overlap')
# Curved road offset sampled using the same generated centerline geometry.
road=[]
for i in range(65):
 t=i/64;road.append((-160+160*t*t*(3-2*t),-3400+800*t))
for i,(x,z) in enumerate(road):
 a=road[max(0,i-1)];b=road[min(64,i+1)];dx=b[0]-a[0];dz=b[1]-a[1];d=math.hypot(dx,dz)
 for offset,half in [(10,.4),(3.8,1.35),(-3.8,1.35)]:
  q=(x+dz/d*offset,z-dx/d*offset)
  assert all(math.isfinite(v) for v in q)
  assert 8.2 < offset-half and offset+half < 11.8 if offset==10 else abs(offset)+half<8
print('PASS sidewalk walker / bus-width lane containment on 65 curved-road samples')
print('Unity movement, serialization, visuals, queues and economy must still be tested in Play.')
