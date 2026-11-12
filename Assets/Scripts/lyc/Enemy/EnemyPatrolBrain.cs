using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Asylum.Enemies
{
    public enum EnemyAction { Waiting, Patrol, Chase, LastSeen, ReturnToPoint, Vent, ReturnToSpawn, Departed }

    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyPatrolBrain : MonoBehaviour
    {
        public EnemyPatrolPoint[] points;
        public EnemyPatrolPoint spawnPoint;
        [Header("Enemy type base values")]
        [Min(.01f)] public float walkSpeed = 2;
        [Min(.01f)] public float ventSpeed = 3;
        [Min(0)] public float ventWeight = 1;
        [Header("Difficulty (sampled once on start)")]
        [Range(0,10)] public int aiLevel = 1;
        [Min(.05f)] public float minimumWait = .25f;
        [Min(1)] public float maximumSpeedMultiplier = 2;
        [Tooltip("Reserved for future dynamic difficulty; currently has no effect.")]
        public float aggression;
        [Header("Vision")]
        public Transform eye;
        [Min(.1f)] public float sightDistance = 15;
        [Range(1,360)] public float fieldOfView = 110;
        [Tooltip("Include walls and closed doors. Exclude enemy/player bodies.")]
        public LayerMask obstructionMask = 1;
        [Min(.05f)] public float visionInterval = .15f;
        [Min(.1f)] public float captureDistance = 1;
        [Header("Navigation")]
        [Min(.05f)] public float arrivalTolerance = .3f;
        [Min(.1f)] public float stuckSeconds = 5;
        [Min(.1f)] public float retrySeconds = 1;
        public UnityEvent onCapture = new UnityEvent();
        public UnityEvent onDeparted = new UnityEvent();
        public bool disableOnDeparture = true;
        [SerializeField] private EnemyAction action;
        [SerializeField] private int currentRoom;
        public EnemyAction Action => action;
        public int CurrentRoom => currentRoom;
        public EnemyPatrolPoint CurrentPoint { get; private set; }
        public EnemyPatrolPoint TargetPoint { get; private set; }
        public Vector3 LastSeenPosition { get; private set; }

        private NavMeshAgent agent;
        private EnemyTarget target;
        private float speedMultiplier, forwardMultiplier, waitMultiplier;
        private float waitUntil, nextVision, nextRepath, lastProgress, bestRemaining;
        private Vector3 progressPosition;
        private bool navigating, recovering;
        private int ventIndex;
        private EnemyPatrolPoint ventEntry;
        private readonly List<EnemyPatrolPoint> options = new List<EnemyPatrolPoint>();
        private readonly List<Vector3> destinations = new List<Vector3>();
        private readonly List<float> weights = new List<float>();
        private NavMeshPath path;
        private Vector3 spawnPosition;

        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            path = new NavMeshPath();
            if (points == null || points.Length == 0 || spawnPoint == null)
            { Debug.LogError("Assign patrol points and spawn point.", this); enabled=false; return; }
            int level = Mathf.Clamp(aiLevel,0,10);
            speedMultiplier = Mathf.Min(Mathf.Max(1, maximumSpeedMultiplier), 1 + level*.08f);
            forwardMultiplier = 1 + level*.15f;
            waitMultiplier = 1 / (1 + level*.15f);
            agent.speed = Mathf.Max(.01f,walkSpeed) * speedMultiplier;
            agent.stoppingDistance = .1f;
            agent.autoTraverseOffMeshLink = false;
            var body = GetComponent<Rigidbody>(); if (body != null) body.isKinematic = true;
            spawnPosition = spawnPoint.transform.position;
            if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position,out var hit,2,agent.areaMask)) agent.Warp(hit.position);
            if (!agent.isOnNavMesh) { Debug.LogError("Enemy is not on a baked NavMesh.",this); enabled=false; return; }
            currentRoom = EnemyRoomVolume.FindRoom(transform.position);
            if (Navigate(spawnPosition)) { action=EnemyAction.ReturnToSpawn; TargetPoint=spawnPoint; }
            else WaitAt(spawnPoint);
        }

        private EnemyPatrolPoint RoomPoint(int room)
        { foreach(var point in points) if(point != null && !point.IsVent && point.id==room) return point; return null; }

        private void Update()
        {
            if (action==EnemyAction.Departed) return;
            currentRoom = action==EnemyAction.Vent ? 0 : EnemyRoomVolume.FindRoom(transform.position);
            if (action==EnemyAction.Vent) { UpdateVent(); return; }
            if (!agent.enabled || !agent.isOnNavMesh) return;
            if (action!=EnemyAction.ReturnToSpawn && Time.time>=nextVision)
            {
                nextVision=Time.time+Mathf.Max(.05f,visionInterval);
                EnemyTarget visible = FindVisible();
                if (visible != null)
                {
                    target=visible; LastSeenPosition=visible.NavigationPosition;
                    if(action!=EnemyAction.Chase) { action=EnemyAction.Chase; nextRepath=0; }
                    if(Vector3.Distance(transform.position-Vector3.up*agent.baseOffset,visible.NavigationPosition)<=captureDistance)
                    { CaptureTarget(); return; }
                }
                else if(action==EnemyAction.Chase) { action=EnemyAction.LastSeen; nextRepath=0; }
            }
            if(action==EnemyAction.Chase || action==EnemyAction.LastSeen)
            {
                if(Time.time>=nextRepath)
                {
                    nextRepath=Time.time+.25f;
                    if(!Navigate(LastSeenPosition, false)) { Recover(); return; }
                }
                if(action==EnemyAction.LastSeen && Arrived()) { Recover(); return; }
            }
            if(navigating && Stuck()) { Recover(); return; }
            if(action==EnemyAction.Waiting)
            {
                if(Time.time<waitUntil) return;
                if(recovering) { Recover(); return; }
                if(CurrentPoint.IsVent) BeginVent(); else ChooseNext();
            }
            else if((action==EnemyAction.Patrol || action==EnemyAction.ReturnToPoint || action==EnemyAction.ReturnToSpawn) && Arrived())
                WaitAt(TargetPoint);
        }

        private EnemyTarget FindVisible()
        {
            var room = RoomPoint(currentRoom);
            if(room==null) return null;
            EnemyTarget nearest=null; float best=float.PositiveInfinity;
            Vector3 origin=eye!=null ? eye.position : transform.position+Vector3.up*1.5f;
            Vector3 forward=eye!=null ? eye.forward : transform.forward;
            foreach(var player in EnemyTarget.Active)
            {
                if(!player.alive) continue;
                int playerRoom=player.CurrentRoom;
                if(playerRoom!=currentRoom && !room.IsAdjacentRoom(playerRoom)) continue;
                Vector3 delta=player.SightPosition-origin;
                if(delta.sqrMagnitude> sightDistance*sightDistance || delta.sqrMagnitude>=best) continue;
                if(Vector3.Angle(forward,delta)>fieldOfView*.5f) continue;
                if(Physics.Linecast(origin,player.SightPosition,obstructionMask,QueryTriggerInteraction.Ignore)) continue;
                best=delta.sqrMagnitude; nearest=player;
            }
            return nearest;
        }

        private void CaptureTarget()
        {
            // Change state first: event listeners may disable actors or update gameplay.
            var captured=target; target=null; Stop(); action=EnemyAction.ReturnToSpawn;
            captured.Capture(); onCapture.Invoke(); TargetPoint=spawnPoint;
            if(!Navigate(spawnPosition)) { agent.Warp(spawnPosition); WaitAt(spawnPoint); }
        }
        private bool Reachable(Vector3 candidate, out Vector3 destination)
        {
            destination=default;
            if(!NavMesh.SamplePosition(candidate,out var hit,1,agent.areaMask)) return false;
            if(!agent.CalculatePath(hit.position,path) || path.status!=NavMeshPathStatus.PathComplete) return false;
            destination=hit.position; return true;
        }
        private bool Navigate(Vector3 destination, bool resetProgress=true)
        {
            if(!Reachable(destination,out var sampled)) return false;
            agent.isStopped=false;
            if(!agent.SetDestination(sampled)) return false;
            if(!navigating || resetProgress) { progressPosition=transform.position; lastProgress=Time.time; bestRemaining=float.PositiveInfinity; }
            navigating=true; return true;
        }
        private bool Arrived() => navigating && !agent.pathPending && agent.pathStatus==NavMeshPathStatus.PathComplete && agent.remainingDistance<=Mathf.Max(arrivalTolerance,agent.stoppingDistance);
        private bool Stuck()
        {
            if(agent.pathPending) return Time.time-lastProgress>stuckSeconds;
            float remaining=agent.remainingDistance;
            if((transform.position-progressPosition).sqrMagnitude>.01f && remaining<bestRemaining-.03f)
            { lastProgress=Time.time; progressPosition=transform.position; bestRemaining=remaining; }
            return Time.time-lastProgress>Mathf.Max(.1f,stuckSeconds);
        }
        private void Stop() { navigating=false; if(agent.enabled && agent.isOnNavMesh) { agent.ResetPath(); agent.isStopped=true; } }
        private void WaitAt(EnemyPatrolPoint point)
        {
            Stop(); CurrentPoint=point; TargetPoint=null; recovering=false;
            if(point==null) { Recover(); return; }
            if(point.IsTerminal)
            {
                action=EnemyAction.Departed; onDeparted.Invoke();
                if(disableOnDeparture) gameObject.SetActive(false);
                return;
            }
            action=EnemyAction.Waiting;
            waitUntil=Time.time+(point.baseWaitSeconds<=0 ? 0 : Mathf.Max(minimumWait,point.baseWaitSeconds*waitMultiplier));
        }
        private bool DestinationFor(EnemyPatrolPoint point,out Vector3 destination)
        {
            for(int attempt=0; attempt<6; attempt++)
            {
                Vector2 random=Random.insideUnitCircle*point.arrivalRadius;
                Vector3 candidate=point.transform.position+(point.IsVent ? Vector3.zero : new Vector3(random.x,0,random.y));
                if(Reachable(candidate,out destination) && EnemyRoomVolume.FindRoom(destination)==point.EntryRoom) return true;
            }
            return Reachable(point.transform.position,out destination) && EnemyRoomVolume.FindRoom(destination)==point.EntryRoom;
        }
        private void ChooseNext()
        {
            options.Clear(); destinations.Clear(); weights.Clear();
            foreach(var edge in CurrentPoint.connections)
            {
                if(edge.destination==null || edge.weight<=0) continue;
                float weight=PatrolWeights.Evaluate(edge.weight,edge.direction==PatrolDirection.Vent,edge.direction==PatrolDirection.Forward,ventWeight,forwardMultiplier);
                if(weight<=0 || !DestinationFor(edge.destination,out var destination)) continue;
                options.Add(edge.destination); destinations.Add(destination); weights.Add(weight);
            }
            if(options.Count==0) { waitUntil=Time.time+Mathf.Max(.1f,retrySeconds); return; }
            int chosen=PatrolWeights.Choose(weights,Random.value);
            if(chosen<0) { waitUntil=Time.time+Mathf.Max(.1f,retrySeconds); return; }
            TargetPoint=options[chosen]; action=EnemyAction.Patrol;
            if(!Navigate(destinations[chosen])) Recover();
        }
        private void Recover()
        {
            target=null; Stop(); recovering=false;
            EnemyPatrolPoint nearest=null; Vector3 destination=default; float best=float.PositiveInfinity;
            var roomPoint=RoomPoint(currentRoom);
            if(roomPoint!=null && Reachable(roomPoint.transform.position,out destination)) nearest=roomPoint;
            else foreach(var point in points)
            {
                if(point==null || point.IsVent || !Reachable(point.transform.position,out var sampled)) continue;
                float length=0; var corners=path.corners;
                for(int i=1;i<corners.Length;i++) length+=Vector3.Distance(corners[i-1],corners[i]);
                if(length<best) { best=length; nearest=point; destination=sampled; }
            }
            if(nearest!=null && Navigate(destination)) { TargetPoint=nearest; action=EnemyAction.ReturnToPoint; }
            else { action=EnemyAction.Waiting; recovering=true; waitUntil=Time.time+Mathf.Max(.1f,retrySeconds); }
        }
        private void BeginVent()
        {
            if(CurrentPoint.ventRoute==null || CurrentPoint.ventRoute.Length==0)
            { waitUntil=Time.time+Mathf.Max(.1f,retrySeconds); return; }
            foreach(var step in CurrentPoint.ventRoute) if(step==null) { waitUntil=Time.time+retrySeconds; return; }
            Vector3 exitPosition=CurrentPoint.ventRoute[CurrentPoint.ventRoute.Length-1].position;
            if(!NavMesh.SamplePosition(exitPosition,out var exitHit,1,agent.areaMask) ||
                EnemyRoomVolume.FindRoom(exitHit.position)!=CurrentPoint.ExitRoom)
            {
                // Do not commit to a pipe whose authored exit cannot rejoin navigation.
                waitUntil=Time.time+Mathf.Max(.1f,retrySeconds); return;
            }
            ventEntry=CurrentPoint; ventIndex=0; target=null; Stop(); agent.enabled=false;
            action=EnemyAction.Vent; currentRoom=0;
        }
        private void UpdateVent()
        {
            Vector3 end=ventEntry.ventRoute[ventIndex].position;
            Vector3 delta=end-transform.position;
            transform.position=Vector3.MoveTowards(transform.position,end,Mathf.Max(.01f,ventSpeed)*speedMultiplier*Time.deltaTime);
            if(delta.sqrMagnitude>.001f) transform.rotation=Quaternion.LookRotation(delta.normalized, Mathf.Abs(delta.normalized.y)>.99f ? Vector3.forward : Vector3.up);
            if(Vector3.Distance(transform.position,end)>.02f) return;
            if(++ventIndex<ventEntry.ventRoute.Length) return;
            var exit=RoomPoint(ventEntry.ExitRoom);
            Vector3 position=transform.position;
            if(exit==null || !NavMesh.SamplePosition(position,out var hit,1,agent.areaMask) || EnemyRoomVolume.FindRoom(hit.position)!=exit.id)
            { ventIndex=ventEntry.ventRoute.Length-1; return; }
            agent.enabled=true;
            if(!agent.Warp(hit.position)) { agent.enabled=false; ventIndex--; return; }
            currentRoom=exit.id;
            // Avoid retaining the vertical pipe orientation after rejoining navigation.
            transform.rotation=Quaternion.Euler(0,transform.eulerAngles.y,0);
            TargetPoint=exit; action=EnemyAction.Patrol; nextVision=0;
            if(!Navigate(exit.transform.position)) Recover();
        }
    }
}
