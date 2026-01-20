using UnityEngine.UI;

namespace GameLogic
{
    public class GMPanel : UIWindow
    {
	    #region 脚本工具生成的代码

		private UIBindComponent m_bindComponent;
		private Text m_textDisplay;
		private Button m_btnClose;
		private InputField m_inputGm;
		private InputField m_inputGmID;
		private InputField m_inputGmNum;
		private InputField m_inputGmExecute;
		private Button m_btnExecute;
		private Button m_btnServerTemp;
		private Button m_btnClientTemp;
		private Button m_btnBatchTemp;
		private Button m_btnOtherTemp;
		private Button m_btnLastExecute;
		private Dropdown m_dropDownSelect;
		private InputField m_inputOldGm;
		private Button m_btnExecuteNoClose;

		protected override void ScriptGenerator()
		{
			m_bindComponent = gameObject.GetComponent<UIBindComponent>();
			m_textDisplay = m_bindComponent.GetComponent<Text>(0);
			m_btnClose = m_bindComponent.GetComponent<Button>(1);
			m_inputGm = m_bindComponent.GetComponent<InputField>(2);
			m_inputGmID = m_bindComponent.GetComponent<InputField>(3);
			m_inputGmNum = m_bindComponent.GetComponent<InputField>(4);
			m_inputGmExecute = m_bindComponent.GetComponent<InputField>(5);
			m_btnExecute = m_bindComponent.GetComponent<Button>(6);
			m_btnServerTemp = m_bindComponent.GetComponent<Button>(7);
			m_btnClientTemp = m_bindComponent.GetComponent<Button>(8);
			m_btnBatchTemp = m_bindComponent.GetComponent<Button>(9);
			m_btnOtherTemp = m_bindComponent.GetComponent<Button>(10);
			m_btnLastExecute = m_bindComponent.GetComponent<Button>(11);
			m_dropDownSelect = m_bindComponent.GetComponent<Dropdown>(12);
			m_inputOldGm = m_bindComponent.GetComponent<InputField>(13);
			m_btnExecuteNoClose = m_bindComponent.GetComponent<Button>(14);
			m_btnClose.onClick.AddListener(OnClickCloseBtn);
			m_btnExecute.onClick.AddListener(OnClickExecuteBtn);
			m_btnServerTemp.onClick.AddListener(OnClickServerTempBtn);
			m_btnClientTemp.onClick.AddListener(OnClickClientTempBtn);
			m_btnBatchTemp.onClick.AddListener(OnClickBatchTempBtn);
			m_btnOtherTemp.onClick.AddListener(OnClickOtherTempBtn);
			m_btnLastExecute.onClick.AddListener(OnClickLastExecuteBtn);
			m_btnExecuteNoClose.onClick.AddListener(OnClickExecuteNoCloseBtn);
		}

		#endregion

		#region 字段



		#endregion

		#region 函数



		#endregion

		#region 事件

		private void OnClickLastExecuteBtn()
		{
		}

		private void OnClickExecuteNoCloseBtn()
		{
		}

		private void OnClickCloseBtn()
		{
			Close();
		}

		private void OnClickExecuteBtn()
		{
		}

		private void OnClickServerTempBtn()
		{
		}

		private void OnClickClientTempBtn()
		{
		}

		private void OnClickBatchTempBtn()
		{
		}

		private void OnClickOtherTempBtn()
		{
		}

		#endregion
    }
}