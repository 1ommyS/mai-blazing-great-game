using UnityEngine;

namespace IndustrialDemo.Combat
{
    public interface IShotDamageReceiver
    {
        void ReceiveShotDamage(ShotImpactContext context);
    }
}
