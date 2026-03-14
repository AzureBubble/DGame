using DGame;
using GameProto;

namespace GameLogic
{
	public partial class StateTipsItem
	{
		#region 函数

		public void Init(ServerStateConfig config)
		{
			if (config == null)
			{
				return;
			}

			m_textState.text = config.StateName;
			m_imgState.color = config.Color.ParseColor();
			if (!string.IsNullOrEmpty(config.Location))
			{
				m_imgState.SetSprite(config.Location);
			}
		}

		#endregion

		#region 事件

		#endregion
	}
}