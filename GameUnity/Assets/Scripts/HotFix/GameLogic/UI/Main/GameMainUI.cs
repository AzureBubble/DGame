using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DGame;

namespace GameLogic
{
	public enum GameMainBtnType
	{
		/// <summary>
		/// 商城
		/// </summary>
		Shop = 0,
		
		/// <summary>
		/// 角色
		/// </summary>
		Actor = 1,
		
		/// <summary>
		/// 主界面
		/// </summary>
		Main = 2,
		
		/// <summary>
		/// 公会
		/// </summary>
		League = 3,
		
		/// <summary>
		/// 玩法
		/// </summary>
		GamePlay = 4,
		
		/// <summary>
		/// 家园
		/// </summary>
		Home = 5,
		
		/// <summary>
		/// 最大
		/// </summary>
		Max = 6
	}
	
	public partial class GameMainUI
	{
		#region Override

		protected override void BindMemberProperty()
		{
			m_gridBtnNode = m_tfButtons.GetComponent<GridLayoutGroup>();
			m_btnGm.SetActive(false);
			m_itemBtn.SetActive(false);
			CreateGameMainButtons();
		}

		#endregion
		
		#region 字段
		
		private GridLayoutGroup m_gridBtnNode;
		private List<GameMainUIBtnItem> m_mainBtnList = new List<GameMainUIBtnItem>();

		#endregion
		
		#region 函数

		private void CreateGameMainButtons()
		{
			for (int i = 0; i < (int)GameMainBtnType.Max; i++)
			{
				var btnType = (GameMainBtnType)i;
				switch (btnType)
				{
					case GameMainBtnType.Shop:
						CreateGameButton<GameMainUIBtnShop>(btnType);
						break;

					case GameMainBtnType.Actor:
						CreateGameButton<GameMainUIBtnActor>(btnType);
						break;

					case GameMainBtnType.Main:
						CreateGameButton<GameMainUIBtnMain>(btnType);
						break;

					case GameMainBtnType.League:
						CreateGameButton<GameMainUIBtnLeague>(btnType);
						break;

					case GameMainBtnType.GamePlay:
						CreateGameButton<GameMainUIBtnGamePlay>(btnType);
						break;

					case GameMainBtnType.Home:
						CreateGameButton<GameMainUIBtnHome>(btnType);
						break;
				}
			}
			var showCnt = m_mainBtnList.Count;
			var tabWidth = ((RectTransform)m_tfButtons).sizeDelta.x / showCnt;
			m_gridBtnNode.cellSize = new Vector2(tabWidth, m_gridBtnNode.cellSize.y);
		}

		private void CreateGameButton<T>(GameMainBtnType btnType) where T : GameMainUIBtnItem, new()
		{
			var btn = CreateWidgetByPrefab<T>(m_itemBtn, m_tfButtons);
			btn.Init(btnType);
			btn.BindClickEvent(OnClickGameButton);
			m_mainBtnList.Add(btn);
		}

		private void OnClickGameButton(GameMainUIBtnItem item)
		{
			var btnType = item.MainBtnType;
			SwitchPage(btnType);
		}

		private void SwitchPage(GameMainBtnType btnType, Action<UIWidget> onSwitchPage = null)
		{
			
		}

		#endregion
		
		#region 事件

		private partial void OnClickGmBtn()
		{
			
		}

		#endregion
	}
}
