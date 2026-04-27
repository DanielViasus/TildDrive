using UnityEngine;

namespace TiltDrive.VehicleSystem
{
    public class VehicleTransformController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private VehicleOutputStore vehicleOutputStore;

        [Header("Aplicacion")]
        [SerializeField] private bool autoFindReferences = true;
        [SerializeField] private bool applyPosition = true;
        [SerializeField] private bool applyRotation = true;
        [SerializeField] private bool useFixedUpdate = false;

        [Header("Ejes")]
        [SerializeField] private Vector3 localForwardAxis = Vector3.forward;
        [SerializeField] private Vector3 worldUpAxis = Vector3.up;

        [Header("Suavizado")]
        [SerializeField] [Min(0f)] private float rotationSharpness = 18f;

        private void Awake()
        {
            EnsureReferences();
        }

        private void Update()
        {
            if (useFixedUpdate) return;

            ApplyTransform(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!useFixedUpdate) return;

            ApplyTransform(Time.fixedDeltaTime);
        }

        private void EnsureReferences()
        {
            if (!autoFindReferences || vehicleOutputStore != null)
            {
                return;
            }

            vehicleOutputStore = VehicleOutputStore.Instance;

            if (vehicleOutputStore == null)
            {
                vehicleOutputStore = FindFirstObjectByType<VehicleOutputStore>();
            }
        }

        private void ApplyTransform(float deltaTime)
        {
            EnsureReferences();

            if (vehicleOutputStore == null || vehicleOutputStore.Current == null)
            {
                return;
            }

            VehicleOutputState state = vehicleOutputStore.Current;
            float safeDeltaTime = Mathf.Max(0.0001f, deltaTime);

            Vector3 upAxis = GetSafeUpAxis();
            Quaternion headingRotation = Quaternion.AngleAxis(state.headingDegrees, upAxis);
            Quaternion targetRotation = headingRotation * Quaternion.FromToRotation(GetSafeForwardAxis(), Vector3.forward);

            if (applyRotation)
            {
                float rotationBlend = rotationSharpness <= 0f
                    ? 1f
                    : 1f - Mathf.Exp(-rotationSharpness * safeDeltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationBlend);
            }

            if (applyPosition)
            {
                Vector3 forward = headingRotation * Vector3.forward;
                transform.position += forward * state.finalSpeedMS * safeDeltaTime;
            }
        }

        private Vector3 GetSafeForwardAxis()
        {
            return localForwardAxis.sqrMagnitude <= 0.0001f
                ? Vector3.forward
                : localForwardAxis.normalized;
        }

        private Vector3 GetSafeUpAxis()
        {
            return worldUpAxis.sqrMagnitude <= 0.0001f
                ? Vector3.up
                : worldUpAxis.normalized;
        }
    }
}
