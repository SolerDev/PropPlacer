using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PropPlacer.Runtime
{

    [ExecuteAlways]
    public class Prop : MonoBehaviour, ISpawnable, IReposition, IRenamable, IRotate
    {
        [SerializeField] private float _minDistanceToSameProp = 2f;
        [SerializeField] private float _minDistanceToDifferentProp = 0.75f;
        private Vector2 Position => transform.position;
        private Vector2 PointDirection => transform.up;

        [Range(0f, 179f)] [SerializeField] private float _surfaceNormalRange = 10f;
        [Range(0f, 179f)] [SerializeField] private float _pointDirectionRange = 15f;

        private bool HasSurfaceNormalRange => !_surfaceNormalRange.Equals(0f);
        private bool HasPointDirectionRange => !_pointDirectionRange.Equals(0f);
        public float MinDistanceToDifferentProp => _minDistanceToDifferentProp;


        public event Action OnSpawn;
        public event Action<Vector2> OnRotate;
        public event Action<Vector2> OnReposition;

        public string Name { get => name; set => name = value; }

        public void Reposition(Vector2 position)
        {
            transform.position = position;
            OnReposition?.Invoke(position);
        }

        public virtual void Face(Vector2 direction)
        {
            transform.up = direction;
            OnRotate?.Invoke(Vector2.up);
        }

        public virtual void Tilt(float angle)
        {
            transform.Rotate(Vector3.forward, angle);
            OnRotate?.Invoke(Vector2.up);
        }

        private static readonly List<Prop> PROP_OBJS = new List<Prop>();
        public void Spawn() => OnSpawn?.Invoke();
        private void OnEnable() => PROP_OBJS.Add(this);
        private void OnDisable() => PROP_OBJS.Remove(this);







        private void OnDrawGizmosSelected()
        {
            if (HasSurfaceNormalRange)
                DrawSurfaceNormalRange();

            if (HasPointDirectionRange)
                DrawPointDirectionRange();


            Gizmos.DrawWireSphere(Position, _minDistanceToSameProp);
        }

        private void DrawPointDirectionRange()
        {
            Debug.DrawLine(Position, Position + PointDirection.RotatedClockwise(_pointDirectionRange) * 3f, Color.green);
            Debug.DrawLine(Position, Position + PointDirection.RotatedClockwise(-_pointDirectionRange) * 3f, Color.green);
        }

        private void DrawSurfaceNormalRange()
        {
            Debug.DrawLine(Position, Position + PointDirection.RotatedClockwise(_surfaceNormalRange) * 3f, Color.yellow);
            Debug.DrawLine(Position, Position + PointDirection.RotatedClockwise(-_surfaceNormalRange) * 3f, Color.yellow);
        }

        public bool CanBePlacedOnNormal(Vector2 surfaceNormal)
        {
            if (!HasSurfaceNormalRange) return false;


            float dotRange = Vector2.Dot(PointDirection, PointDirection.RotatedClockwise(_surfaceNormalRange));
            float actualDot = Vector2.Dot(PointDirection, surfaceNormal);

            if (actualDot >= dotRange)
                return true;

            if (!PointDirection.x.Equals(0f))
            {
                Vector2 mirroredAngle = PointDirection;
                mirroredAngle.x *= -1f;
                actualDot = Vector2.Dot(mirroredAngle, surfaceNormal);
                return actualDot >= dotRange;
            }

            return false;
        }

        public bool IsFarEnoughtFromOtherProps(Vector2 position)
        {
            var propsOfSameType = PROP_OBJS.Where(propObj => propObj.gameObject.IsOfSamePrefabAs(gameObject));
            var propsOfDifferentType = PROP_OBJS.Except(propsOfSameType);

            bool isFarEnoughtFromSameType = !propsOfSameType.Any(p => IsClosertThan(p, _minDistanceToSameProp));
            bool isFarEnoughtFromDifferentType = !propsOfDifferentType.Any(p =>
            IsClosertThan(p, _minDistanceToDifferentProp) ||
            IsClosertThan(p, p.MinDistanceToDifferentProp));

            return isFarEnoughtFromSameType && isFarEnoughtFromDifferentType;


            bool IsClosertThan(Prop other, float distance)
            {
                Vector2 distanceToProp = (Vector2)other.transform.position - position;
                return Vector2.SqrMagnitude(distanceToProp) < distance * distance;
            }
        }

        public void PointTo(Vector2? surfaceNormal)
        {
            Vector2 targetDirection = surfaceNormal.HasValue //todo:coalesce expression when C#8 in project
                ? surfaceNormal.Value
                : Vector2.zero;

            targetDirection.RotatedClockwise(Vector2.SignedAngle(PointDirection, Vector2.right));

            float offsetRotation = GetValidPointDirectionOffset();
            Vector2 finalRotation = targetDirection.normalized.RotatedClockwise(offsetRotation);
            transform.up = finalRotation;
        }

        private float GetValidPointDirectionOffset()
        {
            if (!HasPointDirectionRange) return 0f;

            float randomRotationOffset = UnityEngine.Random.Range(-_pointDirectionRange, _pointDirectionRange);
            return randomRotationOffset;
        }
    }
}
