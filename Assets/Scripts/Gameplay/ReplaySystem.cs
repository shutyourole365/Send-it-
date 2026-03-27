using UnityEngine;
using System.Collections.Generic;
using SendIt.Physics;

namespace SendIt.Gameplay
{
    /// <summary>
    /// Video replay system for recording and playing back vehicle sessions with telemetry overlay.
    /// Captures full vehicle state and telemetry data for analysis and review.
    /// </summary>
    public class ReplaySystem : MonoBehaviour
    {
        /// <summary>
        /// Represents a single frame of replay data.
        /// </summary>
        public struct ReplayFrame
        {
            // Vehicle Transform Data
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Velocity;
            public Vector3 AngularVelocity;

            // Input Data
            public float ThrottleInput;
            public float BrakeInput;
            public float SteerInput;
            public float ClutchInput;
            public int GearInput;

            // Engine Data
            public float EngineRPM;
            public float EnginePower;
            public float EngineTorque;

            // Speed & Distance
            public float Speed;
            public float CumulativeDistance;

            // Suspension Data
            public float[] SuspensionTravel;      // 4 wheels
            public float[] SuspensionForces;      // 4 wheels
            public float[] WheelAngularVelocities; // 4 wheels

            // Tire Data
            public float[] TireTemperatures;      // 4 wheels
            public float[] TireWear;              // 4 wheels
            public float[] TirePressures;         // 4 wheels
            public float[] SlipRatios;            // 4 wheels
            public float[] SlipAngles;            // 4 wheels

            // Dynamics Data
            public float LateralAcceleration;
            public float LongitudinalAcceleration;
            public float VerticalAcceleration;
            public float RollAngle;
            public float PitchAngle;
            public float YawRate;

            // Session Timing
            public float SessionTime;
            public float LapTime;
            public int LapNumber;
        }

        /// <summary>
        /// Complete replay session containing frames and metadata.
        /// </summary>
        public class ReplaySession
        {
            public string SessionName;
            public string TrackName;
            public System.DateTime RecordedDate;
            public float TotalDuration;
            public int TotalFrames;
            public float BestLapTime;
            public int BestLapNumber;
            public Dictionary<string, float> VehicleParameters; // Tuning setup used
            public List<ReplayFrame> Frames;
            public int CurrentFrameIndex;
            public bool IsPlaying;
            public float PlaybackSpeed;
        }

        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private int recordingFPS = 60; // Record at 60 FPS

        private ReplaySession currentSession;
        private bool isRecording;
        private float recordingTimer;
        private float recordingInterval;
        private float cumulativeDistance;

        private const int maxReplayFrames = 36000; // 10 minutes at 60 FPS
        private const string replayDirectory = "Replays";

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (vehicleController == null)
                vehicleController = FindObjectOfType<VehicleController>();

            recordingInterval = 1f / recordingFPS;

            // Ensure replay directory exists
            string replayPath = System.IO.Path.Combine(Application.persistentDataPath, replayDirectory);
            if (!System.IO.Directory.Exists(replayPath))
            {
                System.IO.Directory.CreateDirectory(replayPath);
            }
        }

        /// <summary>
        /// Start recording a new replay session.
        /// </summary>
        public void StartRecording(string sessionName, string trackName = "")
        {
            if (isRecording)
            {
                Debug.LogWarning("Already recording. Stop current recording first.");
                return;
            }

            currentSession = new ReplaySession
            {
                SessionName = sessionName,
                TrackName = trackName,
                RecordedDate = System.DateTime.Now,
                Frames = new List<ReplayFrame>(maxReplayFrames),
                CurrentFrameIndex = 0,
                IsPlaying = false,
                PlaybackSpeed = 1f,
                VehicleParameters = new Dictionary<string, float>()
            };

            isRecording = true;
            recordingTimer = 0f;
            cumulativeDistance = 0f;

            Debug.Log($"Recording started: {sessionName}");
        }

        /// <summary>
        /// Stop recording the current session.
        /// </summary>
        public void StopRecording()
        {
            if (!isRecording)
            {
                Debug.LogWarning("Not currently recording.");
                return;
            }

            isRecording = false;
            currentSession.TotalDuration = recordingTimer;
            currentSession.TotalFrames = currentSession.Frames.Count;

            // Calculate best lap
            float bestLapTime = float.MaxValue;
            int bestLapNum = 0;
            for (int i = 0; i < currentSession.Frames.Count; i++)
            {
                var frame = currentSession.Frames[i];
                if (frame.LapTime > 0 && frame.LapTime < bestLapTime)
                {
                    bestLapTime = frame.LapTime;
                    bestLapNum = frame.LapNumber;
                }
            }

            if (bestLapTime < float.MaxValue)
            {
                currentSession.BestLapTime = bestLapTime;
                currentSession.BestLapNumber = bestLapNum;
            }

            Debug.Log($"Recording stopped. Duration: {currentSession.TotalDuration:F2}s, Frames: {currentSession.TotalFrames}");
        }

