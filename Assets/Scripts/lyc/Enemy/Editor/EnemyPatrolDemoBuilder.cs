using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Asylum.Enemies.Editor
{
    public static class EnemyPatrolDemoBuilder
    {
        [MenuItem("Tools/Asylum/Create Enemy Patrol Test Scene")]
        public static void CreateScene()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAndVerify();
        }
        public static void BuildAndVerify()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("Enemy Patrol Test");
            var demo=root.AddComponent<EnemyPatrolDemo>(); demo.Build();
            const string folder="Assets/Scenes/EnemyPatrolTestData";
            if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Scenes","EnemyPatrolTestData");
            var data=demo.surface.navMeshData;
            string dataPath=folder+"/DemoNavMesh.asset";
            // Generate a new asset path when rebuilding, preserving existing assets.
            AssetDatabase.CreateAsset(data,AssetDatabase.GenerateUniqueAssetPath(dataPath));
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/EnemyPatrolTest.unity");
            AssetDatabase.SaveAssets();
            Verify(demo);
        }
        public static void Verify(EnemyPatrolDemo demo)
        {
            int checks=0;
            Action<bool,string> check=(ok,message)=> { if(!ok) throw new Exception(message); checks++; };
            check(demo.points.Length==15,"Expected 15 points");
            var path=new NavMeshPath();
            foreach(var point in demo.points)
            {
                check(Mathf.Approximately(point.baseWaitSeconds,EnemyPatrolDefaults.Waits[point.id-1]),"Incorrect wait: "+point.id);
                if(point.IsVent)
                {
                    check(point.EntryRoom>0 && point.ExitRoom>0,"Invalid vent endpoints: "+point.id);
                    var exit=point.ventRoute[point.ventRoute.Length-1].position;
                    check(EnemyRoomVolume.FindRoom(exit)==point.ExitRoom,"Vent exit outside room: "+point.id);
                    check(NavMesh.SamplePosition(exit,out var ventHit,1,NavMesh.AllAreas),"Vent exit off NavMesh: "+point.id);
                    continue;
                }
                check(EnemyRoomVolume.FindRoom(point.transform.position)==point.id,"Room tracking failed: "+point.id);
                check(NavMesh.SamplePosition(point.transform.position,out var start,1,NavMesh.AllAreas),"Point off NavMesh: "+point.id);
                foreach(var link in point.connections)
                {
                    check(NavMesh.SamplePosition(link.destination.transform.position,out var end,1,NavMesh.AllAreas),"Destination off NavMesh");
                    bool complete=NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
                    if(!complete)
                    {
                        Debug.Log("PATH_DIAGNOSTIC "+point.id+" -> "+link.destination.id+" status="+path.status+" corners="+string.Join(";",Array.ConvertAll(path.corners,p=>p.ToString())));
                        for(int sample=0;sample<=30;sample++)
                        {
                            var position=Vector3.Lerp(start.position,end.position,sample/30f);
                            if(!NavMesh.SamplePosition(position,out var nearby,.3f,NavMesh.AllAreas)) Debug.Log("NO_NAV_NEAR "+position);
                        }
                    }
                    check(complete,"Unreachable walking edge "+point.id+" -> "+link.destination.id);
                }
            }
            check(demo.points[9].IsAdjacentRoom(6)&&demo.points[9].IsAdjacentRoom(8),"Zero weight adjacency lost");
            check(!demo.points[0].IsAdjacentRoom(5),"Vent erroneously grants visual adjacency");
            Debug.Log("ENEMY_PATROL_VERIFY_PASS checks="+checks);
        }
    }
}
