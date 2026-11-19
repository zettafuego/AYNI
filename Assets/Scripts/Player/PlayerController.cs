using UnityEngine;

namespace AYNI.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float baseSpeed = 5f;
        [SerializeField] float jumpForce = 6f;
        [SerializeField] float gravity = -18f;
        [SerializeField] float glideGravity = -4f;
        [SerializeField] float rotationSpeed = 10f;

        CharacterController controller;
        AnimalTransformationController transformation;
        Vector3 velocity;
        bool isGrounded;
        int jumpCount;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            transformation = GetComponent<AnimalTransformationController>();
        }

        void Update()
        {
            if (Narrative.MoralChoiceSystem.Instance != null &&
                Narrative.MoralChoiceSystem.Instance.IsChoosing)
                return;

            isGrounded = controller.isGrounded;
            if (isGrounded)
            {
                velocity.y = -2f;
                jumpCount = 0;
            }

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 dir = new Vector3(h, 0f, v).normalized;

            float speed = baseSpeed * (transformation?.GetSpeedMultiplier() ?? 1f);
            if (transformation != null && transformation.CanSneak() && Input.GetKey(KeyCode.LeftShift))
                speed *= 0.5f;

            controller.Move(dir * speed * Time.deltaTime);

            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
            }

            bool canDoubleJump = transformation != null && transformation.CanBurrow();
            if (Input.GetButtonDown("Jump"))
            {
                if (isGrounded || (canDoubleJump && jumpCount < 1))
                {
                    velocity.y = jumpForce;
                    jumpCount++;
                }
            }

            float g = gravity;
            if (transformation != null && transformation.CanGlide() && !isGrounded && Input.GetKey(KeyCode.Space))
                g = glideGravity;

            velocity.y += g * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }
    }
}