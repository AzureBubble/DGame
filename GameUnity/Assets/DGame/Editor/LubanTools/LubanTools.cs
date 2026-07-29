using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    public static class LubanTools
    {
        [MenuItem("DGame Tools/Luban/转表", priority = -100)]
        private static void ConvertTableData()
        {
            if (!ConvertClientTableData())
            {
                Debug.LogError("客户端 LazyLoad 转表失败，请检查进程日志");
            }
        }

        public static bool ConvertClientTableData(int timeoutMs = 300000)
        {
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            string scriptPath = Application.dataPath + "/../../GameConfig/GenerateTool_Binary/gen_bin_client_lazyload.sh";
            string processPath = "/bin/bash";
            string arguments = $"\"{scriptPath}\"";
#elif UNITY_EDITOR_WIN
            string scriptPath = Application.dataPath + "/../../GameConfig/GenerateTool_Binary/gen_bin_client_lazyload.bat";
            string processPath = Environment.GetEnvironmentVariable("ComSpec");
            string arguments = $"/d /s /c \"\"{scriptPath}\"\"";
#else
            Debug.LogError("当前编辑器平台不支持自动转表");
            return false;
#endif
            Debug.Log($"执行转表：{scriptPath}");
            string previousAutoContinue = Environment.GetEnvironmentVariable("AUTO_CONTINUE");
            Environment.SetEnvironmentVariable("AUTO_CONTINUE", "1");
            bool success;
            try
            {
                success = ShellHelper.RunByPath(processPath, arguments, Path.GetDirectoryName(scriptPath), timeoutMs);
            }
            finally
            {
                Environment.SetEnvironmentVariable("AUTO_CONTINUE", previousAutoContinue);
            }
            AssetDatabase.Refresh();
            return success;
        }
    }
}
