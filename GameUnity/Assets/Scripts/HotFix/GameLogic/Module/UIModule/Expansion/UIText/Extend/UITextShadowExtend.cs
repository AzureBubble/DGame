using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GameLogic
{
    [Serializable]
    public class UITextShadowExtend
    {
        [SerializeField] private bool m_isUseTextShadow;
        [SerializeField] private Color m_shadowTopLeftColor = Color.white;
        [SerializeField] private Color m_shadowTopRightColor = Color.white;
        [SerializeField] private Color m_shadowBottomLeftColor = Color.white;
        [SerializeField] private Color m_shadowBottomRightColor = Color.white;
        [SerializeField] private Vector2 m_effectDistance = new Vector2(1f, -1f);
        private const float MAX_EFFECT_DISTANCE = 600f;
        private Vector2 m_vectorColorOffset = Vector2.zero;

        public bool UseShadow { get => m_isUseTextShadow; set => m_isUseTextShadow = value; }
        public Color TopLeftColor { get => m_shadowTopLeftColor; set => m_shadowTopLeftColor = value; }
        public Color TopRightColor { get => m_shadowTopRightColor; set => m_shadowTopRightColor = value; }
        public Color BottomLeftColor { get => m_shadowBottomLeftColor; set => m_shadowBottomLeftColor = value; }
        public Color BottomRightColor { get => m_shadowBottomRightColor; set => m_shadowBottomRightColor = value; }

        public Vector2 EffectDistance
        {
            get => m_effectDistance;
            set
            {
                if (m_effectDistance == value)
                {
                    return;
                }

                if (value.x > MAX_EFFECT_DISTANCE)
                {
                    value.x = MAX_EFFECT_DISTANCE;
                }

                if (value.x < -MAX_EFFECT_DISTANCE)
                {
                    value.x = -MAX_EFFECT_DISTANCE;
                }

                if (value.y > MAX_EFFECT_DISTANCE)
                {
                    value.y = MAX_EFFECT_DISTANCE;
                }

                if (value.y < -MAX_EFFECT_DISTANCE)
                {
                    value.y = -MAX_EFFECT_DISTANCE;
                }

                m_effectDistance = value;
            }
        }

        public void PopulateMesh(VertexHelper vh, RectTransform rectTransform, Color color)
        {
            if (!m_isUseTextShadow)
            {
                return;
            }

            Vector2 min = rectTransform.pivot;
            min.Scale(-rectTransform.rect.size);
            Vector2 max = rectTransform.rect.size + min;
            List<UIVertex> vertexList = new List<UIVertex>();
            vh.GetUIVertexStream(vertexList);
            ApplyShadow(vertexList, min, max, color, m_effectDistance.x, m_effectDistance.y);
            vh.Clear();
            vh.AddUIVertexTriangleStream(vertexList);
        }

        private void ApplyShadow(List<UIVertex> verts, Vector2 min, Vector2 max, Color32 color, float x, float y)
        {
            UIVertex vt;
            int start = 0;
            int end = verts.Count;
            // 扩容顶点数量 把 shadow 的顶点加入到 verts 里
            var neededCapacity = verts.Count * 2;

            if (verts.Capacity < neededCapacity)
            {
                verts.Capacity = neededCapacity;
            }

            for (int i = start; i < end; i++)
            {
                vt = verts[i];
                verts.Add(vt);
                Vector3 vtPos = vt.position;
                vtPos.x += x;
                vtPos.y += y;
                vt.position = vtPos;
                vt.color = RemapColor(min, max, color, vtPos);
                verts[i] = vt;
            }
        }

        private Color RemapColor(Vector2 min, Vector2 max, Color32 color, Vector3 vtPos)
        {
            float x01 = max.x == min.x ? 0f : Mathf.Clamp01((vtPos.x - min.x) / (max.x - min.x));
            float y01 = max.y == min.y ? 0f : Mathf.Clamp01((vtPos.y - min.y) / (max.y - min.y));
            x01 -= m_vectorColorOffset.x * (m_vectorColorOffset.x > 0f ? x01 : (1f - x01));
            y01 -= m_vectorColorOffset.y * (m_vectorColorOffset.y > 0f ? y01 : (1f - y01));
            Color newColor = Color.Lerp(
                Color.Lerp(m_shadowBottomLeftColor, m_shadowBottomRightColor, x01),
                Color.Lerp(m_shadowTopLeftColor, m_shadowTopRightColor, x01),
                y01
            );
            //使用全新颜色 不继承原有的
            return newColor;
            //return color * newColor;
        }
    }
}