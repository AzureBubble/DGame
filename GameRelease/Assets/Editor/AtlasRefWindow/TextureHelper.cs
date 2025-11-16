using UnityEditor;
using UnityEngine;

namespace DGame
{
    public class TextureHelper
    {
        public const string UIRawPath = "Assets/ABAssets/UIRaw/Atlas";
        public const string MonsterPath = "Assets/ActorModel/Monster";
        public const string HomePath = "Assets/ActorModel/Home";
        public const string UIBkgPath = "Assets/ABAssets/UIRaw/Atlas/Background";
        public const string SceneBkgPath = "Assets/SceneRaw/Battleground/AllBkg";
        public const string UIChapterIconPath = "Assets/Resources/UIRaw/MapIcon";

        private static bool IsOnePicOneAtlasPath(string fullName)
        {
            if (fullName.StartsWith(UIBkgPath, System.StringComparison.OrdinalIgnoreCase) ||
                fullName.StartsWith(SceneBkgPath, System.StringComparison.OrdinalIgnoreCase) ||
                fullName.StartsWith(UIChapterIconPath, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }



        private static string GetDeepPath(string oriPath, string targetPath, int deepCnt)
        {
            var idx = oriPath.LastIndexOf(targetPath) + targetPath.Length;

            for (int i = 0; i < deepCnt; i++)
            {
                var newIdx = oriPath.IndexOf("/", idx + 1);

                if (newIdx != -1)
                {
                    idx = newIdx;
                }
                else
                {
                    break;
                }
            }

            var finStr = oriPath.Substring(0, idx).Replace("Assets/", "").Replace("/", "_");
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
            int idx = fullName.LastIndexOf("Assets/");

            if (idx == -1)
            {
                return "";
            }

            //这是Assets/之后的路径
            string str = fullName.Substring(idx);

            if (IsOnePicOneAtlasPath(fullName))
            {
                str = str.Substring(0, str.LastIndexOf(".")).Replace("Assets/UIRaw/", "").Replace("/", "_");
            }
            else
            {
                //怪物的不同动作，打到同一个图集
                if (fullName.StartsWith(MonsterPath, System.StringComparison.OrdinalIgnoreCase))
                {
                    //Debug.LogFormat("{0}", GetDeepPath(fullName, MonsterPath, 1));
                    str = GetDeepPath(str, MonsterPath, 1);
                }
                else if (fullName.StartsWith(HomePath, System.StringComparison.OrdinalIgnoreCase))
                {
                    str = GetDeepPath(str, HomePath, 1);
                }
                else
                {
                    str = str.Substring(0, str.LastIndexOf("/")).Replace("Assets/", "").Replace("/", "_");
                }
            }

            return str;
        }
    }
}