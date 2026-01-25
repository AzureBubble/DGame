using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GameLogic;
using UnityEditor;
using UnityEngine;

#if TextMeshPro
using TMPro;
#endif

namespace DGame
{
    /// <summary>
    /// 文本处理核心类 - 负责代码分析、文本提取、预制体处理等功能
    /// </summary>
    public static class TextProcessorTool
    {
        #region 常量定义

        /// <summary>
        /// 匹配 G.R("xxx") 模式，捕获引号内的内容
        /// </summary>
        private static readonly Regex m_G_R_Pattern = new(@"G\s*\.\s*R\s*\(\s*""([^""]*)""", RegexOptions.Compiled);

        /// <summary>
        /// 匹配字符串格式参数 {0}, {1} 等
        /// </summary>
        private static readonly Regex m_formatParamRegex = new(@"\{[\d}]+\}", RegexOptions.Compiled);

        /// <summary>
        /// 匹配中文字符
        /// </summary>
        private static readonly Regex m_chineseRegex = new(@"[\u4e00-\u9fa5]", RegexOptions.Compiled);

        private static GRCodeExtractOptions m_codeExtractOptionsData;

        private static StringBuilder m_sb = new StringBuilder();

        #endregion

        #region 代码文本提取

        public static void ExtractGRFromCode(GRCodeExtractOptions options)
        {
            m_codeExtractOptionsData = options;
            var strMap = new Dictionary<string, TextEntry>();
            var writeList = new List<TextEntry>();
            var paramList = new List<int>();
            var codeFiles = Directory.GetFiles(options.ScriptFolderPath, "*.cs", SearchOption.AllDirectories);
            int listCount = codeFiles.Length;
            AssetDatabase.StartAssetEditing();

            try
            {
                for (int i = 0; i < listCount; i++)
                {
                    writeList.Clear();
                    var file = codeFiles[i];
                    EditorUtility.DisplayProgressBar("提取代码G.R文本", $"正在处理: {Path.GetFileName(file)}", (float)i / listCount);

                    try
                    {
                        string text = File.ReadAllText(file);
                        m_G_R_Pattern.Replace(text, match =>
                        {
                            string content = match.Groups[1].Value;
                            if (!strMap.TryGetValue(content, out var textEntry))
                            {
                                paramList.Clear();
                                var collect = m_formatParamRegex.Matches(content);
                                foreach (Match item in collect)
                                {
                                    // 匹配{0} {1} 上的数字0 1
                                    if(match.Success && int.TryParse(item.Value.Trim('{', '}'), out int paramIndex))
                                    {
                                        paramList.Add(paramIndex);
                                    }
                                }

                                int maxParamIndex = -1;

                                foreach (var paramIndex in paramList)
                                {
                                    if (paramIndex > maxParamIndex)
                                    {
                                        maxParamIndex = paramIndex;
                                    }
                                }
                                var paramCnt = maxParamIndex + 1;

                                if (options.UseExistingText &&
                                    EditorConfigLoader.TryGetTextDefineStr(content, out var defineText))
                                {
                                    textEntry = new TextEntry(content, defineText, paramCnt);
                                }
                                else
                                {
                                    textEntry = new TextEntry(content, options.StartId++, paramCnt);
                                    strMap[content] = textEntry;
                                }
                            }
                            writeList.Add(textEntry);
                            return match.Value;
                        });

                        if (writeList.Count > 0)
                        {
                            foreach (var textEntry in writeList)
                            {
                                var entry = $"\"{textEntry.Content}\"";
                                text = text.Replace(entry, $"TextDefine.{textEntry.TextDefineIdName}");
                            }

                            using (var stream = new FileStream(file, FileMode.Create))
                            {
                                var bytes = Encoding.UTF8.GetBytes(text);
                                stream.Write(bytes, 0, bytes.Length);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"处理文件 {file} 时出错: {ex.Message}");
                    }
                }

                string textDefine = File.ReadAllText(options.TextDefinePath);
                bool connect = false;
                int startIndex = textDefine.IndexOf("// AutoBuildStart", StringComparison.Ordinal);
                int endIndex = textDefine.IndexOf("\t\t// AutoBuildEnd", StringComparison.Ordinal);

                if (startIndex > 0 && endIndex >= startIndex)
                {
                    var source = textDefine.Substring(startIndex, endIndex - startIndex);
                    string[] allText = source.Split(new[] { "\r\n" }, StringSplitOptions.None);
                    // 如果前一个ID已经定义了，就插在前一个id后面
                    if (EditorConfigLoader.TryGetTextDefineStr(options.StartId - 1, out var defineText))
                    {
                        foreach (var textItem in allText)
                        {
                            if (textItem.Contains(defineText))
                            {
                                var oldIndex = endIndex;
                                var textIndex = textDefine.IndexOf(textItem, StringComparison.Ordinal);
                                endIndex = textIndex + textItem.Length + "\r\n".Length;
                                endIndex = endIndex > oldIndex ? endIndex : oldIndex;
                                connect = true;
                                break;
                            }
                        }
                    }
                }

                // 注册到TextDefine
                string space = "\t\t";

                if (endIndex >= 0)
                {
                    m_sb.Clear();

                    // 不是连接上一行的情况 空一行
                    if (!connect)
                    {
                        m_sb.Append("\n");
                    }
                    // 块标签
                    if (!connect && !string.IsNullOrEmpty(options.Tag))
                    {
                        m_sb.Append($"{space}// {options.Tag}\n");
                    }

                    int cnt = 0;
                    foreach (var textEntry in strMap.Values)
                    {
                        var ret = $"{space}{textEntry.TextDefineIdName}{(cnt == 0 ? $" = {textEntry.TextDefineId}" : "")},   // {textEntry.Content}\r\n";
                        m_sb.Append(ret);
                        cnt++;
                    }
                    textDefine = textDefine.Insert(endIndex, m_sb.ToString());

                    using (var stream = new FileStream(options.TextDefinePath, FileMode.Create))
                    {
                        var bytes = Encoding.UTF8.GetBytes(textDefine);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }

                // 写表
                m_sb.Clear();
                var valueList = strMap.Values.ToList();
                valueList.Sort((a, b) => a.TextDefineId - b.TextDefineId);
                for (int i = 0; i < valueList.Count; i++)
                {
                    var value = valueList[i];

                    if (value == null)
                    {
                        continue;
                    }

                    m_sb.Append(
                        $"\t{value.TextDefineId}\t{(value.ParamCount > 0 ? value.ParamCount.ToString() : string.Empty)}\t{value.Content}\t{(string.IsNullOrEmpty(options.Tag) ? string.Empty : options.Tag)}\r\n");
                }
                var excelContent = Encoding.UTF8.GetBytes(m_sb.ToString());
                using (var stream = new FileStream(options.OutputPath, FileMode.Create))
                {
                    stream.Write(excelContent, 0, excelContent.Length);
                    System.Diagnostics.Process.Start("notepad.exe", options.OutputPath);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
            }
        }

        public static int GetLastTextDefineID(string textDefinePath)
        {
            int id = 0;
            string textDefine = File.ReadAllText(textDefinePath);
            int startIndex = textDefine.IndexOf("// AutoBuildStart", StringComparison.Ordinal);
            int endIndex = textDefine.IndexOf("// AutoBuildEnd", StringComparison.Ordinal);

            if (startIndex > 0 && endIndex >= startIndex)
            {
                var source = textDefine.Substring(startIndex, endIndex - startIndex);
                string[] allText = source.Split(new[] { "\r\n" }, StringSplitOptions.None);
                if (allText.Length > 0)
                {
                    var matchText = "LabelID";

                    for (int i = allText.Length - 1; i >= 0; i--)
                    {
                        var text = allText[i];

                        if (text.Contains(matchText))
                        {
                            var index1 = text.IndexOf(matchText, StringComparison.Ordinal) + matchText.Length;
                            var index2 = text.IndexOf(",", StringComparison.Ordinal);

                            if (index2 > index1)
                            {
                                var label = text.Substring(index1, index2 - index1);
                                int.TryParse(label, out id);
                            }
                            break;
                        }
                    }
                }
            }
            return id;
        }

        #endregion

        #region 预制体提取

        public static PrefabExtractResult ExtractFromPrefabs(PrefabExtractOptions options)
        {
            var result = new PrefabExtractResult();
            var extractedTexts = new Dictionary<string, PrefabTextEntry>();

            // 查找所有预制体
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { options.PrefabFolderPath });
            var prefabPaths = prefabGuids.Select(AssetDatabase.GUIDToAssetPath).ToArray();

            AssetDatabase.StartAssetEditing();

            try
            {
                for (int i = 0; i < prefabPaths.Length; i++)
                {
                    var prefabPath = prefabPaths[i];
                    EditorUtility.DisplayProgressBar("提取预制体文本", $"正在处理: {Path.GetFileNameWithoutExtension(prefabPath)}", (float)i / prefabPaths.Length);

                    try
                    {
                        ExtractFromPrefab(prefabPath, extractedTexts, options, result);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"处理预制体 {prefabPath} 时出错: {ex.Message}");
                        result.ErrorCount++;
                    }

                    result.ProcessedPrefabs++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
            }

            // 导出配置
            result.OutputPath = ExportPrefabTextConfig(extractedTexts, options.OutputDirectory);
            result.NewTextCount = extractedTexts.Count;

            return result;
        }

        private static void ExtractFromPrefab(string prefabPath, Dictionary<string, PrefabTextEntry> extractedTexts,
            PrefabExtractOptions options, PrefabExtractResult result)
        {
            // 加载预制体
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            var prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            var needSave = false;
            var currentId = options.StartId;

            // 处理所有 Text 组件
            var textComponents = prefab.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            foreach (var text in textComponents)
            {
                if (ExtractFromTextComponent(text, prefabName, extractedTexts, options, ref currentId))
                {
                    needSave = true;
                    result.BinderCount++;
                }
            }

            // 处理 TextMeshPro 组件
            if (options.IncludeTextMeshPro)
            {
#if TextMeshPro
                var tmpComponents = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmpComponents)
                {
                    if (ExtractFromTextComponent(tmp, prefabName, extractedTexts, options, ref currentId))
                    {
                        needSave = true;
                        result.BinderCount++;
                    }
                }
#endif
            }

            if (needSave)
            {
                EditorUtility.SetDirty(prefab);
            }
        }

        private static bool ExtractFromTextComponent(Component textComponent, string prefabName,
            Dictionary<string, PrefabTextEntry> extractedTexts, PrefabExtractOptions options, ref int currentId)
        {
            string textContent = null;
            Type binderType = null;

            // 获取文本内容
            if (textComponent is UnityEngine.UI.Text uiText)
            {
                textContent = uiText.text;
                binderType = typeof(UITextIDBinder);
            }
#if TextMeshPro
            else if (textComponent is TextMeshProUGUI tmp)
            {
                textContent = tmp.text;
                binderType = typeof(UITextIDBinder);
            }
#endif

            if (string.IsNullOrEmpty(textContent)) return false;
            if (!m_chineseRegex.IsMatch(textContent)) return false;

            // 检查是否已有 Binder
            var existingBinder = textComponent.GetComponent(binderType);
            if (existingBinder != null && !options.OverwriteExisting)
            {
                return false;
            }

            // 获取或创建条目
            if (!extractedTexts.TryGetValue(textContent, out var entry))
            {
                entry = new PrefabTextEntry
                {
                    Content = textContent,
                    Id = currentId++,
                    GameObjectName = textComponent.gameObject.name,
                    PrefabName = prefabName
                };
                extractedTexts[textContent] = entry;
            }
            else
            {
                entry.GameObjectName = textComponent.gameObject.name;
                entry.PrefabName = prefabName;
            }

            // 创建或更新 Binder
            if (existingBinder == null)
            {
                existingBinder = textComponent.gameObject.AddComponent(binderType);
            }

            // 设置 TextID
            var textIDField = binderType.GetField("TextID");
            if (textIDField != null)
            {
                var oldValue = (int)textIDField.GetValue(existingBinder);
                if (oldValue != entry.Id)
                {
                    textIDField.SetValue(existingBinder, entry.Id);
                    return true;
                }
            }

            return false;
        }

        private static string ExportPrefabTextConfig(Dictionary<string, PrefabTextEntry> texts, string outputDirectory)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var outputPath = Path.Combine(outputDirectory, $"PrefabTextConfig_{timestamp}.txt");

            var sb = new StringBuilder();
            sb.AppendLine("ID\t内容\t\t\t预制体\t游戏对象");

            foreach (var text in texts.Values.OrderBy(t => t.Id))
            {
                var content = text.Content.Replace("\t", "    ").Replace("\n", "\\n").Replace("\r", "");
                sb.AppendLine($"{text.Id}\t{content}\t{text.PrefabName}\t{text.GameObjectName}");
            }

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();

            return outputPath;
        }

