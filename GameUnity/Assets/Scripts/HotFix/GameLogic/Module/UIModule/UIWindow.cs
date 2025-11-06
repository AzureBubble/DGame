using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DGame;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public abstract class UIWindow : UIBase
    {
        #region Propreties

        private System.Action<UIWindow> m_prepareCallback;
        private bool m_isCreated = false;
        private GameObject m_windowGo;
        private Canvas m_canvas;
        public Canvas Canvas => m_canvas;
        private Canvas[] m_childCanvas;
        private GraphicRaycaster m_graphicRaycaster;
        public GraphicRaycaster GraphicRaycaster => m_graphicRaycaster;
        private GraphicRaycaster[] m_childGraphicRaycasters;
        private bool m_isChildCanvasDirty = false;
        public override UIType Type => UIType.Window;

        /// <summary>
        /// 窗口位置组件
        /// </summary>
        public override Transform transform => m_windowGo.transform;

        /// <summary>
        /// 窗口矩阵位置组件
        /// </summary>
        public override RectTransform rectTransform => m_windowGo.transform as RectTransform;

        /// <summary>
        /// 窗口实例化资源对象
        /// </summary>
        public override GameObject gameObject => m_windowGo;

        /// <summary>
        /// 窗口名称
        /// </summary>
        public string WindowName { get; private set; }

        /// <summary>
        /// 窗口层级
        /// </summary>
        public int WindowLayer { get; private set; }

        /// <summary>
        /// 资源定位地址
        /// </summary>
        public string AssetLocation { get; private set; }

        /// <summary>
        /// 是否全屏窗口
        /// </summary>
        public virtual bool FullScreen { get; private set; } = false;

        /// <summary>
        /// 是否是Resources资源 无需AB包加载
        /// </summary>
        public bool FromResources { get; private set; }

        /// <summary>
        /// 隐藏窗口关闭时间
        /// </summary>
        public int HideTimeToClose { get; set; }

        /// <summary>
        /// 隐藏窗口关闭时间计时器句柄
        /// </summary>
        public GameTimer HideTimer { get; set; }

        public int SortingOrder
        {
            get=> m_canvas != null ? m_canvas.sortingOrder : 0;
            set
            {
                if (m_canvas == null || m_canvas.sortingOrder == value)
                {
                    return;
                }

                var oldOrder = m_canvas.sortingOrder;

                if (m_isChildCanvasDirty)
                {
                    m_childCanvas = gameObject.GetComponentsInChildren<Canvas>(true);
                    m_isChildCanvasDirty = false;
                }

                //设置子类
                if (m_childCanvas != null && m_childCanvas.Length > 0)
                {
                    for (int i = 0; i < m_childCanvas.Length; i++)
                    {
                        var childCanvas = m_childCanvas[i];

                        if (childCanvas != m_canvas)
                        {
                            childCanvas.sortingOrder = value + (childCanvas.sortingOrder - oldOrder);
                        }
                    }
                }
                // 虚函数
                if (m_isCreated)
                {
                    OnSortDepth(value);
                }
            }
        }

        public bool Visible
        {
            get => m_canvas != null && gameObject.layer == UIModule.WINDOW_SHOW_LAYER;
            set
            {
                if (m_canvas == null)
                {
                    return;
                }

                int setLayer = value ? UIModule.WINDOW_SHOW_LAYER : UIModule.WINDOW_HIDE_LAYER;
                if (gameObject.layer == setLayer)
                {
                    return;
                }
                
                gameObject.layer = setLayer;
                for (int i = 0; i < m_childCanvas.Length; i++)
                {
                    m_childCanvas[i].gameObject.layer = setLayer;
                }

                Interactable = value;

                if (m_isCreated)
                {
                    OnSetVisible(value);
                }
            }
        }

        public bool Interactable
        {
            get => m_graphicRaycaster != null && m_graphicRaycaster.enabled;
            set
            {
                if (m_graphicRaycaster == null)
                {
                    return;
                }
                m_graphicRaycaster.enabled = value;

                for (int i = 0; i < m_childGraphicRaycasters.Length; i++)
                {
                    m_childGraphicRaycasters[i].enabled = value;
                }
            }
        }

        /// <summary>
        /// 是否加载完成
        /// </summary>
        internal bool IsLoadDone = false;

        /// <summary>
        /// 是否被销毁
        /// </summary>
        internal bool IsDestroyed = false;

        /// <summary>
        /// UI是否隐藏
        /// </summary>
        public bool IsHide { get; internal set; } = false;

        #endregion

        public void Init(string windowName, int layer, bool fullScreen, string assetLocation, bool fromResources, int hideTimeToClose)
        {
            WindowName = windowName;
            WindowLayer = layer;
            FullScreen = fullScreen;
            AssetLocation = assetLocation;
            FromResources = fromResources;
            HideTimeToClose = hideTimeToClose;
        }

        internal void TryInvokePrepareCallback(System.Action<UIWindow> prepareCallback, System.Object[] userData)
        {
            CancelHideToCloseTimer();
            base.m_userDatas = userData;

            if (IsPrepared)
            {
                prepareCallback?.Invoke(this);
            }
            else
            {
                m_prepareCallback = prepareCallback;
            }
        }

        internal async UniTaskVoid InternalLoad(string location, System.Action<UIWindow> prepareCallback, bool isAsync, System.Object[] userData)
        {
            m_prepareCallback = prepareCallback;
            base.m_userDatas = userData;

            if (!FromResources)
            {
                if (isAsync)
                {
                    var uiInstance = await UIModule.ResourceLoader.LoadGameObjectAsync(location, UIModule.UICanvas);
                    Handle_Complete(uiInstance);
                }
                else
                {
                    var uiInstance = UIModule.ResourceLoader.LoadGameObject(location, UIModule.UICanvas);
                    Handle_Complete(uiInstance);
                }
            }
            else
            {
                var uiInstance = Object.Instantiate(Resources.Load<GameObject>(location), UIModule.UICanvas);
                Handle_Complete(uiInstance);
            }
        }

        internal void InternalCreate()
        {
            if (!m_isCreated)
            {
                m_isCreated = true;
                ScriptGenerator();
                BindMemberProperty();
                RegisterEvent();
                OnCreate();
            }
        }

        internal bool InternalUpdate()
        {
            if (!IsPrepared || !Visible)
            {
                return false;
            }

            List<UIWidget> listNextUpdateChild = null;

            if (ChildList != null && ChildList.Count > 0)
            {
                listNextUpdateChild = m_updateChildList;
                var updateListDirty = m_updateListDirty;
                List<UIWidget> childList = null;
                if (updateListDirty)
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

                    var needValid = uiWidget.InternalUpdate();

                    if (updateListDirty && needValid)
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

            if (listNextUpdateChild == null || listNextUpdateChild.Count <= 0)
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

        internal void InternalDestroy()
        {
            m_isCreated = false;
            RemoveAllUIEvents();

            for (int i = 0; i < ChildList.Count; i++)
            {
                var uiChild = ChildList[i];
                uiChild.CallDestroy();
                uiChild.OnDestroyWidget();
            }

            m_prepareCallback = null;
            OnDestroy();

            if (m_windowGo != null)
            {
                Object.Destroy(m_windowGo);
                m_windowGo = null;
            }

            IsDestroyed = true;

            CancelHideToCloseTimer();
        }

        private void Handle_Complete(GameObject windowGo)
        {
            if (windowGo == null)
            {
                return;
            }

            IsLoadDone = true;

            if (IsDestroyed)
            {
                Object.Destroy(windowGo);
                return;
            }

            windowGo.name = GetType().Name;
            m_windowGo = windowGo;
            m_windowGo.transform.localPosition = Vector3.zero;

            UIDebugBehaviour.AddUIDebugBehaviour(windowGo);

            m_canvas = m_windowGo.GetComponent<Canvas>();
            if (m_canvas == null)
            {
                throw new DGameException($"在UI窗口 {WindowName} 没有找到 {nameof(Canvas)}");
            }
            m_canvas.overrideSorting = true;
            m_canvas.sortingOrder = 0;
            m_canvas.sortingLayerName = "Default";
            m_graphicRaycaster = m_windowGo.GetComponent<GraphicRaycaster>();
            m_childCanvas = m_windowGo.GetComponentsInChildren<Canvas>(true);
            m_childGraphicRaycasters = m_windowGo.GetComponentsInChildren<GraphicRaycaster>(true);

            m_isChildCanvasDirty = false;

            IsPrepared = true;
            m_prepareCallback?.Invoke(this);
        }

        internal void CancelHideToCloseTimer()
        {
            IsHide = false;
            ModuleSystem.GetModule<IGameTimerModule>().DestroyGameTimer(HideTimer);
            HideTimer = null;
        }

        public void Show(bool visible)
        {
            // UIModule.Instance.ShowWindow(this);
        }

        public void MakeChildCanvasDirty()
        {
            m_isChildCanvasDirty = true;
        }

        protected virtual void Hide()
        {
            UIModule.Instance.HideWindow(this);
        }

        protected virtual void Close()
        {
            UIModule.Instance.CloseWindow(this);
        }
    }
}