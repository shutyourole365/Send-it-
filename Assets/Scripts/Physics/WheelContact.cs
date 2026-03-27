using UnityEngine;
using SendIt.Data;

namespace SendIt.Physics
{
    /// <summary>
    /// Manages wheel-ground contact calculations including normal force,
    /// slip angles, and tire friction forces (Phase 2 enhanced).
    /// </summary>
    public class WheelContact
    {
        private int wheelIndex; // 0=FL, 1=FR, 2=RL, 3=RR
        private bool isGrounded;
        private float normalForce;
        private float previousNormalForce;

        // Contact state
        private float slipAngle = 0f; // Radians
        private float slipRatio = 0f; // 0-1
        private Vector3 contactPoint;
        private Vector3 contactNormal;

        // Tire forces
        private float lateralForce;
        private float longitudinalForce;

        // Configuration
        private float wheelRadius = 0.35f; // meters
        private float wheelMass = 25f; // kg
        private float vehicleWidth = 1.5f; // meters (for load transfer calculations)
        private float wheelBase = 2.7f; // meters (for load transfer calculations)
        private float centerOfGravityHeight = 0.5f; // meters (for load transfer)

        public struct ContactData
        {
            public bool IsGrounded;
            public float NormalForce;
            public float SlipAngle; // Radians
            public float SlipRatio; // 0-1
            public Vector3 ContactPoint;
            public float LateralForce;
            public float LongitudinalForce;
        }

        public WheelContact(int index, float mass = 25f)
        {
            wheelIndex = index;
            wheelMass = mass;
        }

        /// <summary>
        /// Update wheel contact state based on wheel collider data.
        /// </summary>
        public void Update(WheelCollider wheelCollider, Rigidbody vehicleBody, Tire tire)
        {
            if (wheelCollider == null)
            {
                isGrounded = false;
                normalForce = 0f;
                return;
            }

            // Get ground contact
            WheelHit wheelHit;
            isGrounded = wheelCollider.GetGroundHit(out wheelHit);

            if (isGrounded)
            {
                // Calculate normal force from wheel compression
                float compressionFraction = 1f - (wheelHit.distance / wheelCollider.suspensionDistance);
                compressionFraction = Mathf.Clamp01(compressionFraction);

                // Normal force = weight supported + spring force
                float wheelWeight = wheelMass * 9.81f;
                normalForce = wheelWeight + (compressionFraction * 5000f); // Add spring contribution

                contactPoint = wheelHit.point;
                contactNormal = wheelHit.normal;

                // Calculate slip angle (angle between velocity and wheel direction)
                CalculateSlipAngle(wheelCollider, vehicleBody);

                // Calculate slip ratio (longitudinal slip)
                CalculateSlipRatio(wheelCollider, vehicleBody);

                // Calculate tire forces
                if (tire != null)
                {
                    float speed = vehicleBody.velocity.magnitude;
                    lateralForce = tire.CalculateGripForce(slipAngle * Mathf.Rad2Deg, normalForce, speed);
                    longitudinalForce = tire.CalculateLongitudinalForce(slipRatio, normalForce, speed);
                }
            }
            else
            {
                // No contact: reset forces
                normalForce = 0f;
                slipAngle = 0f;
                slipRatio = 0f;
                lateralForce = 0f;
                longitudinalForce = 0f;
            }

            previousNormalForce = normalForce;
        }

        /// <summary>
        /// Calculate slip angle: angle between tire heading and velocity vector (Phase 2 enhanced).
        /// </summary>
        private void CalculateSlipAngle(WheelCollider wheelCollider, Rigidbody vehicleBody)
        {
            if (vehicleBody.velocity.magnitude < 0.1f)
            {
                slipAngle = 0f;
                return;
            }

            // Get wheel direction in world space
            Vector3 wheelDirection = wheelCollider.transform.forward;

            // Get velocity at wheel location (considering angular velocity)
            Vector3 wheelWorldPosition = wheelCollider.transform.position;
            Vector3 velocity = vehicleBody.velocity + Vector3.Cross(vehicleBody.angularVelocity, wheelWorldPosition - vehicleBody.worldCenterOfMass);

            // Project velocity onto ground plane
            Vector3 velocityOnGround = new Vector3(velocity.x, 0f, velocity.z);

            if (velocityOnGround.magnitude < 0.1f)
            {
                slipAngle = 0f;
                return;
            }

            velocityOnGround.Normalize();

            // Calculate angle between wheel direction and velocity (signed)
            slipAngle = Vector3.SignedAngle(wheelDirection, velocityOnGround, Vector3.up) * Mathf.Deg2Rad;

            // Clamp to reasonable range (±90 degrees)
            slipAngle = Mathf.Clamp(slipAngle, -Mathf.PI / 2f, Mathf.PI / 2f);
        }

