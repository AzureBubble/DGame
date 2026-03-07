using System.Collections;
using System.Collections.Generic;
using DGame;
using Fantasy.Entitas;
using GameBattle;
using UnityEngine;

namespace GameLogic
{
    public class RenderUnit : Entity, IRenderUnit
    {
        public LogicUnit LogicUnit { get; set; }
        public bool IsDestroyed { get; private set; }
        
        protected GameObject unitObject;
        protected Transform unitTransform;
        
        public GameObject UnitObject => unitObject;
        public Transform UnitTransform => unitTransform;
        
        public void OnEntityEvent(int eventId)
        {
            GameEvent.EventMgr.Dispatcher.Send(eventId, eventId);
        }
    }
}
