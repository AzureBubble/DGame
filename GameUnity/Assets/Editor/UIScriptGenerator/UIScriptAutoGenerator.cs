using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

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

        public class GenerateUIComponentWindow : EditorWindow
        {
            private string m_savePath;
            private bool m_isGenerateUIComponent;
            private bool m_isUniTask;
            private static int MIN_WIDTH = 400;
            private static int MIN_HEIGHT = 60;
            private static Rect mainWindowPosition => EditorGUIUtility.GetMainWindowPosition();

            [MenuItem("GameObject/ScriptGenerator/BindUIComponent", priority = 81)]
            public static void GenerateUIComponent()
            {
                var window = EditorWindow.GetWindow<GenerateUIComponentWindow>();
                window.titleContent = new GUIContent("自动生成组件绑定脚本");
                window.minSize = new Vector2(MIN_WIDTH, MIN_HEIGHT);
                // window.maxSize = new Vector2(float.MaxValue, 60);
                var center = new Vector2(mainWindowPosition.x + mainWindowPosition.width / 2,
                    mainWindowPosition.y + mainWindowPosition.height / 2);

                window.position = new Rect(center.x - MIN_WIDTH / 2, center.y - MIN_HEIGHT / 2, MIN_WIDTH, MIN_HEIGHT);
                window.m_isGenerateUIComponent = true;
            }

            [MenuItem("GameObject/ScriptGenerator/BindUIComponent", true)]
            public static bool ValidateGenerateUIComponent()
            {
                return UIScriptGeneratorSettings.Instance.UseBindComponent;
            }

            [MenuItem("GameObject/ScriptGenerator/GenerateUIProperty", priority = 82)]
            public static void GenerateUIPropertyBindComponent()
            {
                var window = EditorWindow.GetWindow<GenerateUIComponentWindow>();
                window.titleContent = new GUIContent("自动生成窗口脚本");
                window.minSize = new Vector2(MIN_WIDTH, MIN_HEIGHT);
                // window.maxSize = new Vector2(float.MaxValue, 60);
                var center = new Vector2(mainWindowPosition.x + mainWindowPosition.width / 2,
                    mainWindowPosition.y + mainWindowPosition.height / 2);

                window.position = new Rect(center.x - MIN_WIDTH / 2, center.y - MIN_HEIGHT / 2, MIN_WIDTH, MIN_HEIGHT);
                window.m_isGenerateUIComponent = false;
                window.m_isUniTask = false;
            }

            [MenuItem("GameObject/ScriptGenerator/GenerateUIProperty", true)]
            public static bool ValidateGenerateUIPropertyBindComponent()
            {
                return UIScriptGeneratorSettings.Instance.UseBindComponent;
            }

            [MenuItem("GameObject/ScriptGenerator/GenerateUIProperty - UniTask", priority = 83)]
            public static void GenerateUIPropertyBindComponentUniTask()
            {
                var window = EditorWindow.GetWindow<GenerateUIComponentWindow>();
                window.titleContent = new GUIContent("自动生成窗口脚本");
                window.minSize = new Vector2(MIN_WIDTH, MIN_HEIGHT);
                // window.maxSize = new Vector2(float.MaxValue, 60);
                var center = new Vector2(mainWindowPosition.x + mainWindowPosition.width / 2,
                    mainWindowPosition.y + mainWindowPosition.height / 2);

                window.position = new Rect(center.x - MIN_WIDTH / 2, center.y - MIN_HEIGHT / 2, MIN_WIDTH, MIN_HEIGHT);
                window.m_isGenerateUIComponent = false;
                window.m_isUniTask = true;
                window.wantsMouseMove = false;
                window.Show();
                window.Focus();
            }

            [MenuItem("GameObject/ScriptGenerator/GenerateUIProperty - UniTask", true)]
            public static bool ValidateGenerateUIPropertyBindComponentUniTask()
            {
                return UIScriptGeneratorSettings.Instance.UseBindComponent;
            }

            private void OnEnable()
            {
                m_savePath = UIScriptGeneratorSettings.GetCodePath();
            }

            private void OnGUI()
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"生成脚本目录预览: ", EditorStyles.boldLabel, GUILayout.Width(150));
                GUILayout.Label($"{(!string.IsNullOrEmpty(m_savePath) ? m_savePath : UIScriptGeneratorSettings.Instance.CodePath)}", EditorStyles.boldLabel, GUILayout.Width(float.MaxValue));
                GUILayout.EndHorizontal();
                // m_savePath = EditorGUILayout.TextField("生成目标目录名", m_savePath);
                m_savePath = DrawFolderField("生成脚本目录", String.Empty, m_savePath);
                m_savePath = string.IsNullOrEmpty(m_savePath) ? UIScriptGeneratorSettings.Instance.CodePath : m_savePath;
                // 横向排列的按钮
                GUILayout.BeginHorizontal();
                {
                    // 取消按钮
                    if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(true)))
                    {
                        Close();
                    }

                    // 生成按钮
                    GUI.enabled = !string.IsNullOrEmpty(m_savePath); // 输入为空时禁用生成按钮
                    if (GUILayout.Button("Generate", GUILayout.ExpandWidth(true)))
                    {
                        if (m_isGenerateUIComponent)
                        {
                            if (GenerateUIComponentScript(m_savePath))
                            {

                            }
                            Close();
                        }
                        else
                        {
                            if (m_isUniTask)
                            {
                                GenerateCSharpScript(true, true, true, m_savePath);
                            }
                            else
                            {
                                GenerateCSharpScript(true, false, true, m_savePath);
                            }
                            Close();
                        }
                    }
                    GUI.enabled = true; // 恢复GUI启用状态
                }
                GUILayout.EndHorizontal();
            }

            private static string DrawFolderField(string label, string labelIcon, string path)
            {
                using var horizontalScope = new EditorGUILayout.HorizontalScope();

                var buttonGUIContent = new GUIContent("选择", EditorGUIUtility.IconContent("Folder Icon").image);

                if (!string.IsNullOrEmpty(labelIcon))
                {
                    var labelGUIContent = new GUIContent(" " + label, EditorGUIUtility.IconContent(labelIcon).image);
                    path = EditorGUILayout.TextField(labelGUIContent, path);
                }
                else
                {
                    path = EditorGUILayout.TextField(label, path);
                }

                if (GUILayout.Button(buttonGUIContent, GUILayout.Width(60), GUILayout.Height(20)))
                {
                    var newPath = EditorUtility.OpenFolderPanel(label, path, string.Empty);
                    newPath = newPath.Replace(Application.dataPath, "Assets");
                    if (!string.IsNullOrEmpty(newPath) && newPath.StartsWith(UIScriptGeneratorSettings.GetCodePath()))
                    {
                        path = newPath;
                        // path = "Assets" + newPath.Substring(Application.dataPath.Length);
                        // Debug.LogError(newPath);
                    }
                    else
                    {
                        Debug.LogError("路径不在UIScriptGeneratorSettings设置的codePath内: " + newPath);
                    }
                }
                return path;
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

        private static bool GenerateCSharpScript(bool includeListener, bool isUniTask = false, bool isAutoGenerate = false, string savePath = null)
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
            if (root.name.StartsWith(widgetPrefix))
            {
                fileName = $"{root.name.Replace(GetUIWidgetName(), string.Empty)}.cs";
                rootName = $"{root.name.Replace(GetUIWidgetName(), string.Empty)}{widgetComSufName}";
                strVar.AppendLine($"\t\tprivate {rootName} m_bindComponent;");
            }
            else
            {
                strVar.AppendLine($"\t\tprivate {rootName} m_bindComponent;");
            }

            strBind.AppendLine($"\t\t\tm_bindComponent = gameObject.GetComponent<{rootName}>();");
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
                    if (root.name.StartsWith(widgetPrefix))
                    {
                        strFile.AppendLine($"\tpublic class {root.name.Replace(widgetPrefix, string.Empty)} : UIWidget");
                    }
                    else
                    {
                        strFile.AppendLine($"\t[Window(UILayer.UI, location : \"{root.name}\")]");
                        strFile.AppendLine($"\tpublic class {root.name} : UIWindow");
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
                if (!isOk)
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
                    bool isOverride = EditorUtility.DisplayDialog("警告", $"目录: {path} 已存在脚本文件: {fileName} 是否覆盖生成？", "确认", "取消");

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
            strBind.AppendLine($"\t\t\t{varName} = m_bindComponent.{varName};");

            switch (rule.componentName)
            {
                case UIComponentName.Button:
                    var btnFuncName = GetButtonFuncName(varName);
                    if (isUniTask)
                    {
                        strOnCreate.AppendLine($"\t\t\t{varName}.onClick.AddListener(UniTask.UnityAction({btnFuncName}));");
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

        private static bool GenerateUIComponentScript(string savePath)
        {
            var root = Selection.activeTransform;

            if (root == null)
            {
                return false;
            }

            CheckVariableNames();

            var windowComSufName = UIScriptGeneratorSettings.Instance.WindowComponentSuffixName;
            var widgetComSufName = UIScriptGeneratorSettings.Instance.WidgetComponentSuffixName;

            string fileName = null;
            var widgetPrefix = GetUIWidgetName();
            var rootName = $"{root.name}{windowComSufName}";

            if (root.name.StartsWith(widgetPrefix))
            {
                rootName = $"{root.name.Replace(GetUIWidgetName(), string.Empty)}{widgetComSufName}";
            }
            fileName = $"{rootName}.cs";

            StringBuilder strVar = new StringBuilder();
            ErgodicUIComponent(root, root, ref strVar);
            StringBuilder strFile = new StringBuilder();
            strFile.AppendLine("//----------------------------------------------------------");
            strFile.AppendLine("// <auto-generated>");
            strFile.AppendLine("// -This code was generated.");
            strFile.AppendLine("// -Changes to this file may cause incorrect behavior.");
            strFile.AppendLine("// -will be lost if the code is regenerated.");
            strFile.AppendLine("// <auto-generated/>");
            strFile.AppendLine("//----------------------------------------------------------");

#if ENABLE_TEXTMESHPRO
            strFile.AppendLine("using TMPro;");
#endif
            strFile.AppendLine("using UnityEngine;");
            strFile.AppendLine("using UnityEngine.UI;");
            strFile.AppendLine("using DGame;");
            strFile.AppendLine();
            strFile.AppendLine($"namespace {UIScriptGeneratorSettings.GetUINameSpace()}");
            strFile.AppendLine("{");
            {
                if (root.name.StartsWith(widgetPrefix))
                {
                    strFile.AppendLine($"\tpublic class {rootName} : MonoBehaviour");
                }
                else
                {
                    strFile.AppendLine($"\t[DisallowMultipleComponent]");
                    strFile.AppendLine($"\tpublic class {rootName} : MonoBehaviour");
                }
                strFile.AppendLine("\t{");
            }

            // 脚本工具生成的代码
            strFile.Append(strVar.ToString());
            strFile.AppendLine("\t}");
            strFile.AppendLine("}");

            string path = savePath.Replace("\\", "/");

            bool isOk = EditorUtility.DisplayDialog("生成脚本确认", $"将在目录: {path} 生成脚本文件: {fileName}", "确认", "取消");
            if (!isOk)
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
                File.Delete(filePath);
                AssetDatabase.Refresh();
            }
            File.WriteAllText(filePath, strFile.ToString(), Encoding.UTF8);
            EditorPrefs.SetString("GeneratorClassName", fileName.Replace(".cs", string.Empty));
            AssetDatabase.Refresh();
            return true;
            // Debug.Log($"<color=#1E90FF>脚本已生成到剪贴板，请自行Ctl+V粘贴</color>");
        }

        public static void ErgodicUIComponent(Transform root, Transform transform, ref StringBuilder strVar)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                WriteScriptUIComponent(root, child, ref strVar);

                // 跳过 "m_item"
                if (child.name.StartsWith(GetUIWidgetName()))
                {
                    continue;
                }
                ErgodicUIComponent(root, child, ref strVar);
            }
        }

        private static void WriteScriptUIComponent(Transform root, Transform child, ref StringBuilder strVar)
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
            strVar.AppendLine($"\t\tpublic {componentName} {varName};");
        }

        /// <summary>
        /// 编译完成系统自动调用
        /// </summary>
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void AddComponent2Window()
        {
            var generatorClassName = EditorPrefs.GetString("GeneratorClassName");
            if (string.IsNullOrEmpty(generatorClassName))
            {
                return;
            }

            //1.通过反射的方式，从程序集中找到这个脚本，把它挂在到当前的物体上
            //获取所有的程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            //找到Csharp程序集
            var cSharpAssembly = assemblies.First(assembly => assembly.GetName().Name == UIScriptGeneratorSettings.GetUINameSpace());
            //获取类所在的程序集路径
            string relClassName = UIScriptGeneratorSettings.GetUINameSpace() + "." + generatorClassName;
            Type type = cSharpAssembly.GetType(relClassName);

            if (type == null)
            {
                return;
            }

            var windowComSufName = UIScriptGeneratorSettings.Instance.WindowComponentSuffixName;
            var widgetComSufName = UIScriptGeneratorSettings.Instance.WidgetComponentSuffixName;
            //获取要挂载的那个物体
            string windowObjName = generatorClassName.Replace(windowComSufName, "");

            if (generatorClassName.EndsWith(widgetComSufName))
            {
                windowObjName = generatorClassName.Replace(widgetComSufName, "");
                windowObjName = GetPrefixName() + UIScriptGeneratorSettings.GetUIWidgetName() + windowObjName;
            }
            GameObject windowObj = GameObject.Find(windowObjName);

            if (windowObj == null)
            {
                windowObj = GameObject.Find("UIRoot/UICanvas/" + windowObjName);

                if (windowObj == null)
                {
                    return;
                }
            }

            //先获取现窗口上有没有挂载该数据组件，如果没挂载在进行挂载
            Component compt = windowObj.GetComponent(type);

            if (compt != null)
            {
                GameObject.DestroyImmediate(compt);
                compt = null;
            }

            if (compt == null)
            {
                compt = windowObj.AddComponent(type);
            }

            //2.通过反射的方式，遍历数据列表 找到对应的字段，赋值
            //获取对象数据列表
            //获取脚本所有字段
            FieldInfo[] fieldInfoList = type.GetFields();
            foreach (var item in fieldInfoList)
            {
                GameObject go = FindDeepChild(windowObj.transform, item.Name)?.gameObject;

                if (go == null)
                {
                    Debug.LogError($"{windowObj.name}中没有找到相关组件: {item.Name} 请检查！！！");
                    return;
                }
                if (string.Equals(item.FieldType.Name, "GameObject"))
                {
                    item.SetValue(compt, go);
                }
                else
                {
                    item.SetValue(compt, go?.GetComponent(item.FieldType));
                }
            }

            PrefabInstanceStatus status = PrefabUtility.GetPrefabInstanceStatus(windowObj);

            if (status == PrefabInstanceStatus.Connected)
            {
                PrefabUtility.ApplyPrefabInstance(windowObj, InteractionMode.UserAction);
            }

            EditorPrefs.DeleteKey("GeneratorClassName");
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