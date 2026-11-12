using System.Collections.Generic;
using UnityEngine;

namespace Asylum.Enemies
{
    [ExecuteAlways]
    public class EnemyRoomVolume : MonoBehaviour
    {
        public static readonly List<EnemyRoomVolume> Active = new List<EnemyRoomVolume>();
        [Range(1, 10)] public int roomId = 1;
        public BoxCollider bounds;
        private void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
        private void OnDisable() => Active.Remove(this);
        public bool Contains(Vector3 position)
        {
            if (bounds == null || !bounds.enabled) return false;
            Vector3 p = bounds.transform.InverseTransformPoint(position) - bounds.center;
            Vector3 half = bounds.size * .5f;
            return Mathf.Abs(p.x) <= half.x && Mathf.Abs(p.y) <= half.y && Mathf.Abs(p.z) <= half.z;
        }
        public static int FindRoom(Vector3 position)
        {
            foreach (var room in Active) if (room.Contains(position)) return room.roomId;
            return 0;
        }
    }
}
