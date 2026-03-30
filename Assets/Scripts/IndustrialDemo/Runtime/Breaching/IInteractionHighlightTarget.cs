using UnityEngine;

namespace IndustrialDemo.Breaching
{
    public interface IInteractionHighlightTarget
    {
        void SetInteractionHighlight(bool isHighlighted);
        Vector3 GetInteractionWorldPosition();
        Color InteractionIndicatorColor { get; }
    }
}
