using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Asylum.Enemies
{
    public class EnemyTarget : MonoBehaviour
    {
        public static readonly List<EnemyTarget> Active = new List<EnemyTarget>();
        public Transform sightPoint;
        [Tooltip("Optional feet/ground anchor used for navigation and capture distance.")]
        public Transform navigationPoint;
        public bool alive = true;
        public UnityEvent onCaptured = new UnityEvent();
        public Vector3 SightPosition => sightPoint != null ? sightPoint.position : transform.position + Vector3.up;
        public Vector3 NavigationPosition
        {
            get
            {
                if (navigationPoint != null) return navigationPoint.position;
                var controller = GetComponent<CharacterController>();
                if (controller != null && controller.enabled)
                    return controller.transform.TransformPoint(controller.center - Vector3.up * (controller.height * .5f));
                var body = GetComponent<Collider>();
                if (body != null && body.enabled && !body.isTrigger)
                {
                    var bounds = body.bounds;
                    return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                }
                return transform.position;
            }
        }
        public int CurrentRoom => EnemyRoomVolume.FindRoom(transform.position);
        private void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
        private void OnDisable() => Active.Remove(this);
        public void Capture() { if (!alive) return; alive = false; onCaptured.Invoke(); }
    }
}
