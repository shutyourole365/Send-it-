using UnityEngine;

namespace SendIt.Camera
{
    /// <summary>
    /// Advanced multi-view camera system with smooth transitions.
    /// Provides multiple viewing angles for gameplay and cinematics.
    /// </summary>
    public class AdvancedCameraSystem : MonoBehaviour
    {
        public enum CameraMode
        {
            FirstPerson,    // Driver's seat view
            ThirdPersonClose, // Close third-person behind car
            ThirdPersonFar,  // Far cinematic view
            Hood,           // Hood-mounted camera
            Bumper,         // Rear bumper camera
            Free,           // Free flying camera
            Orbit           // Circling orbit around vehicle
        }

        [SerializeField] private Transform vehicleReference;
        [SerializeField] private float transitionSpeed = 5f;
        [SerializeField] private float mouseSensitivity = 2f;

        private UnityEngine.Camera mainCamera;
        private CameraMode currentMode = CameraMode.ThirdPersonClose;
        private CameraMode targetMode = CameraMode.ThirdPersonClose;

        // Camera positions relative to vehicle
        private Vector3 firstPersonOffset = new Vector3(0, 0.6f, 0.3f);
        private Vector3 thirdPersonCloseOffset = new Vector3(0, 1.5f, -4f);
        private Vector3 thirdPersonFarOffset = new Vector3(0, 3f, -8f);
        private Vector3 hoodOffset = new Vector3(0, 0.3f, 1.5f);
        private Vector3 bumperOffset = new Vector3(0, 0.5f, -2f);

        // Free camera control
        private Vector3 freeCameraPosition = Vector3.zero;
        private Vector3 freeCameraRotation = Vector3.zero;
        private float freeCameraSpeed = 20f;

        // Orbit camera
        private float orbitDistance = 5f;
        private float orbitHeight = 2f;
        private float orbitRotation = 0f;
        private float orbitSpeed = 30f;

        // Smooth damping
        private Vector3 velocitySmoothing = Vector3.zero;
        private float positionDamping = 0.1f;

        // Field of view
        private float defaultFOV = 60f;
        private float firstPersonFOV = 75f;
        private float cinematicFOV = 45f;

        private bool isInitialized;

        public static AdvancedCameraSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize camera system.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;

            mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null)
            {
                mainCamera = gameObject.AddComponent<UnityEngine.Camera>();
            }

            // Find vehicle if not assigned
            if (vehicleReference == null)
            {
                VehicleController vc = FindObjectOfType<VehicleController>();
                if (vc != null)
                    vehicleReference = vc.transform;
            }

            mainCamera.fieldOfView = defaultFOV;
            freeCameraPosition = mainCamera.transform.position;

