from pathlib import Path
import re, uuid
root=Path('.')
script=root/'Assets/02.Scripts/YSortedObstacle.cs'
guid=uuid.uuid4().hex
script.with_suffix('.cs.meta').write_text('fileFormatVersion: 2\nguid: '+guid+'\n',encoding='utf-8')
player=(root/'Assets/03.Prefabs/Player/PlayerObject.prefab').read_text(encoding='utf-8-sig')
docs=[(int(m[1]),m[2]) for m in re.finditer(r'^--- !u!(\d+) &[^\n]+\n(.*?)(?=^--- !u!|\Z)',player,re.M|re.S)]
sprite=next(d for t,d in docs if t==212)
collider=next(d for t,d in docs if t==61 and 'm_IsTrigger: 0' in d)
scene=(root/'Assets/01.Scenes/Kwanghee Test.unity').read_text(encoding='utf-8-sig')
tile=re.search(r'^TilemapRenderer:.*?(?=^--- !u!)',scene,re.M|re.S)[0]
material=re.search(r'm_Materials:\n  - (.*)',tile)[1]
chunks=['%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n']
def doc(t,id,body):chunks.append(f'--- !u!{t} &{id}\n'+body)
def go(id,name,components):
 doc(1,id,'GameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  serializedVersion: 6\n  m_Component:\n'+''.join(f'  - component: {{fileID: {c}}}\n' for c in components)+f'  m_Layer: 8\n  m_Name: {name}\n  m_TagString: Untagged\n  m_Icon: {{fileID: 0}}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n')
def transform(id,gid,parent,x,y,children=()):
 doc(4,id,f'Transform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: {gid}}}\n  serializedVersion: 2\n  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}\n  m_LocalPosition: {{x: {x}, y: {y}, z: 0}}\n  m_LocalScale: {{x: 1, y: 1, z: 1}}\n  m_ConstrainProportionsScale: 0\n'+('  m_Children:\n'+''.join(f'  - {{fileID: {c}}}\n' for c in children) if children else '  m_Children: []\n')+f'  m_Father: {{fileID: {parent}}}\n  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}\n')
go(1000,'BaseCornerWall_YSortExample',[1001,1002,1003,1004])
transform(1001,1000,0,0,0,[2001]+[3001+i*10 for i in range(12)])
def box(id,x,y,w,h):
 d=re.sub(r'm_GameObject: \{fileID: \d+\}', 'm_GameObject: {fileID: 1000}',collider)
 d=re.sub(r'm_Offset: \{[^}]+\}',f'm_Offset: {{x: {x}, y: {y}}}',d)
 d=re.sub(r'm_Size: \{[^}]+\}',f'm_Size: {{x: {w}, y: {h}}}',d)
 doc(61,id,d)
box(1003,0,0.22,2.9,0.44)
box(1004,-1.35,2.16,0.3,3.44)
go(2000,'SortPoint',[2001]);transform(2001,2000,1001,0,0.22)
doc(114,1002,f'MonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: 1000}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}\n  m_Name: \n  m_EditorClassIdentifier: Assembly-CSharp::YSortedObstacle\n  _sortPoint: {{fileID: 2001}}\n  _renderers:\n'+''.join(f'  - {{fileID: {3002+i*10}}}\n' for i in range(12))+'  _sortingLayerName: Player\n  _behindPlayerOrder: -10\n  _inFrontOfPlayerOrder: 10\n')
for i in range(12):
 gid=3000+i*10;tid=gid+1;rid=gid+2;row,col=divmod(i,3)
 go(gid,f'Wall_r{row}_c{col}',[tid,rid]);transform(tid,gid,1001,col-1,3.5-row)
 d=re.sub(r'm_GameObject: \{fileID: \d+\}',f'm_GameObject: {{fileID: {gid}}}',sprite)
 d=re.sub(r'm_Materials:\n  - .*',f'm_Materials:\n  - {material}',d)
 d=re.sub(r'm_Sprite: \{[^}]+\}',f'm_Sprite: {{fileID: {21300000+i*2}, guid: 746e4b1a0d9a45de83295740e6a74976, type: 3}}',d)
 d=re.sub(r'm_SortingLayerID: -?\d+','m_SortingLayerID: 68427715',d)
 d=re.sub(r'm_SortingLayer: -?\d+','m_SortingLayer: 4',d)
 d=re.sub(r'm_SortingOrder: -?\d+','m_SortingOrder: 10',d)
 d=re.sub(r'm_Size: \{[^}]+\}','m_Size: {x: 1, y: 1}',d)
 doc(212,rid,d)
out=root/'Assets/03.Prefabs/Etc/BaseCornerWall_YSortExample.prefab'
out.write_text(''.join(chunks),encoding='utf-8')
out.with_suffix('.prefab.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nPrefabImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
text=out.read_text(); ids=re.findall(r'^--- !u!\d+ &(\d+)',text,re.M)
assert len(ids)==len(set(ids))
assert len(re.findall(r'^SpriteRenderer:',text,re.M))==12
assert all(x in set(ids) for x in re.findall(r'\{fileID: (\d+)\}',text) if x!='0')
print('Created prefab: 12 original sprite slices, 2 colliders, sort point, lit material',material)
