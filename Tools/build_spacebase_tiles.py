"""Draw native 32px tiles; no generated-image cropping or rescaling.

Run with Python + Pillow. Only the previews are enlarged.
"""
from pathlib import Path
import hashlib
import json
import math
import random
import uuid
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/04.Sprites/Tilemaps/Map'
PREVIEW = ROOT / 'tools/Previews/SpaceBase32'
T = 32
RIM = 6
CLEAR = (0, 0, 0, 0)
INK = (12, 19, 26, 255)
PROFILE = [(13, 21, 29, 255), (103, 116, 127, 255),
           (66, 80, 94, 255), (66, 80, 94, 255),
           (103, 116, 127, 255), (13, 21, 29, 255)]
NAMESPACE = uuid.UUID('1c6fba84-57a1-4576-b75a-ec0603de9460')


def image(w=32, h=32):
    return Image.new('RGBA', (w, h), CLEAR)


def bolt(d, x, y):
    d.rectangle((x, y, x+1, y+1), fill=INK)
    d.point((x, y), fill=(119, 130, 139, 255))


def panel(d, box, fill=(30, 44, 57, 255)):
    x0, y0, x1, y1 = box
    d.rectangle(box, fill=INK)
    d.rectangle((x0+1, y0+1, x1-1, y1-1), fill=fill)
    d.line((x0+1, y0+1, x1-1, y0+1), fill=(59, 74, 85, 255))
    d.line((x0+1, y0+1, x0+1, y1-1), fill=(46, 62, 76, 255))
    d.line((x0+1, y1-1, x1-1, y1-1), fill=(18, 29, 40, 255))


def wear(im, box, count, seed, base):
    rng = random.Random(seed)
    d = ImageDraw.Draw(im)
    for _ in range(count):
        x, y = rng.randint(box[0], box[2]), rng.randint(box[1], box[3])
        delta = rng.choice([-9, -6, 5, 8])
        col = tuple(max(0, min(255, v+delta)) for v in base[:3])+(255,)
        d.point((x, y), fill=col)
        if rng.random() < .17 and x < box[2]:
            d.point((x+1, y), fill=col)


def floor(grate=False):
    im = image()
    d = ImageDraw.Draw(im)
    # Identical, opaque boundary profiles on all floor variants.
    d.rectangle((0, 0, 31, 31), fill=(39, 51, 64, 255))
    d.rectangle((1, 1, 30, 30), fill=(53, 68, 83, 255))
    wear(im, (3, 3, 28, 28), 110, 901, (53, 68, 83))
    d.line((2, 2, 29, 2), fill=(69, 82, 94, 255))
    d.line((2, 2, 2, 29), fill=(62, 78, 90, 255))
    d.line((3, 29, 29, 29), fill=(29, 42, 54, 255))
    d.line((29, 3, 29, 29), fill=(29, 42, 54, 255))
    for x, y in [(3, 3), (27, 3), (3, 27), (27, 27)]:
        bolt(d, x, y)
    d.line((9, 21, 12, 20), fill=(66, 79, 87, 255))
    d.point((13, 19), fill=(70, 81, 89, 255))
    if grate:
        panel(d, (7, 7, 24, 24), (18, 28, 37, 255))
        d.rectangle((10, 10, 21, 21), fill=(8, 16, 23, 255))
        for x in range(10, 22, 3):
            d.line((x, 10, x, 21), fill=(83, 98, 108, 255))
            d.line((x+1, 10, x+1, 21), fill=(41, 57, 69, 255))
        d.line((9, 22, 22, 22), fill=(14, 24, 33, 255))
    return im


