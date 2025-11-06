using System;
using System.Collections.Generic;
using DGame;
using UnityEngine;

namespace GameLogic
{
    public class SwitchPageMgr
    {
        /// <summary>
        /// oldTabIndex newTabIndex
        /// </summary>
        private event Action<int, int> m_switchTabAction;

        // tab按钮父节点
        protected Transform m_tfTabParent;
        // 子UI父节点
        public Transform TfChildPageParent { get; private set; }
        /// <summary>
        /// 存储子UI字典
        /// </summary>
        private Dictionary<int, List<string>> m_switchPageDict = new Dictionary<int, List<string>>();
        private Dictionary<int, GameObject> m_existPageDict = new Dictionary<int, GameObject>();
        private List<string> m_childPageNames = new List<string>();
        private Dictionary<int, string> m_childPageNamesMap = new Dictionary<int, string>();
        private Dictionary<string, BaseChildPage> m_childPageDict = new Dictionary<string, BaseChildPage>();
        private Dictionary<int, SwitchTabItem> m_tabDict = new Dictionary<int, SwitchTabItem>();
        private Dictionary<int, string> m_tabName = new Dictionary<int, string>();
        protected List<int> m_idList = new List<int>();
        private UIWindow m_parentWindow;
        protected int m_curSelectChildID = -100;
        private ChildPageShareData m_shareData = new ChildPageShareData();
        public object ShareData1 => m_shareData?.ShareData1;
        public object ShareData2 => m_shareData?.ShareData2;
        public object ShareData3 => m_shareData?.ShareData3;

        public int TabCount => m_tabDict.Count;

        public SwitchPageMgr(Transform tfTabParent, Transform tfChildPageParent, UIWindow parentWindow)
        {
            m_tfTabParent = tfTabParent;
            m_parentWindow = parentWindow;
            TfChildPageParent = tfChildPageParent;
        }

        public void AddSwitchAction(Action<int, int> switchTabAction)
        {
            m_switchTabAction += switchTabAction;
        }

        public void RemoveSwitchAction(Action<int, int> switchTabAction)
        {
            m_switchTabAction -= switchTabAction;
        }

        public void BindChildPage<T>(int tabID) where T : BaseChildPage, new()
        {
            BindChildPage<T>(tabID, string.Empty);
        }

        public void BindChildPage<T>(int tabID, string tabName, GameObject goPage = null) where T : BaseChildPage, new()
        {
            var pageName = typeof(T).Name;

            if (!m_idList.Contains(tabID))
            {
                m_idList.Add(tabID);
            }

            if (!m_childPageDict.ContainsKey(pageName))
            {
                m_childPageDict.Add(pageName, new T());
                if (!m_childPageNames.Contains(pageName))
                {
                    m_childPageNames.Add(pageName);
                }
            }

            m_childPageNamesMap[tabID] = pageName;

            if (!m_switchPageDict.TryGetValue(tabID, out var pageList))
            {
                pageList = new List<string>();
                m_switchPageDict[tabID] = pageList;
            }

            if (!pageList.Contains(pageName))
            {
                pageList.Add(pageName);
            }

            if (goPage != null)
            {
                m_existPageDict[tabID] = goPage;
            }

            m_tabName[tabID] = tabName;
        }

        public void CreateTab<T>(int tabID, GameObject tabTemp = null, bool needSwitch = true) where T : SwitchTabItem, new()
        {
            InternalCreateTab<T>(tabID, tabTemp, true, false);
        }

        public void CreateSingleTab<T>(int tabID, GameObject tabTemp = null, bool needSwitch = true) where T : SwitchTabItem, new()
        {
            if (!m_tabDict.ContainsKey(tabID))
            {
                T tab;

                if (tabTemp != null)
                {
                    tab = m_parentWindow.CreateWidgetByPrefab<T>(tabTemp, m_tfTabParent);
                }
                else
                {
                    tab = m_parentWindow.CreateWidgetByType<T>(m_tfTabParent);
                }
                tab.UpdateTabName(m_tabName[tabID]);
                tab.BindClickEvent(OnTabClick, tabID);
                tab.SetSelectedState(m_curSelectChildID == tabID);
                m_tabDict[tabID] = tab;
            }

            if (needSwitch)
            {
                SwitchPage(tabID);
            }
        }

        public void CreateTabByPrefab<T>(int tabID, GameObject tabTemp, bool setSizeDelta = true) where T : SwitchTabItem, new()
        {
            if (tabTemp == null)
            {
                Debugger.Fatal("CreatTabByPrefab failed, prefab is null: {0}", typeof(T).Name);
                return;
            }

            InternalCreateTab<T>(tabID, tabTemp, true, setSizeDelta, false);
        }

        public void CreatTabByType<T>(int tabID, bool setSizeDelta = true) where T : SwitchTabItem, new()
        {
            InternalCreateTab<T>(tabID, null, true, setSizeDelta, false);
        }

