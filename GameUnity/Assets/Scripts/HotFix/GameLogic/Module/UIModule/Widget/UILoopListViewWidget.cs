using System.Collections.Generic;
using DGame;
using SuperScrollView;
using UnityEngine;

namespace GameLogic
{
    public class UILoopListViewWidget<T> : UIWidget where T : UILoopItemWidget, new()
    {
        public LoopListView2 LoopRectView { private set; get; }

        private DGameDictionary<int, T> m_itemCache = new DGameDictionary<int, T>();

        protected override void BindMemberProperty()
        {
            base.BindMemberProperty();
            LoopRectView = this.rectTransform.GetComponent<LoopListView2>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_itemCache.Clear();
        }

        public T CreateItem()
        {
            string typeName = typeof(T).Name;
            return CreateItem(typeName);
        }

        public T CreateItem(string itemName)
        {
            T widget = null;
            var item = LoopRectView.NewListViewItem(itemName);
            if (item != null)
            {
                widget = CreateItem(item);
            }

            return widget;
        }

        public T CreateItem(GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }
            return CreateItem(prefab.name);
        }

        private T CreateItem(LoopListViewItem2 item)
        {
            if (!m_itemCache.TryGetValue(item.GoId, out var widget))
            {
                widget = CreateWidget<T>(item.gameObject);
                widget.LoopItem = item;
                m_itemCache.Add(item.GoId, widget);
            }

            return widget;
        }

        public List<T> GetItemList()
        {
            List<T> list = new List<T>();
            for (int i = 0; i < m_itemCache.Count; i++)
            {
                list.Add(m_itemCache[i]);
            }

            return list;
        }

        public int GetItemCount()
        {
            return m_itemCache.Count;
        }

        public T GetItem(int goID)
        {
            return m_itemCache[goID];
        }

        /// <summary>
        /// 获取Item。
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T GetItemByIndex(int index)
        {
            return m_itemCache.GetValue(index);
        }
    }
}