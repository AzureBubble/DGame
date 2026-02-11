using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DGame;
using Fantasy;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Network.Interface;

namespace GameLogic
{
	public partial class MainWindow
	{
		private static readonly List<string> AuthenticationList = new List<string>()
		{
			"127.0.0.1:20001", "127.0.0.1:20002", "127.0.0.1:20003"
		};

		public string Select(string userName)
		{
			var userNameHashCode = HashCodeHelper.MurmurHash3(userName);
			var authenticationListIndex = userNameHashCode % AuthenticationList.Count;
			// 按照现在的情况下，这个模出的值只会是0 - 3
			return AuthenticationList[(int)authenticationListIndex];
		}


		private Scene m_scene;

		protected override void BindMemberProperty()
		{
			m_scene = GameClient.Instance.Scene;
		}

		protected override void RegisterEvent()
		{
		}

		#region 事件

		private partial void OnClickRegisterBtn()
		{
			// 根据用户名来选择目标的鉴权服务器
			// var authenticationAddress = Select(m_inputUserName.text);
			// 根据鉴权服务器地址来创建一个新的网络会话
			DataCenterSys.Instance.Register("127.0.0.1:20001", m_inputUserName.text, m_inputPassword.text).Coroutine();
		}

		private partial void OnClickLoginBtn()
		{
			DataCenterSys.Instance.Login("127.0.0.1:20001", m_inputUserName.text, m_inputPassword.text).Coroutine();
		}

		private partial void OnClickGetBtn()
		{
		}

		#endregion
	}
}