using UnityEngine;
using System.Collections.Generic;

namespace SendIt.Graphics
{
    /// <summary>
    /// Manages dynamic skid marks on road surfaces.
    /// Creates persistent marks that fade over time with heat-based coloration.
    /// </summary>
    public class SkidMarkSystem : MonoBehaviour
    {
        [SerializeField] private Material skidMarkMaterial;
        [SerializeField] private float markFadeTime = 10f; // How long before marks fade completely
        [SerializeField] private float markWidth = 0.15f; // Width of skid mark
        [SerializeField] private float minSlipForMark = 0.1f; // Minimum slip ratio to create marks

        private List<SkidMark> activeMarks = new List<SkidMark>();
        private List<SkidMarkQuad> markQuads = new List<SkidMarkQuad>();

        // Mesh management for rendering marks
        private Mesh markMesh;
        private MeshFilter markMeshFilter;
        private MeshRenderer markRenderer;

        private struct SkidMark
        {
            public Vector3 Position;
            public Vector3 Normal;
            public float Intensity; // 0-1, based on slip ratio
            public float Heat; // 0-1, based on tire temperature
            public float CreationTime;
            public int MarkIndex;
        }

        private struct SkidMarkQuad
        {
            public Vector3[] Vertices;
            public Color[] Colors;
            public float CreationTime;
            public float Alpha; // Current alpha for fading
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize the skid mark system.
        /// </summary>
        private void Initialize()
        {
            // Create mesh for skid marks
            markMesh = new Mesh();
            markMesh.name = "SkidMarkMesh";

            // Get or create mesh renderer
            markMeshFilter = gameObject.AddComponent<MeshFilter>();
            markMeshFilter.mesh = markMesh;

            markRenderer = gameObject.AddComponent<MeshRenderer>();
            if (skidMarkMaterial == null)
            {
                skidMarkMaterial = new Material(Shader.Find("Standard"));
                skidMarkMaterial.SetFloat("_Mode", 3); // Transparent mode
                skidMarkMaterial.renderQueue = 3000; // Render after most objects
            }
            markRenderer.material = skidMarkMaterial;
        }

        /// <summary>
        /// Create a skid mark at the specified location.
        /// </summary>
        public void CreateSkidMark(Vector3 position, Vector3 normal, float tireTemperature, float slipRatio, float slipAngle)
        {
            // Only create marks if slip is significant enough
            if (slipRatio < minSlipForMark)
                return;

            // Calculate mark properties based on tire condition
            float intensity = Mathf.Clamp01(slipRatio);
            float heatFactor = Mathf.Clamp01(tireTemperature / 130f); // Normalized to max temp
            float slipAngleFactor = Mathf.Abs(slipAngle) * 0.5f; // Angle affects mark darkness

            // Create mark color based on heat
            Color markColor = GetMarkColor(tireTemperature, intensity, slipAngleFactor);

            // Add mark quad
            CreateMarkQuad(position, normal, markColor, intensity);

            // Update mesh
            UpdateMarkMesh();
        }

        /// <summary>
        /// Create a quad for the skid mark.
        /// </summary>
        private void CreateMarkQuad(Vector3 position, Vector3 normal, Color color, float intensity)
        {
            // Calculate perpendicular vector for mark width
            Vector3 direction = Vector3.Cross(normal, Vector3.up).normalized;
            if (direction.magnitude < 0.1f)
                direction = Vector3.Cross(normal, Vector3.forward).normalized;

            // Create quad vertices
            Vector3[] vertices = new Vector3[4];
            vertices[0] = position - direction * (markWidth * 0.5f) + normal * 0.001f;
            vertices[1] = position + direction * (markWidth * 0.5f) + normal * 0.001f;
            vertices[2] = position + direction * (markWidth * 0.5f) + normal * 0.001f;
            vertices[3] = position - direction * (markWidth * 0.5f) + normal * 0.001f;

            // Create colors with intensity
            Color[] colors = new Color[4];
            for (int i = 0; i < 4; i++)
            {
                colors[i] = color;
                colors[i].a = intensity;
            }

            SkidMarkQuad quad = new SkidMarkQuad
            {
                Vertices = vertices,
                Colors = colors,
                CreationTime = Time.time,
                Alpha = intensity
            };

            markQuads.Add(quad);
        }

