
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DGame;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public sealed class UIModule : Singleton<UIModule>, IUpdate
    {
        #region 常量定义

        public const int DEFAULT_LAYER_DEEP = 2000;
        public const int EACH_WINDOW_DEEP = 100;
        public const int WINDOW_HIDE_LAYER = 2; // Ignore Raycast
        public const int WINDOW_SHOW_LAYER = 5; // UI

        #endregion

        private static Transform m_uiCanvas;
        private bool m_enableErrorLog = true;
        private Camera m_uiCamera;
        private readonly List<UIWindow> m_uiStack = new List<UIWindow>(128);
        private readonly Dictionary<string, UIWindow> m_uiMap = new Dictionary<string, UIWindow>(128);
        private ErrorLogger m_errorLogger;

        /// <summary>
        /// 资源加载接口
        /// </summary>
        public static IUIResourceLoader ResourceLoader;

        /// <summary>
        /// UI根节点
        /// </summary>
        public static Transform UICanvas => m_uiCanvas;

        /// <summary>
        /// UI摄像机
        /// </summary>
        public Camera UICamera => m_uiCamera;

        protected override void OnInit()
        {
            var uiRoot = GameObject.Find("UIRoot");

            if (uiRoot != null)
            {
                m_uiCanvas = uiRoot.GetComponentInChildren<Canvas>()?.transform;
                m_uiCamera = uiRoot.GetComponentInChildren<Camera>();
            }
            else
            {
                Debugger.Fatal("不存在UIRoot");
                return;
            }

            ResourceLoader = new UIResourceLoader();
            UnityEngine.Object.DontDestroyOnLoad(uiRoot);

            if (m_uiCanvas != null)
            {
                m_uiCanvas.gameObject.layer = LayerMask.NameToLayer("UI");
            }

            if (m_enableErrorLog)
            {
                m_errorLogger = new ErrorLogger(this);
            }
        }

        public void OnUpdate()
        {
            if (m_uiStack == null || m_uiStack.Count <= 0)
            {
                return;
            }

            int count = m_uiStack.Count;

            for (int i = 0; i < m_uiStack.Count; i++)
            {
                if (m_uiStack.Count != count)
                {
                    return;
                }
                var window = m_uiStack[i];
                window.InternalUpdate();
            }
        }

        /// <summary>
        /// 模块销毁释放
        /// </summary>
        protected override void OnDestroy()
        {
            if (m_errorLogger != null)
            {
                m_errorLogger.Dispose();
                m_errorLogger = null;
            }

            if (m_uiCanvas != null && m_uiCanvas.parent != null)
            {
                UnityEngine.Object.Destroy(m_uiCanvas.parent.gameObject);
            }
        }

        #region 设置安全区域

        /// <summary>
        /// 设置屏幕安全区域（异形屏支持）
        /// </summary>
        /// <param name="safeRect">安全区域举行（基于屏幕像素坐标）</param>
        public static void ApplyScreenSafeRect(Rect safeRect)
        {
            CanvasScaler scaler = UICanvas?.GetComponentInParent<CanvasScaler>();

            if (scaler == null)
            {
                Debugger.Error($"没有找到 {nameof(CanvasScaler)}");
                return;
            }

            float rateX = scaler.referenceResolution.x / Screen.width;
            float rateY = scaler.referenceResolution.y / Screen.height;
            float posX = (int)(safeRect.position.x / rateX);
            float posY = (int)(safeRect.position.y / rateY);
            float width = (int)(safeRect.size.x / rateX);
            float height = (int)(safeRect.size.y / rateY);

            float offsetX = scaler.referenceResolution.x - width - posX;
            float offsetY = scaler.referenceResolution.y - height - posY;

            // 注意：安全区域坐标系的原点为左下角
            var rectTrans = UICanvas.transform as RectTransform;

            if (rectTrans != null)
            {
                // 锚框状态下的屏幕左下角偏移向量
                rectTrans.offsetMin = new Vector2(posX, posY);
                // 锚框状态下的屏幕右上角角偏移向量
                rectTrans.offsetMax = new Vector2(-offsetX, -offsetY);
            }
        }

        public static void SimulateIPhoneXNotchScreen()
        {
            Rect rect;

            if (Screen.height > Screen.width)
            {
                // 竖屏
                float deviceWidth = 1125;
                float deviceHeight = 2436;
                rect = new Rect(0 / deviceWidth, 102f / deviceHeight, 1125f / deviceWidth, 2202f / deviceHeight);
            }
            else
            {
                // 横屏
                float deviceWidth = 2436;
                float deviceHeight = 1125;
                rect = new Rect(132f / deviceWidth, 63f / deviceHeight, 2172f / deviceWidth, 1062f / deviceHeight);
            }

            Rect safeArea = new Rect(Screen.width * rect.x, Screen.height * rect.y, Screen.width * rect.width, Screen.height * rect.height);
            ApplyScreenSafeRect(safeArea);
        }

        #endregion

        #region GetTopWindow

        /// <summary>
        /// 获取所有层级下顶部的窗口名称
        /// </summary>
        /// <returns></returns>
        public string GetTopWindow()
        {
            if (m_uiStack.Count == 0)
            {
                return string.Empty;
            }
            // 队列最后一个一定是最顶部窗口
            UIWindow window = m_uiStack[^1];
            return window.WindowName;
        }

        public string GetTopWindow(int layer)
        {
            UIWindow lastOne = null;

            for (int i = 0; i < m_uiStack.Count; i++)
            {
                if (m_uiStack[i].WindowLayer == layer)
                {
                    lastOne = m_uiStack[i];
                }
            }

            if (lastOne == null)
            {
                return string.Empty;
            }
            return lastOne.WindowName;
        }

        #endregion

        #region ContainsWindow

        public bool ContainsWindow<T>()
        {
            return ContainsWindow(typeof(T));
        }

        public bool ContainsWindow(Type type)
        {
            return IsContains(type.Name);
        }

        private bool IsContains(string windowName)
        {
            return m_uiMap.TryGetValue(windowName, out UIWindow window);
        }

        #endregion

        #region ShowWindowAsync

        public void ShowWindowAsync<T>(params System.Object[] userDatas) where T : UIWindow, new()
        {
            ShowWindowImp<T>(true, userDatas);
        }

        public void ShowWindowAsync(Type type, params System.Object[] userDatas)
        {
            ShowWindowImp(type, true, userDatas);
        }

        public void ShowWindow<T>(params System.Object[] userDatas) where T : UIWindow, new()
        {
            ShowWindowImp<T>(false, userDatas);
        }

        public void ShowWindow(Type type, params System.Object[] userDatas)
        {
            ShowWindowImp(type, false, userDatas);
        }

        private void ShowWindowImp<T>(bool isAsync, params System.Object[] userDatas) where T : UIWindow, new()
        {
            Type type = typeof(T);
            string windowName = type.Name;

            if (!TryGetWindow(windowName, out UIWindow window, userDatas))
            {
                window = CreateWindow<T>();
                Push(window);
                window.InternalLoad(window.AssetLocation, OnWindowPrepare, isAsync, userDatas).Forget();
            }
        }

        private void ShowWindowImp(Type type, bool isAsync, params System.Object[] userDatas)
        {
            string windowName = type.Name;

            if (!TryGetWindow(windowName, out UIWindow window, userDatas))
            {
                window = CreateWindow(type);
                Push(window);
                window.InternalLoad(window.AssetLocation, OnWindowPrepare, isAsync, userDatas).Forget();
            }
        }

        public async UniTask<T> ShowWindowAsyncAwait<T>(params System.Object[] userDatas) where T : UIWindow, new()
        {
            return await ShowWindowAwaitImp<T>(true, userDatas);
        }

        private async UniTask<T> ShowWindowAwaitImp<T>(bool isAsync, params System.Object[] userDatas) where T : UIWindow, new()
        {
            Type type = typeof(T);
            string windowName = type.Name;

            if (TryGetWindow(windowName, out var window, userDatas))
            {
                return window as T;
            }
            else
            {
                window = CreateWindow<T>();
                Push(window);
                window.InternalLoad(window.AssetLocation, OnWindowPrepare, isAsync, userDatas).Forget();
                float time = 0f;

                while (!window.IsLoadDone)
                {
                    time += Time.deltaTime;

                    if (time > 60f)
                    {
                        break;
                    }
                    await UniTask.Yield();
                }
                return window as T;
            }
        }

        #endregion

        #region CreateWindow

        private UIWindow CreateWindow<T>() where T : UIWindow, new()
        {
            Type type = typeof(T);
            UIWindow window = new T();
            WindowAttribute windowAttribute = Attribute.GetCustomAttribute(type, typeof(WindowAttribute)) as WindowAttribute;

            if (window == null)
            {
                throw new DGameException($"窗口 {type.Name} 窗口实例失败");
            }

            if (windowAttribute != null)
            {
                string assetLocation = string.IsNullOrEmpty(windowAttribute.Location) ? type.Name : windowAttribute.Location;
                window.Init(type.Name, windowAttribute.UILayer, windowAttribute.FullScreen, assetLocation, windowAttribute.IsResources, windowAttribute.NeedTweenPop, windowAttribute.HideTimeToClose);
            }
            else
            {
                window.Init(type.Name, (int)UILayer.UI, window.FullScreen, type.Name, false,  !window.FullScreen, 10);
            }
            return window;
        }

        private UIWindow CreateWindow(Type type)
        {
            UIWindow window = Activator.CreateInstance(type) as UIWindow;
            WindowAttribute windowAttribute = Attribute.GetCustomAttribute(type, typeof(WindowAttribute)) as WindowAttribute;

            if (window == null)
            {
                throw new DGameException($"窗口 {type.Name} 窗口实例失败");
            }

            if (windowAttribute != null)
            {
                string assetLocation = string.IsNullOrEmpty(windowAttribute.Location) ? type.Name : windowAttribute.Location;
                window.Init(type.Name, windowAttribute.UILayer, windowAttribute.FullScreen, assetLocation, windowAttribute.IsResources, windowAttribute.NeedTweenPop, windowAttribute.HideTimeToClose);
            }
            else
            {
                window.Init(type.Name, (int)UILayer.UI, window.FullScreen, type.Name, false, !window.FullScreen, 10);
            }
            return window;
        }

        #endregion

        #region GetWindow

        private bool TryGetWindow(string windowName, out UIWindow window, params System.Object[] userDatas)
        {
            window = null;
            if (IsContains(windowName))
            {
                window = GetWindow(windowName);
                Pop(window);
                Push(window);
                window.TryInvokePrepareCallback(OnWindowPrepare, userDatas);
                return true;
            }

            return false;
        }

        private UIWindow GetWindow(string windowName)
        {
            return m_uiMap.TryGetValue(windowName, out UIWindow window) ? window : null;
        }

#if UNITY_EDITOR && ENABLE_DGAME_LOG

        /// <summary>
        /// 仅调试用
        /// </summary>
        public UIWindow GetWindowByName(string windowName)
        {
            return GetWindow(windowName);
        }

#endif

        public async UniTask<T> GetWindowAsyncAwait<T>(CancellationToken cancellationToken = default) where T : UIWindow
        {
            string windowName = typeof(T).Name;
            var window = GetWindow(windowName);

            if (window == null)
            {
                return null;
            }

            var result = window as T;
            if (result == null)
            {
                return null;
            }

            if (result.IsLoadDone)
            {
                return result;
            }

            float time = 0f;

            while (!result.IsLoadDone)
            {
                time += Time.deltaTime;

                if (time > 60f)
                {
                    break;
                }
                await UniTask.Yield(cancellationToken);
            }
            return result;
        }

        public void GetWindowAsync<T>(Action<T> callback) where T : UIWindow
        {
            string windowName = typeof(T).Name;
            var window = GetWindow(windowName);
            if (window == null)
            {
                return;
            }

            var result = window as T;
            if (result == null)
            {
                return;
            }

            GetWindowAsyncImp(callback).Forget();

            async UniTaskVoid GetWindowAsyncImp(Action<T> ctx)
            {
                float time = 0f;

                while (!result.IsLoadDone)
                {
                    time += Time.deltaTime;

                    if (time > 60f)
                    {
                        break;
                    }
                    await UniTask.Yield();
                }
                ctx?.Invoke(result);
            }
        }

        #endregion

        #region CloseWindow

        public void CloseWindow<T>() where T : UIWindow
        {
            CloseWindow(typeof(T));
        }

        private void CloseWindow(Type type)
        {
            string windowName = type.Name;
            UIWindow window = GetWindow(windowName);

            if (window == null)
            {
                return;
            }
            CloseWindow(window);
        }

        private void HideWindow<T>() where T : UIWindow
        {
            HideWindow(typeof(T));
        }

        private void HideWindow(Type type)
        {
            string windowName = type.Name;
            UIWindow window = GetWindow(windowName);

            if (window == null)
            {
                return;
            }

            HideWindow(window);
        }

        public void HideWindow(UIWindow window)
        {
            if (window == null)
            {
                return;
            }

            if (window.HideTimeToClose < 0)
            {
                CloseWindow(window);
                return;
            }
            window.CancelHideToCloseTimer();
            window.Visible = false;
            window.IsHide = true;
            window.HideTimer = GameModule.GameTimerModule.CreateOnceGameTimer(window.HideTimeToClose, _ =>
            {
                CloseWindow(window);
            });

            if (window.FullScreen)
            {
                OnSetWindowVisible();
            }
        }

        public void CloseWindow(UIWindow window)
        {
            if (window == null)
            {
                return;
            }
            window.InternalDestroy();
            Pop(window);
            OnSortWindowSortingOrder(window.WindowLayer);
            OnSetWindowVisible();
        }

        public void CloseAllWindows(bool isDestroy = false)
        {
            for (int i = 0; i < m_uiStack.Count; i++)
            {
                UIWindow window = m_uiStack[i];
                window.InternalDestroy();
            }
            m_uiStack.Clear();
            m_uiMap.Clear();
        }

        public void CloseAllWindowsWithOut(List<UIWindow> withOutWindows)
        {
            for (int i = m_uiStack.Count - 1; i >= 0; i--)
            {
                UIWindow window = m_uiStack[i];

                if (!withOutWindows.Contains(window))
                {
                    window.InternalDestroy();
                    m_uiStack.RemoveAt(i);
                    m_uiMap.Remove(window.WindowName);
                }
            }
        }

        public void CloseAllWindowsWithOut(UIWindow withOutWindows)
        {
            for (int i = m_uiStack.Count - 1; i >= 0; i--)
            {
                UIWindow window = m_uiStack[i];

                if (window != withOutWindows)
                {
                    window.InternalDestroy();
                    m_uiStack.RemoveAt(i);
                    m_uiMap.Remove(window.WindowName);
                }
            }
        }

        public void CloseAllWindowsWithOut<T>() where T : UIWindow
        {
            for (int i = m_uiStack.Count - 1; i >= 0; i--)
            {
                UIWindow window = m_uiStack[i];

                if (window.GetType() != typeof(T))
                {
                    window.InternalDestroy();
                    m_uiStack.RemoveAt(i);
                    m_uiMap.Remove(window.WindowName);
                }
            }
        }

        #endregion

        public bool IsAnyLoading()
        {
            for (int i = 0; i < m_uiStack.Count; i++)
            {
                var window = m_uiStack[i];

                if (!window.IsLoadDone)
                {
                    return true;
                }
            }
            return false;
        }

        private void OnWindowPrepare(UIWindow window)
        {
            window.InternalCreate();
            OnSortWindowSortingOrder(window.WindowLayer);
            OnSetWindowVisible();
        }

        private void OnSetWindowVisible()
        {
            bool isHideNext = false;

            // 隐藏所有层级低于最上级全屏显示窗口的窗口
            for (int i = m_uiStack.Count - 1; i >= 0; i--)
            {
                UIWindow window = m_uiStack[i];

                if (!isHideNext)
                {
                    if (window.IsHide)
                    {
                        continue;
                    }

                    window.Visible = true;

                    if (window.IsPrepared && window.FullScreen)
                    {
                        isHideNext = true;
                    }
                }
                else
                {
                    window.Visible = false;
                }
            }
        }

        private void OnSortWindowSortingOrder(int layer)
        {
            // 重新排序所有的窗口层级
            int sortingOrder = layer * DEFAULT_LAYER_DEEP;

            for (int i = 0; i < m_uiStack.Count; i++)
            {
                if (m_uiStack[i].WindowLayer == layer)
                {
                    m_uiStack[i].SortingOrder = sortingOrder;
                    sortingOrder += EACH_WINDOW_DEEP;
                }
            }
        }


        private void Pop(UIWindow window)
        {
            m_uiStack.Remove(window);
            m_uiMap.Remove(window.WindowName);
        }

        private void Push(UIWindow window)
        {
            if (IsContains(window.WindowName))
            {
                throw new DGameException($"UI窗口 {window.WindowName} 已存在");
            }
            int insertIndex = -1;

            for (int i = 0; i < m_uiStack.Count; i++)
            {
                if (window.WindowLayer == m_uiStack[i].WindowLayer)
                {
                    insertIndex = i + 1;
                }
            }

            if (insertIndex == -1)
            {
                for (int i = 0; i < m_uiStack.Count; i++)
                {
                    if (window.WindowLayer > m_uiStack[i].WindowLayer)
                    {
                        insertIndex = i + 1;
                    }
                }
            }

            if (insertIndex == -1)
            {
                insertIndex = 0;
            }
            m_uiStack.Insert(insertIndex, window);
            m_uiMap[window.WindowName] = window;
        }
    }
}