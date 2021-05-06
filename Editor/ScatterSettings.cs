using UnityEngine;
namespace PropPlacer.Editor
{
    [CreateAssetMenu(fileName = "Scatter", menuName = "PrefabBrush/Scatter Settings")]
    public class ScatterSettings : ScriptableObject
    {
        [Range(0f, 180f)] public float RandomRotationRange = 10f;
    }
}
