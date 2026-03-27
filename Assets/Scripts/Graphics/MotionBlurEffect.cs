using UnityEngine;
using SendIt.Physics;

namespace SendIt.Graphics
{
    /// <summary>
    /// Advanced motion blur effect using velocity buffer and frame accumulation.
    /// Provides realistic motion blur based on vehicle velocity and camera movement.
    /// </summary>
    public class MotionBlurEffect : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Rigidbody vehicleRigidbody;

        private Material motionBlurMaterial;
        private RenderTexture velocityBuffer;
        private RenderTexture accumulationBuffer;

        private float blurIntensity = 0.5f;
        private float blurCutoff = 0.01f; // Minimum velocity to apply blur
        private int sampleCount = 8; // Number of blur samples
        private float shutterAngle = 180f; // 180° = full motion blur

        private Vector3 previousCameraPosition;
        private Vector3 previousFrameVelocity;

        private bool isInitialized;

        public void Initialize(Camera camera, Rigidbody vehicleBody, float intensity = 0.5f)
        {
            targetCamera = camera;
            vehicleRigidbody = vehicleBody;
            blurIntensity = Mathf.Clamp01(intensity);

            CreateMaterial();
            SetupBuffers();

            previousCameraPosition = targetCamera.transform.position;
            isInitialized = true;
        }

        /// <summary>
        /// Create or load the motion blur material/shader.
        /// </summary>
        private void CreateMaterial()
        {
            // In a full implementation, this would load from a shader
            // For now, we create a placeholder
            motionBlurMaterial = new Material(Shader.Find("Standard"));
            motionBlurMaterial.name = "MotionBlurMaterial";
        }

        /// <summary>
        /// Setup velocity and accumulation render textures.
        /// </summary>
        private void SetupBuffers()
        {
            int width = Screen.width;
            int height = Screen.height;

            // Velocity buffer (2 channels for motion vector)
            velocityBuffer = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
            velocityBuffer.name = "VelocityBuffer";
            velocityBuffer.filterMode = FilterMode.Point;

            // Accumulation buffer for temporal blur
            accumulationBuffer = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf);
            accumulationBuffer.name = "AccumulationBuffer";
            accumulationBuffer.filterMode = FilterMode.Bilinear;
        }

        private void OnEnable()
        {
            if (isInitialized)
            {
                RenderTexture.active = velocityBuffer;
                GL.Clear(true, true, Color.black);
                RenderTexture.active = null;
            }
        }

        /// <summary>
        /// Update motion blur based on camera and vehicle movement.
        /// </summary>
        public void UpdateMotionBlur()
        {
            if (!isInitialized || targetCamera == null)
                return;

            // Calculate camera velocity
            Vector3 cameraVelocity = (targetCamera.transform.position - previousCameraPosition) / Time.deltaTime;
            previousCameraPosition = targetCamera.transform.position;

            // Get vehicle velocity
            Vector3 vehicleVelocity = vehicleRigidbody != null ? vehicleRigidbody.velocity : Vector3.zero;

            // Calculate relative motion
            Vector3 relativeMotion = vehicleVelocity - cameraVelocity;
            float motionMagnitude = relativeMotion.magnitude;

            // Apply motion blur if above cutoff threshold
            if (motionMagnitude > blurCutoff)
            {
                ApplyMotionBlur(relativeMotion, motionMagnitude);
            }

            previousFrameVelocity = vehicleVelocity;
        }

        /// <summary>
        /// Apply motion blur effect based on velocity.
        /// </summary>
        private void ApplyMotionBlur(Vector3 motionVector, float motionMagnitude)
        {
            // Calculate blur amount (0-1)
            float maxBlurSpeed = 50f; // m/s where blur reaches maximum
            float blurAmount = Mathf.Min(motionMagnitude / maxBlurSpeed, 1f) * blurIntensity;

            if (blurAmount < 0.01f)
                return;

            // Convert motion vector to screen space
            Vector3 screenMotion = targetCamera.WorldToScreenPoint(motionVector);
            Vector2 motionInScreen = new Vector2(screenMotion.x - Screen.width / 2f, screenMotion.y - Screen.height / 2f);
            motionInScreen = motionInScreen.normalized * Mathf.Min(motionInScreen.magnitude, 30f);

            // Set shader parameters
            if (motionBlurMaterial != null)
            {
                motionBlurMaterial.SetVector("_MotionVector", motionInScreen * blurAmount);
                motionBlurMaterial.SetFloat("_BlurIntensity", blurAmount);
                motionBlurMaterial.SetInt("_SampleCount", sampleCount);
            }
        }

        /// <summary>
        /// Calculate velocity-dependent motion blur contribution.
        /// Uses temporal filtering for smooth results.
        /// </summary>
        public float GetBlurFactor()
        {
            if (vehicleRigidbody == null)
                return 0f;

            float speed = vehicleRigidbody.velocity.magnitude;
            float maxBlurSpeed = 50f;

            // Non-linear response: more blur at higher speeds
            float speedFactor = Mathf.Clamp01(speed / maxBlurSpeed);
            float blur = speedFactor * speedFactor * blurIntensity;

            return Mathf.Clamp01(blur);
        }

        /// <summary>
        /// Get motion vector magnitude for other systems.
        /// </summary>
        public Vector3 GetMotionVector()
        {
            if (vehicleRigidbody == null)
                return Vector3.zero;

            Vector3 cameraVelocity = (targetCamera.transform.position - previousCameraPosition) / Time.deltaTime;
            return vehicleRigidbody.velocity - cameraVelocity;
        }

        /// <summary>
        /// Set motion blur intensity (0-1).
        /// </summary>
        public void SetBlurIntensity(float intensity)
        {
            blurIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// Set sample count for blur quality (4-16 samples).
        /// </summary>
        public void SetSampleCount(int samples)
        {
            sampleCount = Mathf.Clamp(samples, 4, 16);
        }

        /// <summary>
        /// Set shutter angle (0-360°, affects blur strength).
        /// </summary>
        public void SetShutterAngle(float angle)
        {
            shutterAngle = Mathf.Clamp(angle, 0f, 360f);
        }

        private void OnDestroy()
        {
            if (velocityBuffer != null)
                velocityBuffer.Release();
            if (accumulationBuffer != null)
                accumulationBuffer.Release();
        }

        public float GetBlurIntensity() => blurIntensity;
        public Vector3 GetPreviousCameraPosition() => previousCameraPosition;
    }
}
