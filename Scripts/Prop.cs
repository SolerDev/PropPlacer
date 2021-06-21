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

        private bool HasSurfaceNormalRange => !_surfaceNormalRange.Equals(Vector2.zero);
        private bool HasPointDirectionRange => !_pointDirectionRange.Equals(Vector2.zero);
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
            else
            {
                float normalAngle = Vector2.SignedAngle(surfaceNormal, Vector2.right);

                float minNormalAngle = Vector2.SignedAngle(PointDirection.RotatedClockwise(_surfaceNormalRange), Vector2.right);
                float maxNormalAngle = Vector2.SignedAngle(PointDirection.RotatedClockwise(-_surfaceNormalRange), Vector2.right);

                if (!normalAngle.IsBetweenBothExclusive(minNormalAngle, maxNormalAngle))
                {
                    normalAngle = Vector2.SignedAngle(surfaceNormal, Vector2.left);
                    Vector2 reflectedPointDirection = Vector2.Reflect(PointDirection, Vector2.up);
                    minNormalAngle = Vector2.SignedAngle(reflectedPointDirection.RotatedClockwise(_surfaceNormalRange), Vector2.right);
                    maxNormalAngle = Vector2.SignedAngle(reflectedPointDirection.RotatedClockwise(-_surfaceNormalRange), Vector2.right);

                    if (!normalAngle.IsBetweenBothExclusive(minNormalAngle, maxNormalAngle))
                        return false;
                }
            }


            return true;
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

        public void Rotate(Vector2? surfaceNormal)
        {
            Vector2 newRotation = GetValidPointDirection();
            if (surfaceNormal.HasValue)
                newRotation.x *= Mathf.Sign(surfaceNormal.Value.x);

            transform.up = newRotation;
        }

        private Vector2 GetValidPointDirection()
        {
            if (HasPointDirectionRange)
            {
                float randomRotationOffset = UnityEngine.Random.Range(-_pointDirectionRange, _pointDirectionRange);
                return PointDirection.RotatedClockwise(randomRotationOffset);
            }

            return PointDirection;
        }
    }
}
