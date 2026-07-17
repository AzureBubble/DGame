using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace DGame
{
    class AtlasRefWindow : EditorWindow
    {
        /// <summary>
        /// UI Prefab 中 Image 组件引用 Sprite 的具体位置
        /// </summary>
        public class SpriteRefInfo
        {
            public GameObject Prefab;
            public Sprite Sprite;
            public SpriteAtlas Atlas;
            public string HierarchyPath;

            public SpriteRefInfo(Sprite sprite, SpriteAtlas atlas, GameObject prefab, string hierarchyPath)
            {
                Sprite = sprite;
                Prefab = prefab;
                Atlas = atlas;
                HierarchyPath = hierarchyPath;
            }
        }

        /// <summary>
        /// 引用了指定 Sprite 的 UI Prefab 数据
        /// </summary>
        public class SpriteRefData
        {
            public Sprite Sprite;
            public SpriteAtlas Atlas;
            public Dictionary<GameObject, SpriteRefPrefabInfo> PrefabInfoList = new Dictionary<GameObject, SpriteRefPrefabInfo>();

            public SpriteRefData(Sprite sprite, SpriteAtlas atlas)
            {
                Sprite = sprite;
                Atlas = atlas;
            }
        }

        /// <summary>
        /// UI Prefab 引用指定 Sprite 的位置集合
        /// </summary>
        public class SpriteRefPrefabInfo
        {
            public Sprite Sprite;
            public GameObject Prefab;
            public List<SpriteRefInfo> RefList = new List<SpriteRefInfo>();

            public SpriteRefPrefabInfo(Sprite sprite, GameObject prefab)
            {
                Sprite = sprite;
                Prefab = prefab;
            }
        }

        /// <summary>
        /// UI Prefab 使用的 SpriteAtlas 数据
        /// </summary>
        public class PrefabRefData
        {
            public GameObject Prefab;
            public Dictionary<string, PrefabRefAtlasInfo> AtlasInfoList = new Dictionary<string, PrefabRefAtlasInfo>();

            public PrefabRefData(GameObject prefab)
            {
                Prefab = prefab;
            }
        }

        /// <summary>
        /// UI Prefab 使用指定 SpriteAtlas 的引用位置集合
        /// </summary>
        public class PrefabRefAtlasInfo
        {
            public GameObject Prefab;
            public SpriteAtlas Atlas;
            public List<SpriteRefInfo> RefList = new List<SpriteRefInfo>();

            public PrefabRefAtlasInfo(GameObject prefab, SpriteAtlas atlas)
            {
                Prefab = prefab;
                Atlas = atlas;
            }
        }

        /// <summary>
        /// Sprite 目录对应的 SpriteAtlas、Sprite 和引用 Prefab 数据
        /// </summary>
        public class AtlasRefData
        {
            public string SpriteFolderPath;
            public SpriteAtlas Atlas;
            public List<SpriteRefData> SpriteList = new List<SpriteRefData>();
            public Dictionary<GameObject, PrefabRefAtlasInfo> PrefabInfoList = new Dictionary<GameObject, PrefabRefAtlasInfo>();

            public AtlasRefData(string spriteFolderPath, SpriteAtlas atlas)
            {
                SpriteFolderPath = spriteFolderPath;
                Atlas = atlas;
            }
        }

        class RefStackData
        {
            public ShowType ShowType;
            public object Data;
            public Vector2 LastScrollPos;

            public RefStackData(ShowType showType, object data, Vector2 lastScrollPos)
            {
                ShowType = showType;
                Data = data;
                LastScrollPos = lastScrollPos;
            }
        }

        enum ShowType
        {
            Sprite,
            SpritePrefab,
            SpritePrefabReason,

            Prefab,
            PrefabAtlas,
            PrefabAtlasReason,

            Atlas,
            AtlasSprite,
            AtlasPrefab,
            AtlasPrefabReason,

            SceneGameObject,
        }

        private List<RefStackData> m_stackData = new List<RefStackData>();
        private object m_lastSelect;

        private string m_searchSpriteName;
        private string m_searchPrefabName;
        private string m_searchAtlasName;

        private Vector2 m_scrollPosition;

        private static readonly string[] INSPECT_TOOLBAR_LABELS =
            { "查看Sprite引用", "查看Prefab引用", "查看Atlas引用", "查看选中场景物体引用" };

        enum InspectType
        {
            Sprite,
            Prefab,
            Atlas,
            SceneGameObject,
        }
        private InspectType m_activeInspectType;

        private static readonly List<SpriteRefData> s_spriteRefDataList = new List<SpriteRefData>();
        private static readonly Dictionary<Sprite, SpriteRefData> s_spriteRefDataBySprite = new Dictionary<Sprite, SpriteRefData>();

        private static readonly List<PrefabRefData> s_prefabRefDataList = new List<PrefabRefData>();
        private static readonly Dictionary<GameObject, PrefabRefData> s_prefabRefDataByPrefab = new Dictionary<GameObject, PrefabRefData>();

        private static readonly List<AtlasRefData> s_atlasRefDataList = new List<AtlasRefData>();
        private static readonly Dictionary<string, AtlasRefData> s_atlasRefDataBySpriteFolder = new Dictionary<string, AtlasRefData>();

        private PrefabRefData m_selectedObjectReference;

        private const string DEFAULT_UI_PREFAB_FOLDER_PATH = "Assets/BundleAssets/UI";
        private static string AtlasExtension => AtlasConfig.Instance.enableV2 ? ".spriteatlasv2" : ".spriteatlas";
        private static string AtlasFolderPath => NormalizeAssetFolderPath(AtlasConfig.Instance.outputAtlasDir, string.Empty);
        private string UIPrefabPath => GetFullAssetFolderPath(AtlasRefWindowSettings.Instance?.UIPrefabFolderPath ?? DEFAULT_UI_PREFAB_FOLDER_PATH, DEFAULT_UI_PREFAB_FOLDER_PATH);

        private static Texture2D s_selectBackground;
        private GUIStyle m_selectStyle;
        private GUIStyle m_panelStyle;
        private GUIStyle m_titleStyle;
        private GUIStyle m_subtitleStyle;
        private GUIStyle m_tableHeaderStyle;
        private GUIStyle m_primaryButtonStyle;
        private GUIStyle m_metricValueStyle;
        private GUIStyle m_metricLabelStyle;
        private static readonly StringBuilder s_hierarchyPathBuilder = new StringBuilder();
        private bool m_showSettings;
        private Vector2 m_settingsScrollPos;

        [UnityEditor.FilePath("ProjectSettings/AtlasRefWindowSettings.asset", UnityEditor.FilePathAttribute.Location.ProjectFolder)]
        private class AtlasRefWindowSettings : UnityEditor.ScriptableSingleton<AtlasRefWindowSettings>
        {
            [SerializeField]
            private string m_uiPrefabFolderPath = DEFAULT_UI_PREFAB_FOLDER_PATH;

            public static AtlasRefWindowSettings Instance => instance;

            public string UIPrefabFolderPath => NormalizeAssetFolderPath(m_uiPrefabFolderPath, DEFAULT_UI_PREFAB_FOLDER_PATH);

            public void SetUIPrefabFolderPath(string path)
            {
                m_uiPrefabFolderPath = NormalizeAssetFolderPath(path, DEFAULT_UI_PREFAB_FOLDER_PATH);
                Save(true);
            }

            public void ResetToDefault()
            {
                m_uiPrefabFolderPath = DEFAULT_UI_PREFAB_FOLDER_PATH;
                Save(true);
            }
        }

        [MenuItem("DGame Tools/性能分析工具/图集引用分析工具")]
        static void OpenWindow()
        {
            AtlasRefWindow window = GetWindow<AtlasRefWindow>();
            window.Init();
            window.titleContent = new GUIContent("Atlas 引用分析",
                EditorGUIUtility.IconContent("SpriteAtlas Icon").image);
            window.minSize = new Vector2(1200, 600);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Atlas 引用分析",
                EditorGUIUtility.IconContent("SpriteAtlas Icon").image);
            minSize = new Vector2(1200, 600);
        }

        private void Init()
        {
            m_stackData.Clear();
            PushStackData(new RefStackData(ShowType.Sprite, null, Vector2.zero));
            m_activeInspectType = InspectType.Sprite;
        }

        private static string NormalizeAssetFolderPath(string path, string defaultPath)
        {
            if (string.IsNullOrEmpty(path))
            {
                return defaultPath;
            }

            path = path.Replace("\\", "/").TrimEnd('/');
            var dataPath = Application.dataPath.Replace("\\", "/");
            if (path.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                path = "Assets" + path.Substring(dataPath.Length);
            }

            if (path != "Assets" && !path.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return defaultPath;
            }

            return path;
        }

        private static string GetFullAssetFolderPath(string assetFolderPath, string defaultPath)
        {
            assetFolderPath = NormalizeAssetFolderPath(assetFolderPath, defaultPath);
            var projectPath = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectPath))
            {
                return Application.dataPath;
            }

            return Path.GetFullPath(Path.Combine(projectPath, assetFolderPath)).Replace("\\", "/");
        }

        private static bool IsValidAssetFolder(string assetFolderPath)
        {
            return !string.IsNullOrEmpty(assetFolderPath) && AssetDatabase.IsValidFolder(assetFolderPath);
        }

        private bool ValidateAnalysisSettings()
        {
            var settings = AtlasRefWindowSettings.Instance;
            if (settings == null)
            {
                Debug.LogError("AtlasRefWindow设置加载失败");
                return false;
            }

            if (!IsValidAssetFolder(settings.UIPrefabFolderPath))
            {
                Debug.LogError($"AtlasRefWindow UI Prefab目录无效：{settings.UIPrefabFolderPath}");
                return false;
            }

            if (!Directory.Exists(UIPrefabPath))
            {
                Debug.LogError($"AtlasRefWindow UI Prefab目录不存在：{UIPrefabPath}");
                return false;
            }

            if (!IsValidAssetFolder(AtlasFolderPath))
            {
                Debug.LogError($"AtlasRefWindow 图集目录无效：{AtlasFolderPath}");
                return false;
            }

            return true;
        }

        private void AnalyzeReferences()
        {
            if (!ValidateAnalysisSettings())
            {
                m_showSettings = true;
                return;
            }

            s_spriteRefDataList.Clear();
            s_spriteRefDataBySprite.Clear();
            s_prefabRefDataList.Clear();
            s_prefabRefDataByPrefab.Clear();
            s_atlasRefDataList.Clear();
            s_atlasRefDataBySpriteFolder.Clear();

            string prefabPath = UIPrefabPath;
            DirectoryInfo dicInfo = new DirectoryInfo(prefabPath);
            FileInfo[] fileInfos = dicInfo.GetFiles("*.prefab", SearchOption.AllDirectories);
            int count = fileInfos.Length;
            for (var index = 0; index < count; index++)
            {
                var file = fileInfos[index];
                var cancel = EditorUtility.DisplayCancelableProgressBar("分析图集", $"分析预制体：{index}/{count}", (float)index / count);
                if (cancel)
                {
                    break;
                }
                string path = file.FullName;
                string assePath = path.Substring(path.IndexOf("Assets", StringComparison.Ordinal));
                GameObject prefab = AssetDatabase.LoadAssetAtPath(assePath, typeof(GameObject)) as GameObject;
                if (prefab != null)
                {
                    var arrImage = prefab.GetComponentsInChildren<Image>(true);
                    for (int i = 0; i < arrImage.Length; i++)
                    {
                        var image = arrImage[i];
                        if (image != null && image.sprite != null)
                        {
                            var sprite = image.sprite;
                            var assetPath = AssetDatabase.GetAssetPath(sprite);
                            var spriteFolderPath = Path.GetDirectoryName(assetPath);
                            var atlas = ResolveSpriteAtlas(sprite);

                            var hierarchyPath = BuildHierarchyPath(image.transform, prefab.transform);

                            // Sprite 数据
                            bool isNewSprite = false;
                            var spriteRefInfo = new SpriteRefInfo(sprite, atlas, prefab, hierarchyPath);
                            if (!s_spriteRefDataBySprite.TryGetValue(sprite, out var spriteRefData))
                            {
                                isNewSprite = true;
                                spriteRefData = new SpriteRefData(sprite, atlas);
                                s_spriteRefDataBySprite.Add(sprite, spriteRefData);
                                s_spriteRefDataList.Add(spriteRefData);
                            }
                            if (!spriteRefData.PrefabInfoList.TryGetValue(prefab, out var spriteRefPrefabInfo))
                            {
                                spriteRefPrefabInfo = new SpriteRefPrefabInfo(sprite, prefab);
                                spriteRefData.PrefabInfoList.Add(prefab, spriteRefPrefabInfo);
                            }
                            spriteRefPrefabInfo.RefList.Add(spriteRefInfo);

                            // Prefab 数据
                            if (!s_prefabRefDataByPrefab.TryGetValue(prefab, out var prefabRefData))
                            {
                                prefabRefData = new PrefabRefData(prefab);
                                s_prefabRefDataByPrefab.Add(prefab, prefabRefData);
                                s_prefabRefDataList.Add(prefabRefData);
                            }
                            if (!prefabRefData.AtlasInfoList.TryGetValue(spriteFolderPath, out var atlasRefInfo))
                            {
                                atlasRefInfo = new PrefabRefAtlasInfo(prefab, atlas);
                                prefabRefData.AtlasInfoList.Add(spriteFolderPath, atlasRefInfo);
                            }
                            atlasRefInfo.RefList.Add(spriteRefInfo);

                            // Atlas 数据
                            if (!s_atlasRefDataBySpriteFolder.TryGetValue(spriteFolderPath, out var atlasRefData))
                            {
                                atlasRefData = new AtlasRefData(spriteFolderPath, atlas);
                                s_atlasRefDataBySpriteFolder.Add(spriteFolderPath, atlasRefData);
                                s_atlasRefDataList.Add(atlasRefData);
                            }
                            if (isNewSprite)
                            {
                                atlasRefData.SpriteList.Add(spriteRefData);
                            }
                            atlasRefData.PrefabInfoList.TryAdd(prefab, atlasRefInfo);
                        }
                    }
                }
            }

            EditorUtility.ClearProgressBar();

            // 补充已发现 Sprite 目录中未被 UI Prefab 引用的顶层 PNG Sprite。
            int atlasCount = s_atlasRefDataList.Count;
            for (var atlasIndex = 0; atlasIndex < atlasCount; atlasIndex++)
            {
                var atlasRefData = s_atlasRefDataList[atlasIndex];
                var cancel = EditorUtility.DisplayCancelableProgressBar("分析图集", $"分析图集：{atlasIndex}/{atlasCount}", (float)atlasIndex / atlasCount);
                if (cancel)
                {
                    break;
                }
                var spriteFolderPath = atlasRefData.SpriteFolderPath;
                DirectoryInfo atlasDirInfo = new DirectoryInfo(spriteFolderPath);
                if (!atlasDirInfo.Exists)
                {
                    continue;
                }
                FileInfo[] spriteFileInfos = atlasDirInfo.GetFiles("*.png", SearchOption.TopDirectoryOnly);
                foreach (var spriteFileInfo in spriteFileInfos)
                {
                    string path = spriteFileInfo.FullName;
                    string assePath = path.Substring(path.IndexOf("Assets", StringComparison.Ordinal));
                    Sprite sprite = AssetDatabase.LoadAssetAtPath(assePath, typeof(Sprite)) as Sprite;
                    if (sprite != null && !s_spriteRefDataBySprite.ContainsKey(sprite))
                    {
                        var spriteRefData = new SpriteRefData(sprite, atlasRefData.Atlas);
                        s_spriteRefDataBySprite.Add(sprite, spriteRefData);
                        s_spriteRefDataList.Add(spriteRefData);
                        atlasRefData.SpriteList.Add(spriteRefData);
                    }
                }
                atlasRefData.SpriteList.Sort((x, y) =>
                {
                    var xSize = x.Sprite.texture.width * x.Sprite.texture.height;
                    var ySize = y.Sprite.texture.width * y.Sprite.texture.height;
                    return ySize - xSize;
                });
            }

            EditorUtility.ClearProgressBar();

            s_spriteRefDataList.Sort((x, y) => -x.PrefabInfoList.Count.CompareTo(y.PrefabInfoList.Count));
            s_prefabRefDataList.Sort((x, y) => -x.AtlasInfoList.Count.CompareTo(y.AtlasInfoList.Count));
            s_atlasRefDataList.Sort((x, y) => -x.PrefabInfoList.Count.CompareTo(y.PrefabInfoList.Count));

            Debug.Log("AtlasRefWindow 引用分析完成");
        }

        private void AnalyzeSelectedSceneObject()
        {
            m_selectedObjectReference = null;

            var prefab = Selection.activeGameObject;
            if (prefab != null)
            {
                m_selectedObjectReference = new PrefabRefData(prefab);

                var arrImage = prefab.GetComponentsInChildren<Image>(true);
                for (int i = 0; i < arrImage.Length; i++)
                {
                    var image = arrImage[i];
                    if (image != null && image.sprite != null)
                    {
                        var sprite = image.sprite;
                        var assetPath = AssetDatabase.GetAssetPath(sprite);
                        var spriteFolderPath = Path.GetDirectoryName(assetPath);
                        var atlas = ResolveSpriteAtlas(sprite);

                        var hierarchyPath = BuildHierarchyPath(image.transform, prefab.transform);

                        var spriteRefInfo = new SpriteRefInfo(sprite, atlas, prefab, hierarchyPath);
                        if (!m_selectedObjectReference.AtlasInfoList.TryGetValue(spriteFolderPath, out var atlasRefInfo))
                        {
                            atlasRefInfo = new PrefabRefAtlasInfo(prefab, atlas);
                            m_selectedObjectReference.AtlasInfoList.Add(spriteFolderPath, atlasRefInfo);
                        }
                        atlasRefInfo.RefList.Add(spriteRefInfo);
                    }
                }
            }
        }

        private static SpriteAtlas ResolveSpriteAtlas(Sprite sprite)
        {
            var assetPath = AssetDatabase.GetAssetPath(sprite);
            string atlasName = EditorSpriteSaveInfo.ResolveAtlasName(assetPath);
            var path = $"{AtlasFolderPath}/{atlasName}{AtlasExtension}";
            return AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
        }
        private static string BuildHierarchyPath(Transform target, Transform root = null)
        {
            s_hierarchyPathBuilder.Clear();
            s_hierarchyPathBuilder.Append(target.name);

            var transform = target.parent;
            while (transform != root && transform != null)
            {
                s_hierarchyPathBuilder.Insert(0, transform.name + "/");
                transform = transform.parent;
            }

            return s_hierarchyPathBuilder.ToString();
        }

        private static Texture2D CreateSolidTexture(int r, int g, int b, int a)
        {
            Color color = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
            Texture2D tex = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
            };
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private void EnsureStyles()
        {
            if (s_selectBackground == null)
            {
                s_selectBackground = CreateSolidTexture(52, 120, 210, EditorGUIUtility.isProSkin ? 72 : 45);
            }

            m_selectStyle ??= new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(6, 6, 3, 3),
                margin = new RectOffset(0, 0, 1, 1),
            };
            m_selectStyle.normal.background = s_selectBackground;

            m_panelStyle ??= new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(6, 6, 6, 6),
            };
            m_titleStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 17,
            };
            m_subtitleStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
                normal = { textColor = EditorGUIUtility.isProSkin
                    ? new Color(0.72f, 0.72f, 0.72f)
                    : new Color(0.35f, 0.35f, 0.35f) },
            };
            m_tableHeaderStyle ??= new GUIStyle(EditorStyles.toolbar)
            {
                fixedHeight = 24f,
                padding = new RectOffset(6, 6, 3, 3),
                fontStyle = FontStyle.Bold,
            };
            m_primaryButtonStyle ??= new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fixedHeight = 28f,
            };
            m_metricValueStyle ??= new GUIStyle(EditorStyles.largeLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = EditorGUIUtility.isProSkin
                    ? new Color(0.42f, 0.72f, 1f)
                    : new Color(0.08f, 0.4f, 0.75f) },
            };
            m_metricLabelStyle ??= new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
            };
        }
        private static void DrawObjectField(UnityEngine.Object obj, Type objType, bool allowSceneObjects, params GUILayoutOption[] options)
        {
            var rect = EditorGUILayout.GetControlRect(false, 18f, options);
            EditorGUI.ObjectField(rect, obj, objType, allowSceneObjects);

            EventType eventType = Event.current.type;
            // ObjectField 会消费鼠标按下事件，因此这里通过 Used 判断对象字段是否被点击。
            if (eventType == EventType.Used)
            {
                if (rect.Contains(Event.current.mousePosition))
                {
                    Selection.activeObject = obj;
                }
            }
        }

        private static string FormatSpriteSize(Sprite sprite)
        {
            return sprite.texture.width + " x " + sprite.texture.height;
        }

        private RefStackData PopStackData()
        {
            var data = m_stackData[m_stackData.Count - 1];
            m_stackData.RemoveAt(m_stackData.Count - 1);

            m_lastSelect = data.Data;
            m_scrollPosition = data.LastScrollPos;
            return data;
        }

        private RefStackData PeekStackData()
        {
            return m_stackData[m_stackData.Count - 1];
        }

        private void PushStackData(RefStackData data)
        {
            m_stackData.Add(data);
            m_scrollPosition = Vector2.zero;
        }

        private void ShowMainToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(26f));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(m_showSettings ? "返回结果" : "设置", EditorStyles.toolbarButton,
                    GUILayout.Width(80f), GUILayout.Height(22f)))
            {
                m_showSettings = !m_showSettings;
            }
            GUILayout.EndHorizontal();
        }
        private void ShowSettings()
        {
            var settings = AtlasRefWindowSettings.Instance;
            if (settings == null)
            {
                EditorGUILayout.HelpBox("AtlasRefWindow设置加载失败，请重新打开窗口后重试。", MessageType.Error);
                return;
            }

            m_settingsScrollPos = EditorGUILayout.BeginScrollView(m_settingsScrollPos);
            using (new EditorGUILayout.VerticalScope(m_panelStyle))
            {
                GUILayout.Label("Atlas 引用分析设置", m_titleStyle);
                GUILayout.Label("设置参与扫描的 UI Prefab 根目录；图集目录和命名规则统一读取图集配置。",
                    m_subtitleStyle);
            }

            using (new EditorGUILayout.VerticalScope(m_panelStyle))
            {
                GUILayout.Label("扫描目录", EditorStyles.boldLabel);
                EditorGUILayout.Space(3f);

                var uiPrefabFolderPath = DrawFolderField("UI Prefab 目录", settings.UIPrefabFolderPath,
                    DEFAULT_UI_PREFAB_FOLDER_PATH);
                if (uiPrefabFolderPath != settings.UIPrefabFolderPath)
                {
                    settings.SetUIPrefabFolderPath(uiPrefabFolderPath);
                }
            }

            using (new EditorGUILayout.VerticalScope(m_panelStyle))
            {
                GUILayout.Label("图集格式", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("图集输出目录", AtlasFolderPath);
                EditorGUILayout.LabelField("当前扩展名", AtlasExtension);
                EditorGUILayout.HelpBox(
                    "扩展名来源：图集配置窗口中的“启用V2打包”设置。启用时使用 .spriteatlasv2，未启用时使用 .spriteatlas。",
                    MessageType.Info);
                EditorGUILayout.HelpBox(
                    "图集资源目录与命名规则来自图集配置窗口中的收集目录、根目录子级图集和单张图集目录。",
                    MessageType.Info);
                if (GUILayout.Button("打开图集配置窗口", GUILayout.Width(140f)))
                {
                    AtlasConfigWindow.ShowWindow();
                }

                if (!IsValidAssetFolder(AtlasFolderPath))
                {
                    EditorGUILayout.HelpBox($"图集目录无效：{AtlasFolderPath}", MessageType.Error);
                }

                if (!IsValidAssetFolder(settings.UIPrefabFolderPath))
                {
                    EditorGUILayout.HelpBox($"UI Prefab 目录无效：{settings.UIPrefabFolderPath}", MessageType.Error);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("恢复默认", GUILayout.Width(120f), GUILayout.Height(28f)))
                {
                    settings.ResetToDefault();
                }

                if (GUILayout.Button("返回分析结果", m_primaryButtonStyle, GUILayout.Width(140f)))
                {
                    m_showSettings = false;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private string DrawFolderField(string label, string folderPath, string defaultPath)
        {
            var folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);
            EditorGUI.BeginChangeCheck();
            var newFolderAsset = EditorGUILayout.ObjectField(label, folderAsset, typeof(DefaultAsset), false) as DefaultAsset;
            var result = folderPath;
            if (EditorGUI.EndChangeCheck())
            {
                if (newFolderAsset == null)
                {
                    result = defaultPath;
                }
                else
                {
                    var newPath = AssetDatabase.GetAssetPath(newFolderAsset);
                    if (IsValidAssetFolder(newPath))
                    {
                        result = NormalizeAssetFolderPath(newPath, defaultPath);
                    }
                    else
                    {
                        Debug.LogWarning($"AtlasRefWindow 只支持拖入Assets下的文件夹：{newPath}");
                    }
                }
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("路径", result);
            EditorGUI.EndDisabledGroup();
            return result;
        }

        void OnGUI()
        {
            EnsureStyles();

            if (m_stackData.Count == 0)
            {
                Init();
            }

            ShowMainToolbar();
            if (m_showSettings)
            {
                ShowSettings();
                return;
            }

            ShowSummaryHeader();
            if (m_stackData.Count > 1)
            {
                using (new EditorGUILayout.HorizontalScope(m_panelStyle))
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"引用路径 · {m_stackData.Count - 1} 层",
                        EditorStyles.centeredGreyMiniLabel, GUILayout.Width(110f), GUILayout.Height(28f));
                    GUILayout.Space(8f);

                    Color oldBackgroundColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.35f, 0.65f, 1f);
                    if (GUILayout.Button("←  返回上一级", m_primaryButtonStyle, GUILayout.Width(135f)))
                    {
                        PopStackData();
                    }
                    GUI.backgroundColor = oldBackgroundColor;
                }

                foreach (var data in m_stackData)
                {
                    switch (data.ShowType)
                    {
                        case ShowType.SpritePrefab:
                            var spriteData = data.Data as SpriteRefData;
                            if (spriteData != null)
                            {
                                ShowSpriteTitle();
                                GUILayout.BeginHorizontal(m_selectStyle);
                                ShowSpriteItem(spriteData);
                                GUILayout.EndHorizontal();
                            }
                            break;
                        case ShowType.SpritePrefabReason:
                            var spritePrefabData = data.Data as SpriteRefPrefabInfo;
                            if (spritePrefabData != null)
                            {
                                ShowSpritePrefabTitle();
                                GUILayout.BeginHorizontal(m_selectStyle);
                                ShowSpritePrefabItem(spritePrefabData);
                                GUILayout.EndHorizontal();
                            }
                            break;
                        case ShowType.PrefabAtlas:
                            var prefabData = data.Data as PrefabRefData;
                            if (prefabData != null)
                            {
                                ShowPrefabTitle();
                                GUILayout.BeginHorizontal(m_selectStyle);
                                ShowPrefabItem(prefabData);
                                GUILayout.EndHorizontal();
                            }
                            break;
                        case ShowType.PrefabAtlasReason:
                            var prefabAtlasData = data.Data as PrefabRefAtlasInfo;
                            if (prefabAtlasData != null)
                            {
                                ShowPrefabAtlasTitle();
                                GUILayout.BeginHorizontal(m_selectStyle);
                                ShowPrefabAtlasItem(prefabAtlasData);
                                GUILayout.EndHorizontal();
                            }
                            break;
                        case ShowType.AtlasSprite:
                        case ShowType.AtlasPrefab:
                            var atlasData = data.Data as AtlasRefData;
                            if (atlasData != null)
                            {
                                ShowAtlasTitle();
                                GUILayout.BeginHorizontal(m_selectStyle);
                                ShowAtlasItem(atlasData);
                                GUILayout.EndHorizontal();
                            }
                            break;
                        case ShowType.AtlasPrefabReason:
                            var atlasPrefabData = data.Data as PrefabRefAtlasInfo;
                            if (atlasPrefabData != null)
                            {
                                ShowAtlasPrefabTitle();
                                GUILayout.BeginHorizontal(m_selectStyle);
                                ShowAtlasPrefabItem(atlasPrefabData);
                                GUILayout.EndHorizontal();
                            }
                            break;
                    }
                }

                EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
                GUILayout.Space(6f);
            }

            var showType = PeekStackData().ShowType;
            switch (showType)
            {
                case ShowType.Sprite:
                    ShowSprite();
                    break;
                case ShowType.SpritePrefab:
                    ShowSpritePrefab();
                    break;
                case ShowType.SpritePrefabReason:
                    ShowSpritePrefabReason();
                    break;
                case ShowType.Prefab:
                    ShowPrefab();
                    break;
                case ShowType.PrefabAtlas:
                    ShowPrefabAtlas();
                    break;
                case ShowType.PrefabAtlasReason:
                case ShowType.AtlasPrefabReason:
                    ShowPrefabAtlasReason();
                    break;
                case ShowType.Atlas:
                    ShowAtlas();
                    break;
                case ShowType.AtlasSprite:
                    ShowAtlasSprite();
                    break;
                case ShowType.AtlasPrefab:
                    ShowAtlasPrefab();
                    break;
                case ShowType.SceneGameObject:
                    ShowSceneGameObject();
                    break;
            }
        }

        private void ShowSummaryHeader()
        {
            using (new EditorGUILayout.VerticalScope(m_panelStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("图集资源引用概览", m_titleStyle);
                    GUILayout.FlexibleSpace();
                    DrawMetric("Sprite", s_spriteRefDataList.Count);
                    DrawMetricSeparator();
                    DrawMetric("Prefab", s_prefabRefDataList.Count);
                    DrawMetricSeparator();
                    DrawMetric("Atlas", s_atlasRefDataList.Count);
                }

                GUILayout.Label("分析 UI Prefab、Sprite 与 SpriteAtlas 之间的引用关系，点击对象字段可在 Project 中定位资源。",
                    m_subtitleStyle);
            }
        }

        private void DrawMetric(string label, int value, float width = 72f)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(width)))
            {
                GUILayout.Label(value.ToString(), m_metricValueStyle, GUILayout.Height(22f));
                GUILayout.Label(label, m_metricLabelStyle, GUILayout.Height(16f));
            }
        }

        private static void DrawMetricSeparator()
        {
            Rect separatorRect = GUILayoutUtility.GetRect(1f, 34f, GUILayout.Width(1f), GUILayout.Height(34f));
            EditorGUI.DrawRect(separatorRect, EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.15f)
                : new Color(0f, 0f, 0f, 0.15f));
        }

        private void ShowTypeToolbar()
        {
            InspectType type;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(6f);
                type = (InspectType)GUILayout.Toolbar((int)m_activeInspectType, INSPECT_TOOLBAR_LABELS,
                    GUILayout.Height(30f));
                GUILayout.Space(6f);
            }

            if (type == m_activeInspectType)
            {
                return;
            }

            m_activeInspectType = type;
            switch (m_activeInspectType)
            {
                case InspectType.Sprite:
                    PopStackData();
                    PushStackData(new RefStackData(ShowType.Sprite, null, Vector2.zero));
                    break;
                case InspectType.Prefab:
                    PopStackData();
                    PushStackData(new RefStackData(ShowType.Prefab, null, Vector2.zero));
                    break;
                case InspectType.Atlas:
                    PopStackData();
                    PushStackData(new RefStackData(ShowType.Atlas, null, Vector2.zero));
                    break;
                case InspectType.SceneGameObject:
                    PopStackData();
                    PushStackData(new RefStackData(ShowType.SceneGameObject, null, Vector2.zero));
                    break;
            }
        }

        private void DrawAnalysisPanel(string title, string description, int resultCount, Action analysisAction,
            string buttonText = "重新分析")
        {
            using (new EditorGUILayout.VerticalScope(m_panelStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(title, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    DrawMetric("结果", resultCount, 64f);
                    GUILayout.Space(8f);
                    if (GUILayout.Button(new GUIContent(buttonText,
                            EditorGUIUtility.IconContent("Refresh").image), m_primaryButtonStyle,
                        GUILayout.Width(140f)))
                    {
                        analysisAction?.Invoke();
                    }
                }

                GUILayout.Label(description, m_subtitleStyle);
            }
        }

        private string DrawSearchToolbar(string searchText, string placeholder)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(EditorGUIUtility.IconContent("Search Icon"), GUILayout.Width(22f));
                GUIStyle searchStyle = GUI.skin.FindStyle("ToolbarSearchTextField")
                                       ?? EditorStyles.toolbarTextField;
                searchText = GUILayout.TextField(searchText ?? string.Empty, searchStyle,
                    GUILayout.ExpandWidth(true));
                if (string.IsNullOrEmpty(searchText))
                {
                    GUILayout.Label(placeholder, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(160f));
                }

                if (GUILayout.Button("清空", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                {
                    GUI.FocusControl(null);
                    searchText = string.Empty;
                }
            }

            return searchText;
        }

        private void DrawEmptyState(string message)
        {
            using (new EditorGUILayout.VerticalScope(m_panelStyle, GUILayout.MinHeight(90f)))
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(EditorGUIUtility.IconContent("console.infoicon"),
                    new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter },
                    GUILayout.Height(24f));
                GUILayout.Label(message, new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    fontSize = 12,
                    wordWrap = true,
                });
                GUILayout.FlexibleSpace();
            }
        }

        #region 查看Sprite引用

        #region Sprite引用数据

        private void ShowSprite()
        {
            ShowTypeToolbar();
            DrawAnalysisPanel("Sprite 引用", "查看每个 Sprite 所属图集及被 UI Prefab 引用的数量。",
                s_spriteRefDataList.Count, AnalyzeReferences, s_spriteRefDataList.Count > 0 ? "重新分析" : "开始分析");

            if (s_spriteRefDataList.Count == 0)
            {
                DrawEmptyState("暂无 Sprite 引用数据，点击“开始分析”扫描当前配置目录。");
                return;
            }

            m_searchSpriteName = DrawSearchToolbar(m_searchSpriteName, "按 Sprite 名称筛选");

            ShowSpriteTitle();
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
            for (var index = 0; index < s_spriteRefDataList.Count; index++)
            {
                var data = s_spriteRefDataList[index];
                if (!string.IsNullOrEmpty(m_searchSpriteName) && !data.Sprite.name.Contains(m_searchSpriteName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (data == m_lastSelect)
                {
                    GUILayout.BeginHorizontal(m_selectStyle);
                }
                else
                {
                    GUILayout.BeginHorizontal();
                }
                ShowSpriteItem(data);
                if (GUILayout.Button("查看引用原因", GUILayout.Width(150)))
                {
                    PushStackData(new RefStackData(ShowType.SpritePrefab, data, m_scrollPosition));
                }
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void ShowSpriteTitle()
        {
            GUILayout.BeginHorizontal(m_tableHeaderStyle);
            GUILayout.Label("Sprite", GUILayout.Width(200));
            GUILayout.Label("所属图集", GUILayout.Width(200));
            GUILayout.Label("引用Prefab数量", GUILayout.Width(100));
            GUILayout.Label("Size", GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }

        private void ShowSpriteItem(SpriteRefData data)
        {
            EditorGUI.BeginDisabledGroup(true);
            DrawObjectField(data.Sprite, typeof(Sprite), false, GUILayout.Width(200));
            DrawObjectField(data.Atlas, typeof(SpriteAtlas), false, GUILayout.Width(200));
            EditorGUI.EndDisabledGroup();

            GUILayout.Label(data.PrefabInfoList.Count.ToString(), GUILayout.Width(100));
            GUILayout.Label(FormatSpriteSize(data.Sprite), GUILayout.Width(100));
        }

        #endregion

        #region Sprite引用Prefab数据

        private void ShowSpritePrefab()
        {
            var curData = PeekStackData().Data as SpriteRefData;
            if (curData != null)
            {
                ShowSpritePrefabTitle();
                m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
                foreach (var iter in curData.PrefabInfoList)
                {
                    var data = iter.Value;
                    if (data == m_lastSelect)
                    {
                        GUILayout.BeginHorizontal(m_selectStyle);
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                    }
                    ShowSpritePrefabItem(data);
                    if (GUILayout.Button("查看引用原因", GUILayout.Width(150)))
                    {
                        PushStackData(new RefStackData(ShowType.SpritePrefabReason, data, m_scrollPosition));
                    }
                    GUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void ShowSpritePrefabTitle()
        {
            GUILayout.BeginHorizontal(m_tableHeaderStyle);
            GUILayout.Label("Prefab", GUILayout.Width(200));
            GUILayout.Label("引用数量", GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }

        private void ShowSpritePrefabItem(SpriteRefPrefabInfo data)
        {
            EditorGUI.BeginDisabledGroup(true);
            DrawObjectField(data.Prefab, typeof(GameObject), false, GUILayout.Width(200));
            EditorGUI.EndDisabledGroup();
            GUILayout.Label(data.RefList.Count.ToString(), GUILayout.Width(100));
        }

        #endregion

        #region Sprite引用Prefab原因

        private void ShowSpritePrefabReason()
        {
            var curData = PeekStackData().Data as SpriteRefPrefabInfo;
            if (curData != null)
            {
                ShowSpritePrefabReasonTitle();
                m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
                foreach (var data in curData.RefList)
                {
                    GUILayout.BeginHorizontal();
                    ShowSpritePrefabReasonItem(data);
                    GUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void ShowSpritePrefabReasonTitle()
        {
            GUILayout.BeginHorizontal(m_tableHeaderStyle);
            GUILayout.Label("Prefab", GUILayout.Width(200));
            GUILayout.Label("图集", GUILayout.Width(200));
            GUILayout.Label("Sprite", GUILayout.Width(200));
            GUILayout.Label("引用数量", GUILayout.Width(100));
            GUILayout.Label("HierarchyPath");
            GUILayout.EndHorizontal();
        }

        private void ShowSpritePrefabReasonItem(SpriteRefInfo data)
        {
            EditorGUI.BeginDisabledGroup(true);
            DrawObjectField(data.Prefab, typeof(GameObject), false, GUILayout.Width(200));
            DrawObjectField(data.Atlas, typeof(SpriteAtlas), false, GUILayout.Width(200));
            DrawObjectField(data.Sprite, typeof(Sprite), false, GUILayout.Width(200));
            EditorGUI.EndDisabledGroup();
            int spriteRefPrefabCnt = 0;
            if (s_spriteRefDataBySprite.TryGetValue(data.Sprite, out var spriteRefData))
            {
                spriteRefPrefabCnt = spriteRefData.PrefabInfoList.Count;
            }
            GUILayout.Label(spriteRefPrefabCnt.ToString(), GUILayout.Width(100));
            EditorGUILayout.TextField(data.HierarchyPath);

            if (GUILayout.Button("查看引用原因", GUILayout.Width(150)))
            {
                if (m_stackData[0].ShowType == ShowType.SceneGameObject)
                {
                    Transform targetChild = m_selectedObjectReference.Prefab.transform.Find(data.HierarchyPath);
                    if (targetChild != null)
                    {
                        EditorGUIUtility.PingObject(targetChild.gameObject);
                        Selection.activeGameObject = targetChild.gameObject;
                    }
                }
                else
                {
                    var prefabPath = AssetDatabase.GetAssetPath(data.Prefab);
                    PrefabStageUtility.OpenPrefab(prefabPath);

                    GameObject prefabRoot = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
                    Transform targetChild = prefabRoot.transform.Find(data.HierarchyPath);
                    if (targetChild != null)
                    {
                        EditorGUIUtility.PingObject(targetChild.gameObject);
                        Selection.activeGameObject = targetChild.gameObject;
                    }
                }
            }
        }

        #endregion

        #endregion

        #region 查看Prefab引用

        #region Prefab引用数据

        private void ShowPrefab()
        {
            ShowTypeToolbar();
            DrawAnalysisPanel("Prefab 引用", "查看每个 UI Prefab 使用的 SpriteAtlas 及引用详情。",
                s_prefabRefDataList.Count, AnalyzeReferences, s_prefabRefDataList.Count > 0 ? "重新分析" : "开始分析");

            if (s_prefabRefDataList.Count == 0)
            {
                DrawEmptyState("暂无 Prefab 引用数据，点击“开始分析”扫描当前配置目录。");
                return;
            }

            m_searchPrefabName = DrawSearchToolbar(m_searchPrefabName, "按 Prefab 名称筛选");

            ShowPrefabTitle();
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
            for (var index = 0; index < s_prefabRefDataList.Count; index++)
            {
                var data = s_prefabRefDataList[index];
                if (!string.IsNullOrEmpty(m_searchPrefabName) && !data.Prefab.name.Contains(m_searchPrefabName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (data == m_lastSelect)
                {
                    GUILayout.BeginHorizontal(m_selectStyle);
                }
                else
                {
                    GUILayout.BeginHorizontal();
                }
                ShowPrefabItem(data);
                if (GUILayout.Button("查看引用原因", GUILayout.Width(150)))
                {
                    PushStackData(new RefStackData(ShowType.PrefabAtlas, data, m_scrollPosition));
                }
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void ShowPrefabTitle()
        {
            GUILayout.BeginHorizontal(m_tableHeaderStyle);
            GUILayout.Label("Prefab", GUILayout.Width(200));
            GUILayout.Label("引用图集数量", GUILayout.Width(200));
            GUILayout.EndHorizontal();
        }

        private void ShowPrefabItem(PrefabRefData data)
        {
            EditorGUI.BeginDisabledGroup(true);
            DrawObjectField(data.Prefab, typeof(GameObject), false, GUILayout.Width(200));
            EditorGUI.EndDisabledGroup();
            GUILayout.Label(data.AtlasInfoList.Count.ToString(), GUILayout.Width(100));
        }

        #endregion

        #region Prefab引用图集数据

        private void ShowPrefabAtlas()
        {
            var curData = PeekStackData().Data as PrefabRefData;
            if (curData != null)
            {
                ShowPrefabAtlasTitle();
                m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
                foreach (var iter in curData.AtlasInfoList)
                {
                    var data = iter.Value;
                    if (data == m_lastSelect)
                    {
                        GUILayout.BeginHorizontal(m_selectStyle);
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                    }
                    ShowPrefabAtlasItem(data);
                    if (GUILayout.Button("查看引用原因", GUILayout.Width(150)))
                    {
                        PushStackData(new RefStackData(ShowType.PrefabAtlasReason, data, m_scrollPosition));
                    }
                    GUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void ShowPrefabAtlasTitle()
        {
            GUILayout.BeginHorizontal(m_tableHeaderStyle);
            GUILayout.Label("图集", GUILayout.Width(200));
            GUILayout.Label("引用数量", GUILayout.Width(200));
            GUILayout.EndHorizontal();
        }

        private void ShowPrefabAtlasItem(PrefabRefAtlasInfo data)
        {
            EditorGUI.BeginDisabledGroup(true);
            DrawObjectField(data.Atlas, typeof(SpriteAtlas), false, GUILayout.Width(200));
            EditorGUI.EndDisabledGroup();

            GUILayout.Label(data.RefList.Count.ToString(), GUILayout.Width(100));
        }

        #endregion

        #region Prefab引用Sprite原因

        private void ShowPrefabAtlasReason()
        {
            var curData = PeekStackData().Data as PrefabRefAtlasInfo;
            if (curData != null)
            {
                ShowSpritePrefabReasonTitle();
                m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
                foreach (var data in curData.RefList)
                {
                    GUILayout.BeginHorizontal();
                    ShowSpritePrefabReasonItem(data);
                    if (GUILayout.Button("查看Sprite引用", GUILayout.Width(150)))
                    {
                        if (s_spriteRefDataBySprite.TryGetValue(data.Sprite, out var spriteData))
                        {
                            PushStackData(new RefStackData(ShowType.SpritePrefab, spriteData, m_scrollPosition));
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
        }

        #endregion

        #endregion

        #region 查看Atlas引用

        #region Atlas引用数据

        private void ShowAtlas()
        {
            ShowTypeToolbar();
            DrawAnalysisPanel("Atlas 引用", "查看每个 SpriteAtlas 包含的 Sprite 与引用它的 UI Prefab。",
                s_atlasRefDataList.Count, AnalyzeReferences, s_atlasRefDataList.Count > 0 ? "重新分析" : "开始分析");

            if (s_atlasRefDataList.Count == 0)
            {
                DrawEmptyState("暂无 Atlas 引用数据，点击“开始分析”扫描当前配置目录。");
                return;
            }

            m_searchAtlasName = DrawSearchToolbar(m_searchAtlasName, "按 Atlas 路径筛选");

            ShowAtlasTitle();
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
            for (var index = 0; index < s_atlasRefDataList.Count; index++)
            {
                var data = s_atlasRefDataList[index];
                if (!string.IsNullOrEmpty(m_searchAtlasName) && !data.SpriteFolderPath.Contains(m_searchAtlasName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (data == m_lastSelect)
                {
                    GUILayout.BeginHorizontal(m_selectStyle);
                }
                else
                {
                    GUILayout.BeginHorizontal();
                }
                ShowAtlasItem(data);
                if (GUILayout.Button("查看包含Sprite", GUILayout.Width(150)))
                {
                    PushStackData(new RefStackData(ShowType.AtlasSprite, data, m_scrollPosition));
                }
                if (GUILayout.Button("查看引用Prefab", GUILayout.Width(150)))
                {
                    PushStackData(new RefStackData(ShowType.AtlasPrefab, data, m_scrollPosition));
                }
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void ShowAtlasTitle()
        {
            GUILayout.BeginHorizontal(m_tableHeaderStyle);
            GUILayout.Label("图集", GUILayout.Width(200));
            GUILayout.Label("包含Sprite数量", GUILayout.Width(100));
            GUILayout.Label("引用Prefab数量", GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }

        private void ShowAtlasItem(AtlasRefData data)
        {
            EditorGUI.BeginDisabledGroup(true);
            DrawObjectField(data.Atlas, typeof(SpriteAtlas), false, GUILayout.Width(200));
            EditorGUI.EndDisabledGroup();
            GUILayout.Label(data.SpriteList.Count.ToString(), GUILayout.Width(100));
            GUILayout.Label(data.PrefabInfoList.Count.ToString(), GUILayout.Width(100));
        }

        #endregion

        #region Atlas包含Sprite

        private void ShowAtlasSprite()
        {
            var curData = PeekStackData().Data as AtlasRefData;
            if (curData != null)
            {
                ShowSpriteTitle();
                m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
                foreach (var data in curData.SpriteList)
                {
                    if (data == m_lastSelect)
                    {
                        GUILayout.BeginHorizontal(m_selectStyle);
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                    }
                    ShowSpriteItem(data);
                    if (GUILayout.Button("查看引用原因", GUILayout.Width(150)))
                    {
                        PushStackData(new RefStackData(ShowType.SpritePrefab, data, m_scrollPosition));
                    }
                    GUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
        }

        #endregion


        #region Atlas引用Prefab

        private void ShowAtlasPrefab()
        {
            var curData = PeekStackData().Data as AtlasRefData;
            if (curData != null)
            {
                ShowAtlasPrefabTitle();
                m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
                foreach (var iter in curData.PrefabInfoList)
                {
                    var data = iter.Value;
                    if (data == m_lastSelect)
                    {
                        GUILayout.BeginHorizontal(m_selectStyle);
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                    }
                    ShowAtlasPrefabItem(data);
                    if (GUILayout.Button("查看引用原因", GUILayout.Width(150)))
                    {
                        PushStackData(new RefStackData(ShowType.AtlasPrefabReason, data, m_scrollPosition));
                    }
                    GUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void ShowAtlasPrefabTitle()
        {
            GUILayout.BeginHorizontal(m_tableHeaderStyle);
            GUILayout.Label("Prefab", GUILayout.Width(200));
            GUILayout.Label("引用数量", GUILayout.Width(200));
            GUILayout.EndHorizontal();
        }

        private void ShowAtlasPrefabItem(PrefabRefAtlasInfo data)
        {
            EditorGUI.BeginDisabledGroup(true);
            DrawObjectField(data.Prefab, typeof(SpriteAtlas), false, GUILayout.Width(200));
            EditorGUI.EndDisabledGroup();

            GUILayout.Label(data.RefList.Count.ToString(), GUILayout.Width(100));
        }

        #endregion

        #endregion

        #region 查看场景选中界面引用

        private void ShowSceneGameObject()
        {
            ShowTypeToolbar();
            DrawAnalysisPanel("场景物体引用", "先刷新 Atlas 引用索引，再分析 Hierarchy 中当前选中的场景物体。",
                m_selectedObjectReference?.AtlasInfoList.Count ?? 0, AnalyzeReferences,
                s_prefabRefDataList.Count > 0 ? "刷新引用索引" : "生成引用索引");

            if (s_prefabRefDataList.Count == 0)
            {
                DrawEmptyState("尚未生成引用索引，请先点击“生成引用索引”。");
                return;
            }

            using (new EditorGUILayout.VerticalScope(m_panelStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("当前选择", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(new GUIContent("分析选中场景物体",
                            EditorGUIUtility.IconContent("d_SceneViewOrtho").image), m_primaryButtonStyle,
                        GUILayout.Width(170f)))
                    {
                        AnalyzeSelectedSceneObject();
                    }
                }

                if (Selection.activeGameObject == null)
                {
                    EditorGUILayout.HelpBox("请先在 Hierarchy 中选择一个场景物体。", MessageType.Info);
                }
            }

            ShowSceneGameObjectTitle();
            GUILayout.BeginHorizontal();
            ShowSceneGameObjectItem(m_selectedObjectReference);
            if (m_selectedObjectReference != null)
            {
                if (GUILayout.Button("查看引用原因", GUILayout.Width(150)))
                {
                    PushStackData(new RefStackData(ShowType.PrefabAtlas, m_selectedObjectReference, m_scrollPosition));
                }
            }
            GUILayout.EndHorizontal();
        }

        private void ShowSceneGameObjectTitle()
        {
            GUILayout.BeginHorizontal(m_tableHeaderStyle);
            GUILayout.Label("当前选中场景物体", GUILayout.Width(200));
            GUILayout.Label("引用图集数量", GUILayout.Width(200));
            GUILayout.EndHorizontal();
        }

        private void ShowSceneGameObjectItem(PrefabRefData data)
        {
            EditorGUI.BeginDisabledGroup(true);
            DrawObjectField(data != null ? data.Prefab : null, typeof(GameObject), true, GUILayout.Width(200));
            EditorGUI.EndDisabledGroup();
            int count = data != null ? data.AtlasInfoList.Count : 0;
            GUILayout.Label(count.ToString(), GUILayout.Width(100));
        }

        #endregion

    }
}
