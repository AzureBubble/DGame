using UnityEngine;
using UnityEngine.SceneManagement;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class SceneInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Scene Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Scene Count", SceneManager.sceneCount.ToString(), "当前加载的场景总数");
                    DrawItem("Scene Count In Build Settings", SceneManager.sceneCountInBuildSettings.ToString(), "在构建设置中配置的场景数量");

                    Scene activeScene = SceneManager.GetActiveScene();
#if UNITY_2018_3_OR_NEWER
                    DrawItem("Active Scene Handle", activeScene.handle.ToString(), "活动场景的内部句柄标识符");
#endif
                    DrawItem("Active Scene Name", activeScene.name, "当前活动场景的名称");
                    DrawItem("Active Scene Path", activeScene.path, "当前活动场景文件的完整资源路径");
                    DrawItem("Active Scene Build Index", activeScene.buildIndex.ToString(), "当前活动场景在构建设置中的索引号");
                    DrawItem("Active Scene Is Dirty", activeScene.isDirty.ToString(), "当前活动场景是否有未保存的修改");
                    DrawItem("Active Scene Is Loaded", activeScene.isLoaded.ToString(), "当前活动场景是否已完成加载");
                    DrawItem("Active Scene Is Valid", activeScene.IsValid().ToString(), "当前活动场景是否有效且可用的");
                    DrawItem("Active Scene Root Count", activeScene.rootCount.ToString(), "当前活动场景根层级中的游戏对象数量");
#if UNITY_2019_1_OR_NEWER
                    DrawItem("Active Scene Is Sub Scene", activeScene.isSubScene.ToString(), "当前活动场景是否为子场景");
#endif
                }
                GUILayout.EndVertical();
            }
        }
    }
}