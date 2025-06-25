using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DGame.Editor
{
    public static class OpenFolder
    {
        [MenuItem("DGame Tools/打开文件夹/打开PersistentDataPathFolder")]
        private static void OpenPersistentDataPathFolder()
        {
            string path = Application.persistentDataPath;
            path = path.Replace(@"\", "/");
            Application.OpenURL("file://" + path);
        }

        [MenuItem("DGame Tools/打开文件夹/打开StreamingAssetsPathFolder")]
        private static void OpenStreamingAssetsPathFolder()
        {
            string path = Application.streamingAssetsPath;
            path = path.Replace(@"\", "/");
            Application.OpenURL("file://" + path);
        }
    }
}