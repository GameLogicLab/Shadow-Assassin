using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float gravity = -20f;

    public Joystick joystick;

    private CharacterController controller;
    private Animator animator;

    [Header("Walking Audio")]
    public AudioSource walkingAudioSource;
    public AudioClip walkingSound;

    private float verticalVelocity = -2f;
    private bool lastIsMoving = false;
    private static readonly int IsRunningHash = Animator.StringToHash("isRunning");

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (walkingAudioSource != null && walkingSound != null)
        {
            walkingAudioSource.clip = walkingSound;
            walkingAudioSource.loop = true;
            walkingAudioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        Vector2 joystickInput = joystick != null ? joystick.Input : Vector2.zero;
        Vector2 keyboardInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        Vector2 finalInput = joystickInput.sqrMagnitude > 0.01f ? joystickInput : keyboardInput;
        if (finalInput.sqrMagnitude > 1f)
        {
            finalInput.Normalize();
        }

        Vector3 horizontalMove = new Vector3(finalInput.x, 0f, finalInput.y);

        // Apply gravity
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f; // Slight downward push to keep grounded
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 velocity = (horizontalMove * moveSpeed) + (Vector3.up * verticalVelocity);
        controller.Move(velocity * Time.deltaTime);

        bool isMoving = horizontalMove.sqrMagnitude > 0.001f;

        if (isMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontalMove);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        if (animator != null && isMoving != lastIsMoving)
        {
            animator.SetBool(IsRunningHash, isMoving);
            lastIsMoving = isMoving;
        }

        // Footstep audio
        if (walkingAudioSource != null && walkingSound != null)
        {
            if (isMoving && !walkingAudioSource.isPlaying)
            {
                walkingAudioSource.Play();
            }
            else if (!isMoving && walkingAudioSource.isPlaying)
            {
                walkingAudioSource.Stop();
            }
        }
    }
}
