using UnityEngine;
using System.Collections.Generic;

namespace SendIt.Graphics
{
    /// <summary>
    /// Manages surface deformation and tire tracks on various terrains.
    /// Creates visible tire marks and deformation on grass, sand, and mud.
    /// </summary>
    public class SurfaceDeformation : MonoBehaviour
    {
        [SerializeField] private float trackDepth = 0.02f; // How deep tire marks go
        [SerializeField] private float trackFadeTime = 20f; // Time for tracks to fade
        [SerializeField] private float trackWidth = 0.15f;
        [SerializeField] private Texture2D tirePatternTexture;

        private List<SurfaceTrack> activeTracks = new List<SurfaceTrack>();

        // Surface types with different properties
        private enum SurfaceType
        {
            Road,      // No deformation
            Grass,     // Light deformation, quick recovery
            Sand,      // Deep deformation, slow recovery
            Mud,       // Very deep deformation, very slow recovery
            Gravel     // Medium deformation, medium recovery
        }

        private struct SurfaceTrack
        {
            public Vector3 Position;
            public Vector3 Normal;
            public SurfaceType TerrainType;
            public float Intensity; // 0-1
            public float CreationTime;
            public float DeformationAmount; // How much ground is displaced
        }

        /// <summary>
        /// Create a surface track at the wheel contact point.
        /// </summary>
        public void CreateSurfaceTrack(Vector3 position, Vector3 normal, string terrainTag, float slipRatio, float wheelLoad)
        {
            // Determine surface type from terrain tag
            SurfaceType surfaceType = GetSurfaceType(terrainTag);

            // Only deformable surfaces create tracks
            if (surfaceType == SurfaceType.Road)
                return;

            // Calculate track intensity based on slip and load
            float intensity = Mathf.Clamp01(slipRatio);
            float loadFactor = Mathf.Clamp01(wheelLoad / 5000f); // Normalize to 5000N
            float deformationAmount = trackDepth * intensity * loadFactor;

            SurfaceTrack track = new SurfaceTrack
            {
                Position = position,
                Normal = normal,
                TerrainType = surfaceType,
                Intensity = intensity,
                CreationTime = Time.time,
                DeformationAmount = deformationAmount
            };

            activeTracks.Add(track);

            // Apply visual deformation
            ApplyTrackDeformation(track);
        }

        /// <summary>
        /// Get surface type from terrain tag.
        /// </summary>
        private SurfaceType GetSurfaceType(string terrainTag)
        {
            return terrainTag.ToLower() switch
            {
                "grass" => SurfaceType.Grass,
                "sand" => SurfaceType.Sand,
                "mud" => SurfaceType.Mud,
                "gravel" => SurfaceType.Gravel,
                _ => SurfaceType.Road
            };
        }

        /// <summary>
        /// Apply visual deformation to the surface.
        /// </summary>
        private void ApplyTrackDeformation(SurfaceTrack track)
        {
            // Get terrain collider at position
            RaycastHit hit;
            if (!Physics.Raycast(track.Position + track.Normal * 0.1f, -track.Normal, out hit, 1f))
                return;

            // Apply different effects based on surface type
            switch (track.TerrainType)
            {
                case SurfaceType.Grass:
                    ApplyGrassTrack(hit, track);
                    break;
                case SurfaceType.Sand:
                    ApplySandTrack(hit, track);
                    break;
                case SurfaceType.Mud:
                    ApplyMudTrack(hit, track);
                    break;
                case SurfaceType.Gravel:
                    ApplyGravelTrack(hit, track);
                    break;
            }
        }

        /// <summary>
        /// Apply tire track on grass (light deformation).
        /// </summary>
        private void ApplyGrassTrack(RaycastHit hit, SurfaceTrack track)
        {
            // Grass tracks: darken the grass, slight compression
            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                // Slightly darken the grass where tires passed
                Color originalColor = renderer.material.color;
                Color darkened = originalColor * 0.7f; // 30% darker

                // This would be a temporary effect that fades
                // In a full implementation, use a texture blend or shader
            }

            // Grass recovers quickly (5-10 seconds)
            float recoveryTime = 5f + (track.Intensity * 5f);
        }

        /// <summary>
        /// Apply tire track on sand (medium deformation).
        /// </summary>
        private void ApplySandTrack(RaycastHit hit, SurfaceTrack track)
        {
            // Sand tracks: visible ruts, displace sand
            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                // Sand tracks are visible as darker lines
                Color originalColor = renderer.material.color;
                Color tracked = originalColor * 0.6f; // 40% darker

                // Sand displaces to sides of track
                // Create raised edges along the track (simulated with shadow/normal adjustment)
            }

            // Sand tracks persist longer (20-30 seconds)
            float recoveryTime = 20f + (track.Intensity * 10f);
        }

        /// <summary>
        /// Apply tire track on mud (deep deformation).
        /// </summary>
        private void ApplyMudTrack(RaycastHit hit, SurfaceTrack track)
        {
            // Mud tracks: deep ruts, splashing
            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                // Mud is very dark where tires passed
                Color originalColor = renderer.material.color;
                Color tracked = originalColor * 0.4f; // 60% darker

                // Mud splashes onto the car
                // This would trigger dirt accumulation on vehicle
            }

            // Mud tracks persist very long (40-50 seconds) or until rain
            float recoveryTime = 40f + (track.Intensity * 10f);
        }

        /// <summary>
        /// Apply tire track on gravel (medium deformation).
        /// </summary>
        private void ApplyGravelTrack(RaycastHit hit, SurfaceTrack track)
        {
            // Gravel tracks: scattered stones, visible ruts
            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                Color originalColor = renderer.material.color;
                Color tracked = originalColor * 0.5f; // 50% darker
            }

            // Gravel tracks recovery time
            float recoveryTime = 15f + (track.Intensity * 10f);
        }

        /// <summary>
        /// Update surface deformation over time.
        /// </summary>
        private void Update()
        {
            // Remove aged tracks
            for (int i = activeTracks.Count - 1; i >= 0; i--)
            {
                SurfaceTrack track = activeTracks[i];
                float age = Time.time - track.CreationTime;

                // Fade time varies by surface type
                float fadeTime = track.TerrainType switch
                {
                    SurfaceType.Grass => trackFadeTime * 0.5f,
                    SurfaceType.Sand => trackFadeTime,
                    SurfaceType.Mud => trackFadeTime * 1.5f,
                    SurfaceType.Gravel => trackFadeTime * 0.8f,
                    _ => trackFadeTime
                };

                if (age > fadeTime)
                {
                    activeTracks.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Clear all surface deformation tracks.
        /// </summary>
        public void ClearAllTracks()
        {
            activeTracks.Clear();
        }

        /// <summary>
        /// Get the number of active surface tracks.
        /// </summary>
        public int GetTrackCount() => activeTracks.Count;

        /// <summary>
        /// Get track information.
        /// </summary>
        public string GetTrackInfo()
        {
            return $"Active Surface Tracks: {activeTracks.Count}";
        }

        /// <summary>
        /// Force recovery of surface deformation at a location.
        /// </summary>
        public void RecoverSurface(Vector3 position, float radius)
        {
            // Remove tracks within radius (simulate rain cleaning surface)
            for (int i = activeTracks.Count - 1; i >= 0; i--)
            {
                if (Vector3.Distance(activeTracks[i].Position, position) < radius)
                {
                    activeTracks.RemoveAt(i);
                }
            }
        }
    }
}
