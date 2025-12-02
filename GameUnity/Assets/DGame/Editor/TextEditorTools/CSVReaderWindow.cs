using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class TextDefineCSVReaderWindow : EditorWindow
{
    private string DEFAULT_CSV_PATH => Application.dataPath + "/ABAssets/Configs/Localization.csv";
    // private const int m_enumStartIndex = 20000000;
    private const string m_csFileName = "TextDefine";
    private const string m_filterStr = "Text";
    private string m_csFileExportPath => Application.dataPath + "/Scripts/HotFix/GameLogic/Src/Text";

    private TextAsset m_csvTextAsset;
    private string m_csvFilePath = "";
    private Dictionary<string, CSVRowData> m_csvRowDatas = new Dictionary<string, CSVRowData>();
    private Vector2 m_scrollPosition;
    private bool m_hasHeader = false;

    [MenuItem("DGame Tools/文本表转TextDefine")]
    public static void ShowWindow()
    {
        GetWindow<TextDefineCSVReaderWindow>();
    }

    private void OnGUI()
    {
        // GUILayout.Label("CSV文件读取工具", EditorStyles.boldLabel);
        DrawHeader();
        // EditorGUILayout.Space(10);
        EditorGUI.BeginChangeCheck();
        m_csvTextAsset = (TextAsset)EditorGUILayout.ObjectField("CSV文件:", m_csvTextAsset, typeof(TextAsset), false);

        if (EditorGUI.EndChangeCheck())
        {
            if (m_csvTextAsset != null)
            {
                m_csvFilePath = AssetDatabase.GetAssetPath(m_csvTextAsset);
            }
            else
            {
                m_csvFilePath = "";
            }
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();

        if (string.IsNullOrEmpty(m_csvFilePath))
        {
            m_csvTextAsset = FindTextAssetInProject(DEFAULT_CSV_PATH);
            if(m_csvTextAsset != null)
                m_csvFilePath = DEFAULT_CSV_PATH;
        }
        EditorGUILayout.TextField("csv文件路径:", m_csvFilePath, GUILayout.ExpandWidth(true));

        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFilePanel("选择CSV文件", "", "csv");

            if (!string.IsNullOrEmpty(path))
            {
                m_csvFilePath = path;
                // 尝试在项目中查找对应的TextAsset
                m_csvTextAsset = FindTextAssetInProject(path);
            }
        }

        EditorGUILayout.EndHorizontal();

        // 显示当前选择的文件
        if (!string.IsNullOrEmpty(m_csvFilePath))
        {
            EditorGUILayout.HelpBox($"当前选择: {Path.GetFileName(m_csvFilePath)}", MessageType.Info);
        }

        // hasHeader = EditorGUILayout.Toggle("包含标题行", hasHeader);

        EditorGUILayout.Space(5);

        // 读取按钮 - 根据拖拽或路径选择
        bool canRead = m_csvTextAsset != null || File.Exists(m_csvFilePath);

        if (canRead)
        {
            if (GUILayout.Button("读取第一列Key", GUILayout.Height(30)))
            {
                ReadCSVFirstColumn();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("请先选择CSV文件", MessageType.Warning);
        }

        DisplayResults();
    }

    private static void DrawHeader()
    {
        // 标题区域
        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        var titleStyle = new GUIStyle(EditorStyles.largeLabel)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        EditorGUILayout.LabelField(new GUIContent("CSV文件读取工具"),
            titleStyle, GUILayout.Height(30));

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // 副标题
        var subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.6f, 0.6f, 0.6f, 1f) }
        };

        EditorGUILayout.LabelField("读取多语言csv文件获取key生成枚举", subtitleStyle);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(10);
    }

    private void ReadCSVFirstColumn()
    {
        m_csvRowDatas.Clear();
        try
        {
            string[] lines;

            // 优先使用拖拽的TextAsset
            if (m_csvTextAsset != null)
            {
                lines = m_csvTextAsset.text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
                Debug.Log($"从TextAsset读取: {m_csvTextAsset.name}");
            }
            // 否则使用文件路径
            else if (File.Exists(m_csvFilePath))
            {
                lines = File.ReadAllLines(m_csvFilePath, Encoding.UTF8);
                Debug.Log($"从文件路径读取: {m_csvFilePath}");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "无法读取CSV文件！", "确定");
                return;
            }

            int startIndex = m_hasHeader ? 1 : 0;
            int count = 0;

            for (int i = startIndex; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                string[] columns = ParseCSVLine(lines[i]);

                if (columns.Length > 0)
                {
                    string key = columns[0].Trim();
                    string type = columns[1].Trim();
                    string description = columns[2].Trim(); // 第三列作为描述
                    if (string.IsNullOrEmpty(description))
                    {
                        description = columns[3].Trim(); // 第四列作为描述
                    }

                    if (!type.Equals(m_filterStr, StringComparison.Ordinal))
                    {
                        continue;
                    }
                    if (!string.IsNullOrEmpty(key))
                    {
                        if (m_csvRowDatas.ContainsKey(key))
                        {
                            Debug.LogError($"存在相同Key值: '{key}' 请检查表格第 {i + 1} 行数据");
                        }
                        CSVRowData tempData = new CSVRowData()
                        {
                            Key = key,
                            Description = description
                        };
                        m_csvRowDatas[key] = tempData;
                        count++;
                    }
                }
            }

            Debug.Log($"成功读取 {m_csvRowDatas.Count} 个Key, 总共存在 {count} 个key, 相同key存在 {count - m_csvRowDatas.Count} 个");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("读取错误", $"读取CSV文件时出错:\n{e.Message}", "确定");
            Debug.LogError($"读取CSV错误: {e}");
        }
    }

    private void DisplayResults()
    {
        if (m_csvRowDatas.Count == 0)
            return;

        EditorGUILayout.Space(5);
        GUILayout.Label($"找到 {m_csvRowDatas.Count} 个Key:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("序号", EditorStyles.boldLabel, GUILayout.Width(30));
        GUILayout.Label("Key", EditorStyles.boldLabel);
        GUILayout.Label("Description", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        // 使用垂直布局来控制整体结构
        EditorGUILayout.BeginVertical();

        // 可滚动的列表区域 - 使用FlexibleSpace来占据剩余空间
        m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition, GUILayout.ExpandHeight(true));

        int index = 1;
        // int startEnumIndex = m_enumStartIndex;
        foreach (var item in m_csvRowDatas.Values)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{index++}.", GUILayout.Width(30));
            EditorGUILayout.TextField(item.Key);
            EditorGUILayout.TextField(item.Description);

            if (GUILayout.Button("复制", GUILayout.Width(50)))
            {
                string str = $"{item.Key}, // {item.Description}"; //  = {startEnumIndex++}
                GUIUtility.systemCopyBuffer = str;
                Debug.Log($"已复制: {str}");
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // 底部固定按钮区域
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("导出到文本文件"))
        {
            ExportKeysToFile();
        }

        if (GUILayout.Button("复制所有Key"))
        {
            CopyKeysToClipboard();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    // 其他辅助方法保持不变
    private string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        StringBuilder current = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }

    private void ExportKeysToFile()
    {
        string exportPath = m_csFileExportPath + "/" + m_csFileName + ".cs";
        if (!string.IsNullOrEmpty(exportPath))
        {
            try
            {
                if (m_csvRowDatas.Count == 0)
                {
                    EditorUtility.DisplayDialog("提示", "没有找到符合条件的行（第二列Type为Text）", "确定");
                    return;
                }

                // 生成枚举类内容
                string enumContent = GenerateEnumClass(m_csvRowDatas.Values.ToList());

                using (StreamWriter writer = new StreamWriter(exportPath, false, Encoding.UTF8))
                {
                    writer.Write(enumContent);
                }

                // 刷新Unity资源数据库（如果在Assets目录内）
                if (exportPath.StartsWith(Application.dataPath))
                {
                    string relativePath = "Assets" + exportPath.Substring(Application.dataPath.Length);
                    AssetDatabase.Refresh();
                    EditorUtility.DisplayDialog("导出成功", $"枚举类已导出到:\n{relativePath}\n\n已自动刷新Unity资源", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("导出成功", $"枚举类已导出到:\n{exportPath}", "确定");
                }

                Debug.Log($"枚举类导出到: {m_csFileExportPath}");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("导出错误", $"导出失败:\n{e.Message}", "确定");
                Debug.LogError(e);
            }
        }
    }

    /// <summary>
    /// 生成枚举类代码
    /// </summary>
    private string GenerateEnumClass(List<CSVRowData> rows)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// 此文件由CSV Reader工具自动生成");
        sb.AppendLine("// 生成时间: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("// 源CSV文件: " + (m_csvTextAsset != null ? m_csvTextAsset.name : Path.GetFileName(m_csvFilePath)));
        sb.AppendLine();

        // 添加命名空间
        sb.AppendLine("namespace GameLogic");
        sb.AppendLine("{");
        sb.AppendLine("\t/// <summary>");
        sb.AppendLine("\t/// 文本枚举");
        sb.AppendLine("\t/// </summary>");
        sb.AppendLine("\tpublic enum " + m_csFileName);
        sb.AppendLine("\t{");

        // 生成枚举项，从20000000开始
        // int currentValue = m_enumStartIndex;

        for (int i = 0; i < rows.Count; i++)
        {
            CSVRowData row = rows[i];
            string enumName = CleanEnumName(row.Key);
            sb.AppendLine($"\t\t{enumName}, // {row.Description}"); //  = {currentValue++}
        }

        sb.AppendLine("\t}");
        sb.AppendLine("}");

        return sb.ToString();
    }

    // 清理字符串，使其成为有效的C#标识符
    private string CleanEnumName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Unknown";

        // 移除所有非法字符
        System.Text.RegularExpressions.Regex regex =
            new System.Text.RegularExpressions.Regex(@"[^a-zA-Z0-9_]");
        string cleaned = regex.Replace(name, "_");

        // 确保不以数字开头
        if (char.IsDigit(cleaned[0]))
        {
            cleaned = "_" + cleaned;
        }

        // 移除连续的下划线
        while (cleaned.Contains("__"))
        {
            cleaned = cleaned.Replace("__", "_");
        }

        // 移除开头和结尾的下划线
        cleaned = cleaned.Trim('_');

        // 如果清理后为空，使用默认值
        if (string.IsNullOrEmpty(cleaned))
        {
            cleaned = "Unknown";
        }

        return cleaned;
    }

    private void CopyKeysToClipboard()
    {
        StringBuilder sb = new StringBuilder();
        // int startEnumIndex = m_enumStartIndex;
        foreach (var item in m_csvRowDatas.Values)
        {
            string key = $"{item.Key}, // {item.Description}"; // = {startEnumIndex++}
            sb.AppendLine(key);
        }

        GUIUtility.systemCopyBuffer = sb.ToString();
        EditorUtility.DisplayDialog("复制成功", "所有Key已复制到剪贴板", "确定");
    }

    // 根据文件路径在项目中查找TextAsset
    private TextAsset FindTextAssetInProject(string filePath)
    {
        // 如果文件在Assets目录内
        if (filePath.StartsWith(Application.dataPath))
        {
            string relativePath = "Assets" + filePath.Substring(Application.dataPath.Length);
            return AssetDatabase.LoadAssetAtPath<TextAsset>(relativePath);
        }

        return null;
    }

    // 临时数据结构
    private class CSVRowData
    {
        public string Key;
        public string Description;
    }
}