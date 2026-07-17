using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

namespace DGame
{
    public static class ReleaseTools
    {
        #region CommandLine Helper

        private static string GetCommandLineArg(string argName)
        {
            string[] args = System.Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals(argName) && i + 1 < args.Length)
                {
                    return args[i + 1];
                }

                // 支持 -arg=value 格式
                if (args[i].StartsWith(argName + "="))
                {
                    string value = args[i].Substring(argName.Length + 1);
                    return value;
                }
            }

            return null;
        }

        #endregion

        #region Build AssetBundle

        [MenuItem("DGame Tools/Build/一键打包AB _F8", priority = 151)]
        public static void BuildCurrentPlatformAB()
        {
            // 编译并复制热更新DLL文件
            BuildDllCommand.BuildAndCopyDlls();
            // 获取当前构建目标平台
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            // 执行AssetBundle构建
            BuildInternal(target, Application.dataPath + "/../Builds/", packageVersion: GetBuildPackageVersion());
            AssetDatabase.Refresh();
            //复制到打包后的StreamingAssets
            CopyStreamingAssetsFiles();
        }

        /// <summary>
        /// 复制StreamingAssets文件去打包目录
        /// </summary>
        private static void CopyStreamingAssetsFiles()
        {
            if (!Settings.UpdateSettings.IsAutoAssetCopyToBuildAddress())
            {
                Debug.Log("[CopyStreamingAssetsFiles] UpdateSettings.IsAutoAssetCopyToBuildAddress关闭，不会生产到打包目录中");
                return;
            }

            // 获取StreamingAssets路径
            string streamingAssetsPath = Application.streamingAssetsPath;
            // 目标路径 可以是任何目录路径
            string targetPath = Settings.UpdateSettings.GetBuildAddress();

            if (!System.IO.Path.IsPathRooted(targetPath))
            {
                // 如果是相对路径，结合 StreamingAssets 的路径进行合并
                targetPath = System.IO.Path.Combine(streamingAssetsPath, targetPath).Replace("\\", "/");
            }

            if (!Directory.Exists(targetPath))
            {
                Debug.LogError($"[CopyStreamingAssetsFiles] 打包目录不存在，检查UpdateSettings.m_buildAddress: {targetPath}");
                return;
            }

            // 删除目标目录下的所有文件
            string[] deleteFiles = Directory.GetFiles(targetPath);

            foreach (var filePath in deleteFiles)
            {
                File.Delete(filePath);
                Debug.Log($"[CopyStreamingAssetsFiles] 删除文件: {filePath}");
            }

            // 删除目录下的所有子目录
            string[] directories = Directory.GetDirectories(targetPath);

            foreach (var directory in directories)
            {
                Directory.Delete(directory, true); // true：递归删除子目录及其内容
                Debug.Log($"[CopyStreamingAssetsFiles] 删除目录: {directory}");
            }

            // 获取StreamingAssets中的所有文件，排除.meta文件
            string[] files = Directory.GetFiles(streamingAssetsPath, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                if (file.EndsWith(".meta"))
                {
                    continue;
                }

                // 获取相对路径 用于目标目录中创建相同的文件夹结构
                string relativePath = file.Substring(streamingAssetsPath.Length + 1);
                string destinationPath = Path.Combine(targetPath, relativePath);
                // 确保目标文件夹存在
                string destinationDir = Path.GetDirectoryName(destinationPath);

                if (!Directory.Exists(destinationDir) && !string.IsNullOrEmpty(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                // 复制文件
                File.Copy(file, destinationPath, true); // true：覆盖存在的文件
            }

            Debug.Log($"[CopyStreamingAssetsFiles] 复制文件成功: {targetPath}");
        }

        /// <summary>
        /// 生成构建包版本号
        /// 格式：yyyy-MM-dd-分钟段（每10分钟一个段）
        /// 例如：1997-01-01-919 表示1997年1月1日的第919个10分钟段
        /// </summary>
        /// <returns></returns>
        private static string GetBuildPackageVersion()
        {
            if (Settings.UpdateSettings != null)
            {
                return Settings.UpdateSettings.GetBuildPackageVersion();
            }

            return GetAutoBuildPackageVersionFallback();
        }

        private static string GetAutoBuildPackageVersionFallback()
        {
            // 计算当天从0点开始的总分钟数，然后除以10得到段数
            int totalMinutes = DateTime.Now.Hour * 6 + DateTime.Now.Minute;
            return DateTime.Now.ToString("yyyy-MM-dd") + "-" + totalMinutes;
        }

        /// <summary>
        /// 内部构建方法，执行具体的AssetBundle打包流程
        /// </summary>
        /// <param name="buildTarget">目标构建平台</param>
        /// <param name="outputRoot">输出根目录</param>
        /// <param name="packageVersion">包版本号</param>
        /// <param name="buildPipeline">构建管线类型，默认使用可编程构建管线</param>
        private static void BuildInternal(BuildTarget buildTarget, string outputRoot, string packageVersion = "1.0",
            EBuildPipeline buildPipeline = EBuildPipeline.ScriptableBuildPipeline)
        {
            if (Settings.UpdateSettings.ForceGenerateAtlas)
            {
                Debug.Log($"[BuildInternal] 强制重新生成所有图集");
                EditorSpriteSaveInfo.ForceGenerateAll(true);
            }

            Debug.Log($"[BuildInternal] 开始构建AssetBundle: {buildTarget}");

            IBuildPipeline pipeline = null;
            BuildParameters buildParameters = null;

            // 根据构建管线类型创建对应的参数和管线实例
            if (buildPipeline == EBuildPipeline.BuiltinBuildPipeline)
            {
                // 使用Unity内置构建管线
                BuiltinBuildParameters builtinParameters = new BuiltinBuildParameters();
                pipeline = new BuiltinBuildPipeline();
                buildParameters = builtinParameters;
                // 使用LZ4压缩，平衡压缩率和加载速度
                builtinParameters.CompressOption = ECompressOption.LZ4;
            }
            else
            {
                // 使用可编程构建管线（推荐，功能更强大）
                ScriptableBuildParameters scriptableBuildParameters = new ScriptableBuildParameters();
                pipeline = new ScriptableBuildPipeline();
                buildParameters = scriptableBuildParameters;
                scriptableBuildParameters.CompressOption = ECompressOption.LZ4;
                // 设置内置着色器资源包名称，避免重复打包着色器
                scriptableBuildParameters.BuiltinShadersBundleName = GetBuiltinShaderBundleName("DefaultPackage");
                scriptableBuildParameters.ReplaceAssetPathWithAddress =
                    Settings.UpdateSettings.GetReplaceAssetPathWithAddress();
            }

            // 配置构建参数
            buildParameters.BuildOutputRoot =
                outputRoot; //AssetBundleBuilderHelper.GetDefaultBuildOutputRoot(); // 构建输出目录
            buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot(); // 内置文件根目录
            buildParameters.BuildPipeline = buildPipeline.ToString(); // 构建管线名称
            buildParameters.BuildTarget = buildTarget; // 目标平台
            buildParameters.BuildBundleType = (int)EBuildBundleType.AssetBundle; // 资源包类型
            buildParameters.PackageName = "DefaultPackage"; // 包名称
            buildParameters.PackageVersion = packageVersion; // 包版本
            buildParameters.VerifyBuildingResult = true; // 验证构建结果
            // 启动共享资源打包
            buildParameters.EnableSharePackRule = true; // 启用共享资源打包规则
            buildParameters.FileNameStyle = EFileNameStyle.BundleName_HashName; // 文件名风格：包名_哈希值
            buildParameters.BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll; // 清理并复制所有内置文件
            buildParameters.BuildinFileCopyParams = string.Empty; // 内置文件复制参数
            buildParameters.EncryptionServices = GetEncryptionFromResourceModuleDriver(); // 加密服务
            buildParameters.ClearBuildCacheFiles = false; // 不清理构建缓存，启用增量构建，可以提高打包速度
            buildParameters.UseAssetDependencyDB = true; // 使用资源依赖关系数据库，可以提高打包速度

            // 执行构建流程
            var buildResult = pipeline.Run(buildParameters, true);

            if (buildResult.Success)
            {
                Debug.Log($"[BuildInternal] AssetBundle资源构建成功: {buildResult.OutputPackageDirectory}");
            }
            else
            {
                Debug.LogError($"[BuildInternal] AssetBundle资源构建失败: {buildResult.ErrorInfo}");
            }
        }

        /// <summary>
        /// 创建加密服务实例
        /// 用于AssetBundle文件加密
        /// </summary>
        private static IEncryptionServices CreateEncryptionInstance(string packageName, EBuildPipeline buildPipeline)
        {
            // 从配置中获取加密类名
            var encryptionClassName =
                AssetBundleBuilderSetting.GetPackageEncyptionServicesClassName(packageName, buildPipeline.ToString());
            // 获取所有实现了IEncryptionServices接口的类型
            var encryptionClassTypes = EditorTools.GetAssignableTypes(typeof(IEncryptionServices));
            // 查找匹配的加密类
            var classType =
                encryptionClassTypes.Find(x => x.FullName != null && x.FullName.Equals(encryptionClassName));

            if (classType != null)
            {
                Debug.Log($"[BuildInternal] Use Encryption: {classType}");
                // 创建加密服务实例
                return (IEncryptionServices)Activator.CreateInstance(classType);
            }

            return null;
        }

        /// <summary>
        /// 根据 ResourceModuleDriver 的 encryptionType 获取对应的加密服务
        /// </summary>
        private static IEncryptionServices GetEncryptionFromResourceModuleDriver()
        {
            // 通过名字查找 GameEntry 预制体
            var guids = AssetDatabase.FindAssets("t:Prefab GameEntry");

            if (guids.Length == 0)
            {
                Debug.LogWarning("[BuildInternal] Failed to find GameEntry.prefab");
                return null;
            }

            var gameEntryPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var gameEntryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(gameEntryPath);

            if (gameEntryPrefab == null)
            {
                Debug.LogWarning("[BuildInternal] Failed to load GameEntry.prefab");
                return null;
            }

            var resourceModuleDriver = gameEntryPrefab.GetComponentInChildren<ResourceModuleDriver>();

            if (resourceModuleDriver == null)
            {
                Debug.LogWarning("[BuildInternal] ResourceModuleDriver not found in GameEntry.prefab");
                return null;
            }

            var encryptionType = resourceModuleDriver.EncryptionType;
            Debug.Log($"[BuildInternal] Use EncryptionType from ResourceModuleDriver: {encryptionType}");

            return encryptionType switch
            {
                EncryptionType.FileOffset => new FileOffsetEncryption(),
                EncryptionType.FileStream => new FileStreamEncryption(),
                _ => null // EncryptionType.None
            };
        }

        /// <summary>
        /// 获取内置着色器资源包名称
        /// 注意：需要和自动收集的着色器资源包名保持一致
        /// 避免着色器被重复打包到多个AB中
        /// </summary>
        private static string GetBuiltinShaderBundleName(string packageName)
        {
            // 获取唯一包名设置
            var uniqueBundleName = AssetBundleCollectorSettingData.Setting.UniqueBundleName;
            // 创建着色器打包规则结果
            var packRuleResult = DefaultPackRule.CreateShadersPackRuleResult();
            // 生成着色器资源包名称
            return packRuleResult.GetBundleName(packageName, uniqueBundleName);
        }

        #endregion

        #region Build Pipeline Window

        /// <summary>
        /// 为指定平台编译并复制热更新 DLL。
        /// </summary>
        public static bool BuildHotFixDll(BuildTarget buildTarget)
        {
            if (!SwitchActiveBuildTarget(buildTarget))
            {
                return false;
            }

            BuildDllCommand.BuildAndCopyDlls();
            AssetDatabase.Refresh();
            return true;
        }

        /// <summary>
        /// 使用窗口参数构建 AssetBundle。
        /// </summary>
        public static bool BuildAssetBundles(BuildPipelineConfig config)
        {
            return ExecuteBuild(config, true, false);
        }

        /// <summary>
        /// 使用窗口参数仅构建 Player。
        /// </summary>
        public static bool BuildPlayer(BuildPipelineConfig config)
        {
            return ExecuteBuild(config, false, true);
        }

        /// <summary>
        /// 使用窗口参数依次构建 AssetBundle 和 Player。
        /// </summary>
        public static bool BuildAll(BuildPipelineConfig config)
        {
            return ExecuteBuild(config, true, true);
        }

        private static bool ExecuteBuild(BuildPipelineConfig config, bool buildAssetBundle, bool buildPlayer)
        {
            if (!ValidateBuildConfig(config, buildAssetBundle, buildPlayer))
            {
                return false;
            }

            if (!SwitchActiveBuildTarget(config.BuildTarget))
            {
                return false;
            }

            if (buildAssetBundle)
            {
                if (config.BuildHotFixDll)
                {
                    BuildDllCommand.BuildAndCopyDlls();
                }

                AssetDatabase.Refresh();
                YooAsset.Editor.BuildResult buildResult = BuildInternal(config);

                if (!buildResult.Success)
                {
                    return false;
                }

                if (config.MinimalPackage
                    && !ProcessMinimalPackage(config, buildResult.OutputPackageDirectory))
                {
                    return false;
                }

                AssetDatabase.Refresh();

                if (config.CopyToBuildAddress && !CopyStreamingAssetsFiles(config))
                {
                    return false;
                }
            }

            if (buildPlayer)
            {
                string playerOutputPath = GetAbsoluteProjectPath(config.PlayerOutputPath);

                if (!BuildPlayerWithConfig(config.BuildTarget, playerOutputPath))
                {
                    return false;
                }
            }

            if (config.OpenOutputDirectory)
            {
                string outputPath = buildPlayer
                    ? GetPlayerOutputDirectory(GetAbsoluteProjectPath(config.PlayerOutputPath), config.BuildTarget)
                    : GetAbsoluteProjectPath(config.AssetBundleOutputRoot);
                OpenBuildSavePath(outputPath);
            }

            return true;
        }

        private static YooAsset.Editor.BuildResult BuildInternal(BuildPipelineConfig config)
        {
            if (config.ForceGenerateAtlas)
            {
                Debug.Log("[BuildInternal] 强制重新生成所有图集");
                EditorSpriteSaveInfo.ForceGenerateAll(true);
            }

            Debug.Log($"[BuildInternal] 开始构建AssetBundle: {config.BuildTarget}");

            IBuildPipeline pipeline;
            BuildParameters buildParameters;

            if (config.BuildPipeline == EBuildPipeline.BuiltinBuildPipeline)
            {
                var builtinBuildParameters = new BuiltinBuildParameters
                {
                    CompressOption = config.CompressOption,
                };
                pipeline = new BuiltinBuildPipeline();
                buildParameters = builtinBuildParameters;
            }
            else
            {
                var scriptableBuildParameters = new ScriptableBuildParameters
                {
                    CompressOption = config.CompressOption,
                    BuiltinShadersBundleName = GetBuiltinShaderBundleName(config.PackageName),
                    ReplaceAssetPathWithAddress = config.ReplaceAssetPathWithAddress,
                };
                pipeline = new ScriptableBuildPipeline();
                buildParameters = scriptableBuildParameters;
            }

            buildParameters.BuildOutputRoot = GetAbsoluteProjectPath(config.AssetBundleOutputRoot);
            buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            buildParameters.BuildPipeline = config.BuildPipeline.ToString();
            buildParameters.BuildTarget = config.BuildTarget;
            buildParameters.BuildBundleType = (int)EBuildBundleType.AssetBundle;
            buildParameters.PackageName = config.PackageName.Trim();
            buildParameters.PackageVersion = config.PackageVersion.Trim();
            buildParameters.VerifyBuildingResult = config.VerifyBuildingResult;
            buildParameters.EnableSharePackRule = config.EnableSharePackRule;
            buildParameters.FileNameStyle = config.FileNameStyle;
            buildParameters.BuildinFileCopyOption = config.BuildinFileCopyOption;
            buildParameters.BuildinFileCopyParams = string.Empty;
            buildParameters.EncryptionServices = GetEncryptionFromType(config.EncryptionType);
            buildParameters.ClearBuildCacheFiles = config.ClearBuildCacheFiles;
            buildParameters.UseAssetDependencyDB = config.UseAssetDependencyDB;

            YooAsset.Editor.BuildResult buildResult = pipeline.Run(buildParameters, true);

            if (buildResult.Success)
            {
                Debug.Log($"[BuildInternal] AssetBundle资源构建成功: {buildResult.OutputPackageDirectory}");
            }
            else
            {
                Debug.LogError($"[BuildInternal] AssetBundle资源构建失败: {buildResult.ErrorInfo}");
            }

            return buildResult;
        }

        private static bool ProcessMinimalPackage(BuildPipelineConfig config, string outputPackageDirectory)
        {
            string reportFileName = YooAssetSettingsData.GetBuildReportFileName(config.PackageName,
                config.PackageVersion);
            string reportPath = Path.Combine(outputPackageDirectory, reportFileName);
            if (!File.Exists(reportPath))
            {
                Debug.LogError($"[最小包] 未找到构建报告: {reportPath}");
                return false;
            }

            YooAsset.Editor.BuildReport buildReport;
            try
            {
                buildReport = YooAsset.Editor.BuildReport.Deserialize(File.ReadAllText(reportPath));
            }
            catch (Exception exception)
            {
                Debug.LogError($"[最小包] 读取构建报告失败: {exception.Message}");
                return false;
            }

            string[] retainTags = ParseRetainTags(config.RetainTags);
            var retainFileNames = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (retainTags.Length > 0)
            {
                foreach (ReportBundleInfo bundleInfo in buildReport.BundleInfos)
                {
                    if (bundleInfo.Tags != null && bundleInfo.Tags.Any(retainTags.Contains))
                    {
                        retainFileNames.Add(bundleInfo.FileName);
                    }
                }
            }

            string streamingAssetsRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            if (!Directory.Exists(streamingAssetsRoot))
            {
                Debug.LogError($"[最小包] StreamingAssets 目录不存在: {streamingAssetsRoot}");
                return false;
            }

            int deletedCount = 0;
            int retainedCount = 0;
            foreach (string bundleFile in Directory.GetFiles(streamingAssetsRoot, "*.bundle",
                         SearchOption.AllDirectories))
            {
                if (retainFileNames.Contains(Path.GetFileName(bundleFile)))
                {
                    retainedCount++;
                    continue;
                }

                File.Delete(bundleFile);
                deletedCount++;
            }

            CleanEmptyDirectories(streamingAssetsRoot);
            Debug.Log($"[最小包] 处理完成，删除 {deletedCount} 个 Bundle，保留 {retainedCount} 个 Bundle");
            return true;
        }

        private static string[] ParseRetainTags(string retainTags)
        {
            if (string.IsNullOrWhiteSpace(retainTags))
            {
                return Array.Empty<string>();
            }

            return retainTags.Split(',', '，')
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrEmpty(tag))
                .Distinct()
                .ToArray();
        }

        private static void CleanEmptyDirectories(string rootPath)
        {
            foreach (string directory in Directory.GetDirectories(rootPath))
            {
                CleanEmptyDirectories(directory);
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
        }

        private static bool ValidateBuildConfig(BuildPipelineConfig config, bool buildAssetBundle, bool buildPlayer)
        {
            if (config == null)
            {
                Debug.LogError("[ReleaseTools] 构建配置不能为空");
                return false;
            }

            if (buildAssetBundle && string.IsNullOrWhiteSpace(config.PackageVersion))
            {
                Debug.LogError("[ReleaseTools] AssetBundle 版本号不能为空");
                return false;
            }

            if (buildAssetBundle && string.IsNullOrWhiteSpace(config.PackageName))
            {
                Debug.LogError("[ReleaseTools] YooAsset 资源包名不能为空");
                return false;
            }

            if (buildAssetBundle && string.IsNullOrWhiteSpace(config.AssetBundleOutputRoot))
            {
                Debug.LogError("[ReleaseTools] AssetBundle 输出目录不能为空");
                return false;
            }

            if (buildPlayer && string.IsNullOrWhiteSpace(config.PlayerOutputPath))
            {
                Debug.LogError("[ReleaseTools] Player 输出路径不能为空");
                return false;
            }

            if (buildAssetBundle && config.CopyToBuildAddress && string.IsNullOrWhiteSpace(config.BuildAddress))
            {
                Debug.LogError("[ReleaseTools] 启用内置资源同步时 BuildAddress 不能为空");
                return false;
            }

            return true;
        }

        private static bool SwitchActiveBuildTarget(BuildTarget buildTarget)
        {
            if (EditorUserBuildSettings.activeBuildTarget == buildTarget)
            {
                return true;
            }

            BuildTargetGroup buildTargetGroup = BuildPipelineConfig.GetBuildTargetGroup(buildTarget);
            if (EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget))
            {
                return true;
            }

            Debug.LogError($"[ReleaseTools] 切换构建平台失败: {buildTarget}");
            return false;
        }

        private static string GetAbsoluteProjectPath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path).Replace('\\', '/');
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, path)).Replace('\\', '/');
        }

        private static string GetPlayerOutputDirectory(string playerOutputPath, BuildTarget buildTarget)
        {
            return buildTarget is BuildTarget.iOS or BuildTarget.WebGL
                ? playerOutputPath
                : Path.GetDirectoryName(playerOutputPath);
        }

        private static IEncryptionServices GetEncryptionFromType(EncryptionType encryptionType)
        {
            return encryptionType switch
            {
                EncryptionType.FileOffset => new FileOffsetEncryption(),
                EncryptionType.FileStream => new FileStreamEncryption(),
                _ => null,
            };
        }

        private static bool CopyStreamingAssetsFiles(BuildPipelineConfig config)
        {
            string streamingAssetsPath = Path.GetFullPath(Application.streamingAssetsPath);
            string targetPath = config.BuildAddress.Trim();

            if (!Directory.Exists(streamingAssetsPath))
            {
                Debug.LogError($"[CopyStreamingAssetsFiles] StreamingAssets 目录不存在: {streamingAssetsPath}");
                return false;
            }

            if (!Path.IsPathRooted(targetPath))
            {
                targetPath = Path.Combine(streamingAssetsPath, targetPath);
            }

            targetPath = Path.GetFullPath(targetPath);
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (IsSameOrSubPath(streamingAssetsPath, targetPath)
                || IsSameOrSubPath(targetPath, streamingAssetsPath)
                || IsSameOrSubPath(targetPath, projectRoot))
            {
                Debug.LogError($"[CopyStreamingAssetsFiles] BuildAddress 与工程或 StreamingAssets 路径存在危险包含关系: {targetPath}");
                return false;
            }

            Directory.CreateDirectory(targetPath);

            foreach (string filePath in Directory.GetFiles(targetPath))
            {
                File.Delete(filePath);
            }

            foreach (string directory in Directory.GetDirectories(targetPath))
            {
                Directory.Delete(directory, true);
            }

            foreach (string filePath in Directory.GetFiles(streamingAssetsPath, "*", SearchOption.AllDirectories))
            {
                if (filePath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string relativePath = filePath.Substring(streamingAssetsPath.Length + 1);
                string destinationPath = Path.Combine(targetPath, relativePath);
                string destinationDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                File.Copy(filePath, destinationPath, true);
            }

            Debug.Log($"[CopyStreamingAssetsFiles] 复制文件成功: {targetPath}");
            return true;
        }

        private static bool IsSameOrSubPath(string parentPath, string childPath)
        {
            string normalizedParent = Path.GetFullPath(parentPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string normalizedChild = Path.GetFullPath(childPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            return normalizedChild.StartsWith(normalizedParent, StringComparison.OrdinalIgnoreCase);
        }

        private static bool BuildPlayerWithConfig(BuildTarget buildTarget, string locationPathName)
        {
            string outputDirectory = Path.GetDirectoryName(locationPathName);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            BuildTargetGroup buildTargetGroup = BuildPipelineConfig.GetBuildTargetGroup(buildTarget);
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray(),
                locationPathName = locationPathName,
                target = buildTarget,
                targetGroup = buildTargetGroup,
                options = BuildOptions.None,
            };
            BuildSummary summary = BuildPipeline.BuildPlayer(buildPlayerOptions).summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"Build {buildTarget} Succeeded: {summary.totalSize / 1024 / 1024}MB");
                return true;
            }

            Debug.LogError($"Build {buildTarget} Failed: {summary.result}");
            return false;
        }

        #endregion

        #region GetBuildTarget

        public static BuildTarget GetBuildTarget(string platform)
            => platform switch
            {
                "Android" => BuildTarget.Android,
                "IOS" => BuildTarget.iOS,
                "Windows" => BuildTarget.StandaloneWindows64,
                "MacOS" => BuildTarget.StandaloneOSX,
                "Linux" => BuildTarget.StandaloneLinux64,
                "WebGL" => BuildTarget.WebGL,
                "Switch" => BuildTarget.Switch,
                "PS4" => BuildTarget.PS4,
                "PS5" => BuildTarget.PS5,
                _ => BuildTarget.NoTarget
            };

        #endregion

        #region Build

        [MenuItem("DGame Tools/Build/AutoBuildWindow", priority = 152)]
        public static void AutoBuildWindow()
        {
            // 编译并复制热更新DLL文件
            BuildDllCommand.BuildAndCopyDlls();
            BuildTarget target = BuildTarget.StandaloneWindows;
            AssetDatabase.Refresh();
            BuildInternal(target, Application.dataPath + "/../Bundles/Windows",
                packageVersion: GetBuildPackageVersion());
            AssetDatabase.Refresh();
            var savePath = $"{Application.dataPath}/../Build/Windows/";
            BuildImp(BuildTargetGroup.Standalone, target, savePath + "Release_Windows.exe");
            OpenBuildSavePath(savePath);
        }

        [MenuItem("DGame Tools/Build/AutoBuildAndroid", priority = 153)]
        public static void AutoBuildAndroid()
        {
            // 编译并复制热更新DLL文件
            BuildDllCommand.BuildAndCopyDlls();
            BuildTarget target = BuildTarget.Android;
            AssetDatabase.Refresh();
            BuildInternal(target, Application.dataPath + "/../Bundles/Android",
                packageVersion: GetBuildPackageVersion());
            AssetDatabase.Refresh();
            var savePath = $"{Application.dataPath}/../Build/Android/";
            BuildImp(BuildTargetGroup.Android, target, savePath + $"{GetBuildPackageVersion()}-Android.apk");
            OpenBuildSavePath(savePath);
        }

        [MenuItem("DGame Tools/Build/AutoBuildIOS", priority = 154)]
        public static void AutoBuildIOS()
        {
            // 编译并复制热更新DLL文件
            BuildDllCommand.BuildAndCopyDlls();
            BuildTarget target = BuildTarget.iOS;
            AssetDatabase.Refresh();
            BuildInternal(target, Application.dataPath + "/../Bundles/IOS", packageVersion: GetBuildPackageVersion());
            AssetDatabase.Refresh();
            var savePath = $"{Application.dataPath}/../Build/IOS/";
            BuildImp(BuildTargetGroup.iOS, target, savePath + "XCode_Project");
            OpenBuildSavePath(savePath);
        }

        private static void OpenBuildSavePath(string path)
        {
            if (!Directory.Exists(path))
            {
                Debug.LogWarning($"构建目录不存在: {path}");
                return;
            }

            string absolutePath = Path.GetFullPath(path);
            EditorUtility.RevealInFinder(absolutePath);
        }

        private static void BuildImp(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget,
            string locationPathName)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);
            AssetDatabase.Refresh();
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
            {
                scenes = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray(),
                locationPathName = locationPathName,
                target = buildTarget,
                targetGroup = buildTargetGroup,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"Build {buildTarget.ToString()} Succeeded: {summary.totalSize / 1024 / 1024}MB");
            }
            else
            {
                Debug.LogError($"Build {buildTarget.ToString()} Failed: {summary.result}");
            }
        }

        #endregion

        #region Build AssetBundle by Command

        public static void BuildWindowWithVersion()
        {
            string version = GetCommandLineArg("-version");

            if (string.IsNullOrEmpty(version))
            {
                Debug.LogError("[BuildWindowWithVersion] Please specify version using -version argument");
                return;
            }

            // 编译并复制热更新DLL文件
            BuildDllCommand.BuildAndCopyDlls();
            BuildTarget target = BuildTarget.StandaloneWindows;
            AssetDatabase.Refresh();
            BuildInternal(target, Application.dataPath + "/../Bundles/Windows", packageVersion: version);
            AssetDatabase.Refresh();
            var savePath = $"{Application.dataPath}/../Build/Windows/";
            BuildImp(BuildTargetGroup.Standalone, target, savePath + "Release_Windows.exe");
            OpenBuildSavePath(savePath);
        }

        public static void BuildAndroidWithVersion()
        {
            string version = GetCommandLineArg("-version");

            if (string.IsNullOrEmpty(version))
            {
                Debug.LogError("[BuildAndroidWithVersion] Please specify version using -version argument");
                return;
            }

            // 编译并复制热更新DLL文件
            BuildDllCommand.BuildAndCopyDlls();
            BuildTarget target = BuildTarget.Android;
            AssetDatabase.Refresh();
            BuildInternal(target, Application.dataPath + "/../Bundles/Android", packageVersion: version);
            AssetDatabase.Refresh();
            var savePath = $"{Application.dataPath}/../Build/Android/";
            BuildImp(BuildTargetGroup.Android, target, savePath + $"{GetBuildPackageVersion()}-Android.apk");
            OpenBuildSavePath(savePath);
        }

        public static void BuildIOSWithVersion()
        {
            string version = GetCommandLineArg("-version");

            if (string.IsNullOrEmpty(version))
            {
                Debug.LogError("[BuildIOSWithVersion] Please specify version using -version argument");
                return;
            }

            // 编译并复制热更新DLL文件
            BuildDllCommand.BuildAndCopyDlls();
            BuildTarget target = BuildTarget.iOS;
            AssetDatabase.Refresh();
            BuildInternal(target, Application.dataPath + "/../Bundles/IOS", packageVersion: version);
            AssetDatabase.Refresh();
            var savePath = $"{Application.dataPath}/../Build/IOS/";
            BuildImp(BuildTargetGroup.iOS, target, savePath + "XCode_Project");
            OpenBuildSavePath(savePath);
        }

        /// <summary>
        /// 打包安卓AB（自动版本号）
        /// </summary>
        public static void BuildAndroidAB()
        {
            BuildDllCommand.BuildAndCopyDlls();
            BuildTarget target = BuildTarget.Android;
            BuildInternal(target, Application.dataPath + "/../Bundles/Android",
                packageVersion: GetBuildPackageVersion());
            AssetDatabase.Refresh();
            CopyStreamingAssetsFiles();
            Debug.Log("[BuildAndroidAB] Android AssetBundle build completed with auto version: " + GetBuildPackageVersion());
        }

        /// <summary>
        /// 打包安卓AB（手动版本号，通过命令行参数 -version 传入）
        /// </summary>
        public static void BuildAndroidABWithVersion()
        {
            string version = GetCommandLineArg("-version");

            if (string.IsNullOrEmpty(version))
            {
                Debug.LogError("[BuildAndroidABWithVersion] Please specify version using -version argument");
                return;
            }

            BuildDllCommand.BuildAndCopyDlls();
            BuildTarget target = BuildTarget.Android;
            BuildInternal(target, Application.dataPath + "/../Bundles/Android", packageVersion: version);
            AssetDatabase.Refresh();
            CopyStreamingAssetsFiles();
            Debug.Log($"[BuildAndroidABWithVersion] Android AssetBundle build completed with manual version: {version}");
        }

        /// <summary>
        /// 打包Windows AB（自动版本号）
        /// </summary>
        public static void BuildWindowsAB()
        {
            BuildDllCommand.BuildAndCopyDlls();
            BuildTarget target = BuildTarget.StandaloneWindows;
            BuildInternal(target, Application.dataPath + "/../Bundles/Windows",
                packageVersion: GetBuildPackageVersion());
            AssetDatabase.Refresh();
            CopyStreamingAssetsFiles();
            Debug.Log($"[BuildWindowsAB] Windows AssetBundle build completed with auto version: {GetBuildPackageVersion()}");
        }

        /// <summary>
        /// 打包Windows AB（手动版本号，通过命令行参数 -version 传入）
        /// </summary>
        public static void BuildWindowsABWithVersion()
        {
            string version = GetCommandLineArg("-version");

            if (string.IsNullOrEmpty(version))
            {
                Debug.LogError("[BuildWindowsABWithVersion] Please specify version using -version argument");
                return;
            }

            BuildDllCommand.BuildAndCopyDlls();
            BuildTarget target = BuildTarget.StandaloneWindows;
            BuildInternal(target, Application.dataPath + "/../Bundles/Windows", packageVersion: version);
            AssetDatabase.Refresh();
            CopyStreamingAssetsFiles();
            Debug.Log($"[BuildWindowsABWithVersion] Windows AssetBundle build completed with manual version: {version}");
        }

        #endregion
    }
}
