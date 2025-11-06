using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [System.Serializable]
    public class UITextVertexColorExtend
    {
        public enum ColorFilterType
        {
            /// <summary>
            /// 基础色加上新颜色
            /// </summary>
            Additive,

            /// <summary>
            /// 颜色叠加
            /// </summary>
            Overlap,
        }

        [SerializeField] private bool m_isUseVertexColor = false;
        [SerializeField] private ColorFilterType m_colorFilterType = ColorFilterType.Overlap;
        [SerializeField] private Color m_vertexTopLeftColor = Color.white;
        [SerializeField] private Color m_vertexTopRightColor = Color.white;
        [SerializeField] private Color m_vertexBottomLeftColor = Color.white;
        [SerializeField] private Color m_vertexBottomRightColor = Color.white;
        [SerializeField] private Vector2 m_vertexColorOffset = Vector2.zero;

        public bool UseVertexColor { get => m_isUseVertexColor;  set => m_isUseVertexColor = value;  }
        public ColorFilterType VertexColorFilterType { get => m_colorFilterType; set => m_colorFilterType = value; }
        public Color VertexTopLeftColor { get => m_vertexTopLeftColor; set => m_vertexTopLeftColor = value; }
        public Color VertexTopRightColor { get => m_vertexTopRightColor; set => m_vertexTopRightColor = value; }
        public Color VertexBottomLeftColor { get => m_vertexBottomLeftColor; set => m_vertexBottomLeftColor = value; }
        public Color VertexBottomRightColor { get => m_vertexBottomRightColor; set => m_vertexBottomRightColor = value; }
        public Vector2 VertexColorOffset { get => m_vertexColorOffset; set => m_vertexColorOffset = value; }

        public void PopulateMesh(VertexHelper toFill, RectTransform rectTransform, Color color)
        {
            if (!m_isUseVertexColor)
            {
                return;
            }

            // 计算UI元素的边界范围 min:左下角坐标 max:右上角坐标
            Vector2 min = rectTransform.pivot;
            min.Scale(-rectTransform.rect.size);
            Vector2 max = rectTransform.rect.size + min;
            int cnt = toFill.currentVertCount;

            for (int i = 0; i < cnt; i++)
            {
                UIVertex v = new UIVertex();
                toFill.PopulateUIVertex(ref v, i);
                v.color = RemapColor(min, max, color, v.position);
                toFill.SetUIVertex(v, i);
            }
        }

        private Color RemapColor(Vector2 min, Vector2 max, Color color, Vector2 pos)
        {
            float x01 = max.x == min.x ? 0f : Mathf.Clamp01((pos.x - min.x) / (max.x - min.x));
            float y01 = max.y == min.y ? 0f : Mathf.Clamp01((pos.y - min.y) / (max.y - min.y));
            x01 -= VertexColorOffset.x * (VertexColorOffset.x > 0f ? x01 : (1f - x01));
            y01 -= VertexColorOffset.y * (VertexColorOffset.y > 0f ? y01 : (1f - y01));
            Color newColor =
                Color.Lerp(
                Color.Lerp(VertexBottomLeftColor, VertexBottomRightColor, x01),
                Color.Lerp(VertexTopLeftColor, VertexTopRightColor, x01), y01
                );
            switch (VertexColorFilterType)
            {
                case ColorFilterType.Overlap:
                    float a = Mathf.Max(newColor.a, color.a);
                    newColor = Color.Lerp(color, newColor, newColor.a);
                    newColor.a = a;
                    return newColor;

                case ColorFilterType.Additive:
                default:
                    return color + newColor;
            }
        }
    }
}