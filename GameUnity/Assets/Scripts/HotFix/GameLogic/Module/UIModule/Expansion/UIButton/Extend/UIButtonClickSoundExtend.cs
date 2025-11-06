using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic
{
    [System.Serializable]
    public class UIButtonClickSoundExtend
    {
        [SerializeField] private bool m_isUseClickSound;
        [SerializeField] private bool m_isUseResourceSound;
        [SerializeField] private int m_clickSoundID;
        [SerializeField] private AudioClip m_clickSoundClip;

        public void OnPointerClick()
        {

        }

        public void OnPointerDown()
        {
            if (!m_isUseClickSound)
            {
                return;
            }

            // 使用音频管理类加载点击音效
            if (m_isUseResourceSound)
            {
                Debug.Log("按钮点击音效：" + m_clickSoundClip?.name);
            }
            else
            {
                Debug.Log("按钮点击音效：" + m_clickSoundID);
            }
        }

        public void OnPointerUp()
        {

        }
    }
}