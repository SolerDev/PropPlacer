using System;
using UnityEngine;

namespace PropPlacer.Runtime
{
    public interface IRotate
    {
        event Action<Vector2> OnRotate;
        void Face(Vector2 direction);
        void Tilt(float angle);
    }
}
