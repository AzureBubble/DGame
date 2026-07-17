using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

namespace DGame
{
    /// <summary>
    /// DGame AssetBundle、热更 DLL 与 Player 的统一可视化构建入口。
    /// </summary>
    public sealed class BuildPipelineWindow : EditorWindow
    {
        private const string EDITOR_PREFS_PREFIX = "DGame.BuildPipelineWindow.";
        private const string HYBRIDCLR_GENERATE_ALL_MENU_ITEM = "HybridCLR/Generate/All";
        private const string LUBAN_CONVERT_MENU_ITEM = "DGame Tools/Luban/转表";
        private const int MAX_LOG_COUNT = 300;

        private static readonly string[] PLATFORM_NAMES =
        {
            "Windows 64-bit",
            "macOS",
            "Linux",
            "Android",
            "iOS",
            "WebGL",
        };

        private static readonly BuildTarget[] PLATFORM_TARGETS =
        {
            BuildTarget.StandaloneWindows64,
            BuildTarget.StandaloneOSX,
            BuildTarget.StandaloneLinux64,
            BuildTarget.Android,
            BuildTarget.iOS,
            BuildTarget.WebGL,
        };

        private readonly List<string> m_buildLogs = new List<string>();

        private BuildPipelineConfig m_config;
        private Vector2 m_scrollPosition;
        private Vector2 m_logScrollPosition;
        private bool m_showBasicSettings = true;
        private bool m_showDGameSettings = true;
        private bool m_showMinimalPackageSettings = true;
        private bool m_showAdvancedSettings;
        private bool m_showPlayerSettings = true;
        private bool m_showBuildLog;
        private bool m_isBuilding;

        [MenuItem("DGame Tools/Build/打包工具窗口", false, 149)]
        public static void ShowWindow()
        {
            var window = GetWindow<BuildPipelineWindow>();
            window.titleContent = new GUIContent("DGame 打包工具", EditorGUIUtility.IconContent("BuildSettings.Editor").image);
            window.minSize = new Vector2(520f, 660f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnBuildLogReceived;
            SaveSettings();
        }

        private void OnGUI()
        {
            if (m_config == null)
            {
                LoadSettings();
            }

            DrawHeader();

            using var scrollScope = new EditorGUILayout.ScrollViewScope(m_scrollPosition);
            m_scrollPosition = scrollScope.scrollPosition;

            DrawPreflightTips();
            DrawBasicSettings();
            DrawDGameSettings();
            DrawMinimalPackageSettings();
            DrawAdvancedSettings();
            DrawPlayerSettings();
            DrawActionButtons();
            DrawBuildLog();
        }

        private void DrawHeader()
        {
            GUILayout.Space(8f);

            var titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
            };

            EditorGUILayout.LabelField("DGame 打包工具", titleStyle, GUILayout.Height(28f));
            EditorGUILayout.LabelField("AssetBundle · HybridCLR DLL · Player",
                new GUIStyle(EditorStyles.centeredGreyMiniLabel), GUILayout.Height(18f));
            EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label($"当前平台: {EditorUserBuildSettings.activeBuildTarget}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("重新加载", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    LoadSettings();
                    AddLog("已重新加载本地构建配置");
                }

                if (GUILayout.Button("恢复默认", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    m_config = BuildPipelineConfig.CreateDefault(BuildTarget.StandaloneWindows64);
                    SaveSettings();
                    AddLog("已恢复 DGame 默认构建配置");
                }
            }
        }

        private void DrawPreflightTips()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "构建前请确认已执行 GameConfig/GenerateTool_Binary/gen_bin_client_lazyload；首包或 AOT/泛型引用变化且启用 HybridCLR 时，还需先执行 HybridCLR GenerateAll。",
                MessageType.Warning);

