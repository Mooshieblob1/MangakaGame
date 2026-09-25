"""Check exported Helper skin bindings, animation values and looping seams."""
import argparse, json, struct
from pathlib import Path
import numpy as np

parser=argparse.ArgumentParser();parser.add_argument('path',nargs='?',default=str(Path(__file__).resolve().parents[1]/'TestResults/modular-people/helper-animated.glb'));parser.add_argument('--clips',default='Idle,Walk,Sit,Write');args=parser.parse_args();path=Path(args.path)
raw=path.read_bytes();size=struct.unpack_from('<I',raw,12)[0]
doc=json.loads(raw[20:20+size]);data=raw[28+size:]
def read(i):
    a=doc['accessors'][i];v=doc['bufferViews'][a['bufferView']]
    width={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}[a['type']]
    dtype={5126:'<f4',5123:'<u2',5125:'<u4',5121:'u1'}[a['componentType']]
    return np.frombuffer(data,dtype=dtype,count=a['count']*width,offset=v.get('byteOffset',0)+a.get('byteOffset',0)).reshape(-1,width)
def rotation(q):
    x,y,z,w=q
    return np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],
                     [2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],
                     [2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])
world={}
def visit(i,parent=np.eye(4)):
    a=doc['nodes'][i];m=np.eye(4)
    m[:3,:3]=rotation(a.get('rotation',[0,0,0,1]))@np.diag(a.get('scale',[1,1,1]))
    m[:3,3]=a.get('translation',[0,0,0]);world[i]=parent@m
    for child in a.get('children',[]):visit(child,world[i])
for i in doc['scenes'][doc.get('scene',0)]['nodes']:visit(i)
skin=doc['skins'][0];attrs=doc['meshes'][0]['primitives'][0]['attributes']
positions=read(attrs['POSITION']);joints=read(attrs['JOINTS_0']);weights=read(attrs['WEIGHTS_0'])
assert np.allclose(weights.sum(axis=1),1,atol=1e-5)
assert joints.max()<len(skin['joints']) and weights.min()>=0
inverse=read(skin['inverseBindMatrices']).reshape(-1,4,4).transpose(0,2,1)
mesh=next(i for i,a in enumerate(doc['nodes']) if 'skin' in a)
matrices=np.array([np.linalg.inv(world[mesh])@world[node]@inverse[k] for k,node in enumerate(skin['joints'])])
vertices=np.column_stack([positions,np.ones(len(positions))])
result=sum(np.einsum('nij,nj->ni',matrices[joints[:,k]],vertices)*weights[:,k,None] for k in range(4))
error=float(np.max(np.abs(result[:,:3]-positions)))
assert error<1e-4,f'Rest skin position error: {error}'
for clip in doc['animations']:
    for channel in clip['channels']:
        sampler=clip['samplers'][channel['sampler']];times=read(sampler['input']);values=read(sampler['output'])
        assert len(times)==len(values) and np.isfinite(values).all() and (np.diff(times[:,0])>0).all()
        if channel['target']['path']=='rotation':
            assert np.allclose(np.linalg.norm(values,axis=1),1,atol=1e-5)
            assert abs(np.dot(values[0],values[-1]))>.99999
        else:assert np.allclose(values[0],values[-1],atol=1e-5)
    print('Validated loop:',clip['name'])
assert {a['name'] for a in doc['animations']}==set(args.clips.split(','))
print(f'Skin weights, bind pose (max error {error:.8f}), finite animation tracks and loop seams passed.')
