using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameLogic;
using DGame;
using UnityEngine;
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
        ExecuteRequireComponentCollector();
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

    private static void ExecuteRequireComponentCollector()
    {
        RequireComponentCollector.ExecuteMethods();
    }

    #region ExecuteRuntimeInitializeOnLoadMethod

    private static void ExecuteRuntimeInitializeOnLoadMethodBeforeSplash()
    {
        RuntimeInitializeOnLoadMethodCollector.ExecuteMethods(RuntimeInitializeLoadType.BeforeSplashScreen);
    }

    private static void ExecuteRuntimeInitializeOnLoadMethodAfterAssembliesLoaded()
    {
        RuntimeInitializeOnLoadMethodCollector.ExecuteMethods(RuntimeInitializeLoadType.AfterAssembliesLoaded);
    }

    private static void ExecuteRuntimeInitializeOnLoadMethodSubsystemRegistration()
    {
        RuntimeInitializeOnLoadMethodCollector.ExecuteMethods(RuntimeInitializeLoadType.SubsystemRegistration);
    }

    private static void ExecuteRuntimeInitializeOnLoadMethodBeforeScene()
    {
        RuntimeInitializeOnLoadMethodCollector.ExecuteMethods(RuntimeInitializeLoadType.BeforeSceneLoad);
    }

    private static void ExecuteRuntimeInitializeOnLoadMethodAfterSceneLoad()
    {
        RuntimeInitializeOnLoadMethodCollector.ExecuteMethods(RuntimeInitializeLoadType.AfterSceneLoad);
    }

    #endregion
}