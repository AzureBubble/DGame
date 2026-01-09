using System;
using UnityEngine;
using UnityEngine.UI;
using DGame;

namespace GameLogic
{
	public partial class RedDotItem
	{
		#region 字段

		private RedDotNode m_redDotNode;
		public RedDotNode RedDotNode => m_redDotNode;
		private bool m_isInit = false;

		#endregion

		#region override

		protected override void BindMemberProperty()
		{
			SetImgRedActive(false);
			SetImgTextRedActive(false);
			SetImgNewRedActive(false);
		}

		protected override void OnDestroy()
		{
			if (!m_isInit)
			{
				return;
			}
			m_redDotNode?.RemoveListener(OnRedDotValueChanged);
			m_isInit = false;
			m_redDotNode = null;
		}

		#endregion

		#region 函数

		public void Init(int redDotNodeID)
		{
			if (m_redDotNode != null)
			{
				if(m_redDotNode.Id == redDotNodeID)
				{
					if (!m_isInit)
					{
						m_redDotNode.AddListener(OnRedDotValueChanged);
						m_isInit = true;
						OnRedDotValueChanged(m_redDotNode);
					}
					return;
				}
				m_redDotNode.RemoveListener(OnRedDotValueChanged);
				m_isInit = false;
			}

			m_redDotNode = RedDotModule.Instance.GetNode(redDotNodeID);
			if (m_redDotNode == null)
			{
				return;
			}

			if (!m_isInit)
			{
				m_redDotNode.AddListener(OnRedDotValueChanged);
				m_isInit = true;
			}
			// 立即同步当前状态
			OnRedDotValueChanged(m_redDotNode);
		}

		public void SetImgRedActive(bool isActive)
		{
			m_imgRed.gameObject.SetActive(isActive);
		}

		public void SetImgTextRedActive(bool isActive)
		{
			m_imgTextRed.gameObject.SetActive(isActive);
		}

		public void SetImgTextRedCount(int count)
		{
			var countStr = count > 99 ? "99+" : count.ToString();
			m_textRed.text = countStr;
		}

		public void SetImgNewRedActive(bool isActive)
		{
			m_goNewRed.SetActive(isActive);
		}

		#endregion

		#region 事件

		private void OnRedDotValueChanged(RedDotNode redDotNode)
		{
			if (redDotNode == null)
			{
				return;
			}

			SetImgRedActive(false);
			SetImgTextRedActive(false);
			SetImgNewRedActive(false);

			switch (redDotNode.Type)
			{
				case RedDotType.Number:
					SetImgTextRedActive(redDotNode.IsShow);
					SetImgTextRedCount(redDotNode.Value);
					break;

				case RedDotType.New:
					SetImgNewRedActive(redDotNode.IsShow);
					break;

				case RedDotType.Dot:
				default:
					SetImgRedActive(redDotNode.IsShow);
					break;
			}
		}

		#endregion
	}
}