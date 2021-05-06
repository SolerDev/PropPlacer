using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace PropPlacer.Editor
{
	[CreateAssetMenu(fileName = "Collection", menuName = "PrefabBrush/Collection")]
	public class PrefabCollection : ScriptableObject
	{
		private static readonly System.Random RND = new System.Random();

		[SerializeField] private List<GameObject> _prefabs;
		[NonReorderable] [SerializeField] private List<int> _chances;

		public GameObject GetRandom()
		{
			int rnd = RND.Next(0, _chances.Sum());
			int i = 0;

			foreach (int weight in _chances)
			{
				if (rnd < weight) break;

				rnd -= _chances[i];
				i++;
			}

			return _prefabs[i];
		}



		private void OnValidate()
		{
			_chances.Sort();
		}
	}
}