        /// <summary>
        /// Capture a frame of replay data. Called every frame during recording.
        /// </summary>
        public void RecordFrame()
        {
            if (!isRecording)
                return;

            recordingTimer += Time.deltaTime;

            // Record at specified FPS
            if (recordingTimer >= recordingInterval)
            {
                recordingTimer -= recordingInterval;

                if (currentSession.Frames.Count >= maxReplayFrames)
                {
                    Debug.LogWarning("Maximum replay frames reached. Recording stopped.");
                    StopRecording();
                    return;
                }

                var frame = CaptureFrameData();
                currentSession.Frames.Add(frame);
            }
        }

        /// <summary>
        /// Capture all relevant vehicle state data for a single frame.
        /// </summary>
        private ReplayFrame CaptureFrameData()
        {
            var frame = new ReplayFrame
            {
                // Transform
                Position = vehicleController.transform.position,
                Rotation = vehicleController.transform.rotation,
                Velocity = vehicleController.GetVelocity(),
                AngularVelocity = vehicleController.GetAngularVelocity(),

                // Input
                ThrottleInput = vehicleController.GetThrottleInput(),
                BrakeInput = vehicleController.GetBrakeInput(),
                SteerInput = vehicleController.GetSteerInput(),
                ClutchInput = vehicleController.GetClutchInput(),
                GearInput = vehicleController.GetCurrentGear(),

                // Timing
                SessionTime = recordingTimer,
                LapTime = vehicleController.GetCurrentLapTime(),
                LapNumber = vehicleController.GetCurrentLapNumber(),
            };

            // Get telemetry if available
            var telemetry = vehicleController.GetTelemetry();
            if (telemetry != null)
            {
                var latestFrame = telemetry.GetLatestFrame();

                frame.EngineRPM = latestFrame.EngineRPM;
                frame.EnginePower = latestFrame.EnginePower;
                frame.EngineTorque = latestFrame.EngineTorque;
                frame.Speed = latestFrame.Speed;

                frame.SuspensionTravel = new float[4];
                frame.SuspensionForces = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    frame.SuspensionTravel[i] = latestFrame.SuspensionTravel[i];
                    frame.SuspensionForces[i] = latestFrame.SuspensionForces[i];
                    frame.WheelAngularVelocities[i] = latestFrame.WheelAngularVelocities[i];
                }

                frame.TireTemperatures = new float[4];
                frame.TireWear = new float[4];
                frame.TirePressures = new float[4];
                frame.SlipRatios = new float[4];
                frame.SlipAngles = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    frame.TireTemperatures[i] = latestFrame.TireTemperatures[i];
                    frame.TireWear[i] = latestFrame.TireWear[i];
                    frame.TirePressures[i] = latestFrame.TirePressures[i];
                    frame.SlipRatios[i] = latestFrame.SlipRatios[i];
                    frame.SlipAngles[i] = latestFrame.SlipAngles[i];
                }

