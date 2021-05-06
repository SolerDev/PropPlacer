using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using PropPlacer.Runtime;

namespace PropPlacer.Editor
{
    public abstract class PrefabPlacerWindow : EditorWindow
    {
        protected const string MENU_PATH = "Window/Prop/";

        private static GameObject LAST_CREATED_OBJ;
        #region Properties

        public static PrefabCollection OBJECT_COLLECTION;
        public static GameObject OBJECT_TO_SPAWN;
        public static bool IS_USING_OBJECT_COLLECTION => OBJECT_COLLECTION != null;

        public static Transform PARENT;

        public static NamingSettings NAMING_SETTINGS;
        public static string NAME_OVERRIDE;
        public static string SUBSTITUTE_INPUT;
        public static string SUBSTITUTE_OUTPUT;
        public static string PREFIX;
        public static string POSTFIX;
        public static bool IS_USING_NAMING_SETTINGS => NAMING_SETTINGS != null;

        public static bool IS_TARGETING_SURFACES;
        public static bool POINT_TO_SURFACE_NORMAL = true;
        public static LayerMask SURFACES_MASK;

        #endregion


        #region GUI

        protected virtual void OnGUI()
        {
            ObjectsToSpawnGUI();

            GUILayout.Space(20);

            NamingSettingsGUI();

            GUILayout.Space(20);

            RotationSettingsGUI();

            GUILayout.Space(20);
        }

        private static void ObjectsToSpawnGUI()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Prefabs to spawn");
                OBJECT_COLLECTION = (PrefabCollection)EditorGUILayout.ObjectField(OBJECT_COLLECTION, typeof(PrefabCollection), false);
                if (!IS_USING_OBJECT_COLLECTION)
                {
                    OBJECT_TO_SPAWN = (GameObject)EditorGUILayout.ObjectField(OBJECT_TO_SPAWN, typeof(GameObject), false);
                }
                PARENT = (Transform)EditorGUILayout.ObjectField(PARENT, typeof(Transform), true);
            }
        }

        private static void NamingSettingsGUI()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Naming Settings");
                NAMING_SETTINGS = (NamingSettings)EditorGUILayout.ObjectField(NAMING_SETTINGS, typeof(NamingSettings), false);
                if (!IS_USING_NAMING_SETTINGS)
                {
                    NAME_OVERRIDE = EditorGUILayout.TextField(nameof(NAME_OVERRIDE), NAME_OVERRIDE);
                    if (string.IsNullOrEmpty(NAME_OVERRIDE))
                    {
                        SUBSTITUTE_INPUT = EditorGUILayout.TextField(nameof(SUBSTITUTE_INPUT), SUBSTITUTE_INPUT);
                        SUBSTITUTE_OUTPUT = EditorGUILayout.TextField(nameof(SUBSTITUTE_OUTPUT), SUBSTITUTE_OUTPUT);
                    }

                    PREFIX = EditorGUILayout.TextField(nameof(PREFIX), PREFIX);
                    POSTFIX = EditorGUILayout.TextField(nameof(POSTFIX), POSTFIX);
                }
            }
        }

        private static void RotationSettingsGUI()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Rotation Settings");

                IS_TARGETING_SURFACES =
                    EditorGUILayout.BeginToggleGroup(nameof(IS_TARGETING_SURFACES), IS_TARGETING_SURFACES);
                POINT_TO_SURFACE_NORMAL =
                    IS_TARGETING_SURFACES
                    && EditorGUILayout.Toggle(nameof(POINT_TO_SURFACE_NORMAL), POINT_TO_SURFACE_NORMAL);

                if (IS_TARGETING_SURFACES)
                    SURFACES_MASK = EditorGUILayout.MaskField(
                        InternalEditorUtility.LayerMaskToConcatenatedLayersMask(SURFACES_MASK),
                        InternalEditorUtility.layers);

                EditorGUILayout.EndToggleGroup();
            }
        }

        #endregion


        protected static bool TrySpawnProp(Vector2 position, Vector2? surfaceNormal = null)
        {
            Prop prop = GetProp();

            if (prop.HasDuplicateWithinMinDistance(position))
                return DisposeOfLastCreatedProp();
            if (surfaceNormal.HasValue && !prop.CanBePlacedOnNormal(surfaceNormal.Value))
                return DisposeOfLastCreatedProp();


            Rename(prop);
            prop.Reposition(position);
            prop.Rotate(surfaceNormal);

            return true;
        }

        protected static Prop GetProp()
        {
            LAST_CREATED_OBJ = IS_USING_OBJECT_COLLECTION
                ? (GameObject)PrefabUtility.InstantiatePrefab(OBJECT_COLLECTION.GetRandom(), PARENT)
                : (GameObject)PrefabUtility.InstantiatePrefab(OBJECT_TO_SPAWN, PARENT);

            Undo.RegisterCreatedObjectUndo(LAST_CREATED_OBJ, "Prefab Brush painted an object");
            return LAST_CREATED_OBJ.GetComponent<Prop>();
        }

        public static bool DisposeOfLastCreatedProp()
        {
            DestroyImmediate(LAST_CREATED_OBJ);
            return false;
        }



        protected static void Rename(IRenamable prop)
        {
            if (IS_USING_NAMING_SETTINGS)
                NAMING_SETTINGS.Data.ApplyToObject(prop);
            else
                new NamingSettings.NamingData()
                {
                    NameOverride = NAME_OVERRIDE,
                    SubstituteInput = SUBSTITUTE_INPUT,
                    SubstituteOutput = SUBSTITUTE_OUTPUT,
                    Prefix = PREFIX,
                    Postfix = POSTFIX
                }.ApplyToObject(prop);
        }
    }

}