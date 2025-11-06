using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class SwitchTabItemDataComponent : MonoBehaviour
    {
        public Transform m_noSelectNode;
        public Image m_noSelectBg;
        public Image m_noSelectIcon;
        public Text m_noSelectText;

        public Transform m_selectedNode;
        public Image m_selectedBg;
        public Image m_selectedIcon;
        public Text m_selectedText;

        public Transform m_tfRedNode;
    }
}