            isInitialized = true;
            Debug.Log("AdvancedCameraSystem initialized");
        }

        private void LateUpdate()
        {
            if (!isInitialized || vehicleReference == null)
                return;

            // Handle camera mode switching
            HandleCameraInput();

            // Update current camera mode
            UpdateCameraPosition();

            // Apply smooth damping
            ApplyCameraDamping();
        }

        /// <summary>
        /// Handle camera mode switching input.
        /// </summary>
        private void HandleCameraInput()
        {
            // Number keys to switch cameras
            if (Input.GetKeyDown(KeyCode.Alpha1))
                SetCameraMode(CameraMode.FirstPerson);
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                SetCameraMode(CameraMode.ThirdPersonClose);
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                SetCameraMode(CameraMode.ThirdPersonFar);
            else if (Input.GetKeyDown(KeyCode.Alpha4))
                SetCameraMode(CameraMode.Hood);
            else if (Input.GetKeyDown(KeyCode.Alpha5))
                SetCameraMode(CameraMode.Bumper);
            else if (Input.GetKeyDown(KeyCode.Alpha6))
                SetCameraMode(CameraMode.Free);
            else if (Input.GetKeyDown(KeyCode.Alpha7))
                SetCameraMode(CameraMode.Orbit);

            // Free camera controls
            if (currentMode == CameraMode.Free)
            {
                HandleFreeCameraInput();
            }

            // Orbit camera controls
            if (currentMode == CameraMode.Orbit)
            {
                HandleOrbitCameraInput();
            }
        }

        /// <summary>
        /// Update camera position based on current mode.
        /// </summary>
        private void UpdateCameraPosition()
        {
            Vector3 targetPosition = Vector3.zero;
            Quaternion targetRotation = Quaternion.identity;
            float targetFOV = defaultFOV;

            switch (currentMode)
            {
                case CameraMode.FirstPerson:
                    UpdateFirstPersonCamera(out targetPosition, out targetRotation, out targetFOV);
                    break;
                case CameraMode.ThirdPersonClose:
                    UpdateThirdPersonCamera(thirdPersonCloseOffset, out targetPosition, out targetRotation, out targetFOV);
                    break;
                case CameraMode.ThirdPersonFar:
                    UpdateThirdPersonCamera(thirdPersonFarOffset, out targetPosition, out targetRotation, out targetFOV);
                    targetFOV = cinematicFOV;
                    break;
                case CameraMode.Hood:
                    UpdateThirdPersonCamera(hoodOffset, out targetPosition, out targetRotation, out targetFOV);
                    break;
                case CameraMode.Bumper:
                    UpdateThirdPersonCamera(bumperOffset, out targetPosition, out targetRotation, out targetFOV);
                    break;
                case CameraMode.Free:
                    targetPosition = freeCameraPosition;
                    targetRotation = Quaternion.Euler(freeCameraRotation);
                    break;
                case CameraMode.Orbit:
                    UpdateOrbitCamera(out targetPosition, out targetRotation, out targetFOV);
                    break;
            }

            // Smooth transition
            mainCamera.transform.position = Vector3.SmoothDamp(
                mainCamera.transform.position,
                targetPosition,
                ref velocitySmoothing,
                positionDamping
            );
            mainCamera.transform.rotation = Quaternion.Lerp(
                mainCamera.transform.rotation,
                targetRotation,
                Time.deltaTime * transitionSpeed
            );

            // Smooth FOV transition
            mainCamera.fieldOfView = Mathf.Lerp(
                mainCamera.fieldOfView,
                targetFOV,
                Time.deltaTime * transitionSpeed
            );
        }

        /// <summary>
        /// Update first-person camera (driver's seat).
        /// </summary>
        private void UpdateFirstPersonCamera(out Vector3 position, out Quaternion rotation, out float fov)
        {
            position = vehicleReference.position + vehicleReference.TransformDirection(firstPersonOffset);
            rotation = vehicleReference.rotation;
            fov = firstPersonFOV;
        }

        /// <summary>
        /// Update third-person camera views.
        /// </summary>
        private void UpdateThirdPersonCamera(Vector3 offset, out Vector3 position, out Quaternion rotation, out float fov)
        {
            position = vehicleReference.position + vehicleReference.TransformDirection(offset);
            rotation = Quaternion.LookRotation(vehicleReference.position - position + Vector3.up * 0.5f);
            fov = defaultFOV;
        }

        /// <summary>
        /// Update free-flying camera.
        /// </summary>
        private void HandleFreeCameraInput()
        {
            // WASD for movement
            Vector3 moveDirection = Vector3.zero;
            if (Input.GetKey(KeyCode.W))
                moveDirection += mainCamera.transform.forward;
            if (Input.GetKey(KeyCode.S))
                moveDirection -= mainCamera.transform.forward;
            if (Input.GetKey(KeyCode.A))
                moveDirection -= mainCamera.transform.right;
            if (Input.GetKey(KeyCode.D))
                moveDirection += mainCamera.transform.right;
            if (Input.GetKey(KeyCode.Space))
                moveDirection += Vector3.up;
            if (Input.GetKey(KeyCode.LeftControl))
                moveDirection -= Vector3.up;

            freeCameraPosition += moveDirection.normalized * freeCameraSpeed * Time.deltaTime;

            // Mouse for rotation
            freeCameraRotation.y += Input.GetAxis("Mouse X") * mouseSensitivity;
            freeCameraRotation.x -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            freeCameraRotation.x = Mathf.Clamp(freeCameraRotation.x, -90f, 90f);
        }

        /// <summary>
        /// Update orbit camera (circles vehicle).
        /// </summary>
        private void UpdateOrbitCamera(out Vector3 position, out Quaternion rotation, out float fov)
        {
            orbitRotation += Input.GetAxis("Mouse X") * orbitSpeed;
            orbitDistance += Input.GetAxis("Mouse Y") * -0.5f;
            orbitDistance = Mathf.Clamp(orbitDistance, 2f, 15f);

            float x = Mathf.Cos(orbitRotation * Mathf.Deg2Rad) * orbitDistance;
            float z = Mathf.Sin(orbitRotation * Mathf.Deg2Rad) * orbitDistance;

            position = vehicleReference.position + new Vector3(x, orbitHeight, z);
            rotation = Quaternion.LookRotation(vehicleReference.position - position + Vector3.up * 0.5f);
            fov = cinematicFOV;
        }

        /// <summary>
        /// Handle orbit camera input.
        /// </summary>
        private void HandleOrbitCameraInput()
        {
            // Mouse controls handled in UpdateOrbitCamera
        }

        /// <summary>
        /// Apply additional camera damping for smooth motion.
        /// </summary>
        private void ApplyCameraDamping()
        {
            // Additional smoothing can be applied here
        }

        /// <summary>
        /// Set active camera mode.
        /// </summary>
        public void SetCameraMode(CameraMode mode)
        {
            if (targetMode == mode)
                return;

            targetMode = mode;
            currentMode = mode;
            Debug.Log($"Camera mode: {mode}");
        }

        /// <summary>
        /// Get current camera mode.
        /// </summary>
        public CameraMode GetCameraMode() => currentMode;

        /// <summary>
        /// Get list of available camera modes.
        /// </summary>
        public string GetCameraModes()
        {
            return "1: First-Person | 2: Third-Close | 3: Third-Far | 4: Hood | 5: Bumper | 6: Free | 7: Orbit";
        }

        /// <summary>
        /// Set camera transition speed.
        /// </summary>
        public void SetTransitionSpeed(float speed)
        {
            transitionSpeed = speed;
        }

        /// <summary>
        /// Get current camera info.
        /// </summary>
        public string GetCameraInfo()
        {
            return $"Camera: {currentMode}\nFOV: {mainCamera.fieldOfView:F1}°";
        }
    }
}
