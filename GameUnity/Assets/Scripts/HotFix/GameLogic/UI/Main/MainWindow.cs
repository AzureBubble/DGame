using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DGame;
using Fantasy;
using Fantasy.Async;
using Fantasy.Helper;

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
			GameClient.Instance.Connect("127.0.0.1:20001");
			m_scene = GameClient.Instance.Scene;
		}

		#region 事件

		private partial void OnClickRegisterBtn()
		{
			RegisterAction().Coroutine();
		}

		private async FTask RegisterAction()
		{
			// 根据用户名来选择目标的鉴权服务器
			// var authenticationAddress = Select(m_inputUserName.text);
			// 根据鉴权服务器地址来创建一个新的网络会话

			// 发送一个注册的请求消息到目标服务器
			var response = (A2C_RegisterResponse)await m_scene.Session.Call(new C2A_RegisterRequest()
			{
				UserName = m_inputUserName.text,
				Password = m_inputPassword.text
			});

			if (response.ErrorCode != 0)
			{
				Log.Error($"Error: {response.ErrorCode}");
				return;
			}

			Log.Debug("Registered Successfully");
		}

		private partial void OnClickLoginBtn()
		{
		}

		private partial void OnClickGetBtn()
		{
		}

		#endregion
	}
}