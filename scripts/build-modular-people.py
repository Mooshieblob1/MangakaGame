"""Author the original modular chibi kit as GLB; no third-party/generated mesh edits.

One fixed joint layout, material colour slots, three hair and three clothing modules.
The game and browser preview consume this same file. Pure Python standard library.
"""
import json
import math
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'godot/Assets/People'
PALETTE = {
    'Skin': '#ebba91', 'Hair': '#252332', 'Cloth': '#4c7382',
    'Undershirt': '#eee8d9', 'Trousers': '#343b4c', 'Shoes': '#282c39',
    'EyeWhite': '#f8f6ee', 'Iris': '#536d94', 'Ink': '#242635',
    'Glasses': '#292d3a', 'Sole': '#454959', 'HairHighlight': '#484252',
}
nodes, groups, materials = [], {}, list(PALETTE)

def node(name, parent=None, at=(0, 0, 0), **extras):
    index = len(nodes)
    nodes.append({'name': name, 'translation': list(at), 'children': [], 'extras': extras})
    if parent is not None:
        nodes[parent]['children'].append(index)
    return index

def norm(v):
    length = math.sqrt(sum(x*x for x in v))
    return tuple(x / length for x in v) if length else (0, 1, 0)

def add(n, material, positions, normals, indices):
    p, no, ix = groups.setdefault((n, material), ([], [], []))
    base = len(p)
    p.extend(positions); no.extend(normals); ix.extend(base+i for i in indices)

def sphere(n, center, size, material, segments=16, rings=10, exponent=1, tilt=0, cap=None):
    positions, normals, indices = [], [], []
    c, s = math.cos(tilt), math.sin(tilt)
    def signed(v, power): return math.copysign(abs(v)**power, v)
    for i in range(rings+1):
        for j in range(segments+1):
            phi = j*math.tau/segments
            theta = i/rings*(cap(phi) if cap else math.pi)
            unit = (math.sin(theta)*math.cos(phi), math.cos(theta), math.sin(theta)*math.sin(phi))
            q = [signed(v, exponent) for v in unit]
            p = [q[k]*size[k]/2 for k in range(3)]
            normal = norm([signed(q[k], 2/exponent-1)/size[k] for k in range(3)])
            positions.append((center[0]+p[0]*c-p[1]*s, center[1]+p[0]*s+p[1]*c, center[2]+p[2]))
            normals.append((normal[0]*c-normal[1]*s, normal[0]*s+normal[1]*c, normal[2]))
    for i in range(rings):
        for j in range(segments):
            a=i*(segments+1)+j; b=a+segments+1
            indices.extend((a,a+1,b,a+1,b+1,b))
    add(n, material, positions, normals, indices)

def tube(n, points, radii, material, sides=8, flatten=1):
    if material=='Hair' and len(points)<=4:
        smooth, widths=[],[]
        for segment in range(len(points)-1):
            p0,p1,p2,p3=(points[max(0,segment-1)],points[segment],points[segment+1],points[min(len(points)-1,segment+2)])
            for step in range(6):
                t=step/6
                smooth.append(tuple(.5*(2*p1[k]+(-p0[k]+p2[k])*t+(2*p0[k]-5*p1[k]+4*p2[k]-p3[k])*t*t+(-p0[k]+3*p1[k]-3*p2[k]+p3[k])*t*t*t) for k in range(3)))
                widths.append(radii[segment]*(1-t)+radii[segment+1]*t)
        points=smooth+[points[-1]];radii=widths+[radii[-1]]
    positions, normals, indices = [], [], []
    for i,p in enumerate(points):
        # Parallel-ish frames suffice for the short, gently curved locks and frames.
        prev=points[max(0,i-1)]; nxt=points[min(len(points)-1,i+1)]
        tangent=norm([nxt[k]-prev[k] for k in range(3)])
        ref=(0,0,1) if abs(tangent[2])<.9 else (0,1,0)
        x=norm((tangent[1]*ref[2]-tangent[2]*ref[1],tangent[2]*ref[0]-tangent[0]*ref[2],tangent[0]*ref[1]-tangent[1]*ref[0]))
        y=(tangent[1]*x[2]-tangent[2]*x[1],tangent[2]*x[0]-tangent[0]*x[2],tangent[0]*x[1]-tangent[1]*x[0])
        for j in range(sides):
            angle=j*math.tau/sides
            direction=[math.cos(angle)*x[k]+math.sin(angle)*y[k] for k in range(3)]
            positions.append(tuple(p[k]+radii[i]*direction[k]*(flatten if k==2 else 1) for k in range(3)))
            normals.append(norm([direction[k]/(flatten if k==2 else 1) for k in range(3)]))
    for i in range(len(points)-1):
        for j in range(sides):
            a=i*sides+j;b=i*sides+(j+1)%sides;c=a+sides;d=b+sides
            indices.extend((a,b,c,b,d,c))
    add(n,material,positions,normals,indices)