def dust_set():
    im = image(64, 64)
    for y in [0, 32]:
        for x in [0, 32]:
            im.paste(floor(), (x, y))
    d = ImageDraw.Draw(im)
    rng = random.Random(664)
    sand = [(98, 88, 68, 255), (124, 107, 76, 255),
            (145, 122, 82, 255), (165, 138, 94, 255)]
    for y in range(3, 61):
        for x in range(3, 61):
            # Leave outer panel seams untouched, including on the 2x2 joins.
            if x % 32 < 3 or x % 32 > 28 or y % 32 < 3 or y % 32 > 28:
                continue
            dist = ((x-56)/27)**2 + ((y-7)/32)**2
            trail = math.exp(-((x-(58-y*.43))/8)**2) * math.exp(-y/65)
            if rng.random() < .45*math.exp(-dist) + .15*trail:
                d.point((x, y), fill=rng.choice(sand))
    for x, y in [(53, 12), (45, 22), (54, 35)]:
        d.rectangle((x, y+1, x+3, y+3), fill=(26, 32, 36, 255))
        d.rectangle((x, y, x+2, y+2), fill=(109, 105, 85, 255))
        d.line((x, y, x+2, y), fill=(149, 140, 109, 255))
    return im


def north(kind):
    im = image(32, 96)
    d = ImageDraw.Draw(im)
    # The shared y profile is the connection contract, at x=0 and x=31.
    for y in range(96):
        col = (25, 38, 50, 255)
        if y < 6:
            col = PROFILE[y]
        elif y in (6, 7, 10, 89, 94):
            col = (12, 22, 31, 255)
        elif y in (8, 90, 93):
            col = (63, 79, 91, 255)
        elif y == 95:
            col = (16, 26, 35, 255)
        d.line((0, y, 31, y), fill=col)
    panel(d, (3, 13, 28, 87))
    wear(im, (6, 17, 25, 83), 98, 23, (30, 44, 57))
    for y in (16, 82):
        bolt(d, 5, y)
        bolt(d, 25, y)
    if kind == 'Light':
        panel(d, (7, 20, 24, 27), (10, 36, 49, 255))
        d.rectangle((9, 22, 22, 24), fill=(31, 178, 204, 255))
        d.line((10, 22, 21, 22), fill=(200, 251, 254, 255))
        d.line((10, 25, 21, 25), fill=(14, 79, 100, 255))
        panel(d, (8, 37, 23, 69), (29, 43, 54, 255))
        for y in (75, 78):
            d.line((10, y, 21, y), fill=(8, 17, 26, 255))
            d.line((10, y+1, 21, y+1), fill=(53, 68, 78, 255))
    elif kind == 'Electrical':
        d.rectangle((7, 19, 24, 79), fill=(11, 23, 32, 255))
        for i, x in enumerate((9, 13, 18, 22)):
            col = [(81, 74, 61, 255), (62, 81, 94, 255),
                   (48, 69, 84, 255), (83, 54, 42, 255)][i]
            d.line([(x, 20), (x, 34+i*2), (x-1, 35+i*2), (x-1, 77)], fill=col)
            d.point((x, 23), fill=(131, 134, 123, 255))
        for y in (29, 72):
            d.rectangle((8, y, 23, y+2), fill=(57, 71, 82, 255))
            bolt(d, 10, y)
        panel(d, (11, 43, 25, 66), (57, 70, 82, 255))
        for y in (47, 50, 53):
            d.line((15, y, 22, y), fill=(16, 28, 38, 255))
        d.rectangle((15, 58, 21, 59), fill=(62, 200, 213, 255))
        d.point((23, 62), fill=(139, 116, 70, 255))
    else:
        d.rectangle((12, 12, 19, 88), fill=(12, 22, 30, 255))
        d.rectangle((13, 12, 18, 88), fill=(75, 89, 101, 255))
        d.line((14, 13, 14, 87), fill=(114, 124, 132, 255))
        d.rectangle((14, 32, 17, 35), fill=(45, 60, 73, 255))
        d.rectangle((14, 63, 17, 66), fill=(45, 60, 73, 255))
        for y in (18, 46, 78):
            bolt(d, 15, y)
    return im


def side(pattern=False, east=False):
    im = image()
    d = ImageDraw.Draw(im)
    for x in range(RIM):
        d.line((x, 0, x, 31), fill=PROFILE[x])
    if pattern:
        for y in range(10, 23, 3):
            d.line((2, y, 3, y), fill=(14, 25, 35, 255))
            d.line((2, y+1, 3, y+1), fill=(94, 108, 118, 255))
    else:
        d.point((2, 10), fill=(84, 96, 106, 255))
        d.point((3, 23), fill=(49, 66, 78, 255))
    return im.transpose(Image.Transpose.FLIP_LEFT_RIGHT) if east else im


