"""Locally authored animation clips for the purchased Helper rig; no animation API.

Keeps the original downloaded rig untouched. Requires numpy. Coordinates in the
source asset are +X forward, +Y up; the preview rotates the entire asset to -Z.
"""
import json, math, struct, io
from pathlib import Path
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'TestResults/modular-people'
SOURCE=ROOT/'TestResults/tripo-chibi-trial/helper-chan/helper-chan-rigged.glb'
raw=SOURCE.read_bytes(); length=struct.unpack_from('<I',raw,12)[0]
doc=json.loads(raw[20:20+length]); offset=20+length
binary=bytearray(raw[offset+8:offset+8+struct.unpack_from('<I',raw,offset)[0]])
nodes=doc['nodes']; parents={c:i for i,n in enumerate(nodes) for c in n.get('children',[])}
names={n['name'].replace('mixamorig:',''):i for i,n in enumerate(nodes)}

def rotation(q):
    x,y,z,w=q
    return np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],
                     [2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],
                     [2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])

def quaternion(m):
    # Eigenvector form avoids unstable trace branches near 180 degrees.
    k=np.array([[m[0,0]-m[1,1]-m[2,2],m[1,0]+m[0,1],m[2,0]+m[0,2],m[2,1]-m[1,2]],
                [m[1,0]+m[0,1],m[1,1]-m[0,0]-m[2,2],m[2,1]+m[1,2],m[0,2]-m[2,0]],
                [m[2,0]+m[0,2],m[2,1]+m[1,2],m[2,2]-m[0,0]-m[1,1],m[1,0]-m[0,1]],
                [m[2,1]-m[1,2],m[0,2]-m[2,0],m[1,0]-m[0,1],np.trace(m)]])/3
    _,v=np.linalg.eigh(k);q=v[:,-1];return q if q[3]>=0 else -q

def rz(a):
    c,s=math.cos(a),math.sin(a);return np.array([[c,-s,0],[s,c,0],[0,0,1.]])

def aim(a,b):
    a=a/np.linalg.norm(a);b=np.array(b,dtype=float);b/=np.linalg.norm(b)
    v=np.cross(a,b);c=np.dot(a,b)
    if c < -.99999: return rotation([1,0,0,0])
    k=np.array([[0,-v[2],v[1]],[v[2],0,-v[0]],[-v[1],v[0],0]])
    return np.eye(3)+k+k@k/(1+c)

rest={}; order=[]
def visit(i,p=np.eye(4)):
    n=nodes[i];m=np.eye(4);m[:3,:3]=rotation(n.get('rotation',[0,0,0,1]));m[:3,3]=n.get('translation',[0,0,0])
    rest[i]=p@m;order.append(i)
    for c in n.get('children',[]):visit(c,rest[i])
for i in doc['scenes'][doc.get('scene',0)]['nodes']:visit(i)

def accessor(values,kind,component=5126):
    values=np.asarray(values,dtype={5126:'<f4',5123:'<u2'}[component]);assert np.isfinite(values).all()
    while len(binary)%4:binary.append(0)
    start=len(binary);binary.extend(values.tobytes());view=len(doc['bufferViews'])
    doc['bufferViews'].append({'buffer':0,'byteOffset':start,'byteLength':values.nbytes})
    entry={'bufferView':view,'componentType':component,'count':len(values),'type':kind}
    if kind=='SCALAR':entry.update(min=[float(values.min())],max=[float(values.max())])
    doc['accessors'].append(entry);return len(doc['accessors'])-1

def read_accessor(index):
    a=doc['accessors'][index];v=doc['bufferViews'][a['bufferView']]
    count={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}[a['type']]
    return np.frombuffer(binary,dtype={5126:'<f4',5123:'<u2',5125:'<u4',5121:'u1'}[a['componentType']],count=a['count']*count,offset=v.get('byteOffset',0)+a.get('byteOffset',0)).reshape(-1,count).copy()

