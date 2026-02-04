using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GameLogic;
using Object = UnityEngine.Object;

namespace DGame
{
    public sealed class FrameImportHelper
    {
        [MenuItem("Assets/GenerateTools/序列帧/导入序列帧资源")]
        public static void ImportFrame()
        {
            var selectedFolderPaths = UnityEditorUtil.GetSelectedObjectFolderPaths();

            if (selectedFolderPaths.Count != 1)
            {
                Debug.LogWarning("请先选中单个目录");
                return;
            }
            var importPath = selectedFolderPaths[0];
            if (!CheckIsFrameImportPath(importPath))
            {
                Debug.LogError("请先选中序列帧配置文件中设置的序列帧导入根目录中的子目录");
                return;
            }
            ImportFrameSprite(importPath);
        }

        [MenuItem("Assets/GenerateTools/序列帧/重置锚点")]
        public static void ResetPivot()
        {
            UnityEditorUtil.DoActionWithSelectedTargets(OnResetFrameSpritePivot);
        }

        [MenuItem("Assets/GenerateTools/序列帧/生成序列帧配置")]
        public static void StartGenerateFrameConfig()
        {
            UnityEditorUtil.DoActionWithSelectedTargets(GenerateFrameConfig);
        }

        [MenuItem("Assets/GenerateTools/序列帧/生成所有序列帧配置")]
        public static void StartGenerateAllFrameConfig()
        {
            UnityEditorUtil.DoActionWithSelectedTargets(GenerateAllFrameConfig);
        }

        private static void GenerateAllFrameConfig(string dirPath)
        {
            var dirs = Directory.GetDirectories(dirPath);
            foreach (var t in dirs)
            {
                var animDir = GetAssetPath(t);
                GenerateFrameConfig(animDir);
            }
        }

        private static void GenerateFrameConfig(string dirPath)
        {
            Debug.Log($"开始生成序列帧配置文件...  {dirPath}");
            var idxNum = dirPath.LastIndexOf('/');
            var prefabName = dirPath.Substring(idxNum + 1);

            if (!Directory.Exists(FrameImportConfig.Instance.FrameConfigGenerateDir))
            {
                Directory.CreateDirectory(FrameImportConfig.Instance.FrameConfigGenerateDir);
            }

            var configPath = $"{FrameImportConfig.Instance.FrameConfigGenerateDir}/{prefabName}";
            var configGo = AssetDatabase.LoadAssetAtPath<GameObject>(configPath);
            if (configGo == null)
            {
                configGo = new GameObject(prefabName);
                configGo.AddComponent<FrameSpritePool>();
                PrefabUtility.SaveAsPrefabAsset(configGo, $"{configPath}.prefab");
            }
            var pool = configGo.GetComponent<FrameSpritePool>();
            if (pool == null)
            {
                pool = configGo.AddComponent<FrameSpritePool>();
            }
            var animDirs = Directory.GetDirectories(dirPath);
            if (animDirs.Length <= 0)
            {
                Debug.LogWarning($"序列帧资源 {prefabName} 为空，生成序列帧配置文件结束.");
                return;
            }

            foreach (var animDir in animDirs)
            {
                var fullAnimDir = GetAssetPath(animDir);
                AddFrameSpriteToConfigPrefab(fullAnimDir, pool);
            }
            pool.SortAllSprites();
            PrefabUtility.SaveAsPrefabAsset(configGo, $"{configPath}.prefab");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Object.DestroyImmediate(configGo);
            Debug.LogFormat("生成序列帧配置结束！");
        }

        private static void AddFrameSpriteToConfigPrefab(string dirPath, FrameSpritePool pool)
        {
            var idxNum = dirPath.LastIndexOf('/');
            var animName = dirPath.Substring(idxNum + 1);
            var files = Directory.GetFiles(dirPath, FrameImportConfig.GetFrameSpriteExtensionName(), SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var spriteFileName = Path.GetFileNameWithoutExtension(file);
                var spritePath = file.Substring(file.IndexOf("Assets", StringComparison.Ordinal));
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

                if (sprite == null)
                {
                    Debug.LogError($"加载Sprite失败: {spritePath}");
                    continue;
                }

                if (sprite.texture.width > FrameImportConfig.Instance.spriteMaxSize ||
                    sprite.texture.height > FrameImportConfig.Instance.spriteMaxSize)
                {
                    Debug.LogWarning($"序列帧资源{sprite.name} 大小不匹配: 宽 x 高 = {sprite.texture.width} x {sprite.texture.height}");
                }

                Enum.TryParse<FrameAnimName>(animName, out var animNameEnum);
                foreach (var name in FrameImportConfig.Instance.FrameAnimNames)
                {
                    if (name != animNameEnum)
                    {
                        continue;
                    }
                    pool.AddSprite(animNameEnum, sprite);
                }

                if (pool.GetSprites(animNameEnum) != null && pool.GetSprites(animNameEnum).Count > FrameImportConfig.Instance.spriteMaxCapacity)
                {
                    Debug.LogWarning($"序列帧资源{spriteFileName} 容量不匹配: {pool.GetSprites(animNameEnum).Count}");
                }
            }
        }