                frame.LateralAcceleration = latestFrame.LateralAcceleration;
                frame.LongitudinalAcceleration = latestFrame.LongitudinalAcceleration;
                frame.VerticalAcceleration = latestFrame.VerticalAcceleration;
                frame.RollAngle = latestFrame.RollAngle;
                frame.PitchAngle = latestFrame.PitchAngle;
                frame.YawRate = latestFrame.YawRate;
            }

            // Calculate cumulative distance
            cumulativeDistance += frame.Speed * Time.deltaTime;
            frame.CumulativeDistance = cumulativeDistance;

            return frame;
        }

        /// <summary>
        /// Start playing back a recorded session.
        /// </summary>
        public bool StartPlayback(ReplaySession session, float playbackSpeed = 1f)
        {
            if (session == null || session.Frames.Count == 0)
            {
                Debug.LogError("Invalid or empty replay session.");
                return false;
            }

            currentSession = session;
            currentSession.IsPlaying = true;
            currentSession.PlaybackSpeed = Mathf.Max(0.1f, playbackSpeed); // Minimum 0.1x speed
            currentSession.CurrentFrameIndex = 0;

            Debug.Log($"Playback started: {session.SessionName} at {playbackSpeed}x speed");
            return true;
        }

        /// <summary>
        /// Stop playback of current session.
        /// </summary>
        public void StopPlayback()
        {
            if (currentSession != null)
            {
                currentSession.IsPlaying = false;
            }
        }

        /// <summary>
        /// Pause playback without stopping.
        /// </summary>
        public void PausePlayback()
        {
            if (currentSession != null)
            {
                currentSession.IsPlaying = false;
            }
        }

        /// <summary>
        /// Resume playback from pause.
        /// </summary>
        public void ResumePlayback()
        {
            if (currentSession != null && !currentSession.IsPlaying)
            {
                currentSession.IsPlaying = true;
            }
        }

        /// <summary>
        /// Update playback position. Call from Update().
        /// </summary>
        public ReplayFrame UpdatePlayback()
        {
            if (currentSession == null || !currentSession.IsPlaying)
                return new ReplayFrame();

            if (currentSession.CurrentFrameIndex >= currentSession.Frames.Count - 1)
            {
                currentSession.IsPlaying = false;
                return new ReplayFrame();
            }

            // Advance frame based on playback speed and deltaTime
            float framesToAdvance = (1f / recordingFPS) * currentSession.PlaybackSpeed / Time.deltaTime;
            currentSession.CurrentFrameIndex = Mathf.Min(
                currentSession.CurrentFrameIndex + (int)framesToAdvance,
                currentSession.Frames.Count - 1
            );

            return currentSession.Frames[currentSession.CurrentFrameIndex];
        }

        /// <summary>
        /// Jump to specific frame in playback.
        /// </summary>
        public void SeekToFrame(int frameIndex)
        {
            if (currentSession == null)
                return;

            currentSession.CurrentFrameIndex = Mathf.Clamp(frameIndex, 0, currentSession.Frames.Count - 1);
        }

        /// <summary>
        /// Jump to specific time in playback.
        /// </summary>
        public void SeekToTime(float time)
        {
            if (currentSession == null)
                return;

            float timePerFrame = 1f / recordingFPS;
            int frameIndex = Mathf.RoundToInt(time / timePerFrame);
            SeekToFrame(frameIndex);
        }

        /// <summary>
        /// Set playback speed multiplier.
        /// </summary>
        public void SetPlaybackSpeed(float speed)
        {
            if (currentSession != null)
            {
                currentSession.PlaybackSpeed = Mathf.Max(0.1f, speed);
            }
        }

        /// <summary>
        /// Save replay session to file.
        /// </summary>
        public bool SaveReplay(ReplaySession session = null)
        {
            var sessionToSave = session ?? currentSession;
            if (sessionToSave == null)
            {
                Debug.LogError("No session to save.");
                return false;
            }

            string replayPath = System.IO.Path.Combine(
                Application.persistentDataPath,
                replayDirectory,
                $"{sessionToSave.SessionName}_{sessionToSave.RecordedDate:yyyyMMdd_HHmmss}.replay"
            );

            try
            {
                // Create a simplified version for serialization
                string json = SerializeReplaySession(sessionToSave);
                System.IO.File.WriteAllText(replayPath, json);
                Debug.Log($"Replay saved: {replayPath}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save replay: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load replay from file.
        /// </summary>
        public ReplaySession LoadReplay(string sessionName, string timestamp = "")
        {
            string replayPath = System.IO.Path.Combine(
                Application.persistentDataPath,
                replayDirectory
            );

            try
            {
                string[] files = System.IO.Directory.GetFiles(replayPath, $"{sessionName}*.replay");
                if (files.Length == 0)
                {
                    Debug.LogError($"No replay found for: {sessionName}");
                    return null;
                }

                string json = System.IO.File.ReadAllText(files[0]);
                ReplaySession session = DeserializeReplaySession(json);
                Debug.Log($"Replay loaded: {sessionName}");
                return session;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load replay: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get list of available replays.
        /// </summary>
        public List<string> GetAvailableReplays()
        {
            var replays = new List<string>();
            string replayPath = System.IO.Path.Combine(
                Application.persistentDataPath,
                replayDirectory
            );

            if (!System.IO.Directory.Exists(replayPath))
                return replays;

            string[] files = System.IO.Directory.GetFiles(replayPath, "*.replay");
            foreach (var file in files)
            {
                replays.Add(System.IO.Path.GetFileNameWithoutExtension(file));
            }

            return replays;
        }

        /// <summary>
        /// Get current playback session.
        /// </summary>
        public ReplaySession GetCurrentSession() => currentSession;

        /// <summary>
        /// Check if currently recording.
        /// </summary>
        public bool IsRecording => isRecording;

        /// <summary>
        /// Check if currently playing.
        /// </summary>
        public bool IsPlaying => currentSession != null && currentSession.IsPlaying;

        // Serialization helpers
        private string SerializeReplaySession(ReplaySession session)
        {
            // Simplified serialization - in production, use a binary format for efficiency
            var wrapper = new ReplaySessionWrapper
            {
                sessionName = session.SessionName,
                trackName = session.TrackName,
                totalDuration = session.TotalDuration,
                totalFrames = session.TotalFrames,
                bestLapTime = session.BestLapTime,
                bestLapNumber = session.BestLapNumber,
                recordedDate = session.RecordedDate.ToString("o")
            };

            return JsonUtility.ToJson(wrapper, true);
        }

        private ReplaySession DeserializeReplaySession(string json)
        {
            var wrapper = JsonUtility.FromJson<ReplaySessionWrapper>(json);
            var session = new ReplaySession
            {
                SessionName = wrapper.sessionName,
                TrackName = wrapper.trackName,
                TotalDuration = wrapper.totalDuration,
                TotalFrames = wrapper.totalFrames,
                BestLapTime = wrapper.bestLapTime,
                BestLapNumber = wrapper.bestLapNumber,
                Frames = new List<ReplayFrame>(),
                IsPlaying = false,
                PlaybackSpeed = 1f
            };

            return session;
        }

        [System.Serializable]
        private class ReplaySessionWrapper
        {
            public string sessionName;
            public string trackName;
            public float totalDuration;
            public int totalFrames;
            public float bestLapTime;
            public int bestLapNumber;
            public string recordedDate;
        }
    }
}
