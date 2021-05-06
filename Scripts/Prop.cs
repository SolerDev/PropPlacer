using System;
using System.Collections.Generic;
using System.Linq;
using PropPlacer;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

namespace PropPlacer.Runtime
{

    [ExecuteAlways]
    public class Prop : MonoBehaviour, ISpawnable, IReposition, IRenamable, IRotate
    {
        [SerializeField] private float _minDistanceToSameProp = 1f;
        [SerializeField] private bool _isSurfaceProp = true;
        private Vector2 Position => transform.position;
        private Vector2 PointDirection => transform.up;

        [ShowIf(nameof(_isSurfaceProp))] [Range(0f, 180f)] [SerializeField] private float _surfaceNormalRange = 10f;
        [ShowIf(nameof(_isSurfaceProp))] [Range(0f, 180f)] [SerializeField] private float _pointDirectionRange = 15f;

        public bool IsSurfaceProp => _isSurfaceProp;
        private bool HasSurfaceNormalRange => IsSurfaceProp && !_surfaceNormalRange.Equals(Vector2.zero);
        public bool HasPointDirectionRange => IsSurfaceProp && !_pointDirectionRange.Equals(Vector2.zero);


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

        private static readonly List<GameObject> PROP_OBJS = new List<GameObject>();
        public void Spawn() => OnSpawn?.Invoke();
        private void OnEnable() => PROP_OBJS.Add(gameObject);
        private void OnDisable() => PROP_OBJS.Remove(gameObject);







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
            if (!IsSurfaceProp) return false;

            if (HasSurfaceNormalRange)
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

        public bool HasDuplicateWithinMinDistance(Vector2 point)
        {
            IEnumerable<GameObject> nearbyProps = PROP_OBJS.Where(obj =>
            {
                Vector2 distanceToProp = (Vector2)obj.transform.position - point;
                return Vector2.SqrMagnitude(distanceToProp) < _minDistanceToSameProp * _minDistanceToSameProp;
            });

            return nearbyProps.Any(propObj => propObj.IsOfSamePrefabAs(gameObject));
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
