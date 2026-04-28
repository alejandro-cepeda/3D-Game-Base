using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.6f;
    [SerializeField] private float turnSpeedDegreesPerSecond = 720f;
    [SerializeField] private bool faceMouse = true;
    [SerializeField] private bool faceMouseOnlyWhileAttacking = true;
    [SerializeField] private float faceMouseTurnSpeedDegreesPerSecond = 1080f;
    [SerializeField] private float jumpForce = 9.0f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private Animator animator;
    private Rigidbody rb;

    private Vector3 desiredVelocity;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }

        // Get input from WASD keys
        float horizontalInput = Input.GetAxis("Horizontal"); // A/D keys
        float verticalInput = Input.GetAxis("Vertical");     // W/S keys

        // Calculate movement direction
        Vector3 movement = new Vector3(horizontalInput, 0f, verticalInput);

        bool isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);
        Vector3 movementDirection = movement.sqrMagnitude > 0f ? movement.normalized : Vector3.zero;

        bool isGrounded = IsGrounded();
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space);

        if (rb != null)
        {
            desiredVelocity = new Vector3(
                movementDirection.x * currentSpeed,
                0f,
                movementDirection.z * currentSpeed
            );

            if (jumpPressed && isGrounded)
            {
                // Reset vertical velocity before jumping to ensure consistent jump heights
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            }
        }

        if (animator != null)
        {
            float speed01 = Mathf.Clamp01(movement.magnitude) * (isSprinting ? 1f : 0.5f);
            animator.SetFloat(SpeedHash, speed01);
            animator.SetBool(IsGroundedHash, isGrounded);

            if (rb != null)
            {
                animator.SetFloat(VerticalVelocityHash, rb.linearVelocity.y);
            }

            if (jumpPressed && isGrounded)
            {
                animator.SetTrigger(JumpHash);
            }
        }

        if (movementDirection.sqrMagnitude > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeedDegreesPerSecond * Time.deltaTime
            );
        }

        if (faceMouse)
        {
            bool shouldFaceMouse = !faceMouseOnlyWhileAttacking || Input.GetMouseButton(0);
            if (shouldFaceMouse)
            {
                RotateTowardsMouse();
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        if (Time.timeScale == 0f)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // Apply horizontal movement but preserve the existing vertical velocity (gravity, jumping)
        rb.linearVelocity = new Vector3(desiredVelocity.x, rb.linearVelocity.y, desiredVelocity.z);
    }

    private void RotateTowardsMouse()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Camera? cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
        if (!plane.Raycast(ray, out float enter))
        {
            return;
        }

        Vector3 point = ray.GetPoint(enter);
        Vector3 dir = point - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            faceMouseTurnSpeedDegreesPerSecond * Time.deltaTime
        );
    }

    private bool IsGrounded()
    {
        if (groundCheck == null)
        {
            return false;
        }

        return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    public void AddMoveSpeed(float amount)
    {
        moveSpeed = Mathf.Max(0f, moveSpeed + amount);
    }
}
