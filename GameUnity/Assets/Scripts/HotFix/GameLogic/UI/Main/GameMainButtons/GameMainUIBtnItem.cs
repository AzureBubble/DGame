using UnityEngine;
using UnityEngine.UI;
using DGame;

namespace GameLogic
{
	public partial class GameMainUIBtnItem
	{
		#region Override
		
		protected override void BindMemberProperty()
		{
			m_goLock.SetActive(false);
			m_goSelect.SetActive(false);
		}

		#endregion
		
		#region 字段
		
		public GameMainBtnType MainBtnType { get; private set; }

		#endregion
		
		#region 函数

		public void Init(GameMainBtnType btnType)
		{
			MainBtnType = btnType;
		}

		#endregion
		
		#region 事件

		#endregion
	}
}
