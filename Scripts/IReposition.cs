using System;
using UnityEngine;

namespace PropPlacer.Runtime
{
    public interface IReposition
    {
        event Action<Vector2> OnReposition;
        void Reposition(Vector2 position);
    }
}
