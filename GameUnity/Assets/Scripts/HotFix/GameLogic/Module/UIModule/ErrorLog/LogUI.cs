using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

namespace GameLogic
{
	public partial class LogUI : UIWindow
	{
		#region 脚本工具生成的代码

		private UIBindComponent m_bindComponent;
		private Button m_btnClose;
		private Text m_textError;

		protected override void ScriptGenerator()
		{
			m_bindComponent = gameObject.GetComponent<UIBindComponent>();
			m_btnClose = m_bindComponent.GetComponent<Button>(0);
			m_textError = m_bindComponent.GetComponent<Text>(1);
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