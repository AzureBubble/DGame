using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.Utilities;

using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

#endif

#if ODIN_INSPECTOR && UNITY_EDITOR

using Sirenix.OdinInspector;

#endif

namespace DGame
{
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR

    public enum GameSpeedPreset
    {
        [LabelText("0x")] x0,
        [LabelText("0.01x")] x0_01,
        [LabelText("0.1x")] x0_1,
        [LabelText("0.25x")] x0_25,
        [LabelText("0.5x")] x0_5,
        [LabelText("1x")] x1,
        [LabelText("1.5x")] x1_5,
        [LabelText("2x")] x2,
        [LabelText("4x")] x4,
        [LabelText("8x")] x8,
        // [LabelText("16x")] x16,
        // [LabelText("32x")] x32
    }

#endif

    [DisallowMultipleComponent]
    public sealed class RootModule : MonoBehaviour
    {
        private static RootModule m_instance = null;

        public static RootModule Instance => m_instance == null ? UnityEngine.Object.FindObjectOfType<RootModule>() : m_instance;

        private const int DEFAULT_DPI = 96;

        private float m_gameSpeedBeforePause = 1f;

        [SerializeField]
#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR && UNITY_EDITOR
        [ValueDropdown("GetIStringUtilHelperImplementations"), LabelText("字符串辅助器"), FoldoutGroup("全局辅助器设置", true), DisableInPlayMode]
#endif
        private string stringUtilHelperTypeName = "DGame.DGameStringUtilHelper";

        [SerializeField]
#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR && UNITY_EDITOR
        [ValueDropdown("GetILogHelperImplementations"), LabelText("日志系统辅助器"), FoldoutGroup("全局辅助器设置", true), DisableInPlayMode]
#endif
        private string logHelperTypeName = "DGame.DGameLogHelper";

        [SerializeField]
#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR && UNITY_EDITOR
        [ValueDropdown("GetIJsonHelperImplementations"), LabelText("Json系统辅助器"), FoldoutGroup("全局辅助器设置", true), DisableInPlayMode]
#endif
        private string jsonHelperTypeName = "DGame.DefaultJsonHelper";

        [SerializeField]
#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR && UNITY_EDITOR
        [LabelText("游戏帧率"), OnValueChanged("OnFrameChanged"), ProgressBar(1, 120, DrawValueLabel = true, ValueLabelAlignment = TextAlignment.Center, Height = 18)]
#endif
        private int frameRate = 120;

        [SerializeField]
#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR && UNITY_EDITOR
        [LabelText("游戏速度"), OnValueChanged("OnGameSpeedChanged"), Range(0f, 8f), BoxGroup("游戏速度设置", centerLabel:true)]
#endif
        private float gameSpeed = 1f;

        [SerializeField]
#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR && UNITY_EDITOR
        [LabelText("可在后台运行"), OnValueChanged("OnRunInBackgroundChanged"), ToggleLeft]
#endif
        private bool runInBackground = true;

        [SerializeField]
#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR && UNITY_EDITOR
        [LabelText("从不休眠"), OnValueChanged("OnNeverSleepChanged"), ToggleLeft]
#endif
        private bool neverSleep = true;

        public int FrameRate
        {
            get => frameRate;
            set => Application.targetFrameRate = frameRate = value;
        }

        public float GameSpeed
        {
            get => gameSpeed;
            set => Time.timeScale = gameSpeed = value >= 0f ? value : 0f;
        }

        public bool IsGamePaused => gameSpeed <= 0f;

        public bool IsNormalGameSpeed => Mathf.Abs(gameSpeed - 1f) < 0.01f;

        public bool RunInBackground
        {
            get => runInBackground;
            set => Application.runInBackground = runInBackground = value;
        }

        public bool NeverSleep
        {
            get => neverSleep;
            set
            {
                neverSleep = value;
                Screen.sleepTimeout = value ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            }
        }

        private void Awake()
        {
            Debugger.Info($"======== RootModule Awake() ========");
            m_instance = this;
            InitStringUtilHelper();
            InitLogHelper();
            InitJsonHelper();
            Debugger.Info($"======== Unity Version: {Application.unityVersion} ========");

            Utility.Converter.ScreenDpi = Screen.dpi;
            if (Utility.Converter.ScreenDpi <= 0)
            {
                Utility.Converter.ScreenDpi = DEFAULT_DPI;
            }

            Application.targetFrameRate = frameRate;
            Time.timeScale = gameSpeed;
            Application.runInBackground = runInBackground;
            Screen.sleepTimeout = neverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;

            Application.lowMemory += OnLowMemory;
            GameTime.StartFrame();
        }

