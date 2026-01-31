using GameProto;
using UnityEngine;

namespace GameLogic
{
    [System.Serializable]
    public class UIButtonClickSoundExtend
    {
        [SerializeField] private bool m_isUseClickSound = true;
        [SerializeField] private int m_clickSoundID = 1000002;

        public void OnPointerClick()
        {

        }

        public void OnPointerDown()
        {
            if (!m_isUseClickSound)
            {
                return;
            }

            if(TbSoundConfig.TryGetValue((int)SysSoundID.BTN_CLICK, out var soundCfg))
            {
                GameModule.AudioModule.Play(DGame.AudioType.UISound, soundCfg.Location, isInPool: true);
            }
        }

        public void OnPointerUp()
        {

        }

        public void SetClickSoundID(int soundID)
        {
            m_clickSoundID = soundID;
        }
    }
}