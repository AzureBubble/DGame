using System;
using System.Collections;
using System.Collections.Generic;
using DGame;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Launcher
{
    public static class LauncherMgr
    {
        private static Transform m_uiRoot;
        private static readonly Dictionary<string, string> m_uiDict = new Dictionary<string, string>();
        private static readonly Dictionary<string, UIBase> m_uiMapDict = new Dictionary<string, UIBase>();

        public static void Initialize()
        {
            m_uiRoot = GameObject.Find("UIRoot/UICanvas")?.transform;

            if (m_uiRoot == null)
            {
                Debugger.Error($"======== 找不到 UIRoot 节点 请检查资源路径或Hierarchy窗口中的游戏对象 ========");
                return;
            }

            RegisterUI();
            Debugger.Info("======== 初始化 LauncherMgr 完成 ========");
        }

        private static void RegisterUI()
        {
            UIDefine.RegisterUI(m_uiDict);
        }

        public static void ShowUI(string uiName, object param = null)
        {
            if (string.IsNullOrEmpty(uiName))
            {
                Debugger.Warning($"======== LauncherMgr.ShowUI UIName 为空 ========");
                return;
            }

            if (!m_uiDict.ContainsKey(uiName))
            {
                Debugger.Error($"======== LauncherMgr.ShowUI 找不到UI窗口: {uiName} ========");
                return;
            }

            GameObject uiWindow = null;

            if (!m_uiMapDict.ContainsKey(uiName))
            {
                Object obj = Resources.Load(m_uiDict[uiName]);
                if (obj != null)
                {
                    uiWindow = Object.Instantiate(obj) as GameObject;
                    if (uiWindow != null)
                    {
                        uiWindow.transform.SetParent(m_uiRoot.transform);
                        uiWindow.transform.localScale = Vector3.one;
                        uiWindow.transform.localPosition = Vector3.zero;
                        uiWindow.transform.localRotation = Quaternion.identity;
                        RectTransform rectTransform = uiWindow.GetComponent<RectTransform>();
                        rectTransform.sizeDelta = Vector2.zero;
                    }
                }

                UIBase component = uiWindow?.GetComponent<UIBase>();

                if (component != null)
                {
                    m_uiMapDict.Add(uiName, component);
                }
            }

            m_uiMapDict[uiName].gameObject.SetActive(true);

            if (param != null)
            {
                m_uiMapDict[uiName]?.OnEnter(param);
            }
        }

        public static void HideUI(string uiName)
        {
            if (string.IsNullOrEmpty(uiName))
            {
                Debugger.Warning($"======== LauncherMgr.HideUI UIName 为空 ========");
                return;
            }

            if (!m_uiMapDict.TryGetValue(uiName, out UIBase uiWindow))
            {
                return;
            }

            uiWindow?.gameObject.SetActive(false);
            Object.DestroyImmediate(uiWindow?.gameObject);
            m_uiMapDict.Remove(uiName);
        }

        public static UIBase GetActiveUI(string uiName)
        {
            return m_uiMapDict.GetValueOrDefault(uiName);
        }

        public static void HideAllUI()
        {
            foreach (var ui in m_uiMapDict.Values)
            {
                Object.Destroy(ui?.gameObject);
            }
            m_uiMapDict.Clear();
        }

        #region UI调用

        public static void ShowMessageBox(string desc, MessageShowType showType = MessageShowType.OneButton,
            Action onOk = null, Action onCancel = null, Action onPackage = null)
        {
            ShowUI(UIDefine.LoadTipsUI, desc);
            var ui = GetActiveUI(UIDefine.LoadTipsUI) as LoadTipsUI;

            if (ui == null)
            {
                return;
            }

            ui.OnOk = onOk;
            ui.OnCancel = onCancel;
            // ui.OnEnter(desc);
        }

        public static void UpdateUIProgress(float progress)
        {
            ShowUI(UIDefine.LoadUpdateUI);
            var ui = GetActiveUI(UIDefine.LoadUpdateUI) as LoadUpdateUI;
            ui?.OnUpdateUIProgress(progress);
        }

        #endregion
    }
}