        private void OnLowMemory()
        {
            Debugger.Warning("======== 内存不足 自动清理缓存... ========");
            var objectPoolModule = ModuleSystem.GetModule<IObjectPoolModule>();
            objectPoolModule?.ReleaseAllUnusedToMemoryPool();
            var resourceModule = ModuleSystem.GetModule<IResourceModule>();
            resourceModule?.OnLowMemory();
        }

        private void InitLogHelper()
        {
            if (string.IsNullOrEmpty(logHelperTypeName))
            {
                return;
            }

            Type logHelperType = Utility.AssemblyUtil.GetType(logHelperTypeName);

            if (logHelperType == typeof(Nullable))
            {
                return;
            }

            if (logHelperType == null)
            {
                Debugger.Error("查找不到默认的ILogHelper类型：'{0}'", logHelperTypeName);
                return;
            }

            DGameLog.ILogHelper logHelper = Activator.CreateInstance(logHelperType) as DGameLog.ILogHelper;

            if (logHelper == null)
            {
                Debugger.Error("无法创建ILogHelper类型实例：'{0}'", logHelperTypeName);
                return;
            }

            DGameLog.SetLogHelper(logHelper);
        }

        private void InitStringUtilHelper()
        {
            if (string.IsNullOrEmpty(stringUtilHelperTypeName))
            {
                return;
            }
            Type type = Utility.AssemblyUtil.GetType(stringUtilHelperTypeName);

            if (type == typeof(Nullable))
            {
                return;
            }

            if (type == null)
            {
                Debugger.Error("查找不到默认的IStringUtilHelper类型：'{0}'", stringUtilHelperTypeName);
                return;
            }

            Utility.StringUtil.IStringUtilHelper stringUtilHelper = Activator.CreateInstance(type) as Utility.StringUtil.IStringUtilHelper;

            if (stringUtilHelper == null)
            {
                Debugger.Error("无法创建IStringUtilHelper类型实例：'{0}'", stringUtilHelperTypeName);
                return;
            }

            Utility.StringUtil.SetStringHelper(stringUtilHelper);
        }

        private void InitJsonHelper()
        {
            if (string.IsNullOrEmpty(jsonHelperTypeName))
            {
                return;
            }

            Type jsonHelperType = Utility.AssemblyUtil.GetType(jsonHelperTypeName);

            if (jsonHelperType == typeof(Nullable))
            {
                return;
            }

            if (jsonHelperType == null)
            {
                Debugger.Error("查找不到默认的IJsonHelper类型：'{0}'", jsonHelperTypeName);
                return;
            }

            Utility.IJsonHelper jsonHelper = Activator.CreateInstance(jsonHelperType) as Utility.IJsonHelper;

            if (jsonHelper == null)
            {
                Debugger.Error("无法创建IJsonHelper类型实例：'{0}'", jsonHelperTypeName);
                return;
            }

            Utility.JsonUtil.SetJsonHelper(jsonHelper);
        }

        private void Update()
        {
            GameTime.StartFrame();
            ModuleSystem.Update(GameTime.DeltaTime, GameTime.UnscaledDeltaTime);
        }

        private void FixedUpdate()
        {
            GameTime.StartFrame();
        }

        private void LateUpdate()
        {
            GameTime.StartFrame();
        }

        private void OnDestroy()
        {
#if !UNITY_EDITOR

            ModuleSystem.Destroy();

#endif
        }

        private void OnApplicationQuit()
        {
            Application.lowMemory -= OnLowMemory;
            StopAllCoroutines();
        }

        internal void Destroy()
        {
            Destroy(gameObject);
        }

        public void PauseGame()
        {
            if (IsGamePaused)
            {
                return;
            }
            m_gameSpeedBeforePause = GameSpeed;
            GameSpeed = 0f;
        }

        public void ResumeGame()
        {
            if (!IsGamePaused)
            {
                return;
            }
            GameSpeed = m_gameSpeedBeforePause;
        }

        public void ResetNormalGameSpeed()
        {
            if (IsNormalGameSpeed)
            {
                return;
            }
            GameSpeed = 1f;
        }

        #region Odin 相关处理

#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR

        #region 绘制游戏速度快速选择按钮

        [ShowInInspector]
        [HideLabel]
        [BoxGroup("游戏速度设置")]
        [HorizontalGroup("游戏速度设置/SpeedControls")]
        [EnumToggleButtons]
        private GameSpeedPreset GameSpeedPreset
        {
            get => GetGameSpeedPreset(GameSpeed);
            set
            {
                float newSpeed = GetGameSpeed(value);
                if (Mathf.Abs(GameSpeed - newSpeed) > 0.001f)
                {
                    if (EditorApplication.isPlaying)
                    {
                        GameSpeed = newSpeed;
                    }
                }
            }
        }

