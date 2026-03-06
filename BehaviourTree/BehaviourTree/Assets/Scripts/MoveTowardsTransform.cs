using System;
using UnityEngine;

public class MoveTowardsTransform : MonoBehaviour
{
    [SerializeField]
    private float pixelPerUnit;
    [SerializeField]
    private bool pixelSnapping;
    [Header("Target")]
    public Transform toLerpTowards;
    [SerializeField]
    private float distFromTargetMin = 0.1f;
    [SerializeField]
    private float distFromTargetMax = 10f;
    [SerializeField]
    private float lookAheadTime = 0.1f;
    [Header("Velocity")]
    [SerializeField]
    private float speed = 1f;
    [SerializeField]
    private AnimationCurve curve;
    [SerializeField]
    private float curveStrength = 2f;
    [SerializeField] 
    private float velocitySmooth = 10f;
    private Vector3 smoothedVelocity;
    private Vector3 lastTargetPosition;

    private void Start() => lastTargetPosition = toLerpTowards.position;
    private void OnValidate() =>
        enabled = toLerpTowards &&
                  speed > 0 &&
                  curve != null &&
                  curveStrength > 0 &&
                  ((pixelSnapping && pixelPerUnit > 0) || !pixelSnapping);

    private void LateUpdate()
    {
        var rawVelocity = (toLerpTowards.position - lastTargetPosition) / Time.deltaTime;
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, rawVelocity, velocitySmooth * Time.deltaTime);
        var velocity = smoothedVelocity;
        var predictedTarget = toLerpTowards.position + Vector3.ClampMagnitude(velocity * lookAheadTime, distFromTargetMax);
        lastTargetPosition = toLerpTowards.position;
        
        var toLerpTowardsPosition = new Vector3(predictedTarget.x, predictedTarget.y, transform.position.z);
        var distFromDesiredPosition = Vector3.Distance(transform.position, toLerpTowardsPosition);
        
        if (ClampMaxPosition(distFromDesiredPosition, toLerpTowardsPosition)) return;
        var nextPos = CalcNextPos(toLerpTowardsPosition, distFromDesiredPosition);
        transform.position = nextPos;
        ApplyPixelSnapping(nextPos);
    }

    private bool ClampMaxPosition(float distFromDesiredPosition, Vector3 toLerpTowardsPosition)
    {
        if (!(distFromDesiredPosition > distFromTargetMax)) return false;
        var posToBeAt = (transform.position - toLerpTowardsPosition).normalized * distFromTargetMax;
        transform.position = toLerpTowardsPosition + posToBeAt;
        return true;
    }

    private float GetAlpha(float distFromDesiredPosition)
    {
        var alpha = distFromDesiredPosition / distFromTargetMax;
        alpha = Mathf.Clamp01(alpha);
        alpha = 1f + curve.Evaluate(alpha) * curveStrength;
        return alpha;
    }

    private Vector3 CalcNextPos(Vector3 toLerpTowardsPosition, float distFromDesiredPosition)
    {
        if (distFromDesiredPosition < distFromTargetMin)
            return transform.position;
        var alpha = GetAlpha(distFromDesiredPosition);
        var step = distFromDesiredPosition * alpha * speed * Time.deltaTime;
        step = Mathf.Min(step, distFromDesiredPosition);
        return Vector3.MoveTowards(transform.position, toLerpTowardsPosition, step);
    }

    private void ApplyPixelSnapping(Vector3 nextPos)
    {
        if (!pixelSnapping) 
            return; 
        nextPos.x = Mathf.Round(nextPos.x * pixelPerUnit) / pixelPerUnit; 
        nextPos.y = Mathf.Round(nextPos.y * pixelPerUnit) / pixelPerUnit; 
        transform.position = nextPos;
    }
}
