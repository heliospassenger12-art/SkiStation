"""Offline regression and layout checks; Unity is still required for Play and rendering."""
from pathlib import Path
import sys,zipfile,math
from tree_sitter import Language,Parser
import tree_sitter_c_sharp
r=Path(__file__).resolve().parents[1]
p=Parser(Language(tree_sitter_c_sharp.language()))
fs=list((r/'Assets').rglob('*.cs'))
assert not [str(f) for f in fs if p.parse(f.read_bytes()).root_node.has_error]
print('PASS syntax:',len(fs),'C# files (not compilation)')
hook='\n            ArrivalWelcomeVisuals256.Install(this);'
with zipfile.ZipFile(sys.argv[1]) as z:
 for n in z.namelist():
  if n.endswith('/'):continue
  rel=n.split('/',1)[1];b=z.read(n);a=(r/rel).read_bytes()
  if rel.endswith('/ArrivalHub250.cs'):
   assert a.decode().replace(hook,'')==b.decode(),rel
  else:assert a==b,rel
print('PASS all 2.55 files unchanged except one visual-install call in hub Build')
# Shelter and signs expressed in the hub's local ground coordinates.
rects=[('shelter',92.775,103.225,-52.225,-47.775),('cars sign',102.45,109.55,23.875,24.125),('bus sign',77.95,86.05,-51.125,-50.875),('welcome sign',13.45,18.55,27.875,28.125)]
def hit(x,z,b,pad=0):return b[1]-pad<=x<=b[2]+pad and b[3]-pad<=z<=b[4]+pad
paths=[]
for bus,count in [(False,12),(True,3)]:
 for i in range(count):
  x=42+i*15 if bus else 38+i*5.5;z=-37 if bus else 12
  paths.append(('vehicle',[(24,0),(24,-12),(x,-12),(x,z)],7.5 if bus else 3))
  sx=x+(-1.7 if bus else 1.5);sz=-47 if bus else 22
  paths.append(('pedestrian',[(sx,z),(sx,sz),(18,sz),(18,4),(10,4),(0,0)],.25))
paths.append(('office',[(0,0),(10,4),(18,4),(18,23),(25,24)],.25))
for label,path,pad in paths:
 for a,b in zip(path,path[1:]):
  for step in range(201):
   t=step/200;x=a[0]+(b[0]-a[0])*t;z=a[1]+(b[1]-a[1])*t
   for box in rects:assert not hit(x,z,box,pad),(label,box[0],x,z)
print('PASS shelter/sign envelopes outside sampled arrival and parking paths')
print('Unity compilation, visuals, terrain contact and performance are NOT validated here.')
