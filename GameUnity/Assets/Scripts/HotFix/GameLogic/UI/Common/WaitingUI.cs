using DG.Tweening;
using DGame;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class WaitingUI : UIWindow
    {
        #region 脚本工具生成的代码

        private RectTransform m_rectContent;
        private Transform m_tfEffect;
        private Transform m_tfImage;
        private Text m_textTips;

        protected override void ScriptGenerator()
        {
            m_rectContent = FindChildComponent<RectTransform>("m_rectContent");
            m_tfEffect = FindChild("m_rectContent/m_tfEffect");
            m_tfImage = FindChild("m_rectContent/m_tfEffect/m_tfImage");
            m_textTips = FindChildComponent<Text>("m_rectContent/m_textTips");
        }

        #endregion

        #region Override

        protected override bool NeedTweenPop => false;

        protected override ModelType GetModelType() => ModelType.TransparentType;

        protected override UILayer windowLayer => UILayer.Tips;

        protected override void OnCreate()
        {
            m_lastShowTime = GameTime.UnscaledTime;
            StartRotateAnimation();
        }

        protected override void OnVisible()
        {
            m_lastShowTime = GameTime.UnscaledTime;
        }

        protected override void OnUpdate()
        {
            float timer = GameTime.UnscaledTime - m_lastShowTime;

            if (timer > MAX_WAITING_UI_SHOW_TIME && m_waitFuncID != WaitingUISeq.RELAY_BATTLE)
            {
                Close();
            }
        }

        protected override void OnDestroy()
        {
            m_tfImage.DOKill();
        }

        #endregion

        #region 字段

        // 等待界面最大显示时间，超过这个时间，没有收到回报就关闭界面
        private const float MAX_WAITING_UI_SHOW_TIME = 10;

        private float m_lastShowTime;
        private uint m_waitFuncID;

        #endregion

        #region 函数

        public void Init(uint waitFuncID, string tips, System.Action callback)
        {
            if (!string.IsNullOrEmpty(tips))
            {
                m_textTips.text = tips;
            }
            m_waitFuncID = waitFuncID;
            callback?.Invoke();
        }

        public bool FinishWaiting(uint waitFuncID)
        {
            if (waitFuncID == 0 || waitFuncID == m_waitFuncID)
            {
                m_waitFuncID = 0;
                return true;
            }
            return false;
        }

        private void StartRotateAnimation()
        {
            m_tfImage.DOLocalRotate(new Vector3(0, 0, -360), 3f, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental)
                .SetUpdate(true);
        }

        #endregion
    }
}