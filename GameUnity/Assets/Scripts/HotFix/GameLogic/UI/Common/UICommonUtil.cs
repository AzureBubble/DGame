using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public static class UICommonUtil
    {
        public static Color ParseColor(this string hex) => DGame.Utility.Converter.HexToColor(hex);

        #region SetGray & SetCircle

        public static void SetGray(this Image image, bool isGray)
        {
            if (isGray)
            {
                DGame.Utility.UnityUtil.AddMonoBehaviour<UIImageEffect>(image).IsGray = true;
            }
            else
            {
                if (image.TryGetComponent<UIImageEffect>(out var effect))
                {
                    effect.IsGray = false;
                }
            }
        }
        
        public static void SetGray(this GameObject go, bool isGray)
        {
            var images = go.GetComponentsInChildren<Image>(true);

            if (images != null)
            {
                foreach (var image in images)
                {
                    image.SetGray(isGray);
                }
            }
        }
        
        public static void SetCircle(this Image image, bool isCircle)
        {
            if (isCircle)
            {
                DGame.Utility.UnityUtil.AddMonoBehaviour<UIImageEffect>(image.gameObject).IsCircle = true;
            }
            else
            {
                if (image.TryGetComponent<UIImageEffect>(out var effect))
                {
                    effect.IsCircle = false;
                }
            }
        }

        #endregion
    }
}