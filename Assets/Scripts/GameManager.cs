using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Central game coordinator for Tejiendo Rutas.
/// Tracks score (social welfare), remaining thread, and game state.
/// Wires together the HUD and listens for circuit-completion events.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ── singleton ──────────────────────────────────────────────────────────

    public static GameManager Instance { get; private set; }

    // ── serialised config ──────────────────────────────────────────────────

    [Header("References")]
    [SerializeField] private ThreadManager threadManager;
    [SerializeField] private CircuitManager circuitManager;

    [Header("Scoring")]
    [Tooltip("Points awarded per node in a completed circuit.")]
    [SerializeField] private int pointsPerNode = 100;

    [Tooltip("Bonus multiplier for circuits larger than the minimum size.")]
    [SerializeField] private float bonusMultiplier = 1.5f;

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI threadRemainingText;
    [SerializeField] private Slider threadSlider;
    [SerializeField] private TextMeshProUGUI circuitCountText;

    [Header("Win Condition")]
    [Tooltip("Number of circuits to close to win the level.")]
    [SerializeField] private int circuitsToWin = 3;
    [SerializeField] private GameObject winPanel;

    // ── public state ───────────────────────────────────────────────────────

    public int Score { get; private set; }
    public int CircuitsClosed { get; private set; }

    // ── lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (threadManager == null)
            threadManager = FindObjectOfType<ThreadManager>();
        if (circuitManager == null)
            circuitManager = FindObjectOfType<CircuitManager>();
    }

    private void OnEnable()
    {
        if (circuitManager != null)
            circuitManager.OnCircuitClosed.AddListener(HandleCircuitClosed);
    }

    private void OnDisable()
    {
        if (circuitManager != null)
            circuitManager.OnCircuitClosed.RemoveListener(HandleCircuitClosed);
    }

    private void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        UpdateHUD();
    }

    private void Update()
    {
        UpdateHUD();
    }

    // ── event handling ─────────────────────────────────────────────────────

    private void HandleCircuitClosed(System.Collections.Generic.List<Vector3> positions)
    {
        int nodes = positions.Count;
        int points = Mathf.RoundToInt(nodes * pointsPerNode * bonusMultiplier);
        Score += points;
        CircuitsClosed++;

        Debug.Log($"[GameManager] Circuit closed! +{points} welfare points. " +
                  $"Total: {Score}. Circuits: {CircuitsClosed}/{circuitsToWin}");

        if (CircuitsClosed >= circuitsToWin)
            TriggerWin();
    }

    private void TriggerWin()
    {
        Debug.Log("[GameManager] Level Complete!");
        if (winPanel != null) winPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    // ── HUD update ─────────────────────────────────────────────────────────

    private void UpdateHUD()
    {
        if (scoreText != null)
            scoreText.text = $"Bienestar: {Score}";

        if (threadManager != null)
        {
            float fraction = threadManager.ThreadFraction;

            if (threadSlider != null) threadSlider.value = fraction;

            if (threadRemainingText != null)
                threadRemainingText.text =
                    $"Hilo: {threadManager.ThreadRemaining:F1}m";
        }

        if (circuitCountText != null)
            circuitCountText.text = $"Circuitos: {CircuitsClosed}/{circuitsToWin}";
    }
}
