using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PropPlacer.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace PropPlacer.Editor
{
    public class PropScatterWindow : PropPlacerWindow
    {
        [MenuItem(MENU_PATH + "Scatter")]
        private static void Open() => GetWindow<PropScatterWindow>();

        private static Collider2D PROP_AREA_COLLIDER;
        private static int POINTS_TO_ATTEMPT_COUNT = 300;
        public static Vector2 COLLIDER_CENTER => PROP_AREA_COLLIDER.bounds.center;

        [Range(0f, 1f)]
        private static float LERP_T = 0f;

        private static float POINT_EXPANSION => PLACE_INSIDE ? 1f : -1f;
        private static bool PLACE_INSIDE = false;

        private void OnEnable() => SceneView.duringSceneGui += DuringSceneGUI;
        private void OnDisable() => SceneView.duringSceneGui -= DuringSceneGUI;

        private void DuringSceneGUI(SceneView obj)
        {
            if (!InternalEditorUtility.isApplicationActive) return;

            if (PROP_AREA_COLLIDER != null)
            {
                Handles.color = Color.yellow;
                Handles.DrawWireCube(PROP_AREA_COLLIDER.bounds.center, PROP_AREA_COLLIDER.bounds.extents * 2f);

                if (Extensions.TryGetColliderPoints(PROP_AREA_COLLIDER, out IList<Vector2> originalPoints, (Vector2)PROP_AREA_COLLIDER.transform.position))
                {
                    IEnumerable<Vector2> expandedPoints = originalPoints.ExpandedBy(POINT_EXPANSION);
                    Vector2 lerpedPoint = expandedPoints.Lerp(LERP_T, LerpEndType.Closed, LerpOvershootType.Cyclic);

                    for (int i = 0; i < expandedPoints.Count(); i++)
                        Debug.DrawLine(expandedPoints.ElementAt(i), expandedPoints.ElementAfter(i), Color.yellow);
                    Handles.DrawWireDisc(lerpedPoint, Vector3.forward, 1f, 2f);
                }
            }
        }

        protected override void OnGUI()
        {
            if (!InternalEditorUtility.isApplicationActive) return;


            base.OnGUI();

            ScaterSettingsGUI();

            ScatterButton();
        }

        private static void ScatterButton()
        {
            if (PROP_AREA_COLLIDER != null && GUILayout.Button("Scatter"))
            {
                if (!IS_TARGETING_SURFACES)
                {
                    Bounds bounds = PROP_AREA_COLLIDER.bounds;
                    Vector2[] points = Extensions.ArrayFiledWithFunctionResults(() =>
                        RandomPointInsideExtents(bounds.extents * 2f), POINTS_TO_ATTEMPT_COUNT);

                    foreach (Vector2 p in points.Where(point => PROP_AREA_COLLIDER.OverlapPoint(point)))
                        TrySpawnProp(p);
                }
                else if (Extensions.TryGetColliderPoints(PROP_AREA_COLLIDER, out IList<Vector2> originalPoints, PROP_AREA_COLLIDER.transform.position))
                {
                    IEnumerable<Vector2> expandedPoints = originalPoints.ExpandedBy(POINT_EXPANSION);

                    IList<RaycastHit2D> hits = new RaycastHit2D[POINTS_TO_ATTEMPT_COUNT];
                    Vector2 originalPoint;
                    Vector2 expandedPoint;
                    float t;

                    for (int i = 0; i < POINTS_TO_ATTEMPT_COUNT; i++)
                    {
                        t = Random.value;
                        originalPoint = originalPoints.Lerp(t, LerpEndType.Closed, LerpOvershootType.Cyclic);
                        expandedPoint = expandedPoints.Lerp(t, LerpEndType.Closed, LerpOvershootType.Cyclic);
                        Vector2 dir = originalPoint - expandedPoint;

                        hits[i] = Physics2D.Raycast(expandedPoint, dir, dir.magnitude * 2f);

                        Debug.DrawLine(originalPoint, expandedPoint, Color.magenta, 5f);
                    }

                    foreach (RaycastHit2D hit in hits)
                        TrySpawnProp(hit.point, hit.normal);
                }
            }

            Vector2 RandomPointInsideExtents(Vector2 area)
            {
                return Extensions.RandomVector2Range(-area.x, area.x, -area.y, area.y);
            }

            Vector2 GetDirectionFromOldToNew(Vector2 point)
            {
                float dir = Mathf.Sign(-POINT_EXPANSION);
                Vector2 final = (COLLIDER_CENTER - point).normalized * dir;

                return final;
            }
        }

        private static RaycastHit2D TryHitSurfacePoint(Vector2 p)
        {
            bool queriesStartInColliders = Physics2D.queriesStartInColliders;
            Physics2D.queriesStartInColliders = false;

            int attempts = 0;
            int maxAttempts = 12;
            float rotationPerAttempt = 360 / maxAttempts;

            RaycastHit2D hit;
            Vector2 initialRayDirection = Extensions.GetRandomDirection();

            do
            {
                hit = Physics2D.Raycast(p, initialRayDirection.RotatedClockwise(attempts * rotationPerAttempt), Mathf.Infinity, SURFACES_MASK);
                attempts++;

            } while (attempts < maxAttempts && (!hit || hit.collider.Equals(PROP_AREA_COLLIDER) || !PROP_AREA_COLLIDER.OverlapPoint(hit.point)));

            Physics2D.queriesStartInColliders = queriesStartInColliders;

            return hit;
        }

        private void ScaterSettingsGUI()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Scatter Settings");

                PROP_AREA_COLLIDER = (Collider2D)EditorGUILayout.ObjectField(PROP_AREA_COLLIDER, typeof(Collider2D), true);
                if (PROP_AREA_COLLIDER != null) Repaint();

                POINTS_TO_ATTEMPT_COUNT = EditorGUILayout.IntField(POINTS_TO_ATTEMPT_COUNT);

                PLACE_INSIDE = EditorGUILayout.Toggle("InsideCollider", PLACE_INSIDE);
            }
        }
    }
}