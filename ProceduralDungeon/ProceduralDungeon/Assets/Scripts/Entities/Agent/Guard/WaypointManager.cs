using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Guard
{
    [Serializable]
    public class WaypointManager
    {
        public List<Vector2> waypoints;
        public float minimumDistanceToWaypoint;
        public WaypointLoopType loopType;
        public bool direction = true;
        
        [Serializable]
        public enum WaypointLoopType
        {
            Loop,
            PingPong
        }
        
        public bool OnValidate() => waypoints.Count >= 2 && minimumDistanceToWaypoint > 0f;
        
        public void OnDrawGizmosSelected()
        {
            if (!OnValidate())
                return;
            Gizmos.color = Color.green;
            Vector2? lastPos = loopType == WaypointLoopType.Loop ? waypoints.Last() : null;
            foreach (var waypoint in waypoints)
            {
                Gizmos.DrawSphere(waypoint, 0.1f);
                if (lastPos != null)
                    Gizmos.DrawLine(waypoint, lastPos.Value);
                lastPos = waypoint;
            }
        }

        public Vector2 GetNextWaypoint(Vector2? targetPosition)
        {
            if (targetPosition == null)
                return Vector2.zero;
            var index = waypoints.FindIndex(a => a == targetPosition.Value);
            var nextIndex = direction ? index + 1 : index - 1;

            switch (loopType)
            {
                case WaypointLoopType.PingPong when (nextIndex < 0 || nextIndex >= waypoints.Count):
                    direction = !direction;
                    nextIndex = Math.Clamp(nextIndex, 0, waypoints.Count - 1);
                    break;
                case WaypointLoopType.Loop: 
                    if (nextIndex >= waypoints.Count && direction)
                        nextIndex = 0;
                    if (nextIndex < 0 && !direction)
                        nextIndex = waypoints.Count - 1;
                    break;
                default:
                    nextIndex = Math.Clamp(nextIndex, 0, waypoints.Count - 1);
                    break;
            }
            
            return waypoints[nextIndex];
        }

        public Vector2 GetNearestWayPoint(Vector2 position)
        {
            var closetsPoint = waypoints[0];
            var closetsDist = Vector2.Distance(waypoints[0], position);
            
            for (var i = 1; i < waypoints.Count; i++)
            {
                var waypoint = waypoints[i];
                var dist = Vector2.Distance(waypoint, position);
                if (!(dist < closetsDist)) continue;
                closetsPoint = waypoint;
                closetsDist = dist;
            }
            
            return closetsPoint;
        }
    }
}