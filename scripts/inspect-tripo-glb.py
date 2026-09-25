"""Read-only GLB structure checks for the external asset trial; no game compilation."""
import json
import struct
import sys
from pathlib import Path

path = Path(sys.argv[1])
raw = path.read_bytes()
magic, version, length = struct.unpack_from('<4sII', raw)
assert magic == b'glTF' and version == 2 and length == len(raw), 'Invalid GLB header'
offset = 12
chunks = {}
while offset < length:
    size, kind = struct.unpack_from('<II', raw, offset)
    offset += 8
    assert offset + size <= length, 'Truncated GLB chunk'
    chunks[kind] = raw[offset:offset + size]
    offset += size
doc = json.loads(chunks[0x4e4f534a])
binary = chunks.get(0x004e4942, b'')
for view in doc.get('bufferViews', []):
    assert view.get('buffer', 0) == 0, 'Unexpected external buffer'
    assert view.get('byteOffset', 0) + view['byteLength'] <= len(binary), 'Buffer view exceeds GLB payload'
accessors = doc.get('accessors', [])
triangles = vertices = 0
for mesh in doc.get('meshes', []):
    for primitive in mesh['primitives']:
        position = accessors[primitive['attributes']['POSITION']]
        vertices += position['count']
        count = accessors[primitive['indices']]['count'] if 'indices' in primitive else position['count']
        if primitive.get('mode', 4) == 4:
            assert count % 3 == 0, 'Incomplete triangle'
            triangles += count // 3
report = {
    'file': str(path), 'bytes': len(raw), 'glb_version': version,
    'mesh_count': len(doc.get('meshes', [])), 'vertices': vertices, 'triangles': triangles,
    'materials': len(doc.get('materials', [])),
    'images': [{'mime_type': image.get('mimeType'), 'embedded': 'bufferView' in image} for image in doc.get('images', [])],
    'skins': len(doc.get('skins', [])), 'animations': len(doc.get('animations', [])),
    'extensions_required': doc.get('extensionsRequired', []),
    'external_resources': [entry['uri'] for category in ('buffers', 'images') for entry in doc.get(category, []) if 'uri' in entry and not entry['uri'].startswith('data:')],
    'position_bounds': [{'min': item.get('min'), 'max': item.get('max')} for item in accessors if item.get('type') == 'VEC3' and 'min' in item],
}
path.with_suffix('.inspection.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
