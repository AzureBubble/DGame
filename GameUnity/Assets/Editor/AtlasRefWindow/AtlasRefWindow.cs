using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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
        /// Sprite 和 Prefab 的具体引用原因
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
        /// Sprite 引用了哪些 Prefab 的数据
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
        /// Sprite 引用某个 prefab 的原因
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
        /// Prefab 引用了哪些 Atlas 的数据
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
        /// Prefab 引用某个 Atlas 的原因
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
        /// Atlas 引用数据
        /// </summary>
        public class AtlasRefData
        {
            public string AtlasPath;
            public SpriteAtlas Atlas;
            public List<SpriteRefData> SpriteList = new List<SpriteRefData>();
            public Dictionary<GameObject, PrefabRefAtlasInfo> PrefabInfoList = new Dictionary<GameObject, PrefabRefAtlasInfo>();

            public AtlasRefData(string atlasPath, SpriteAtlas atlas)
            {
                AtlasPath = atlasPath;
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

        private Vector2 m_scrollPos;

        string[] inspectToolbarStrings = { "查看Sprite引用", "查看Prefab引用", "查看Atlas引用", "查看选中场景物体引用" };

        enum InspectType
        {
            Sprite,
            Prefab,
            Atlas,
            SceneGameObject,
        }
        private InspectType ActiveInspectType;

        private static List<SpriteRefData> m_listSpriteRef = new List<SpriteRefData>();
        private static Dictionary<Sprite, SpriteRefData> m_dicSpriteRef = new Dictionary<Sprite, SpriteRefData>();

        private static List<PrefabRefData> m_listPrefabRef = new List<PrefabRefData>();
        private static Dictionary<GameObject, PrefabRefData> m_dicPrefabRef = new Dictionary<GameObject, PrefabRefData>();

        private static List<AtlasRefData> m_listAtlasRef = new List<AtlasRefData>();
        private static Dictionary<string, AtlasRefData> m_dicAtlasRef = new Dictionary<string, AtlasRefData>();

        private PrefabRefData m_goRef;

        private const string NormalAtlasDir = "Assets/AssetArt/Atlas";
        private static string AtlasExtension => AtlasConfig.Instance.enableV2 ? ".spriteatlasv2" : ".spriteatlas";
        private string UIPrefabPath =>  Application.dataPath + "/BundleAssets/UI/";

        private static Texture2D m_selectBackground;
        private GUIStyle m_selectStyle;

        private static StringBuilder m_sbGetHierarchyPath = new StringBuilder();

        [MenuItem("DGame Tools/性能分析工具/AtlasRefWindow")]
        static void OpenWindow()
        {
            m_selectBackground = MakeTex(0, 255, 0, 128);
            AtlasRefWindow window = EditorWindow.CreateWindow<AtlasRefWindow>();
            window.Init();
            window.minSize = new Vector2(1200, 600);
        }

        private void Init()
        {
            m_stackData.Clear();
            PushStackData(new RefStackData(ShowType.Sprite, null, Vector2.zero));
            ActiveInspectType = InspectType.Sprite;
        }

        private void AnalysisAtlas()
        {
            m_listSpriteRef.Clear();
            m_dicSpriteRef.Clear();
            m_listPrefabRef.Clear();
            m_dicPrefabRef.Clear();
            m_listAtlasRef.Clear();
            m_dicAtlasRef.Clear();

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
                            var atlasPath = Path.GetDirectoryName(assetPath);
                            var atlas = GetAtlas(sprite);

                            var hierarchyPath = GetHierarchyPath(image.transform, prefab.transform);

                            // Sprite 数据
                            bool isNewSprite = false;
                            var spriteRefInfo = new SpriteRefInfo(sprite, atlas, prefab, hierarchyPath);
                            if (!m_dicSpriteRef.TryGetValue(sprite, out var spriteRefData))
                            {
                                isNewSprite = true;
                                spriteRefData = new SpriteRefData(sprite, atlas);
                                m_dicSpriteRef.Add(sprite, spriteRefData);
                                m_listSpriteRef.Add(spriteRefData);
                            }
                            if (!spriteRefData.PrefabInfoList.TryGetValue(prefab, out var spriteRefPrefabInfo))
                            {
                                spriteRefPrefabInfo = new SpriteRefPrefabInfo(sprite, prefab);
                                spriteRefData.PrefabInfoList.Add(prefab, spriteRefPrefabInfo);
                            }
                            spriteRefPrefabInfo.RefList.Add(spriteRefInfo);

                            // Prefab 数据
                            if (!m_dicPrefabRef.TryGetValue(prefab, out var prefabRefData))
                            {
                                prefabRefData = new PrefabRefData(prefab);
                                m_dicPrefabRef.Add(prefab, prefabRefData);
                                m_listPrefabRef.Add(prefabRefData);
                            }
                            if (!prefabRefData.AtlasInfoList.TryGetValue(atlasPath, out var atlasRefInfo))
                            {
                                atlasRefInfo = new PrefabRefAtlasInfo(prefab, atlas);
                                prefabRefData.AtlasInfoList.Add(atlasPath, atlasRefInfo);
                            }
                            atlasRefInfo.RefList.Add(spriteRefInfo);

                            // Atlas 数据
                            if (!m_dicAtlasRef.TryGetValue(atlasPath, out var atlasRefData))
                            {
                                atlasRefData = new AtlasRefData(atlasPath, atlas);
                                m_dicAtlasRef.Add(atlasPath, atlasRefData);
                                m_listAtlasRef.Add(atlasRefData);
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

            //处理没有引用的 Sprite
            int atlasCount = m_listAtlasRef.Count;
            for (var atlasIndex = 0; atlasIndex < atlasCount; atlasIndex++)
            {
                var atlasRefData = m_listAtlasRef[atlasIndex];
                var cancel = EditorUtility.DisplayCancelableProgressBar("分析图集", $"分析图集：{atlasIndex}/{atlasCount}", (float)atlasIndex / atlasCount);
                if (cancel)
                {
                    break;
                }
                var atlasPath = atlasRefData.AtlasPath;
                DirectoryInfo atlasDirInfo = new DirectoryInfo(atlasPath);
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
                    if (sprite != null && !m_dicSpriteRef.ContainsKey(sprite))
                    {
                        var spriteRefData = new SpriteRefData(sprite, atlasRefData.Atlas);
                        m_dicSpriteRef.Add(sprite, spriteRefData);
                        m_listSpriteRef.Add(spriteRefData);
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

            m_listSpriteRef.Sort((x, y) => -x.PrefabInfoList.Count.CompareTo(y.PrefabInfoList.Count));
            m_listPrefabRef.Sort((x, y) => -x.AtlasInfoList.Count.CompareTo(y.AtlasInfoList.Count));
            m_listAtlasRef.Sort((x, y) => -x.PrefabInfoList.Count.CompareTo(y.PrefabInfoList.Count));

            Debug.Log("AnalysisAtlas Complete");
        }

        private void AnalysisGameObject()
        {
            m_goRef = null;

            var prefab = Selection.activeGameObject;
            if (prefab != null)
            {
                m_goRef = new PrefabRefData(prefab);

                var arrImage = prefab.GetComponentsInChildren<Image>(true);
                for (int i = 0; i < arrImage.Length; i++)
                {
                    var image = arrImage[i];
                    if (image != null && image.sprite != null)
                    {
                        var sprite = image.sprite;
                        var assetPath = AssetDatabase.GetAssetPath(sprite);
                        var atlasPath = Path.GetDirectoryName(assetPath);
                        var atlas = GetAtlas(sprite);

                        var hierarchyPath = GetHierarchyPath(image.transform, prefab.transform);

                        var spriteRefInfo = new SpriteRefInfo(sprite, atlas, prefab, hierarchyPath);
                        if (!m_goRef.AtlasInfoList.TryGetValue(atlasPath, out var atlasRefInfo))
                        {
                            atlasRefInfo = new PrefabRefAtlasInfo(prefab, atlas);
                            m_goRef.AtlasInfoList.Add(atlasPath, atlasRefInfo);
                        }
                        atlasRefInfo.RefList.Add(spriteRefInfo);
                    }
                }
            }
        }

        private static SpriteAtlas GetAtlas(Sprite sprite)
        {
            var assetPath = AssetDatabase.GetAssetPath(sprite);
            string atlasName = TextureHelper.GetPackageTag(assetPath);
            var path = $"{NormalAtlasDir}/{atlasName}{AtlasExtension}";
            return AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
        }

        public static string GetHierarchyPath(Transform target, Transform root = null)
        {
            m_sbGetHierarchyPath.Clear();
            m_sbGetHierarchyPath.Append(target.name);

            var transform = target.parent;
            while (transform != root && transform != null)
            {
                m_sbGetHierarchyPath.Insert(0, transform.name + "/");
                transform = transform.parent;
            }

            //if (root != null)
            //{
            //    m_sbGetHierarchyPath.Insert(0, root.name + "/");
            //}

            return m_sbGetHierarchyPath.ToString();
        }

        private static Texture2D MakeTex(int r, int g, int b, int a)
        {
            Color color = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private void ShowObjectField(UnityEngine.Object obj, System.Type objType, bool allowSceneObjects, params GUILayoutOption[] options)
        {
            //EditorGUILayout.ObjectField(obj, objType, allowSceneObjects, options);
            var rect = EditorGUILayout.GetControlRect(false, 18f, options);
            EditorGUI.ObjectField(rect, obj, objType, allowSceneObjects);

            EventType eventType = Event.current.type;
            //if (!GUI.enabled && Event.current.rawType == EventType.MouseDown)
            //{
            //    eventType = Event.current.rawType;
            //}

            // EditorGUI.ObjectField 内部调用了 Event.current.Use() 改变了 EventType ，所以这里用  EventType.Used 而不是 EventType.MouseDown
            if (eventType == EventType.Used)
            {
                //var rect = GUILayoutUtility.GetLastRect();
                if (rect.Contains(Event.current.mousePosition))
                {
                    //EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                }
            }
        }

        private string GetSpriteSize(Sprite sprite)
        {
            return sprite.texture.width + " x " + sprite.texture.height;
        }

        private RefStackData PopStackData()
        {
            var data = m_stackData[m_stackData.Count - 1];
            m_stackData.RemoveAt(m_stackData.Count - 1);

            m_lastSelect = data.Data;
            m_scrollPos = data.LastScrollPos;
            return data;
        }

        private RefStackData PeekStackData()
        {
            return m_stackData[m_stackData.Count - 1];
        }

        private void PushStackData(RefStackData data)
        {
            m_stackData.Add(data);
            m_scrollPos = Vector2.zero;
        }

        void OnGUI()
        {
            m_selectStyle = new GUIStyle(GUI.skin.box);
            // 设置背景颜色
            m_selectStyle.normal.background = m_selectBackground;

            if (m_stackData.Count > 1)
            {
                if (GUILayout.Button("返回"))
                {
                    PopStackData();
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

                GUILayout.Box("", m_selectStyle, GUILayout.ExpandWidth(true), GUILayout.Height(2));
                GUILayout.Space(10);
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

        private void ShowTypeToolbar()
        {
            var type = (InspectType)GUILayout.Toolbar((int)ActiveInspectType, inspectToolbarStrings);
            if (type != ActiveInspectType)
            {
                ActiveInspectType = type;
                switch (ActiveInspectType)
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
        }

        #region 查看Sprite引用

        #region Sprite引用数据

        private void ShowSprite()
        {
            if (GUILayout.Button("分析图集"))
            {
                AnalysisAtlas();
            }

            if (m_listSpriteRef.Count == 0)
            {
                return;
            }

            ShowTypeToolbar();

            m_searchSpriteName = EditorGUILayout.TextField("搜索：", m_searchSpriteName);

            ShowSpriteTitle();
            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
            for (var index = 0; index < m_listSpriteRef.Count; index++)
            {
                var data = m_listSpriteRef[index];
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
                    PushStackData(new RefStackData(ShowType.SpritePrefab, data, m_scrollPos));
                }
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void ShowSpriteTitle()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Sprite", GUILayout.Width(200));
            GUILayout.Label("所属图集", GUILayout.Width(200));
            GUILayout.Label("引用Prefab数量", GUILayout.Width(100));
            GUILayout.Label("Size", GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }

        private void ShowSpriteItem(SpriteRefData data)
        {
            //GUILayout.Label(data.Sprite.name, GUILayout.Width(m_spriteNameWidth));
            //GUILayout.Box(data.Sprite.texture, GUILayout.Width(m_spriteSize), GUILayout.Height(m_spriteSize));
            //var boxRect = GUILayoutUtility.GetLastRect();
            //if (Event.current.type == EventType.MouseDown && boxRect.Contains(Event.current.mousePosition))
            //{
            //    EditorGUIUtility.PingObject(data.Sprite);
            //}

            EditorGUI.BeginDisabledGroup(true);
            ShowObjectField(data.Sprite.texture, typeof(Texture), false, GUILayout.Width(200));
            ShowObjectField(data.Atlas, typeof(SpriteAtlas), false, GUILayout.Width(200));
            //GUILayout.Label(data.Atlas, GUILayout.Width(200));
            EditorGUI.EndDisabledGroup();

            GUILayout.Label(data.PrefabInfoList.Count.ToString(), GUILayout.Width(100));
            GUILayout.Label(GetSpriteSize(data.Sprite), GUILayout.Width(100));
        }

        #endregion

        #region Sprite引用Prefab数据

        private void ShowSpritePrefab()
        {
            var curData = PeekStackData().Data as SpriteRefData;
            if (curData != null)
            {
                ShowSpritePrefabTitle();
                m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
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
                        PushStackData(new RefStackData(ShowType.SpritePrefabReason, data, m_scrollPos));
                    }
                    GUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void ShowSpritePrefabTitle()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Prefab", GUILayout.Width(200));
            GUILayout.Label("引用数量", GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }

        private void ShowSpritePrefabItem(SpriteRefPrefabInfo data)
        {
            EditorGUI.BeginDisabledGroup(true);
            ShowObjectField(data.Prefab, typeof(GameObject), false, GUILayout.Width(200));
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
                m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
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
            GUILayout.BeginHorizontal();
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
            ShowObjectField(data.Prefab, typeof(GameObject), false, GUILayout.Width(200));
            ShowObjectField(data.Atlas, typeof(SpriteAtlas), false, GUILayout.Width(200));
            ShowObjectField(data.Sprite.texture, typeof(Texture), false, GUILayout.Width(200));
            EditorGUI.EndDisabledGroup();
            int spriteRefPrefabCnt = 0;
            if (m_dicSpriteRef.TryGetValue(data.Sprite, out var spriteRefData))
            {
                spriteRefPrefabCnt = spriteRefData.PrefabInfoList.Count;
            }
            GUILayout.Label(spriteRefPrefabCnt.ToString(), GUILayout.Width(100));
            EditorGUILayout.TextField(data.HierarchyPath);

            if (GUILayout.Button("查看引用原因", GUILayout.Width(150)))
            {
                if (m_stackData[0].ShowType == ShowType.SceneGameObject)
                {
                    Transform targetChild = m_goRef.Prefab.transform.Find(data.HierarchyPath);
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
            if (GUILayout.Button("分析图集"))
            {
                AnalysisAtlas();
            }

            if (m_listPrefabRef.Count == 0)
            {
                return;
            }

            ShowTypeToolbar();

            m_searchPrefabName = EditorGUILayout.TextField("搜索：", m_searchPrefabName);

            ShowPrefabTitle();
            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
            for (var index = 0; index < m_listPrefabRef.Count; index++)
            {
                var data = m_listPrefabRef[index];
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
                    PushStackData(new RefStackData(ShowType.PrefabAtlas, data, m_scrollPos));
                }
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void ShowPrefabTitle()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Prefab", GUILayout.Width(200));
            GUILayout.Label("引用图集数量", GUILayout.Width(200));
            GUILayout.EndHorizontal();
        }

        private void ShowPrefabItem(PrefabRefData data)
        {
            EditorGUI.BeginDisabledGroup(true);
            ShowObjectField(data.Prefab, typeof(GameObject), false, GUILayout.Width(200));
            EditorGUI.EndDisabledGroup();
            GUILayout.Label(data.AtlasInfoList.Count.ToString(), GUILayout.Width(100));
            //GUILayout.Label(data.Atlas, GUILayout.Width(200));
            //EditorGUILayout.TextField(data.HierarchyPath, GUILayout.Width(300));
        }

        #endregion

        #region Prefab引用图集数据

        private void ShowPrefabAtlas()
        {
            var curData = PeekStackData().Data as PrefabRefData;
            if (curData != null)
            {
                ShowPrefabAtlasTitle();
                m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
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
                        PushStackData(new RefStackData(ShowType.PrefabAtlasReason, data, m_scrollPos));
                    }
                    GUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void ShowPrefabAtlasTitle()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("图集", GUILayout.Width(200));
            GUILayout.Label("引用数量", GUILayout.Width(200));
            GUILayout.EndHorizontal();
        }

        private void ShowPrefabAtlasItem(PrefabRefAtlasInfo data)
        {
            EditorGUI.BeginDisabledGroup(true);
            ShowObjectField(data.Atlas, typeof(SpriteAtlas), false, GUILayout.Width(200));
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
                m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
                foreach (var data in curData.RefList)
                {
                    GUILayout.BeginHorizontal();
                    ShowSpritePrefabReasonItem(data);
                    if (GUILayout.Button("查看Sprite引用", GUILayout.Width(150)))
                    {
                        if (m_dicSpriteRef.TryGetValue(data.Sprite, out var spriteData))
                        {
                            PushStackData(new RefStackData(ShowType.SpritePrefab, spriteData, m_scrollPos));
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
            if (GUILayout.Button("分析图集"))
            {
                AnalysisAtlas();
            }

            if (m_listAtlasRef.Count == 0)
            {
                return;
            }

            ShowTypeToolbar();

            m_searchAtlasName = EditorGUILayout.TextField("搜索：", m_searchAtlasName);

            ShowAtlasTitle();
            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
            for (var index = 0; index < m_listAtlasRef.Count; index++)
            {
                var data = m_listAtlasRef[index];
                if (!string.IsNullOrEmpty(m_searchAtlasName) && !data.AtlasPath.Contains(m_searchAtlasName, StringComparison.OrdinalIgnoreCase))
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
                    PushStackData(new RefStackData(ShowType.AtlasSprite, data, m_scrollPos));
                }
                if (GUILayout.Button("查看引用Prefab", GUILayout.Width(150)))
                {
                    PushStackData(new RefStackData(ShowType.AtlasPrefab, data, m_scrollPos));
                }
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void ShowAtlasTitle()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("图集", GUILayout.Width(200));
            GUILayout.Label("包含Sprite数量", GUILayout.Width(100));
            GUILayout.Label("引用Prefab数量", GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }

        private void ShowAtlasItem(AtlasRefData data)
        {
            EditorGUI.BeginDisabledGroup(true);
            ShowObjectField(data.Atlas, typeof(SpriteAtlas), false, GUILayout.Width(200));
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
                m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
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
                        PushStackData(new RefStackData(ShowType.SpritePrefab, data, m_scrollPos));
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
                m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
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
                        PushStackData(new RefStackData(ShowType.AtlasPrefabReason, data, m_scrollPos));
                    }
                    GUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void ShowAtlasPrefabTitle()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Prefab", GUILayout.Width(200));
            GUILayout.Label("引用数量", GUILayout.Width(200));
            GUILayout.EndHorizontal();
        }

        private void ShowAtlasPrefabItem(PrefabRefAtlasInfo data)
        {
            EditorGUI.BeginDisabledGroup(true);
            ShowObjectField(data.Prefab, typeof(SpriteAtlas), false, GUILayout.Width(200));
            EditorGUI.EndDisabledGroup();

            GUILayout.Label(data.RefList.Count.ToString(), GUILayout.Width(100));
        }

        #endregion

        #endregion

        #region 查看场景选中界面引用

        private void ShowSceneGameObject()
        {
            if (GUILayout.Button("分析图集"))
            {
                AnalysisAtlas();
            }

            if (m_listPrefabRef.Count == 0)
            {
                return;
            }

            ShowTypeToolbar();

            if (GUILayout.Button("分析选中场景物体"))
            {
                AnalysisGameObject();
            }

            ShowSceneGameObjectTitle();
            GUILayout.BeginHorizontal();
            ShowSceneGameObjectItem(m_goRef);
            if (m_goRef != null)
            {
                if (GUILayout.Button("查看引用原因", GUILayout.Width(150)))
                {
                    PushStackData(new RefStackData(ShowType.PrefabAtlas, m_goRef, m_scrollPos));
                }
            }
            GUILayout.EndHorizontal();
        }

        private void ShowSceneGameObjectTitle()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("当前选中场景物体", GUILayout.Width(200));
            GUILayout.Label("引用图集数量", GUILayout.Width(200));
            GUILayout.EndHorizontal();
        }

        private void ShowSceneGameObjectItem(PrefabRefData data)
        {
            EditorGUI.BeginDisabledGroup(true);
            ShowObjectField(data != null ? data.Prefab : null, typeof(GameObject), true, GUILayout.Width(200));
            EditorGUI.EndDisabledGroup();
            int count = data != null ? data.AtlasInfoList.Count : 0;
            GUILayout.Label(count.ToString(), GUILayout.Width(100));
        }

        #endregion

    }
}