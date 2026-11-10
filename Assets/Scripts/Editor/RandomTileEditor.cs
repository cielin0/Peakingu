#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace UnityEditor.Tilemaps
{
    [CustomEditor(typeof(RandomTile))]
    [CanEditMultipleObjects]
    public class RandomTileEditor : Editor
    {
        private SerializedProperty m_Color;
        private SerializedProperty m_ColliderType;
        private SerializedProperty m_Sprites;

        private void OnEnable()
        {
            m_Color = serializedObject.FindProperty("m_Color");
            m_ColliderType = serializedObject.FindProperty("m_ColliderType");
            m_Sprites = serializedObject.FindProperty("m_Sprites");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (m_Color != null)
                EditorGUILayout.PropertyField(m_Color);
            if (m_ColliderType != null)
                EditorGUILayout.PropertyField(m_ColliderType);
            if (m_Sprites != null)
                EditorGUILayout.PropertyField(m_Sprites, true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
