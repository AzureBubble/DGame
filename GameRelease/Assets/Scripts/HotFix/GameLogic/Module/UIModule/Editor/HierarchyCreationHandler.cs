#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class HierarchyCreationHandler : Editor
    {
        private static CanvasScaler.ScaleMode UIWINDOW_DEFAULT_SCALE_MODE = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        private static int UIWINDOW_WIDTH = 1920;
        private static int UIWINDOW_HEIGHT = 1080;

        [InitializeOnLoadMethod]
        private static void OnHierarchyCreateUIComponent()
        {
            // 监听Hierarchy窗口中的对象创建事件
            EditorApplication.hierarchyChanged -= OnComponentCreated;
            EditorApplication.hierarchyChanged -= LoadUIWindowCamera;
            EditorApplication.hierarchyChanged += OnComponentCreated;
            EditorApplication.hierarchyChanged += LoadUIWindowCamera;
        }

        private static void OnComponentCreated()
        {
            GameObject obj = Selection.activeGameObject;

            if (obj == null)
            {
                return;
            }

            #region 替换组件

            if (obj.name.Contains("Button"))
            {
                if (obj.TryGetComponent<Button>(out var button))
                {
                    obj.name = "m_btn";
                    DestroyImmediate(button);
                    var uiButton = obj.AddComponent<UIButton>();
                    uiButton.transition = Selectable.Transition.None;

                    if (obj.TryGetComponent<Image>(out var image))
                    {
                        if (!(image is UIImage))
                        {
                            DestroyImmediate(image);
                            obj.AddComponent<UIImage>();
                        }
                    }

                    Text[] texts = obj.GetComponentsInChildren<Text>(true);
                    if (texts != null && texts.Length > 0)
                    {
                        foreach (var btnText in texts)
                        {
                            if (!(btnText is UIText))
                            {
                                var textContent = btnText.text;
                                var font = btnText.font;
                                var fontStyle = btnText.fontStyle;
                                var fontSize = btnText.fontSize;
                                var textColor = btnText.color;
                                var textRich = btnText.supportRichText;
                                var textHorizontalOverflow = btnText.horizontalOverflow;
                                var textVerticalOverflow = btnText.verticalOverflow;
                                var textObj = btnText.gameObject;

                                DestroyImmediate(btnText);
                                var uiText = textObj.AddComponent<UIText>();
                                uiText.raycastTarget = false;
                                uiText.text = textContent;
                                uiText.font = font;
                                uiText.color = textColor;
                                uiText.fontSize = fontSize;
                                uiText.fontStyle = fontStyle;
                                uiText.supportRichText = textRich;
                                uiText.horizontalOverflow = textHorizontalOverflow;
                                uiText.verticalOverflow = textVerticalOverflow;
                                uiText.raycastTarget = false;
                            }
                            else
                            {
                                btnText.raycastTarget = false;
                            }
                        }
                    }

                    TextMeshProUGUI[] textPros = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
                    if (textPros != null && textPros.Length > 0)
                    {
                        foreach (var btnText in textPros)
                        {
                            btnText.raycastTarget = false;
                        }
                    }
                }

                return;
            }
            else if (obj.name.Contains("Image"))
            {
                if (obj.TryGetComponent<Image>(out var image))
                {
                    obj.name = "m_img";
                    if (!(image is UIImage))
                    {
                        var sprite = image.sprite;
                        var color = image.color;
                        var material = image.material;
                        var raycastTarget = image.raycastTarget;
                        var maskable = image.maskable;
                        var raycastPadding = image.raycastPadding;
                        var imageType = image.type;
                        var imgUseSpriteMesh = image.useSpriteMesh;
                        var imgPreserveAspect = image.preserveAspect;
                        var fillCenter = image.fillCenter;
                        var pixelsPerUnitMultiplier = image.pixelsPerUnitMultiplier;
                        var fillMethod = image.fillMethod;
                        var fillOrigin = image.fillOrigin;
                        var fillClockwise = image.fillClockwise;
                        var fillAmount = image.fillAmount;
                        GameObject.DestroyImmediate(image);
                        var img = obj.AddComponent<UIImage>();
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
                    }
                }

                return;
            }
            else if (obj.name.Contains("Text"))
            {
                if (obj.TryGetComponent<Text>(out var text))
                {
                    // obj.name = "m_text";
                    if (!(text is UIText))
                    {
                        var textContent = text.text;
                        var font = text.font;
                        var fontStyle = text.fontStyle;
                        var fontSize = text.fontSize;
                        var textColor = text.color;
                        var textRich = text.supportRichText;
                        var textHorizontalOverflow = text.horizontalOverflow;
                        var textVerticalOverflow = text.verticalOverflow;
                        var textObj = text.gameObject;

                        DestroyImmediate(text);
                        var uiText = textObj.AddComponent<UIText>();
                        uiText.raycastTarget = false;
                        uiText.text = textContent;
                        uiText.font = font;
                        uiText.color = textColor;
                        uiText.fontSize = fontSize;
                        uiText.fontStyle = fontStyle;
                        uiText.supportRichText = textRich;
                        uiText.horizontalOverflow = textHorizontalOverflow;
                        uiText.verticalOverflow = textVerticalOverflow;
                        uiText.raycastTarget = false;
                    }
                    else
                    {
                        text.raycastTarget = false;
                    }
                }

                return;
            }
            else if (obj.name.Contains("Scroll View"))
            {
                if (obj.TryGetComponent<ScrollRect>(out var scrollRect))
                {
                    obj.name = "m_scroll";
                }
                GameObject viewPort = obj.transform.Find("Viewport").gameObject;
                if (viewPort.TryGetComponent<Mask>(out Mask mask))
                {
                    DestroyImmediate(mask);
                    viewPort.AddComponent<RectMask2D>();
                }
                if (viewPort.TryGetComponent<Image>(out Image viewPortImage))
                {
                    DestroyImmediate(viewPortImage);
                }

                Image[] images = obj.GetComponentsInChildren<Image>(true);
                if (images != null && images.Length > 0)
                {
                    foreach (var img in images)
                    {
                        if (!(img is UIImage))
                        {
                            var sprite = img.sprite;
                            var imgType = img.type;
                            var imgColor = img.color;
                            var imageObj = img.gameObject;
                            DestroyImmediate(img);
                            var uiImage = imageObj.AddComponent<UIImage>();
                            uiImage.sprite = sprite;
                            uiImage.type = imgType;
                            uiImage.color = imgColor;
                        }
                    }
                }

                Scrollbar[] scrollbars = obj.GetComponentsInChildren<Scrollbar>(true);
                if (scrollbars != null && scrollbars.Length > 0)
                {
                    foreach (var tmpScrollbar in scrollbars)
                    {
                        tmpScrollbar.transition = Selectable.Transition.None;
                    }
                }

                return;
            }
            else if (obj.name.Contains("Slider"))
            {
                if (obj.TryGetComponent<Slider>(out var slider))
                {
                    obj.name = "m_slider";
                    slider.transition = Selectable.Transition.None;
                }
                Image[] images = obj.GetComponentsInChildren<Image>(true);
                if (images != null && images.Length > 0)
                {
                    foreach (var img in images)
                    {
                        if (!(img is UIImage))
                        {
                            var sprite = img.sprite;
                            var imgType = img.type;
                            var imageObj = img.gameObject;
                            var imgColor = img.color;
                            DestroyImmediate(img);
                            var uiImage = imageObj.AddComponent<UIImage>();
                            uiImage.sprite = sprite;
                            uiImage.type = imgType;
                            uiImage.color = imgColor;
                        }
                    }
                }

                return;
            }
            else if (obj.name.Contains("Scrollbar"))
            {
                if (obj.TryGetComponent<Scrollbar>(out var scrollbar))
                {
                    obj.name = "m_scrollbar";
                    scrollbar.transition = Selectable.Transition.None;
                }
                Image[] images = obj.GetComponentsInChildren<Image>(true);
                if (images != null && images.Length > 0)
                {
                    foreach (var img in images)
                    {
                        if (!(img is UIImage))
                        {
                            var sprite = img.sprite;
                            var imgType = img.type;
                            var imgColor = img.color;
                            var imageObj = img.gameObject;
                            DestroyImmediate(img);
                            var uiImage = imageObj.AddComponent<UIImage>();
                            uiImage.sprite = sprite;
                            uiImage.type = imgType;
                            uiImage.color = imgColor;
                        }
                    }
                }

                return;
            }
            else if (obj.name.Contains("InputField"))
            {
                if (obj.TryGetComponent<InputField>(out var inputField))
                {
                    obj.name = "m_input";
                    inputField.transition = Selectable.Transition.None;
                }
                Image[] images = obj.GetComponentsInChildren<Image>(true);
                if (images != null && images.Length > 0)
                {
                    foreach (var img in images)
                    {
                        if (!(img is UIImage))
                        {
                            var sprite = img.sprite;
                            var imgType = img.type;
                            var imgColor = img.color;
                            var imageObj = img.gameObject;
                            DestroyImmediate(img);
                            var uiImage = imageObj.AddComponent<UIImage>();
                            uiImage.sprite = sprite;
                            uiImage.type = imgType;
                            uiImage.color = imgColor;
                        }
                    }
                }

                Text[] texts = obj.GetComponentsInChildren<Text>(true);
                if (texts != null && texts.Length > 0)
                {
                    foreach (var text in texts)
                    {
                        if (!(text is UIText))
                        {
                            var textContent = text.text;
                            var font = text.font;
                            var fontStyle = text.fontStyle;
                            var fontSize = text.fontSize;
                            var textColor = text.color;
                            var textRich = text.supportRichText;
                            var textHorizontalOverflow = text.horizontalOverflow;
                            var textVerticalOverflow = text.verticalOverflow;
                            var textObj = text.gameObject;

                            DestroyImmediate(text);
                            var uiText = textObj.AddComponent<UIText>();
                            uiText.raycastTarget = false;
                            uiText.text = textContent;
                            uiText.font = font;
                            uiText.color = textColor;
                            uiText.fontSize = fontSize;
                            uiText.fontStyle = fontStyle;
                            uiText.supportRichText = textRich;
                            uiText.horizontalOverflow = textHorizontalOverflow;
                            uiText.verticalOverflow = textVerticalOverflow;
                            uiText.raycastTarget = false;
                        }
                        else
                        {
                            text.raycastTarget = false;
                        }
                    }
                }

                var tmpTxt = obj.GetComponentsInChildren<UIText>();
                if (tmpTxt != null && tmpTxt.Length > 0)
                {
                    inputField.placeholder = tmpTxt[0];
                    inputField.textComponent = tmpTxt[^1];
                }

                return;
            }
            else if (obj.name.Contains("Dropdown"))
            {
                if (obj.TryGetComponent<Dropdown>(out var dropdown))
                {
                    obj.name = "m_dropDown";
                    dropdown.transition = Selectable.Transition.None;
                }

                var maskArr = obj.GetComponentsInChildren<Mask>(true);

                if (maskArr != null && maskArr.Length > 0)
                {
                    for (int i = 0; i < maskArr.Length; i++)
                    {
                        var tmpMask = maskArr[i];
                        var viewPort = tmpMask.gameObject;
                        // GameObject viewPort = obj.transform.Find("Viewport").gameObject;
                        if (viewPort.TryGetComponent<Mask>(out Mask mask))
                        {
                            DestroyImmediate(mask);
                            viewPort.AddComponent<RectMask2D>();
                        }
                        if (viewPort.TryGetComponent<Image>(out Image viewPortImage))
                        {
                            DestroyImmediate(viewPortImage);
                        }
                    }
                }

                Image[] images = obj.GetComponentsInChildren<Image>(true);
                if (images != null && images.Length > 0)
                {
                    foreach (var img in images)
                    {
                        if (!(img is UIImage))
                        {
                            var sprite = img.sprite;
                            var imgType = img.type;
                            var imgColor = img.color;
                            var imageObj = img.gameObject;
                            DestroyImmediate(img);
                            var uiImage = imageObj.AddComponent<UIImage>();
                            uiImage.sprite = sprite;
                            uiImage.type = imgType;
                            uiImage.color = imgColor;
                        }
                    }
                }

                Text[] texts = obj.GetComponentsInChildren<Text>(true);
                if (texts != null && texts.Length > 0)
                {
                    foreach (var text in texts)
                    {
                        if (!(text is UIText))
                        {
                            var textContent = text.text;
                            var font = text.font;
                            var fontStyle = text.fontStyle;
                            var fontSize = text.fontSize;
                            var textColor = text.color;
                            var textRich = text.supportRichText;
                            var textHorizontalOverflow = text.horizontalOverflow;
                            var textVerticalOverflow = text.verticalOverflow;
                            var textObj = text.gameObject;

                            DestroyImmediate(text);
                            var uiText = textObj.AddComponent<UIText>();
                            uiText.raycastTarget = false;
                            uiText.text = textContent;
                            uiText.font = font;
                            uiText.color = textColor;
                            uiText.fontSize = fontSize;
                            uiText.fontStyle = fontStyle;
                            uiText.supportRichText = textRich;
                            uiText.horizontalOverflow = textHorizontalOverflow;
                            uiText.verticalOverflow = textVerticalOverflow;
                            uiText.raycastTarget = false;
                        }
                        else
                        {
                            text.raycastTarget = false;
                        }
                    }
                }

                var scrollbars = obj.GetComponentsInChildren<Scrollbar>(true);
                if (scrollbars != null && scrollbars.Length > 0)
                {
                    for (int i = 0; i < scrollbars.Length; i++)
                    {
                        var scrollbar = scrollbars[i];
                        scrollbar.transition = Selectable.Transition.None;
                    }
                }

                var toggles = obj.GetComponentsInChildren<Toggle>(true);
                if (toggles != null && toggles.Length > 0)
                {
                    for (int i = 0; i < toggles.Length; i++)
                    {
                        var toggle = toggles[i];
                        toggle.transition = Selectable.Transition.None;
                        toggle.graphic = toggle.transform.Find("Item Checkmark").GetComponent<Image>();
                    }
                }

                Text[] tmpTexts = obj.GetComponentsInChildren<Text>();
                dropdown.captionText = tmpTexts[0];

                return;
            }
            else if (obj.name.Contains("Toggle"))
            {
                if (obj.TryGetComponent<Toggle>(out var toggle))
                {
                    obj.name = "m_toggle";
                    toggle.transition = Selectable.Transition.None;
                }
                Image[] images = obj.GetComponentsInChildren<Image>(true);
                if (images != null && images.Length > 0)
                {
                    foreach (var img in images)
                    {
                        if (!(img is UIImage))
                        {
                            var sprite = img.sprite;
                            var imgType = img.type;
                            var imgColor = img.color;
                            var imageObj = img.gameObject;
                            DestroyImmediate(img);
                            var uiImage = imageObj.AddComponent<UIImage>();
                            uiImage.sprite = sprite;
                            uiImage.type = imgType;
                            uiImage.color = imgColor;
                        }
                    }
                }

                Text[] texts = obj.GetComponentsInChildren<Text>(true);
                if (texts != null && texts.Length > 0)
                {
                    foreach (var text in texts)
                    {
                        if (!(text is UIText))
                        {
                            var textContent = text.text;
                            var font = text.font;
                            var fontStyle = text.fontStyle;
                            var fontSize = text.fontSize;
                            var textColor = text.color;
                            var textRich = text.supportRichText;
                            var textHorizontalOverflow = text.horizontalOverflow;
                            var textVerticalOverflow = text.verticalOverflow;
                            var textObj = text.gameObject;

                            DestroyImmediate(text);
                            var uiText = textObj.AddComponent<UIText>();
                            uiText.raycastTarget = false;
                            uiText.text = textContent;
                            uiText.font = font;
                            uiText.color = textColor;
                            uiText.fontSize = fontSize;
                            uiText.fontStyle = fontStyle;
                            uiText.supportRichText = textRich;
                            uiText.horizontalOverflow = textHorizontalOverflow;
                            uiText.verticalOverflow = textVerticalOverflow;
                            uiText.raycastTarget = false;
                        }
                        else
                        {
                            text.raycastTarget = false;
                        }
                    }
                }

                Image[] tmpImages = obj.GetComponentsInChildren<Image>(true);
                toggle.graphic = tmpImages[^1];

                return;
            }

            #endregion

            #region 替换手动添加的UI组件


            // if (obj.TryGetComponent(out Button btn))
            // {
            //     if (!(btn is UIButton))
            //     {
            //         DestroyImmediate(btn);
            //         var uiBtn = obj.AddComponent<UIButton>();
            //         uiBtn.transition = Selectable.Transition.None;
            //     }
            //     else
            //     {
            //         btn.transition = Selectable.Transition.None;
            //     }
            // }
            // if (obj.TryGetComponent(out Text tmpT))
            // {
            //     if (!(tmpT is UIText))
            //     {
            //         DestroyImmediate(tmpT);
            //         var uiText = obj.AddComponent<UIText>();
            //         uiText.raycastTarget = false;
            //     }
            //     else
            //     {
            //         tmpT.raycastTarget = false;
            //     }
            // }
            // if (obj.TryGetComponent(out Image tmpImg))
            // {
            //     if (!(tmpImg is UIImage))
            //     {
            //         DestroyImmediate(tmpImg);
            //         obj.AddComponent<UIImage>();
            //     }
            // }

            #endregion
        }

        private static void LoadUIWindowCamera()
        {
            var window = Selection.activeGameObject;
            if (window != null)
            {
                bool isUIWindow = window.name.ToUpper().Contains("UI")
                    || window.name.Contains("Window");
                if (isUIWindow && window.TryGetComponent<Canvas>(out Canvas canvas))
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    GameObject uiCameraObj = GameObject.Find("UIRoot/UICamera");
                    if (uiCameraObj != null)
                    {
                        canvas.worldCamera = uiCameraObj.GetComponent<Camera>();
                    }

                    if (window.TryGetComponent<CanvasScaler>(out CanvasScaler canvasScaler))
                    {
                        canvasScaler.uiScaleMode = UIWINDOW_DEFAULT_SCALE_MODE;
                        canvasScaler.referenceResolution = new Vector2(UIWINDOW_WIDTH, UIWINDOW_HEIGHT);
                    }
                }
            }
        }
    }
}

#endif