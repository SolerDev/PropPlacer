using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PropPlacer.Runtime
{
    [ExecuteAlways]
    public class Prop : MonoBehaviour, ISpawnable, IReposition, IRenamable
    {
        private static readonly List<Prop> PROP_OBJS = new List<Prop>();


        [SerializeField] private float _minDistanceToSameProp = 2f;
        [SerializeField] private float _minDistanceToDifferentProp = 0.75f;
        [SerializeField] private bool _flipOnSpawn = true;
        [Range(0f, 180f)] [SerializeField] private float _surfaceNormalRange = 10f;
        [Range(0f, 180f)] [SerializeField] private float _pointDirectionRange = 15f;

        private Transform Transf
        {
            get
            {
                if (_transform == null)
                    _transform = transform;

                return _transform;
            }
        }
        private Transform _transform;

        private GameObject PrefabReference
        {
            get
            {
                if (_prefabReference == null)
                    _prefabReference = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);

                return _prefabReference;
            }
        }
        private GameObject _prefabReference;

        public Vector2 Position => Transf.position;
        private Vector2 PointDirection => Transf.up;

        public float MinDistanceToDifferentProp => _minDistanceToDifferentProp;

        public event Action OnSpawn;
        public event Action<Vector2> OnRotate;
        public event Action<Vector2> OnReposition;

        public string Name { get => name; set => name = value; }

        private void OnEnable() => PROP_OBJS.Add(this);
        private void OnDisable() => PROP_OBJS.Remove(this);
        private void OnDrawGizmosSelected()
        {
            DrawSurfaceNormalRange();
            DrawPointDirectionRange();
            Gizmos.DrawWireSphere(Position, _minDistanceToSameProp);

            void DrawPointDirectionRange()
            {
                Debug.DrawLine(Position, Position + PointDirection.RotatedClockwise(_pointDirectionRange) * 3f, Color.green);
                Debug.DrawLine(Position, Position + PointDirection.RotatedClockwise(-_pointDirectionRange) * 3f, Color.green);
            }

            void DrawSurfaceNormalRange()
            {
                Debug.DrawLine(Position, Position + PointDirection.RotatedClockwise(_surfaceNormalRange) * 3f, Color.yellow);
                Debug.DrawLine(Position, Position + PointDirection.RotatedClockwise(-_surfaceNormalRange) * 3f, Color.yellow);
            }
        }

        
        
        public void Spawn()
        {
            OnSpawn?.Invoke();

            if (_flipOnSpawn && UnityEngine.Random.value < 0.5f)
            {
                Vector3 scale = Transf.localScale;
                scale.x = -1f;
                Transf.localScale = scale;
            }
        }

        public void Reposition(Vector2 position)
        {
            Transf.position = position;
            OnReposition?.Invoke(position);
        }

        public void PointTo(Vector2? surfaceNormal)
        {
            Vector2 targetDirection = surfaceNormal.HasValue //todo:coalesce expression when C#8 in project
                ? surfaceNormal.Value
                : Vector2.zero;

            targetDirection.RotatedClockwise(Vector2.SignedAngle(PointDirection, Vector2.right));

            float offsetRotation = UnityEngine.Random.Range(-_pointDirectionRange, _pointDirectionRange);
            Vector2 finalRotation = targetDirection.normalized.RotatedClockwise(offsetRotation);
            Transf.up = finalRotation;
        }


        public bool CanBePlacedOnNormal(Vector2 surfaceNormal)
        {
            if (_surfaceNormalRange.Equals(0f)) return false;
            if (_surfaceNormalRange.Equals(180f)) return true;


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

        public bool TargetPositionIsFarEnoughtFromOtherProps(Vector2 targetPosition)
        {
            var propsOfSameType = PROP_OBJS.Where(propObj => propObj.PrefabReference.Equals(this.PrefabReference));
            var propsOfDifferentType = PROP_OBJS.Except(propsOfSameType);

            bool isFarEnoughtFromSameType = !propsOfSameType.Any(p => IsClosertThan(p, _minDistanceToSameProp));
            bool isFarEnoughtFromDifferentType = !propsOfDifferentType.Any(p =>
            IsClosertThan(p, this.MinDistanceToDifferentProp) ||
            IsClosertThan(this, p.MinDistanceToDifferentProp));

            return isFarEnoughtFromSameType && isFarEnoughtFromDifferentType;


            bool IsClosertThan(Prop other, float distance)
            {
                Vector2 distanceToProp = other.Position - targetPosition;
                return Vector2.SqrMagnitude(distanceToProp) < distance * distance;
            }
        }
    }
}