        private GameSpeedPreset GetGameSpeedPreset(float speed)
        {
            return speed switch
            {
                <= 0f => GameSpeedPreset.x0,
                <= 0.01f => GameSpeedPreset.x0_01,
                <= 0.1f => GameSpeedPreset.x0_1,
                <= 0.25f => GameSpeedPreset.x0_25,
                <= 0.5f => GameSpeedPreset.x0_5,
                <= 1f => GameSpeedPreset.x1,
                <= 1.5f => GameSpeedPreset.x1_5,
                <= 2f => GameSpeedPreset.x2,
                <= 4f => GameSpeedPreset.x4,
                <= 8f => GameSpeedPreset.x8,
                // <= 16f => GameSpeedPreset.x16,
                // <= 32f => GameSpeedPreset.x32,
                _ => GameSpeedPreset.x1
            };
        }

        private float GetGameSpeed(GameSpeedPreset preset)
        {
            return preset switch
            {
                GameSpeedPreset.x0 => 0f,
                GameSpeedPreset.x0_01 => 0.01f,
                GameSpeedPreset.x0_1 => 0.1f,
                GameSpeedPreset.x0_25 => 0.25f,
                GameSpeedPreset.x0_5 => 0.5f,
                GameSpeedPreset.x1 => 1.0f,
                GameSpeedPreset.x1_5 => 1.5f,
                GameSpeedPreset.x2 => 2.0f,
                GameSpeedPreset.x4 => 4.0f,
                GameSpeedPreset.x8 => 8.0f,
                // GameSpeedPreset.x16 => 16.0f,
                // GameSpeedPreset.x32 => 32.0f,
                _ => 1f
            };
        }

        #endregion

        private void OnFrameChanged()
        {
            if (EditorApplication.isPlaying)
            {
                FrameRate = frameRate;
            }
        }

        private void OnGameSpeedChanged()
        {
            if (EditorApplication.isPlaying)
            {
                GameSpeed = gameSpeed;
            }
        }

        private void OnNeverSleepChanged()
        {
            if (EditorApplication.isPlaying)
            {
                NeverSleep = neverSleep;
            }
        }

        private void OnRunInBackgroundChanged()
        {
            if (EditorApplication.isPlaying)
            {
                RunInBackground = runInBackground;
            }
        }

        private IEnumerable<ValueDropdownItem> GetIStringUtilHelperImplementations()
        {
            var types = Utility.AssemblyUtil.GetTypes(typeof(Utility.StringUtil.IStringUtilHelper));

            for (int i = 0; i < types.Count + 1; i++)
            {
                if (i == 0)
                {
                    var type = typeof(Nullable);
                    yield return new ValueDropdownItem(
                        text: "<None>",
                        value: type.AssemblyQualifiedName
                    );
                }
                else
                {
                    var type = types[i - 1];
                    yield return new ValueDropdownItem(
                        text: type.FullName,
                        value: type.AssemblyQualifiedName
                    );
                }
            }
        }

        private IEnumerable<ValueDropdownItem> GetILogHelperImplementations()
        {
            var types = Utility.AssemblyUtil.GetTypes(typeof(DGameLog.ILogHelper));

            for (int i = 0; i < types.Count + 1; i++)
            {
                if (i == 0)
                {
                    var type = typeof(Nullable);
                    yield return new ValueDropdownItem(
                        text: "<None>",
                        value: type.AssemblyQualifiedName
                    );
                }
                else
                {
                    var type = types[i - 1];
                    yield return new ValueDropdownItem(
                        text: type.FullName,
                        value: type.AssemblyQualifiedName
                    );
                }
            }
        }

        private IEnumerable<ValueDropdownItem> GetIJsonHelperImplementations()
        {
            var types = Utility.AssemblyUtil.GetTypes(typeof(Utility.IJsonHelper));

            for (int i = 0; i < types.Count + 1; i++)
            {
                if (i == 0)
                {
                    var type = typeof(Nullable);
                    yield return new ValueDropdownItem(
                        text: "<None>",
                        value: type.AssemblyQualifiedName
                    );
                }
                else
                {
                    var type = types[i - 1];
                    yield return new ValueDropdownItem(
                        text: type.FullName,
                        value: type.AssemblyQualifiedName
                    );
                }
            }
        }

#endif

        #endregion
    }
}