            using (new EditorGUI.DisabledScope(m_isBuilding || EditorApplication.isCompiling))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("执行客户端懒加载转表",
                            $"调用菜单 {LUBAN_CONVERT_MENU_ITEM}，执行 gen_bin_client_lazyload。"),
                        GUILayout.Height(28f)))
                    {
                        EditorApplication.delayCall += ExecuteLubanConvert;
                        AddLog("已提交客户端 LazyLoad 转表任务");
                    }

                    if (GUILayout.Button(new GUIContent("执行 HybridCLR GenerateAll",
                            $"调用菜单 {HYBRIDCLR_GENERATE_ALL_MENU_ITEM}，刷新 HybridCLR 生成产物。"),
                        GUILayout.Height(28f)))
                    {
                        EditorApplication.delayCall += ExecuteHybridCLRGenerateAll;
                        AddLog("已提交 HybridCLR GenerateAll 任务");
                    }
                }
            }

            if (Settings.UpdateSettings == null)
            {
                EditorGUILayout.HelpBox("未找到 UpdateSettings，将使用窗口内置默认值；窗口构建仍可独立执行。",
                    MessageType.Warning);
            }

            if (Array.FindIndex(EditorBuildSettings.scenes, scene => scene.enabled) < 0)
            {
                EditorGUILayout.HelpBox("Build Settings 中没有配置场景，Player 构建不会包含启动场景。", MessageType.Error);
            }
        }

        private void DrawBasicSettings()
        {
            m_showBasicSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_showBasicSettings,
                new GUIContent("基础设置", EditorGUIUtility.IconContent("BuildSettings.Editor").image));

            if (m_showBasicSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    int oldPlatformIndex = GetPlatformIndex(m_config.BuildTarget);
                    int platformIndex = EditorGUILayout.Popup("目标平台", oldPlatformIndex, PLATFORM_NAMES);

                    if (platformIndex != oldPlatformIndex)
                    {
                        ChangeBuildTarget(PLATFORM_TARGETS[platformIndex]);
                    }

                    m_config.BuildPipeline = (EBuildPipeline)EditorGUILayout.EnumPopup("YooAsset 构建管线",
                        m_config.BuildPipeline);
                    m_config.CompressOption = (ECompressOption)EditorGUILayout.EnumPopup("压缩方式",
                        m_config.CompressOption);

                    EditorGUILayout.Space(3f);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        m_config.PackageVersion = EditorGUILayout.TextField("资源版本号", m_config.PackageVersion);
                        if (GUILayout.Button("读取设置", GUILayout.Width(72f)))
                        {
                            m_config.PackageVersion = BuildPipelineConfig.GetDefaultPackageVersion();
                            RefreshDefaultAndroidPlayerPath();
                        }
                    }

                    DrawAssetBundleOutputPath();
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(3f);
        }

        private void DrawDGameSettings()
        {
            m_showDGameSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_showDGameSettings,
                new GUIContent("DGame 构建链路", EditorGUIUtility.IconContent("Settings").image));

            if (m_showDGameSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    UpdateSettings updateSettings = Settings.UpdateSettings;
                    string[] packageNames = GetCollectorPackageNames(m_config.PackageName);
                    int packageIndex = Mathf.Max(0, Array.IndexOf(packageNames, m_config.PackageName));
                    packageIndex = EditorGUILayout.Popup("资源包名", packageIndex, packageNames);
                    m_config.PackageName = packageNames[packageIndex];
                    m_config.EncryptionType = (EncryptionType)EditorGUILayout.EnumPopup("资源加密",
                        m_config.EncryptionType);
                    m_config.ForceGenerateAtlas = EditorGUILayout.Toggle("强制生成图集",
                        m_config.ForceGenerateAtlas);
                    m_config.ReplaceAssetPathWithAddress = EditorGUILayout.Toggle("路径替换为 Address",
                        m_config.ReplaceAssetPathWithAddress);

                    EditorGUILayout.Space(3f);
                    m_config.BuildHotFixDll = EditorGUILayout.ToggleLeft(
                        new GUIContent("构建 AB 前编译并复制热更 DLL",
                            "调用 DGame.BuildDllCommand.BuildAndCopyDlls，目标平台切换后执行。"),
                        m_config.BuildHotFixDll);

                    m_config.CopyToBuildAddress = EditorGUILayout.ToggleLeft(
                        new GUIContent("构建 AB 后同步 StreamingAssets 到 BuildAddress",
                            "仅影响当前窗口构建，不读取 UpdateSettings 的自动同步开关。"),
                        m_config.CopyToBuildAddress);

                    if (m_config.CopyToBuildAddress)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            m_config.BuildAddress = EditorGUILayout.TextField("BuildAddress",
                                m_config.BuildAddress);
                            if (GUILayout.Button("浏览", GUILayout.Width(52f)))
                            {
                                string selectedPath = EditorUtility.OpenFolderPanel("选择 BuildAddress",
                                    GetAbsoluteBuildAddress(m_config.BuildAddress), string.Empty);
                                if (!string.IsNullOrEmpty(selectedPath))
                                {
                                    m_config.BuildAddress = selectedPath.Replace('\\', '/');
                                }
                            }
                        }
                    }

                    m_config.OpenOutputDirectory = EditorGUILayout.ToggleLeft("构建成功后打开输出目录",
                        m_config.OpenOutputDirectory);

                    EditorGUILayout.HelpBox(
                        "以上参数属于 BuildPipelineWindow 独立配置；UpdateSettings 与 GameEntry 只用于生成默认值。",
                        MessageType.Info);

                    EditorGUILayout.Space(3f);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("读取项目默认", GUILayout.Height(24f)))
                        {
                            ApplyDGameProjectDefaults();
                        }

                        using (new EditorGUI.DisabledScope(updateSettings == null))
                        {
                            if (GUILayout.Button("定位 UpdateSettings", GUILayout.Height(24f)))
                            {
                                Selection.activeObject = updateSettings;
                                EditorGUIUtility.PingObject(updateSettings);
                            }
                        }

                        if (GUILayout.Button("打开客户端导表目录", GUILayout.Height(24f)))
                        {
                            OpenClientConfigGeneratorDirectory();
                        }
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(3f);
        }

        private void DrawAdvancedSettings()
        {
            m_showAdvancedSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_showAdvancedSettings,
                new GUIContent("YooAsset 高级设置", EditorGUIUtility.IconContent("ToolHandleGlobal").image));

            if (m_showAdvancedSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    m_config.VerifyBuildingResult = EditorGUILayout.ToggleLeft("验证构建结果",
                        m_config.VerifyBuildingResult);
                    m_config.EnableSharePackRule = EditorGUILayout.ToggleLeft("启用共享资源打包",
                        m_config.EnableSharePackRule);
                    m_config.UseAssetDependencyDB = EditorGUILayout.ToggleLeft("使用资源依赖数据库",
                        m_config.UseAssetDependencyDB);
                    m_config.ClearBuildCacheFiles = EditorGUILayout.ToggleLeft("清理构建缓存（禁用增量构建）",
                        m_config.ClearBuildCacheFiles);
                    m_config.BuildinFileCopyOption = (EBuildinFileCopyOption)EditorGUILayout.EnumPopup(
                        "内置文件复制方式", m_config.BuildinFileCopyOption);
                    m_config.FileNameStyle = (EFileNameStyle)EditorGUILayout.EnumPopup("文件名风格",
                        m_config.FileNameStyle);
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(3f);
        }

        private void DrawMinimalPackageSettings()
        {
            m_showMinimalPackageSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_showMinimalPackageSettings,
                new GUIContent("最小包设置", EditorGUIUtility.IconContent("Package Manager").image));

            if (m_showMinimalPackageSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    m_config.MinimalPackage = EditorGUILayout.ToggleLeft(
                        new GUIContent("启用最小包模式",
                            "构建后删除 StreamingAssets 中未保留的 .bundle 文件，完整远端资源仍保留在 AB 输出目录。"),
                        m_config.MinimalPackage);

                    if (m_config.MinimalPackage)
                    {
                        m_config.RetainTags = EditorGUILayout.TextField(
                            new GUIContent("保留 Tag（逗号分隔）", "匹配任一 Tag 的 Bundle 会保留在首包中。"),
                            m_config.RetainTags);

                        string tagInfo = string.IsNullOrWhiteSpace(m_config.RetainTags)
                            ? "所有 .bundle 文件将被删除（仅保留清单）"
                            : $"保留带 [{m_config.RetainTags}] Tag 的 bundle，其余删除";
                        EditorGUILayout.HelpBox(
                            $"最小包模式：删除 StreamingAssets 中所有 .bundle 文件，仅保留清单文件（.bytes/.hash/.version）。\n" +
                            $"当前: {tagInfo}\n\n" +
                            "适用于 HostPlayMode 在线下载资源的场景，可大幅减小首包体积。",
                            MessageType.Info);
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(3f);
        }

        private void DrawPlayerSettings()
        {
            m_showPlayerSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_showPlayerSettings,
                new GUIContent("Player 设置", EditorGUIUtility.IconContent("UnityLogo").image));

            if (m_showPlayerSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(
                        $"Player 与 AssetBundle 使用同一目标平台：{m_config.BuildTarget}",
                        EditorStyles.wordWrappedLabel);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        m_config.PlayerOutputPath = EditorGUILayout.TextField("Player 输出路径",
                            m_config.PlayerOutputPath);

                        if (GUILayout.Button("浏览", GUILayout.Width(52f)))
                        {
                            string selectedPath = SelectPlayerOutputPath();
                            if (!string.IsNullOrEmpty(selectedPath))
                            {
                                m_config.PlayerOutputPath = MakeProjectRelativePath(selectedPath);
                            }
                        }

                        if (GUILayout.Button("默认", GUILayout.Width(52f)))
                        {
                            m_config.PlayerOutputPath = BuildPipelineConfig.GetDefaultPlayerOutputPath(
                                m_config.BuildTarget, m_config.PackageVersion);
                        }
                    }

                    EditorGUILayout.HelpBox("Player 使用 Build Settings 中配置的场景；构建前会自动切换到所选平台。",
                        MessageType.Info);
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(3f);
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);

            bool canBuild = !m_isBuilding && !EditorApplication.isCompiling
                                             && !EditorApplication.isPlayingOrWillChangePlaymode;
            using (new EditorGUI.DisabledScope(!canBuild))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("仅编译热更 DLL", GUILayout.Height(32f)))
                    {
                        QueueBuild("热更 DLL", () => ReleaseTools.BuildHotFixDll(m_config.BuildTarget));
                    }

                    if (GUILayout.Button("构建 AssetBundle", GUILayout.Height(32f)))
                    {
                        QueueBuild("AssetBundle", () => ReleaseTools.BuildAssetBundles(m_config));
                    }

                    if (GUILayout.Button("仅构建 Player", GUILayout.Height(32f)))
                    {
                        QueueBuild("Player", () => ReleaseTools.BuildPlayer(m_config));
                    }
                }

                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.35f, 0.7f, 1f);
                string fullBuildLabel = m_config.BuildHotFixDll
                    ? "一键构建（DLL + AssetBundle + Player）"
                    : "一键构建（AssetBundle + Player）";
                if (GUILayout.Button(fullBuildLabel, GUILayout.Height(38f)))
                {
                    QueueBuild("完整构建", () => ReleaseTools.BuildAll(m_config));
                }

                GUI.backgroundColor = oldColor;
            }

            if (!canBuild)
            {
                EditorGUILayout.HelpBox("Unity 正在编译、运行或已有构建任务执行中。", MessageType.Info);
            }
        }

        private void DrawBuildLog()
        {
            m_showBuildLog = EditorGUILayout.BeginFoldoutHeaderGroup(m_showBuildLog,
                $"构建日志 ({m_buildLogs.Count})");

            if (m_showBuildLog)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("清空", GUILayout.Width(60f)))
                        {
                            m_buildLogs.Clear();
                        }
                    }

                    m_logScrollPosition = EditorGUILayout.BeginScrollView(m_logScrollPosition,
                        GUILayout.Height(160f));
                    for (int i = 0; i < m_buildLogs.Count; i++)
                    {
                        EditorGUILayout.SelectableLabel(m_buildLogs[i], EditorStyles.miniLabel,
                            GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    }

                    EditorGUILayout.EndScrollView();
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            GUILayout.Space(8f);
        }

        private void QueueBuild(string buildName, Func<bool> buildAction)
        {
            SaveSettings();
            m_isBuilding = true;
            m_showBuildLog = true;
            AddLog($"已提交{buildName}任务");
            EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    ExecuteBuild(buildName, buildAction);
                }
            };
            Repaint();
        }

        private void ExecuteBuild(string buildName, Func<bool> buildAction)
        {
            AddLog($"========== 开始{buildName} ==========");
            AddLog($"平台: {m_config.BuildTarget}，版本: {m_config.PackageVersion}");

            Application.logMessageReceived -= OnBuildLogReceived;
            Application.logMessageReceived += OnBuildLogReceived;

            try
            {
                bool success = buildAction();
                AddLog(success ? $"========== {buildName}成功 ==========" : $"========== {buildName}失败 ==========");
            }
            catch (Exception exception)
            {
                AddLog($"[ERR] {exception.Message}");
                Debug.LogException(exception);
            }
            finally
            {
                Application.logMessageReceived -= OnBuildLogReceived;
                m_isBuilding = false;
                Repaint();
            }
        }

        private void OnBuildLogReceived(string condition, string stackTrace, LogType logType)
        {
            bool isImportant = logType is LogType.Error or LogType.Exception or LogType.Warning;
            bool isBuildLog = condition.Contains("Build", StringComparison.OrdinalIgnoreCase)
                              || condition.Contains("构建")
                              || condition.Contains("AssetBundle", StringComparison.OrdinalIgnoreCase)
                              || condition.Contains("StreamingAssets", StringComparison.OrdinalIgnoreCase)
                              || condition.Contains("DLL", StringComparison.OrdinalIgnoreCase);

            if (!isImportant && !isBuildLog)
            {
                return;
            }

            string prefix = logType switch
            {
                LogType.Error => "[ERR] ",
                LogType.Exception => "[ERR] ",
                LogType.Warning => "[WARN] ",
                LogType.Assert => "[ASSERT] ",
                _ => string.Empty,
            };
            AddLog(prefix + condition);
        }

        private void AddLog(string message)
        {
            if (m_buildLogs.Count >= MAX_LOG_COUNT)
            {
                m_buildLogs.RemoveAt(0);
            }

            m_buildLogs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            m_logScrollPosition = new Vector2(0f, float.MaxValue);
        }

        private void ChangeBuildTarget(BuildTarget buildTarget)
        {
            BuildTarget oldBuildTarget = m_config.BuildTarget;
            string oldDefaultAssetBundlePath = BuildPipelineConfig.GetDefaultAssetBundleOutputRoot(oldBuildTarget);
            string oldDefaultPlayerPath = BuildPipelineConfig.GetDefaultPlayerOutputPath(oldBuildTarget,
                m_config.PackageVersion);

            m_config.BuildTarget = buildTarget;

            if (m_config.AssetBundleOutputRoot == oldDefaultAssetBundlePath)
            {
                m_config.AssetBundleOutputRoot = BuildPipelineConfig.GetDefaultAssetBundleOutputRoot(buildTarget);
            }

            if (m_config.PlayerOutputPath == oldDefaultPlayerPath)
            {
                m_config.PlayerOutputPath = BuildPipelineConfig.GetDefaultPlayerOutputPath(buildTarget,
                    m_config.PackageVersion);
            }
        }

        private void DrawAssetBundleOutputPath()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                m_config.AssetBundleOutputRoot = EditorGUILayout.TextField("AB 输出目录",
                    m_config.AssetBundleOutputRoot);
                if (GUILayout.Button("浏览", GUILayout.Width(52f)))
                {
                    string absolutePath = GetAbsoluteProjectPath(m_config.AssetBundleOutputRoot);
                    string selectedPath = EditorUtility.OpenFolderPanel("选择 AssetBundle 输出目录", absolutePath, string.Empty);
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        m_config.AssetBundleOutputRoot = MakeProjectRelativePath(selectedPath);
                    }
                }

                if (GUILayout.Button("默认", GUILayout.Width(52f)))
                {
                    m_config.AssetBundleOutputRoot = BuildPipelineConfig.GetDefaultAssetBundleOutputRoot(
                        m_config.BuildTarget);
                }
            }
        }

        private string SelectPlayerOutputPath()
        {
            string currentPath = GetAbsoluteProjectPath(m_config.PlayerOutputPath);
            if (m_config.BuildTarget is BuildTarget.iOS or BuildTarget.WebGL)
            {
                return EditorUtility.OpenFolderPanel("选择 Player 输出目录", currentPath, string.Empty);
            }

            return EditorUtility.SaveFilePanel("选择 Player 输出路径", Path.GetDirectoryName(currentPath),
                Path.GetFileName(currentPath), Path.GetExtension(currentPath).TrimStart('.'));
        }

        private void RefreshDefaultAndroidPlayerPath()
        {
            if (m_config.BuildTarget == BuildTarget.Android)
            {
                m_config.PlayerOutputPath = BuildPipelineConfig.GetDefaultPlayerOutputPath(m_config.BuildTarget,
                    m_config.PackageVersion);
            }
        }

        private void ApplyDGameProjectDefaults()
        {
            BuildPipelineConfig defaults = BuildPipelineConfig.CreateDefault(m_config.BuildTarget);
            m_config.PackageName = defaults.PackageName;
            m_config.EncryptionType = defaults.EncryptionType;
            m_config.ForceGenerateAtlas = defaults.ForceGenerateAtlas;
            m_config.ReplaceAssetPathWithAddress = defaults.ReplaceAssetPathWithAddress;
            m_config.CopyToBuildAddress = defaults.CopyToBuildAddress;
            m_config.BuildAddress = defaults.BuildAddress;
            AddLog("已读取 UpdateSettings 与 GameEntry 的项目默认值");
        }

        private void ExecuteLubanConvert()
        {
            if (this == null)
            {
                return;
            }

            if (EditorApplication.ExecuteMenuItem(LUBAN_CONVERT_MENU_ITEM))
            {
                AddLog("已调用 DGame Tools/Luban/转表（客户端 LazyLoad）");
                Repaint();
                return;
            }

            Debug.LogError($"[BuildPipelineWindow] 调用菜单失败: {LUBAN_CONVERT_MENU_ITEM}");
        }

        private void ExecuteHybridCLRGenerateAll()
        {
            if (this == null)
            {
                return;
            }

            if (EditorApplication.ExecuteMenuItem(HYBRIDCLR_GENERATE_ALL_MENU_ITEM))
            {
                AddLog("已调用 HybridCLR/Generate/All");
                Repaint();
                return;
            }

            Debug.LogError($"[BuildPipelineWindow] 调用菜单失败: {HYBRIDCLR_GENERATE_ALL_MENU_ITEM}");
        }

        private static string[] GetCollectorPackageNames(string currentPackageName)
        {
            var packageNames = new List<string>();
            AssetBundleCollectorSetting collectorSetting = AssetBundleCollectorSettingData.Setting;
            if (collectorSetting != null && collectorSetting.Packages != null)
            {
                foreach (AssetBundleCollectorPackage package in collectorSetting.Packages)
                {
                    if (!string.IsNullOrWhiteSpace(package.PackageName)
                        && !packageNames.Contains(package.PackageName))
                    {
                        packageNames.Add(package.PackageName);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(currentPackageName) && !packageNames.Contains(currentPackageName))
            {
                packageNames.Add(currentPackageName);
            }

            if (packageNames.Count == 0)
            {
                packageNames.Add("DefaultPackage");
            }

            return packageNames.ToArray();
        }

        private static void OpenClientConfigGeneratorDirectory()
        {
            string generatorDirectory = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../GameConfig/GenerateTool_Binary"));
            if (!Directory.Exists(generatorDirectory))
            {
                Debug.LogError($"[BuildPipelineWindow] 客户端导表目录不存在: {generatorDirectory}");
                return;
            }

            EditorUtility.RevealInFinder(generatorDirectory);
        }

        private static string GetAbsoluteProjectPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            }

            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
        }

        private static string GetAbsoluteBuildAddress(string buildAddress)
        {
            if (string.IsNullOrWhiteSpace(buildAddress))
            {
                return Application.streamingAssetsPath;
            }

            return Path.IsPathRooted(buildAddress)
                ? Path.GetFullPath(buildAddress)
                : Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, buildAddress));
        }

        private static string MakeProjectRelativePath(string path)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string absolutePath = Path.GetFullPath(path);

            var projectUri = new Uri(projectRoot);
            var pathUri = new Uri(absolutePath);
            string relativePath = Uri.UnescapeDataString(projectUri.MakeRelativeUri(pathUri).ToString());
            return relativePath.StartsWith("../", StringComparison.Ordinal)
                ? absolutePath.Replace('\\', '/')
                : relativePath.Replace('\\', '/');
        }

        private static int GetPlatformIndex(BuildTarget buildTarget)
        {
            for (int i = 0; i < PLATFORM_TARGETS.Length; i++)
            {
                if (PLATFORM_TARGETS[i] == buildTarget)
                {
                    return i;
                }
            }

            return 0;
        }

        private void LoadSettings()
        {
            int platformIndex = Mathf.Clamp(EditorPrefs.GetInt(EDITOR_PREFS_PREFIX + "BuildTarget",
                GetPlatformIndex(BuildTarget.StandaloneWindows64)), 0, PLATFORM_TARGETS.Length - 1);
            BuildTarget buildTarget = PLATFORM_TARGETS[platformIndex];
            BuildPipelineConfig defaults = BuildPipelineConfig.CreateDefault(buildTarget);

            m_config = new BuildPipelineConfig
            {
                BuildTarget = buildTarget,
                BuildPipeline = (EBuildPipeline)EditorPrefs.GetInt(EDITOR_PREFS_PREFIX + "BuildPipeline",
                    (int)defaults.BuildPipeline),
                CompressOption = (ECompressOption)EditorPrefs.GetInt(EDITOR_PREFS_PREFIX + "CompressOption",
                    (int)defaults.CompressOption),
                PackageName = EditorPrefs.GetString(EDITOR_PREFS_PREFIX + "PackageName",
                    defaults.PackageName),
                EncryptionType = (EncryptionType)EditorPrefs.GetInt(EDITOR_PREFS_PREFIX + "EncryptionType",
                    (int)defaults.EncryptionType),
                ForceGenerateAtlas = EditorPrefs.GetBool(EDITOR_PREFS_PREFIX + "ForceGenerateAtlas",
                    defaults.ForceGenerateAtlas),
                ReplaceAssetPathWithAddress = EditorPrefs.GetBool(
                    EDITOR_PREFS_PREFIX + "ReplaceAssetPathWithAddress", defaults.ReplaceAssetPathWithAddress),
                PackageVersion = EditorPrefs.GetString(EDITOR_PREFS_PREFIX + "PackageVersion",
                    defaults.PackageVersion),
                AssetBundleOutputRoot = EditorPrefs.GetString(EDITOR_PREFS_PREFIX + "AssetBundleOutputRoot",
                    defaults.AssetBundleOutputRoot),
                BuildHotFixDll = EditorPrefs.GetBool(EDITOR_PREFS_PREFIX + "BuildHotFixDll",
                    defaults.BuildHotFixDll),
                CopyToBuildAddress = EditorPrefs.GetBool(EDITOR_PREFS_PREFIX + "CopyToBuildAddress",
                    defaults.CopyToBuildAddress),
                BuildAddress = EditorPrefs.GetString(EDITOR_PREFS_PREFIX + "BuildAddress",
                    defaults.BuildAddress),
                MinimalPackage = EditorPrefs.GetBool(EDITOR_PREFS_PREFIX + "MinimalPackage",
                    defaults.MinimalPackage),
                RetainTags = EditorPrefs.GetString(EDITOR_PREFS_PREFIX + "RetainTags",
                    defaults.RetainTags),
                VerifyBuildingResult = EditorPrefs.GetBool(EDITOR_PREFS_PREFIX + "VerifyBuildingResult",
                    defaults.VerifyBuildingResult),
                EnableSharePackRule = EditorPrefs.GetBool(EDITOR_PREFS_PREFIX + "EnableSharePackRule",
                    defaults.EnableSharePackRule),
                FileNameStyle = (EFileNameStyle)EditorPrefs.GetInt(EDITOR_PREFS_PREFIX + "FileNameStyle",
                    (int)defaults.FileNameStyle),
                BuildinFileCopyOption = (EBuildinFileCopyOption)EditorPrefs.GetInt(
                    EDITOR_PREFS_PREFIX + "BuildinFileCopyOption", (int)defaults.BuildinFileCopyOption),
                ClearBuildCacheFiles = EditorPrefs.GetBool(EDITOR_PREFS_PREFIX + "ClearBuildCacheFiles",
                    defaults.ClearBuildCacheFiles),
                UseAssetDependencyDB = EditorPrefs.GetBool(EDITOR_PREFS_PREFIX + "UseAssetDependencyDB",
                    defaults.UseAssetDependencyDB),
                PlayerOutputPath = EditorPrefs.GetString(EDITOR_PREFS_PREFIX + "PlayerOutputPath",
                    defaults.PlayerOutputPath),
                OpenOutputDirectory = EditorPrefs.GetBool(EDITOR_PREFS_PREFIX + "OpenOutputDirectory",
                    defaults.OpenOutputDirectory),
            };
        }

        private void SaveSettings()
        {
            if (m_config == null)
            {
                return;
            }

            EditorPrefs.SetInt(EDITOR_PREFS_PREFIX + "BuildTarget", GetPlatformIndex(m_config.BuildTarget));
            EditorPrefs.SetInt(EDITOR_PREFS_PREFIX + "BuildPipeline", (int)m_config.BuildPipeline);
            EditorPrefs.SetInt(EDITOR_PREFS_PREFIX + "CompressOption", (int)m_config.CompressOption);
            EditorPrefs.SetString(EDITOR_PREFS_PREFIX + "PackageName", m_config.PackageName ?? string.Empty);
            EditorPrefs.SetInt(EDITOR_PREFS_PREFIX + "EncryptionType", (int)m_config.EncryptionType);
            EditorPrefs.SetBool(EDITOR_PREFS_PREFIX + "ForceGenerateAtlas", m_config.ForceGenerateAtlas);
            EditorPrefs.SetBool(EDITOR_PREFS_PREFIX + "ReplaceAssetPathWithAddress",
                m_config.ReplaceAssetPathWithAddress);
            EditorPrefs.SetString(EDITOR_PREFS_PREFIX + "PackageVersion", m_config.PackageVersion ?? string.Empty);
            EditorPrefs.SetString(EDITOR_PREFS_PREFIX + "AssetBundleOutputRoot",
                m_config.AssetBundleOutputRoot ?? string.Empty);
            EditorPrefs.SetBool(EDITOR_PREFS_PREFIX + "BuildHotFixDll", m_config.BuildHotFixDll);
            EditorPrefs.SetBool(EDITOR_PREFS_PREFIX + "CopyToBuildAddress", m_config.CopyToBuildAddress);
            EditorPrefs.SetString(EDITOR_PREFS_PREFIX + "BuildAddress", m_config.BuildAddress ?? string.Empty);
            EditorPrefs.SetBool(EDITOR_PREFS_PREFIX + "MinimalPackage", m_config.MinimalPackage);
            EditorPrefs.SetString(EDITOR_PREFS_PREFIX + "RetainTags", m_config.RetainTags ?? string.Empty);
            EditorPrefs.SetBool(EDITOR_PREFS_PREFIX + "VerifyBuildingResult", m_config.VerifyBuildingResult);
            EditorPrefs.SetBool(EDITOR_PREFS_PREFIX + "EnableSharePackRule", m_config.EnableSharePackRule);
            EditorPrefs.SetInt(EDITOR_PREFS_PREFIX + "FileNameStyle", (int)m_config.FileNameStyle);
            EditorPrefs.SetInt(EDITOR_PREFS_PREFIX + "BuildinFileCopyOption",
                (int)m_config.BuildinFileCopyOption);
            EditorPrefs.SetBool(EDITOR_PREFS_PREFIX + "ClearBuildCacheFiles", m_config.ClearBuildCacheFiles);
            EditorPrefs.SetBool(EDITOR_PREFS_PREFIX + "UseAssetDependencyDB", m_config.UseAssetDependencyDB);
            EditorPrefs.SetString(EDITOR_PREFS_PREFIX + "PlayerOutputPath",
                m_config.PlayerOutputPath ?? string.Empty);
            EditorPrefs.SetBool(EDITOR_PREFS_PREFIX + "OpenOutputDirectory", m_config.OpenOutputDirectory);
        }
    }
}
