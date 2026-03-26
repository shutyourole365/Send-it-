using UnityEngine;
using SendIt.Data;

namespace SendIt.Physics
{
    /// <summary>
    /// Manages wheel-ground contact calculations including normal force,
    /// slip angles, and tire friction forces.
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
        /// Calculate slip angle: angle between tire heading and velocity vector.
        /// </summary>
        private void CalculateSlipAngle(WheelCollider wheelCollider, Rigidbody vehicleBody)
        {
            if (vehicleBody.velocity.magnitude < 0.1f)
            {
                slipAngle = 0f;
                return;
            }

            // Get wheel direction
            Vector3 wheelDirection = wheelCollider.transform.forward;

            // Project velocity onto ground plane
            Vector3 velocity = vehicleBody.velocity;
            Vector3 velocityOnGround = new Vector3(velocity.x, 0f, velocity.z).normalized;

            // Calculate angle between wheel direction and velocity
            slipAngle = Vector3.SignedAngle(wheelDirection, velocityOnGround, Vector3.up) * Mathf.Deg2Rad;
            slipAngle = Mathf.Clamp(slipAngle, -Mathf.PI / 4f, Mathf.PI / 4f); // ±45°
        }

        /// <summary>
        /// Calculate slip ratio: difference between wheel speed and ground speed.
        /// </summary>
        private void CalculateSlipRatio(WheelCollider wheelCollider, Rigidbody vehicleBody)
        {
            if (vehicleBody.velocity.magnitude < 0.1f)
            {
                slipRatio = 0f;
                return;
            }

            // Wheel speed from RPM
            float wheelRPM = wheelCollider.rpm;
            float wheelLinearSpeed = wheelRPM * 2f * Mathf.PI * wheelRadius / 60f;

            // Vehicle speed in direction of wheel
            float vehicleSpeed = Vector3.Dot(vehicleBody.velocity, wheelCollider.transform.forward);

            // Slip ratio = (wheel speed - vehicle speed) / vehicle speed
            if (Mathf.Abs(vehicleSpeed) > 0.1f)
            {
                slipRatio = (wheelLinearSpeed - vehicleSpeed) / Mathf.Abs(vehicleSpeed);
            }
            else
            {
                slipRatio = 0f;
            }

            slipRatio = Mathf.Clamp(slipRatio, -1f, 1f);
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
