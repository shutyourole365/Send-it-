using UnityEngine;

namespace SendIt.UI
{
    /// <summary>
    /// Manages the preview camera for viewing the vehicle in the tuning garage.
    /// Allows rotation, zoom, and pan for better visualization.
    /// </summary>
    public class PreviewCamera : MonoBehaviour
    {
        [SerializeField] private Transform vehicleTarget;
        [SerializeField] private float rotationSpeed = 2f;
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float panSpeed = 0.5f;

        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 10f;
        [SerializeField] private float defaultDistance = 5f;

        private Camera previewCamera;
        private float currentDistance;
        private float currentRotationX;
        private float currentRotationY;
        private Vector3 panOffset = Vector3.zero;

        private Vector3 defaultPosition;
        private bool isRotating;
        private bool isPanning;

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize the preview camera.
        /// </summary>
        private void Initialize()
        {
            previewCamera = GetComponent<Camera>();
            if (previewCamera == null)
            {
                previewCamera = gameObject.AddComponent<Camera>();
            }

            if (vehicleTarget == null)
            {
                // Find vehicle in scene
                GameObject vehicle = GameObject.Find("Vehicle");
                if (vehicle != null)
                {
                    vehicleTarget = vehicle.transform;
                }
                else
                {
                    Debug.LogWarning("Vehicle target not found for preview camera");
                    return;
                }
            }

            // Setup camera
            previewCamera.fieldOfView = 60f;
            currentDistance = defaultDistance;
            currentRotationX = 20f;
            currentRotationY = 45f;
            defaultPosition = transform.position;

            UpdateCameraPosition();
        }

        private void Update()
        {
            if (vehicleTarget == null)
                return;

            HandleInput();
            UpdateCameraPosition();
        }

        /// <summary>
        /// Handle player input for camera control.
        /// </summary>
        private void HandleInput()
        {
            // Rotation with middle mouse button
            if (Input.GetMouseButton(2))
            {
                currentRotationX += Input.GetAxis("Mouse Y") * rotationSpeed;
                currentRotationY += Input.GetAxis("Mouse X") * rotationSpeed;

                currentRotationX = Mathf.Clamp(currentRotationX, -30f, 60f);
            }

            // Zoom with scroll wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentDistance -= scroll * zoomSpeed;
                currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
            }

            // Pan with right mouse button
            if (Input.GetMouseButton(1))
            {
                panOffset += transform.right * Input.GetAxis("Mouse X") * panSpeed;
                panOffset += transform.up * Input.GetAxis("Mouse Y") * panSpeed;
            }

            // Reset view with R key
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetCameraView();
            }
        }

        /// <summary>
        /// Update camera position and rotation based on current parameters.
        /// </summary>
        private void UpdateCameraPosition()
        {
            // Calculate desired position
            Vector3 targetPosition = vehicleTarget.position + panOffset;

            // Apply rotation
            Quaternion rotation = Quaternion.Euler(-currentRotationX, currentRotationY, 0f);

            // Calculate camera position
            Vector3 cameraOffset = rotation * Vector3.back * currentDistance;
            Vector3 finalPosition = targetPosition + cameraOffset;

            // Apply position and rotation
            transform.position = finalPosition;
            transform.LookAt(targetPosition, Vector3.up);
        }

        /// <summary>
        /// Reset camera to default view.
        /// </summary>
        private void ResetCameraView()
        {
            currentDistance = defaultDistance;
            currentRotationX = 20f;
            currentRotationY = 45f;
            panOffset = Vector3.zero;
        }

        /// <summary>
        /// Set camera to front view.
        /// </summary>
        public void SetFrontView()
        {
            currentRotationX = 0f;
            currentRotationY = 0f;
            currentDistance = defaultDistance;
            panOffset = Vector3.zero;
        }

        /// <summary>
        /// Set camera to side view.
        /// </summary>
        public void SetSideView()
        {
            currentRotationX = 0f;
            currentRotationY = 90f;
            currentDistance = defaultDistance;
            panOffset = Vector3.zero;
        }

        /// <summary>
        /// Set camera to top-down view.
        /// </summary>
        public void SetTopView()
        {
            currentRotationX = 85f;
            currentRotationY = 0f;
            currentDistance = defaultDistance * 1.5f;
            panOffset = Vector3.zero;
        }

        /// <summary>
        /// Set camera to isometric view (common for design).
        /// </summary>
        public void SetIsometricView()
        {
            currentRotationX = 35f;
            currentRotationY = 45f;
            currentDistance = defaultDistance * 1.2f;
            panOffset = Vector3.zero;
        }

        public void SetTargetVehicle(Transform vehicle) => vehicleTarget = vehicle;
    }
}
