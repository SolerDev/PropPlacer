using System.Collections.Generic;
using PropPlacer.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace PropPlacer.Editor
{
    public class PropPainterWindow : PropPlacerWindow
    {
        protected static readonly List<RaycastHit2D> SURFACES_POINTS_HIT = new List<RaycastHit2D>(64);

        public static BrushSettings BRUSH_SETTINGS;
        public static float BRUSH_SIZE = 5f;
        public static bool IS_USING_BRUSH_SETTINGS => BRUSH_SETTINGS != null;

        [MenuItem(MENU_PATH + "Painter")]
        private static void Open() => GetWindow<PropPainterWindow>();


        private static int CIRCUNFERENCE_DIVISIONS => Mathf.Max(48, Mathf.RoundToInt(BRUSH_SIZE * 0.85f));
        private const float SPIN_SPEED = 10f;
        private static float TIME_OFFSET => (float)EditorApplication.timeSinceStartup * SPIN_SPEED;
        private static bool IS_PAINTING = false;

        private void OnEnable() => SceneView.duringSceneGui += DuringSceneGUI;

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;

            IS_PAINTING = false;
            SURFACES_POINTS_HIT.Clear();
        }

        private void DuringSceneGUI(SceneView view)
        {
            if (!InternalEditorUtility.isApplicationActive) return;

            //setup

            Event e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button.Equals(1))
                    {
                        IS_PAINTING = true;
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (e.button.Equals(1)) IS_PAINTING = false;
                    break;
                case EventType.MouseMove:
                    view.Repaint();
                    break;
                case EventType.ScrollWheel:
                    if (e.modifiers.HasFlag(EventModifiers.Alt))
                    {
                        float scrollDir = -Mathf.Sign(e.delta.y);
                        BRUSH_SIZE *= 1 + (scrollDir * 0.05f);
                        e.Use();
                    }
                    break;
            }

            Vector2 brushPosition = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin.With(z: 0);
            SURFACES_POINTS_HIT.Clear();


            using (new Handles.DrawingScope(IS_PAINTING ? Color.red : Color.white))
            {
                if (IS_TARGETING_SURFACES)
                {
                    for (int i = 0; i < CIRCUNFERENCE_DIVISIONS; i++)
                    {
                        Vector2 dir = Vector2.up.RotatedCounterClockwise((i + TIME_OFFSET) * 360 / CIRCUNFERENCE_DIVISIONS);
                        Handles.DrawLine(brushPosition, brushPosition + dir * BRUSH_SIZE);

                        RaycastHit2D surfacePointHit = IS_TARGETING_SURFACES
                            ? Physics2D.Raycast(brushPosition, dir, BRUSH_SIZE, SURFACES_MASK)
                            : Physics2D.Raycast(brushPosition, dir, BRUSH_SIZE);


                        if (surfacePointHit)
                            SURFACES_POINTS_HIT.Add(surfacePointHit.ToPerimeter());
                    }

                    if (IS_PAINTING && SURFACES_POINTS_HIT.Count > 0)
                    {
                        RaycastHit2D rndHit = SURFACES_POINTS_HIT.GetRandom();
                        TrySpawnProp(rndHit.point, rndHit.normal);
                    }
                }
                else
                {
                    Vector2 rndPoint = brushPosition + UnityEngine.Random.insideUnitCircle * BRUSH_SIZE;
                    Handles.DrawWireDisc(rndPoint, Vector3.forward, 0.3f);
                    if (IS_PAINTING) TrySpawnProp(rndPoint);
                }


                // visuals
                Handles.DrawWireDisc(brushPosition, Vector3.forward, BRUSH_SIZE);
                foreach (RaycastHit2D hit in SURFACES_POINTS_HIT)
                    Handles.DrawWireDisc(hit.point, Vector3.forward, 0.25f);
            }
        }

        protected override void OnGUI()
        {
            base.OnGUI();


            BrushSettingsGUI();
        }

        private static void BrushSettingsGUI()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Brush Settings");
                BRUSH_SETTINGS = (BrushSettings)EditorGUILayout.ObjectField(BRUSH_SETTINGS, typeof(BrushSettings), false);
                if (!IS_USING_BRUSH_SETTINGS)
                    BRUSH_SIZE = EditorGUILayout.FloatField(nameof(BRUSH_SIZE), BRUSH_SIZE);
            }
        }
    }
}