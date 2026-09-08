using UnityEngine;

public class TacticalCameraController : MonoBehaviour
{
    [Header("Lateral Pan Settings")]
    [SerializeField] private float keyPanSpeed = 20f;
    [SerializeField] private float mousePanSensitivity = 1.0f;

    [Header("Height / Zoom Settings ($Y$-Axis)")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float zoomSpeed = 15f;
    [SerializeField] private float minHeight = 5f;
    [SerializeField] private float maxHeight = 35f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 100f;

    [Header("Lateral Map Boundaries (Confine to Map)")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 minBounds = new Vector2(-50f, -50f); // Minimum (X, Z) map limits
    [SerializeField] private Vector2 maxBounds = new Vector2(50f, 50f);   // Maximum (X, Z) map limits

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float targetHeight;
    private Vector3 dragOrigin;

    private void Start()
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
        {
            targetHeight = cameraTransform.position.y;
        }
    }

    private void Update()
    {
        HandleKeyboardPan();
        HandleMouseLeftDragPan();
        HandleRotation();
        HandleZoom();
        ClampAndSmooth();
    }

    private void HandleKeyboardPan()
    {
        Vector3 moveInput = Vector3.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveInput += transform.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveInput -= transform.forward;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveInput -= transform.right;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveInput += transform.right;

        moveInput.y = 0f;
        moveInput.Normalize();

        targetPosition += moveInput * keyPanSpeed * Time.deltaTime;
    }

    private void HandleMouseLeftDragPan()
    {
        // 1. Capture drag start point when Left Mouse Button goes down
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = GetGroundPointUnderMouse();
        }

        // 2. Drag map along ground plane while holding Left Mouse Button
        if (Input.GetMouseButton(0))
        {
            Vector3 currentGroundPoint = GetGroundPointUnderMouse();
            Vector3 difference = dragOrigin - currentGroundPoint;

            // Apply horizontal offset along X and Z
            targetPosition.x += difference.x * mousePanSensitivity;
            targetPosition.z += difference.z * mousePanSensitivity;
        }
    }

    private void HandleRotation()
    {
        float rotationDirection = 0f;

        if (Input.GetKey(KeyCode.Q)) rotationDirection -= 1f;
        if (Input.GetKey(KeyCode.E)) rotationDirection += 1f;

        if (Input.GetMouseButton(2)) // Middle Mouse Drag
        {
            rotationDirection += Input.GetAxis("Mouse X") * 2.0f;
        }

        if (Mathf.Abs(rotationDirection) > 0.01f)
        {
            targetRotation *= Quaternion.Euler(Vector3.up * rotationDirection * rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.001f)
        {
            // Adjust camera Y height directly
            targetHeight -= scroll * zoomSpeed * 100f * Time.deltaTime;
            targetHeight = Mathf.Clamp(targetHeight, minHeight, maxHeight);
        }
    }

    private void ClampAndSmooth()
    {
        // Lateral Map Boundaries (Clamp X and Z)
        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.z = Mathf.Clamp(targetPosition.z, minBounds.y, maxBounds.y);
        }

        // Smoothly interpolate rig position and rotation
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

        // Smoothly interpolate Camera height along the Y axis
        if (cameraTransform != null)
        {
            Vector3 currentCamPos = cameraTransform.position;
            Vector3 targetCamPos = new Vector3(currentCamPos.x, targetHeight, currentCamPos.z);
            cameraTransform.position = Vector3.Lerp(currentCamPos, targetCamPos, Time.deltaTime * 10f);
        }
    }

    private Vector3 GetGroundPointUnderMouse()
    {
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (groundPlane.Raycast(ray, out float entry))
        {
            return ray.GetPoint(entry);
        }

        return transform.position;
    }
}