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
            ForceGenerateAll(false);
            m_isBuildChange = false;
        }

        public static void ForceGenerateAll(bool isClearAll)
        {
            m_isInScanExistingSprites = true;
            if (isClearAll)
            {
                m_atlasPathMap.Clear();
                ClearCache();
                ClearAllAtlas();
            }
            m_atlasMap.Clear();
            ScanExistingSprites();
            if (m_isBuildChange)
            {
                foreach (var item in m_atlasMap)
                {
                    if (GetLatestAtlasTime(item.Key) >= GetLatestSpriteTime(item.Key))
                    {
                        continue;
                    }
                    else
                    {
                        m_dirtyAtlasNamesNeedCreateNew.Add(item.Key);
                    }
                }
            }
            else
            {
                m_dirtyAtlasNamesNeedCreateNew.UnionWith(m_atlasMap.Keys);
            }
            ProcessDirtyAtlases(true);
            m_isInScanExistingSprites = false;
        }

        private static void ClearAllAtlas()
        {
            string[] atlasV2Files =
                Directory.GetFiles(Config.outputAtlasDir, "*.spriteatlasv2", SearchOption.AllDirectories);
            string[] atlasFiles =
                Directory.GetFiles(Config.outputAtlasDir, "*.spriteatlas", SearchOption.AllDirectories);

            foreach (string filePath in atlasFiles)
            {
                AssetDatabase.DeleteAsset(filePath);
            }

            foreach (string filePath in atlasV2Files)
            {
                AssetDatabase.DeleteAsset(filePath);
            }

            AssetDatabase.Refresh();
            Debug.Log($"已删除 {atlasFiles?.Length + atlasV2Files?.Length} 个图集文件");
        }

        private static void ProcessDirtyAtlases(bool force = false)
        {
            try
            {
                AssetDatabase.StartAssetEditing();

                while (m_dirtyAtlasNames.Count > 0)
                {
                    var atlasName = m_dirtyAtlasNames.First();
                    if (force || ShouldUpdateAtlas(atlasName))
                    {
                        GenerateAtlas(atlasName, false);
                    }
                    m_dirtyAtlasNames.Remove(atlasName);
                }

                while (m_dirtyAtlasNamesNeedCreateNew.Count > 0)
                {
                    var atlasName = m_dirtyAtlasNamesNeedCreateNew.First();
                    if (force || ShouldUpdateAtlas(atlasName))
                    {
                        GenerateAtlas(atlasName, true);
                    }
                    m_dirtyAtlasNamesNeedCreateNew.Remove(atlasName);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
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
            AssetDatabase.Refresh();
            EditorApplication.delayCall += () => { InternalGenerateAtlas(atlasName, sprites, outputPath); };
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
                AssetDatabase.Refresh();
                EditorApplication.delayCall += () =>
                {
#if UNITY_2022_1_OR_NEW
                    SpriteAtlasImporter sai = (SpriteAtlasImporter)AssetImporter.GetAtPath(outputPath);
                    ConfigureAtlasV2Settings(sai);
#else
                    ConfigureAtlasV2Settings(spriteAtlasAsset);
                    SpriteAtlasAsset.Save(spriteAtlasAsset, outputPath);
#endif
                    AssetDatabase.WriteImportSettingsIfDirty(outputPath);
                    AssetDatabase.Refresh();
                };
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
            EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
                enableTightPacking = Config.tightPacking
            };
            atlas.SetPackingSettings(PackingSettings);
        }

#if UNITY_2022_1_OR_NEW
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
                enableAlphaDilation = true
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

            ScanExistingSprites(false);
            m_intialized = true;
        }

        public static void OnImportSprite(string spritePath, bool isCreateNew = false)
        {
            spritePath = spritePath.Replace("\\", "/");
            // 检测是否需要打图集
            if (!ShouldProcess(spritePath))
            {
                return;
            }

            // 获取图集名字
            var atlasName = GetAtlasName(spritePath);

            if (string.IsNullOrEmpty(atlasName))
            {
                return;
            }

            if (CheckIsNeedGenerateSingleAtlas(spritePath))
            {
                atlasName = GetSingleAtlasName(spritePath);
            }
            else if (CheckIsNeedGenerateRootChildDirAtlas(spritePath))
            {
                atlasName = GetRootChildDirAtlasName(spritePath);
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
                MarkDirty(atlasName, isCreateNew);
                MarkParentAtlasesDirty(spritePath, isCreateNew);
            }
        }

        public static void OnDeleteSprite(string spritePath, bool isCreateNew = true)
        {
            spritePath = spritePath.Replace("\\", "/");
            if (!ShouldProcess(spritePath))
            {
                return;
            }
            var atlasName = GetAtlasName(spritePath);

            if (string.IsNullOrEmpty(atlasName))
            {
                return;
            }

            if (CheckIsNeedGenerateSingleAtlas(spritePath))
            {
                atlasName = GetSingleAtlasName(spritePath);
            }
            else if (CheckIsNeedGenerateRootChildDirAtlas(spritePath))
            {
                atlasName = GetRootChildDirAtlasName(spritePath);
            }

            if (m_atlasMap.TryGetValue(atlasName, out var atlasList))
            {
                if (atlasList.Remove(spritePath))
                {
                    MarkDirty(atlasName, isCreateNew);
                    MarkParentAtlasesDirty(spritePath, isCreateNew);
                }
            }
        }

        public static void MarkParentAtlasesDirty(string spritePath, bool isCreateNew)
        {
            var currentPath = Path.GetDirectoryName(spritePath)?.Replace("\\", "/");

            if(string.IsNullOrEmpty(currentPath)) return;
            var tempRootDirArr = new List<string>(Config.sourceAtlasRootDir);
            tempRootDirArr.AddRange(Config.rootChildAtlasDir);
            foreach (var rootPath in tempRootDirArr)
            {
                var tempPath = rootPath.Replace("\\", "/").TrimEnd('/');
                var tempCurrentPath = currentPath;

                if (!tempCurrentPath.StartsWith(tempPath))
                {
                    continue;
                }
                while (tempCurrentPath != null && tempCurrentPath.StartsWith(tempPath))
                {
                    var parentAtlasName = GetAtlasNameForDirectory(tempCurrentPath);

                    if (!string.IsNullOrEmpty(parentAtlasName))
                    {
                        MarkDirty(parentAtlasName, isCreateNew);
                    }
                    tempCurrentPath = Path.GetDirectoryName(tempCurrentPath)?.Replace("\\", "/");
                }
            }
        }

        private static string GetAtlasNameForDirectory(string directoryPath)
        {
            foreach (var rootPath in Config.sourceAtlasRootDir)
            {
                var tempPath = rootPath.Replace("\\", "/").TrimEnd('/');
                if (!directoryPath.StartsWith(tempPath + "/"))
                {
                    continue;
                }
                var relativePath = directoryPath.Substring(rootPath.Length + 1).Split('/');
                var atlasNamePart = string.Join("_", relativePath);
                var rootFolderName = Path.GetFileName(rootPath);
                return $"{rootFolderName}_{atlasNamePart}";
            }
            return null;
        }

        private static void ScanExistingSprites(bool isCreateNew = true)
        {
            List<string> sprites = new List<string>();
            var guids = AssetDatabase.FindAssets("t:sprite", Config.sourceAtlasRootDir);
            sprites.AddRange(guids);
            guids = AssetDatabase.FindAssets("t:sprite", Config.rootChildAtlasDir);
            sprites.AddRange(guids);
            foreach (var guid in sprites)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (ShouldProcess(path))
                {
                    OnImportSprite(path, isCreateNew);
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

        private static string GetAtlasName(string spritePath)
        {
            var tempRootDirArr = new List<string>(Config.sourceAtlasRootDir);
            tempRootDirArr.AddRange(Config.rootChildAtlasDir);
            foreach (var rootPath in tempRootDirArr)
            {
                var tempPath = rootPath.Replace("\\", "/").TrimEnd('/');
                if (!spritePath.StartsWith(tempPath + "/"))
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
                if (spritePath.StartsWith(tempPath))
                {
                    string[] subDirectories = AssetDatabase.GetSubFolders(tempPath);
                    foreach (var subDirectory in subDirectories)
                    {
                        if (spritePath.StartsWith(subDirectory))
                        {
                            string rootName = Path.GetFileName(tempPath);
                            string directoryName = Path.GetFileName(subDirectory);
                            return $"{rootName}_{directoryName}";
                        }
                    }
                }
            }
            return null;
        }

        private static string GetSingleAtlasName(string spritePath)
        {
            foreach (var rootPath in Config.sourceAtlasRootDir)
            {
                var tempPath = rootPath.Replace("\\", "/").TrimEnd('/');
                if (!spritePath.StartsWith(tempPath + "/"))
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
                   && Config.singleAtlasDir.Any(key => spritePath.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool CheckIsNeedGenerateRootChildDirAtlas(string spritePath)
        {
            // 检查是否是需要排除的路径
            return !CheckIsExcludeFolder(spritePath)//spritePath.StartsWith(Config.excludeFolder)
                   && Config.rootChildAtlasDir.Any(key => spritePath.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool CheckIsExcludeFolder(string assetPath)
        {
            foreach (var rootPath in AtlasConfig.Instance.excludeFolder)
            {
                var tempPath = rootPath.Replace("\\", "/").TrimEnd('/');
                if (assetPath.StartsWith(tempPath + "/"))
                {
                    return true;
                }
            }
            return false;
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

        private static bool ShouldUpdateAtlas(string atlasName)
        {
            return true;
        }

        private static DateTime GetLatestSpriteTime(string atlasName)
        {
            if (m_atlasMap.TryGetValue(atlasName, out List<string> list))
            {
                return list
                    .Select(p => new FileInfo(p).LastWriteTime)
                    .DefaultIfEmpty()
                    .Max();
            }
            return DateTime.MinValue;
        }

        private static DateTime GetLatestAtlasTime(string atlasName)
        {
            if (m_atlasPathMap.TryGetValue(atlasName, out var atlasPath))
            {
                return new FileInfo(atlasPath).LastWriteTime;
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
                AssetDatabase.Refresh();
            }
        }

        private static void EnsureOutputDirectory()
        {
            if (!Directory.Exists(Config.outputAtlasDir))
            {
                Directory.CreateDirectory(Config.outputAtlasDir);
                AssetDatabase.Refresh();
            }
        }

        public static void ClearCache()
        {
            m_dirtyAtlasNamesNeedCreateNew.Clear();
            m_dirtyAtlasNames.Clear();
            m_atlasMap.Clear();
            AssetDatabase.Refresh();
        }
    }
}

#endif