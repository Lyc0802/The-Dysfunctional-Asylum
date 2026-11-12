using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace Asylum.Enemies
{
    // Standalone fixture only: actual levels can use the same components without this builder.
    public class EnemyPatrolDemo : MonoBehaviour
    {
        public bool buildOnStart = true;
        public EnemyPatrolBrain enemy;
        public EnemyTarget player;
        public NavMeshSurface surface;
        public EnemyPatrolPoint[] points;
        private readonly Vector3[] positions = {
            new Vector3(12,0,18), new Vector3(0,0,18), new Vector3(-12,0,18),
            new Vector3(-18,0,6), new Vector3(-6,0,6), new Vector3(-18,0,-8),
            new Vector3(12,0,6), new Vector3(12,0,-8), new Vector3(-3,0,-6), new Vector3(-3,0,-23) };
        private readonly Vector2[] sizes = {
            new Vector2(7,7),new Vector2(7,7),new Vector2(7,7),new Vector2(7,7),new Vector2(7,7),
            new Vector2(7,7),new Vector2(7,7),new Vector2(7,7),new Vector2(9,4),new Vector2(22,14) };
        private void Start() { if(buildOnStart) Build(); }
        private GameObject Box(string name,Vector3 position,Vector3 size,Transform parent)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name=name;
            obj.transform.SetParent(parent,false); obj.transform.position=position; obj.transform.localScale=size; return obj;
        }
        private Vector3 Boundary(int room, Vector3 toward)
        {
            Vector3 d=toward-positions[room]; Vector2 half=sizes[room]*.5f;
            float scale=Mathf.Min(Mathf.Abs(d.x)>.001f ? half.x/Mathf.Abs(d.x) : float.PositiveInfinity,
                Mathf.Abs(d.z)>.001f ? half.y/Mathf.Abs(d.z) : float.PositiveInfinity);
            return positions[room]+d*scale;
        }
        private void Wall(string name,Vector3 a,Vector3 b,Transform root)
        {
            if(Vector3.Distance(a,b)<.05f) return;
            var obj=Box(name,(a+b)*.5f+Vector3.up*1.5f,new Vector3(.15f,3,Vector3.Distance(a,b)),root);
            obj.transform.rotation=Quaternion.LookRotation(b-a);
        }
        private void Side(int room,Vector3 a,Vector3 b,List<Vector3> doors,Transform root)
        {
            Vector3 axis=(b-a).normalized; float length=Vector3.Distance(a,b);
            var intervals=new List<Vector2>();
            foreach(var door in doors)
            {
                float along=Vector3.Dot(door-a,axis);
                if(Vector3.Distance(door,a+axis*along)<.15f) intervals.Add(new Vector2(Mathf.Clamp(along-2.3f,0,length),Mathf.Clamp(along+2.3f,0,length)));
            }
            intervals.Sort((x,y)=>x.x.CompareTo(y.x)); float cursor=0;
            foreach(var gap in intervals) { if(gap.x>cursor) Wall("Room "+(room+1)+" wall",a+axis*cursor,a+axis*gap.x,root); cursor=Mathf.Max(cursor,gap.y); }
            if(cursor<length) Wall("Room "+(room+1)+" wall",a+axis*cursor,b,root);
        }
        public void Build()
        {
            var environment=new GameObject("Walkable geometry"); environment.transform.SetParent(transform,false);
            points=new EnemyPatrolPoint[15];
            for(int i=0;i<15;i++)
            {
                var obj=new GameObject("Point "+(i+1)); obj.transform.SetParent(transform,false);
                if(i<10) obj.transform.position=positions[i];
                points[i]=obj.AddComponent<EnemyPatrolPoint>();
            }
            EnemyPatrolDefaults.Apply(points);
            var doors=new List<Vector3>[10]; for(int i=0;i<10;i++) doors[i]=new List<Vector3>();
            var links=new HashSet<string>();
            for(int i=0;i<10;i++) foreach(var edge in points[i].connections)
            {
                int j=edge.destination.id-1; if(j>=10 || j==i) continue;
                string key=Mathf.Min(i,j)+":"+Mathf.Max(i,j); if(!links.Add(key)) continue;
                // Enter rooms through the middle of a wall, not a diagonal corner.
                // Outer lanes to room 10 avoid crossing the walls of room 9.
                Vector3 a,b; Vector3[] route;
                if(j==9 && (i==5 || i==7))
                {
                    float sign=i==5 ? -1 : 1;
                    a=positions[i]+Vector3.right*(sizes[i].x*.5f*sign);
                    b=positions[j]+Vector3.right*(sizes[j].x*.5f*sign);
                    float lane=i==5 ? -25 : 21;
                    route=new[]{a,new Vector3(lane,0,a.z),new Vector3(lane,0,b.z),b};
                }
                else
                {
                    Vector3 delta=positions[j]-positions[i];
                    if(Mathf.Abs(delta.x)>Mathf.Abs(delta.z))
                    {
                        float sign=Mathf.Sign(delta.x);
                        a=positions[i]+Vector3.right*(sizes[i].x*.5f*sign);
                        b=positions[j]-Vector3.right*(sizes[j].x*.5f*sign);
                        float middle=(a.x+b.x)*.5f;
                        route=new[]{a,new Vector3(middle,0,a.z),new Vector3(middle,0,b.z),b};
                    }
                    else
                    {
                        float sign=Mathf.Sign(delta.z);
                        a=positions[i]+Vector3.forward*(sizes[i].y*.5f*sign);
                        b=positions[j]-Vector3.forward*(sizes[j].y*.5f*sign);
                        float middle=(a.z+b.z)*.5f;
                        route=new[]{a,new Vector3(a.x,0,middle),new Vector3(b.x,0,middle),b};
                    }
                }
                doors[i].Add(a); doors[j].Add(b);
                for(int segment=1;segment<route.Length;segment++)
                {
                    Vector3 from=route[segment-1],to=route[segment];
                    if(Vector3.Distance(from,to)<.01f) continue;
                    Vector3 direction=(to-from).normalized, side=Vector3.Cross(Vector3.up,direction)*1.4f;
                    var floor=Box("Corridor "+(i+1)+"-"+(j+1),(from+to)*.5f-Vector3.up*.15f,new Vector3(2.8f,.3f,Vector3.Distance(from,to)+.3f),environment.transform);
                    floor.transform.rotation=Quaternion.LookRotation(direction);
                    // Leave elbow junctions open so rail ends cannot divide the NavMesh.
                    Vector3 wallFrom=segment>1 ? from+direction*1.4f : from;
                    Vector3 wallTo=segment<route.Length-1 ? to-direction*1.4f : to;
                    if(Vector3.Dot(wallTo-wallFrom,direction)>0)
                    { Wall("Corridor wall",wallFrom+side,wallTo+side,environment.transform); Wall("Corridor wall",wallFrom-side,wallTo-side,environment.transform); }
                    if(segment<route.Length-1) Box("Corridor elbow",to-Vector3.up*.15f,new Vector3(2.8f,.3f,2.8f),environment.transform);
                }
            }
            for(int i=0;i<10;i++)
            {
                Vector3 p=positions[i]; Vector2 h=sizes[i]*.5f;
                Box("Room "+(i+1)+" floor",p-Vector3.up*.15f,new Vector3(sizes[i].x,.3f,sizes[i].y),environment.transform);
                var volume=new GameObject("Room "+(i+1)+" volume"); volume.transform.SetParent(transform,false); volume.transform.position=p+Vector3.up*1.5f;
                var collider=volume.AddComponent<BoxCollider>(); collider.size=new Vector3(sizes[i].x,4,sizes[i].y); collider.isTrigger=true;
                var room=volume.AddComponent<EnemyRoomVolume>(); room.roomId=i+1; room.bounds=collider;
                Vector3 a=p+new Vector3(-h.x,0,-h.y), b=p+new Vector3(h.x,0,-h.y), c=p+new Vector3(h.x,0,h.y), d=p+new Vector3(-h.x,0,h.y);
                Side(i,a,b,doors[i],environment.transform); Side(i,b,c,doors[i],environment.transform); Side(i,c,d,doors[i],environment.transform); Side(i,d,a,doors[i],environment.transform);
            }
            for(int i=10;i<15;i++)
            {
                var point=points[i]; Vector3 entrance=positions[point.EntryRoom-1]+new Vector3(0,0,1.5f);
                point.transform.position=entrance; point.arrivalRadius=0;
                Vector3 exit=positions[point.ExitRoom-1]+new Vector3(0,0,-1.5f);
                var route=new List<Transform>();
                foreach(var position in new[]{entrance+Vector3.up*4,exit+Vector3.up*4,exit})
                {
                    var step=new GameObject("Vent "+point.id+" route "+route.Count); step.transform.SetParent(transform,false); step.transform.position=position; route.Add(step.transform);
                }
                point.ventRoute=route.ToArray();
                var marker=Box("Vent "+point.id+" entrance",entrance+Vector3.up*.03f,new Vector3(.7f,.06f,.7f),transform);
                marker.layer=2; DestroyCollider(marker.GetComponent<Collider>());
            }
            surface=environment.AddComponent<NavMeshSurface>(); surface.collectObjects=CollectObjects.Children;
            surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders; Physics.SyncTransforms(); surface.BuildNavMesh();
            var actor=GameObject.CreatePrimitive(PrimitiveType.Capsule); actor.name="Enemy (select to inspect action)"; actor.layer=2;
            actor.transform.SetParent(transform,false); actor.transform.position=positions[0];
            actor.GetComponent<CapsuleCollider>().center=Vector3.up;
            var renderer=actor.GetComponent<Renderer>(); renderer.enabled=false;
            var visual=GameObject.CreatePrimitive(PrimitiveType.Capsule); visual.name="Enemy visual"; visual.layer=2; visual.transform.SetParent(actor.transform,false); visual.transform.localPosition=Vector3.up;
            DestroyCollider(visual.GetComponent<Collider>());
            var agent=actor.AddComponent<NavMeshAgent>(); agent.radius=.35f; agent.height=2; agent.baseOffset=0; agent.acceleration=12; agent.angularSpeed=240;
            enemy=actor.AddComponent<EnemyPatrolBrain>(); enemy.points=points; enemy.spawnPoint=points[0];
            var eye=new GameObject("Eyes"); eye.transform.SetParent(actor.transform,false); eye.transform.localPosition=Vector3.up*1.5f; enemy.eye=eye.transform;
            var playerObject=GameObject.CreatePrimitive(PrimitiveType.Capsule); playerObject.name="Player target (WASD or arrow keys)"; playerObject.layer=2;
            playerObject.transform.SetParent(transform,false); playerObject.transform.position=positions[2]+Vector3.up;
            player=playerObject.AddComponent<EnemyTarget>(); player.sightPoint=playerObject.transform;
            var controller=playerObject.AddComponent<CharacterController>(); controller.radius=.35f; controller.height=2;
            DestroyCollider(playerObject.GetComponent<CapsuleCollider>());
            playerObject.AddComponent<EnemyDemoPlayer>();
            var cameraObject=new GameObject("Demo camera"); cameraObject.transform.SetParent(transform,false); cameraObject.tag="MainCamera";
            cameraObject.transform.position=new Vector3(-3,65,-2); cameraObject.transform.rotation=Quaternion.Euler(90,0,0);
            var camera=cameraObject.AddComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=32; camera.farClipPlane=120;
            var lightObject=new GameObject("Demo light"); lightObject.transform.SetParent(transform,false); lightObject.transform.rotation=Quaternion.Euler(55,-30,0);
            var light=lightObject.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.3f;
            buildOnStart=false;
        }
        private void DestroyCollider(Collider collider)
        { if(collider==null) return; collider.enabled=false; if(Application.isPlaying) Destroy(collider); else DestroyImmediate(collider); }
    }
}
