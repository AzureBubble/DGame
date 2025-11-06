#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    public class SpritePostprocessor : AssetPostprocessor
    {
        private static List<string> m_resourcesToDelete = new List<string>();
        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            m_resourcesToDelete.Clear();
            var config = AtlasConfig.Instance;

            if (!config.autoGenerate)
            {
                return;
            }

            try
            {
                ProcessAssetChanges(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
            }
            catch (Exception e)
            {
                Debug.LogError($"图集生成失败：{e.Message}\n{e.StackTrace}");
            }
            finally
            {
                bool isDelete = m_resourcesToDelete.Count > 0;
                foreach (var res in m_resourcesToDelete)
                {
                    AssetDatabase.DeleteAsset(res);
                }

                if (isDelete)
                {
                    Debug.LogError($"<color=red>针对 {AtlasConfig.Instance.sourceAtlasRootDir} 路径下资源</color>\n<color=red>移除了空格和同名资源，请检查重新合入相关资源</color>");
                }
                AssetDatabase.Refresh();
            }
        }

        private static void ProcessAssetChanges(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            ProcessAssets(importedAssets, (path) =>
            {
                EditorSpriteSaveInfo.OnImportSprite(path);
                LogProcessed("[Added]", path);
            });

            ProcessAssets(deletedAssets, (path) =>
            {
                EditorSpriteSaveInfo.OnDeleteSprite(path);
                LogProcessed("[Deleted]", path);
            }, true);

            ProcessMovedAssets(movedFromAssetPaths, movedAssets);
        }

        private static void ProcessMovedAssets(string[] oldPaths, string[] newPaths)
        {
            if (oldPaths == null || newPaths == null)
            {
                return;
            }

            for (int i = 0; i < oldPaths.Length; i++)
            {
                if (ShouldProcessAsset(oldPaths[i]))
                {
                    EditorSpriteSaveInfo.OnDeleteSprite(oldPaths[i]);
                    LogProcessed("[Moved From]", oldPaths[i]);
                    EditorSpriteSaveInfo.MarkParentAtlasesDirty(oldPaths[i], true);
                }

                if (ShouldProcessAsset(newPaths[i]))
                {
                    if (CheckFileNameContainsSpace(newPaths[i]) || CheckDuplicateAssetName(newPaths[i]) || ChangeSpriteTextureType(newPaths[i]))
                    {
                        continue;
                    }
                    EditorSpriteSaveInfo.OnImportSprite(newPaths[i]);
                    LogProcessed("[Moved To]", newPaths[i]);
                    EditorSpriteSaveInfo.MarkParentAtlasesDirty(newPaths[i], false);
                }
            }
        }

        private static void LogProcessed(string operation, string path)
        {
            if (AtlasConfig.Instance.enableLogging)
            {
                Debug.Log($"<b>[{operation}]</b> {Path.GetFileName(path)}\nPath: {path}");
            }
        }

        private static void ProcessAssets(string[] importedAssets, Action<string> processor, bool isDelete = false)
        {
            if (importedAssets == null || importedAssets.Length == 0)
            {
                return;
            }

            foreach (var path in importedAssets)
            {
                if (ShouldProcessAsset(path))
                {
                    if (!isDelete && (CheckFileNameContainsSpace(path) || CheckDuplicateAssetName(path) || ChangeSpriteTextureType(path)))
                    {
                        continue;
                    }
                    processor?.Invoke(path);
                }
            }
        }

        private static bool CheckFileNameContainsSpace(string assetPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(assetPath);

            if (fileName.Contains(" "))
            {
                m_resourcesToDelete.Add(assetPath);
                Debug.LogError($"<color=red>发现资源名存在空格: {assetPath}</color>");
                return true;
            }
            return false;
        }

        private static bool CheckDuplicateAssetName(string assetPath)
        {
            var currentFileName = Path.GetFileNameWithoutExtension(assetPath);

            string rootDir = "";
            var tempRootDirArr = new List<string>(AtlasConfig.Instance.sourceAtlasRootDir);
            tempRootDirArr.AddRange(AtlasConfig.Instance.rootChildAtlasDir);
            foreach (var rootPath in tempRootDirArr)
            {
                var tempPath = rootPath.Replace("\\", "/");
                if (!assetPath.StartsWith(tempPath))
                {
                    continue;
                }
                rootDir = tempPath;
            }
            // var rootDir = AtlasConfig.Instance.sourceAtlasRootDir;
            if (string.IsNullOrEmpty(rootDir))
            {
                return false;
            }

            // 获取当前目录下所有图片文件
            var filesInDirectory = Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories)
                .Where(CheckIsValidImageFile)
                .ToArray();
            var normalizedCurrentPath = Path.GetFullPath(assetPath).Replace("\\", "/");
            foreach (var file in filesInDirectory)
            {
                var normalizedFile = Path.GetFullPath(file).Replace("\\", "/");
                if (normalizedFile.Equals(normalizedCurrentPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue; // 跳过自身
                }

                var otherFileName = Path.GetFileNameWithoutExtension(file);
                if (string.Equals(currentFileName, otherFileName, StringComparison.OrdinalIgnoreCase))
                {
                    m_resourcesToDelete.Add(assetPath);
                    Debug.LogError($"<color=red>发现同名资源冲突: 合入资源: {assetPath} 存在资源: {file}</color>");
                    return true;
                }
            }

            return false;
        }

        private static bool ChangeSpriteTextureType(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                return false;
            }
            bool isChange = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                isChange = true;
            }

            if (AtlasConfig.Instance.checkMipmaps)
            {
                if (AtlasConfig.Instance.enableMipmaps && !importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = true;
                    isChange = true;
                }
                else if (!AtlasConfig.Instance.enableMipmaps && importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    isChange = true;
                }
            }

            if (isChange)
            {
                LogProcessed("[Sprite Import Changed Reimport]", path);
                importer.SaveAndReimport();
            }
            return isChange;
        }

        private static bool ShouldProcessAsset(string assetPath)
        {
            var config = AtlasConfig.Instance;

            if (string.IsNullOrEmpty(assetPath) || assetPath.StartsWith("Packages/")
                || !CheckIsShowProcessPath(assetPath)//!assetPath.StartsWith(config.sourceAtlasRootDir)
                || CheckIsExcludeFolder(assetPath)//assetPath.StartsWith(config.excludeFolder)
                || !CheckIsValidImageFile(assetPath))
            {
                return false;
            }

            foreach (var keyword in config.excludeKeywords)
            {
                if (assetPath.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }
            }
            return true;
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

        private static bool CheckIsShowProcessPath(string assetPath)
        {
            var tempRootDirArr = new List<string>(AtlasConfig.Instance.sourceAtlasRootDir);
            tempRootDirArr.AddRange(AtlasConfig.Instance.rootChildAtlasDir);
            foreach (var rootPath in tempRootDirArr)
            {
                var tempPath = rootPath.Replace("\\", "/").TrimEnd('/');
                if (!assetPath.StartsWith(tempPath + "/"))
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        private static bool CheckIsValidImageFile(string path)
        {
            var ext = Path.GetExtension(path).ToLower();
            return ext.Equals(".png") || ext.Equals(".jpg") || ext.Equals(".jpeg");
        }
    }
}

#endif