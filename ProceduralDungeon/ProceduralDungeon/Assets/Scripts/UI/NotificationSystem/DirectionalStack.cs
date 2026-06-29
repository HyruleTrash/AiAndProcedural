using UnityEngine;

/// <summary>
/// A simple layout component, made with gemini
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class DirectionalStack : MonoBehaviour
{
    [Header("Layout Configuration")]
    [Tooltip("The exact vector direction to stack children. (0, -1) is downward, (1, 0) is right, (1, 1) is diagonal up-right.")]
    [SerializeField] private Vector2 layoutDirection = Vector2.down;
    
    [Tooltip("The pixel spacing between each child element.")]
    [SerializeField] private float spacing = 10f;
    
    [Tooltip("Initial padding offset from the starting position.")]
    [SerializeField] private float initialPadding;

    [Tooltip("The anchor point from which the layout stack originates relative to the parent container.")]
    [SerializeField] private TextAnchor childAlignment = TextAnchor.MiddleCenter;

    [Tooltip("If true, reverses the layout order so that the newest hierarchy items (at the bottom) appear at the start of the stack.")]
    [SerializeField] private bool inverseStack;

    [Header("Child Size Control")]
    [Tooltip("Force the children's width to match the parent container's width.")]
    [SerializeField] private bool controlChildWidth;

    [Tooltip("Force the children's height to match the parent container's height.")]
    [SerializeField] private bool controlChildHeight;

    [Header("Behavior")]
    [Tooltip("Automatically adjust child anchors to match the selected alignment setting.")]
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

    /// <summary>
    /// Built-in Unity callback triggered instantly whenever a child is added or removed.
    /// </summary>
    private void OnTransformChildrenChanged()
    {
        UpdateLayout();
    }

    /// <summary>
    /// Built-in Unity UI callback triggered when the parent container shifts dimensions.
    /// </summary>
    private void OnRectTransformDimensionsChange()
    {
        UpdateLayout();
    }

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

        // Determine the target anchor positions based on the alignment selection
        Vector2 anchorVector = GetAnchorVector(this.childAlignment);

        // Cache parent dimensions to prevent repetitive property access inside the layout loop
        Rect parentRect = this.rectTransform.rect;

        // Position tracking shifts along your custom vector path, originating from the selected anchor point
        Vector2 currentPosition = normalizedDir * this.initialPadding;

        for (int i = 0; i < childCount; i++)
        {
            // If inverseStack is active, process hierarchy elements from last to first
            int targetIndex = this.inverseStack ? (childCount - 1 - i) : i;
            RectTransform child = (this.transform.GetChild(targetIndex) as RectTransform)!;

            // Gracefully ignore inactive elements, just like standard native layout groups
            if (!child || !child.gameObject.activeSelf) continue;

            // Control child dimensions if options are explicitly enabled
            if (this.controlChildWidth) child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentRect.width);
            if (this.controlChildHeight) child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, parentRect.height);

            if (this.autoSnapAnchors)
            {
                child.anchorMin = anchorVector;
                child.anchorMax = anchorVector;
            }

            // Project the child's UI dimensions onto your direction vector to calculate bounding size
            float childSizeAlongAxis = Mathf.Abs(child.rect.width * normalizedDir.x) + 
                                       Mathf.Abs(child.rect.height * normalizedDir.y);

            // Compute alignment offset dynamically to prevent anchor/pivot mismatch clipping
            Vector2 alignmentOffset = new(
                (child.pivot.x - anchorVector.x) * child.rect.width,
                (child.pivot.y - anchorVector.y) * child.rect.height
            );

            // Place the child precisely in line, compounding the layout tracking path and alignment offset
            child.anchoredPosition = currentPosition + alignmentOffset;

            // Advance the tracking line for the next child element
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