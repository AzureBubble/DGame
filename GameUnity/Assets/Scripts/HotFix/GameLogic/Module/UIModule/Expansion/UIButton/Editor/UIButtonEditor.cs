#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace GameLogic
{
    [CustomEditor(typeof(UIButton), true)]
    [CanEditMultipleObjects]
    public class UIButtonEditor : ButtonEditor
    {
        // 折叠布局控制字段
        private static bool m_clickProtectPanelOpen = false;
        private static bool m_clickScalePanelOpen = false;
        private static bool m_longPressPanelOpen = false;
        private static bool m_doubleClickPanelOpen = false;
        private static bool m_clickSoundPanelOpen = false;

        // 连点保护模式
        private SerializedProperty m_isUseClickProtect;
        private SerializedProperty m_protectTime;
        private SerializedProperty m_isShowProtectText;
        private SerializedProperty m_protectText;

        // 点击缩放
        private SerializedProperty m_isUseClickScale;
        private SerializedProperty m_clickScaleTime;
        private SerializedProperty m_clickRecoverTime;
        private SerializedProperty m_normalScale;
        private SerializedProperty m_clickScale;
        private SerializedProperty m_isUseDoTween;
        private SerializedProperty m_reboundEffect;
        private SerializedProperty m_childList;

        // 长按
        private SerializedProperty m_isUseLongPress;
        private SerializedProperty m_pressDuration;
        private SerializedProperty m_isLoopLongPress;
        private SerializedProperty m_interval;
        private SerializedProperty m_onLongPressEvent;

        // 双击
        private SerializedProperty m_isUseDoubleClick;
        private SerializedProperty m_clickInterval;
        private SerializedProperty m_onDoubleClickEvent;

        // 点击音效
        private SerializedProperty m_isUseClickSound;
        private SerializedProperty m_clickSoundID;

        protected override void OnEnable()
        {
            base.OnEnable();
            // 折叠布局控制字段
            m_clickProtectPanelOpen = EditorPrefs.GetBool("UIButton.m_clickProtectPanelOpen", m_clickProtectPanelOpen);
            m_clickScalePanelOpen = EditorPrefs.GetBool("UIButton.m_clickScalePanelOpen", m_clickScalePanelOpen);
            m_longPressPanelOpen = EditorPrefs.GetBool("UIButton.m_longPressPanelOpen", m_longPressPanelOpen);
            m_doubleClickPanelOpen = EditorPrefs.GetBool("UIButton.m_doubleClickPanelOpen", m_doubleClickPanelOpen);
            m_clickSoundPanelOpen = EditorPrefs.GetBool("UIButton.m_clickSoundPanelOpen", m_clickSoundPanelOpen);

            // 连点保护模式
            {
                m_isUseClickProtect = serializedObject.FindProperty("m_uiButtonClickProtect.m_isUseClickProtect");
                m_protectTime = serializedObject.FindProperty("m_uiButtonClickProtect.m_protectTime");
                m_isShowProtectText = serializedObject.FindProperty("m_uiButtonClickProtect.m_isShowProtectText");
                m_protectText = serializedObject.FindProperty("m_uiButtonClickProtect.m_protectText");
            }

            // 点击缩放
            {
                m_isUseClickScale = serializedObject.FindProperty("m_uiButtonClickScale.m_isUseClickScale");
                m_normalScale = serializedObject.FindProperty("m_uiButtonClickScale.m_normalScale");
                m_clickScale = serializedObject.FindProperty("m_uiButtonClickScale.m_clickScale");
                m_clickScaleTime = serializedObject.FindProperty("m_uiButtonClickScale.m_clickScaleTime");
                m_clickRecoverTime = serializedObject.FindProperty("m_uiButtonClickScale.m_clickRecoverTime");
                m_isUseDoTween = serializedObject.FindProperty("m_uiButtonClickScale.m_isUseDoTween");
                m_reboundEffect = serializedObject.FindProperty("m_uiButtonClickScale.m_reboundEffect");
                m_childList = serializedObject.FindProperty("m_uiButtonClickScale.m_childList");
            }

            // 长按
            {
                m_isUseLongPress = serializedObject.FindProperty("m_uiButtonLongPress.m_isUseLongPress");
                m_pressDuration = serializedObject.FindProperty("m_uiButtonLongPress.m_pressDuration");
                m_isLoopLongPress = serializedObject.FindProperty("m_uiButtonLongPress.m_isLoopLongPress");
                m_interval = serializedObject.FindProperty("m_uiButtonLongPress.m_interval");
                m_onLongPressEvent = serializedObject.FindProperty("m_uiButtonLongPress.m_onLongPressEvent");
            }

            // 双击
            {
                m_isUseDoubleClick = serializedObject.FindProperty("m_uiButtonDoubleClick.m_isUseDoubleClick");
                m_clickInterval = serializedObject.FindProperty("m_uiButtonDoubleClick.m_clickInterval");
                m_onDoubleClickEvent = serializedObject.FindProperty("m_uiButtonDoubleClick.m_onDoubleClickEvent");
            }

            // 点击音效
            {
                m_isUseClickSound = serializedObject.FindProperty("m_uiButtonClickSound.m_isUseClickSound");
                m_clickSoundID = serializedObject.FindProperty("m_uiButtonClickSound.m_clickSoundID");
            }
        }

        public override void OnInspectorGUI()
        {
            // 更新编辑器修改
            serializedObject.Update();
            // 绘制 UIButton
            UIButtonGUI();
            // 应用接编辑器修改
            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }

        private void UIButtonGUI()
        {
            UIButtonDrawEditor.DrawClickScaleGUI("点击缩放模式", ref m_clickScalePanelOpen, m_isUseClickScale, m_normalScale,
                m_clickScale, m_clickScaleTime, m_clickRecoverTime, m_isUseDoTween, m_reboundEffect, m_childList);
            UIButtonDrawEditor.DrawClickProtectGUI("连点保护模式", ref m_clickProtectPanelOpen, m_isUseClickProtect,
                m_protectTime, m_isShowProtectText, m_protectText);
            UIButtonDrawEditor.DrawLongPressGUI("点击长按模式", ref m_longPressPanelOpen, m_isUseLongPress,
                m_pressDuration, m_isLoopLongPress, m_interval, m_onLongPressEvent);
            UIButtonDrawEditor.DrawDoubleClickGUI("按钮双击模式", ref m_doubleClickPanelOpen, m_isUseDoubleClick,
                m_clickInterval, m_onDoubleClickEvent);
            UIButtonDrawEditor.DrawClickSoundGUI("点击音效模式", ref m_clickSoundPanelOpen, m_isUseClickSound,
                m_clickSoundID);

            if (GUI.changed)
            {
                EditorPrefs.SetBool("UIButton.m_clickProtectPanelOpen", m_clickProtectPanelOpen);
                EditorPrefs.SetBool("UIButton.m_clickScalePanelOpen", m_clickScalePanelOpen);
                EditorPrefs.SetBool("UIButton.m_longPressPanelOpen", m_longPressPanelOpen);
                EditorPrefs.SetBool("UIButton.m_doubleClickPanelOpen", m_doubleClickPanelOpen);
                EditorPrefs.SetBool("UIButton.m_clickSoundPanelOpen", m_clickSoundPanelOpen);
            }
        }
    }
}

#endif