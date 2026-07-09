using UnityEngine;

namespace GameLogic
{
    public class SetUISafeFitHelper
    {
        /// <summary>
        /// 是否适配刘海屏
        /// </summary>
        public bool LiuHaiFit { get; set; } = false;

        /// <summary>
        /// 顶部适配偏移高度
        /// </summary>
        public float TopSpacing { get; set; } = 0;

        /// <summary>
        /// 是否底部适配
        /// </summary>
        public bool BottomFit { get; set; } = false;

        /// <summary>
        /// 底部适配偏移高度
        /// </summary>
        public float BottomSpacing { get; set; } = 0;

        private readonly RectTransform m_curFitRect;

        /// <summary>
        /// 移动设备屏幕适配
        /// </summary>
        /// <param name="fitRect">适配的RectTransform对象</param>
        /// <param name="liuHaiFit">是否开启刘海屏顶部适配</param>
        /// <param name="topSpacing">刘海屏顶部适配偏移高度</param>
        /// <param name="bottomFit">是否开启刘海屏底部适配</param>
        /// <param name="bottomSpacing">刘海屏底部适配偏移高度</param>
        public SetUISafeFitHelper(RectTransform fitRect, bool liuHaiFit = true, float topSpacing = 0, bool bottomFit = true, float bottomSpacing = 0)
        {
            LiuHaiFit = liuHaiFit;
            TopSpacing = topSpacing;
            BottomFit = bottomFit;
            BottomSpacing = bottomSpacing;
            m_curFitRect = fitRect;
        }

        public SetUISafeFitHelper() { }

        /// <summary>
        /// 设置UI安全区域适配
        /// </summary>
        public void SetUIFit()
        {
            if (m_curFitRect == null)
            {
                return;
            }

            Vector3 offsetMax = new Vector2(0f, 0f);
            Vector3 offsetMin = new Vector2(0f, 0f);

            // 挖孔屏
            Rect[] cutouts = Screen.cutouts;

            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    TopSpacing = 70;
                    BottomSpacing = 80;
                    break;

                case RuntimePlatform.Android:
                    break;

                case RuntimePlatform.IPhonePlayer:
                    var phoneType = SystemInfo.deviceModel;
                    TopSpacing = 70;
                    BottomSpacing = 80;

                    if (phoneType == "iPhone12,1" || phoneType == "iPhone11,8")
                    {
                        //特定机型做下特点的偏移
                        TopSpacing = 30;
                        BottomSpacing = 70;
                    }

                    break;
            }

            // 横屏：把竖屏的上下逻辑镜像到左右两侧
            // LiuHaiFit(Top) 对准刘海侧、BottomFit(Bottom) 对准非刘海侧
            if (Screen.width > Screen.height)
            {
                SetUIFitLandscape();
                return;
            }

            //启动刘海适配
            if (LiuHaiFit)
            {
                if (cutouts != null && cutouts.Length > 0 && Application.platform != RuntimePlatform.IPhonePlayer)
                {
                    offsetMax = new Vector3(m_curFitRect.offsetMax.x, (cutouts[0].height));
                }
                else if (Screen.safeArea.yMax > 0 && Screen.height - Screen.safeArea.yMax > 0)
                {
                    offsetMax = new Vector3(Screen.width - Screen.safeArea.xMax, Screen.height - (Screen.safeArea.yMax + TopSpacing));
                }
                //刘海屏适配
                m_curFitRect.offsetMax = new Vector2(offsetMax.x, -offsetMax.y);
            }
            else
            {
                //非刘海屏适配
                m_curFitRect.offsetMax = offsetMax;
            }

