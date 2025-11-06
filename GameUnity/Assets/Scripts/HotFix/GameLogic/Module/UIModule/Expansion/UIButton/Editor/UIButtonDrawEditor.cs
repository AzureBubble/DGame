#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public static class UIButtonDrawEditor
    {
        [MenuItem("GameObject/UI/UIButton", priority = 30)]
        public static void CreateUIButton()
        {
            // 创建 UIButton 物体
            UIButton uiBtn = new GameObject("UIButton", typeof(RectTransform), typeof(Image), typeof(UIButton)).GetComponent<UIButton>();
            UnityEditorUtil.ResetInCanvasFor(uiBtn.rectTransform);
            uiBtn.transition = Selectable.Transition.None;
            // Navigation btnNavigation = uiBtn.navigation;
            // btnNavigation.mode = Navigation.Mode.None;
            // uiBtn.navigation = btnNavigation;

            UIText uiText = new GameObject("Text", typeof(RectTransform), typeof(UIText)).GetComponent<UIText>();
            uiText.transform.SetParent(uiBtn.rectTransform);
            uiText.transform.localPosition = Vector3.zero;
            uiText.transform.localRotation = Quaternion.identity;
            uiText.transform.localScale = Vector3.one;
            uiText.color = Color.black;
            uiText.fontSize = 24;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.raycastTarget = false;
            uiText.supportRichText = false;
            uiText.text = "UI Button";
            RectTransform textRect = uiText.GetComponent<RectTransform>();
            textRect.anchorMax = Vector2.one;
            textRect.anchorMin = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
            uiBtn.rectTransform.sizeDelta = new Vector2(163, 50);
            uiBtn.rectTransform.localPosition = Vector3.zero;
        }

        #region 开启连点保护模式

        public static void DrawClickProtectGUI(string title, ref bool isPanelOpen, SerializedProperty isUseClickProtect,
            SerializedProperty protectTime, SerializedProperty isShowProtectText, SerializedProperty protectText)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUILayout.PropertyField(isUseClickProtect, new GUIContent("开启连点保护模式"));

                if (isUseClickProtect.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(protectTime, new GUIContent("连点保护时间"));
                    EditorGUI.indentLevel--;
                    EditorGUILayout.PropertyField(isShowProtectText, new GUIContent("显示连点保护倒计时"));

                    if (isShowProtectText.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.ObjectField(protectText, new GUIContent("倒计时依赖文本"));
                        EditorGUI.indentLevel--;
                    }
                }
            }, title, ref isPanelOpen, true);
        }

        #endregion

        #region 开启点击缩放模式

        public static void DrawClickScaleGUI(string title, ref bool isPanelOpen, SerializedProperty isUseClickScale,
            SerializedProperty normalScale, SerializedProperty clickScale, SerializedProperty clickScaleTime,
            SerializedProperty clickRecoverTime, SerializedProperty isUseDoTween, SerializedProperty reboundEffect,
            SerializedProperty childList)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUILayout.PropertyField(isUseClickScale, new GUIContent("开启点击缩放模式"));

                if (isUseClickScale.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(clickRecoverTime, new GUIContent("恢复时间"));
                    EditorGUILayout.PropertyField(normalScale, new GUIContent("默认缩放"));
                    EditorGUILayout.PropertyField(clickScaleTime, new GUIContent("缩放时间"));
                    EditorGUILayout.PropertyField(clickScale, new GUIContent("按下缩放"));
                    EditorGUILayout.PropertyField(isUseDoTween, new GUIContent("使用DoTween动画"));
                    EditorGUILayout.PropertyField(reboundEffect, new GUIContent("开启缩放回弹效果"));
                    EditorGUILayout.PropertyField(childList, new GUIContent("其他同时缩放节点"));
                    EditorGUI.indentLevel--;
                }
            }, title, ref isPanelOpen, true);
        }

        #endregion

        #region 开启长按模式

        public static void DrawLongPressGUI(string title, ref bool isPanelOpen, SerializedProperty isUseLongPress,
            SerializedProperty pressDuration, SerializedProperty isLoopLongPress, SerializedProperty interval,
            SerializedProperty onLongPressEvent)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUILayout.PropertyField(isUseLongPress, new GUIContent("开启长按模式"));

                if (isUseLongPress.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(pressDuration, new GUIContent("长按触发时间"));
                    EditorGUILayout.PropertyField(isLoopLongPress, new GUIContent("长按循环触发模式"));

                    if (isLoopLongPress.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(interval, new GUIContent("长按循环触发间隔"));
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.PropertyField(onLongPressEvent, new GUIContent("长按触发回调"));
                    EditorGUI.indentLevel--;
                }
            }, title, ref isPanelOpen, true);
        }

        #endregion

        #region 开启双击模式

        public static void DrawDoubleClickGUI(string title, ref bool isPanelOpen, SerializedProperty isUseDoubleClick,
            SerializedProperty clickInterval, SerializedProperty onDoubleClockEvent)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUILayout.PropertyField(isUseDoubleClick, new GUIContent("开启双击模式"));

                if (isUseDoubleClick.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(clickInterval, new GUIContent("双击有效时间"));
                    EditorGUILayout.PropertyField(onDoubleClockEvent, new GUIContent("双击触发回调"));
                    EditorGUI.indentLevel--;
                }
            }, title, ref isPanelOpen, true);
        }

        #endregion

        #region 开启点击音效

        public static void DrawClickSoundGUI(string title, ref bool isPanelOpen, SerializedProperty isUseClickSound,
            SerializedProperty isUseResourceSound, SerializedProperty clickSoundID, SerializedProperty clickSoundClip)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUILayout.PropertyField(isUseClickSound, new GUIContent("开启点击音效"));

                if (isUseClickSound.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(isUseResourceSound, new GUIContent("使用内嵌音频"));
                    if (isUseResourceSound.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(clickSoundClip, new GUIContent("点击音效文件"));
                        EditorGUI.indentLevel--;
                    }
                    else
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(clickSoundID, new GUIContent("点击音效ID"));
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                }
            }, title, ref isPanelOpen, true);
        }

        #endregion
    }
}

#endif