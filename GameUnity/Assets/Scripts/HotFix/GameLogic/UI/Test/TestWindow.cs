using UnityEngine;
using UnityEngine.UI;
using DGame;

namespace GameLogic
{
	[Window(UILayer.UI, location : "TestWindow")]
	public class TestWindow : UIWindow
	{
		#region 脚本工具生成的代码

		private TestWindowDataComponent m_bindComponent;
		private Button m_btnTest;
		private Text m_textTest;
		private Scrollbar m_scrollBarTest;
		private GameObject m_itemCommonDesc;
		private Transform m_tfGo;
		private GameObject m_itemCommonDesc1;
		private Toggle m_toggleTest;

		protected override void ScriptGenerator()
		{
			m_bindComponent = gameObject.GetComponent<TestWindowDataComponent>();
			m_btnTest = m_bindComponent.m_btnTest;
			m_textTest = m_bindComponent.m_textTest;
			m_scrollBarTest = m_bindComponent.m_scrollBarTest;
			m_itemCommonDesc = m_bindComponent.m_itemCommonDesc;
			m_tfGo = m_bindComponent.m_tfGo;
			m_itemCommonDesc1 = m_bindComponent.m_itemCommonDesc1;
			m_toggleTest = m_bindComponent.m_toggleTest;
			m_btnTest.onClick.AddListener(OnClickTestBtn);
			m_toggleTest.onValueChanged.AddListener(OnToggleTestChange);
		}

		#endregion

		#region 事件

		private void OnClickTestBtn()
		{
			Debugger.Warning("OnClickTestBtn");
		}

		private void OnToggleTestChange(bool isOn)
		{
		}

		#endregion
	}
}