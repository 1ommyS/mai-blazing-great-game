using UnityEngine;

namespace IndustrialDemo.Foam
{
    public interface IFoamHighlightTarget
    {
        string FoamActionLabel { get; }
        string FoamPurposeLabel { get; }
        Color FoamHighlightColor { get; }
        void SetFoamHighlight(bool isHighlighted);
    }
}