            //启动底部适配
            if (BottomFit)
            {
                if (Screen.safeArea.y > 0)
                {
                    offsetMin = new Vector2(Screen.safeArea.x, Mathf.Abs(Screen.safeArea.y - BottomSpacing));
                }

                if (Mathf.Abs(offsetMin.y) > 0)
                {
                    m_curFitRect.offsetMin = new Vector2(m_curFitRect.offsetMin.x, Mathf.Abs(offsetMin.y));
                }
                else
                {
                    m_curFitRect.offsetMin = offsetMin;
                }
            }
            else
            {
                m_curFitRect.offsetMin = offsetMin;
            }
        }

        /// <summary>
        /// 横屏适配：复用竖屏上下逻辑，映射到左右两侧。
        /// 刘海侧套用顶部逻辑（TopSpacing 回补），非刘海侧套用底部逻辑（BottomSpacing 回补）。
        /// 刘海在左还是右由安全区左右内缩量自动判定，无需固定横屏方向。
        /// </summary>
        private void SetUIFitLandscape()
        {
            Rect safeArea = Screen.safeArea;
            // 左右两侧被系统安全区裁掉的量（屏幕像素）
            float leftInset = Mathf.Max(0f, safeArea.xMin);
            float rightInset = Mathf.Max(0f, Screen.width - safeArea.xMax);

            // 刘海方向由屏幕朝向决定，而非左右内缩大小：
            // 系统 safeArea 常左右对称收缩，用 inset 大小无法区分刘海真实方向。
            // LandscapeLeft：竖屏顶部转到左边 → 刘海在左；LandscapeRight → 刘海在右。
            bool notchOnLeft = Screen.orientation != ScreenOrientation.LandscapeRight;
            float notchInset = notchOnLeft ? leftInset : rightInset;
            float otherInset = notchOnLeft ? rightInset : leftInset;

            // 刘海侧：对应竖屏 Top，安全区内缩基础上用 TopSpacing 回补（减少内缩）
            float notchFit = 0f;
            if (LiuHaiFit && notchInset > 0f)
            {
                notchFit = Mathf.Max(0f, notchInset - TopSpacing);
            }

            // 非刘海侧：对应竖屏 Bottom，安全区内缩基础上用 BottomSpacing 回补
            float otherFit = 0f;
            if (BottomFit && otherInset > 0f)
            {
                otherFit = Mathf.Abs(otherInset - BottomSpacing);
            }

            Vector2 offsetMin = m_curFitRect.offsetMin;
            Vector2 offsetMax = m_curFitRect.offsetMax;

            if (notchOnLeft)
            {
                // 左侧（刘海）向右内缩
                offsetMin.x = notchFit;    
                // 右侧（非刘海）向左内缩
                offsetMax.x = -otherFit;   
            }
            else
            {
                // 右侧（刘海）向左内缩
                offsetMax.x = -notchFit;  
                // 左侧（非刘海）向右内缩
                offsetMin.x = otherFit;    
            }

            // 横屏只处理左右，纵向保持铺满
            offsetMin.y = 0f;
            offsetMax.y = 0f;

            m_curFitRect.offsetMin = offsetMin;
            m_curFitRect.offsetMax = offsetMax;
        }

        /// <summary>
        /// 设置某一个节点不受m_curRect影响
        /// </summary>
        /// <param name="rect"></param>
        public void SetUINotFit(RectTransform rect)
        {
            if (m_curFitRect == null || rect == null)
            {
                return;
            }

            var position = rect.anchoredPosition;

            rect.anchoredPosition = new Vector2(position.x - m_curFitRect.sizeDelta.x,
                position.y - m_curFitRect.sizeDelta.y);
        }

        /// <summary>
        /// 设置某一个节点不受指定RectTransform的影响
        /// </summary>
        /// <param name="rect">设置的RectTransform</param>
        /// <param name="refRect">依赖的RectTransform</param>
        public void SetUINotFit(RectTransform rect, RectTransform refRect)
        {
            if (rect == null || refRect == null)
            {
                return;
            }

            var position = rect.anchoredPosition;

            rect.anchoredPosition = new Vector2(position.x - refRect.sizeDelta.x,
                position.y - refRect.sizeDelta.y);
        }
    }
}