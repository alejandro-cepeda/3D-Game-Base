using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.6f;
    [SerializeField] private float turnSpeedDegreesPerSecond = 720f;
    [SerializeField] private float jumpForce = 5.5f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private Animator animator;
    private Rigidbody rb;

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
        }
    }

    private void Update()
    {
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
            rb.linearVelocity = new Vector3(
                movementDirection.x * currentSpeed,
                rb.linearVelocity.y,
                movementDirection.z * currentSpeed
            );

            if (jumpPressed && isGrounded)
            {
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
    }

    private bool IsGrounded()
    {
        if (groundCheck == null)
        {
            return false;
        }

        return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }
}
