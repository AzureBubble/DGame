using System.Collections.Generic;
using DGame;
using SuperScrollView;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 支持多种 Item 类型的 LoopListView 组件。
    /// </summary>
    public class UILoopListViewWidget : UIWidget
    {
        /// <summary>
        /// LoopListView组件
        /// </summary>
        public LoopListView2 LoopRectView { private set; get; }

        private readonly DGameDictionary<int, UILoopItemWidget> m_itemCache =
            new DGameDictionary<int, UILoopItemWidget>();

        protected override void BindMemberProperty()
        {
            base.BindMemberProperty();
            LoopRectView = rectTransform.GetComponent<LoopListView2>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_itemCache.Clear();
        }

        /// <summary>
        /// 创建Item
        /// </summary>
        public TItem CreateItem<TItem>() where TItem : UILoopItemWidget, new()
        {
            return CreateItem<TItem>(typeof(TItem).Name);
        }

        /// <summary>
        /// 创建Item
        /// </summary>
        /// <param name="itemName">Item名称</param>
        public TItem CreateItem<TItem>(string itemName) where TItem : UILoopItemWidget, new()
        {
            var item = LoopRectView.NewListViewItem(itemName);
            return item == null ? null : GetOrCreateItem<TItem>(item);
        }

        /// <summary>
        /// 创建Item
        /// </summary>
        /// <param name="prefab">预制体</param>
        public TItem CreateItem<TItem>(GameObject prefab) where TItem : UILoopItemWidget, new()
        {
            if (prefab == null)
            {
                return null;
            }

            var item = LoopRectView.NewListViewItem(prefab);
            return item == null ? null : GetOrCreateItem<TItem>(item);
        }

        private TItem GetOrCreateItem<TItem>(LoopListViewItem2 item)
            where TItem : UILoopItemWidget, new()
        {
            if (m_itemCache.TryGetValue(item.GoId, out var cachedItem))
            {
                if (cachedItem is TItem typedItem)
                {
                    return typedItem;
                }

                DLogger.Error(
                    $"LoopListView Item类型不匹配，预期：{typeof(TItem).Name}，实际：{cachedItem.GetType().Name}");
                return null;
            }

            var widget = CreateWidget<TItem>(item.gameObject);
            if (widget == null)
            {
                return null;
            }

            widget.LoopItem = item;
            m_itemCache.Add(item.GoId, widget);
            return widget;
        }

        /// <summary>
        /// 获取所有Item列表
        /// </summary>
        public List<TItem> GetItemList<TItem>() where TItem : UILoopItemWidget
        {
            var list = new List<TItem>();
            foreach (var item in m_itemCache.Values)
            {
                if (item is TItem typedItem)
                {
                    list.Add(typedItem);
                }
            }

            return list;
        }

        /// <summary>
        /// 根据GoID获取Item
        /// </summary>
        public TItem GetItem<TItem>(int goID) where TItem : UILoopItemWidget
        {
            return m_itemCache[goID] as TItem;
        }

        /// <summary>
        /// 获取Item。
        /// </summary>
        /// <param name="index">索引</param>
        /// <returns>Item</returns>
        public TItem GetItemByIndex<TItem>(int index) where TItem : UILoopItemWidget
        {
            return m_itemCache.GetValue(index) as TItem;
        }
    }

    /// <summary>
    /// 单一 Item 类型的 LoopListView 组件。
    /// </summary>
    public class UILoopListViewWidget<T> : UILoopListViewWidget where T : UILoopItemWidget, new()
    {
        public T CreateItem()
        {
            return base.CreateItem<T>();
        }

        public T CreateItem(string itemName)
        {
            return base.CreateItem<T>(itemName);
        }

        public T CreateItem(GameObject prefab)
        {
            return base.CreateItem<T>(prefab);
        }

        public List<T> GetItemList()
        {
            return base.GetItemList<T>();
        }

        public T GetItem(int goID)
        {
            return base.GetItem<T>(goID);
        }

        public T GetItemByIndex(int index)
        {
            return base.GetItemByIndex<T>(index);
        }
    }
}