# Long blonde hair is not part of the arm/leg skin. Identify the outer silhouette
# and golden strands behind the body, then bind it to dedicated head children.
primitive=doc['meshes'][0]['primitives'][0];attrs=primitive['attributes']
positions=read_accessor(attrs['POSITION']);uv=read_accessor(attrs['TEXCOORD_0'])
joints=read_accessor(attrs['JOINTS_0']);weights=read_accessor(attrs['WEIGHTS_0'])
texture=doc['materials'][0]['pbrMetallicRoughness']['baseColorTexture']['index']
image=doc['images'][doc['textures'][texture]['source']];view=doc['bufferViews'][image['bufferView']]
im=Image.open(io.BytesIO(binary[view['byteOffset']:view['byteOffset']+view['byteLength']])).convert('RGB')
pixels=np.array(im)[np.clip((uv[:,1]*im.height).astype(int),0,im.height-1),np.clip((uv[:,0]*im.width).astype(int),0,im.width-1)].astype(float)
gold=(pixels[:,0]>160)&(pixels[:,1]-pixels[:,2]>42)&(pixels[:,0]-pixels[:,2]>75)
hair=(np.abs(positions[:,2])>.24)|((np.abs(positions[:,2])>.15)&gold)|((positions[:,0]<-.075)&gold)
hair &= positions[:,1]<.91
skin=doc['skins'][0]
mesh_world=rest[next(i for i,n in enumerate(nodes) if 'skin' in n)]
# Recalculate all bind matrices in the same mesh coordinate space. The vendor
# rig has a small armature translation baked inconsistently into its bindings.
inverse=[(np.linalg.inv(rest[i])@mesh_world).T.flatten().tolist() for i in skin['joints']]
hair_counts={}
for side,sign in [('Left',-1),('Right',1)]:
    name='Hair'+side;world=np.eye(4);world[:3,3]=[.005,.83,sign*.225]
    parent=names['Head'];local=np.linalg.inv(rest[parent])@world;i=len(nodes)
    nodes.append({'name':name,'translation':local[:3,3].tolist(),'rotation':quaternion(local[:3,:3]).tolist()})
    nodes[parent].setdefault('children',[]).append(i);parents[i]=parent;names[name]=i
    rest[i]=world;order.append(i);index=len(skin['joints']);skin['joints'].append(i)
    inverse.append((np.linalg.inv(world)@mesh_world).T.flatten().tolist())
    mask=hair&(positions[:,2]*sign>0);hair_counts[side]=int(mask.sum())
    joints[mask]=[index,0,0,0];weights[mask]=[1,0,0,0]
attrs['JOINTS_0']=accessor(joints,'VEC4',5123);attrs['WEIGHTS_0']=accessor(weights,'VEC4')
skin['inverseBindMatrices']=accessor(inverse,'MAT4')
assert np.allclose(weights.sum(axis=1),1,atol=1e-4)
print('Rebound twintail vertices:',hair_counts)
bone_ids=skin['joints']
def sample(mode,t):
    phase=t*math.tau;desired={i:rest[i][:3,:3].copy() for i in order}
    def turn(name,angle):
        i=names[name];desired[i]=rz(angle)@rest[i][:3,:3]
    def point(name,child,direction):
        i,c=names[name],names[child];desired[i]=aim(rest[c][:3,3]-rest[i][:3,3],direction)@rest[i][:3,:3]
    sit=mode in ('Sit','Write')
    if mode=='Walk':
        for side,sign in [('Left',1),('Right',-1)]:
            stride=math.sin(phase)*sign
            turn(side+'UpLeg',stride*.32)
            turn(side+'Leg',stride*.32-max(0,-stride)*.36)
            turn(side+'Arm',-stride*.12)
            turn(side+'ForeArm',-stride*.12+.08)
            turn(side+'Hand',-stride*.12+.08)
    if sit:
        for side,sign in [('Left',-1),('Right',1)]:
            point(side+'UpLeg',side+'Leg',(1,-.03,sign*.10))
            point(side+'Leg',side+'Foot',(0,-1,0))
            point(side+'Arm',side+'ForeArm',(.42,-.85,sign*.12))
            point(side+'ForeArm',side+'Hand',(1,.10,0))
        # Keep the enormous curls upright while seated; glance with a tiny tilt.
        turn('Head',-.012)
        if mode=='Write':
            for side in ('Left','Right'):
                point(side+'Arm',side+'ForeArm',(1,-.28,0))
                point(side+'ForeArm',side+'Hand',(1,.40+math.sin(phase*3)*(.045 if side=='Right' else 0),0))
                hand,fore=names[side+'Hand'],names[side+'ForeArm']
                desired[hand]=desired[fore]@rest[fore][:3,:3].T@rest[hand][:3,:3]
    if mode=='Idle':turn('Head',math.sin(phase)*.012)
    for side,sign in [('Left',-1),('Right',1)]:
        turn('Hair'+side,math.sin(phase+sign*.5)*(.018 if mode=='Walk' else .004))
    result={}
    for i in bone_ids:
        parent=parents.get(i);local=desired[parent].T@desired[i] if parent is not None else desired[i]
        result[i]=quaternion(local)
    root=names['Root'];position=np.array(nodes[root]['translation'])
    if sit:position+=np.array([.04,.122,0])
    elif mode=='Walk':position[1]+=.006*(1-math.cos(phase*2))
    return result,position