body=node('Body',at=(0,-.14,0),kit_version=1)
torso=node('Torso',body)
head=node('Head',body,(0,1.03,0))
sphere(head,(0,0,0),(.52,.54,.48),'Skin',24,16)
def face_patch(x,y,width,height,material,layer,almond=False):
    # Flat illustrated features hug the curved face instead of protruding eyes.
    def point(px,py):
        z=-.24*math.sqrt(max(.01,1-(px/.26)**2-(py/.27)**2))-layer
        return (px,py,z)
    points=[point(x,y)]
    for j in range(33):
        a=j*math.tau/32;sy=math.sin(a)
        points.append(point(x+math.cos(a)*width/2,y+sy*(abs(sy)**.28 if almond else 1)*height/2))
    indices=[]
    for j in range(32):indices.extend((0,j+2,j+1))
    add(head,material,points,[(0,0,-1)]*len(points),indices)
for sign in (-1,1):
    sphere(head,(sign*.254,-.025,0),(.075,.12,.065),'Skin')
    face_patch(sign*.103,-.008,.128,.090,'Ink',.004,True)
    face_patch(sign*.103,-.012,.116,.073,'EyeWhite',.006,True)
    face_patch(sign*.103,-.012,.051,.069,'Iris',.008)
    face_patch(sign*.103,-.010,.024,.051,'Ink',.010)
    face_patch(sign*.103-.012,.010,.014,.018,'EyeWhite',.012)
    tube(head,[(sign*.15,.084,-.222),(sign*.105,.093,-.239),(sign*.067,.085,-.239)],[.008]*3,'Hair')
# No mouth; identity reads through the eyes, hair and glasses.
sphere(torso,(0,.46,0),(.33,.16,.25),'Trousers',exponent=.5)
sphere(torso,(0,.82,0),(.12,.14,.12),'Skin')
for style in range(3):
    clothing=node(['Cardigan','CollaredShirt','Sweater'][style],torso,slot='wardrobe',option=style)
    sphere(clothing,(0,.66,0),(.37,.34,.255),'Cloth',exponent=.68)
    if style==0:
        sphere(clothing,(0,.70,-.122),(.12,.23,.024),'Undershirt',exponent=.60)
        for sign in (-1,1):
            tube(clothing,[(sign*.086,.79,-.12),(sign*.076,.66,-.145),(sign*.075,.515,-.123)],[.012]*3,'Cloth')
    if style in (0,1):
        for sign in (-1,1):
            sphere(clothing,(sign*.065,.788,-.077),(.096,.085,.065),'Undershirt',exponent=.55,tilt=sign*.45)
        for y in (.725,.665,.605):
            sphere(clothing,(.007,y,-.144),(.016,.016,.008),'Ink',8,6)
    else:
        sphere(clothing,(0,.80,-.012),(.175,.052,.15),'Cloth',exponent=.7)
        tube(clothing,[(-.12,.52,-.12),(0,.50,-.145),(.12,.52,-.12)],[.012]*3,'Cloth')
for side,sign in (('Left',-1),('Right',1)):
    arm=node(side+'Arm',body,(sign*.23,.8,0))
    sphere(arm,(0,-.085,0),(.14,.205,.15),'Cloth',exponent=.7)
    forearm=node(side+'Forearm',arm,(0,-.17,0))
    sphere(forearm,(0,-.06,-.005),(.115,.15,.125),'Skin',exponent=.75)
    sphere(forearm,(sign*.045,-.10,-.043),(.05,.055,.07),'Skin')
    sleeve=node('Sleeve'+side,forearm,slot='sleeve')
    sphere(sleeve,(0,-.037,0),(.13,.10,.14),'Cloth',exponent=.75)
    leg=node(side+'Leg',body,(sign*.10,.46,0))
    sphere(leg,(0,-.06,0),(.165,.14,.17),'Trousers',exponent=.6)
    knee=node(side+'Knee',leg,(0,-.12,0))
    sphere(knee,(0,-.065,0),(.155,.14,.18),'Trousers',exponent=.6)
    sphere(knee,(0,-.15,-.044),(.195,.10,.265),'Shoes',exponent=.55)
    sphere(knee,(0,-.185,-.046),(.2,.025,.27),'Sole',exponent=.45)
