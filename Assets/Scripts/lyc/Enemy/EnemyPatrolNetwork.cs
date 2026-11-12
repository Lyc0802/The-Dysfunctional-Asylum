using UnityEngine;

namespace Asylum.Enemies
{
    public class EnemyPatrolNetwork : MonoBehaviour
    {
        [Tooltip("Point IDs 1 through 15, in order.")]
        public EnemyPatrolPoint[] points = new EnemyPatrolPoint[15];
        [ContextMenu("Apply agreed 15-point graph and waiting times")]
        public void ApplyDefaults()
        {
            if(points==null || points.Length!=15) { Debug.LogError("Assign exactly 15 points in ID order.",this); return; }
            for(int i=0;i<points.Length;i++)
            {
                if(points[i]==null) { Debug.LogError("Missing point "+(i+1),this); return; }
                for(int j=0;j<i;j++) if(points[j]==points[i]) { Debug.LogError("Duplicate point reference.",this); return; }
            }
            EnemyPatrolDefaults.Apply(points);
        }
    }
}
