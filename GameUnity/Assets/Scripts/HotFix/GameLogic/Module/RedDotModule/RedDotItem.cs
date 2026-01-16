using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
	public class RedDotItem : UIWidget
	{
		#region 脚本工具生成的代码

		private Image m_imgRed;
		private Image m_imgTextRed;
		private Text m_textRed;
		private GameObject m_goNewRed;

		protected override void ScriptGenerator()
		{
			m_imgRed = FindChildComponent<Image>("m_imgRed");
			m_imgTextRed = FindChildComponent<Image>("m_imgTextRed");
			m_textRed = FindChildComponent<Text>("m_imgTextRed/m_textRed");
			m_goNewRed = FindChild("m_goNewRed").gameObject;
		}

		#endregion

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