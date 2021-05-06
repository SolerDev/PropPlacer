using System;
using System.Text;
using PropPlacer.Runtime;
using UnityEngine;

namespace PropPlacer.Editor
{
    [CreateAssetMenu(fileName = "Naming", menuName = "PrefabBrush/Naming Settings")]
    public class NamingSettings : ScriptableObject
    {
        [SerializeField] private NamingData _data;
        public NamingData Data => _data;

        [Serializable]
        public struct NamingData
        {
            private static readonly StringBuilder NAME_BUILDER = new StringBuilder();

            public string Prefix;
            public string Postfix;
            public string NameOverride;

            public string SubstituteInput;
            public string SubstituteOutput;

            public void ApplyToObject(IRenamable renamable)
            {
                NAME_BUILDER.Append(renamable.Name);

                if (!string.IsNullOrEmpty(NameOverride))
                {
                    NAME_BUILDER.Clear();
                    NAME_BUILDER.Append(NameOverride);
                }
                else if (!string.IsNullOrEmpty(SubstituteInput))
                {
                    NAME_BUILDER.Replace(SubstituteInput, SubstituteOutput);
                }

                NAME_BUILDER.Insert(0, Prefix).Append(Postfix);

                renamable.Name = NAME_BUILDER.ToString();

                NAME_BUILDER.Clear();
            }
        }
    }
}
