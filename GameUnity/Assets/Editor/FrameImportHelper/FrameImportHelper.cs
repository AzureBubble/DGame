using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    public class FrameImportHelper
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