using System.Collections.Generic;
using DGame;
using UnityEngine;

namespace GameLogic
{
    public abstract class UIWidget : UIBase
    {
        public override GameObject gameObject { get; protected set; }

        public override RectTransform rectTransform { get; protected set; }

        public override Transform transform { get; protected set; }

        public override UIType Type => UIType.Widget;

        /// <summary>
        /// 窗口组件名称
        /// </summary>
        public string WidgetName { get; protected set; } = string.Empty;

        /// <summary>
        /// 所属窗口
        /// </summary>
        public UIWindow OwnerWindow
        {
            get
            {
                var parentUI = base.m_parent;

                while (parentUI != null)
                {
                    if (parentUI.Type == UIType.Window)
                    {
                        return parentUI as UIWindow;
                    }
                    parentUI = parentUI.Parent;
                }
                return null;
            }
        }

        /// <summary>
        /// 组件可见性
        /// </summary>
        public bool Visible
        {
            get => gameObject.activeSelf;
            set
            {
                gameObject?.SetActive(value);
                OnSetVisible(value);
            }
        }

        public bool InternalUpdate()
        {
            if (!IsPrepared)
            {
                return false;
            }

            List<UIWidget> listNextUpdateChild = new List<UIWidget>();

            if (ChildList != null && ChildList.Count > 0)
            {
                listNextUpdateChild = m_updateChildList;
                var updateListDirty = m_updateListDirty;
                List<UIWidget> childList = new List<UIWidget>();
                if (m_updateListDirty)
                {
                    if (listNextUpdateChild == null)
                    {
                        listNextUpdateChild = new List<UIWidget>();
                        m_updateChildList = listNextUpdateChild;
                    }
                    else
                    {
                        listNextUpdateChild.Clear();
                    }
                    childList = ChildList;
                }
                else
                {
                    childList = listNextUpdateChild;
                }

                for (int i = 0; i < childList.Count; i++)
                {
                    var uiWidget = childList[i];

                    if (uiWidget == null)
                    {
                        continue;
                    }
                    var childNeedUpdate = uiWidget.InternalUpdate();

                    if (updateListDirty && childNeedUpdate)
                    {
                        listNextUpdateChild.Add(uiWidget);
                    }
                }

                if (updateListDirty)
                {
                    m_updateListDirty = false;
                }
            }

            bool needUpdate = false;

            if (listNextUpdateChild is not null && listNextUpdateChild.Count > 0)
            {
                m_hasOverrideUpdate = true;
                OnUpdate();
                needUpdate = m_hasOverrideUpdate;
            }
            else
            {
                OnUpdate();
                needUpdate = true;
            }
            return needUpdate;
        }

        #region Create

        public bool CreateByPath(string resPath, UIBase parentUI, Transform parentTrans = null, bool visible = true)
        {
            GameObject goInstance = UIModule.ResourceLoader.LoadGameObject(resPath, parentTrans);
            if (goInstance == null)
            {
                return false;
            }

            if (!Create(parentUI, goInstance, visible))
            {
                return false;
            }

            goInstance.transform.localScale = Vector3.one;
            goInstance.transform.localPosition = Vector3.zero;
            return true;
        }

        public bool Create(UIBase parentUI, GameObject widgetPrefab, bool visible = true)
        {
            return CreateImp(parentUI, widgetPrefab, visible);
        }

        public bool CreateByPrefab(UIBase parentUI, GameObject goPrefab, Transform parentTrans, bool visible = true)
        {
            if (parentTrans == null)
            {
                parentTrans = parentUI.transform;
            }

            return CreateImp(parentUI, Object.Instantiate(goPrefab, parentTrans), visible);
        }

        private bool CreateImp(UIBase parentUI, GameObject widgetPrefab, bool visible = true)
        {
            if (!CreateBase(widgetPrefab))
            {
                return false;
            }

            ResetChildCanvas(parentUI);
            m_parent = parentUI;
            Parent.AddChild(this);
            ScriptGenerator();
            BindMemberProperty();
            RegisterEvent();
            OnCreate();
            IsPrepared = true;

            if (visible)
            {
                Show(true);
            }
            else
            {
                gameObject?.SetActive(false);
            }

            return true;
        }

        private bool CreateBase(GameObject go)
        {
            if (go == null)
            {
                return false;
            }

            WidgetName = GetType().Name;
            transform = go.GetComponent<Transform>();
            rectTransform = transform as RectTransform;
            gameObject = go;
            Debugger.Assert(rectTransform != null, $"{go.name} UI元素必须具有 RectTransform");
            return true;
        }

        protected void ResetChildCanvas(UIBase parentUI)
        {
            if (parentUI == null || parentUI.gameObject == null)
            {
                return;
            }

            Canvas parentCanvas = parentUI.gameObject.GetComponentInParent<Canvas>();

            if (parentCanvas == null)
            {
                return;
            }

            if (gameObject != null)
            {
                var listCanvas = gameObject.GetComponentsInChildren<Canvas>(true);

                for (int i = 0; i < listCanvas.Length; i++)
                {
                    var childCanvas = listCanvas[i];
                    childCanvas.sortingOrder = parentCanvas.sortingOrder + childCanvas.sortingOrder % UIModule.EACH_WINDOW_DEEP;
                }
            }
        }

        #endregion

        #region Destroy

        protected internal void OnDestroyWidget()
        {
            Parent?.SetUpdateDirty();
            
            RemoveAllUIEvents();

            foreach (var child in ChildList)
            {
                child.CallDestroy();
                child.OnDestroyWidget();
            }

            if (gameObject != null)
            {
                Object.Destroy(gameObject);
            }
        }

        public void Destroy()
        {
            if (Parent != null)
            {
                Parent.RemoveChild(this);
                CallDestroy();
                OnDestroyWidget();
            }
        }

        #endregion

        public void Show(bool visible)
        {
            Visible = visible;
        }
    }
}