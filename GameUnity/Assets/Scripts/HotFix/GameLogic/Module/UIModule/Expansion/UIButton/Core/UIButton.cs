using UnityEngine;

namespace GameLogic
{
    public class UIButton : BaseUIButton
    {
        public RectTransform rectTransform => transform as RectTransform;
    }
}