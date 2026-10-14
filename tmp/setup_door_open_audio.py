from pathlib import Path
import re,uuid
pattern=r'^--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=^--- !u!|\Z)'
clip=Path('Assets/06.Sounds/UI/DoorOpen.mp3')
meta=clip.with_suffix('.mp3.meta')
if not meta.exists():
 sample=Path('Assets/06.Sounds/UI/LobbyBGM.wav.meta').read_text(encoding='utf-8-sig')
 sample=re.sub(r'^guid: .*','guid: '+uuid.uuid4().hex,sample,flags=re.M)
 meta.write_text(sample,encoding='utf-8')
guid=re.search(r'^guid: (\w+)',meta.read_text(encoding='utf-8-sig'),re.M)[1]
scene=Path('Assets/01.Scenes/04.GameScene.unity').read_text(encoding='utf-8-sig')
template=next(m[0] for m in re.finditer(pattern,scene,re.M|re.S) if m[1]=='82' and m[2]=='408124210')
backup=Path('tmp/door-open-audio-before');backup.mkdir(exist_ok=True)
for path in Path('Assets/03.Prefabs').rglob('*.prefab'):
 raw=path.read_bytes();s=raw.decode('utf-8-sig').replace('\r\n','\n')
 docs={int(m[2]):m[0] for m in re.finditer(pattern,s,re.M|re.S)}
 controllers=[d for d in docs.values() if 'guid: 59a07d0ac927db0439ee2b41691acaae' in d]
 if not controllers:continue
 (backup/path.name).write_bytes(raw)
 nextid=2026101509
 for controller in controllers:
  assert '_doorOpenAudioSource:' not in controller
  while nextid in docs:nextid+=1
  id=nextid;nextid+=1
  go=int(re.search(r'm_GameObject: \{fileID: (\d+)\}',controller)[1])
  go_doc=docs[go]
  new_go=go_doc.replace('  m_Component:\n',f'  m_Component:\n  - component: {{fileID: {id}}}\n',1)
  s=s.replace(go_doc,new_go,1)
  replacement=re.sub(r'(  _doorBlocker: \{fileID: \d+\}\n)',r'\g<1>'+f'  _doorOpenAudioSource: {{fileID: {id}}}\n',controller,count=1)
  s=s.replace(controller,replacement,1)
  audio=re.sub(r'^--- !u!82 &\d+',f'--- !u!82 &{id}',template)
  audio=re.sub(r'm_GameObject: \{fileID: \d+\}',f'm_GameObject: {{fileID: {go}}}',audio)
  audio=re.sub(r'm_Resource: \{[^}]+\}',f'm_Resource: {{fileID: 8300000, guid: {guid}, type: 3}}',audio)
  audio=re.sub(r'm_audioClip: \{[^}]+\}','m_audioClip: {fileID: 0}',audio)
  audio=re.sub(r'm_PlayOnAwake: \d+','m_PlayOnAwake: 0',audio)
  audio=re.sub(r'^  Loop: \d+','  Loop: 0',audio,flags=re.M)
  s=s.rstrip()+'\n'+audio
 ids=re.findall(r'^--- !u!\d+ &(-?\d+)',s,re.M);assert len(ids)==len(set(ids))
 assert s.count('_doorOpenAudioSource:')==len(controllers)
 if b'\r\n' in raw:s=s.replace('\n','\r\n')
 path.write_bytes((b'\xef\xbb\xbf' if raw.startswith(b'\xef\xbb\xbf') else b'')+s.encode('utf-8'))
 print(path.name,'DoorOpen connected; loop and play-on-awake disabled')
