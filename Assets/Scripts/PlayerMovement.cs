using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    public Joystick joystick;

    private CharacterController controller;
    private Animator animator;

    [Header("Walking Audio")]
    public AudioSource walkingAudioSource;
    public AudioClip walkingSound;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        // Assign the walking sound to the Audio Source
        if (walkingAudioSource != null && walkingSound != null)
        {
            walkingAudioSource.clip = walkingSound;
            walkingAudioSource.loop = true;
            walkingAudioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        Vector2 joystickInput = joystick != null
            ? joystick.Input
            : Vector2.zero;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector2 keyboardInput = new Vector2(horizontal, vertical);

        Vector2 finalInput = keyboardInput;

        if (joystickInput.magnitude > 0.1f)
        {
            finalInput = joystickInput;
        }

        Vector3 movement = new Vector3(
            finalInput.x,
            0f,
            finalInput.y
        );

        controller.Move(movement * moveSpeed * Time.deltaTime);

        if (movement.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        bool isMoving = movement.sqrMagnitude > 0.001f;

        if (animator != null)
        {
            animator.SetBool("isRunning", isMoving);
        }

        // Play footsteps while moving
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