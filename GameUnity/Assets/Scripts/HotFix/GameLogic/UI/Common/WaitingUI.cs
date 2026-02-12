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

        protected override ModelType GetModelType() => ModelType.TransparentType;

        protected override UILayer windowLayer => UILayer.Tips;

        #endregion

        #region 字段



        #endregion

        #region 函数

        public void Init(uint waitFuncID, uint textID, System.Action callback)
        {

        }

        #endregion
    }
}