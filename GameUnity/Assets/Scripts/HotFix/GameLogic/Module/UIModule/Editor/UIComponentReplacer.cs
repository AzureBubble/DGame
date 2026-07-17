#if UNITY_EDITOR

using DGame;
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

            const string undoName = "Replace Extend Components To Unity Components";
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            Undo.RegisterFullObjectHierarchyUndo(root, undoName);

            int imageCount = 0, textCount = 0, buttonCount = 0;

            try
            {
                // 获取所有Transform（包括自身和所有子对象）
                Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);

                foreach (Transform t in allTransforms)
                {
                    GameObject go = t.gameObject;

                    if (ReplaceComponent<UIButton, Button>(go))
                    {
                        buttonCount++;
                    }

                    if (ReplaceComponent<UIImage, Image>(go))
                    {
                        imageCount++;
                    }

                    if (ReplaceComponent<UIText, Text>(go))
                    {
                        textCount++;
                    }
                }

                Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
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

            const string undoName = "Replace Unity Components To Extend Components";
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            Undo.RegisterFullObjectHierarchyUndo(root, undoName);

            int imageCount = 0, textCount = 0, buttonCount = 0;

            try
            {
                // 获取所有Transform（包括自身和所有子对象）
                Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);

                foreach (Transform t in allTransforms)
                {
                    GameObject go = t.gameObject;

                    Button button = go.GetComponent<Button>();
                    if (button != null && !(button is UIButton) && ReplaceComponent<Button, UIButton>(go))
                    {
                        buttonCount++;
                    }

                    Image image = go.GetComponent<Image>();
                    if (image != null && !(image is UIImage) && ReplaceComponent<Image, UIImage>(go))
                    {
                        imageCount++;
                    }

                    Text text = go.GetComponent<Text>();
                    if (text != null && !(text is UIText) && ReplaceComponent<Text, UIText>(go))
                    {
                        textCount++;
                    }
                }

                Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }

            Debug.Log($"[UIComponentReplacer] 替换完成: Image -> UIImage: {imageCount}, Text -> UIText: {textCount}, Button -> UIButton: {buttonCount}");
        }

        [MenuItem("GameObject/UI/替换Unity原生组件成UI拓展组件", true)]
        public static bool ValidateReplaceUnityComponentToExtendComponent()
        {
            return Selection.activeGameObject != null;
        }

        #region 旧版

        // [MenuItem("GameObject/UI/转化成UIImage和UIText和UIButton(给美术做动画后转化回来)")]
        public static void ConvertToDodImage4Comp(MenuCommand menuCommand)
        {
            GameObject go = menuCommand.context as GameObject;
            if (go != null)
            {
                var texts = go.GetComponentsInChildren<Text>(true);
                if (texts != null)
                {
                    for (int i = 0; i < texts.Length; i++)
                    {
                        var text = texts[i];
                        var ob = ClassReplaceHelper.ReplaceClass(text, typeof(UIText));
                        ob.ApplyModifiedProperties();
                    }
                }
                var images = go.GetComponentsInChildren<Image>(true);
                if (images != null)
                {
                    for (int i = 0; i < images.Length; i++)
                    {
                        var image = images[i];
                        var ob = ClassReplaceHelper.ReplaceClass(image, typeof(UIImage));
                        ob.ApplyModifiedProperties();
                    }
                }
                var buttons = go.GetComponentsInChildren<Button>(true);
                if (buttons != null)
                {
                    for (int i = 0; i < buttons.Length; i++)
                    {
                        var button = buttons[i];
                        var ob = ClassReplaceHelper.ReplaceClass(button, typeof(UIButton));
                        ob.ApplyModifiedProperties();
                    }
                }
            }
        }

        // [MenuItem("GameObject/UI/转化成原生Image和Text和Button(给美术做动画)")]
        public static void ConvertToImageAndText(MenuCommand menuCommand)
        {
            GameObject go = menuCommand.context as GameObject;
            if (go != null)
            {
                var texts = go.GetComponentsInChildren<UIText>(true);
                if (texts != null)
                {
                    for (int i = 0; i < texts.Length; i++)
                    {
                        var text = texts[i];
                        var ob = ClassReplaceHelper.ReplaceClass(text, typeof(Text));
                        ob.ApplyModifiedProperties();
                    }
                }
                var images = go.GetComponentsInChildren<UIImage>(true);
                if (images != null)
                {
                    for (int i = 0; i < images.Length; i++)
                    {
                        var image = images[i];
                        var ob = ClassReplaceHelper.ReplaceClass(image, typeof(Image));
                        ob.ApplyModifiedProperties();
                    }
                }
                var buttons = go.GetComponentsInChildren<UIButton>(true);
                if (buttons != null)
                {
                    for (int i = 0; i < buttons.Length; i++)
                    {
                        var btn = buttons[i];
                        var ob = ClassReplaceHelper.ReplaceClass(btn, typeof(Button));
                        ob.ApplyModifiedProperties();
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// 原位替换组件脚本，保留组件fileID和现有序列化引用
        /// </summary>
        private static bool ReplaceComponent<TSource, TTarget>(GameObject go)
            where TSource : MonoBehaviour
            where TTarget : MonoBehaviour
        {
            TSource source = go.GetComponent<TSource>();
            if (source == null)
            {
                return false;
            }

            ClassReplaceHelper.ReplaceClass(source, typeof(TTarget));

            TTarget target = go.GetComponent<TTarget>();
            if (target == null)
            {
                throw new MissingComponentException($"Replace {typeof(TSource).Name} To {typeof(TTarget).Name} failed.");
            }

            EditorUtility.SetDirty(target);
            if (PrefabUtility.IsPartOfPrefabInstance(target))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
            return true;
        }
    }
}

#endif
