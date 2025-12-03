using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    public class TextureHelper
    {
        public const string ReplacePrefix = "Assets/ABAssets/UIRaw/";
        public const string UIRawPath = "Assets/ABAssets/UIRaw/Atlas";
        public const string MonsterPath = "Assets/ActorModel/Monster";
        public const string HomePath = "Assets/ActorModel/Home";
        public const string UIBkgPath = "Assets/ABAssets/UIRaw/Atlas/Background";
        public const string SceneBkgPath = "Assets/SceneRaw/Battleground/AllBkg";
        public const string UIChapterIconPath = "Assets/Resources/UIRaw/MapIcon";

        private static bool IsOnePicOneAtlasPath(string fullName)
        {
            if(AtlasConfig.Instance.singleAtlasDir
               .Any(fullName.StartsWith))
            {
                return true;
            }
            // if (fullName.StartsWith(UIBkgPath, System.StringComparison.OrdinalIgnoreCase) ||
            //     fullName.StartsWith(SceneBkgPath, System.StringComparison.OrdinalIgnoreCase) ||
            //     fullName.StartsWith(UIChapterIconPath, System.StringComparison.OrdinalIgnoreCase))
            // {
            //     return true;
            // }

            return false;
        }

        private static string GetDeepPath(string oriPath, string targetPath, int deepCnt)
        {
            var idx = oriPath.LastIndexOf(targetPath, StringComparison.Ordinal) + targetPath.Length;

            for (int i = 0; i < deepCnt; i++)
            {
                var newIdx = oriPath.IndexOf("/", idx + 1, StringComparison.Ordinal);

                if (newIdx != -1)
                {
                    idx = newIdx;
                }
                else
                {
                    break;
                }
            }

            var finStr = oriPath.Substring(0, idx).Replace("Assets/ABAssets/", "").Replace("/", "_");
            return finStr;
        }

        /// <summary>
        /// 根据文件路径，返回图集名称
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        public static string GetPackageTag(string fullName)
        {
            fullName = fullName.Replace("\\", "/");
            if (!fullName.StartsWith("Assets/"))
            {
                return "";
            }
            // var idx = fullName.IndexOf("Assets/", StringComparison.Ordinal);
            // //这是Assets/之后的路径
            // string str = fullName.Substring(idx);
            string str = fullName;

            if (IsOnePicOneAtlasPath(fullName))
            {
                str = str.Substring(0, str.LastIndexOf(".", StringComparison.Ordinal))
                    .Replace(ReplacePrefix, "").Replace("/", "_");
            }
            else
            {
                if (AtlasConfig.Instance.sourceAtlasRootDir.Any(fullName.StartsWith))
                {
                    //Debug.LogFormat("{0}", GetDeepPath(fullName, MonsterPath, 1));
                    str = Path.GetDirectoryName(fullName)?.Replace("\\","/").Replace("Assets/ABAssets/UIRaw/", "").Replace("/", "_");
                }
                else if (AtlasConfig.Instance.rootChildAtlasDir.Any(fullName.StartsWith))
                {
                    var matchedPaths = AtlasConfig.Instance.rootChildAtlasDir
                        .Where(dir => !string.IsNullOrEmpty(dir))
                        .Select(dir => dir.Replace("\\", "/").TrimEnd('/'))
                        .Where(dir => fullName.StartsWith(dir + "/", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    var matchPath = matchedPaths.OrderByDescending(p => p.Length).First();

                    str = GetDeepPath(str, matchPath, 1);
                }
                else
                {
                    str = str.Substring(0, str.LastIndexOf("/", StringComparison.Ordinal)).Replace("Assets/UIRaw", "").Replace("/", "_");
                }
            }

            return str;
        }
    }
}