def south():
    im = image()
    d = ImageDraw.Draw(im)
    for k in range(RIM):
        d.line((0, 26+k, 31, 26+k), fill=PROFILE[k])
    d.point((10, 28), fill=(87, 100, 112, 255))
    return im


def lower_corner(east=False):
    im = side()
    im.alpha_composite(south())
    d = ImageDraw.Draw(im)
    # All corner outlines use only horizontal and vertical segments.
    d.rectangle((0, 26, 5, 31), fill=PROFILE[2])
    d.line((0, 26, 0, 31), fill=PROFILE[0])
    d.line((0, 31, 5, 31), fill=PROFILE[0])
    d.line((1, 27, 1, 30), fill=PROFILE[1])
    d.line((1, 30, 4, 30), fill=PROFILE[1])
    d.line((5, 26, 5, 26), fill=PROFILE[0])
    if east:
        return im.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    return im


def upper_corner(east=False):
    im = north('Divider')
    d = ImageDraw.Draw(im)
    # Clear centered rib, add a quiet inset and a flush square end column.
    panel(d, (7, 13, 28, 87), (27, 41, 54, 255))
    wear(im, (10, 17, 25, 83), 55, 76, (27, 41, 54))
    for x in range(RIM):
        d.line((x, 6, x, 95), fill=PROFILE[x])
    # Square cap, no bevel across the silhouette.
    d.rectangle((0, 0, 5, 5), fill=PROFILE[2])
    d.line((0, 0, 5, 0), fill=PROFILE[0])
    d.line((0, 0, 0, 5), fill=PROFILE[0])
    d.line((1, 1, 4, 1), fill=PROFILE[1])
    d.line((1, 1, 1, 4), fill=PROFILE[1])
    for y in (25, 57, 83):
        d.point((2, y), fill=(106, 118, 128, 255))
    return im.transpose(Image.Transpose.FLIP_LEFT_RIGHT) if east else im


def uid(name):
    return uuid.uuid5(NAMESPACE, name).hex


def sprite_id(name):
    return int(hashlib.sha256(name.encode()).hexdigest()[:15], 16)


