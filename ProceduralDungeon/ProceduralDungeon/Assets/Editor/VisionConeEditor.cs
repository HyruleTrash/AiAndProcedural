using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VisionCone))]
public class VisionConeEditor : Editor
{
    private void OnSceneGUI()
    {
        VisionCone visionCone = (VisionCone)this.target;
        Handles.color = Color.white;
        Handles.DrawWireArc(visionCone.transform.position, Vector3.forward, Vector3.right, 360, visionCone.ViewDistance);

        Vector3 viewAngleA = visionCone.DirFromAngle(-visionCone.FieldOfView / 2, false);
        Vector3 viewAngleB = visionCone.DirFromAngle(visionCone.FieldOfView / 2, false);
        
        Handles.DrawLine(visionCone.transform.position, visionCone.transform.position + viewAngleA * visionCone.ViewDistance);
        Handles.DrawLine(visionCone.transform.position, visionCone.transform.position + viewAngleB * visionCone.ViewDistance);
    }
}