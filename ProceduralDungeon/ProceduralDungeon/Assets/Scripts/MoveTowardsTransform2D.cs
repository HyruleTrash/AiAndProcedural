using UnityEngine;

public class MoveTowardsTransform2D : MonoBehaviour
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

    private void Start() => this.lastTargetPosition = this.toLerpTowards.position;
    private void OnValidate()
    {
        if (!this.enabled)
            return;
        this.enabled = this.toLerpTowards && this.speed > 0 && this.curve != null && this.curveStrength > 0 &&
                       ((this.pixelSnapping && this.pixelPerUnit > 0) || !this.pixelSnapping);
    }

    private void LateUpdate()
    {
        Vector3 rawVelocity = (this.toLerpTowards.position - this.lastTargetPosition) / Time.deltaTime;
        this.smoothedVelocity = Vector3.Lerp(this.smoothedVelocity, rawVelocity, this.velocitySmooth * Time.deltaTime);
        Vector3 velocity = this.smoothedVelocity;
        Vector3 predictedTarget = this.toLerpTowards.position + Vector3.ClampMagnitude(velocity * this.lookAheadTime, this.distFromTargetMax);
        this.lastTargetPosition = this.toLerpTowards.position;
        
        Vector3 toLerpTowardsPosition = new(predictedTarget.x, predictedTarget.y, this.transform.position.z);
        float distFromDesiredPosition = Vector3.Distance(this.transform.position, toLerpTowardsPosition);
        
        if (ClampMaxPosition(distFromDesiredPosition, toLerpTowardsPosition)) return;
        Vector3 nextPos = CalcNextPos(toLerpTowardsPosition, distFromDesiredPosition);
        this.transform.position = nextPos;
        ApplyPixelSnapping(nextPos);
    }

    private bool ClampMaxPosition(float distFromDesiredPosition, Vector3 toLerpTowardsPosition)
    {
        if (!(distFromDesiredPosition > this.distFromTargetMax)) return false;
        Vector3 posToBeAt = (this.transform.position - toLerpTowardsPosition).normalized * this.distFromTargetMax;
        this.transform.position = toLerpTowardsPosition + posToBeAt;
        return true;
    }

    private float GetAlpha(float distFromDesiredPosition)
    {
        float alpha = distFromDesiredPosition / this.distFromTargetMax;
        alpha = Mathf.Clamp01(alpha);
        alpha = 1f + this.curve.Evaluate(alpha) * this.curveStrength;
        return alpha;
    }

    private Vector3 CalcNextPos(Vector3 toLerpTowardsPosition, float distFromDesiredPosition)
    {
        if (distFromDesiredPosition < this.distFromTargetMin)
            return this.transform.position;
        float alpha = GetAlpha(distFromDesiredPosition);
        float step = distFromDesiredPosition * alpha * this.speed * Time.deltaTime;
        step = Mathf.Min(step, distFromDesiredPosition);
        return Vector3.MoveTowards(this.transform.position, toLerpTowardsPosition, step);
    }

    private void ApplyPixelSnapping(Vector3 nextPos)
    {
        if (!this.pixelSnapping) 
            return; 
        nextPos.x = Mathf.Round(nextPos.x * this.pixelPerUnit) / this.pixelPerUnit; 
        nextPos.y = Mathf.Round(nextPos.y * this.pixelPerUnit) / this.pixelPerUnit;
        this.transform.position = nextPos;
    }
}