        public static PrefabExtractResult ExportPrefabDebugInfo(string prefabPath, bool includeTextMeshPro)
        {
            var result = new PrefabExtractResult();

            var options = new PrefabExtractOptions
            {
                PrefabFolderPath = prefabPath,
                IncludeTextMeshPro = includeTextMeshPro
            };

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabPath });
            var allTexts = new List<PrefabTextEntry>();

            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                // 收集文本组件信息
                var textComponents = prefab.GetComponentsInChildren<UnityEngine.UI.Text>(true);
                foreach (var text in textComponents)
                {
                    CollectTextInfo(text, prefab.name, allTexts);
                }

                if (includeTextMeshPro)
                {
#if TextMeshPro
                    var tmpComponents = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var tmp in tmpComponents)
                    {
                        CollectTextInfo(tmp, prefab.name, allTexts);
                    }
#endif
                }
            }

            result.OutputPath = ExportDebugInfo(allTexts, options.OutputDirectory);
            return result;
        }

        private static void CollectTextInfo(Component textComponent, string prefabName, List<PrefabTextEntry> allTexts)
        {
            string textContent = null;
            string componentName = null;

            if (textComponent is UnityEngine.UI.Text uiText)
            {
                textContent = uiText.text;
                componentName = "Text";
            }
#if TextMeshPro
            else if (textComponent is TextMeshProUGUI tmp)
            {
                textContent = tmp.text;
                componentName = "TMP";
            }
#endif

            if (string.IsNullOrEmpty(textContent)) return;

            allTexts.Add(new PrefabTextEntry
            {
                Content = textContent,
                GameObjectName = textComponent.gameObject.name,
                PrefabName = prefabName,
                ComponentName = componentName,
                Id = allTexts.Count
            });
        }

        private static string ExportDebugInfo(List<PrefabTextEntry> texts, string outputDirectory)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var outputPath = Path.Combine(outputDirectory, $"PrefabDebug_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            var sb = new StringBuilder();
            sb.AppendLine("索引\t内容\t\t\t预制体\t游戏对象\t组件类型");

            foreach (var text in texts)
            {
                var content = text.Content.Replace("\t", "    ").Replace("\n", "\\n").Replace("\r", "");
                var hasChinese = m_chineseRegex.IsMatch(text.Content) ? "[中文]" : "";
                sb.AppendLine($"{text.Id}\t{content}\t{text.PrefabName}\t{text.GameObjectName}\t{text.ComponentName}{hasChinese}");
            }

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();

            return outputPath;
        }

        #endregion

        #region 现有配置加载

        public static Dictionary<int, string> LoadExistingTextConfigs()
        {
            var result = new Dictionary<int, string>();

            // 尝试从 TextDefine.cs 读取
            var textDefinePath = "Assets/Scripts/HotFix/GameLogic/Text/TextDefine.cs";
            if (File.Exists(textDefinePath))
            {
                var content = File.ReadAllText(textDefinePath);
                // 简单解析，实际可能需要更复杂的逻辑
                var lines = content.Split('\n');
                var id = 0;
                foreach (var line in lines)
                {
                    if (line.Trim().StartsWith("//") && line.Contains("ID:"))
                    {
                        result[id++] = line.Trim();
                    }
                }
            }

            return result;
        }

        #endregion
    }

    #region 数据结构

    /// <summary>
    /// 文本条目
    /// </summary>
    public class TextEntry
    {
        public string Content { get; set; }
        public int TextDefineId { get; set; }
        public int ParamCount { get; set; }
        public string TextDefineIdName { get; set; }
        public string ExtraParam { get; set; }
        public string ItemName { get; set; }
        public string Component { get; set; }
        public string TextType { get; set; }

        public TextEntry(string content, int textDefineId, int paramCnt, string extraParam, string itemName,
            string component, string textType)
        {
            Content = content;
            TextDefineId = textDefineId;
            ParamCount = paramCnt;
            TextDefineIdName = GetDefineIdName(textDefineId);
            ExtraParam = extraParam;
            ItemName = itemName;
            Component = component;
            TextType = textType;
        }

        public TextEntry(string content, string defineText, int paramCnt, string extraParam = "")
        {
            Content = content;
            TextDefineId = 0;
            ParamCount = paramCnt;
            TextDefineIdName = defineText;
            ExtraParam = extraParam;
        }
        public TextEntry(string content, int textDefineId, int paramCnt, string extraParam = "")
        {
            Content = content;
            TextDefineId = textDefineId;
            TextDefineIdName = GetDefineIdName(textDefineId);
            ExtraParam = extraParam;
            ParamCount = paramCnt;
        }
        private string GetDefineIdName(int id)
        {
            return "LabelID" + id;
        }
    }

    /// <summary>
    /// 预制体文本条目
    /// </summary>
    public class PrefabTextEntry
    {
        public string Content { get; set; }
        public int Id { get; set; }
        public string PrefabName { get; set; }
        public string GameObjectName { get; set; }
        public string ComponentName { get; set; }
    }

    /// <summary>
    /// 代码提取选项
    /// </summary>
    public class GRCodeExtractOptions
    {
        public string ScriptFolderPath { get; set; }
        public int StartId { get; set; }
        public string Tag { get; set; }
        public bool UseExistingText { get; set; }
        public string TextDefinePath { get; set; }
        public string OutputPath { get; set; }
    }

    /// <summary>
    /// 预制体提取选项
    /// </summary>
    public class PrefabExtractOptions
    {
        public string PrefabFolderPath { get; set; }
        public int StartId { get; set; }
        public bool IncludeTextMeshPro { get; set; }
        public bool OverwriteExisting { get; set; }
        public string OutputDirectory { get; set; }
    }

    /// <summary>
    /// 代码提取结果
    /// </summary>
    public class CodeExtractResult
    {
        public int ProcessedFiles { get; set; }
        public int NewTextCount { get; set; }
        public int ReusedTextCount { get; set; }
        public int ErrorCount { get; set; }
        public string OutputPath { get; set; }
    }

    /// <summary>
    /// 预制体提取结果
    /// </summary>
    public class PrefabExtractResult
    {
        public int ProcessedPrefabs { get; set; }
        public int NewTextCount { get; set; }
        public int BinderCount { get; set; }
        public int ErrorCount { get; set; }
        public string OutputPath { get; set; }
    }

    #endregion

    #region 辅助类

    /// <summary>
    /// 路径辅助类
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// 将绝对路径转换为相对于Assets的相对路径
        /// </summary>
        public static string MakeRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return string.Empty;

            var assetsIndex = absolutePath.IndexOf("Assets", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
            {
                return absolutePath.Substring(assetsIndex).Replace('\\', '/');
            }

            return absolutePath.Replace('\\', '/');
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        public static void EnsureDirectoryExists(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }

    /// <summary>
    /// 字符串扩展方法
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// 将绝对路径转换为相对于Assets的相对路径
        /// </summary>
        public static string MakeRelativePath(this string absolutePath)
        {
            return PathHelper.MakeRelativePath(absolutePath);
        }
    }

    #endregion
}