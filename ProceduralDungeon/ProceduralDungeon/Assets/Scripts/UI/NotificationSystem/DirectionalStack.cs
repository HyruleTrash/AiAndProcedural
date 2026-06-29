using UnityEngine;

/// <summary>
/// A simple layout component, for stacking ui elements
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class DirectionalStack : MonoBehaviour
{
    [Header("Layout Configuration")]
    [SerializeField] private Vector2 layoutDirection = Vector2.down;
    [SerializeField] private float spacing = 10f;
    [SerializeField] private float initialPadding;
    [SerializeField] private TextAnchor childAlignment = TextAnchor.MiddleCenter;
    [SerializeField] private bool inverseStack;

    [Header("Child Size Control")]
    [SerializeField] private bool controlChildWidth;
    [SerializeField] private bool controlChildHeight;

    [Space]
    [SerializeField] private bool autoSnapAnchors = true;

    private RectTransform rectTransform = null!;

    private void Awake() => this.rectTransform = GetComponent<RectTransform>();
    private void OnEnable() => UpdateLayout();

    private void Update()
    {
        if (!this.transform.hasChanged) return;
        UpdateLayout();
        this.transform.hasChanged = false;
    }

    private void OnTransformChildrenChanged() => UpdateLayout();
    private void OnRectTransformDimensionsChange() => UpdateLayout();

#if UNITY_EDITOR
    private void OnValidate() => UpdateLayout();
#endif

    private void UpdateLayout()
    {
        if (!this.rectTransform) this.rectTransform = GetComponent<RectTransform>();

        int childCount = this.transform.childCount;
        if (childCount == 0) return;

        Vector2 normalizedDir = this.layoutDirection.normalized;
        if (normalizedDir == Vector2.zero) return;

        Vector2 anchorVector = GetAnchorVector(this.childAlignment);
        Rect parentRect = this.rectTransform.rect;
        Vector2 currentPosition = normalizedDir * this.initialPadding;

        for (int i = 0; i < childCount; i++)
        {
            int targetIndex = this.inverseStack ? (childCount - 1 - i) : i;
            RectTransform child = (this.transform.GetChild(targetIndex) as RectTransform)!;

            if (!child || !child.gameObject.activeSelf) continue;

            if (this.controlChildWidth) child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentRect.width);
            if (this.controlChildHeight) child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, parentRect.height);

            if (this.autoSnapAnchors)
            {
                child.anchorMin = anchorVector;
                child.anchorMax = anchorVector;
            }

            float childSizeAlongAxis = Mathf.Abs(child.rect.width * normalizedDir.x) + 
                                       Mathf.Abs(child.rect.height * normalizedDir.y);

            Vector2 alignmentOffset = new(
                (child.pivot.x - anchorVector.x) * child.rect.width,
                (child.pivot.y - anchorVector.y) * child.rect.height
            );

            child.anchoredPosition = currentPosition + alignmentOffset;
            currentPosition += normalizedDir * (childSizeAlongAxis + this.spacing);
        }
    }

    private static Vector2 GetAnchorVector(TextAnchor anchor) => anchor switch
    {
        TextAnchor.UpperLeft    => new Vector2(0.0f, 1.0f),
        TextAnchor.UpperCenter  => new Vector2(0.5f, 1.0f),
        TextAnchor.UpperRight   => new Vector2(1.0f, 1.0f),
        TextAnchor.MiddleLeft   => new Vector2(0.0f, 0.5f),
        TextAnchor.MiddleCenter => new Vector2(0.5f, 0.5f),
        TextAnchor.MiddleRight  => new Vector2(1.0f, 0.5f),
        TextAnchor.LowerLeft    => new Vector2(0.0f, 0.0f),
        TextAnchor.LowerCenter  => new Vector2(0.5f, 0.0f),
        TextAnchor.LowerRight   => new Vector2(1.0f, 0.0f),
        _                       => new Vector2(0.5f, 0.5f)
    };
}