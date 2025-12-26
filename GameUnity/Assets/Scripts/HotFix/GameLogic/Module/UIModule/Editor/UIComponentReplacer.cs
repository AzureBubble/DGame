#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public static class UIComponentReplacer
    {
        [MenuItem("GameObject/UI/替换UI拓展组件成Unity原生组件", false, 0)]
        public static void ReplaceExtendComponentToUnityComponent()
        {
            GameObject root = Selection.activeGameObject;
            if (root == null) return;

            Undo.RegisterFullObjectHierarchyUndo(root, "Replace Extend Components To Unity Components");

            int imageCount = 0, textCount = 0, buttonCount = 0;

            // 获取所有Transform（包括自身和所有子对象）
            Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in allTransforms)
            {
                GameObject go = t.gameObject;

                // 替换 UIButton -> Button
                UIButton uiButton = go.GetComponent<UIButton>();
                if (uiButton != null)
                {
                    ReplaceComponent<UIButton, Button>(go);
                    buttonCount++;
                }

                // 替换 UIImage -> Image
                UIImage uiImage = go.GetComponent<UIImage>();
                if (uiImage != null)
                {
                    ReplaceComponent<UIImage, Image>(go);
                    imageCount++;
                }

                // 替换 UIText -> Text
                UIText uiText = go.GetComponent<UIText>();
                if (uiText != null)
                {
                    uiText.SetUITextLocalizationActive(false);
                    ReplaceComponent<UIText, Text>(go);
                    textCount++;
                }
            }

            Debug.Log($"[UIComponentReplacer] 替换完成: UIImage -> Image: {imageCount}, UIText -> Text: {textCount}, UIButton -> Button: {buttonCount}");
        }

        [MenuItem("GameObject/UI/替换UI拓展组件成Unity原生组件", true)]
        public static bool ValidateReplaceExtendComponentToUnityComponent()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem("GameObject/UI/替换Unity原生组件成UI拓展组件", false, 1)]
        public static void ReplaceUnityComponentToExtendComponent()
        {
            GameObject root = Selection.activeGameObject;
            if (root == null) return;

            Undo.RegisterFullObjectHierarchyUndo(root, "Replace Unity Components To Extend Components");

            int imageCount = 0, textCount = 0, buttonCount = 0;

            // 获取所有Transform（包括自身和所有子对象）
            Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in allTransforms)
            {
                GameObject go = t.gameObject;

                // 替换 Button -> UIButton（排除已经是UIButton的）
                Button button = go.GetComponent<Button>();
                if (button != null && !(button is UIButton))
                {
                    ReplaceComponent<Button, UIButton>(go);
                    buttonCount++;
                }

                // 替换 Image -> UIImage（排除已经是UIImage的）
                Image image = go.GetComponent<Image>();
                if (image != null && !(image is UIImage))
                {
                    ReplaceComponent<Image, UIImage>(go);
                    imageCount++;
                }

                // 替换 Text -> UIText（排除已经是UIText的）
                Text text = go.GetComponent<Text>();
                if (text != null && !(text is UIText))
                {
                    ReplaceComponent<Text, UIText>(go);
                    textCount++;
                }
            }

            Debug.Log($"[UIComponentReplacer] 替换完成: Image -> UIImage: {imageCount}, Text -> UIText: {textCount}, Button -> UIButton: {buttonCount}");
        }

        [MenuItem("GameObject/UI/替换Unity原生组件成UI拓展组件", true)]
        public static bool ValidateReplaceUnityComponentToExtendComponent()
        {
            return Selection.activeGameObject != null;
        }

        /// <summary>
        /// 替换组件，保留基类属性
        /// </summary>
        private static void ReplaceComponent<TSource, TTarget>(GameObject go)
            where TSource : Component
            where TTarget : Component
        {
            TSource source = go.GetComponent<TSource>();
            if (source == null) return;

            // 序列化源组件数据
            string json = EditorJsonUtility.ToJson(source);

            // 删除源组件
            Undo.DestroyObjectImmediate(source);

            // 添加目标组件
            TTarget target = Undo.AddComponent<TTarget>(go);

            // 反序列化到目标组件（只会复制共有的字段）
            EditorJsonUtility.FromJsonOverwrite(json, target);
        }
    }
}

#endif