using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PropPlacer.Runtime
{
    public enum LerpEndType
    {
        OpenEnded,
        Closed
    }
    public enum LerpOvershootType
    {
        Cyclic,
        PingPong
    }

    public static class Extensions
    {
        private static readonly System.Random RND = new System.Random();

        public static Vector2[] ToPoints(this Bounds bounds)
        {
            Vector2[] points = new Vector2[4];
            points[0] = new Vector2(-bounds.extents.x, -bounds.extents.y);
            points[1] = new Vector2(bounds.extents.x, -bounds.extents.y);
            points[2] = new Vector2(bounds.extents.x, bounds.extents.y);
            points[3] = new Vector2(-bounds.extents.x, bounds.extents.y);

            return points;
        }

        public static IEnumerable<Vector2> ExpandedBy(this IEnumerable<Vector2> points, float expansion)
        {
            IList<Vector2> pointList = points.ToList();
            Path[] paths = new Path[pointList.Count];
            for (int i = 0; i < pointList.Count; i++)
            {
                Vector2 start = points.ElementBefore(i);
                Vector2 middle = points.ElementAt(i);
                Vector2 end = points.ElementAfter(i);

                paths[i] = new Path(start, middle, end);
            }

            for (int i = 0; i < pointList.Count; i++)
                pointList[i] = paths[i].Expanded(expansion);

            return pointList;
        }

        public static T ElementBefore<T>(this IEnumerable<T> collection, int index) => index.Equals(0) ? collection.Last() : collection.ElementAt(index - 1);

        public static T ElementAfter<T>(this IEnumerable<T> collection, int index) => index.Equals(collection.Count() - 1) ? collection.First() : collection.ElementAt(index + 1);

        public static Vector2 Lerp(this IEnumerable<Vector2> points, float t, LerpEndType lerpEndType, LerpOvershootType lerpOvershootType)
        {
            t %= 1;
            int pointCount = points.Count();

            List<Edge> edges = new List<Edge>(pointCount);
            for (int i = 0; i < pointCount; i++)
                edges.Add((points.ElementAt(i), points.ElementAfter(i)));

            if (lerpEndType.Equals(LerpEndType.OpenEnded))
                edges.Remove(edges.Last());

            if (lerpOvershootType.Equals(LerpOvershootType.PingPong))
                t *= t % 1 > 0 ? -1 : 1;

            float totalEdgeLength = edges.Sum(edge => edge.Length);
            float targetLength = totalEdgeLength * t;
            return edges.MapLengthToMatchingEdgePoint(targetLength);
        }

        private static Vector2 MapLengthToMatchingEdgePoint(this IEnumerable<Edge> edges, float targetLength)
        {
            //voltar daqui
            int edgeCount = edges.Count();
            float[] cumulativeLengths = new float[edgeCount];
            float cumulativeLength = 0;
            for (int i = 0; i < edgeCount; i++)
            {
                cumulativeLength += edges.ElementAt(i).Length;
                cumulativeLengths[i] = cumulativeLength;
            }

            int ledgeIndex = Array.FindIndex(cumulativeLengths, length => length >= targetLength);

            Edge finalEdge = edges.ElementAt(ledgeIndex);
            float finalCumulativeLength = cumulativeLengths.ElementAt(ledgeIndex);
            float targetEdgeT = 1f - (finalCumulativeLength - targetLength) / finalEdge.Length;

            return finalEdge.Lerp(targetEdgeT);
        }

        public static RaycastHit2D ToPerimeter(this RaycastHit2D hit, Vector2 dir)
        {
            bool queriesStartInColliders = Physics2D.queriesStartInColliders;
            Physics2D.queriesStartInColliders = false;

            hit = Physics2D.Raycast(hit.point, dir, Mathf.Infinity, hit.collider.gameObject.layer << 1);
            hit.normal = -hit.normal;

            Physics2D.queriesStartInColliders = queriesStartInColliders;

            return hit;
        }

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

        public static T[] CreateArrayFiledWithFunctionResults<T>(Func<T> function, int count)
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


        public static bool HasPoints(this Collider2D collider)
        {
            return collider is PolygonCollider2D
                || collider is EdgeCollider2D
                || collider is BoxCollider2D;
        }

        public static Vector2[] GetPoints(this Collider2D collider, Vector2? add = null)
        {
            if (!collider.HasPoints()) throw new ArgumentException("Collider is not of a valid type", nameof(collider));

            Vector2[] points = ToPoints(collider).ToArray();

            if (add.HasValue)
                for (int i = 0; i < points.Length; i++)
                    points[i] += add.Value;

            return points;


            Vector2[] ToPoints(Collider2D collider)
            {
                Vector2[] points = null;

                if (collider is PolygonCollider2D polyColl)
                    points = polyColl.points;
                else if (collider is EdgeCollider2D edgeColl)
                    points = edgeColl.points;
                else if (collider is BoxCollider2D boxColl)
                    points = boxColl.bounds.ToPoints();

                return points;
            }
        }
    }

    internal struct Edge
    {
        public Vector2 Start;
        public Vector2 End;
        public Vector2 Line;
        public float Length;

        public Vector2 Lerp(float t) => Vector2.Lerp(Start, End, t);

        public Edge(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
            Line = end - start;
            Length = Line.magnitude;
        }

        public override bool Equals(object obj) => obj is Edge other && Start.Equals(other.Start) && End.Equals(other.End) && Length == other.Length;

        public override int GetHashCode()
        {
            int hashCode = 142631958;
            hashCode = hashCode * -1521134295 + Start.GetHashCode();
            hashCode = hashCode * -1521134295 + End.GetHashCode();
            return hashCode;
        }

        public void Deconstruct(out Vector2 pointA, out Vector2 pointB, out float length)
        {
            pointA = Start;
            pointB = End;
            length = Length;
        }

        public static implicit operator (Vector2 pointA, Vector2 pointB, float length)(Edge value)
        {
            return (value.Start, value.End, value.Length);
        }

        public static implicit operator Edge((Vector2 pointA, Vector2 pointB) value)
        {
            return new Edge(value.pointA, value.pointB);
        }
    }

    internal struct Path
    {
        public Vector2 Start;
        public Vector2 Middle;
        public Vector2 End;

        private Vector2 _startEdge;
        private Vector2 _endEdge;
        private Vector2 _startNormal;
        private Vector2 _endNormal;


        public Vector2 Expanded(float expansion)
        {
            Vector2 midNormal = (_startNormal + _endNormal).normalized;

            return Middle + midNormal * expansion;
        }

        public Path(Vector2 start, Vector2 middle, Vector2 end)
        {
            Start = start;
            Middle = middle;
            End = end;

            _startEdge = start - middle;
            _endEdge = middle - end;

            _startNormal = Vector2.Perpendicular(_startEdge).normalized;
            _endNormal = Vector2.Perpendicular(_endEdge).normalized;
        }

        public override bool Equals(object obj) => obj is Path other && Start.Equals(other.Start) && Middle.Equals(other.Middle) && End.Equals(other.End);

        public override int GetHashCode()
        {
            int hashCode = -865580495;
            hashCode = hashCode * -1521134295 + Start.GetHashCode();
            hashCode = hashCode * -1521134295 + Middle.GetHashCode();
            hashCode = hashCode * -1521134295 + End.GetHashCode();
            return hashCode;
        }

        public void Deconstruct(out Vector2 start, out Vector2 middle, out Vector2 end)
        {
            start = Start;
            middle = Middle;
            end = End;
        }

        public static implicit operator (Vector2 Start, Vector2 Middle, Vector2 End)(Path value)
        {
            return (value.Start, value.Middle, value.End);
        }

        public static implicit operator Path((Vector2 Start, Vector2 Middle, Vector2 End) value)
        {
            return new Path(value.Start, value.Middle, value.End);
        }
    }
}