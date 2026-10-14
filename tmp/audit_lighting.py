import re, pathlib, collections
def vec(s,key):
 m=re.search(r'^  '+key+r': \{x: ([^,]+), y: ([^,}]+)',s,re.M)
 return tuple(map(float,m.groups())) if m else (0,0)
def ref(s,key):
 m=re.search(r'^  '+key+r': \{fileID: (-?\d+)',s,re.M)
 return int(m[1]) if m else 0
for path in [pathlib.Path('Assets/01.Scenes/Kwanghee Test.unity'),pathlib.Path('Assets/01.Scenes/04.GameScene.unity')]:
 s=path.read_text(encoding='utf-8-sig')
 docs={int(m[2]):(int(m[1]),m[3]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)',s,re.M|re.S)}
 gos=collections.defaultdict(list)
 for id,(typ,d) in docs.items(): gos[ref(d,'m_GameObject')].append((id,typ,d))
 def name(go):
  m=re.search(r'^  m_Name: (.*)$',docs.get(go,(0,''))[1],re.M);return m[1] if m else str(go)
 def world(tid):
  if not tid or tid not in docs:return (0,0),(1,1)
  d=docs[tid][1];p=vec(d,'m_LocalPosition');sc=vec(d,'m_LocalScale');pp,ss=world(ref(d,'m_Father'))
  return (pp[0]+p[0]*ss[0],pp[1]+p[1]*ss[1]),(sc[0]*ss[0],sc[1]*ss[1])
 zones=[]; owners=collections.defaultdict(list)
 mgr=next(d for typ,d in docs.values() if 'guid: 982c88dcd9032b6408e043bf7a177a5c' in d)
 listed=set(map(int,re.findall(r'- \{fileID: (\d+)\}',mgr)))
 print('\nSCENE',path.name)
 for id,(typ,d) in docs.items():
  if 'guid: ff6de2f3bf08f3543bcf10dc7958a3f7' not in d:continue
  go=ref(d,'m_GameObject'); n=name(go)
  lights=list(map(int,re.findall(r'- \{fileID: (\d+)\}',d.split('_lights:')[1].split('_areaLightManager:')[0])))
  for lid in lights:owners[lid].append(n)
  print(id,n,'listed',id in listed,'manager',ref(d,'_areaLightManager'),'lights',len(lights),'missing',[x for x in lights if x not in docs])
  for cid,ct,cd in gos[go]:
   if ct not in (60,61):continue
   tid=next(t for t,tp,td in gos[go] if tp==4);p,sc=world(tid);off=vec(cd,'m_Offset')
   if ct==61:
    size=vec(cd,'m_Size');cx=p[0]+off[0]*sc[0];cy=p[1]+off[1]*sc[1];w=size[0]*sc[0];h=size[1]*sc[1];b=(cx-w/2,cy-h/2,cx+w/2,cy+h/2)
    print(' box',tuple(round(v,3) for v in b),'trigger',re.search(r'm_IsTrigger: (\d)',cd)[1]);zones.append((n,b))
   else:print(' polygon at',p)
 print('SHARED LIGHTS',[(lid,names) for lid,names in owners.items() if len(names)>1])
 print('OVERLAPPING BOXES')
 for i,(a,ab) in enumerate(zones):
  for b,bb in zones[i+1:]:
   w=min(ab[2],bb[2])-max(ab[0],bb[0]);h=min(ab[3],bb[3])-max(ab[1],bb[1])
   if w>0 and h>0:print(a,b,'overlap',round(w,3),round(h,3))