        public void CreatTabByType<T>(int tabID, Action<int, T> action, bool setSizeDelta = true) where T : SwitchTabItem, new()
        {
            InternalCreateTab<T>(tabID, null, true, setSizeDelta, false, action);
        }

        private void DoCreateTabByType<T>(int tabID, GameObject tabTemp, bool setSizeDelta = true)
            where T : SwitchTabItem, new()
        {
            InternalCreateTab<T>(tabID, tabTemp, true, setSizeDelta, false);
        }

        private void DoCreateTabByType<T>(int tabID, GameObject tabTemp, Action<int, T> action, bool setSizeDelta = true)
            where T : SwitchTabItem, new()
        {
            InternalCreateTab<T>(tabID, tabTemp, true, setSizeDelta, false, action);
        }

        private void InternalCreateTab<T>(int tabID,
            GameObject tabTemp = null,
            bool needSwitch = true,
            bool setSizeDelta = true,
            bool setSelectedState = true,
            Action<int, T> callback = null)
            where T : SwitchTabItem, new()
        {
            for (int i = 0; i < m_idList.Count; i++)
            {
                var childID = m_idList[i];
                if (!m_tabDict.ContainsKey(childID))
                {
                    T tab = tabTemp != null
                        ? m_parentWindow.CreateWidgetByPrefab<T>(tabTemp, m_tfTabParent)
                        : m_parentWindow.CreateWidgetByType<T>(m_tfTabParent);

                    // 统一名称更新逻辑
                    if (setSizeDelta)
                    {
                        tab.UpdateTabNameChangeSize(m_tabName[childID], true);
                    }
                    else
                    {
                        tab.UpdateTabName(m_tabName[childID]);
                    }

                    tab.BindClickEvent(OnTabClick, childID);

                    if (setSelectedState)
                    {
                        tab.SetSelectedState(m_curSelectChildID == childID);
                    }

                    m_tabDict[childID] = tab;
                    callback?.Invoke(i, tab);
                }
            }

            if (needSwitch)
            {
                SwitchPage(tabID);
            }
        }

        public void SetCustomTabClickAction(int tabID, Action<SwitchTabItem> clickAction, object shareData1 = null,
            object shareData2 = null, object shareData3 = null)
        {
            if (m_tabDict.TryGetValue(tabID, out var tab))
            {
                tab.BindClickEvent(clickAction, shareData1, shareData2, shareData3);
            }
        }

        public void SwitchPage(int tabID)
        {
            if (m_curSelectChildID != tabID)
            {
                List<string> pages = m_switchPageDict[tabID];
                // 先把绑定的Page创建出来
                for (int i = 0; i < pages.Count; i++)
                {
                    var pageName = pages[i];
                    var page = GetChildPageByName(pageName);
                    if (page != null && page.gameObject == null)
                    {
                        if (m_existPageDict.TryGetValue(tabID, out var pageObj))
                        {
                            page.Create(m_parentWindow, pageObj, TfChildPageParent);
                            page.Init(m_shareData, this);
                        }
                        else
                        {
                            page.CreateByPath(pageName, m_parentWindow, TfChildPageParent);
                            page.Init(m_shareData, this);
                        }
                    }
                }

                // 把相关联的Page全部显示出来
                for (int i = 0; i < m_childPageNames.Count; i++)
                {
                    string pageName = m_childPageNames[i];
                    var page = GetChildPageByName(pageName);
                    bool beShow = pages.IndexOf(pageName) > 0;
                    if (page != null && page.gameObject != null)
                    {
                        page.Show(beShow);
                    }
                    if (page != null && beShow)
                    {
                        page.OnPageShowed(m_curSelectChildID, tabID);
                    }
                }

                // 设置tab的状态
                for (int i = 0; i < m_idList.Count; i++)
                {
                    var childID = m_idList[i];

                    if (m_tabDict.TryGetValue(childID, out var tab))
                    {
                        tab.SetSelectedState(tabID == childID);
                    }
                }
            }

            var oldID = m_curSelectChildID;
            m_curSelectChildID = tabID;
            m_switchTabAction?.Invoke(oldID, tabID);
        }

        private void OnTabClick(SwitchTabItem tabItem)
        {
            var tabID = (int)tabItem.EventParam1;
            SwitchPage(tabID);
        }

        public BaseChildPage GetChildPageByName(string pageName)
            => m_childPageDict.GetValueOrDefault(pageName);

        public void RefreshCurrentChildPage()
        {
            if (m_switchPageDict.TryGetValue(m_curSelectChildID, out var pageNames))
            {
                for (int i = 0; i < pageNames.Count; i++)
                {
                    var page = GetChildPageByName(pageNames[i]);
                    if (page != null && page.gameObject != null)
                    {
                        page.RefreshCurrentChildPage();
                    }
                }
            }
        }

