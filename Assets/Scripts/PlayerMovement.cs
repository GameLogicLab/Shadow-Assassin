using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    public Joystick joystick;

    private CharacterController controller;
    private Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        Vector2 joystickInput = joystick.Input;

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

        if (movement != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        bool isMoving = movement != Vector3.zero;

        animator.SetBool("isRunning", isMoving);
    }
}