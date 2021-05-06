using UnityEngine;

namespace PropPlacer.Runtime
{
    public interface IHaveRotationRestrictions
    {
        bool CanFaceDirection(Vector2 normal);
        void RotateToValidRotation();
    }
}
