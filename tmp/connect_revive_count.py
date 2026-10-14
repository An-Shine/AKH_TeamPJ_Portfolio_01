from pathlib import Path
import re
pattern=r'^--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=^--- !u!|\Z)'
targets=[('_gameOverTimeText','_gameOverReviveText',7836868431092773048,'1d8e8049c8018874d96f53d82e08fa17'),('_stageClearTimeText','_stageClearReviveText',6389107180127939612,'c88544376db20df47b68c8db2d2012f8')]
backup=Path('tmp/revive-count-before');backup.mkdir(exist_ok=True)
for path in Path('Assets/01.Scenes').glob('*.unity'):
 raw=path.read_bytes();s=raw.decode('utf-8-sig').replace('\r\n','\n')
 docs={int(m[2]):m[0] for m in re.finditer(pattern,s,re.M|re.S)}
 managers=[d for d in docs.values() if 'guid: d438496d13bc96c449127abab921b59e' in d]
 if not managers:continue
 before=s;added=[];nextid=2026101501
 for manager in managers:
  replacement=manager
  for old,new,source,guid in targets:
   assert new+':' not in replacement
   oldid=int(re.search(r'  '+old+r': \{fileID: (\d+)\}',manager)[1])
   textdoc=docs[oldid]
   instance=int(re.search(r'm_PrefabInstance: \{fileID: (\d+)\}',textdoc)[1])
   assert instance and guid in textdoc,(path,old)
   while nextid in docs:nextid+=1
   id=nextid;nextid+=1
   added.append(f'--- !u!114 &{id} stripped\nMonoBehaviour:\n  m_CorrespondingSourceObject: {{fileID: {source}, guid: {guid}, type: 3}}\n  m_PrefabInstance: {{fileID: {instance}}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: 0}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {{fileID: 11500000, guid: f4688fdb7df04437aeb418b961361dc5, type: 3}}\n  m_Name: \n  m_EditorClassIdentifier: \n')
   replacement=re.sub(r'(  '+old+r': \{fileID: \d+\}\n)',r'\g<1>'+f'  {new}: {{fileID: {id}}}\n',replacement,count=1)
  s=s.replace(manager,replacement,1)
 marker=re.search(r'^--- !u!1660057539 ',s,re.M)
 position=marker.start() if marker else len(s)
 s=s[:position]+''.join(added)+s[position:]
 ids=re.findall(r'^--- !u!\d+ &(-?\d+)',s,re.M);assert len(ids)==len(set(ids))
 (backup/path.name).write_bytes(raw)
 output=s.replace('\n','\r\n') if b'\r\n' in raw else s
 path.write_bytes((b'\xef\xbb\xbf' if raw.startswith(b'\xef\xbb\xbf') else b'')+output.encode('utf-8'))
 print(path.name,'connected both ReviveCount/Text references')
