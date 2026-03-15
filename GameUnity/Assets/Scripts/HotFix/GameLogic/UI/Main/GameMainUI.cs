using UnityEngine;
using UnityEngine.UI;
using DGame;

namespace GameLogic
{
	public partial class GameMainUI
	{
		#region Override

		protected override void BindMemberProperty()
		{
			m_btnGm.SetActive(false);
		}

		#endregion
		
		#region 字段

		#endregion
		
		#region 函数

		#endregion
		
		#region 事件

		private partial void OnClickGmBtn()
		{
			
		}

		#endregion
	}
}
