using UnityEngine;
using UnityEngine.UI;
using DGame;

namespace GameLogic
{
	public partial class MainLoginUI
	{
		#region Ovrrider

		protected override void BindMemberProperty()
		{
			m_goAccountNode.SetActive(false);
		}

		#endregion

		#region 字段


		#endregion

		#region 函数


		#endregion

		#region 事件

		private partial void OnClickAgeTipsBtn()
		{
			DLogger.Info("点击适龄提醒");
		}

		private partial void OnToggleUserPrivacyChange(bool isOn)
		{
		}

		private partial void OnClickUserAgreementBtn()
		{
			DLogger.Info("点击用户协议");
		}

		private partial void OnClickUserPrivacyBtn()
		{
			DLogger.Info("点击用户隐私");
		}

		private partial void OnClickStartGameBtn()
		{
		}

		private partial void OnClickSelectServerBtn()
		{
			UIModule.Instance.ShowWindowAsync<SelectServerUI>();
		}

		private partial void OnClickUserBtn()
		{
			m_goAccountNode.SetActive(true);
		}

		private partial void OnClickNotifyBtn()
		{
			DLogger.Info("点击公告");
		}

		private partial void OnClickCloseAccountBtn()
		{
			m_goAccountNode.SetActive(false);
		}

		private partial void OnClickLoginBtn()
		{
		}

		#endregion
	}
}