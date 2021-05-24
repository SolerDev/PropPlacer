using System.Collections.Generic;
using System.Linq;
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

        [Range(0f, 1f)]
        private static float LERP_T = 0f;

        private void OnEnable() => SceneView.duringSceneGui += DuringSceneGUI;
        private void OnDisable() => SceneView.duringSceneGui -= DuringSceneGUI;

        private void DuringSceneGUI(SceneView obj)
        {
            _sceneView = obj;
            if (!InternalEditorUtility.isApplicationActive) return;

            if (PROP_AREA_COLLIDER != null)
            {
                Handles.color = Color.yellow;
                Handles.DrawWireCube(PROP_AREA_COLLIDER.bounds.center, PROP_AREA_COLLIDER.bounds.extents * 2f);

                Vector2[] points = default;
                if (PROP_AREA_COLLIDER is PolygonCollider2D polyColl)
                    points = polyColl.points;
                else if (PROP_AREA_COLLIDER is EdgeCollider2D edgeColl)
                    points = edgeColl.points;

                if (points != null)
                {
                    Vector2 lerpedPoint = points.Lerp(LERP_T) + (Vector2)PROP_AREA_COLLIDER.transform.position;
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
                Bounds bounds = PROP_AREA_COLLIDER.bounds;
                Vector2[] points = Extensions.ArrayFiledWithFunctionResults(() =>
                    RandomPointInsideExtents(bounds.extents * 2f), POINTS_TO_ATTEMPT_COUNT);

                if (!IS_TARGETING_SURFACES)
                    foreach (Vector2 p in points.Where(point => PROP_AREA_COLLIDER.OverlapPoint(point)))
                        TrySpawnProp(p);
                else
                {
                    //voltar daqui > trocar forma de captação de pontos
                    IEnumerable<RaycastHit2D> hits = points.Select(p => TryHitSurfacePoint(p))
                                                           .Where(hit => hit && PROP_AREA_COLLIDER.OverlapPoint(hit.point));

                    foreach (RaycastHit2D hit in hits)
                        TrySpawnProp(hit.point, hit.normal);
                }
            }

            Vector2 RandomPointInsideExtents(Vector2 area)
            {
                return Extensions.RandomVector2Range(-area.x, area.x, -area.y, area.y);
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

                float prevLerpT = LERP_T;
                LERP_T = EditorGUILayout.Slider(LERP_T, 0f, 1f);
                if (!LERP_T.Equals(prevLerpT))
                {
                    _sceneView.Repaint();
                }
            }
        }
        private SceneView _sceneView;
    }
}