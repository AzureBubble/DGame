using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
			m_btnClose.onClick.AddListener(UniTask.UnityAction(OnClickCloseBtn));
		}

		#endregion

		#region override

		protected override void OnCreate()
		{
			base.OnCreate();
			RefreshUI();
		}

		protected override ModelType GetModelType()
		{
			return ModelType.NoneType;
		}

		#endregion

		#region 字段

		private readonly Stack<string> m_errorTextStack = new Stack<string>();

		#endregion

		#region 函数

		public void RefreshUI()
		{
			m_errorTextStack.Push(UserData.ToString());
			m_textError.text = UserData.ToString();
		}

		#endregion

		#region 事件

		private async UniTaskVoid OnClickCloseBtn()
		{
			if (m_errorTextStack.Count <= 0)
			{
				await UniTask.Yield();
				Close();
				return;
			}

			string error = m_errorTextStack.Pop();
			m_textError.text = error;
		}

		#endregion
	}
}