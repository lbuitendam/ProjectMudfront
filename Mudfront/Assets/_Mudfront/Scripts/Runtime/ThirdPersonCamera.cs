using UnityEngine;

namespace Mudfront
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        public ThirdPersonController Target;
        public Vector3 ShoulderOffset = new Vector3(0.6f, 0f, -4f);
        public float PivotHeight = 1.6f;
        public Vector3 VehicleOffset = new Vector3(0f, 1.2f, -7.5f);
        public float CollisionRadius = 0.25f;

        void LateUpdate()
        {
            if (Target == null) return;
            bool driving = Target.CurrentVehicle != null;
            var anchor = driving ? Target.CurrentVehicle.transform : Target.transform;
            var pivot = anchor.position + Vector3.up * (driving ? 1.8f : PivotHeight);
            var rot = Quaternion.Euler(Target.Pitch, Target.Yaw, 0f);
            var offset = rot * (driving ? VehicleOffset : ShoulderOffset);
            float dist = offset.magnitude;
            var dir = offset / dist;
            if (Physics.SphereCast(pivot, CollisionRadius, dir, out var hit, dist, ~0, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(Target.transform) && !(driving && hit.transform.IsChildOf(anchor)))
                dist = Mathf.Max(0.5f, hit.distance - 0.1f);
            transform.SetPositionAndRotation(pivot + dir * dist, rot);
        }
    }
}
