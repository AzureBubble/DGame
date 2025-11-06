#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace GameLogic
{
    [CustomEditor(typeof(UIImage), true)]
    [CanEditMultipleObjects]
    public class UIImageEditor : ImageEditor
    {
        private bool m_isImageMaskPanelOpen = false;
        private bool m_isImageRoundedCornersPanelOpen = false;

        SerializedProperty m_sprite;

        // 不规则图形
        private SerializedProperty m_isUseMaskImage;
        private SerializedProperty m_fillPercent;
        private SerializedProperty m_fill;
        private SerializedProperty m_ringWidth;
        private SerializedProperty m_segements;
        private SerializedProperty m_verticesDistances;
        private SerializedProperty m_isUsePercentVert;
        private SerializedProperty m_rotation;

        // 圆角
        private SerializedProperty m_isUseRoundedCorners;
        private SerializedProperty m_radius;
        private SerializedProperty m_triangleNum;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_isImageMaskPanelOpen = EditorPrefs.GetBool("UIImage.m_isImageMaskPanelOpen", m_isImageMaskPanelOpen);
            m_isImageRoundedCornersPanelOpen = EditorPrefs.GetBool("UIImage.m_isImageRoundedCornersPanelOpen", m_isImageRoundedCornersPanelOpen);

            m_sprite = serializedObject.FindProperty("m_Sprite");

            // 不规则图形
            {
                m_segements = serializedObject.FindProperty("m_uiImageMaskExtend.m_segements");
                m_isUseMaskImage = serializedObject.FindProperty("m_uiImageMaskExtend.m_isUseMaskImage");
                m_fillPercent = serializedObject.FindProperty("m_uiImageMaskExtend.m_fillPercent");
                m_fill = serializedObject.FindProperty("m_uiImageMaskExtend.m_fill");
                m_ringWidth = serializedObject.FindProperty("m_uiImageMaskExtend.m_ringWidth");
                m_verticesDistances = serializedObject.FindProperty("m_uiImageMaskExtend.m_verticesDistances");
                m_isUsePercentVert = serializedObject.FindProperty("m_uiImageMaskExtend.m_isUsePercentVert");
                m_rotation = serializedObject.FindProperty("m_uiImageMaskExtend.m_rotation");
            }

            // 圆角
            {
                m_isUseRoundedCorners = serializedObject.FindProperty("m_uiImageRoundedCornersExtend.m_isUseRoundedCorners");
                m_radius = serializedObject.FindProperty("m_uiImageRoundedCornersExtend.m_radius");
                m_triangleNum = serializedObject.FindProperty("m_uiImageRoundedCornersExtend.m_triangleNum");
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UIImageGUI();

            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }

        private void UIImageGUI()
        {
            UIImageDrawEditor.DrawImageMaskGUI("不规则图形", ref m_isImageMaskPanelOpen, m_isUseMaskImage, m_fillPercent, m_fill, m_ringWidth, m_segements, m_isUsePercentVert, m_verticesDistances, m_rotation, m_isUseRoundedCorners);
            UIImageDrawEditor.DrawImageRoundedCornersGUI("圆角图形", ref m_isImageRoundedCornersPanelOpen, m_isUseRoundedCorners, m_radius, m_triangleNum, m_isUseMaskImage);

            if (GUI.changed)
            {
                EditorPrefs.SetBool("UIImage.m_isImageMaskPanelOpen", m_isImageMaskPanelOpen);
                EditorPrefs.SetBool("UIImage.m_isImageRoundedCornersPanelOpen", m_isImageRoundedCornersPanelOpen);
            }
        }
    }
}

#endif