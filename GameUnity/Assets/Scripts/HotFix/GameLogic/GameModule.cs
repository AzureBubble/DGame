using System.Collections;
using System.Collections.Generic;
using DGame;
using UnityEngine;

namespace GameLogic
{
    public class GameModule
    {
        #region 模块

        private static RootModule m_rootModule;

        /// <summary>
        /// 基础模块
        /// </summary>
        public static RootModule RootModule
        {
            get => m_rootModule == null ? Object.FindObjectOfType<RootModule>() : m_rootModule;
            private set => m_rootModule = value;
        }

        private static IFsmModule m_fsmModule;

        /// <summary>
        /// 有限状态机模块
        /// </summary>
        public static IFsmModule FsmModule =>m_fsmModule == null ? GetModule<IFsmModule>() : m_fsmModule;

        private static IAnimModule m_animModule;

        /// <summary>
        /// 动画机模块
        /// </summary>
        public static IAnimModule AnimModule => m_animModule == null ? GetModule<IAnimModule>() : m_animModule;

        private static IResourceModule m_resourceModule;

        /// <summary>
        /// 资源管理模块
        /// </summary>
        public static IResourceModule ResourceModule => m_resourceModule == null ? GetModule<IResourceModule>() : m_resourceModule;

        private static IAudioModule m_audioModule;

        /// <summary>
        /// 音频模块
        /// </summary>
        public static IAudioModule AudioModule => m_audioModule == null ? GetModule<IAudioModule>() : m_audioModule;

        private static ISceneModule m_sceneModule;

        /// <summary>
        /// 场景管理模块
        /// </summary>
        public static ISceneModule SceneModule => m_sceneModule == null ? GetModule<ISceneModule>() : m_sceneModule;

        private static IGameTimerModule m_gameTimerModule;

        /// <summary>
        /// 计时器模块
        /// </summary>
        public static IGameTimerModule GameTimerModule => m_gameTimerModule == null ? GetModule<IGameTimerModule>() : m_gameTimerModule;

        private static IInputModule m_inputModule;

        /// <summary>
        /// 输入模块
        /// </summary>
        public static IInputModule InputModule => m_inputModule == null ? GetModule<IInputModule>() : m_inputModule;

        #endregion

        /// <summary>
        /// 获取游戏模块
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static T GetModule<T>() where T : class
        {
            T module = ModuleSystem.GetModule<T>();
            DLogger.Assert(condition: module != null, $"{typeof(T)} is null");
            return module;
        }

        public static void Destroy()
        {
            DLogger.Info("GameModule Destroy");
            m_rootModule = null;
            m_gameTimerModule = null;
            m_audioModule = null;
            m_sceneModule = null;
            m_animModule = null;
            m_resourceModule = null;
            m_fsmModule = null;
            m_inputModule = null;
        }
    }
}