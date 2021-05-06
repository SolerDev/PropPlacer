using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using PropPlacer.Runtime;

namespace PropPlacer.Editor
{
    public class PrefabScatterWindow : PrefabPlacerWindow
    {
        [MenuItem(MENU_PATH + "Scatter")]
        private static void Open() => GetWindow<PrefabScatterWindow>();

        private static Collider2D COLLIDER;
        private static int POINTS_TO_ATTEMPT_COUNT = 300;
        private const float SURFACE_DISTANCE = 10f;

        private void OnEnable() => SceneView.duringSceneGui += DuringSceneGUI;
        private void OnDisable() => SceneView.duringSceneGui -= DuringSceneGUI;

        private void DuringSceneGUI(SceneView obj)
        {
            if (!InternalEditorUtility.isApplicationActive) return;

            if (COLLIDER != null)
            {
                Handles.color = Color.yellow;
                Handles.DrawWireCube(COLLIDER.bounds.center, COLLIDER.bounds.extents * 2f);
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
            if (COLLIDER != null && GUILayout.Button("Scatter"))
            {
                Bounds bounds = COLLIDER.bounds;
                Vector2[] points = Extensions.ArrayFiledWithFunctionResults(() =>
                    RandomPointInsideExtents(bounds.extents * 2f), POINTS_TO_ATTEMPT_COUNT);

                if (!IS_TARGETING_SURFACES)
                    foreach (Vector2 p in points.Where(point => COLLIDER.OverlapPoint(point)))
                        TrySpawnProp(p);
                else
                {
                    IEnumerable<RaycastHit2D> hits = points.Select(p => TryHitSurfacePoint(p))
                                                           .Where(hit => hit && COLLIDER.OverlapPoint(hit.point));

                    foreach (RaycastHit2D hit in hits)
                        TrySpawnProp(hit.point, hit.normal);
                }
            }

            static Vector2 RandomPointInsideExtents(Vector2 area) => Extensions.RandomVector2Range(-area.x, area.x, -area.y, area.y);
        }

        private static RaycastHit2D TryHitSurfacePoint(Vector2 p)
        {
            int attempts = 0;
            int maxAttempts = 12;
            float rotationPerAttempt = 360 / maxAttempts;

            RaycastHit2D hit;
            Vector2 initialRayDirection = Extensions.GetRandomDirection();

            do
            {
                hit = Physics2D.Raycast(p, initialRayDirection.RotatedClockwise(attempts * rotationPerAttempt), SURFACE_DISTANCE, SURFACES_MASK);
                attempts++;

            } while (attempts < maxAttempts && (!hit || hit.collider.Equals(COLLIDER) || !COLLIDER.OverlapPoint(hit.point)));

            return hit;
        }

        private void ScaterSettingsGUI()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Scatter Settings");

                COLLIDER = (Collider2D)EditorGUILayout.ObjectField(COLLIDER, typeof(Collider2D), true);
                if (COLLIDER != null) Repaint();

                POINTS_TO_ATTEMPT_COUNT = EditorGUILayout.IntField(POINTS_TO_ATTEMPT_COUNT);
            }
        }
    }
}