        private static string GetAssetPath(string filePath)
        {
            filePath = filePath.Replace('\\', '/');

            string dataPath = Application.dataPath.Replace('\\', '/');

            if (filePath.StartsWith(dataPath, StringComparison.Ordinal))
            {
                return "Assets" + filePath.Substring(dataPath.Length);
            }

            return filePath;
        }

        private static void OnResetFrameSpritePivot(string dirPath)
        {
            dirPath = dirPath.Replace("\\", "/");
            TextureImporter template = null;

            foreach (var animNameEnum in FrameImportConfig.Instance.restPivotOrders)
            {
                var animName = animNameEnum.ToString();
                var path = Path.Combine(dirPath, animName).Replace("\\", "/");
                if (!Directory.Exists(path))
                {
                    continue;
                }
                var firstFile = Directory.GetFiles(path,FrameImportConfig.GetFrameSpriteExtensionName(), SearchOption.TopDirectoryOnly)?.FirstOrDefault();
                if (firstFile == null)
                {
                    continue;
                }
                firstFile = firstFile?.Replace("\\", "/");
                template = AssetImporter.GetAtPath(firstFile) as TextureImporter;

                if (template != null)
                {
                    break;
                }
            }

            if (template == null)
            {
                Debug.LogWarning("重置锚点失败，没有找到序列帧资源导入配置窗口restPivotOrders顺序的资源");
                return;
            }
            List<string> allSprites = new List<string>();
            UnityEditorUtil.GetAllFilesFromPath(allSprites, dirPath, FrameImportConfig.GetFrameSpriteExtensionName());
            Debug.LogFormat("<color=yellow>开始重置对象锚点: {0}</color>", dirPath);
            foreach (var file in allSprites)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(file);
                if (sprite == null)
                {
                    continue;
                }
                var ti = AssetImporter.GetAtPath(file) as TextureImporter;

                if (ti == null)
                {
                    continue;
                }
                var settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                ti.SetTextureSettings(settings);
                ti.spritePivot = new Vector2(template.spritePivot.x, template.spritePivot.y);
                ti.SaveAndReimport();
            }
            Debug.LogFormat("<color=green>重置锚点完成！</color>");
        }

        private static void ImportFrameSprite(string importPath)
        {
            string frameSourcePath = string.Empty;
            frameSourcePath = EditorUtility.OpenFolderPanel("选择序列帧图片根目录", frameSourcePath, "");
            if (string.IsNullOrEmpty(frameSourcePath))
            {
                Debug.LogError("请选择正确的资源目录");
                return;
            }
            var importDirName = Path.GetFileName(importPath);
            List<string> listSourcePath = new List<string>();
            UnityEditorUtil.GetAllFilesFromPath(listSourcePath, frameSourcePath, FrameImportConfig.GetFrameSpriteExtensionName());
            if (listSourcePath.Count > 0 && !listSourcePath[0].Contains(importDirName))
            {
                Debug.LogError("资源文件夹名必须和序列帧组名相同");
                return;
            }
            Directory.Delete(importPath, true);
            Directory.CreateDirectory(frameSourcePath);
            foreach (var sourcePath in listSourcePath)
            {
                string spriteImportPath = GetFrameSpriteImportPath(sourcePath, importPath);
                if (!string.IsNullOrEmpty(spriteImportPath))
                {
                    File.Copy(sourcePath, spriteImportPath);
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static string GetFrameSpriteImportPath(string spritePath, string dirPath)
        {
            foreach (var animNameEnum in FrameImportConfig.Instance.FrameAnimNames)
            {
                var animName = animNameEnum.ToString();
                if (spritePath.Contains(animName))
                {
                    var animDir = Path.Combine(dirPath, animName);
                    if (!Directory.Exists(animDir))
                    {
                        Directory.CreateDirectory(animDir);
                    }
                    var spriteName = Path.GetFileName(spritePath);
                    return Path.Combine(animDir, spriteName);
                }
            }
            return string.Empty;
        }

        private static bool CheckIsFrameImportPath(string path)
        {
            return path.StartsWith(FrameImportConfig.Instance.ImportFrameRootDir);
        }
    }
}