        /// <summary>
        /// Calculate slip ratio: difference between wheel speed and ground speed (Phase 2 enhanced).
        /// </summary>
        private void CalculateSlipRatio(WheelCollider wheelCollider, Rigidbody vehicleBody)
        {
            if (vehicleBody.velocity.magnitude < 0.1f)
            {
                slipRatio = 0f;
                return;
            }

            // Wheel speed from RPM (linear velocity at tire contact point)
            float wheelRPM = wheelCollider.rpm;
            float wheelLinearSpeed = wheelRPM * 2f * Mathf.PI * wheelRadius / 60f;

            // Vehicle speed in direction of wheel (considering rotation)
            Vector3 wheelWorldPosition = wheelCollider.transform.position;
            Vector3 velocityAtWheel = vehicleBody.velocity + Vector3.Cross(vehicleBody.angularVelocity, wheelWorldPosition - vehicleBody.worldCenterOfMass);
            float vehicleSpeed = Vector3.Dot(velocityAtWheel, wheelCollider.transform.forward);

            // Slip ratio calculation
            // Positive slip ratio = wheel spinning (acceleration)
            // Negative slip ratio = wheel locking (braking)
            if (Mathf.Abs(vehicleSpeed) > 0.1f)
            {
                slipRatio = (wheelLinearSpeed - vehicleSpeed) / Mathf.Abs(vehicleSpeed);
            }
            else if (wheelLinearSpeed > 0.1f)
            {
                // Stationary vehicle with spinning wheel
                slipRatio = 1f;
            }
            else
            {
                slipRatio = 0f;
            }

            // Clamp to realistic range (-2 to 2 for extreme cases)
            slipRatio = Mathf.Clamp(slipRatio, -2f, 2f);
        }

        /// <summary>
        /// Get the total friction force vector for this wheel.
        /// </summary>
        public Vector3 GetFrictionForce(WheelCollider wheelCollider)
        {
            if (!isGrounded)
                return Vector3.zero;

            Vector3 frictionForce = Vector3.zero;

            // Longitudinal component (forward/backward)
            frictionForce += wheelCollider.transform.forward * longitudinalForce;

            // Lateral component (left/right)
            frictionForce += wheelCollider.transform.right * lateralForce;

            return frictionForce;
        }

        /// <summary>
        /// Get downforce contribution from this wheel to improve grip.
        /// </summary>
        public float GetDownforceEffect(float aerodynamicDownforce)
        {
            if (!isGrounded)
                return 0f;

            // Distribute aerodynamic downforce to wheels
            float perWheelDownforce = aerodynamicDownforce / 4f;
            return perWheelDownforce;
        }

        /// <summary>
        /// Calculate load transfer effects based on vehicle acceleration.
        /// Returns adjusted normal load considering lateral and longitudinal load transfer.
        /// </summary>
        public float GetLoadTransferAdjustedForce(float baseNormalForce, Vector3 vehicleAcceleration)
        {
            float adjustedForce = baseNormalForce;

            // Lateral load transfer during cornering
            if (wheelIndex == 0 || wheelIndex == 2) // Left wheels
            {
                adjustedForce += (vehicleAcceleration.x * centerOfGravityHeight / vehicleWidth) * 0.3f;
            }
            else // Right wheels
            {
                adjustedForce -= (vehicleAcceleration.x * centerOfGravityHeight / vehicleWidth) * 0.3f;
            }

            // Longitudinal load transfer during acceleration/braking
            if (wheelIndex < 2) // Front wheels
            {
                adjustedForce -= (vehicleAcceleration.z * centerOfGravityHeight / wheelBase) * 0.4f;
            }
            else // Rear wheels
            {
                adjustedForce += (vehicleAcceleration.z * centerOfGravityHeight / wheelBase) * 0.4f;
            }

            return Mathf.Max(adjustedForce, 100f); // Minimum load
        }

        // Getters for telemetry
        public ContactData GetContactData()
        {
            return new ContactData
            {
                IsGrounded = isGrounded,
                NormalForce = normalForce,
                SlipAngle = slipAngle,
                SlipRatio = slipRatio,
                ContactPoint = contactPoint,
                LateralForce = lateralForce,
                LongitudinalForce = longitudinalForce
            };
        }

        public float GetNormalForce() => normalForce;
        public float GetSlipAngle() => slipAngle;
        public float GetSlipRatio() => slipRatio;
        public float GetLateralForce() => lateralForce;
        public float GetLongitudinalForce() => longitudinalForce;
        public bool IsGrounded => isGrounded;
        public int WheelIndex => wheelIndex;
    }
}
