using System;
using System.Collections.Generic;
using DGame;
using UnityEngine;

namespace GameLogic
{
    public class SwitchPageMgr
    {
        private class TabPageInfo
        {
            public string TabName { get; set; }
            public HashSet<Type> PageTypes { get; set; } = new HashSet<Type>();
            public GameObject ExistPage { get; set; }
            public SwitchTabItem TabItem { get; set; }
        }

        /// <summary>
        /// oldTabIndex newTabIndex
        /// </summary>
        private event Action<int, int> m_switchTabAction;

        // tab按钮父节点
        protected readonly Transform m_tfTabParent;
        // 子UI父节点
        public Transform TfChildPageParent { get; private set; }

        private readonly Dictionary<int, TabPageInfo> m_tabPageInfoDict = new Dictionary<int, TabPageInfo>();
        private readonly Dictionary<Type, BaseChildPage> m_childPageDict = new Dictionary<Type, BaseChildPage>();
        protected readonly List<int> m_idList = new List<int>();
        private readonly UIWindow m_parentWindow;
        protected int m_curSelectChildID = -100;
        private readonly ChildPageShareData m_shareData = new ChildPageShareData();
        public object ShareData1 => m_shareData?.ShareData1;
        public object ShareData2 => m_shareData?.ShareData2;
        public object ShareData3 => m_shareData?.ShareData3;
        public int TabCount => m_tabPageInfoDict.Count;

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
            var pageType = typeof(T);

            if (!m_idList.Contains(tabID))
            {
                m_idList.Add(tabID);
            }

            if (!m_childPageDict.ContainsKey(pageType))
            {
                m_childPageDict.Add(pageType, new T());
            }

            if (!m_tabPageInfoDict.TryGetValue(tabID, out var tabInfo))
            {
                tabInfo = new TabPageInfo();
                m_tabPageInfoDict[tabID] = tabInfo;
            }

            tabInfo.TabName = tabName;
            if (!tabInfo.PageTypes.Contains(pageType))
            {
                tabInfo.PageTypes.Add(pageType);
            }

            if (goPage != null)
            {
                tabInfo.ExistPage = goPage;
            }
        }

        public void CreateTab<T>(int tabID, GameObject tabTemp = null, bool needSwitch = true) where T : SwitchTabItem, new()
        {
            InternalCreateTab<T>(tabID, tabTemp, true, false);
        }

        public void CreateSingleTab<T>(int tabID, GameObject tabTemp = null, bool needSwitch = true) where T : SwitchTabItem, new()
        {
            if (!m_tabPageInfoDict.TryGetValue(tabID, out var tabInfo))
            {
                return;
            }

            if (tabInfo.TabItem == null)
            {
                T tab = tabTemp != null
                    ? m_parentWindow.CreateWidgetByPrefab<T>(tabTemp, m_tfTabParent)
                    : m_parentWindow.CreateWidgetByType<T>(m_tfTabParent);

                tab.UpdateTabName(tabInfo.TabName);
                tab.BindClickEvent(OnTabClick, tabID);
                tab.SetSelectedState(m_curSelectChildID == tabID);
                tabInfo.TabItem = tab;
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
                DLogger.Fatal("CreatTabByPrefab failed, prefab is null: {0}", typeof(T).Name);
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
                if (!m_tabPageInfoDict.TryGetValue(childID, out var tabInfo))
                {
                    continue;
                }

                if (tabInfo.TabItem == null)
                {
                    T tab = tabTemp != null
                        ? m_parentWindow.CreateWidgetByPrefab<T>(tabTemp, m_tfTabParent)
                        : m_parentWindow.CreateWidgetByType<T>(m_tfTabParent);

                    if (setSizeDelta)
                    {
                        tab.UpdateTabNameChangeSize(tabInfo.TabName, true);
                    }
                    else
                    {
                        tab.UpdateTabName(tabInfo.TabName);
                    }
                    tab.BindClickEvent(OnTabClick, childID);
                    if (setSelectedState)
                    {
                        tab.SetSelectedState(m_curSelectChildID == childID);
                    }
                    tabInfo.TabItem = tab;
                }
                else
                {
                    if (setSizeDelta)
                    {
                        tabInfo.TabItem.UpdateTabNameChangeSize(tabInfo.TabName, true);
                    }
                    else
                    {
                        tabInfo.TabItem.UpdateTabName(tabInfo.TabName);
                    }
                    tabInfo.TabItem.BindClickEvent(OnTabClick, childID);
                    if (setSelectedState)
                    {
                        tabInfo.TabItem.SetSelectedState(m_curSelectChildID == childID);
                    }
                }
                callback?.Invoke(i, tabInfo.TabItem as T);
            }

            if (needSwitch)
            {
                SwitchPage(tabID);
            }
        }

        public void SetCustomTabClickAction(int tabID, Action<SwitchTabItem> clickAction, object shareData1 = null,
            object shareData2 = null, object shareData3 = null)
        {
            if (m_tabPageInfoDict.TryGetValue(tabID, out var tabInfo))
            {
                tabInfo.TabItem?.BindClickEvent(clickAction, shareData1, shareData2, shareData3);
            }
        }

