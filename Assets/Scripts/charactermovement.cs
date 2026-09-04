using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class KnightController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5.0f;
    [SerializeField] private float sprintSpeed = 9.0f; // Speed while holding Left Shift
    [SerializeField] private float rotationSpeed = 10.0f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -19.62f;

    private CharacterController characterController;
    private Animator animator;
    private Vector3 velocity;
    private bool isGrounded;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // 0. Respawn if Falling
        if (transform.position.y < -5) {
            // 1. Disable the CharacterController to unhook physics caching
            characterController.enabled = false;
            // 2. Reset stored vertical/horizontal momentum
            velocity = Vector3.zero;
            // 3. Update the transform position
            transform.position = new Vector3(0, 0, 0);
            // 4. Re-enable the CharacterController
            characterController.enabled = true;
        }
        // 1. Check Grounding
        isGrounded = characterController.isGrounded;
        animator.SetBool(IsGroundedHash, isGrounded);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 2. Horizontal Movement & Sprinting
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
        Vector3 moveVelocity = Vector3.zero;

        // Check if sprint key is held (Left Shift)
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        if (moveDirection.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            moveVelocity = moveDirection * currentSpeed;
        }

        // 3. Jump Logic
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger(JumpHash);
        }

        // 4. Gravity & Final Movement
        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMovement = moveVelocity + (Vector3.up * velocity.y);
        characterController.Move(finalMovement * Time.deltaTime);

        // Calculate animator speed value: 0 = Idle, 0.5 = Walk, 1.0 = Sprint
        float animSpeedValue = 0f;
        if (moveDirection.magnitude >= 0.1f)
        {
            animSpeedValue = isSprinting ? 1.0f : 0.5f;
        }

        animator.SetFloat(SpeedHash, animSpeedValue, 0.1f, Time.deltaTime);
    }
}