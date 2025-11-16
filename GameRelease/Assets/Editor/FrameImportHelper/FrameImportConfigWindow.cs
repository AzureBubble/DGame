using UnityEditor;
using UnityEngine;

namespace DGame
{
    public class FrameImportConfigWindow : EditorWindow
    {
        [MenuItem("DGame Tools/GenerateTools/序列帧资源导入配置窗口")]
        public static void ShowWindow()
        {
            var window = GetWindow<FrameImportConfigWindow>();
            window.titleContent = new GUIContent(" 序列帧资源导入配置窗口", EditorGUIUtility.IconContent("Settings").image);
            window.minSize = new Vector2(450, 400);
        }

        private Vector2 m_scrollPos = Vector2.zero;
        private SerializedObject serializedObject;

        private void OnGUI()
        {
            var config = FrameImportConfig.Instance;
            serializedObject = new SerializedObject(config);

            using var scrollScope = new EditorGUILayout.ScrollViewScope(m_scrollPos);
            m_scrollPos = scrollScope.scrollPosition;
            EditorGUI.BeginChangeCheck();

            DrawFolderSettings(config);
            DrawFrameSpriteExtensionName(config);
            DrawFrameAnimNames(config);

            if (EditorGUI.EndChangeCheck())
            {
                FrameImportConfig.Save();
                AssetDatabase.Refresh();
            }
        }

        private void DrawFrameSpriteExtensionName(FrameImportConfig config)
        {
            config.frameSpriteExtensionName =
                (FrameSpriteExtensionName)EditorGUILayout.EnumPopup(
                    new GUIContent("资源后缀", EditorGUIUtility.IconContent("Sprite Icon").image, "选择精灵图片的扩展名格式"),
                    config.frameSpriteExtensionName);
        }

        private void DrawFrameAnimNames(FrameImportConfig config)
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("frameAnimNames"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("restPivotOrders"));
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawFolderSettings(FrameImportConfig config)
        {
            var labelGUIContent = new GUIContent(" 目录设置", EditorGUIUtility.IconContent("Folder Icon").image);
            GUILayout.Label(labelGUIContent, EditorStyles.boldLabel, GUILayout.ExpandWidth(true), GUILayout.Height(20));
            config.ImportFrameRootDir = DrawFolderField("序列帧导入根目录", "FolderOpened Icon", config.ImportFrameRootDir);
            config.frameConfigGenerateDir = DrawFolderField("配置文件生成路径", "FolderOpened Icon", config.frameConfigGenerateDir);
        }

        private string DrawFolderField(string label, string labelIcon, string path)
        {
            using var horizontalScope = new EditorGUILayout.HorizontalScope();
            var buttonGUIContent = new GUIContent("选择", EditorGUIUtility.IconContent("Folder Icon").image);
            var labelGUIContent = new GUIContent(" " + label, EditorGUIUtility.IconContent(labelIcon).image);
            path = EditorGUILayout.TextField(labelGUIContent, path);

            if (GUILayout.Button(buttonGUIContent, GUILayout.Width(60), GUILayout.Height(20)))
            {
                var newPath = EditorUtility.OpenFolderPanel(label, Application.dataPath, string.Empty);

                if (!string.IsNullOrEmpty(newPath) && newPath.StartsWith(Application.dataPath))
                {
                    path = "Assets" + newPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    Debug.LogError("路径不在Unity项目内: " + newPath);
                }
            }
            return path;
        }
    }
}