using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GameLogic;
using DGame;
using UnityEngine;

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
        GameEventLauncher.Init();
        m_hotfixAssembly = (List<Assembly>)objects[0];
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
}