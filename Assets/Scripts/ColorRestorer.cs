using UnityEngine;

/// <summary>
/// Attached to environment props (buildings, streets, etc.).
/// Stores the intended vivid colour so that <see cref="CircuitManager"/>
/// can lerp this object from greyscale back to full colour when a nearby
/// circuit is completed.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ColorRestorer : MonoBehaviour
{
    [Header("Colours")]
    [Tooltip("The vibrant colour this object will restore to. " +
             "If left as white, the target colour is inferred from the node.")]
    [SerializeField] private Color targetColor = Color.white;

    [Tooltip("When true, colour restoration begins on Start " +
             "(useful for testing without a circuit).")]
    [SerializeField] private bool restoreOnStart = false;

    [Tooltip("Duration of the restore animation when triggered from Start (seconds).")]
    [SerializeField] private float autoRestoreDuration = 2f;

    // ── public state ───────────────────────────────────────────────────────

    /// <summary>True once the object has been fully colourised.</summary>
    public bool IsRestored { get; private set; }

    // ── private state ──────────────────────────────────────────────────────

    private Renderer _renderer;
    private MaterialPropertyBlock _block;

    private static readonly int BaseColorId    = Shader.PropertyToID("_BaseColor");   // URP
    private static readonly int ColorId        = Shader.PropertyToID("_Color");        // Legacy
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    // ── lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _block    = new MaterialPropertyBlock();

        // Initialise as grey
        ApplyColor(Color.gray);
    }

    private void Start()
    {
        if (restoreOnStart)
            StartCoroutine(AutoRestoreCoroutine());
    }

    // ── public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the intermediate lerp colour; called per-frame by CircuitManager.
    /// <paramref name="t"/> is in [0, 1].
    /// <paramref name="nodeColor"/> is the colour coming from the nearby node
    /// (only used if <see cref="targetColor"/> has not been set explicitly).
    /// </summary>
    public void SetLerpAmount(float t, Color nodeColor)
    {
        Color final = (targetColor == Color.white) ? nodeColor : targetColor;
        ApplyColor(Color.Lerp(Color.gray, final, t));
    }

    /// <summary>
    /// Snaps the object to its full colour immediately.
    /// </summary>
    public void CompleteRestore(Color nodeColor)
    {
        Color final = (targetColor == Color.white) ? nodeColor : targetColor;
        ApplyColor(final);
        IsRestored = true;
    }

    // ── private helpers ────────────────────────────────────────────────────

    private void ApplyColor(Color color)
    {
        _renderer.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId,     color);
        _block.SetColor(ColorId,         color);
        _block.SetColor(EmissionColorId, color * 0.2f);
        _renderer.SetPropertyBlock(_block);
    }

    private System.Collections.IEnumerator AutoRestoreCoroutine()
    {
        Color final = targetColor;
        float elapsed = 0f;

        while (elapsed < autoRestoreDuration)
        {
            float t = elapsed / autoRestoreDuration;
            SetLerpAmount(t, final);
            elapsed += Time.deltaTime;
            yield return null;
        }
        CompleteRestore(final);
    }
}
