#if UNITY_EDITOR

using System;
using System.IO;
using GameLogic;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    public static class SaveDataTools
    {
        private const string MENU_ROOT = "DGame Tools/存档工具/";

        [MenuItem(MENU_ROOT + "清除PlayerPrefs", false, 91)]
        private static void ClearPlayerPrefs()
        {
            if (!EditorUtility.DisplayDialog("清除PlayerPrefs", "确定清除当前项目的所有PlayerPrefs数据吗？此操作不可恢复。", "清除", "取消"))
            {
                return;
            }

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[存档工具] PlayerPrefs已清除。");
        }

        [MenuItem(MENU_ROOT + "清除Json存档", false, 92)]
        private static void ClearJsonSaveData()
        {
            string saveDirectoryPath = BaseClientSaveData.JsonSaveDirectoryPath;
            if (!Directory.Exists(saveDirectoryPath))
            {
                EditorUtility.DisplayDialog("清除Json存档", $"未找到Json存档目录：\n{saveDirectoryPath}", "确定");
                return;
            }

            if (!EditorUtility.DisplayDialog("清除Json存档",
                    $"确定删除以下目录中的所有Json存档吗？此操作不可恢复。\n{saveDirectoryPath}", "清除", "取消"))
            {
                return;
            }

            try
            {
                Directory.Delete(saveDirectoryPath, true);
                Debug.Log($"[存档工具] Json存档已清除：{saveDirectoryPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[存档工具] Json存档清除失败：{saveDirectoryPath}\n{e}");
                EditorUtility.DisplayDialog("清除Json存档失败", $"无法删除存档目录，请查看Console。\n{saveDirectoryPath}", "确定");
            }
        }
    }
}

#endif