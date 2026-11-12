using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Asylum.Enemies.Editor
{
    [InitializeOnLoad]
    public static class EnemyPatrolPlayVerification
    {
        private const string Running="Asylum.EnemyPlayVerification";
        private static EnemyPatrolDemo demo;
        private static int stage, vent=10, captures, lastFrame;
        private static float began;
        private static bool moved;
        private static float chaseSince=-1;
        private static readonly MethodInfo Wait=typeof(EnemyPatrolBrain).GetMethod("WaitAt",BindingFlags.Instance|BindingFlags.NonPublic);
        static EnemyPatrolPlayVerification() { EditorApplication.update+=Tick; }
        public static void Run()
        {
            EnemyPatrolDemoBuilder.BuildAndVerify();
            SessionState.SetBool(Running,true);
            SessionState.SetFloat(Running+"Deadline",(float)EditorApplication.timeSinceStartup+180);
            EditorApplication.EnterPlaymode();
        }
        private static void Next(int value)
        { Debug.Log("ENEMY_PLAY_STAGE_PASS "+stage); stage=value; began=Time.time; }
        private static void PlacePlayer(Vector3 position,bool alive)
        {
            var controller=demo.player.GetComponent<CharacterController>(); controller.enabled=false;
            demo.player.transform.position=position+Vector3.up*1.2f; controller.enabled=true; demo.player.alive=alive;
            // Exercise a real grounded controller, including its skin-width offset.
            controller.Move(Vector3.down*.4f);
            Physics.SyncTransforms();
        }
        private static void At(EnemyPatrolPoint point)
        {
            var agent=demo.enemy.GetComponent<NavMeshAgent>();
            agent.enabled=true;
            if(!agent.Warp(point.transform.position)) throw new Exception("Test warp failed "+point.id);
            demo.enemy.transform.rotation=Quaternion.identity;
            Wait.Invoke(demo.enemy,new object[]{point});
        }
        private static void Tick()
        {
            if(!SessionState.GetBool(Running,false)) return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Running+"Deadline",0)) throw new Exception("Play verification real-time timeout");
                if(!EditorApplication.isPlaying || Time.frameCount<5 || lastFrame==Time.frameCount) return;
                lastFrame=Time.frameCount;
                if(demo==null)
                {
                    demo=UnityEngine.Object.FindFirstObjectByType<EnemyPatrolDemo>();
                    if(demo==null || demo.enemy==null) throw new Exception("No demo actor");
                    Time.timeScale=5;
                    demo.enemy.disableOnDeparture=false;
                    demo.enemy.onCapture.AddListener(()=>captures++);
                    demo.player.alive=false;
                    demo.points[0].connections.Clear(); demo.points[0].connections.Add(new PatrolConnection(demo.points[1],PatrolDirection.Forward,1));
                    demo.points[0].baseWaitSeconds=0; demo.points[1].baseWaitSeconds=30;
                    At(demo.points[0]); began=Time.time;
                }
                if(Time.time-began>100) throw new Exception("Stage "+stage+" timeout; action="+demo.enemy.Action+" room="+demo.enemy.CurrentRoom+" position="+demo.enemy.transform.position);
                var brain=demo.enemy;
                switch(stage)
                {
                    case 0:
                        moved|=Vector3.Distance(brain.transform.position,demo.points[0].transform.position)>1;
                        if(brain.CurrentPoint==demo.points[1] && brain.Action==EnemyAction.Waiting)
                        { if(!moved) throw new Exception("Patrol did not move"); Next(1); }
                        break;
                    case 1:
                        At(demo.points[1]); PlacePlayer(demo.points[1].transform.position+Vector3.forward*2.5f,true); Next(2); break;
                    case 2:
                        if(brain.Action!=EnemyAction.Chase) chaseSince=-1;
                        else
                        {
                            if(chaseSince<0) chaseSince=Time.time;
                            if(Time.time-chaseSince>.4f && Vector3.Distance(brain.transform.position,demo.points[1].transform.position)>.4f)
                            { demo.player.alive=false; Next(3); }
                        }
                        break;
                    case 3:
                        if(brain.Action==EnemyAction.LastSeen) Next(4); break;
                    case 4:
                        if(brain.Action==EnemyAction.Waiting && brain.CurrentPoint==demo.points[1])
                        { At(demo.points[1]); PlacePlayer(demo.points[1].transform.position+Vector3.forward*.5f,true); Next(5); } break;
                    case 5:
                        if(captures>0)
                        { if(demo.player.alive) throw new Exception("Captured player still alive"); Next(6); } break;
                    case 6:
                        if(brain.CurrentPoint==demo.points[0] && brain.Action!=EnemyAction.ReturnToSpawn) Next(7); break;
                    case 7:
                        demo.player.alive=false; demo.points[vent].baseWaitSeconds=0;
                        At(demo.points[vent]); Next(8); break;
                    case 8:
                        if(brain.Action==EnemyAction.Vent)
                        {
                            if(brain.CurrentRoom!=0 || brain.GetComponent<NavMeshAgent>().enabled) throw new Exception("Vent did not suspend room/navigation");
                            PlacePlayer(demo.points[vent].transform.position+Vector3.forward*.5f,true); Next(9);
                        } break;
                    case 9:
                        if(brain.Action==EnemyAction.Vent)
                        {
                            if(captures!=1) throw new Exception("Capture occurred inside vent");
                            if(Time.time-began>.25f) demo.player.alive=false;
                        }
                        else if(brain.CurrentRoom==demo.points[vent].ExitRoom)
                        {
                            Debug.Log("ENEMY_PLAY_VENT_PASS "+(vent+1));
                            if(++vent<15) Next(7); else Next(10);
                        } break;
                    case 10:
                        if(brain.Action==EnemyAction.Departed)
                        {
                            if(Time.frameCount<30) throw new Exception("Insufficient advancing frames");
                            Debug.Log("ENEMY_PLAY_VERIFY_PASS patrol, vision, last-seen, recovery, capture, spawn return, five vents, departure; frames="+Time.frameCount);
                            SessionState.SetBool(Running,false); EditorApplication.Exit(0);
                        } break;
                }
            }
            catch(Exception error)
            {
                Debug.LogException(error); SessionState.SetBool(Running,false); EditorApplication.Exit(1);
            }
        }
    }
}
