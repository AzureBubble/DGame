using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GameLogic;
using DGame;
using UnityEngine;
using YooAsset;
using AOT;

#if ENABLE_OBFUZ
using Obfuz;
#endif

#pragma warning disable CS0436

/// <summary>
/// 热更DLL入口
/// </summary>
#if ENABLE_OBFUZ
[ObfuzIgnore(ObfuzScope.TypeName | ObfuzScope.MethodName)]
#endif
public partial class GameStart
{
    private static List<Assembly> m_hotfixAssembly;

    public static void Entrance(object[] objects)
    {
        m_hotfixAssembly = (List<Assembly>)objects[0];
        ExecuteRuntimeInitializeOnLoadMethodBeforeSplash();
        ExecuteRuntimeInitializeOnLoadMethodAfterAssembliesLoaded();
        GameEventLauncher.Init();
        ExecuteRuntimeInitializeOnLoadMethodSubsystemRegistration();
        ExecuteRuntimeInitializeOnLoadMethodBeforeScene();
        ExecuteRuntimeInitializeOnLoadMethodAfterSceneLoad();
        Debugger.Warning("======= 看到此条日志代表你成功运行了热更新代码 =======");
        Debugger.Warning("======= Entrance GameStart =======");
        DGame.Utility.UnityUtil.AddDestroyListener(OnDestroy);
        Debugger.Warning("======= 开始游戏 =======");
        StartGame();
    }

    private static void StartGame()
    {
        UIModule.Instance.ShowWindow<TestWindow>();
    }

    private static void OnDestroy()
    {
        SingletonSystem.Destroy();
        Debugger.Warning("====== Destroy GameStart =======");
    }

    public static Assembly GetAssembly(string assemblyName)
    {
        assemblyName = assemblyName.Replace(".dll", "");
        IEnumerable<Assembly> allAssemblies =
            Settings.UpdateSettings.Enable ? m_hotfixAssembly : AppDomain.CurrentDomain.GetAssemblies();
        return allAssemblies.First(assembly => assembly.FullName.Contains(assemblyName));
    }

    #region ExecuteRuntimeInitializeOnLoadMethod

    private static void ExecuteRuntimeInitializeOnLoadMethodBeforeSplash()
    {
#if !UNITY_EDITOR
        if (!Settings.UpdateSettings.Enable)
        {
            return;
        }
        RuntimeInitializeOnLoadMethodCollector.ExecuteMethods(RuntimeInitializeLoadType.BeforeSplashScreen);
#endif
    }

    private static void ExecuteRuntimeInitializeOnLoadMethodAfterAssembliesLoaded()
    {
#if !UNITY_EDITOR
        if (!Settings.UpdateSettings.Enable)
        {
            return;
        }
        RuntimeInitializeOnLoadMethodCollector.ExecuteMethods(RuntimeInitializeLoadType.AfterAssembliesLoaded);
#endif
    }

    private static void ExecuteRuntimeInitializeOnLoadMethodSubsystemRegistration()
    {
#if !UNITY_EDITOR
        if (!Settings.UpdateSettings.Enable)
        {
            return;
        }
        RuntimeInitializeOnLoadMethodCollector.ExecuteMethods(RuntimeInitializeLoadType.SubsystemRegistration);
#endif
    }

    private static void ExecuteRuntimeInitializeOnLoadMethodBeforeScene()
    {
#if !UNITY_EDITOR
        if (!Settings.UpdateSettings.Enable)
        {
            return;
        }
        RuntimeInitializeOnLoadMethodCollector.ExecuteMethods(RuntimeInitializeLoadType.BeforeSceneLoad);
#endif
    }

    private static void ExecuteRuntimeInitializeOnLoadMethodAfterSceneLoad()
    {
#if !UNITY_EDITOR
        if (!Settings.UpdateSettings.Enable)
        {
            return;
        }
        RuntimeInitializeOnLoadMethodCollector.ExecuteMethods(RuntimeInitializeLoadType.AfterSceneLoad);
#endif
    }

    #endregion
}