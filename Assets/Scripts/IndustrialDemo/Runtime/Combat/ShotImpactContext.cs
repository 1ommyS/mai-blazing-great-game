using UnityEngine;

namespace IndustrialDemo.Combat
{
    public readonly struct ShotImpactContext
    {
        public ShotImpactContext(
            RaycastHit hit,
            Vector3 incomingDirection,
            float damage,
            float remainingPenetration,
            int ricochetCount,
            GameObject source)
        {
            Hit = hit;
            IncomingDirection = incomingDirection;
            Damage = damage;
            RemainingPenetration = remainingPenetration;
            RicochetCount = ricochetCount;
            Source = source;
        }

        public RaycastHit Hit { get; }
        public Vector3 IncomingDirection { get; }
        public float Damage { get; }
        public float RemainingPenetration { get; }
        public int RicochetCount { get; }
        public GameObject Source { get; }
    }
}
