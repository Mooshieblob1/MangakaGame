"""Local ambient animation clips for the fixed parent models. No animation API."""
import argparse, json, math, struct
from pathlib import Path
import numpy as np
parser=argparse.ArgumentParser();parser.add_argument('character',choices=['mom','dad']);args=parser.parse_args()
ROOT=Path(__file__).resolve().parents[1];name=args.character
source=ROOT/f'TestResults/tripo-chibi-trial/{name}/{name}-rigged.glb'
raw=source.read_bytes();length=struct.unpack_from('<I',raw,12)[0]
doc=json.loads(raw[20:20+length]);offset=20+length
binary=bytearray(raw[offset+8:offset+8+struct.unpack_from('<I',raw,offset)[0]])
nodes=doc['nodes'];parents={c:i for i,n in enumerate(nodes) for c in n.get('children',[])}
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

skin=doc['skins'][0];bone_ids=skin['joints']
mesh_world=rest[next(i for i,n in enumerate(nodes) if 'skin' in n)]
inverse=[(np.linalg.inv(rest[i])@mesh_world).T.flatten().tolist() for i in bone_ids]
skin['inverseBindMatrices']=accessor(inverse,'MAT4')

def sample(mode,t):
    phase=t*math.tau;desired={i:rest[i][:3,:3].copy() for i in order}
    def turn(name,angle):
        i=names[name];desired[i]=rz(angle)@rest[i][:3,:3]
    def point(name,child,direction):
        i,c=names[name],names[child];desired[i]=aim(rest[c][:3,3]-rest[i][:3,3],direction)@rest[i][:3,:3]
    for side,sign in [('Left',-1),('Right',1)]:
        stride=math.sin(phase)*sign if mode=='Walk' else 0
        # Keep sleeves clear of the body with a small natural outward angle.
        point(side+'Arm',side+'ForeArm',(-stride*.20,-1,sign*.18))
        point(side+'ForeArm',side+'Hand',(.10-stride*.20,-1,sign*.08))
        if mode=='Walk':
            turn(side+'UpLeg',stride*.26)
            turn(side+'Leg',stride*.26-max(0,-stride)*.28)
        elif mode in ('Sit','Eat'):
            point(side+'UpLeg',side+'Leg',(1,-.03,sign*.08))
            point(side+'Leg',side+'Foot',(0,-1,0))
            point(side+'Arm',side+'ForeArm',(.30,-1,sign*.15))
            point(side+'ForeArm',side+'Hand',(.85,-.3,-sign*.12))
            if mode=='Eat' and side=='Right':
                # Reach down to the bowl, then lift a small bite with a relaxed pause.
                bite=(1-math.cos(phase))*.5
                point(side+'Arm',side+'ForeArm',(.48,-1,sign*.16))
                point(side+'ForeArm',side+'Hand',(.85-.50*bite,-.25+1.35*bite,-sign*.16))
        hand,fore=names[side+'Hand'],names[side+'ForeArm']
        desired[hand]=desired[fore]@rest[fore][:3,:3].T@rest[hand][:3,:3]
    if mode=='Idle':turn('Head',math.sin(phase)*.013)
    result={}
    for i in bone_ids:
        parent=parents.get(i);local=desired[parent].T@desired[i] if parent is not None else desired[i]
        result[i]=quaternion(local)
    position=np.array(nodes[names['Root']]['translation'])
    if mode in ('Sit','Eat'):
        # Seat top .51 at a preview scale of 1.2, pelvis slightly above cushion.
        position+=np.array([.015,.51/1.2+.035-rest[names['Hips']][1,3],0])
    elif mode=='Walk':position[1]+=.004*(1-math.cos(phase*2))
    return result,position

doc['animations']=[]
for mode,duration in [('Idle',4),('Walk',1.1),('Sit',3),('Eat',4.5)]:
    times=np.linspace(0,duration,int(duration*30)+1);time_accessor=accessor(times,'SCALAR')
    rotations={i:[] for i in bone_ids};translations=[]
    for t in times:
        r,p=sample(mode,t/duration)
        for i,q in r.items():
            if rotations[i] and np.dot(rotations[i][-1],q)<0:q=-q
            rotations[i].append(q)
        translations.append(p)
    animation={'name':mode,'samplers':[],'channels':[]}
    def track(node,path,values,kind):
        animation['channels'].append({'sampler':len(animation['samplers']),'target':{'node':node,'path':path}})
        animation['samplers'].append({'input':time_accessor,'output':accessor(values,kind),'interpolation':'LINEAR'})
    for i,values in rotations.items():track(i,'rotation',values,'VEC4')
    track(names['Root'],'translation',translations,'VEC3');doc['animations'].append(animation)

doc['asset']['generator']='Mangaka Studio local parent animation trial'
doc['buffers'][0]['byteLength']=len(binary)
encoded=json.dumps(doc,separators=(',',':')).encode();encoded+=b' '*((-len(encoded))%4);binary+=b'\0'*((-len(binary))%4)
output=struct.pack('<4sII',b'glTF',2,28+len(encoded)+len(binary))+struct.pack('<II',len(encoded),0x4e4f534a)+encoded+struct.pack('<II',len(binary),0x004e4942)+binary
for path in [ROOT/f'TestResults/modular-people/{name}-animated.glb',ROOT/f'godot/Assets/Parents/{name}/{name}-animated.glb']:
    path.parent.mkdir(exist_ok=True,parents=True);path.write_bytes(output)
print(f'{name}: {len(bone_ids)} joints, four local clips, {len(output):,} bytes.')

