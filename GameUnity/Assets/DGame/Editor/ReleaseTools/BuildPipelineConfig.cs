using System;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

namespace DGame
{
    /// <summary>
    /// DGame 可视化构建窗口使用的构建参数。
    /// </summary>
    public sealed class BuildPipelineConfig
    {
        public BuildTarget BuildTarget { get; set; }

        public EBuildPipeline BuildPipeline { get; set; } = EBuildPipeline.ScriptableBuildPipeline;

        public ECompressOption CompressOption { get; set; } = ECompressOption.LZ4;

        public string PackageName { get; set; } = "DefaultPackage";

        public EncryptionType EncryptionType { get; set; } = EncryptionType.None;

        public bool ForceGenerateAtlas { get; set; }

        public bool ReplaceAssetPathWithAddress { get; set; }

        public string PackageVersion { get; set; }

        public string AssetBundleOutputRoot { get; set; }

        public bool BuildHotFixDll { get; set; } = true;

        public bool CopyToBuildAddress { get; set; } = true;

        public string BuildAddress { get; set; }

        public bool MinimalPackage { get; set; }

        public string RetainTags { get; set; } = string.Empty;

        public bool VerifyBuildingResult { get; set; } = true;

        public bool EnableSharePackRule { get; set; } = true;

        public EFileNameStyle FileNameStyle { get; set; } = EFileNameStyle.BundleName_HashName;

        public EBuildinFileCopyOption BuildinFileCopyOption { get; set; } = EBuildinFileCopyOption.ClearAndCopyAll;

        public bool ClearBuildCacheFiles { get; set; }

        public bool UseAssetDependencyDB { get; set; } = true;

        public string PlayerOutputPath { get; set; }

        public bool OpenOutputDirectory { get; set; } = true;

        public static BuildPipelineConfig CreateDefault(BuildTarget buildTarget)
        {
            UpdateSettings updateSettings = Settings.UpdateSettings;
            string packageVersion = GetDefaultPackageVersion();
            return new BuildPipelineConfig
            {
                BuildTarget = buildTarget,
                PackageName = !string.IsNullOrWhiteSpace(updateSettings?.PackageName)
                    ? updateSettings.PackageName
                    : "DefaultPackage",
                EncryptionType = GetDefaultEncryptionType(),
                ForceGenerateAtlas = updateSettings != null && updateSettings.ForceGenerateAtlas,
                ReplaceAssetPathWithAddress = updateSettings != null
                                              && updateSettings.GetReplaceAssetPathWithAddress(),
                PackageVersion = packageVersion,
                AssetBundleOutputRoot = GetDefaultAssetBundleOutputRoot(buildTarget),
                CopyToBuildAddress = updateSettings != null && updateSettings.IsAutoAssetCopyToBuildAddress(),
                BuildAddress = updateSettings != null ? updateSettings.GetBuildAddress() : string.Empty,
                PlayerOutputPath = GetDefaultPlayerOutputPath(buildTarget, packageVersion),
            };
        }

        public static string GetDefaultAssetBundleOutputRoot(BuildTarget buildTarget)
        {
            return $"Bundles/{GetPlatformFolderName(buildTarget)}";
        }

        public static string GetDefaultPlayerOutputPath(BuildTarget buildTarget)
        {
            return GetDefaultPlayerOutputPath(buildTarget, GetDefaultPackageVersion());
        }

        public static string GetDefaultPackageVersion()
        {
            if (Settings.UpdateSettings != null)
            {
                return Settings.UpdateSettings.GetBuildPackageVersion();
            }

            int totalMinutes = DateTime.Now.Hour * 6 + DateTime.Now.Minute;
            return DateTime.Now.ToString("yyyy-MM-dd") + "-" + totalMinutes;
        }

        public static string GetDefaultPlayerOutputPath(BuildTarget buildTarget, string packageVersion)
        {
            return buildTarget switch
            {
                BuildTarget.StandaloneWindows64 => "Build/Windows/Release_Windows.exe",
                BuildTarget.StandaloneOSX => "Build/MacOS/Release_MacOS.app",
                BuildTarget.StandaloneLinux64 => "Build/Linux/Release_Linux",
                BuildTarget.Android => $"Build/Android/{packageVersion}-Android.apk",
                BuildTarget.iOS => "Build/IOS/XCode_Project",
                BuildTarget.WebGL => "Build/WebGL",
                _ => $"Build/{buildTarget}/Release",
            };
        }

        public static BuildTargetGroup GetBuildTargetGroup(BuildTarget buildTarget)
        {
            return buildTarget switch
            {
                BuildTarget.StandaloneWindows64 => BuildTargetGroup.Standalone,
                BuildTarget.StandaloneOSX => BuildTargetGroup.Standalone,
                BuildTarget.StandaloneLinux64 => BuildTargetGroup.Standalone,
                BuildTarget.Android => BuildTargetGroup.Android,
                BuildTarget.iOS => BuildTargetGroup.iOS,
                BuildTarget.WebGL => BuildTargetGroup.WebGL,
                _ => throw new ArgumentOutOfRangeException(nameof(buildTarget), buildTarget, "不支持的构建平台"),
            };
        }

        private static string GetPlatformFolderName(BuildTarget buildTarget)
        {
            return buildTarget switch
            {
                BuildTarget.StandaloneWindows64 => "Windows",
                BuildTarget.StandaloneOSX => "MacOS",
                BuildTarget.StandaloneLinux64 => "Linux",
                BuildTarget.Android => "Android",
                BuildTarget.iOS => "IOS",
                BuildTarget.WebGL => "WebGL",
                _ => buildTarget.ToString(),
            };
        }

        private static EncryptionType GetDefaultEncryptionType()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab GameEntry");
            if (guids.Length == 0)
            {
                return EncryptionType.None;
            }

            string gameEntryPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject gameEntryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(gameEntryPath);
            ResourceModuleDriver resourceModuleDriver = gameEntryPrefab != null
                ? gameEntryPrefab.GetComponentInChildren<ResourceModuleDriver>()
                : null;
            return resourceModuleDriver != null ? resourceModuleDriver.EncryptionType : EncryptionType.None;
        }
    }
}
