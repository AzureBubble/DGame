using System;
using System.Collections.Generic;
using DGame.I2.Loc;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [Serializable]
    public class UITextLocalizationExtend
    {
        [SerializeField] private bool m_useI2Localization;

        [SerializeField] private bool m_hasParams;

        [SerializeField] private TextDefine m_textDefine;

        private Localize m_localize;
        private LocalizationParamsManager m_localizationParamsManager;
        private Text m_text;

        public TextDefine UITextDefine
        {
            get => m_textDefine;
            set
            {
                if (m_textDefine != value)
                {
                    m_textDefine = value;
                    // SetTerm(m_textDefine);
                }
            }
        }

        public bool UseI2Localization
        {
            get => m_useI2Localization;
            set
            {
                if (m_useI2Localization != value)
                {
                    m_useI2Localization = value;

                    if (value)
                    {
                        if (m_localize == null)
                        {
                            if (!m_text.TryGetComponent(out m_localize))
                            {
                                m_localize = m_text.gameObject.AddComponent<Localize>();
                            }
                        }
                    }
                    else
                    {
                        if (m_localize != null)
                        {
                            SafeDestroyComponent(ref m_localize);
                        }
                    }
                }
            }
        }

        public bool HasParams
        {
            get => m_hasParams;
            set
            {
                if (m_hasParams != value)
                {
                    m_hasParams = value;
                    if (value)
                    {
                        if (m_localizationParamsManager == null)
                        {
                            if (!m_text.TryGetComponent(out m_localizationParamsManager))
                            {
                                m_localizationParamsManager = m_text.gameObject.AddComponent<LocalizationParamsManager>();
                            }
                        }
                    }
                    else
                    {
                        if (m_localizationParamsManager != null)
                        {
                            SafeDestroyComponent(ref m_localizationParamsManager);
                        }
                    }
                }
            }
        }

        public void Initialize(Text text)
        {
            m_text = text;
        }

#if UNITY_EDITOR
        public void EditorInitialize(Text text)
        {
            m_text = text;
            SafeRefreshText();
        }

        private void SafeRefreshText()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                RefreshText();
            }
            else
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (m_text != null)
                        RefreshText();
                };
            }
        }
#endif

        public void RefreshText()
        {
            if (!m_useI2Localization)
            {
                SafeDestroyComponent(ref m_localize);
                SafeDestroyComponent(ref m_localizationParamsManager);
                return;
            }

            if (m_localize == null)
            {
                if (!m_text.TryGetComponent(out m_localize))
                {
                    m_localize = m_text.gameObject.AddComponent<Localize>();
                }
            }

            if (m_hasParams && m_localizationParamsManager == null)
            {
                if (!m_text.TryGetComponent(out m_localizationParamsManager))
                {
                    m_localizationParamsManager = m_text.gameObject.AddComponent<LocalizationParamsManager>();
                }
            }
            else if (!m_hasParams)
            {
                SafeDestroyComponent(ref m_localizationParamsManager);
            }

            // SetTerm(m_textDefine);
        }

        // 安全的组件销毁方法
        private void SafeDestroyComponent<T>(ref T component) where T : MonoBehaviour
        {
            if (component != null && component.gameObject != null)
            {
                // 确保组件仍然附加在游戏对象上
                if (component.transform != null && component.gameObject != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        // 编辑器模式：使用 DestroyImmediate，但检查上下文
                        if (UnityEditor.EditorUtility.IsPersistent(component))
                        {
                            Debug.LogWarning($"试图销毁资源组件: {component.name}");
                            return;
                        }

                        // 记录 Undo 操作
                        UnityEditor.Undo.DestroyObjectImmediate(component);
                    }
                    else
#endif
                    {
                        UnityEngine.Object.Destroy(component);
                    }
                }
                component = null;
            }
        }

        // public string GetParameterValue(int ParamName)
        //     => m_localizationParamsManager.GetParameterValue(ParamName);
        //
        // public string GetParameterValue(string ParamName)
        //     => m_localizationParamsManager.GetParameterValue(ParamName);
        //
        //
        // public void SetParameterValue(int ParamName, string ParamValue, bool localize = true)
        // {
        //     if (!UseI2Localization)
        //     {
        //         UseI2Localization = true;
        //     }
        //     if (!HasParams)
        //     {
        //         HasParams = true;
        //     }
        //
        //     m_localizationParamsManager.SetParameterValue(ParamName, ParamValue, localize);
        //     SetTerm(m_textDefine);
        // }
        //
        // public void SetParameterValue(string paramName, TextDefine termDefine, bool localize = true)
        // {
        //     string term = termDefine.ToString();
        //     string translatedValue = GetTranslation(term);
        //     SetParameterValue(paramName, translatedValue, localize);
        // }
        //
        // public void SetParameterValue(int paramName, TextDefine termDefine, bool localize = true)
        // {
        //     string term = termDefine.ToString();
        //     string translatedValue = GetTranslation(term);
        //     SetParameterValue(paramName, translatedValue, localize);
        // }
        //
        // private string GetTranslation(string term)
        // {
        //     if (DGame.I2.Loc.LocalizationManager.Sources.Count == 0)
        //         return term;
        //
        //     string translation = DGame.I2.Loc.LocalizationManager.GetTranslation(term);
        //     return string.IsNullOrEmpty(translation) ? term : translation;
        // }
        //
        // public void SetParameterValue(string ParamName, string ParamValue, bool localize = true)
        // {
        //     if (!UseI2Localization)
        //     {
        //         UseI2Localization = true;
        //     }
        //     if (!HasParams)
        //     {
        //         HasParams = true;
        //     }
        //     m_localizationParamsManager.SetParameterValue(ParamName, ParamValue, localize);
        //     SetTerm(m_textDefine);
        // }
        //
        // public void SetParameterValue(List<int> ParamNames, List<string> ParamValues, bool localize = true)
        // {
        //     if (!UseI2Localization)
        //     {
        //         UseI2Localization = true;
        //     }
        //     if (!HasParams)
        //     {
        //         HasParams = true;
        //     }
        //     for (int i = 0; i < ParamNames.Count && i < ParamValues.Count; i++)
        //     {
        //         m_localizationParamsManager.SetParameterValue(ParamNames[i], ParamValues[i], localize);
        //     }
        //     SetTerm(m_textDefine);
        // }
        //
        // public void SetTerm(TextDefine textDefine)
        // {
        //     if (!UseI2Localization)
        //     {
        //         UseI2Localization = true;
        //     }
        //     m_textDefine = textDefine;
        //     m_localize?.SetTerm(m_textDefine.Convert());
        // }
    }
}