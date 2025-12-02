using DGame;
using GameLogic;
using Launcher;
using YooAsset;

namespace Procedure
{
    /// <summary>
    /// 1 - 游戏启动
    /// </summary>
    public class LaunchProcedure : ProcedureBase
    {
        public override bool UseNativeDialog => true;
        private IAudioModule m_audioModule;

        public override void OnCreate(IFsm<IProcedureModule> fsm)
        {
            base.OnCreate(fsm);
            m_audioModule = ModuleSystem.GetModule<IAudioModule>();
        }

        public override void OnEnter()
        {
            DLogger.Info("======== 1-进入游戏启动流程 ========");
            LauncherMgr.Initialize();
            InitAudioModuleSettings();
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            SwitchState<SplashProcedure>();
        }

        public override void OnFixedUpdate()
        {
        }

        public override void OnExit()
        {
        }

        public override void OnDestroy()
        {
        }

        private void InitLanguageSettings()
        {
            if (m_resourceModule.PlayMode == EPlayMode.EditorSimulateMode && RootModule.Instance.EditorLanguage == DGame.Language.Unspecified)
            {
                // 编辑器资源模式直接使用 Inspector 上设置的语言
                return;
            }

            ILocalizationModule localizationModule = ModuleSystem.GetModule<ILocalizationModule>();
            DGame.Language language = localizationModule.CurrentLanguage;
            if (DGame.Utility.PlayerPrefsUtil.HasSetting(Constant.Settings.LANGUAGE))
            {
                try
                {
                    string languageString = DGame.Utility.PlayerPrefsUtil.GetString(Constant.Settings.LANGUAGE);
                    // language = (DGame.Language)System.Enum.Parse(typeof(DGame.Language), languageString);
                    System.Enum.TryParse(languageString, out language);
                }
                catch(System.Exception exception)
                {
                    DLogger.Error("Init language error, reason {0}", exception.ToString());
                }
            }

            if (language != DGame.Language.English
                && language != DGame.Language.ChineseSimplified
                && language != DGame.Language.ChineseTraditional)
            {
                // 若是暂不支持的语言，则使用英语
                language = DGame.Language.English;

                DGame.Utility.PlayerPrefsUtil.SetString(Constant.Settings.LANGUAGE, language.ToString());
                DGame.Utility.PlayerPrefsUtil.Save();
            }

            localizationModule.CurrentLanguage = language;
            DLogger.Info("Init language settings complete, current language is '{0}'.", language.ToString());
        }


        private void InitAudioModuleSettings()
        {
            m_audioModule.MusicEnable = !DGame.Utility.PlayerPrefsUtil.GetBool(Constant.Settings.MUSIC_MUTED, false);
            m_audioModule.MusicVolume = DGame.Utility.PlayerPrefsUtil.GetFloat(Constant.Settings.MUSIC_VOLUME, 1f);
            m_audioModule.SoundEnable = !DGame.Utility.PlayerPrefsUtil.GetBool(Constant.Settings.SOUND_MUTED, false);
            m_audioModule.SoundVolume = DGame.Utility.PlayerPrefsUtil.GetFloat(Constant.Settings.SOUND_VOLUME, 1f);
            m_audioModule.UISoundEnable = !DGame.Utility.PlayerPrefsUtil.GetBool(Constant.Settings.UI_SOUND_MUTED, false);
            m_audioModule.UISoundVolume = DGame.Utility.PlayerPrefsUtil.GetFloat(Constant.Settings.UI_SOUND_VOLUME, 1f);
            DLogger.Info("======== 初始化音频模块完成 ========");
        }
    }
}