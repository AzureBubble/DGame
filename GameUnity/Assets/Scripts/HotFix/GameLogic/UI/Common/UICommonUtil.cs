using UnityEngine;

namespace GameLogic
{
    public static class UICommonUtil
    {
        public static Color ParseColor(string hex) => DGame.Utility.Converter.HexToColor(hex);
    }
}