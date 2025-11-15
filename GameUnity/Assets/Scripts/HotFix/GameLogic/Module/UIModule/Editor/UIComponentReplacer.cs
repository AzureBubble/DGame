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
            ReplaceSelectedExtendComponentToUnityComponent();
        }

        [MenuItem("GameObject/UI/替换UI拓展组件成Unity原生组件", true)]
        public static bool ValidateReplaceExtendComponentToUnityComponent()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem("GameObject/UI/替换Unity原生组件成UI拓展组件", false, 1)]
        public static void ReplaceUnityComponentToExtendComponent()
        {
            ReplaceSelectedExtendComponentToUnityComponent();
        }

        [MenuItem("GameObject/UI/替换Unity原生组件成UI拓展组件", true)]
        public static bool ValidateReplaceUnityComponentToExtendComponent()
        {
            return Selection.activeGameObject != null;
        }

        /// <summary>
        /// 将选中的GameObject及其所有子物体中的UIImage组件替换为Unity的Image组件
        /// </summary>
        private static void ReplaceSelectedExtendComponentToUnityComponent()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogWarning("请先选择一个GameObject");
                return;
            }

            GameObject selectedObject = Selection.activeGameObject;
            List<Component> replaceComponents = new List<Component>();

            // 查找所有UIImage组件（包括选中物体和所有子物体）
            Component[] allComponents = selectedObject.GetComponentsInChildren<Component>(true);

            foreach (Component component in allComponents)
            {
                // 使用类型名称匹配，因为UIImage可能是自定义组件
                if (component != null && component.GetType().Name == "UIImage")
                {
                    replaceComponents.Add(component);
                }
            }

            if (replaceComponents.Count == 0)
            {
                return;
            }

            int replacementCount = 0;

            foreach (Component replaceComponent in replaceComponents)
            {
                GameObject targetGameObject = replaceComponent.gameObject;

                if (targetGameObject.TryGetComponent<UIImage>(out var uiImage))
                {
                    var sprite = uiImage.sprite;
                    var color = uiImage.color;
                    var material = uiImage.material;
                    var raycastTarget = uiImage.raycastTarget;
                    var maskable = uiImage.maskable;
                    var raycastPadding = uiImage.raycastPadding;
                    var imageType = uiImage.type;
                    var imgUseSpriteMesh = uiImage.useSpriteMesh;
                    var imgPreserveAspect = uiImage.preserveAspect;
                    var fillCenter = uiImage.fillCenter;
                    var pixelsPerUnitMultiplier = uiImage.pixelsPerUnitMultiplier;
                    var fillMethod = uiImage.fillMethod;
                    var fillOrigin = uiImage.fillOrigin;
                    var fillClockwise = uiImage.fillClockwise;
                    var fillAmount = uiImage.fillAmount;
                    GameObject.DestroyImmediate(uiImage);
                    var img = targetGameObject.AddComponent<Image>();
                    img.sprite = sprite;
                    img.color = color;
                    img.material = material;
                    img.raycastTarget = raycastTarget;
                    img.maskable = maskable;
                    img.raycastPadding = raycastPadding;
                    img.type = imageType;
                    img.useSpriteMesh = imgUseSpriteMesh;
                    img.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
                    img.fillMethod = fillMethod;
                    img.fillOrigin = fillOrigin;
                    img.fillClockwise = fillClockwise;
                    img.fillAmount = fillAmount;
                    img.preserveAspect = imgPreserveAspect;
                    img.fillCenter = fillCenter;
                    replacementCount++;
                }
            }

            Debug.Log($"成功将 {replacementCount} 个UIImage组件替换为Image组件");
        }
    }
}

#endif