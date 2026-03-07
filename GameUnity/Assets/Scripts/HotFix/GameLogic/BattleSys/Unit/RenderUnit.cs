using System.Collections;
using System.Collections.Generic;
using DGame;
using Fantasy.Entitas;
using GameBattle;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 渲染层对象
    /// </summary>
    public class RenderUnit : Entity, IRenderUnit
    {
        /// <summary>
        /// 逻辑层对象
        /// </summary>
        public LogicUnit LogicUnit { get; set; }
        
        /// <summary>
        /// 是否已销毁
        /// </summary>
        public bool IsDestroyed { get; private set; }
        
        /// <summary>
        /// 渲染层 GameObject
        /// </summary>
        protected GameObject unitObject;
        
        /// <summary>
        /// 渲染层 Transform
        /// </summary>
        protected Transform unitTransform;
        
        public GameObject UnitObject => unitObject;
        public Transform UnitTransform => unitTransform;
        
        public void OnUnitEvent(int eventId)
        {
            GameEvent.EventMgr.Dispatcher.Send(eventId, eventId);
        }
    }
}
