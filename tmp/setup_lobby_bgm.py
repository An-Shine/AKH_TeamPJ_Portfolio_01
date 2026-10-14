from pathlib import Path
import re,uuid
script=Path('Assets/02.Scripts/Audio/LobbyBGMManager.cs')
guid=uuid.uuid4().hex
script.with_suffix('.cs.meta').write_text('fileFormatVersion: 2\nguid: '+guid+'\n',encoding='utf-8')
backup=Path('tmp/lobby-bgm-before');backup.mkdir(exist_ok=True)
init=Path('Assets/01.Scenes/00.InitSetting.unity');menu=Path('Assets/01.Scenes/03.MainMenu.unity')
raw_init=init.read_bytes();raw_menu=menu.read_bytes()
(backup/init.name).write_bytes(raw_init);(backup/menu.name).write_bytes(raw_menu)
s=raw_init.decode('utf-8-sig').replace('\r\n','\n');ms=raw_menu.decode('utf-8-sig').replace('\r\n','\n')
pattern=r'^--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=^--- !u!|\Z)'
docs={int(m[2]):m[0] for m in re.finditer(pattern,ms,re.M|re.S)}
audio=docs[636952312]
ids=[2026101505,2026101506,2026101507,2026101508]
assert all(not re.search(r'^--- !u!\d+ &'+str(i)+r'\b',s,re.M) for i in ids)
audio=re.sub(r'^--- !u!82 &\d+', '--- !u!82 &2026101507',audio)
audio=audio.replace('m_GameObject: {fileID: 636952311}','m_GameObject: {fileID: 2026101505}')
audio=audio.replace('m_PlayOnAwake: 1','m_PlayOnAwake: 0')
audio=re.sub(r'm_Resource: \{[^}]+\}','m_Resource: {fileID: 8300000, guid: d247e91f7b7cf934b8524118d44deb66, type: 3}',audio)
new='''--- !u!1 &2026101505
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 2026101506}
  - component: {fileID: 2026101507}
  - component: {fileID: 2026101508}
  m_Layer: 0
  m_Name: LobbyBGMManager
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &2026101506
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2026101505}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
'''+audio+'''--- !u!114 &2026101508
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2026101505}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: SCRIPTGUID, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::LobbyBGMManager
  _audioSource: {fileID: 2026101507}
'''.replace('SCRIPTGUID',guid)
marker=re.search(r'^--- !u!1660057539 ',s,re.M);assert marker
s=s[:marker.start()]+new+s[marker.start():]
s=s.replace('  _backgroundCanvas: {fileID: 1335550950}\n','  _backgroundCanvas: {fileID: 1335550950}\n  _lobbyBGMManager: {fileID: 2026101508}\n',1)
assert '_lobbyBGMManager:' in s
s=s.rstrip()+'\n  - {fileID: 2026101506}\n'
# Keep the old source available but prevent its automatic playback.
old=docs[636952312];disabled=old.replace('m_PlayOnAwake: 1','m_PlayOnAwake: 0')
ms=ms.replace(old,disabled,1)
for path,text,raw in [(init,s,raw_init),(menu,ms,raw_menu)]:
 ids=re.findall(r'^--- !u!\d+ &(-?\d+)',text,re.M);assert len(ids)==len(set(ids))
 if b'\r\n' in raw:text=text.replace('\n','\r\n')
 path.write_bytes((b'\xef\xbb\xbf' if raw.startswith(b'\xef\xbb\xbf') else b'')+text.encode('utf-8'))
print('InitSetting: persistent lobby manager, LobbyBGM.wav, SceneFlow reference connected.')
print('MainMenu: old Background Music Play On Awake disabled.')