        public void RefreshChildPage(int tabID)
        {
            if (m_switchPageDict.TryGetValue(tabID, out var pageNames))
            {
                for (int i = 0; i < pageNames.Count; i++)
                {
                    var page = GetChildPageByName(pageNames[i]);
                    if (page != null && page.gameObject != null)
                    {
                        page.RefreshCurrentChildPage();
                    }
                }
            }
        }

        public bool TryGetChildPage<T>(out T page) where T : BaseChildPage
        {
            page = GetChildPage<T>();
            return page != null;
        }

        public T GetChildPage<T>() where T : BaseChildPage
        {
            return m_childPageDict.TryGetValue(typeof(T).Name, out var page) ? page as T : null;
        }

        public bool ContainsChildPage(int tabID) => m_idList.Contains(tabID);

        public bool ContainsTab(int tabID) => m_idList.Contains(tabID);

        public int GetCurrentShowTabID() => m_curSelectChildID;

        public void SetShareData(int shareDataIndex, object shareData)
            => m_shareData.SetShareData(shareDataIndex, shareData);

        public T GetChildPageByTabID<T>(int tabID) where T : BaseChildPage
        {
            m_childPageNamesMap.TryGetValue(tabID, out var pageName);
            var page = GetChildPageByName(pageName);
            return page as T;
        }

        public void BindChildPage<T, U>(int tabID, string tabName)
            where T : BaseChildPage, new()
            where U : BaseChildPage, new()
        {
            BindChildPage<T>(tabID, tabName);
            BindChildPage<U>(tabID, tabName);
        }


        public void BindChildPage<T, U, V>(int tabID, string tabName)
            where T : BaseChildPage, new()
            where U : BaseChildPage, new()
            where V : BaseChildPage, new()
        {
            BindChildPage<T>(tabID, tabName);
            BindChildPage<U>(tabID, tabName);
            BindChildPage<V>(tabID, tabName);
        }

        public void BindChildPage<T, U, V, W>(int tabID, string tabName)
            where T : BaseChildPage, new()
            where U : BaseChildPage, new()
            where V : BaseChildPage, new()
            where W : BaseChildPage, new()
        {
            BindChildPage<T>(tabID, tabName);
            BindChildPage<U>(tabID, tabName);
            BindChildPage<V>(tabID, tabName);
            BindChildPage<W>(tabID, tabName);
        }

        public UIWindow GetParentWindow() => m_parentWindow;

        // public void DestroyParentWindow() => m_parentWindow?.Destroy();

        public void SetTabRedNode(int tabID, bool isShow)
        {
            if (m_tabDict.TryGetValue(tabID, out var tab))
            {
                tab.SetSelectedState(isShow);
            }
        }

        public void SetTabIcon(int tabID, string selectIconPath, string noSelectIconPath)
        {
            if (m_tabDict.TryGetValue(tabID, out var tab))
            {
                tab.SetTabIcon(selectIconPath, noSelectIconPath);
            }
        }

        public void SetTabTextFontSize(int tabID, int fontSize)
        {
            if (m_tabDict.TryGetValue(tabID, out var tab))
            {
                tab.SetTabTextFontSize(fontSize);
            }
        }

        public void SetTabBg(int tabID, string selectBgPath, string noSelectBgPath)
        {
            if (m_tabDict.TryGetValue(tabID, out var tab))
            {
                tab.SetTabBg(selectBgPath, noSelectBgPath);
            }
        }

        public void SetAllTabBg(string selectBgPath, string noSelectBgPath)
        {
            foreach (var tab in m_tabDict.Values)
            {
                tab.SetTabBg(selectBgPath, noSelectBgPath);
            }
        }

        public void SetTabName(int tabID, string tabName)
        {
            m_tabName[tabID] = tabName;
            if (m_tabDict.TryGetValue(tabID, out var tab))
            {
                tab.UpdateTabName(tabName);
            }
        }

        public void SetTabTextColor(int tabID, string selectedColor, string noSelectColor)
        {
            if (m_tabDict.TryGetValue(tabID, out var tab))
            {
                tab.SetTabTextColor(selectedColor, noSelectColor);
            }
        }

        public void SetAllTabTextColor(int tabID, string selectedColor, string noSelectColor)
        {
            foreach (var tab in m_tabDict.Values)
            {
                tab.SetTabTextColor(selectedColor, noSelectColor);
            }
        }

        public SwitchTabItem GetTabByID(int tabID)
        {
            return m_tabDict.GetValueOrDefault(tabID);
        }

        #region 页签从前到后排序

        private List<int> m_idListTemp = new List<int>();

        public void SortTab()
        {
            m_idListTemp.Clear();
            m_idListTemp.AddRange(m_idList);
            m_idListTemp.Sort(OnSortTab);

            foreach (var item in m_tabDict)
            {
                var tabID = item.Key;
                var tabItem = item.Value;
                tabItem.transform.SetSiblingIndex(m_idListTemp.IndexOf(tabID));
            }
        }

        private int OnSortTab(int l, int r) => l.CompareTo(r);

        #endregion
    }
}