using System;
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

        [Range(0f, 1f)] private static float LERP_T = 0f;

        private static float POINT_EXPANSION => PLACE_INSIDE ? 1f : -1f;
        private static bool PLACE_INSIDE = false;

        private void OnEnable() => SceneView.duringSceneGui += DuringSceneGUI;
        private void OnDisable() => SceneView.duringSceneGui -= DuringSceneGUI;
        protected override void OnGUI()
        {
            if (!InternalEditorUtility.isApplicationActive) return;


            base.OnGUI();

            ScaterSettingsGUI();

            ScatterButton();
        }

        private void DuringSceneGUI(SceneView obj)
        {
            if (!InternalEditorUtility.isApplicationActive) return;


            if (PROP_AREA_COLLIDER != null && PROP_AREA_COLLIDER.HasPoints())
            {
                Vector2[] originalPoints = PROP_AREA_COLLIDER.GetPoints(PROP_AREA_COLLIDER.transform.position);
                Vector2[] expandedPoints = originalPoints.ExpandedBy(POINT_EXPANSION).ToArray();

                for (int i = 0; i < expandedPoints.Length; i++)
                    Debug.DrawLine(expandedPoints[i], expandedPoints[i], Color.yellow);

                Handles.color = Color.yellow;
                Handles.DrawWireCube(PROP_AREA_COLLIDER.bounds.center, PROP_AREA_COLLIDER.bounds.extents * 2f);
                Vector2 lerpedPoint = expandedPoints.Lerp(LERP_T, LerpEndType.Closed, LerpOvershootType.Cyclic);
                Handles.DrawWireDisc(lerpedPoint, Vector3.forward, 1f);
            }
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




        //razão de ser
        private static void ScatterButton()
        {
            if (PROP_AREA_COLLIDER != null && GUILayout.Button("Scatter"))
            {
                if (!IS_TARGETING_SURFACES)
                    SpawnRandomlyInsideCollider();
                else if (PROP_AREA_COLLIDER.HasPoints())
                    SpawnOnColliderSurface();
            }


            void SpawnRandomlyInsideCollider()
            {
                Bounds bounds = PROP_AREA_COLLIDER.bounds;
                Vector2[] points = Extensions.CreateArrayFiledWithFunctionResults(() =>
                {
                    Vector2 randomPointInsideArea = RandomPointInsideArea(bounds.extents);
                    return randomPointInsideArea + (Vector2)bounds.center;
                }, POINTS_TO_ATTEMPT_COUNT);


                foreach (Vector2 p in points.Where(point => ColliderOverlapsPoint(point) == PLACE_INSIDE))
                    TrySpawnProp(p);


                Vector2 RandomPointInsideArea(Vector2 area) => Extensions.RandomVector2Range(-area.x, area.x, -area.y, area.y);

                bool ColliderOverlapsPoint(Vector2 point)
                {
                    if (PROP_AREA_COLLIDER is EdgeCollider2D edge)
                    {
                        var poly = edge.gameObject.AddComponent<PolygonCollider2D>();
                        poly.points = edge.points;
                        bool polyOverlaps = poly.OverlapPoint(point);

                        DestroyImmediate(poly);
                        return polyOverlaps;
                    }

                    return PROP_AREA_COLLIDER.OverlapPoint(point);
                }
            }

            void SpawnOnColliderSurface()
            {
                Vector2[] colliderPoints = PROP_AREA_COLLIDER.GetPoints(PROP_AREA_COLLIDER.transform.position);
                Vector2[] expandedPoints = colliderPoints.ExpandedBy(POINT_EXPANSION).ToArray();

                IList<RaycastHit2D> hits = new RaycastHit2D[POINTS_TO_ATTEMPT_COUNT];
                Vector2 originalPoint;
                Vector2 expandedPoint;
                float t;

                for (int i = 0; i < POINTS_TO_ATTEMPT_COUNT; i++)
                {
                    t = UnityEngine.Random.value;
                    originalPoint = colliderPoints.Lerp(t, LerpEndType.Closed, LerpOvershootType.Cyclic);
                    expandedPoint = expandedPoints.Lerp(t, LerpEndType.Closed, LerpOvershootType.Cyclic);
                    Vector2 dir = originalPoint - expandedPoint;

                    hits[i] = Physics2D.Raycast(expandedPoint, dir, dir.magnitude * 2f);

                    Debug.DrawLine(originalPoint, expandedPoint, Color.magenta, 5f);
                }

                foreach (RaycastHit2D hit in hits/*.Where(hit =>  hit.collider.Equals(PROP_AREA_COLLIDER))*/)
                    TrySpawnProp(hit.point, hit.normal);
            }
        }
    }
}