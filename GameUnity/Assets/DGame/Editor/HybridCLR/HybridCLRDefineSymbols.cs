#if ENABLE_HYBRIDCLR

using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Installer;

#endif

using UnityEditor;

namespace DGame
{
    public static class HybridCLRDefineSymbols
    {
        private const string ENABLE_HYBRIDCLR_SCRIPTING_DEFINE_SYMBOLS= "ENABLE_HYBRIDCLR";
        private const string ENABLE_OBFUZ_SCRIPTING_DEFINE_SYMBOLS = "ENABLE_OBFUZ";

        [MenuItem("DGame Tools/HybridCLR/启用HybridCLR")]
        public static void EnableHybridCLR()
        {
            ScriptingDefineSymbolsTools.EnableScriptingDefineSymbol(ENABLE_HYBRIDCLR_SCRIPTING_DEFINE_SYMBOLS);
#if ENABLE_HYBRIDCLR

            var controller = new InstallerController();
            if (!controller.HasInstalledHybridCLR())
            {
                controller.InstallDefaultHybridCLR();
            }

            HybridCLR.Editor.SettingsUtil.Enable = true;
            UpdateSettingsInspector.ForceUpdateAssemblies();

#endif
        }

        [MenuItem("DGame Tools/HybridCLR/禁用HybridCLR")]
        public static void DisableHybridCLR()
        {
            ScriptingDefineSymbolsTools.DisableScriptingDefineSymbol(ENABLE_HYBRIDCLR_SCRIPTING_DEFINE_SYMBOLS);
#if ENABLE_HYBRIDCLR

            HybridCLR.Editor.SettingsUtil.Enable = false;

#endif
        }

#if ENABLE_OBFUZ
        [MenuItem("DGame Tools/HybridCLR/启用Obfuz")]
        public static void EnableObfuz()
        {
            ScriptingDefineSymbolsTools.EnableScriptingDefineSymbol(ENABLE_OBFUZ_SCRIPTING_DEFINE_SYMBOLS);
#if ENABLE_OBFUZ

#endif
        }

        [MenuItem("DGame Tools/HybridCLR/禁用Obfuz")]
        public static void DisableObfuz()
        {
            ScriptingDefineSymbolsTools.DisableScriptingDefineSymbol(ENABLE_OBFUZ_SCRIPTING_DEFINE_SYMBOLS);
#if ENABLE_OBFUZ

#endif
        }
#endif
    }
}