        /// <summary>
        /// Get skid mark color based on tire temperature and slip.
        /// </summary>
        private Color GetMarkColor(float tireTemperature, float slipIntensity, float slipAngleFactor)
        {
            // Cold tires = light grey marks
            // Hot tires = dark black marks
            // Very hot (overheating) = reddish marks

            Color markColor;

            if (tireTemperature < 40f)
            {
                // Cold: light grey
                markColor = Color.Lerp(new Color(0.8f, 0.8f, 0.8f), new Color(0.5f, 0.5f, 0.5f), (tireTemperature - 20f) / 20f);
            }
            else if (tireTemperature < 85f)
            {
                // Warming up: medium grey
                markColor = Color.Lerp(new Color(0.5f, 0.5f, 0.5f), new Color(0.2f, 0.2f, 0.2f), (tireTemperature - 40f) / 45f);
            }
            else if (tireTemperature < 120f)
            {
                // Optimal to hot: dark black
                markColor = Color.Lerp(new Color(0.2f, 0.2f, 0.2f), new Color(0f, 0f, 0f), (tireTemperature - 85f) / 35f);
            }
            else
            {
                // Overheating: dark with red tint
                float overheatFactor = (tireTemperature - 120f) / 10f;
                markColor = Color.Lerp(Color.black, new Color(0.3f, 0f, 0f), overheatFactor);
            }

            // Darken based on slip angle (more aggressive slip = darker marks)
            markColor *= (1f - slipAngleFactor * 0.3f);

            return markColor;
        }

        /// <summary>
        /// Update the mesh with current mark quads.
        /// </summary>
        private void UpdateMarkMesh()
        {
            if (markQuads.Count == 0)
                return;

            List<Vector3> vertices = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<int> triangles = new List<int>();

            // Build mesh from quads
            for (int i = 0; i < markQuads.Count; i++)
            {
                // Add vertices
                for (int j = 0; j < 4; j++)
                {
                    vertices.Add(markQuads[i].Vertices[j]);
                    colors.Add(markQuads[i].Colors[j]);
                }

                // Add triangles (2 triangles per quad)
                int baseIndex = i * 4;
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);

                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);
            }

            // Apply to mesh
            markMesh.Clear();
            markMesh.vertices = vertices.ToArray();
            markMesh.colors = colors.ToArray();
            markMesh.triangles = triangles.ToArray();
            markMesh.RecalculateNormals();
        }

        /// <summary>
        /// Update mark fading over time.
        /// </summary>
        private void Update()
        {
            // Fade out old marks
            for (int i = markQuads.Count - 1; i >= 0; i--)
            {
                SkidMarkQuad quad = markQuads[i];
                float age = Time.time - quad.CreationTime;

                if (age > markFadeTime)
                {
                    markQuads.RemoveAt(i);
                }
                else
                {
                    // Fade alpha
                    float fadeAlpha = Mathf.Lerp(1f, 0f, age / markFadeTime);
                    quad.Alpha = fadeAlpha;
                    markQuads[i] = quad;
                }
            }

            // Update mesh if marks changed
            if (markQuads.Count > 0)
            {
                UpdateMarkMesh();
            }
        }

        /// <summary>
        /// Clear all skid marks.
        /// </summary>
        public void ClearAllMarks()
        {
            markQuads.Clear();
            if (markMesh != null)
                markMesh.Clear();
        }

        /// <summary>
        /// Get the number of active skid marks.
        /// </summary>
        public int GetMarkCount() => markQuads.Count;

        /// <summary>
        /// Get information about mark visibility.
        /// </summary>
        public string GetMarkInfo()
        {
            return $"Active Skid Marks: {markQuads.Count}";
        }
    }
}
