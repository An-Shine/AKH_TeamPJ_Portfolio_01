from pathlib import Path
import re
import json
import hashlib
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Assets/01.Scenes/Kwanghee Test.unity'
TARGET = ROOT / 'Assets/01.Scenes/04.GameScene.unity'
HEADER = re.compile(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\s*$', re.M)

def parse(path):
    text = path.read_bytes().decode('utf-8-sig').replace('\r\n', '\n')
    matches = list(HEADER.finditer(text))
    blocks = {}
    for i, match in enumerate(matches):
        end = matches[i+1].start() if i+1 < len(matches) else len(text)
        key = int(match[2])
        assert key not in blocks, f'Duplicate fileID: {key}'
        blocks[key] = (int(match[1]), text[match.start():end])
    return text[:matches[0].start()], blocks

def ref(text, field):
    m = re.search(r'^  '+re.escape(field)+r': \{fileID: (-?\d+)', text, re.M)
    return int(m[1]) if m else None

def name(text):
    m = re.search(r'^  m_Name: (.+)$', text, re.M)
    return m[1] if m else None

def describe(path):
    _, blocks = parse(path)
    print('\nSCENE', path.name, 'blocks', len(blocks))
    for key, (kind, text) in blocks.items():
        n = name(text)
        if kind == 1 and n in ('Canvas', 'Main Camera', 'MapSystem', 'CameraSystem'):
            components = [int(x) for x in re.findall(r'component: \{fileID: (-?\d+)', text)]
            print(n, key, 'components', components)
            for component in components:
                ck, ct = blocks[component]
                if ck in (4, 224):
                    print(ct)
        if kind == 1001 and re.search(r'm_SourcePrefab:.*guid: (8ea71c8b51783054db4799ff60ebb042|12723f68de7cced449f926cb93a69d9f)', text):
            print('MAP UI INSTANCE', key, text)
        if kind == 114 and any(x in text for x in ('Assembly-CSharp::MapPlayerMarker', 'Assembly-CSharp::MinimapController', 'Assembly-CSharp::MapUIController', 'Assembly-CSharp::CameraFollow')):
            print('MAP/CAMERA COMPONENT', key, text)

def component_ids(text):
    return [int(x) for x in re.findall(r'component: \{fileID: (-?\d+)', text)]

def children(text):
    m = re.search(r'^  m_Children:(.*?)(?=^  \w|\Z)', text, re.M | re.S)
    return [int(x) for x in re.findall(r'fileID: (-?\d+)', m[1])] if m else []

def local_refs(text):
    return {int(x) for x in re.findall(r'\{fileID: (-?\d+)\}', text) if int(x) != 0}

def add_list_refs(text, field, additions):
    m = re.search(r'^  '+re.escape(field)+r':(?: \[\])?\n(?:  - \{fileID: -?\d+\}\n)*', text, re.M)
    assert m, f'Missing list {field}'
    current = [int(x) for x in re.findall(r'fileID: (-?\d+)', m[0])]
    current += [x for x in additions if x not in current]
    replacement = '  '+field+':\n' + ''.join(f'  - {{fileID: {x}}}\n' for x in current)
    return text[:m.start()] + replacement + text[m.end():]

def collect(source):
    # Copy hierarchy ownership, not arbitrary serialized references to the rest of the scene.
    selected = {527357422, 945682192, 449865803,
                3653195159723180327, 4168283088364540187}
    changed = True
    while changed:
        previous = set(selected)
        for key in list(selected):
            kind, text = source[key]
            if kind == 1:
                selected.update(component_ids(text))
            elif kind in (4, 224):
                owner = ref(text, 'm_GameObject')
                if owner:
                    selected.add(owner)
                selected.update(children(text))
            elif kind == 1001:
                selected.update(int(x) for x in re.findall(r'addedObject: \{fileID: (-?\d+)\}', text))
        for key, (kind, text) in source.items():
            if ref(text, 'm_PrefabInstance') in selected:
                selected.add(key)
            if kind in (4, 224) and ref(text, 'm_Father') in selected:
                selected.add(key)
        changed = selected != previous
    return selected

def transfer():
    source_bytes = SOURCE.read_bytes()
    before_bytes = TARGET.read_bytes()
    source_header, source = parse(SOURCE)
    target_header, target = parse(TARGET)
    selected = collect(source)
    camera_ids = {449865803, *component_ids(source[449865803][1])}
    assert 1409805389 in target
    assert source[1409805389] == target[1409805389], 'Player reference identity changed'
    # Parent placement must already match to retain the exact world coordinates.
    for parent in (1954974086, 2109030686):
        for field in ('m_LocalPosition', 'm_LocalRotation', 'm_LocalScale'):
            pattern = r'^  '+field+r':.*$'
            assert re.search(pattern, source[parent][1], re.M)[0] == re.search(pattern, target[parent][1], re.M)[0]
    collisions = selected & target.keys()
    for key in collisions - camera_ids:
        assert source[key] == target[key], f'Unrelated fileID collision: {key}'
    result = dict(target)
    for key in selected:
        result[key] = source[key]
    allowed_existing = set(camera_ids & target.keys())
    for parent, additions in ((1954974086, [527357423, 945682193]),
                              (2109030686, [23520264, 1312355624])):
        kind, text = result[parent]
        result[parent] = kind, add_list_refs(text, 'm_Children', additions)
        allowed_existing.add(parent)
    # Remove the old prefab-added child registration as well as changing camera's m_Father.
    player_instance = ref(target[1409805389][1], 'm_PrefabInstance')
    kind, text = result[player_instance]
    section = re.search(r'^    m_AddedGameObjects:(.*?)(?=^    m_AddedComponents:)', text, re.M | re.S)
    assert section
    entries = re.findall(r'^    - targetCorrespondingSourceObject:.*?(?=^    - targetCorrespondingSourceObject:|\Z)', section[1], re.M | re.S)
    retained = [entry for entry in entries if 'addedObject: {fileID: 449865806}' not in entry]
    assert len(retained) == len(entries)-1, 'Expected exactly one camera child registration'
    replacement = '    m_AddedGameObjects:' + ('\n'+''.join(retained) if retained else ' []\n')
    text = text[:section.start()] + replacement + text[section.end():]
    result[player_instance] = kind, text
    allowed_existing.add(player_instance)
    roots_id = 9223372036854775807
    kind, text = result[roots_id]
    result[roots_id] = kind, add_list_refs(text, 'm_Roots', [449865806])
    allowed_existing.add(roots_id)

    assert all(result[key] == source[key] for key in selected), 'Copied settings differ'
    changed_existing = {key for key in target if result[key] != target[key]}
    assert changed_existing <= allowed_existing, f'Unexpected mutations: {changed_existing-allowed_existing}'
    for key in selected:
        missing = local_refs(result[key][1]) - result.keys()
        assert not missing, f'Missing local references in {key}: {missing}'
    assert ref(result[449865806][1], 'm_Father') == 0
    assert 449865806 not in children(result[1409805389][1])
    follow = result[449865808][1]
    areas = re.search(r'  _cameraRoomAreas:(.*?)(?=^  _smoothTime:)', follow, re.M | re.S)[1]
    registered = [int(x) for x in re.findall(r'fileID: (-?\d+)', areas)]
    assert len(registered) == 9
    assert all('Assembly-CSharp::CameraRoomArea' in result[x][1] for x in registered)
    assert all(x in children(result[2109030686][1]) for x in (23520264, 1312355624))
    assert all(x in children(result[1954974086][1]) for x in (527357423, 945682193))

    # Keep pre-existing blocks in their original order and SceneRoots as the final block.
    order = [key for key in target if key != roots_id]
    order += [key for key in source if key in selected and key not in target]
    order.append(roots_id)
    text = target_header + ''.join(result[key][1] for key in order)
    newline = '\r\n' if b'\r\n' in before_bytes else '\n'
    output = text.replace('\n', newline).encode('utf-8')
    backup_dir = ROOT / 'tmp/scene_transfer'
    backup_dir.mkdir(parents=True, exist_ok=True)
    backup_path = backup_dir / '04.GameScene.before-map-camera.unity.bak'
    assert not backup_path.exists(), 'Backup exists; avoid overwriting recovery snapshot'
    backup_path.write_bytes(before_bytes)
    assert TARGET.read_bytes() == before_bytes, 'Game scene changed during transfer'
    assert SOURCE.read_bytes() == source_bytes, 'Test scene changed during transfer'
    TARGET.write_bytes(output)
    _, written = parse(TARGET)
    assert written == result
    assert SOURCE.read_bytes() == source_bytes
    report = {
        'source': str(SOURCE), 'target': str(TARGET), 'backup': str(backup_path),
        'source_sha256': hashlib.sha256(source_bytes).hexdigest(),
        'before_sha256': hashlib.sha256(before_bytes).hexdigest(),
        'after_sha256': hashlib.sha256(output).hexdigest(),
        'copied_blocks': len(selected), 'added_blocks': len(result)-len(target),
        'changed_existing_blocks': sorted(changed_existing),
        'camera_area_count': sum('Assembly-CSharp::CameraRoomArea' in result[key][1] for key in selected),
        'registered_camera_area_count': len(registered),
        'copied_file_ids': sorted(selected),
        'validation': 'Exact source equality, local reference resolution, hierarchy links and unrelated block preservation passed'
    }
    (backup_dir / 'map-camera-transfer-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps({k:v for k,v in report.items() if k != 'copied_file_ids'}, indent=2))

if __name__ == '__main__':
    if '--apply' in sys.argv:
        transfer()
    else:
        describe(SOURCE)
        describe(TARGET)
