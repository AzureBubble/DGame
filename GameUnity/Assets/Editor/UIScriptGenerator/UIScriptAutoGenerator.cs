using System;
using System.IO;
using System.Text;
using GameLogic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DGame
{
    public partial class UIScriptGenerator
    {
        private static void CheckVariableNames()
        {
            var cnt = (int)UIFieldCodeStyle.Max;
            VARIABLE_NAME_REGEX = new string[cnt];

            for (int i = 0; i < cnt; i++)
            {
                VARIABLE_NAME_REGEX[i] = GetPrefixNameByCodeStyle((UIFieldCodeStyle)i);
            }
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyBindComponent", priority = 84)]
        public static void UIPropertyBindComponent()
        {
            GenerateCSharpScript(false);
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyBindComponent", true)]
        public static bool ValidateUIPropertyBindComponent()
        {
            return UIScriptGeneratorSettings.Instance.UseBindComponent;
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyBindComponent - UniTask", priority = 85)]
        public static void UIPropertyBindComponentUniTask()
        {
            GenerateCSharpScript(false, true);
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyBindComponent - UniTask", true)]
        public static bool ValidateUIPropertyBindComponentUniTask()
        {
            return UIScriptGeneratorSettings.Instance.UseBindComponent;
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyAndListenerBindComponent", priority = 86)]
        public static void UIPropertyAndListenerBindComponent()
        {
            GenerateCSharpScript(true);
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyAndListenerBindComponent", true)]
        public static bool ValidateUIPropertyAndListenerBindComponent()
        {
            return UIScriptGeneratorSettings.Instance.UseBindComponent;
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyAndListenerBindComponentUniTask - UniTask", priority = 87)]
        public static void UIPropertyAndListenerBindComponentUniTask()
        {
            GenerateCSharpScript(true, true);
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyAndListenerBindComponentUniTask - UniTask", true)]
        public static bool ValidateUIPropertyAndListenerBindComponentUniTask()
        {
            return UIScriptGeneratorSettings.Instance.UseBindComponent;
        }

        public static bool GenerateCSharpScript(bool includeListener, bool isUniTask = false,
            bool isAutoGenerate = false, string savePath = null, bool isAutoDiff = true, bool m_isWidget = false)
        {
            var root = Selection.activeTransform;

            if (root == null)
            {
                return false;
            }

            CheckVariableNames();
            StringBuilder strVar = new StringBuilder();
            StringBuilder strBind = new StringBuilder();
            StringBuilder strOnCreate = new StringBuilder();
            StringBuilder strCallback = new StringBuilder();

            var windowComSufName = UIScriptGeneratorSettings.Instance.WindowComponentSuffixName;
            var widgetComSufName = UIScriptGeneratorSettings.Instance.WidgetComponentSuffixName;

            var widgetPrefix = GetUIWidgetName();
            var rootName = $"{root.name}{windowComSufName}";
            string fileName = $"{root.name}.cs";
            string uiType = "UIWindow";

            if (root.name.StartsWith(widgetPrefix))
            {
                uiType = "UIWidget";
                fileName = $"{root.name.Replace(GetUIWidgetName(), string.Empty)}.cs";
                rootName = $"{root.name.Replace(GetUIWidgetName(), string.Empty)}{widgetComSufName}";
            }

            if (!isAutoDiff)
            {
                if (m_isWidget)
                {
                    uiType = "UIWidget";
                    rootName = $"{root.name}{widgetComSufName}";
                }
                else
                {
                    uiType = "UIWindow";
                    rootName = $"{root.name}{windowComSufName}";
                }

                fileName = $"{root.name}.cs";
            }

            strVar.AppendLine($"\t\tprivate UIBindComponent m_bindComponent;");

            strBind.AppendLine($"\t\t\tm_bindComponent = gameObject.GetComponent<UIBindComponent>();");
            m_bindIndex = 0;
            AutoErgodic(root, root, ref strVar, ref strBind, ref strOnCreate, ref strCallback, isUniTask);
            StringBuilder strFile = new StringBuilder();

            if (includeListener)
            {
#if ENABLE_TEXTMESHPRO
                    strFile.AppendLine("using TMPro;");
#endif
                if (isUniTask)
                {
                    strFile.AppendLine("using Cysharp.Threading.Tasks;");
                }

                strFile.AppendLine("using UnityEngine;");
                strFile.AppendLine("using UnityEngine.UI;");
                strFile.AppendLine("using DGame;");
                strFile.AppendLine();
                strFile.AppendLine($"namespace {UIScriptGeneratorSettings.GetUINameSpace()}");
                strFile.AppendLine("{");
                {
                    if (isAutoDiff)
                    {
                        if (root.name.StartsWith(widgetPrefix))
                        {
                            strFile.AppendLine($"\tpublic class {fileName.Replace(".cs", "")} : {uiType}");
                        }
                        else
                        {
                            strFile.AppendLine($"\t[Window(UILayer.UI, location : \"{fileName.Replace(".cs", "")}\")]");
                            strFile.AppendLine($"\tpublic class {fileName.Replace(".cs", "")} : {uiType}");
                        }
                    }
                    else
                    {
                        if (!m_isWidget)
                        {
                            strFile.AppendLine($"\t[Window(UILayer.UI, location : \"{fileName.Replace(".cs", "")}\")]");
                        }

                        strFile.AppendLine($"\tpublic class {fileName.Replace(".cs", "")} : {uiType}");
                    }

                    strFile.AppendLine("\t{");
                }
            }

            // 脚本工具生成的代码
            strFile.AppendLine($"\t\t#region 脚本工具生成的代码");
            strFile.AppendLine();
            strFile.AppendLine(strVar.ToString());
            strFile.AppendLine("\t\tprotected override void ScriptGenerator()");
            strFile.AppendLine("\t\t{");
            {
                strFile.Append(strBind.ToString());
                strFile.Append(strOnCreate.ToString());
            }
            strFile.AppendLine("\t\t}");
            strFile.AppendLine();
            strFile.Append($"\t\t#endregion");
            strFile.AppendLine();

            if (includeListener)
            {
                strFile.AppendLine();
                strFile.AppendLine("\t\t#region 事件");
                strFile.AppendLine();
                strFile.Append(strCallback.ToString());
                strFile.AppendLine($"\t\t#endregion");
                strFile.AppendLine("\t}");
                strFile.AppendLine("}");
            }

            m_textEditor.Delete();
            m_textEditor.text = strFile.ToString();
            m_textEditor.SelectAll();
            m_textEditor.Copy();

            if (isAutoGenerate)
            {
                string path = savePath?.Replace("\\", "/");

                bool isOk = EditorUtility.DisplayDialog("生成脚本确认", $"将在目录: {path} 生成脚本文件: {fileName}", "确认", "取消");

                if (!isOk || string.IsNullOrEmpty(path))
                {
                    return false;
                }

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var filePath = Path.Combine(path, fileName).Replace("\\", "/");

                if (File.Exists(filePath))
                {
                    bool isOverride =
                        EditorUtility.DisplayDialog("警告", $"目录: {path} 已存在脚本文件: {fileName} 是否覆盖生成？", "确认", "取消");

                    if (isOverride)
                    {
                        File.Delete(filePath);
                        AssetDatabase.Refresh();
                    }
                    else
                    {
                        return false;
                    }
                }

                File.WriteAllText(filePath, strFile.ToString(), Encoding.UTF8);
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.Log($"<color=#1E90FF>脚本已生成到剪贴板，请自行Ctl+V粘贴</color>");
            }

            return true;
        }

        private static int m_bindIndex = 0;
        public static void AutoErgodic(Transform root, Transform transform, ref StringBuilder strVar,
            ref StringBuilder strBind, ref StringBuilder strOnCreate, ref StringBuilder strCallback, bool isUniTask)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                WriteAutoScript(root, child, ref strVar, ref strBind, ref strOnCreate, ref strCallback, isUniTask);
                // 跳过 "m_item"
                if (child.name.StartsWith(GetUIWidgetName()))
                {
                    continue;
                }

                AutoErgodic(root, child, ref strVar, ref strBind, ref strOnCreate, ref strCallback, isUniTask);
            }
        }

        private static void WriteAutoScript(Transform root, Transform child, ref StringBuilder strVar,
            ref StringBuilder strBind, ref StringBuilder strOnCreate, ref StringBuilder strCallback, bool isUniTask)
        {
            string varName = child.name;
            // 查找相关的规则定义
            var rule = UIScriptGeneratorSettings.GetScriptGenerateRulers()
                .Find(r => varName.StartsWith(r.uiElementRegex));

            if (rule == null)
            {
                return;
            }

            var componentName = rule.componentName.ToString();

            if (string.IsNullOrEmpty(componentName))
            {
                return;
            }

            varName = GetVariableName(varName);

            if (string.IsNullOrEmpty(varName))
            {
                return;
            }

            // string varPath = GetRelativePath(child, root);
            strVar.AppendLine($"\t\tprivate {componentName} {varName};");

            if (rule.componentName == UIComponentName.GameObject)
            {
                strBind.AppendLine($"\t\t\t{varName} = m_bindComponent.GetComponent<RectTransform>({m_bindIndex}).gameObject;");
            }
            else
            {
                strBind.AppendLine($"\t\t\t{varName} = m_bindComponent.GetComponent<{componentName}>({m_bindIndex});");
            }
            m_bindIndex++;

            switch (rule.componentName)
            {
                case UIComponentName.Button:
                    var btnFuncName = GetButtonFuncName(varName);

                    if (isUniTask)
                    {
                        strOnCreate.AppendLine(
                            $"\t\t\t{varName}.onClick.AddListener(UniTask.UnityAction({btnFuncName}));");
                        strCallback.AppendLine($"\t\tprivate async UniTaskVoid {btnFuncName}()");
                        strCallback.AppendLine("\t\t{");
                        strCallback.AppendLine("\t\t\tawait UniTask.Yield();");
                        strCallback.AppendLine("\t\t}");
                    }
                    else
                    {
                        strOnCreate.AppendLine($"\t\t\t{varName}.onClick.AddListener({btnFuncName});");
                        strCallback.AppendLine($"\t\tprivate void {btnFuncName}()");
                        strCallback.AppendLine("\t\t{");
                        strCallback.AppendLine("\t\t}");
                    }

                    strCallback.AppendLine();
                    break;

                case UIComponentName.Toggle:
                    var toggleFuncName = GetToggleFuncName(varName);
                    strOnCreate.AppendLine($"\t\t\t{varName}.onValueChanged.AddListener({toggleFuncName});");
                    strCallback.AppendLine($"\t\tprivate void {toggleFuncName}(bool isOn)");
                    strCallback.AppendLine("\t\t{");
                    strCallback.AppendLine("\t\t}");
                    strCallback.AppendLine();
                    break;

                case UIComponentName.Slider:
                    var sliderFuncName = GetSliderFuncName(varName);
                    strOnCreate.AppendLine($"\t\t\t{varName}.onValueChanged.AddListener({sliderFuncName});");
                    strCallback.AppendLine($"\t\tprivate void {sliderFuncName}(float value)");
                    strCallback.AppendLine("\t\t{");
                    strCallback.AppendLine("\t\t}");
                    strCallback.AppendLine();
                    break;
            }
        }

        public static bool GenerateUIComponentScript()
        {
            var root = Selection.activeTransform;

            if (root == null)
            {
                return false;
            }
            // 检查是否在预制体编辑模式下
            bool isInPrefabMode = IsInPrefabMode(root.gameObject);

            CheckVariableNames();
            var uiComponent = AddComponent3Window();

            if (uiComponent == null)
            {
                return false;
            }

            ErgodicUIComponent(root, root, uiComponent);
            // 如果是预制体模式，需要特殊处理保存
            if (isInPrefabMode)
            {
                SavePrefabChanges(root.gameObject);
            }
            AssetDatabase.Refresh();
            return true;
            // Debug.Log($"<color=#1E90FF>脚本已生成到剪贴板，请自行Ctl+V粘贴</color>");
        }

        public static void ErgodicUIComponent(Transform root, Transform transform, UIBindComponent uiBindComponent)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                WriteScriptUIComponent(root, child, uiBindComponent);

                // 跳过 "m_item"
                if (child.name.StartsWith(GetUIWidgetName()))
                {
                    continue;
                }

                ErgodicUIComponent(root, child, uiBindComponent);
            }
        }

        private static void WriteScriptUIComponent(Transform root, Transform child, UIBindComponent uiBindComponent)
        {
            string varName = child.name;
            // 查找相关的规则定义
            var rule = UIScriptGeneratorSettings.GetScriptGenerateRulers()
                .Find(r => varName.StartsWith(r.uiElementRegex));

            if (rule == null)
            {
                return;
            }

            var componentName = rule.componentName.ToString();

            if (string.IsNullOrEmpty(componentName))
            {
                return;
            }

            if (rule.componentName == UIComponentName.GameObject)
            {
                var c = child.gameObject.GetComponent<RectTransform>();
                uiBindComponent.AddComponent(c);
                return;
            }

            Type componentType = GetComponentTypeFromEnumName(rule.componentName);

            if (componentType == null)
            {
                componentType = GetComponentTypeFromEnumName(componentName);

                if (componentType == null)
                {
                    Debug.LogWarning($"未找到对应的组件类型: {componentName}");
                    return;
                }
            }

            varName = GetVariableName(varName);

            if (string.IsNullOrEmpty(varName))
            {
                return;
            }

            var com = child.GetComponent(componentType);
            uiBindComponent.AddComponent(com);
        }

        private static Type GetComponentTypeFromEnumName(string enumName)
        {
            Type type = Type.GetType($"UnityEngine.{enumName}, UnityEngine");
            if (type != null) return type;

            type = Type.GetType($"UnityEngine.UI.{enumName}, UnityEngine.UI");
            if (type != null) return type;

            type = Type.GetType($"GameLogic.{enumName}, GameLogic");
            if (type != null) return type;

            type = Type.GetType(enumName);
            if (type != null) return type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(enumName);
                if (type != null) return type;

                type = assembly.GetType($"UnityEngine.{enumName}");
                if (type != null) return type;

                type = assembly.GetType($"UnityEngine.UI.{enumName}");
                if (type != null) return type;

                type = assembly.GetType($"GameLogic.{enumName}");
                if (type != null) return type;
            }

            return null;
        }

        private static Type GetComponentTypeFromEnumName(UIComponentName enumName)
        {
            return enumName switch
            {
                UIComponentName.GameObject => typeof(GameObject),
                UIComponentName.Button => typeof(Button),
                UIComponentName.Toggle => typeof(Toggle),
                UIComponentName.Slider => typeof(Slider),
                UIComponentName.Text => typeof(Text),
                UIComponentName.Canvas => typeof(Canvas),
                UIComponentName.Image => typeof(Image),
                UIComponentName.RectTransform => typeof(RectTransform),
                UIComponentName.Transform => typeof(Transform),
                UIComponentName.AnimationCurve => typeof(AnimationCurve),
                UIComponentName.Scrollbar => typeof(Scrollbar),
                UIComponentName.ScrollRect => typeof(ScrollRect),
                UIComponentName.CanvasGroup => typeof(CanvasGroup),
                UIComponentName.InputField => typeof(InputField),
                UIComponentName.ToggleGroup => typeof(ToggleGroup),
                UIComponentName.RawImage => typeof(RawImage),
                UIComponentName.GridLayoutGroup => typeof(GridLayoutGroup),
                UIComponentName.HorizontalLayoutGroup => typeof(HorizontalLayoutGroup),
                UIComponentName.VerticalLayoutGroup => typeof(VerticalLayoutGroup),
                UIComponentName.Dropdown => typeof(Dropdown),
                UIComponentName.TextMeshProUGUI => typeof(TextMeshProUGUI),
                _ => null,
            };
        }

        private static UIBindComponent AddComponent3Window()
        {
            var root = Selection.activeTransform;

            if (root == null)
            {
                Debug.LogWarning("请先选中一个物体，再进行脚本生成");
                return null;
            }

            GameObject rootObj = root.gameObject;

            var compt = rootObj.GetComponent<UIBindComponent>();

            if (compt == null)
            {
                compt = rootObj.AddComponent<UIBindComponent>();
            }

            compt.Clear();
            return compt;
        }

        private static bool IsInPrefabMode(GameObject gameObject)
        {
#if UNITY_EDITOR
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                return true;
            }

            var prefabAssetType = PrefabUtility.GetPrefabAssetType(gameObject);

            if (prefabAssetType != PrefabAssetType.NotAPrefab)
            {
                return true;
            }

            var prefabInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(gameObject);
            return prefabInstanceStatus != PrefabInstanceStatus.NotAPrefab;
#else
            return false;
#endif
        }

        private static void SavePrefabChanges(GameObject prefabObject)
        {
#if UNITY_EDITOR
            try
            {
                EditorUtility.SetDirty(prefabObject);
                var prefabInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(prefabObject);

                if (prefabInstanceStatus == PrefabInstanceStatus.Connected)
                {
                    var rootPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefabObject);

                    if (rootPrefab != null)
                    {
                        PrefabUtility.ApplyPrefabInstance(prefabObject,
                            InteractionMode.AutomatedAction);
                    }
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Debug.Log("预制体修改已保存");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"保存预制体时出错: {e.Message}");
            }
#endif
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            // 先在直接子级中查找
            Transform result = parent.Find(childName);
            if (result != null)
                return result;

            // 递归在子级的子级中查找
            foreach (Transform child in parent)
            {
                result = FindDeepChild(child, childName);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}