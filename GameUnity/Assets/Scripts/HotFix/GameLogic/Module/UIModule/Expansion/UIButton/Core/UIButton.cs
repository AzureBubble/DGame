using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    public class UIButton : BaseUIButton
    {
        public RectTransform rectTransform => transform as RectTransform;
    }
}