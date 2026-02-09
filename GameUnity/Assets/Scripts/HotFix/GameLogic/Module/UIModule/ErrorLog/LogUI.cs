using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DGame;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
	public partial class LogUI : UIWindow
	{
		#region 脚本工具生成的代码

		private Button m_btnClose;
		private Text m_textError;

		protected override void ScriptGenerator()
		{
			m_btnClose = FindChildComponent<Button>("m_btnClose");
			m_textError = FindChildComponent<Text>("m_textError");
			m_btnClose.onClick.AddListener(OnClickCloseBtn);
		}

		#endregion

		#region Override

		protected override ModelType GetModelType() => ModelType.NoneType;

		protected override UILayer windowLayer => UILayer.System;

		#endregion

		#region 字段

		private readonly Stack<string> m_errorTextStack = new Stack<string>();
		private bool m_isInit = false;

		#endregion

		#region 函数

		public void Init(string errorText)
		{
			m_errorTextStack.Push(errorText);

			if (!m_isInit)
			{
				m_textError.text = m_errorTextStack.Pop();
				m_isInit = true;
			}
		}

		#endregion

		#region 事件

		private void OnClickCloseBtn()
		{
			if (m_errorTextStack.Count <= 0)
			{
				Close();
				return;
			}

			string error = m_errorTextStack.Pop();
			m_textError.text = error;
		}

		#endregion
	}
}