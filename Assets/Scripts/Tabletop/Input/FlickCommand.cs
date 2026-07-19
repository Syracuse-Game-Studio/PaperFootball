using UnityEngine;

namespace PaperFootball.Tabletop.Input
{
    public struct FlickCommand
    {
        public FlickCommand(
            bool isValid,
            Vector3 dragStartWorld,
            Vector3 currentWorld,
            Vector3 releaseWorld,
            Vector3 direction,
            float force,
            float dragDistance,
            float dragDuration,
            float strength01)
            : this(
                isValid,
                dragStartWorld,
                currentWorld,
                releaseWorld,
                direction,
                force,
                dragDistance,
                dragDuration,
                strength01,
                dragStartWorld,
                false)
        {
        }

        public FlickCommand(
            bool isValid,
            Vector3 dragStartWorld,
            Vector3 currentWorld,
            Vector3 releaseWorld,
            Vector3 direction,
            float force,
            float dragDistance,
            float dragDuration,
            float strength01,
            Vector3 contactPointWorld)
            : this(
                isValid,
                dragStartWorld,
                currentWorld,
                releaseWorld,
                direction,
                force,
                dragDistance,
                dragDuration,
                strength01,
                contactPointWorld,
                true)
        {
        }

        private FlickCommand(
            bool isValid,
            Vector3 dragStartWorld,
            Vector3 currentWorld,
            Vector3 releaseWorld,
            Vector3 direction,
            float force,
            float dragDistance,
            float dragDuration,
            float strength01,
            Vector3 contactPointWorld,
            bool hasContactPoint)
        {
            IsValid = isValid;
            DragStartWorld = dragStartWorld;
            CurrentWorld = currentWorld;
            ReleaseWorld = releaseWorld;
            Direction = direction;
            Force = force;
            DragDistance = dragDistance;
            DragDuration = dragDuration;
            Strength01 = strength01;
            ContactPointWorld = contactPointWorld;
            HasContactPoint = hasContactPoint;
        }

        public bool IsValid { get; }
        public Vector3 DragStartWorld { get; }
        public Vector3 CurrentWorld { get; }
        public Vector3 ReleaseWorld { get; }
        public Vector3 Direction { get; }
        public float Force { get; }
        public float DragDistance { get; }
        public float DragDuration { get; }
        public float Strength01 { get; }
        public Vector3 ContactPointWorld { get; }
        public bool HasContactPoint { get; }

        public static FlickCommand Invalid(Vector3 dragStartWorld, Vector3 currentWorld, float dragDuration)
        {
            return new FlickCommand(false, dragStartWorld, currentWorld, currentWorld, Vector3.zero, 0f, 0f, dragDuration, 0f);
        }

        public static FlickCommand Invalid(Vector3 dragStartWorld, Vector3 currentWorld, float dragDuration, Vector3 contactPointWorld)
        {
            return new FlickCommand(false, dragStartWorld, currentWorld, currentWorld, Vector3.zero, 0f, 0f, dragDuration, 0f, contactPointWorld);
        }
    }
}
