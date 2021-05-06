using UnityEngine;

namespace PropPlacer.Editor
{
	[CreateAssetMenu(fileName = "Brush", menuName = "PrefabBrush/Brush Settings")]
	public class BrushSettings : ScriptableObject
	{
		public float BrushSize = 5f;
		[Range(1, 100)] public int SpawnRate = 2;
	}
}
