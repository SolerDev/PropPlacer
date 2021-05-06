using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PropPlacer.Runtime
{
    public static class Extensions
    {
        private static readonly System.Random RND = new System.Random();

        public static bool IsBetweenBothExclusive(this IComparable v, IComparable a, IComparable b) => (v.CompareTo(a) * v.CompareTo(b)).Equals(-1);

        public static T GetRandom<T>(this IEnumerable<T> collection) => collection.ElementAt(RND.Next(0, collection.Count()));

        public static Vector2 GetRandomDirection() => UnityEngine.Random.insideUnitCircle.normalized;

        public static Vector3 With(this Vector3 v, float? x = null, float? y = null, float? z = null)
        {
            if (x != null) v.x = x.Value;
            if (y != null) v.y = y.Value;
            if (z != null) v.z = z.Value;

            return v;
        }

        public static Vector2 RotatedCounterClockwise(this Vector2 v, float deg) => Quaternion.Euler(0, 0, deg) * v;

        public static Vector2 RotatedClockwise(this Vector2 v, float deg) => v.RotatedCounterClockwise(-deg);

        public static Vector2 RandomVector2Range(float xMin, float xMax, float yMin, float yMax) => new Vector2(
                UnityEngine.Random.Range(xMin, xMax),
                UnityEngine.Random.Range(yMin, yMax));

        public static T[] ArrayFiledWithFunctionResults<T>(Func<T> function, int count)
        {
            T[] result = new T[count];
            for (int i = 0; i < count; i++)
                result[i] = function.Invoke();

            return result;
        }

        public static bool IsOfSamePrefabAs(this GameObject a, GameObject b)
        {
            GameObject prefabA = PrefabUtility.GetCorrespondingObjectFromSource(a);
            GameObject prefabB = PrefabUtility.GetCorrespondingObjectFromSource(b);

            return ReferenceEquals(prefabA, prefabB);
        }

    }
}