#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace DGame
{
    public static class EditorSpriteSaveInfo
    {
        private static readonly HashSet<string> m_dirtyAtlasNames = new HashSet<string>();
        private static readonly HashSet<string> m_dirtyAtlasNamesNeedCreateNew = new HashSet<string>();
        private static readonly Dictionary<string, List<string>> m_atlasMap = new Dictionary<string, List<string>>();
        private static readonly Dictionary<string, string> m_atlasPathMap = new Dictionary<string, string>();
        private static bool m_intialized;
        private static bool m_isInScanExistingSprites;
        private static bool m_isBuildChange = false;
        private static AtlasConfig Config => AtlasConfig.Instance;

        static EditorSpriteSaveInfo()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            Initialize();
        }

        [MenuItem("DGame Tools/图集工具/立即重新生成变动的图集数据")]
        public static void ForceGenerateAll()
        {
            m_isBuildChange = true;
            try
            {
                RefreshChangedAtlases();
            }
            finally
            {
                m_isBuildChange = false;
            }
        }

        private static void RefreshChangedAtlases()
        {
            m_isInScanExistingSprites = true;
            try
            {
                EditorUtility.DisplayProgressBar("生成图集", "正在初始化...", 0f);
                m_atlasMap.Clear();
                EditorUtility.DisplayProgressBar("生成图集", "扫描现有精灵...", 0.2f);
                ScanExistingSprites();

                EditorUtility.DisplayProgressBar("生成图集", "分析变更...", 0.4f);
                if (m_isBuildChange)
                {
                    int current = 0;
                    int total = m_atlasMap.Count;
                    foreach (var item in m_atlasMap)
                    {
                        current++;
                        if (total > 0)
                        {
                            EditorUtility.DisplayProgressBar("生成图集", $"检查图集时间戳 ({current}/{total})...", 0.4f + 0.2f * current / total);
                        }

                        if (GetLatestAtlasTime(item.Key) >= GetLatestSpriteTime(item.Key))
                        {
                            continue;
                        }

                        m_dirtyAtlasNamesNeedCreateNew.Add(item.Key);
                    }
                }
                else
                {
                    m_dirtyAtlasNamesNeedCreateNew.UnionWith(m_atlasMap.Keys);
                }

                EditorUtility.DisplayProgressBar("生成图集", "生成图集文件...", 0.6f);
                ProcessDirtyAtlases();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                m_isInScanExistingSprites = false;
            }
        }

        /// <summary>
        /// 构建前同步刷新全部图集。保留现有图集资产与 GUID，仅删除已经没有来源的孤儿图集。
        /// </summary>
        public static void RefreshAllForBuild()
        {
            m_isInScanExistingSprites = true;
            try
            {
                EditorUtility.DisplayProgressBar("生成图集", "扫描现有精灵...", 0.2f);
                m_atlasPathMap.Clear();
                ClearCache();
                ScanExistingSprites(false, false);
                m_dirtyAtlasNames.UnionWith(m_atlasMap.Keys);
                EditorUtility.DisplayProgressBar("生成图集", "同步刷新图集文件...", 0.6f);
                ProcessDirtyAtlases();
                EditorUtility.DisplayProgressBar("生成图集", "清理失效图集...", 0.95f);
                DeleteOrphanAtlases();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                m_isInScanExistingSprites = false;
            }
        }

        private static void DeleteOrphanAtlases()
        {
            if (!Directory.Exists(Config.outputAtlasDir))
            {
                return;
            }

            IEnumerable<string> atlasFiles = Directory.GetFiles(Config.outputAtlasDir, "*.spriteatlas",
                    SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(Config.outputAtlasDir, "*.spriteatlasv2",
                    SearchOption.AllDirectories));
            foreach (string atlasPath in atlasFiles)
            {
                string atlasName = Path.GetFileNameWithoutExtension(atlasPath);
                if (!m_atlasMap.ContainsKey(atlasName))
                {
                    DeleteAtlas(atlasPath.Replace('\\', '/'));
                }
            }
        }

        private static void ProcessDirtyAtlases()
        {
            int totalCount = m_dirtyAtlasNames.Count + m_dirtyAtlasNamesNeedCreateNew.Count;
            int processedCount = 0;
            bool showProgress = totalCount > 3 && m_isInScanExistingSprites;
            try
            {
                while (m_dirtyAtlasNames.Count > 0)
                {
                    var atlasName = m_dirtyAtlasNames.First();
                    if (showProgress)
                    {
                        processedCount++;
                        EditorUtility.DisplayProgressBar("生成图集", $"更新图集: {atlasName} ({processedCount}/{totalCount})", 0.6f + 0.4f * processedCount / totalCount);
                    }
                    GenerateAtlas(atlasName, false);
                    m_dirtyAtlasNames.Remove(atlasName);
                }

                while (m_dirtyAtlasNamesNeedCreateNew.Count > 0)
                {
                    var atlasName = m_dirtyAtlasNamesNeedCreateNew.First();
                    if (showProgress)
                    {
                        processedCount++;
                        EditorUtility.DisplayProgressBar("生成图集", $"创建图集: {atlasName} ({processedCount}/{totalCount})", 0.6f + 0.4f * processedCount / totalCount);
                    }
                    // 删除、移动资源会进入此集合，即使剩余文件时间戳更旧也必须重建。
                    GenerateAtlas(atlasName, true);
                    m_dirtyAtlasNamesNeedCreateNew.Remove(atlasName);
                }
            }
            finally
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static void GenerateAtlas(string atlasName, bool createNew = false)
        {
            var outputPath = $"{Config.outputAtlasDir}/{atlasName}.spriteatlas";
            var outputPathV2 = outputPath.Replace(".spriteatlas", ".spriteatlasv2");
            string deletePath = outputPath;
            if (Config.enableV2)
            {
                DeleteAtlas(outputPath);
                deletePath = outputPathV2;
            }
            else
            {
                DeleteAtlas(outputPathV2);
                deletePath = outputPath;
            }

            if (createNew)
            {
                DeleteAtlas(deletePath);
                // AssetDatabase.DeleteAsset(deletePath);
            }
            var sprites = LoadValidSprites(atlasName);
            EnsureOutputDirectory();
            if (sprites.Count == 0)
            {
                DeleteAtlas(deletePath);
                return;
            }
            InternalGenerateAtlas(atlasName, sprites, outputPath);
        }

        private static string InternalGenerateAtlas(string atlasName, List<Sprite> sprites, string outputPath)
        {
            SpriteAtlasAsset spriteAtlasAsset = null;
            SpriteAtlas atlas = null;
            if (Config.enableV2)
            {
                outputPath = outputPath.Replace(".spriteatlas", ".spriteatlasv2");

                if (!File.Exists(outputPath))
                {
                    spriteAtlasAsset = new SpriteAtlasAsset();
                    atlas = new SpriteAtlas();
                }
                else
                {
                    spriteAtlasAsset = SpriteAtlasAsset.Load(outputPath);
                    atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(outputPath);
                    if (atlas != null)
                    {
                        var olds = atlas.GetPackables();

                        if (olds != null)
                        {
                            spriteAtlasAsset.Remove(olds);
                        }
                    }
                }
            }

            if (Config.enableV2)
            {
                spriteAtlasAsset?.Add(sprites.ToArray());
                SpriteAtlasAsset.Save(spriteAtlasAsset, outputPath);
                AssetDatabase.ImportAsset(outputPath,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
#if UNITY_2022_1_OR_NEWER
                SpriteAtlasImporter sai = AssetImporter.GetAtPath(outputPath) as SpriteAtlasImporter;
                if (sai == null)
                {
                    throw new InvalidOperationException($"无法获取图集导入器: {outputPath}");
                }

                ConfigureAtlasV2Settings(sai);
                if (AssetDatabase.WriteImportSettingsIfDirty(outputPath))
                {
                    AssetDatabase.ImportAsset(outputPath,
                        ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                }
#else
                ConfigureAtlasV2Settings(spriteAtlasAsset);
                SpriteAtlasAsset.Save(spriteAtlasAsset, outputPath);
                AssetDatabase.ImportAsset(outputPath,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
#endif
            }
            else
            {
                atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(outputPath);

                if (atlas != null)
                {
                    var olds = atlas.GetPackables();
                    if (olds != null)
                    {
                        atlas.Remove(olds);
                    }
                    ConfigureAtlasSettings(atlas);
                    atlas.Add(sprites.ToArray());
                    atlas.SetIsVariant(false);
                }
                else
                {
                    atlas = new SpriteAtlas();
                    ConfigureAtlasSettings(atlas);
                    atlas.Add(sprites.ToArray());
                    atlas.SetIsVariant(false);
                    AssetDatabase.CreateAsset(atlas, outputPath);
                }
            }
            if (atlas != null)
            {
                EditorUtility.SetDirty(atlas);
            }
            if (File.Exists(outputPath))
            {
                m_atlasPathMap[atlasName] = outputPath;
            }
            if (Config.enableLogging)
            {
                Debug.Log($"<b>[Generate Atlas]</b>: {atlasName} ({sprites.Count} sprites)");
            }

            return outputPath;
        }

        private static void ConfigureAtlasSettings(SpriteAtlas atlas)
        {
            void SetPlatform(string platform, TextureImporterFormat format)
            {
                var settings = atlas.GetPlatformSettings(platform);
                settings.overridden = true;
                settings.format = format;
                settings.compressionQuality = Config.compressionQuality;
                atlas.SetPlatformSettings(settings);
            }
            SetPlatform("Android", Config.androidFormat);
            SetPlatform("iPhone", Config.iosFormat);
            SetPlatform("WebGL", Config.webGLFormat);

            var PackingSettings = new SpriteAtlasPackingSettings()
            {
                padding = Config.padding,
                enableRotation = Config.enableRotation,
                blockOffset = Config.blockOffset,
                enableTightPacking = Config.tightPacking,
                enableAlphaDilation = Config.alphaDilation
            };
            atlas.SetPackingSettings(PackingSettings);
        }

#if UNITY_2022_1_OR_NEWER
        private static void ConfigureAtlasV2Settings(SpriteAtlasImporter atlasImporter)
        {
            void SetPlatform(string platform, TextureImporterFormat format)
            {
                var settings = atlasImporter.GetPlatformSettings(platform);

                if (settings == null)
                {
                    return;
                }
                settings.overridden = true;
                settings.format = format;
                settings.compressionQuality = Config.compressionQuality;
                atlasImporter.SetPlatformSettings(settings);
            }
            SetPlatform("Android", Config.androidFormat);
            SetPlatform("iPhone", Config.iosFormat);
            SetPlatform("WebGL", Config.webGLFormat);
            var packingSettings = new SpriteAtlasPackingSettings
            {
                padding = Config.padding,
                enableRotation = Config.enableRotation,
                blockOffset = Config.blockOffset,
                enableTightPacking = Config.tightPacking,
                enableAlphaDilation = Config.alphaDilation
            };
            atlasImporter.packingSettings = packingSettings;
        }
#else
        private static void ConfigureAtlasV2Settings(SpriteAtlasAsset atlasImporter)
        {
            void SetPlatform(string platform, TextureImporterFormat format)
            {
                var settings = atlasImporter.GetPlatformSettings(platform);
                if (settings == null)
                {
                    return;
                }
                settings.overridden = true;
                settings.format = format;
                settings.compressionQuality = Config.compressionQuality;
                atlasImporter.SetPlatformSettings(settings);
            }
            SetPlatform("Android", Config.androidFormat);
            SetPlatform("iPhone", Config.iosFormat);
            SetPlatform("WebGL", Config.webGLFormat);
            var packingSettings = new SpriteAtlasPackingSettings
            {
                padding = Config.padding,
                enableRotation = Config.enableRotation,
                blockOffset = Config.blockOffset,
                enableTightPacking = Config.tightPacking,
                enableAlphaDilation = true
            };
            atlasImporter.SetPackingSettings(packingSettings);
        }
#endif

        private static List<Sprite> LoadValidSprites(string atlasName)
        {
            if (m_atlasMap.TryGetValue(atlasName, out var spriteList))
            {
                var allSprites = new List<Sprite>();

                foreach (var spritePath in spriteList.Where(File.Exists))
                {
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(spritePath)
                        .OfType<Sprite>()
                        .Where(s => s != null)
                        .ToArray();
                    allSprites.AddRange(sprites);
                }
                return allSprites;
            }
            return new List<Sprite>();
        }

        private static void Initialize()
        {
            if (m_intialized)
            {
                return;
            }

            ScanExistingSprites(false, false);
            foreach (string atlasName in m_atlasMap.Keys)
            {
                if (GetLatestAtlasTime(atlasName) == DateTime.MinValue)
                {
                    m_dirtyAtlasNamesNeedCreateNew.Add(atlasName);
                }
            }
            m_intialized = true;
        }

        public static void OnImportSprite(string spritePath, bool isCreateNew = false, bool markDirty = true)
        {
            spritePath = spritePath.Replace("\\", "/");
            // 检测是否需要打图集
            if (!ShouldProcess(spritePath))
            {
                return;
            }

            // 获取图集名字
            var atlasName = ResolveAtlasName(spritePath);

            if (string.IsNullOrEmpty(atlasName))
            {
                return;
            }

            // 缓存sprite到图集缓存中
            if (!m_atlasMap.TryGetValue(atlasName, out var atlasList))
            {
                atlasList = new List<string>();
                m_atlasMap[atlasName] = atlasList;
            }

            if (!atlasList.Contains(spritePath))
            {
                atlasList.Add(spritePath);
            }

            if (markDirty)
            {
                MarkDirty(atlasName, isCreateNew);
            }
        }

        public static void OnDeleteSprite(string spritePath, bool isCreateNew = true)
        {
            spritePath = spritePath.Replace("\\", "/");
            if (!ShouldProcess(spritePath))
            {
                return;
            }
            var atlasName = ResolveAtlasName(spritePath);

            if (string.IsNullOrEmpty(atlasName))
            {
                return;
            }

            if (m_atlasMap.TryGetValue(atlasName, out var atlasList))
            {
                if (atlasList.Remove(spritePath))
                {
                    MarkDirty(atlasName, isCreateNew);
                }
            }
        }

        private static void ScanExistingSprites(bool isCreateNew = true, bool markDirty = true)
        {
            var sprites = new HashSet<string>(AssetDatabase.FindAssets("t:sprite", Config.sourceAtlasRootDir));
            sprites.UnionWith(AssetDatabase.FindAssets("t:sprite", Config.rootChildAtlasDir));
            foreach (var guid in sprites)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (ShouldProcess(path))
                {
                    OnImportSprite(path, isCreateNew, markDirty);
                }
            }
        }

        private static void OnUpdate()
        {
            if (m_isInScanExistingSprites) return;
            if (m_dirtyAtlasNames.Count > 0 || m_dirtyAtlasNamesNeedCreateNew.Count > 0)
            {
                ProcessDirtyAtlases();
            }
        }

        /// <summary>
        /// 根据 AtlasConfig 的目录规则解析 Sprite 对应的图集名称。
        /// </summary>
        public static string ResolveAtlasName(string spritePath)
        {
            spritePath = spritePath.Replace("\\", "/");
            string atlasName = GetAtlasName(spritePath);
            if (string.IsNullOrEmpty(atlasName))
            {
                return atlasName;
            }

            if (CheckIsNeedGenerateSingleAtlas(spritePath))
            {
                return GetSingleAtlasName(spritePath);
            }

            if (CheckIsNeedGenerateRootChildDirAtlas(spritePath))
            {
                return GetRootChildDirAtlasName(spritePath);
            }

            return atlasName;
        }

        private static string GetAtlasName(string spritePath)
        {
            var tempRootDirArr = new List<string>(Config.sourceAtlasRootDir);
            tempRootDirArr.AddRange(Config.rootChildAtlasDir);
            foreach (var rootPath in tempRootDirArr)
            {
                var tempPath = rootPath.Replace("\\", "/").TrimEnd('/');
                if (!IsPathUnderRoot(spritePath, tempPath))
                {
                    continue;
                }
                var relativePath = spritePath.Substring(tempPath.Length + 1).Split('/');
                // 根目录下文本不处理
                if (relativePath.Length < 2)
                {
                    return null;
                }
                // 提取目录部分
                var directories = relativePath.Take(relativePath.Length - 1);
                var atlasNames = string.Join("_", directories);
                // 根目录文件名
                var rootFolderName = Path.GetFileName(tempPath);
                return $"{rootFolderName}_{atlasNames}";
            }
            return null;
        }

        private static string GetRootChildDirAtlasName(string spritePath)
        {
            foreach (var rootPath in Config.rootChildAtlasDir)
            {
                var tempPath = rootPath.Replace("\\", "/").TrimEnd('/');
                if (!IsPathUnderRoot(spritePath, tempPath))
                {
                    continue;
                }

                string relativePath = spritePath.Substring(tempPath.Length + 1);
                int separatorIndex = relativePath.IndexOf('/');
                if (separatorIndex <= 0)
                {
                    return null;
                }

                string rootName = Path.GetFileName(tempPath);
                string directoryName = relativePath.Substring(0, separatorIndex);
                return $"{rootName}_{directoryName}";
            }
            return null;
        }

        private static string GetSingleAtlasName(string spritePath)
        {
            foreach (var rootPath in Config.sourceAtlasRootDir)
            {
                var tempPath = rootPath.Replace("\\", "/").TrimEnd('/');
                if (!IsPathUnderRoot(spritePath, tempPath))
                {
                    continue;
                }
                var relativePath = spritePath.Substring(tempPath.Length + 1).Split('/');
                // 根目录下文本不处理
                if (relativePath.Length < 2)
                {
                    return null;
                }
                // 提取目录部分
                // var directories = relativePath.Take(relativePath.Length - 1);
                relativePath[^1] = Path.GetFileNameWithoutExtension(spritePath);
                var atlasNames = string.Join("_", relativePath);
                // 根目录文件名
                var rootFolderName = Path.GetFileName(tempPath);
                return $"{rootFolderName}_{atlasNames}";
            }
            return null;
        }

        private static bool ShouldProcess(string spritePath)
        {
            return CheckIsImageFile(spritePath) && !CheckIsExcluded(spritePath);
        }

        private static bool CheckIsExcluded(string spritePath)
        {
            // 检查是否是需要排除的路径
            return CheckIsExcludeFolder(spritePath)//spritePath.StartsWith(Config.excludeFolder)
                   || Config.excludeKeywords.Any(key => spritePath.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool CheckIsNeedGenerateSingleAtlas(string spritePath)
        {
            // 检查是否是需要排除的路径
            return !CheckIsExcludeFolder(spritePath)//spritePath.StartsWith(Config.excludeFolder)
                   && Config.singleAtlasDir.Any(rootPath => IsPathUnderRoot(spritePath, rootPath));
        }

        private static bool CheckIsNeedGenerateRootChildDirAtlas(string spritePath)
        {
            // 检查是否是需要排除的路径
            return !CheckIsExcludeFolder(spritePath)//spritePath.StartsWith(Config.excludeFolder)
                   && Config.rootChildAtlasDir.Any(rootPath => IsPathUnderRoot(spritePath, rootPath));
        }

        private static bool CheckIsExcludeFolder(string assetPath)
        {
            foreach (var rootPath in AtlasConfig.Instance.excludeFolder)
            {
                var tempPath = rootPath.Replace("\\", "/").TrimEnd('/');
                if (IsPathUnderRoot(assetPath, tempPath))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsPathUnderRoot(string assetPath, string rootPath)
        {
            string normalizedPath = assetPath.Replace("\\", "/");
            string normalizedRoot = rootPath.Replace("\\", "/").TrimEnd('/');
            return normalizedPath.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool CheckIsImageFile(string spritePath)
        {
            // 检测是否是符合格式的Sprite资源
            var ext = Path.GetExtension(spritePath).ToLower();
            return ext.Equals(".png") || ext.Equals(".jpg") || ext.Equals(".jpeg");
        }

        private static void MarkDirty(string atlasName, bool isCreateNew = false)
        {
            if (m_isBuildChange)
            {
                if (GetLatestAtlasTime(atlasName) > GetLatestSpriteTime(atlasName))
                {
                    return;
                }
            }
            if (isCreateNew)
            {
                m_dirtyAtlasNamesNeedCreateNew.Add(atlasName);
            }
            else
            {
                if (!m_dirtyAtlasNamesNeedCreateNew.Contains(atlasName))
                {
                    m_dirtyAtlasNames.Add(atlasName);
                }
            }
        }

        private static DateTime GetLatestSpriteTime(string atlasName)
        {
            if (m_atlasMap.TryGetValue(atlasName, out List<string> list))
            {
                DateTime maxTime = DateTime.MinValue;
                foreach (var path in list)
                {
                    if (File.Exists(path))
                    {
                        var time = File.GetLastWriteTime(path);
                        if (time > maxTime) maxTime = time;
                    }
                }
                return maxTime;
            }
            return DateTime.MinValue;
        }

        private static DateTime GetLatestAtlasTime(string atlasName)
        {
            if (!m_atlasPathMap.TryGetValue(atlasName, out var atlasPath))
            {
                string extension = Config.enableV2 ? ".spriteatlasv2" : ".spriteatlas";
                atlasPath = $"{Config.outputAtlasDir}/{atlasName}{extension}";
            }

            if (File.Exists(atlasPath))
            {
                m_atlasPathMap[atlasName] = atlasPath;
                return File.GetLastWriteTime(atlasPath);
            }
            return DateTime.MinValue;
        }

        private static void DeleteAtlas(string atlasPath)
        {
            if (File.Exists(atlasPath))
            {
                AssetDatabase.DeleteAsset(atlasPath);

                if (Config.enableLogging)
                {
                    Debug.Log($"<b>[DeleteAtlas]</b> {atlasPath} path: {Path.GetFileName(atlasPath)}");
                }
            }
        }

        private static void EnsureOutputDirectory()
        {
            if (!Directory.Exists(Config.outputAtlasDir))
            {
                Directory.CreateDirectory(Config.outputAtlasDir);
            }
        }

        public static void ClearCache()
        {
            m_dirtyAtlasNamesNeedCreateNew.Clear();
            m_dirtyAtlasNames.Clear();
            m_atlasMap.Clear();
        }
    }
}

#endif
