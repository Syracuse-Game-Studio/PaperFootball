using UnityEngine;

namespace PaperFootball.Tabletop.FieldGoals
{
    public readonly struct FieldGoalKickResult
    {
        public FieldGoalKickResult(
            bool isValid,
            Vector3 horizontalDirection,
            float forwardImpulse,
            float upwardImpulse,
            float launchAngle,
            float normalizedPower,
            float dragDistance,
            Vector3 totalImpulse)
        {
            IsValid = isValid;
            HorizontalDirection = horizontalDirection;
            ForwardImpulse = forwardImpulse;
            UpwardImpulse = upwardImpulse;
            LaunchAngle = launchAngle;
            NormalizedPower = normalizedPower;
            DragDistance = dragDistance;
            TotalImpulse = totalImpulse;
        }

        public bool IsValid { get; }
        public Vector3 HorizontalDirection { get; }
        public float ForwardImpulse { get; }
        public float UpwardImpulse { get; }
        public float LaunchAngle { get; }
        public float NormalizedPower { get; }
        public float DragDistance { get; }
        public Vector3 TotalImpulse { get; }

        public static FieldGoalKickResult Invalid()
        {
            return new FieldGoalKickResult(false, Vector3.zero, 0f, 0f, 0f, 0f, 0f, Vector3.zero);
        }
    }
}