for style in range(3):
    hair=node(['HairShort','HairBob','HairTied'][style],head,slot='hair',option=style)
    sphere(hair,(0,.035,.018),(.556,.54,.512),'Hair',24,14,cap=lambda phi:1.22+.70*(math.sin(phi)+1)/2)
    # Individually sculpted broad locks with tapered tips, not a solid helmet.
    for x,tip in ((-.20,-.17),(-.105,-.045),(.00,.07),(.115,.16),(.205,.21)):
        points=[(x*.7,.24,-.11),(x,.19,-.19),(x+.02,.10,-.245),(tip,.052-abs(x)*.17,-.233)]
        tube(hair,points,[.045,.065,.045,.002],'Hair',12,flatten=.38)
        highlight=[(x*.7-.008,.24,-.132),(x-.008,.19,-.218),(x+.012,.115,-.260)]
        tube(hair,highlight,[.003,.005,.001],'HairHighlight',6,flatten=.35)
    if style==0:
        for sign in (-1,1):
            sphere(hair,(sign*.238,.04,.005),(.085,.21,.28),'Hair',tilt=sign*.15)
    elif style==1:
        for sign in (-1,1):
            tube(hair,[(sign*.22,.16,.02),(sign*.265,0,.04),(sign*.257,-.19,.01),(sign*.19,-.25,-.025)],[.08,.09,.085,.025],'Hair',12)
        sphere(hair,(0,-.07,.19),(.48,.37,.20),'Hair')
    else:
        sphere(hair,(0,.03,.26),(.12,.11,.08),'Ink')
        tube(hair,[(0,.07,.24),(.012,.0,.35),(.015,-.19,.40),(0,-.36,.30)],[.08,.10,.075,.009],'Hair',12)
glasses=node('Glasses',head,slot='glasses',option=1)
for sign in (-1,1):
    points=[]
    for i in range(33):
        a=i*math.tau/32
        points.append((sign*.105+math.copysign(abs(math.cos(a))**.5,math.cos(a))*.089, -.005+math.copysign(abs(math.sin(a))**.5,math.sin(a))*.065,-.274))
    tube(glasses,points,[.008]*len(points),'Glasses')
    tube(glasses,[(sign*.195,.018,-.27),(sign*.247,.020,-.11),(sign*.252,.012,.02)],[.008]*3,'Glasses')
tube(glasses,[(-.018,.01,-.275),(0,.02,-.278),(.018,.01,-.275)],[.008]*3,'Glasses')

binary=bytearray(); views=[]; accessors=[]; meshes=[]
def accessor(values, count, kind, component=5126, bounds=False):
    while len(binary)%4: binary.append(0)
    start=len(binary);code='f' if component==5126 else 'I'
    binary.extend(struct.pack('<'+code*len(values),*values))
    view=len(views);views.append({'buffer':0,'byteOffset':start,'byteLength':len(binary)-start})
    result={'bufferView':view,'componentType':component,'count':count,'type':kind}
    if bounds:
        result['min']=[min(values[k::3]) for k in range(3)]; result['max']=[max(values[k::3]) for k in range(3)]
    accessors.append(result);return len(accessors)-1
for (parent,material),(p,n,ix) in groups.items():
    position=accessor([v for xyz in p for v in xyz],len(p),'VEC3',bounds=True)
    normal=accessor([v for xyz in n for v in xyz],len(n),'VEC3')
    index=accessor(ix,len(ix),'SCALAR',5125)
    meshes.append({'name':nodes[parent]['name']+'_'+material,'primitives':[{'attributes':{'POSITION':position,'NORMAL':normal},'indices':index,'material':materials.index(material)}]})
    mesh_node=node(nodes[parent]['name']+'_'+material,parent)
    nodes[mesh_node]['mesh']=len(meshes)-1
def linear(value): return value/12.92 if value<=.04045 else ((value+.055)/1.055)**2.4
def color(hex_value): return [linear(int(hex_value[i:i+2],16)/255) for i in (1,3,5)]+[1]
doc={'asset':{'version':'2.0','generator':'Mangaka Studio original modular kit v1'},'scene':0,'scenes':[{'nodes':[body]}],
     'nodes':nodes,'meshes':meshes,'accessors':accessors,'bufferViews':views,'buffers':[{'byteLength':len(binary)}],
     'materials':[{'name':name,'pbrMetallicRoughness':{'baseColorFactor':color(PALETTE[name]),'metallicFactor':0,'roughnessFactor':.78},'doubleSided':False} for name in materials]}
for item in nodes:
    if not item['children']: del item['children']
    if not item['extras']: del item['extras']
encoded=json.dumps(doc,separators=(',',':')).encode()
encoded+=b' '*((-len(encoded))%4);binary+=b'\0'*((-len(binary))%4)
glb=struct.pack('<4sII',b'glTF',2,12+8+len(encoded)+8+len(binary))+struct.pack('<II',len(encoded),0x4e4f534a)+encoded+struct.pack('<II',len(binary),0x004e4942)+binary
OUT.mkdir(parents=True,exist_ok=True)
(OUT/'modular-person.glb').write_bytes(glb)
(OUT/'kit.json').write_text(json.dumps({'version':1,'height_m':1.20,'standing_offset':-.14,'pelvis_bottom':.38,'forward':'-Z','hair':['HairShort','HairBob','HairTied'],'wardrobe':['Cardigan','CollaredShirt','Sweater'],'materials':PALETTE,'joint_names':['Body','Torso','Head','LeftArm','RightArm','LeftForearm','RightForearm','LeftLeg','RightLeg','LeftKnee','RightKnee'],'all_module_triangles':sum(len(v[2])//3 for v in groups.values()),'bytes':len(glb)},indent=2),encoding='utf-8')
print(f'Wrote modular kit: {len(glb):,} bytes; {len(meshes)} material batches across all modules.')
