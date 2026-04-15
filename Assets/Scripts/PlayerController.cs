using UnityEngine;

/// <summary>
/// Phase 1: Fluid Character Controller for Tejiendo Rutas.
/// Handles movement, jumping, gravity and camera-relative input.
/// Attach this component to the player GameObject alongside a
/// Unity CharacterController component.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Horizontal movement speed in units/second.")]
    [SerializeField] private float moveSpeed = 6f;

    [Tooltip("How quickly the character accelerates / decelerates.")]
    [SerializeField] private float acceleration = 20f;

    [Header("Jumping")]
    [Tooltip("Initial vertical velocity applied when the player jumps.")]
    [SerializeField] private float jumpForce = 8f;

    [Tooltip("Gravity magnitude (positive value).")]
    [SerializeField] private float gravity = 20f;

    [Header("Thread")]
    [Tooltip("How far from the player a node can be to initiate a connection.")]
    [SerializeField] private float interactRadius = 3f;

    [Header("Camera Reference")]
    [Tooltip("Main camera transform used for camera-relative movement. " +
             "Defaults to Camera.main if left empty.")]
    [SerializeField] private Transform cameraTransform;

    // ── private state ──────────────────────────────────────────────────────

    private CharacterController _controller;
    private ThreadManager _threadManager;
    private Vector3 _velocity;        // current physics velocity
    private Vector3 _moveDirection;   // smoothed horizontal movement

    // Pre-allocated buffer for OverlapSphereNonAlloc to avoid per-frame heap allocation
    private readonly Collider[] _overlapBuffer = new Collider[16];

    // ── lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _threadManager = GetComponent<ThreadManager>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        HandleMovement();
        HandleJump();
        HandleThreadInteraction();
    }

    // ── movement ───────────────────────────────────────────────────────────

    /// <summary>
    /// Reads input axes and smoothly moves the character relative to the camera.
    /// </summary>
    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical   = Input.GetAxisRaw("Vertical");

        // Build camera-relative direction
        Vector3 desiredMove = Vector3.zero;
        if (cameraTransform != null)
        {
            Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 camRight   = Vector3.ProjectOnPlane(cameraTransform.right,   Vector3.up).normalized;
            desiredMove = (camForward * vertical + camRight * horizontal);
        }
        else
        {
            desiredMove = new Vector3(horizontal, 0f, vertical);
        }

        if (desiredMove.sqrMagnitude > 1f) desiredMove.Normalize();

        // Smooth acceleration
        _moveDirection = Vector3.MoveTowards(
            _moveDirection,
            desiredMove * moveSpeed,
            acceleration * Time.deltaTime);

        // Rotate character to face movement direction
        if (_moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.deltaTime);
        }

        // Apply horizontal velocity
        _velocity.x = _moveDirection.x;
        _velocity.z = _moveDirection.z;

        _controller.Move(_velocity * Time.deltaTime);
    }

    /// <summary>
    /// Applies gravity and handles jump input.
    /// </summary>
    private void HandleJump()
    {
        if (_controller.isGrounded)
        {
            // Keep the character grounded with a small downward force
            if (_velocity.y < 0f) _velocity.y = -2f;

            if (Input.GetButtonDown("Jump"))
                _velocity.y = jumpForce;
        }
        else
        {
            _velocity.y -= gravity * Time.deltaTime;
        }
    }

    // ── thread interaction ─────────────────────────────────────────────────

    /// <summary>
    /// Allows the player to start or cancel a thread connection via the
    /// Interact key (default: E or Fire1).
    /// </summary>
    private void HandleThreadInteraction()
    {
        if (_threadManager == null) return;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetButtonDown("Fire1"))
        {
            NodePoint nearest = FindNearestNode();
            if (nearest != null)
                _threadManager.TryConnect(nearest);
        }

        if (Input.GetKeyDown(KeyCode.R) || Input.GetButtonDown("Fire2"))
            _threadManager.CancelCurrentThread();
    }

    /// <summary>
    /// Returns the closest active <see cref="NodePoint"/> within
    /// <see cref="interactRadius"/>, or null if none is found.
    /// </summary>
    private NodePoint FindNearestNode()
    {
        NodePoint best = null;
        float bestDist = interactRadius * interactRadius;

        // Use the pre-allocated buffer to avoid per-call heap allocation
        int count = Physics.OverlapSphereNonAlloc(transform.position, interactRadius, _overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            NodePoint node = _overlapBuffer[i].GetComponent<NodePoint>();
            if (node == null) continue;

            float dist = (node.transform.position - transform.position).sqrMagnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = node;
            }
        }
        return best;
    }

    // ── gizmos ─────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
