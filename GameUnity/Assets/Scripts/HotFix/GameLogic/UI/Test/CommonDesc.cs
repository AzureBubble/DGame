using UnityEngine;
using UnityEngine.UI;
using DGame;

namespace GameLogic
{
	public class CommonDesc : UIWidget
	{
		#region 脚本工具生成的代码

		private CommonDescWidgetDataComponent m_bindComponent;
		private Text m_textTest;
		private Text m_textTest1;

		protected override void ScriptGenerator()
		{
			m_bindComponent = gameObject.GetComponent<CommonDescWidgetDataComponent>();
			m_textTest = m_bindComponent.m_textTest;
			m_textTest1 = m_bindComponent.m_textTest1;
		}

		#endregion

		#region 事件

		#endregion
	}
}
