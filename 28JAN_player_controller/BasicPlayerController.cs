using UnityEngine;

public class SimpleThirdPersonController : MonoBehaviour
{
    public float moveSpeed = 5f;               // Player movement speed
    public float jumpForce = 5f;               // Jump force applied when jumping
    public float gravity = -9.81f;             // Gravity that affects the player
    public Transform cameraTransform;         // Reference to the camera's transform
    public Transform cameraPivot;             // Reference to the pivot for camera rotation
    public float mouseSensitivity = 2f;        // Sensitivity of the mouse for camera rotation
    public float maxCameraDistance = 5f;       // Maximum distance of the camera from the player
    public float minCameraDistance = 2f;       // Minimum distance of the camera from the player
    public float cameraHeight = 2f;            // Height of the camera above the player
    public float minVerticalAngle = -30f;      // Minimum vertical angle (looking downwards)
    public float maxVerticalAngle = 90f;       // Maximum vertical angle (looking upwards)
    public LayerMask collisionMask;            // Mask to detect collision for camera raycasting

    private CharacterController characterController;  // Reference to the CharacterController for movement
    private Vector3 velocity;                    // To track the player's velocity (for gravity and jump)
    private bool isGrounded;                     // Flag to check if the player is on the ground
    private float rotationX = 0f;                // X-axis rotation of the camera (up/down)
    private float rotationY = 0f;                // Y-axis rotation of the camera (left/right)
    private float currentCameraDistance;         // The current camera distance, used for smooth camera zoom

    void Start()
    {
        // Initialize the CharacterController component
        characterController = GetComponent<CharacterController>();

        // Lock the cursor to the center of the screen and hide it for a better third-person view
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Set the initial camera distance to the maximum distance
        currentCameraDistance = maxCameraDistance;
    }

    void Update()
    {
        // Call methods to move the player and rotate the camera
        MovePlayer();
        RotateCamera();
    }

    void MovePlayer()
    {
        // Check if the player is on the ground
        isGrounded = characterController.isGrounded;

        // If the player is on the ground and falling, reset vertical velocity to simulate gravity
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative value to keep the player grounded
        }

        // Get horizontal and vertical input from the player (WASD or arrow keys)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Calculate the movement direction relative to the camera's rotation
        Vector3 move = cameraPivot.forward * vertical + cameraPivot.right * horizontal;
        move.y = 0; // Ensure no movement in the Y-axis (to avoid camera or player flying up/down)

        // Apply the calculated movement to the CharacterController
        characterController.Move(move * moveSpeed * Time.deltaTime);

        // If the player presses the "Jump" button and is on the ground, apply the jump force
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity); // Calculate the upward jump velocity
        }

        // Apply gravity to the player's vertical velocity
        velocity.y += gravity * Time.deltaTime;

        // Apply the vertical velocity (gravity/jump) to the CharacterController
        characterController.Move(velocity * Time.deltaTime);
    }

    void RotateCamera()
    {
        // Get the mouse input for rotating the camera
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity; // Rotation around Y-axis (left/right)
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity; // Rotation around X-axis (up/down)

        // Update the horizontal rotation (left/right)
        rotationY += mouseX;

        // Update the vertical rotation (up/down) and clamp it to avoid extreme angles
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);

        // Apply the calculated rotations to the camera pivot (the object the camera is following)
        cameraPivot.rotation = Quaternion.Euler(rotationX, rotationY, 0f);

        // Smoothly adjust camera distance based on the player's vertical rotation
        // This helps to bring the camera closer when looking up, and farther when looking down
        float t = Mathf.Clamp01((rotationX - 60f) / (maxVerticalAngle - 60f));
        currentCameraDistance = Mathf.Lerp(maxCameraDistance, minCameraDistance, t);

        // Calculate the desired position of the camera based on the camera pivot's rotation and the current distance
        Vector3 desiredCameraPosition = cameraPivot.position + cameraPivot.rotation * new Vector3(0f, cameraHeight, -currentCameraDistance);

        // Perform a raycast to check if the camera position collides with any objects (like terrain or walls)
        Vector3 finalCameraPosition = desiredCameraPosition;

        if (Physics.Raycast(cameraPivot.position, (desiredCameraPosition - cameraPivot.position).normalized, out RaycastHit hit, currentCameraDistance, collisionMask))
        {
            finalCameraPosition = hit.point; // If a collision is detected, set the camera to the point of collision
        }

        // Ensure camera stays above terrain
        // Perform a raycast to check if the camera is too low and adjust its height if needed
        if (Physics.Raycast(finalCameraPosition + Vector3.up * 1f, Vector3.down, out RaycastHit terrainHit, Mathf.Infinity, collisionMask))
        {
            if (finalCameraPosition.y < terrainHit.point.y + 1f) // Keep the camera 1 unit above the terrain
            {
                finalCameraPosition.y = terrainHit.point.y + 1f;
            }
        }

        // Prevent NaN errors before applying the position
        // Check if the camera position is valid (no NaN values)
        if (!float.IsNaN(finalCameraPosition.x) && !float.IsNaN(finalCameraPosition.y) && !float.IsNaN(finalCameraPosition.z))
        {
            cameraTransform.position = finalCameraPosition; // Apply the calculated position to the camera
        }

        // Make the camera always look at the camera pivot with a slight offset (cameraHeight)
        cameraTransform.LookAt(cameraPivot.position + Vector3.up * cameraHeight);
    }
}
