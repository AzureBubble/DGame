#if UNITY_EDITOR && SPINE_UNITY && SPINE_CSHARP

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    public static partial class SpineModelHelper
    {
        [MenuItem("Assets/Spine/处理UISpine模型")]
        static void GenUISpineModel()
    {
        if (Selection.assetGUIDs.Length <= 0)
        {
            return;
        }
        for (int i = 0; i < Selection.assetGUIDs.Length; i++)
        {
            var guid = Selection.assetGUIDs[i];
            var onePath = AssetDatabase.GUIDToAssetPath(guid);
            Debug.LogFormat("处理UISpine模型:{0}", onePath);
            if (File.Exists(onePath))
            {
                var skeletonDataAsset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(onePath);
                if (skeletonDataAsset.GetSkeletonData(true) == null)
                {
                    EditorUtility.DisplayDialog("Invalid SkeletonDataAsset", "Unable to create Spine GameObject.\n\nPlease check your SkeletonDataAsset.", "Ok");
                    return;
                }
                if (skeletonDataAsset != null)
                {
                    var assetName = skeletonDataAsset.name.Replace(AssetUtility.SkeletonDataSuffix, "");
                    var animationName =
                        onePath.StartsWith(ITEM_ORIGINAL_PATH) ||
                        onePath.StartsWith(PLAYER_ORIGINAL_PATH) ||
                        onePath.StartsWith(PLAYER_SHOW_ORIGINAL_PATH) ||
                        onePath.StartsWith(WALL_ORIGINAL_PATH) ||
                        onePath.StartsWith(WEAPON_ORIGINAL_PATH) ||
                        onePath.StartsWith(ROBOT_ORIGINAL_PATH) ||
                        onePath.StartsWith(ROBOT_SHOW_ORIGINAL_PATH) ||
                        onePath.StartsWith(BOSS_ORIGINAL_PATH) ||
                        onePath.StartsWith(PET_ORIGINAL_PATH) ||
                        onePath.StartsWith(PET_JN_ORIGINAL_PATH) ||
                        onePath.StartsWith(PLANT_LIHUI_ORIGINAL_PATH) ||
                        onePath.StartsWith(PLANT_MIRROR_ORIGINAL_PATH) ||
                        onePath.StartsWith(CHESSBOARD_ORIGINAL_PATH) ||
                        onePath.StartsWith(NPC_ORIGINAL_PATH) ||
                        onePath.StartsWith(UP_BG_ORIGINAL_PATH) ||
                        onePath.StartsWith(LOGIN_ORIGINAL_BACKGROUND_PATH) ||
                        onePath.StartsWith(HOME_DECORATION_ORIGINAL_PATH)
                            ? "idle"
                            : "run";

                    GameObject canvasGameObject = new GameObject("Canvas");
                    Canvas canvas = canvasGameObject.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                    GameObject parentGameObject = new GameObject();
                    parentGameObject.name = assetName;
                    parentGameObject.layer = LayerMask.NameToLayer("UI");
                    parentGameObject.transform.SetParent(canvas.transform, false);
                    parentGameObject.AddComponent<RectTransform>();

                    GameObject shaPanGameObject = new GameObject("m_tfUISpineRoot");
                    shaPanGameObject.transform.SetParent(parentGameObject.transform, false);
                    shaPanGameObject.AddComponent<RectTransform>();

                    GameObject effGameObject = new GameObject("m_tfEffRoot");
                    effGameObject.transform.SetParent(parentGameObject.transform, false);
                    effGameObject.AddComponent<RectTransform>();

                    var skeletonGraphicInspectorType = System.Type.GetType("Spine.Unity.Editor.SkeletonGraphicInspector");
                    var graphicInstantiateDelegate = skeletonGraphicInspectorType.GetMethod("SpawnSkeletonGraphicFromDrop", BindingFlags.Static | BindingFlags.Public);
                    var instantiateDelegate = System.Delegate.CreateDelegate(typeof(EditorInstantiation.InstantiateDelegate), graphicInstantiateDelegate) as EditorInstantiation.InstantiateDelegate;
                    SkeletonGraphic newSkeletonComponent = (SkeletonGraphic)instantiateDelegate.Invoke(skeletonDataAsset);
                    newSkeletonComponent.startingAnimation = animationName;
                    newSkeletonComponent.raycastTarget = false;
                    var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Mat/DodSkeletonGraphicDefault.mat");
                    newSkeletonComponent.material = material;
                    newSkeletonComponent.MeshGenerator.settings.useClipping = false;
                    newSkeletonComponent.allowMultipleCanvasRenderers = true;
                    GameObject newGameObject = newSkeletonComponent.gameObject;
                    newGameObject.transform.position = Vector3.zero;
                    newGameObject.name = "m_goSpineModel";
                    newGameObject.transform.SetParent(shaPanGameObject.transform, false);

                    SetExpandedRecursive(canvasGameObject, true);
                    //Selection.activeGameObject = newGameObject;

                    string savePath = GetSavePath(onePath,true);
                    if (string.IsNullOrEmpty(savePath))
                    {
                        Debug.Log("savePath error.");
                        return;
                    }
                    if (!Directory.Exists(savePath))
                    {
                        Directory.CreateDirectory(savePath);
                    }
                    savePath = string.Format("{0}/{1}.prefab", savePath, assetName);
                    PrefabUtility.SaveAsPrefabAsset(parentGameObject, savePath);
                    Debug.LogFormat("UISpine模型生成结束. {0}", savePath);
                    //GameObject.DestroyImmediate(newGameObject);
                }
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    }
}

#endif