doc['animations']=[]
for mode,duration in [('Idle',3),('Walk',.85),('Sit',2),('Write',2)]:
    times=np.linspace(0,duration,int(duration*30)+1);time_accessor=accessor(times,'SCALAR')
    rotations={i:[] for i in bone_ids};positions=[]
    for t in times:
        r,p=sample(mode,t/duration)
        for i,q in r.items():
            if rotations[i] and np.dot(rotations[i][-1],q)<0:q=-q
            rotations[i].append(q)
        positions.append(p)
    animation={'name':mode,'samplers':[],'channels':[]}
    def track(node,path,values,kind):
        animation['channels'].append({'sampler':len(animation['samplers']),'target':{'node':node,'path':path}})
        animation['samplers'].append({'input':time_accessor,'output':accessor(values,kind),'interpolation':'LINEAR'})
    for i,values in rotations.items():track(i,'rotation',values,'VEC4')
    track(names['Root'],'translation',positions,'VEC3');doc['animations'].append(animation)

# A small rigid pen follows the right hand. Its rest transform is authored from
# the writing pose, so it is an attachment, not painted-on or deforming skin.
pose,root_position=sample('Write',0);posed={}
for i in order:
    local=np.eye(4);local[:3,:3]=rotation(pose.get(i,nodes[i].get('rotation',[0,0,0,1])))
    local[:3,3]=root_position if i==names['Root'] else nodes[i].get('translation',[0,0,0])
    posed[i]=posed.get(parents.get(i),np.eye(4))@local
hand=names['RightHand'];matrix=posed[hand];origin=matrix[:3,3]
tip=origin+np.array([.053,-.019,-.018]);top=origin+np.array([-.012,.048,.008])
axis=top-tip;axis/=np.linalg.norm(axis);side=np.cross(axis,[0,0,1]);side/=np.linalg.norm(side);other=np.cross(axis,side)
inverse_hand=np.linalg.inv(matrix);pen_positions=[];pen_normals=[];pen_indices=[]
for endpoint,radius in [(tip,0.001),(tip+(top-tip)*.13,.0035),(top,.0035)]:
    for j in range(8):
        normal=math.cos(j*math.tau/8)*side+math.sin(j*math.tau/8)*other
        pen_positions.append((inverse_hand@np.append(endpoint+normal*radius,1))[:3])
        pen_normals.append(inverse_hand[:3,:3]@normal)
for ring in range(2):
    for j in range(8):
        a=ring*8+j;b=ring*8+(j+1)%8;pen_indices.extend([a,b,a+8,b,b+8,a+8])
pen_material=len(doc['materials']);doc['materials'].append({'name':'Writing pen','pbrMetallicRoughness':{'baseColorFactor':[.035,.06,.09,1],'metallicFactor':.15,'roughnessFactor':.5}})
pi=accessor(pen_positions,'VEC3');doc['accessors'][pi].update(min=np.min(pen_positions,axis=0).tolist(),max=np.max(pen_positions,axis=0).tolist())
mesh=len(doc['meshes']);doc['meshes'].append({'name':'Writing pen','primitives':[{'attributes':{'POSITION':pi,'NORMAL':accessor(pen_normals,'VEC3')},'indices':accessor(pen_indices,'SCALAR',5123),'material':pen_material}]})
nodes[hand].setdefault('children',[]).append(len(nodes));nodes.append({'name':'WritingPen','mesh':mesh})
doc['asset']['generator']='Mangaka Studio local Helper animation trial'
doc['buffers'][0]['byteLength']=len(binary)
encoded=json.dumps(doc,separators=(',',':')).encode();encoded+=b' '*((-len(encoded))%4);binary+=b'\0'*((-len(binary))%4)
output=struct.pack('<4sII',b'glTF',2,28+len(encoded)+len(binary))+struct.pack('<II',len(encoded),0x4e4f534a)+encoded+struct.pack('<II',len(binary),0x004e4942)+binary
OUT.mkdir(exist_ok=True,parents=True);(OUT/'helper-animated.glb').write_bytes(output)
candidate=ROOT/'godot/Assets/Helper/Model'
candidate.mkdir(exist_ok=True,parents=True);(candidate/'helper-chan-animated.glb').write_bytes(output)
print(f'Authored {len(doc["animations"])} clips locally: {len(output):,} bytes; {len(bone_ids)} joints.')
