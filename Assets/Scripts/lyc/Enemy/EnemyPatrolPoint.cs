using System;
using System.Collections.Generic;
using UnityEngine;

namespace Asylum.Enemies
{
    public enum PatrolDirection { Forward, Backward, Same, Vent }
    [Serializable] public class PatrolConnection
    {
        public EnemyPatrolPoint destination;
        public PatrolDirection direction;
        [Min(0)] public float weight = 1;
        public PatrolConnection(EnemyPatrolPoint point, PatrolDirection kind, float value)
        { destination = point; direction = kind; weight = value; }
    }

    public class EnemyPatrolPoint : MonoBehaviour
    {
        [Range(1, 15)] public int id = 1;
        [Min(0)] public float baseWaitSeconds = 3;
        [Min(0)] public float arrivalRadius = .25f;
        public List<PatrolConnection> connections = new List<PatrolConnection>();
        [Tooltip("Ordered pipe route, ending on walkable ground in the exit room.")]
        public Transform[] ventRoute = Array.Empty<Transform>();
        public bool IsVent => id > 10;
        public bool IsTerminal => id == 10;
        public int EntryRoom => IsVent ? LinkedRoom(PatrolDirection.Backward) : id;
        public int ExitRoom => LinkedRoom(PatrolDirection.Forward);
        private int LinkedRoom(PatrolDirection direction)
        {
            foreach (var link in connections)
                if (link.direction == direction && link.destination != null) return link.destination.id;
            return 0;
        }
        public bool IsAdjacentRoom(int room)
        {
            // A vent exit is not a line-of-sight connection.
            if (IsVent) return false;
            foreach (var link in connections)
                if (link.destination != null && !link.destination.IsVent && link.destination.id != id && link.destination.id == room) return true;
            return false;
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsVent ? Color.cyan : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, arrivalRadius);
            foreach (var link in connections)
                if (link.destination != null) Gizmos.DrawLine(transform.position, link.destination.transform.position);
        }
    }
}
