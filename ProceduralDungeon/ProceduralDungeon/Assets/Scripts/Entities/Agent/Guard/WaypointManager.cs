using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Guard
{
    [Serializable]
    public class WaypointManager
    {
        public List<Vector2> waypoints = new();
        public float minimumDistanceToWaypoint;
        public WaypointLoopType loopType;
        public bool direction = true;
        
        [Serializable]
        public enum WaypointLoopType
        {
            Loop,
            PingPong
        }
        
        public bool OnValidate() => this.waypoints.Count >= 2 && this.minimumDistanceToWaypoint > 0f;
        
        public void OnDrawGizmosSelected()
        {
            if (!OnValidate())
                return;
            Gizmos.color = Color.green;
            Vector2? lastPos = this.loopType == WaypointLoopType.Loop ? this.waypoints.Last() : null;
            foreach (Vector2 waypoint in this.waypoints)
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
            int index = this.waypoints.FindIndex(a => a == targetPosition.Value);
            int nextIndex = this.direction ? index + 1 : index - 1;

            switch (this.loopType)
            {
                case WaypointLoopType.PingPong when (nextIndex < 0 || nextIndex >= this.waypoints.Count):
                    this.direction = !this.direction;
                    nextIndex = Math.Clamp(nextIndex, 0, this.waypoints.Count - 1);
                    break;
                case WaypointLoopType.Loop: 
                    if (nextIndex >= this.waypoints.Count && this.direction)
                        nextIndex = 0;
                    if (nextIndex < 0 && !this.direction)
                        nextIndex = this.waypoints.Count - 1;
                    break;
                default:
                    nextIndex = Math.Clamp(nextIndex, 0, this.waypoints.Count - 1);
                    break;
            }
            
            return this.waypoints[nextIndex];
        }

        public Vector2 GetNearestWayPoint(Vector2 position)
        {
            Vector2 closetsPoint = this.waypoints[0];
            float closetsDist = Vector2.Distance(this.waypoints[0], position);
            
            for (int i = 1; i < this.waypoints.Count; i++)
            {
                Vector2 waypoint = this.waypoints[i];
                float dist = Vector2.Distance(waypoint, position);
                if (!(dist < closetsDist)) continue;
                closetsPoint = waypoint;
                closetsDist = dist;
            }
            
            return closetsPoint;
        }
    }
}