using System;

namespace PropPlacer.Runtime
{
    public interface ISpawnable
    {
        event Action OnSpawn;
        void Spawn();
    }
}
