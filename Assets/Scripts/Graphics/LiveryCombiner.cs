using UnityEngine;
using System.Collections.Generic;
using SendIt.Data;

namespace SendIt.Graphics
{
    /// <summary>
    /// Manages custom livery and decal system for vehicle personalization.
    /// Dynamically generates textures from decal layers and custom designs.
    /// </summary>
    public class LiveryCombiner : MonoBehaviour
    {
        [SerializeField] private int textureResolution = 2048;

        // Base texture and decal layers
        private Texture2D baseTexture;
        private List<Texture2D> decalLayers = new List<Texture2D>();
        private Texture2D combinedTexture;

        // Material to apply combined texture
        [SerializeField] private Material targetMaterial;

        public struct LiverySettings
        {
            public List<DecalLayer> Decals;
            public Color BaseColor;
            public bool HasCustomLivery;
        }

        [System.Serializable]
        public struct DecalLayer
        {
            public string Name;
            public Texture2D Texture;
            public Vector2 Position;
            public Vector2 Scale;
            public float Rotation;
            public float Opacity;
        }

        public void Initialize(Material material)
        {
            targetMaterial = material;
            CreateBaseTexture();
        }

        /// <summary>
        /// Create a base texture for the livery system.
        /// </summary>
        private void CreateBaseTexture()
        {
            baseTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.ARGB32, false);

            // Fill with white base color
            Color[] pixels = new Color[textureResolution * textureResolution];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            baseTexture.SetPixels(pixels);
            baseTexture.Apply();
        }

        /// <summary>
        /// Add a decal layer to the livery.
        /// </summary>
        public void AddDecalLayer(string name, Texture2D texture, Vector2 position, Vector2 scale, float rotation = 0f, float opacity = 1f)
        {
            DecalLayer layer = new DecalLayer
            {
                Name = name,
                Texture = texture,
                Position = position,
                Scale = scale,
                Rotation = rotation,
                Opacity = Mathf.Clamp01(opacity)
            };

            decalLayers.Add(layer);
            UpdateLiveryTexture();
        }

        /// <summary>
        /// Remove a decal layer by name.
        /// </summary>
        public void RemoveDecalLayer(string name)
        {
            decalLayers.RemoveAll(d => d.Name == name);
            UpdateLiveryTexture();
        }

        /// <summary>
        /// Clear all decal layers.
        /// </summary>
        public void ClearAllDecals()
        {
            decalLayers.Clear();
            UpdateLiveryTexture();
        }

        /// <summary>
        /// Update the combined livery texture with all decal layers.
        /// </summary>
        private void UpdateLiveryTexture()
        {
            // Create a copy of the base texture
            Texture2D workingTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.ARGB32, false);
            Graphics.CopyTexture(baseTexture, workingTexture);

            // Blend each decal layer onto the texture
            foreach (var decal in decalLayers)
            {
                if (decal.Texture == null)
                    continue;

                BlendDecalLayer(workingTexture, decal);
            }

            workingTexture.Apply();
            combinedTexture = workingTexture;

            // Apply to material
            if (targetMaterial != null)
            {
                targetMaterial.mainTexture = combinedTexture;
            }
        }

        /// <summary>
        /// Blend a single decal layer onto the working texture.
        /// </summary>
        private void BlendDecalLayer(Texture2D workingTexture, DecalLayer decal)
        {
            // Create a temporary rendered texture for the decal with transformation
            Texture2D transformedDecal = TransformDecal(decal.Texture, decal.Scale, decal.Rotation);

            // Get pixel data
            Color[] basePixels = workingTexture.GetPixels();
            Color[] decalPixels = transformedDecal.GetPixels();

            // Calculate decal position in texture coordinates
            int decalStartX = (int)(decal.Position.x * textureResolution);
            int decalStartY = (int)(decal.Position.y * textureResolution);

            // Blend decal onto base texture using alpha blending
            for (int i = 0; i < decalPixels.Length; i++)
            {
                Color decalColor = decalPixels[i];
                decalColor.a *= decal.Opacity;

                int decalIndex = i;
                int decalX = decalIndex % transformedDecal.width;
                int decalY = decalIndex / transformedDecal.width;

                int baseX = decalStartX + decalX;
                int baseY = decalStartY + decalY;

                if (baseX >= 0 && baseX < textureResolution && baseY >= 0 && baseY < textureResolution)
                {
                    int baseIndex = baseY * textureResolution + baseX;
                    if (baseIndex < basePixels.Length)
                    {
                        // Alpha blending: result = decal + base * (1 - decal.a)
                        basePixels[baseIndex] = Color.Lerp(basePixels[baseIndex], decalColor, decalColor.a);
                    }
                }
            }

            workingTexture.SetPixels(basePixels);
        }

        /// <summary>
        /// Transform a decal texture (scale and rotation).
        /// </summary>
        private Texture2D TransformDecal(Texture2D sourceTexture, Vector2 scale, float rotation)
        {
            // In a full implementation, this would:
            // 1. Create a new texture with scaled dimensions
            // 2. Apply rotation transformation
            // 3. Return the transformed result

            // For now, return a scaled version
            int newWidth = (int)(sourceTexture.width * scale.x);
            int newHeight = (int)(sourceTexture.height * scale.y);

            Texture2D scaled = new Texture2D(newWidth, newHeight, TextureFormat.ARGB32, false);

            // Simple bilinear scaling
            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    float u = (float)x / newWidth;
                    float v = (float)y / newHeight;

                    Color pixel = sourceTexture.GetPixelBilinear(u, v);
                    scaled.SetPixel(x, y, pixel);
                }
            }

            scaled.Apply();
            return scaled;
        }

        /// <summary>
        /// Export the current livery as a PNG file.
        /// </summary>
        public void ExportLiveryPNG(string filepath)
        {
            if (combinedTexture == null)
            {
                Debug.LogWarning("No livery texture to export");
                return;
            }

            byte[] pngData = combinedTexture.EncodeToPNG();
            System.IO.File.WriteAllBytes(filepath, pngData);
            Debug.Log($"Livery exported to: {filepath}");
        }

        /// <summary>
        /// Import a livery from a PNG file.
        /// </summary>
        public void ImportLiveryPNG(string filepath)
        {
            if (!System.IO.File.Exists(filepath))
            {
                Debug.LogWarning($"File not found: {filepath}");
                return;
            }

            byte[] pngData = System.IO.File.ReadAllBytes(filepath);
            Texture2D importedTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.ARGB32, false);
            importedTexture.LoadImage(pngData);

            baseTexture = importedTexture;
            ClearAllDecals();
            UpdateLiveryTexture();

            Debug.Log($"Livery imported from: {filepath}");
        }

        // Getters
        public List<DecalLayer> GetDecalLayers() => decalLayers;
        public Texture2D GetCombinedTexture() => combinedTexture;
        public int GetDecalCount() => decalLayers.Count;
    }
}
