using System;
using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    [DisallowMultipleComponent]
    public sealed partial class DebuggerDriver : MonoBehaviour
    {
        private static DebuggerDriver m_instance;
        public static DebuggerDriver Instance => m_instance;

        /// <summary>
        /// 默认调试器漂浮框大小
        /// </summary>
        internal static readonly Rect DefaultIconRect = new Rect(10f, 10f, 60f, 60f);

        /// <summary>
        /// 默认调试器窗口大小
        /// </summary>
        internal static readonly Rect DefaultWindowRect = new Rect(10f, 10f, 640f, 640f);

        /// <summary>
        /// 默认调试器窗口缩放比例
        /// </summary>
        internal static readonly float DefaultWindowScale = 1.5f;

        private static TextEditor m_textEditor;
        private IDebuggerModule m_debuggerModule;
        private readonly Rect m_dragRect = new Rect(0f, 0f, float.MaxValue, 25f);
        private Rect m_iconRect = DefaultIconRect;
        private Rect m_windowRect = DefaultWindowRect;
        private float m_windowScale = DefaultWindowScale;

        [SerializeField] private ImageSettings imageSettings;

        [SerializeField] [InspectorName("调试窗口开启模式")] private DebuggerActiveWindowType activeWindowType = DebuggerActiveWindowType.AlwaysOpen;

        public DebuggerActiveWindowType ActiveWindowType => activeWindowType;

        [SerializeField] private bool m_showFullWindow = false;
        [SerializeField] private ConsoleWindow m_consoleWindow = new ConsoleWindow();

        private SystemInformationWindow m_systemInformationWindow = new SystemInformationWindow();
        private EnvironmentInformationWindow m_environmentInformationWindow = new EnvironmentInformationWindow();
        private ScreenInformationWindow m_screenInformationWindow = new ScreenInformationWindow();
        private GraphicsInformationWindow m_graphicsInformationWindow = new GraphicsInformationWindow();
        private InputSummaryInformationWindow m_inputSummaryInformationWindow = new InputSummaryInformationWindow();
        private InputTouchInformationWindow m_inputTouchInformationWindow = new InputTouchInformationWindow();
        private InputLocationInformationWindow m_inputLocationInformationWindow = new InputLocationInformationWindow();
        private InputAccelerationInformationWindow m_inputAccelerationInformationWindow = new InputAccelerationInformationWindow();
        private InputGyroscopeInformationWindow m_inputGyroscopeInformationWindow = new InputGyroscopeInformationWindow();
        private InputCompassInformationWindow m_inputCompassInformationWindow = new InputCompassInformationWindow();
        private SceneInformationWindow m_sceneInformationWindow = new SceneInformationWindow();
        private PathInformationWindow m_pathInformationWindow = new PathInformationWindow();
        private TimeInformationWindow m_timeInformationWindow = new TimeInformationWindow();
        private QualityInformationWindow m_qualityInformationWindow = new QualityInformationWindow();
        private RuntimeMemorySummaryWindow m_runtimeMemorySummaryWindow = new RuntimeMemorySummaryWindow();
        private RuntimeMemoryInformationWindow<UnityEngine.Object> m_runtimeMemoryAllInformationWindow = new RuntimeMemoryInformationWindow<UnityEngine.Object>();
        private RuntimeMemoryInformationWindow<Texture> m_runtimeMemoryTextureInformationWindow = new RuntimeMemoryInformationWindow<Texture>();
        private RuntimeMemoryInformationWindow<Mesh> m_runtimeMemoryMeshInformationWindow = new RuntimeMemoryInformationWindow<Mesh>();
        private RuntimeMemoryInformationWindow<Material> m_runtimeMemoryMaterialInformationWindow = new RuntimeMemoryInformationWindow<Material>();
        private RuntimeMemoryInformationWindow<Shader> m_runtimeMemoryShaderInformationWindow = new RuntimeMemoryInformationWindow<Shader>();
        private RuntimeMemoryInformationWindow<AnimationClip> m_runtimeMemoryAnimationClipInformationWindow = new RuntimeMemoryInformationWindow<AnimationClip>();
        private RuntimeMemoryInformationWindow<AudioClip> m_runtimeMemoryAudioClipInformationWindow = new RuntimeMemoryInformationWindow<AudioClip>();
        private RuntimeMemoryInformationWindow<Font> m_runtimeMemoryFontInformationWindow = new RuntimeMemoryInformationWindow<Font>();
        private RuntimeMemoryInformationWindow<TextAsset> m_runtimeMemoryTextAssetInformationWindow = new RuntimeMemoryInformationWindow<TextAsset>();
        private RuntimeMemoryInformationWindow<ScriptableObject> m_runtimeMemoryScriptableObjectInformationWindow = new RuntimeMemoryInformationWindow<ScriptableObject>();
        private ObjectPoolInformationWindow m_objectPoolInformationWindow = new ObjectPoolInformationWindow();
        private MemoryPoolPoolInformationWindow m_memoryPoolPoolInformationWindow = new MemoryPoolPoolInformationWindow();
        private ProfilerInformationWindow m_profilerInformationWindow = new ProfilerInformationWindow();

        private SettingsWindow m_settingsWindow = new SettingsWindow();

        private FpsCounter m_fpsCounter;

        private GameObject m_eventSystem;

        private bool m_activeWindow;

        public bool ActiveWindow
        {
            get => m_debuggerModule.ActiveWindow;
            set=> enabled = m_debuggerModule.ActiveWindow = value;
        }

        public bool ShowFullWindow
        {
            get => m_showFullWindow;
            set
            {
                if (m_eventSystem != null)
                {
                    m_eventSystem.SetActive(!value);
                }
                m_showFullWindow = value;
            }
        }

        public Rect IconRect { get => m_iconRect; set => m_iconRect = value; }
        public Rect WindowRect { get => m_windowRect; set => m_windowRect = value; }
        public float WindowScale { get => m_windowScale; set => m_windowScale = value; }

        private void Awake()
        {
            switch (activeWindowType)
            {
                case DebuggerActiveWindowType.AlwaysOpen:
                    m_activeWindow = true;
                    break;

                case DebuggerActiveWindowType.OnlyOpenWhenDevelopment:
                    m_activeWindow = Debug.isDebugBuild;
                    break;

                case DebuggerActiveWindowType.OnlyOpenInEditor:
                    m_activeWindow = Application.isEditor;
                    break;

                default:
                    m_activeWindow = false;
                    break;
            }

            if (!m_activeWindow)
            {
                Destroy(gameObject);
            }

            m_instance = this;
            m_textEditor = new TextEditor();
            m_instance.gameObject.name = $"[{nameof(DebuggerDriver)}]";
            m_eventSystem = GameObject.Find("EventSystem");
        }

        private void Start()
        {
            Initialize();

            RegisterDebuggerWindow("Console", m_consoleWindow);
            RegisterDebuggerWindow("Information/System", m_systemInformationWindow);
            RegisterDebuggerWindow("Information/Environment", m_environmentInformationWindow);
            RegisterDebuggerWindow("Information/Screen", m_screenInformationWindow);
            RegisterDebuggerWindow("Information/Graphics", m_graphicsInformationWindow);
            RegisterDebuggerWindow("Information/Input/Summary", m_inputSummaryInformationWindow);
            RegisterDebuggerWindow("Information/Input/Touch", m_inputTouchInformationWindow);
            RegisterDebuggerWindow("Information/Input/Location", m_inputLocationInformationWindow);
            RegisterDebuggerWindow("Information/Input/Acceleration", m_inputAccelerationInformationWindow);
            RegisterDebuggerWindow("Information/Input/Gyroscope", m_inputGyroscopeInformationWindow);
            RegisterDebuggerWindow("Information/Input/Compass", m_inputCompassInformationWindow);
            RegisterDebuggerWindow("Information/Other/Scene", m_sceneInformationWindow);
            RegisterDebuggerWindow("Information/Other/Path", m_pathInformationWindow);
            RegisterDebuggerWindow("Information/Other/Time", m_timeInformationWindow);
            RegisterDebuggerWindow("Information/Other/Quality", m_qualityInformationWindow);
            RegisterDebuggerWindow("Profiler/Summary", m_profilerInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Summary", m_runtimeMemorySummaryWindow);
            RegisterDebuggerWindow("Profiler/Memory/All", m_runtimeMemoryAllInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Texture", m_runtimeMemoryTextureInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Mesh", m_runtimeMemoryMeshInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Material", m_runtimeMemoryMaterialInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Shader", m_runtimeMemoryShaderInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/AnimationClip", m_runtimeMemoryAnimationClipInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/AudioClip", m_runtimeMemoryAudioClipInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Font", m_runtimeMemoryFontInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/TextAsset", m_runtimeMemoryTextAssetInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/ScriptableObject", m_runtimeMemoryScriptableObjectInformationWindow);
            RegisterDebuggerWindow("Profiler/Object Pool", m_objectPoolInformationWindow);
            RegisterDebuggerWindow("Profiler/Reference Pool", m_memoryPoolPoolInformationWindow);
            RegisterDebuggerWindow("Other/Settings", m_settingsWindow);

            ActiveWindow = m_activeWindow;
        }

        private void Update()
        {
            m_fpsCounter?.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void OnGUI()
        {
            if (m_debuggerModule == null || !m_debuggerModule.ActiveWindow)
            {
                return;
            }
            GUISkin cachedGuiSkin = GUI.skin;
            Matrix4x4 cachedMatrix = GUI.matrix;
            GUI.skin = imageSettings.reporterScrollerSkin;
            GUI.matrix = Matrix4x4.Scale(new Vector3(m_windowScale, m_windowScale, 1f));
            if (m_showFullWindow)
            {
                m_windowRect = GUILayout.Window(0, m_windowRect, DrawWindow, "<b>DEBUGGER</b>");
            }
            else
            {
                m_iconRect = GUILayout.Window(0, m_iconRect, DrawDebuggerWindowIcon, "<b>DEBUGGER</b>");
            }

            GUI.matrix = cachedMatrix;
            GUI.skin = cachedGuiSkin;
        }

        private void DrawDebuggerWindowIcon(int windowID)
        {
            GUI.DragWindow(m_dragRect);
            GUILayout.Space(5);
            Color32 color = Color.white;
            m_consoleWindow.RefreshCount();
            if (m_consoleWindow.FatalCount > 0)
            {
                color = m_consoleWindow.GetLogStringColor(LogType.Exception);
            }
            else if (m_consoleWindow.ErrorCount > 0)
            {
                color = m_consoleWindow.GetLogStringColor(LogType.Error);
            }
            else if (m_consoleWindow.WarningCount > 0)
            {
                color = m_consoleWindow.GetLogStringColor(LogType.Warning);
            }
            else
            {
                color = m_consoleWindow.GetLogStringColor(LogType.Log);
            }

            string title = Utility.StringUtil.Format(Constant.DEFAULT_DEBUGGER_WINDOW_FPS_STRING, color.r, color.g, color.b, color.a, m_fpsCounter.CurrentFps);
            if (GUILayout.Button(title, GUILayout.Width(100f), GUILayout.Height(40f)))
            {
                ShowFullWindow = true;
            }
        }

        public void GetRecentLogs(List<LogNode> results)
        {
            m_consoleWindow.GetRecentLogs(results);
        }

        public void GetRecentLogs(List<LogNode> results, int count)
        {
            m_consoleWindow.GetRecentLogs(results, count);
        }

        private void DrawDebuggerWindowGroup(IDebuggerWindowGroup debuggerWindowGroup)
        {
            if (debuggerWindowGroup == null)
            {
                return;
            }
            List<string> names = new List<string>();
            string[] debuggerWindowNames = debuggerWindowGroup.GetDebuggerWindowNames();

            for (int i = 0; i < debuggerWindowNames.Length; i++)
            {
                names.Add(Utility.StringUtil.Format("<b>{0}</b>", debuggerWindowNames[i]));
            }

            if (debuggerWindowGroup == m_debuggerModule.DebuggerWindowRoot)
            {
                names.Add("<b>Close</b>");
            }

            int toolbarIndex = 0;
            if (names.Count > 8)
            {
                int itemsPerRow = 6;
                int colCnt = Mathf.CeilToInt((float)names.Count / itemsPerRow);
                float buttonHeight = 30f; // 每个按钮高度
                 toolbarIndex = GUILayout.SelectionGrid(
                    debuggerWindowGroup.SelectedIndex,
                    names.ToArray(),
                    itemsPerRow,
                    GUILayout.MaxWidth(Screen.width), // 总宽度
                    GUILayout.Height(buttonHeight * colCnt) // 每个按钮高度
                );
            }
            else
            {
                toolbarIndex = GUILayout.Toolbar(debuggerWindowGroup.SelectedIndex, names.ToArray(), GUILayout.Height(30f), GUILayout.MaxWidth(Screen.width));
            }
            if (toolbarIndex >= debuggerWindowGroup.DebuggerWindowCount)
            {
                ShowFullWindow = false;
                return;
            }

            if (debuggerWindowGroup.SelectedWindow == null)
            {
                return;
            }

            if (debuggerWindowGroup.SelectedIndex != toolbarIndex)
            {
                debuggerWindowGroup.SelectedWindow?.OnExit();
                debuggerWindowGroup.SelectedIndex = toolbarIndex;
                debuggerWindowGroup.SelectedWindow?.OnEnter();
            }

            if (debuggerWindowGroup.SelectedWindow is IDebuggerWindowGroup subDebuggerWindowGroup)
            {
                DrawDebuggerWindowGroup(subDebuggerWindowGroup);
            }
            debuggerWindowGroup.SelectedWindow?.OnDraw();
        }

        private void DrawWindow(int windowID)
        {
            GUI.DragWindow(m_dragRect);
            DrawDebuggerWindowGroup(m_debuggerModule.DebuggerWindowRoot);
        }

        private void OnDestroy()
        {
            PlayerPrefs.Save();
        }

        private void Initialize()
        {
            m_debuggerModule = ModuleSystem.GetModule<IDebuggerModule>();

            if (m_debuggerModule == null)
            {
                Debugger.Fatal("DebuggerModule无效");
                return;
            }

            m_fpsCounter = new FpsCounter(Constant.DEFAULT_DEBUGGER_WINDOW_FPS_UPDATE_INTERVAL);

            var lastIconX = PlayerPrefs.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_ICON_X, DefaultIconRect.x);
            var lastIconY = PlayerPrefs.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_ICON_Y, DefaultIconRect.y);
            var lastWindowX = PlayerPrefs.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_X, DefaultWindowRect.x);
            var lastWindowY = PlayerPrefs.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_Y, DefaultWindowRect.y);
            var lastWindowWidth = PlayerPrefs.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_WIDTH, DefaultWindowRect.width);
            var lastWindowHeight = PlayerPrefs.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_HEIGHT, DefaultWindowRect.height);
            m_windowScale = PlayerPrefs.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_SCALE, DefaultWindowScale);
            // m_windowRect = new Rect(lastIconX, lastIconY, DefaultIconRect.width, DefaultIconRect.height);
            m_windowRect = new Rect(lastWindowX, lastWindowY, lastWindowWidth, lastWindowHeight);
        }

        public void RegisterDebuggerWindow(string path, IDebuggerWindow debuggerWindow, params object[] args)
        {
            m_debuggerModule.RegisterDebuggerWindow(path, debuggerWindow, args);
        }

        public bool UnRegisterDebuggerWindow(string path)
        {
            return m_debuggerModule.UnRegisterDebuggerWindow(path);
        }

        public IDebuggerWindow GetDebuggerWindow(string path)
        {
            return m_debuggerModule.GetDebuggerWindow(path);
        }

        public bool SelectDebuggerWindow(string path)
        {
            return m_debuggerModule.SelectDebuggerWindow(path);
        }

        public void ResetWindowLayout()
        {
            IconRect = DefaultIconRect;
            WindowRect = DefaultWindowRect;
            WindowScale = DefaultWindowScale;
        }

        private static void CopyToClipboard(string content)
        {
            m_textEditor.text = content;
            m_textEditor.OnFocus();
            m_textEditor.Copy();
            m_textEditor.text = string.Empty;
        }
    }
}