        public void SwitchPage(int tabID)
        {
            if (m_curSelectChildID != tabID && m_tabPageInfoDict.TryGetValue(tabID, out var curTabInfo))
            {
                var pages = curTabInfo.PageTypes;
                if (pages != null)
                {
                    foreach (var pageType in pages)
                    {
                        var page = GetChildPageByType(pageType);
                        if (page != null && page.gameObject == null)
                        {
                            if (curTabInfo.ExistPage != null)
                            {
                                page.Create(m_parentWindow, curTabInfo.ExistPage, TfChildPageParent);
                                page.Init(m_shareData, this);
                            }
                            else
                            {
                                page.CreateByPath(pageType.Name, m_parentWindow, TfChildPageParent);
                                page.Init(m_shareData, this);
                            }
                        }
                    }
                }

                foreach (var item in m_childPageDict)
                {
                    var pageType = item.Key;
                    var page = item.Value;
                    bool beShow = curTabInfo.PageTypes.Contains(pageType);

                    if (page.gameObject != null)
                    {
                        page.Show(beShow);
                    }
                    if (beShow)
                    {
                        page.OnPageShowed(m_curSelectChildID, tabID);
                    }
                }


                // 设置tab的状态
                for (var index = 0; index < m_idList.Count; index++)
                {
                    var childID = m_idList[index];

                    if (m_tabPageInfoDict.TryGetValue(childID, out var tabInfo))
                    {
                        tabInfo.TabItem?.SetSelectedState(tabID == childID);
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

        public BaseChildPage GetChildPageByType(Type pageType)
            => m_childPageDict.GetValueOrDefault(pageType);

        public void RefreshCurrentChildPage()
        {
            if (m_tabPageInfoDict.TryGetValue(m_curSelectChildID, out var curTabInfo))
            {
                foreach (var pageType in curTabInfo.PageTypes)
                {
                    var page = GetChildPageByType(pageType);
                    if (page != null && page.gameObject != null)
                    {
                        page.RefreshCurrentChildPage();
                    }
                }
            }
        }

        public void RefreshChildPage(int tabID)
        {
            if (m_tabPageInfoDict.TryGetValue(tabID, out var curTabInfo))
            {
                foreach (var pageType in curTabInfo.PageTypes)
                {
                    var page = GetChildPageByType(pageType);
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
            return m_childPageDict.TryGetValue(typeof(T), out var page) ? page as T : null;
        }

        public bool ContainsChildPage(int tabID) => m_idList.Contains(tabID);

        public bool ContainsTab(int tabID) => m_idList.Contains(tabID);

        public int GetCurrentShowTabID() => m_curSelectChildID;

        public void SetShareData(int shareDataIndex, object shareData)
            => m_shareData.SetShareData(shareDataIndex, shareData);

        public T GetChildPageByTabID<T>(int tabID) where T : BaseChildPage
        {
            if (m_tabPageInfoDict.TryGetValue(tabID, out var tabInfo))
            {
                foreach (var pageType in tabInfo.PageTypes)
                {
                    var page = GetChildPageByType(pageType);

                    if (page != null)
                    {
                        return page as T;
                    }
                }
            }

            return null;
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
            if (m_tabPageInfoDict.TryGetValue(tabID, out var tabInfo))
            {
                tabInfo.TabItem?.SetSelectedState(isShow);
            }
        }

        public void SetTabIcon(int tabID, string selectIconPath, string noSelectIconPath)
        {
            if (m_tabPageInfoDict.TryGetValue(tabID, out var tabInfo))
            {
                tabInfo.TabItem?.SetTabIcon(selectIconPath, noSelectIconPath);
            }
        }

        public void SetTabTextFontSize(int tabID, int fontSize)
        {
            if (m_tabPageInfoDict.TryGetValue(tabID, out var tabInfo))
            {
                tabInfo.TabItem?.SetTabTextFontSize(fontSize);
            }
        }

        public void SetTabBg(int tabID, string selectBgPath, string noSelectBgPath)
        {
            if (m_tabPageInfoDict.TryGetValue(tabID, out var tabInfo))
            {
                tabInfo.TabItem?.SetTabBg(selectBgPath, noSelectBgPath);
            }
        }

        public void SetAllTabBg(string selectBgPath, string noSelectBgPath)
        {
            foreach (var tabInfo in m_tabPageInfoDict.Values)
            {
                tabInfo.TabItem?.SetTabBg(selectBgPath, noSelectBgPath);
            }
        }

        public void SetTabName(int tabID, string tabName)
        {
            if (m_tabPageInfoDict.TryGetValue(tabID, out var tabInfo))
            {
                tabInfo.TabItem?.UpdateTabName(tabName);
            }
        }

        public void SetTabTextColor(int tabID, string selectedColor, string noSelectColor)
        {
            if (m_tabPageInfoDict.TryGetValue(tabID, out var tabInfo))
            {
                tabInfo.TabItem?.SetTabTextColor(selectedColor, noSelectColor);
            }
        }

        public void SetAllTabTextColor(string selectedColor, string noSelectColor)
        {
            foreach (var tabInfo in m_tabPageInfoDict.Values)
            {
                tabInfo.TabItem?.SetTabTextColor(selectedColor, noSelectColor);
            }
        }

        public SwitchTabItem GetTabByID(int tabID)
        {
            return m_tabPageInfoDict.TryGetValue(tabID, out var tabInfo) ? tabInfo.TabItem : null;
        }

        #region 页签从前到后排序

        private List<int> m_idListTemp = new List<int>();

        public void SortTab()
        {
            m_idListTemp.Clear();
            m_idListTemp.AddRange(m_idList);
            m_idListTemp.Sort(OnSortTab);

            foreach (var item in m_tabPageInfoDict)
            {
                var tabID = item.Key;
                var tabItem = item.Value.TabItem;
                tabItem?.transform?.SetSiblingIndex(m_idListTemp.IndexOf(tabID));
            }
        }

        private int OnSortTab(int l, int r) => l.CompareTo(r);

        #endregion
    }
}