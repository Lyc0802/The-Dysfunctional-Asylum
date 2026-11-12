using System;
using UnityEngine;

namespace Asylum.Enemies
{
    public static class EnemyPatrolDefaults
    {
        public static readonly float[] Waits = { 5,2,2,2,3,5,5,8,5,0,0,0,5,5,5 };
        // Zero-weight edges remain room adjacency data. Terminal 10 never draws a destination.
        private static readonly string[] Links = {
            "2F15,11V3", "1B5,3F15,12V3", "2B5,4F10,5F10",
            "3B10,5S10,6F10", "3B5,4S10,7F15", "4B5,9F10,10F15,15V5",
            "5B5,8F15,13V5", "7B5,9F10,10F15", "6S10,8S10,14V10",
            "6B0,8B0", "1B0,5F1", "2B0,7F1", "7B0,10F1", "9B0,10F1", "6B0,10F1" };
        public static void Apply(EnemyPatrolPoint[] points)
        {
            if (points.Length != 15) throw new ArgumentException("Exactly 15 points are required.");
            for (int i=0; i<15; i++)
            {
                var point = points[i]; point.id = i+1; point.baseWaitSeconds = Waits[i];
                point.connections.Clear();
                foreach (string entry in Links[i].Split(','))
                {
                    int at = entry.IndexOfAny(new[] {'F','B','S','V'});
                    int destination = int.Parse(entry.Substring(0,at));
                    var direction = entry[at]=='F' ? PatrolDirection.Forward : entry[at]=='B' ? PatrolDirection.Backward : entry[at]=='V' ? PatrolDirection.Vent : PatrolDirection.Same;
                    point.connections.Add(new PatrolConnection(points[destination-1], direction, float.Parse(entry.Substring(at+1), System.Globalization.CultureInfo.InvariantCulture)));
                }
            }
        }
    }
}
