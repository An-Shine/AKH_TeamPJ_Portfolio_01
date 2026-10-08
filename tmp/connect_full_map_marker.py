from pathlib import Path
import hashlib
import re
from transfer_map_camera import ROOT, TARGET, parse, local_refs, ref

original = TARGET.read_bytes()
header, before = parse(TARGET)
result = dict(before)
component = 527357426
content = 23520265
marker = 23520266
assert not {component, content, marker} & before.keys()
instance = 3653195159723180327
prefab_guid = '8ea71c8b51783054db4799ff60ebb042'
prefab = parse(ROOT / 'Assets/03.Prefabs/UI/FullMapUI.prefab')[1]
assert prefab[4822427732026517447][0] == 224
assert prefab[655481399628341986][0] == 224
for key, asset_key in ((content, 4822427732026517447), (marker, 655481399628341986)):
    result[key] = (224, f'''--- !u!224 &{key} stripped
RectTransform:
  m_CorrespondingSourceObject: {{fileID: {asset_key}, guid: {prefab_guid}, type: 3}}
  m_PrefabInstance: {{fileID: {instance}}}
  m_PrefabAsset: {{fileID: 0}}
''')
result[component] = (114, f'''--- !u!114 &{component}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 527357422}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 7dc9b5d5eea057843ad4ffc1dd1bce57, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::MapPlayerMarker
  _player: {{fileID: 1409805389}}
  _mapBottomLeft: {{fileID: 368899313}}
  _mapTopRight: {{fileID: 1782376228}}
  _mapContent: {{fileID: {content}}}
  _playerMarker: {{fileID: {marker}}}
''')
kind, text = result[527357422]
text = text.replace('  - component: {fileID: 527357424}\n',
                    f'  - component: {{fileID: 527357424}}\n  - component: {{fileID: {component}}}\n')
assert text != result[527357422][1]
result[527357422] = kind, text
for key in (component, content, marker):
    assert not local_refs(result[key][1]) - result.keys()
assert all(result[key] == before[key] for key in before if key != 527357422)
backup = ROOT / 'tmp/scene_transfer/04.GameScene.before-full-map-marker.unity.bak'
assert not backup.exists()
backup.write_bytes(original)
roots = 9223372036854775807
order = [key for key in before if key != roots] + [content, marker, component, roots]
newline = '\r\n' if b'\r\n' in original else '\n'
output = (header + ''.join(result[key][1] for key in order)).replace('\n', newline).encode('utf-8')
assert TARGET.read_bytes() == original
TARGET.write_bytes(output)
assert parse(TARGET)[1] == result
print('Full-map marker connected: MapSystem -> GameScene Player, map bounds, FullMapUI/MapContent, FullMapUI/PlayerMarker')
print('All other existing scene blocks unchanged; all local references resolve.')