def write_meta(path, cells, height):
    # Match this project's TextureImporter schema; never reuse a draft GUID.
    template = (OUT / 'SpaceBase_Tileset_Draft_v2.png.meta').read_text(encoding='utf-8')
    settings = template[template.index('  externalObjects:'):template.index('  spriteSheet:')]
    settings = settings.replace('  spriteGenerateFallbackPhysicsShape: 1', '  spriteGenerateFallbackPhysicsShape: 0')
    settings = settings.replace('    textureCompression: 1', '    textureCompression: 0')
    result = ['fileFormatVersion: 2', f'guid: {uid(path.name)}', 'TextureImporter:', '  internalIDToNameTable:']
    for name, col, row in cells:
        result += ['  - first:', f'      213: {sprite_id(name)}', f'    second: {name}']
    result.append(settings.rstrip())
    result += ['  spriteSheet:', '    serializedVersion: 2', '    sprites:']
    for name, col, row in cells:
        result += ['    - serializedVersion: 2', f'      name: {name}', '      rect:',
                   '        serializedVersion: 2', f'        x: {col*T}',
                   f'        y: {height-(row+1)*T}', '        width: 32', '        height: 32',
                   '      alignment: 0', '      pivot: {x: 0.5, y: 0.5}',
                   '      border: {x: 0, y: 0, z: 0, w: 0}', '      customData: ',
                   '      outline: []', '      physicsShape: []', '      tessellationDetail: -1',
                   '      bones: []', f'      spriteID: {uid(name)}', f'      internalID: {sprite_id(name)}',
                   '      vertices: []', '      indices: ', '      edges: []', '      weights: []']
    result += ['    outline: []', '    customData: ', '    physicsShape: []', '    bones: []',
               f'    spriteID: {uid(path.name + "/sheet")}', '    internalID: 0', '    vertices: []',
               '    indices: ', '    edges: []', '    weights: []', '    secondaryTextures: []',
               '    spriteCustomMetadata:', '      entries: []', '    nameFileIdTable:']
    result += [f'      {name}: {sprite_id(name)}' for name, _, _ in cells]
    result += ['  mipmapLimitGroupName: ', '  pSDRemoveMatte: 0', '  userData: Native32_NoPadding',
               '  assetBundleName: ', '  assetBundleVariant: ']
    path.with_suffix('.png.meta').write_text('\n'.join(result)+'\n', encoding='utf-8')


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    atlas = image(256, 160)
    cells = []
    parts = {}

    def put(name, im, col, row):
        assert im.width % 32 == 0 and im.height % 32 == 0
        atlas.paste(im, (col*T, row*T))
        parts[name] = im
        for dy in range(im.height//T):
            for dx in range(im.width//T):
                suffix = f'_{dy}_{dx}' if im.size != (T, T) else ''
                cells.append((name+suffix, col+dx, row+dy))

    for i, kind in enumerate(('Light', 'Electrical', 'Divider')):
        put('North_'+kind, north(kind), i, 0)
    put('Corner_NW', upper_corner(), 3, 0)
    put('Corner_NE', upper_corner(True), 4, 0)
    put('Floor_Plain', floor(), 0, 3)
    put('Floor_Drain', floor(True), 1, 3)
    put('Floor_Dust', dust_set(), 2, 3)
    put('West_Plain', side(), 4, 3)
    put('West_Vent', side(True), 5, 3)
    put('Corner_SW', lower_corner(), 6, 3)
    put('Corner_SE', lower_corner(True), 7, 3)
    put('East_Plain', side(east=True), 4, 4)
    put('East_Vent', side(True, True), 5, 4)
    put('South_Plain', south(), 6, 4)

    # Contract tests: actual alpha and boundary pixels, not approximate image sizes.
    assert len(cells) == len({(x, y) for _, x, y in cells}) == 28
    assert atlas.size == (256, 160)
    for name in ('North_Light', 'North_Electrical', 'North_Divider', 'Corner_NW', 'Corner_NE'):
        assert parts[name].size == (32, 96)
        assert parts[name].getchannel('A').getextrema() == (255, 255)
    for a in ('North_Light', 'North_Electrical', 'North_Divider'):
        for b in ('North_Light', 'North_Electrical', 'North_Divider'):
            assert [parts[a].getpixel((31,y)) for y in range(96)] == [parts[b].getpixel((0,y)) for y in range(96)]
    for corner, x, edge in [('Corner_NW',31,0), ('Corner_NE',0,31)]:
        assert [parts[corner].getpixel((x,y)) for y in range(96)] == [parts['North_Light'].getpixel((edge,y)) for y in range(96)]
    for corner, side_name, x in [('Corner_NW','West_Plain',0), ('Corner_NE','East_Plain',26)]:
        assert [parts[corner].getpixel((i,95)) for i in range(x,x+6)] == [parts[side_name].getpixel((i,0)) for i in range(x,x+6)]
    for name in ('West_Plain','West_Vent','East_Plain','East_Vent'):
        assert [parts[name].getpixel((x,0)) for x in range(32)] == [parts[name].getpixel((x,31)) for x in range(32)]
    for corner, side_name in [('Corner_SW','West_Plain'),('Corner_SE','East_Plain')]:
        im = parts[corner]
        assert [im.getpixel((x,0)) for x in range(32)] == [parts[side_name].getpixel((x,31)) for x in range(32)]
        join_x = 31 if corner.endswith('SW') else 0
        assert [im.getpixel((join_x,y)) for y in range(32)] == [parts['South_Plain'].getpixel((0,y)) for y in range(32)]
        for y in range(32):
            for x in range(32):
                wall = (x < 6 if corner.endswith('SW') else x >= 26) or y >= 26
                assert im.getpixel((x,y))[3] == (255 if wall else 0)
    for name in ('Floor_Plain','Floor_Drain','Floor_Dust'):
        assert parts[name].getchannel('A').getextrema() == (255,255)
    floor_pieces = [parts['Floor_Plain'], parts['Floor_Drain']]
    floor_pieces += [parts['Floor_Dust'].crop((x,y,x+32,y+32))
                     for y in (0,32) for x in (0,32)]
    for tile in floor_pieces:
        for i in range(32):
            assert tile.getpixel((0,i)) == tile.getpixel((31,i)) == floor_pieces[0].getpixel((0,i))
            assert tile.getpixel((i,0)) == tile.getpixel((i,31)) == floor_pieces[0].getpixel((i,0))
    for _, col, row in cells:
        assert atlas.crop((col*T,row*T,(col+1)*T,(row+1)*T)).getbbox() is not None

    target = OUT / 'SpaceBase_Tileset_32px_v1.png'
    atlas.save(target)
    write_meta(target, cells, atlas.height)
    (PREVIEW/'layout.json').write_text(json.dumps({'cellSize':32, 'sheetSize':atlas.size,
        'origin':'top-left', 'sprites':[{'name':n,'column':c,'row':r} for n,c,r in cells]}, indent=2), encoding='utf-8')
    # Checkerboard is preview-only; the atlas retains genuine transparency.
    shown = Image.new('RGBA', atlas.size, (22,26,32,255))
    sd = ImageDraw.Draw(shown)
    for y in range(0,160,8):
        for x in range(0,256,8):
            if (x//8+y//8)%2:
                sd.rectangle((x,y,x+7,y+7), fill=(29,34,40,255))
    shown.alpha_composite(atlas)
    shown.resize((1536,960),Image.Resampling.NEAREST).save(PREVIEW/'Sheet_6x.png')
    grid = shown.resize((1536,960),Image.Resampling.NEAREST)
    gd = ImageDraw.Draw(grid)
    for x in range(0,1536,192):
        gd.line((x,0,x,959), fill=(112,135,149,255))
    for y in range(0,960,192):
        gd.line((0,y,1535,y), fill=(112,135,149,255))
    grid.save(PREVIEW/'Sheet_Grid_6x.png')

    room = Image.new('RGBA',(384,384),(0,0,0,255))
    for row in range(3,12):
        for col in range(12):
            room.paste(floor((col,row) in {(3,4),(7,7),(2,9),(9,10)}),(col*32,row*32))
    for col in range(1,11):
        kind = ('Light','Electrical','Divider')[col%3]
        room.paste(parts['North_'+kind],(col*32,0))
    room.paste(parts['Corner_NW'],(0,0))
    room.paste(parts['Corner_NE'],(352,0))
    room.paste(parts['Floor_Dust'],(288,96))
    for row in range(3,11):
        room.alpha_composite(parts['West_Vent' if row%3==0 else 'West_Plain'],(0,row*32))
        room.alpha_composite(parts['East_Vent' if row%3==0 else 'East_Plain'],(352,row*32))
    for col in range(1,11):
        room.alpha_composite(parts['South_Plain'],(col*32,352))
    room.alpha_composite(parts['Corner_SW'],(0,352))
    room.alpha_composite(parts['Corner_SE'],(352,352))
    assert room.getchannel('A').getextrema() == (255,255)
    framed = Image.new('RGBA',(416,416),(0,0,0,255))
    framed.paste(room,(16,16))
    framed.resize((1248,1248),Image.Resampling.NEAREST).save(PREVIEW/'Room_3x.png')
    report = {'sheet':str(target),'size':list(atlas.size),'cellSize':32,'occupiedCells':len(cells),
              'checks':['native 32px cells','32x96 north walls','identical north joining edges',
              'north corner join edges','side repetition edges','lower corner join edges',
              'exact 6px axis-aligned lower corner alpha masks','opaque floors and north walls',
              'matching boundaries on all six floor cells',
              'no sprite overlap','opaque assembled room'], 'result':'PASS'}
    (PREVIEW/'validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report,indent=2))


if __name__ == '__main__':
    main()
