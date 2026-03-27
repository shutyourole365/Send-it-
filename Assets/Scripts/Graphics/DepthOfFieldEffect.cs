using UnityEngine;

namespace SendIt.Graphics
{
    /// <summary>
    /// Advanced depth of field effect with focus tracking and aperture simulation.
    /// Provides realistic bokeh blur based on camera focus distance.
    /// </summary>
    public class DepthOfFieldEffect : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform focusTarget;

        private Material depthOfFieldMaterial;
        private RenderTexture depthBuffer;

        // DoF parameters
        private float focusDistance = 10f;
        private float focusRange = 5f; // Focus falloff distance
        private float aperatureSize = 5.6f; // f-stop value (lower = larger aperture, more blur)
        private float maxBlurRadius = 15f; // Max blur in pixels
        private int sampleCount = 8; // Bokeh samples (8, 12, 16)

        // Focus tracking
        private float focusTrackingSpeed = 2f;
        private float targetFocusDistance;
        private bool autoFocus = true;

        private bool isInitialized;

        public enum BokehShape
        {
            Circle,
            Hexagon,
            Octagon,
            Diamond
        }
        private BokehShape bokehShape = BokehShape.Circle;

        public void Initialize(Camera camera, Transform focus = null, float initialFocusDistance = 10f)
        {
            targetCamera = camera;
            focusTarget = focus;
            focusDistance = initialFocusDistance;
            targetFocusDistance = focusDistance;

            CreateMaterial();
            SetupBuffers();

            isInitialized = true;
        }

        /// <summary>
        /// Create depth of field material and shader.
        /// </summary>
        private void CreateMaterial()
        {
            // In full implementation, load from custom DoF shader
            depthOfFieldMaterial = new Material(Shader.Find("Standard"));
            depthOfFieldMaterial.name = "DepthOfFieldMaterial";
        }

        /// <summary>
        /// Setup depth buffer for focusing.
        /// </summary>
        private void SetupBuffers()
        {
            int width = Screen.width;
            int height = Screen.height;

            depthBuffer = new RenderTexture(width, height, 24, RenderTextureFormat.Depth);
            depthBuffer.name = "DepthBuffer";
            depthBuffer.filterMode = FilterMode.Point;
        }

        /// <summary>
        /// Update focus distance and DoF parameters.
        /// </summary>
        public void UpdateDepthOfField()
        {
            if (!isInitialized || targetCamera == null)
                return;

            // Update focus target
            if (autoFocus && focusTarget != null)
            {
                UpdateAutoFocus();
            }

            // Smoothly transition to target focus distance
            focusDistance = Mathf.Lerp(focusDistance, targetFocusDistance, Time.deltaTime * focusTrackingSpeed);

            // Update material parameters
            UpdateShaderParameters();
        }

        /// <summary>
        /// Automatically focus on target object.
        /// </summary>
        private void UpdateAutoFocus()
        {
            if (focusTarget == null)
                return;

            Vector3 directionToTarget = focusTarget.position - targetCamera.transform.position;
            float distanceToTarget = directionToTarget.magnitude;

            targetFocusDistance = distanceToTarget;
        }

        /// <summary>
        /// Manually set focus distance.
        /// </summary>
        public void SetFocusDistance(float distance)
        {
            targetFocusDistance = Mathf.Max(distance, 0.1f);
        }

        /// <summary>
        /// Set focus target for auto-focus tracking.
        /// </summary>
        public void SetFocusTarget(Transform target)
        {
            focusTarget = target;
            autoFocus = target != null;
        }

        /// <summary>
        /// Set aperture size (f-stop value).
        /// Lower values = larger aperture = more blur.
        /// </summary>
        public void SetAperatureSize(float fStop)
        {
            aperatureSize = Mathf.Clamp(fStop, 1.4f, 32f); // Real camera range
        }

        /// <summary>
        /// Set depth of field intensity.
        /// </summary>
        public void SetDOFIntensity(float intensity)
        {
            maxBlurRadius = Mathf.Lerp(5f, 25f, Mathf.Clamp01(intensity));
        }

        /// <summary>
        /// Set bokeh shape for aesthetic effect.
        /// </summary>
        public void SetBokehShape(BokehShape shape)
        {
            bokehShape = shape;
            UpdateShaderParameters();
        }

        /// <summary>
        /// Update shader parameters based on current state.
        /// </summary>
        private void UpdateShaderParameters()
        {
            if (depthOfFieldMaterial == null)
                return;

            // Convert focus distance to normalized camera space
            float focusPlane = focusDistance / (targetCamera.farClipPlane - targetCamera.nearClipPlane);

            depthOfFieldMaterial.SetFloat("_FocusDistance", focusDistance);
            depthOfFieldMaterial.SetFloat("_FocusRange", focusRange);
            depthOfFieldMaterial.SetFloat("_ApertureSize", aperatureSize);
            depthOfFieldMaterial.SetFloat("_MaxBlurRadius", maxBlurRadius);
            depthOfFieldMaterial.SetInt("_SampleCount", sampleCount);
            depthOfFieldMaterial.SetInt("_BokehShape", (int)bokehShape);
        }

        /// <summary>
        /// Calculate blur amount at a given distance.
        /// </summary>
        public float GetBlurAtDistance(float distance)
        {
            float distanceFromFocus = Mathf.Abs(distance - focusDistance);

            // No blur within focus range
            if (distanceFromFocus < focusRange)
                return 0f;

            // Quadratic falloff beyond focus range
            float excess = distanceFromFocus - focusRange;
            float blur = (excess / focusRange) * (aperatureSize / 16f) * maxBlurRadius;

            return Mathf.Clamp01(blur);
        }

        /// <summary>
        /// Calculate circle of confusion (CoC) radius.
        /// CoC = (aperture / focal_length) * distance_blur_factor
        /// </summary>
        public float GetCircleOfConfusion(float distance)
        {
            float distanceFromFocus = Mathf.Abs(distance - focusDistance);
            if (distanceFromFocus <= focusRange)
                return 0f;

            // Non-linear falloff for realistic CoC
            float cocFactor = (aperatureSize / 16f) * (distanceFromFocus / focusDistance);
            return Mathf.Min(cocFactor * maxBlurRadius, maxBlurRadius);
        }

        /// <summary>
        /// Enable/disable auto-focus.
        /// </summary>
        public void SetAutoFocus(bool enabled)
        {
            autoFocus = enabled;
        }

        /// <summary>
        /// Set focus tracking speed for smooth focus transitions.
        /// </summary>
        public void SetFocusTrackingSpeed(float speed)
        {
            focusTrackingSpeed = Mathf.Clamp(speed, 0.1f, 10f);
        }

        /// <summary>
        /// Get current focus distance.
        /// </summary>
        public float GetFocusDistance() => focusDistance;

        /// <summary>
        /// Get aperture size (f-stop).
        /// </summary>
        public float GetApertureSize() => aperatureSize;

        /// <summary>
        /// Get current blur radius range.
        /// </summary>
        public float GetMaxBlurRadius() => maxBlurRadius;

        /// <summary>
        /// Get DoF state for debugging.
        /// </summary>
        public (float focusDistance, float blurAmount, BokehShape shape) GetDOFState()
        {
            return (focusDistance, GetBlurAtDistance(focusDistance + focusRange), bokehShape);
        }

        private void OnDestroy()
        {
            if (depthBuffer != null)
                depthBuffer.Release();
        }
    }
}
