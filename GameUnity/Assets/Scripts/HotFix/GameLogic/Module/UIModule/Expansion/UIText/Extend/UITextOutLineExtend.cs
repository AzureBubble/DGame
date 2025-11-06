using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [Serializable]
    public class UITextOutLineExtend
    {
        [SerializeField] private bool m_isUseOutLine;
        [SerializeField] private Color m_effectColor = new Color(0f, 0f, 0f, 0.5f);
        [SerializeField] private Vector2 m_effectDistance = new Vector2(1f, -1f);
        private const float MAX_EFFECT_DISTANCE = 600f;

        public Color EffectColor { get => m_effectColor; set => m_effectColor = value; }
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

        public void PopulateMesh(VertexHelper vh)
        {
            if (!m_isUseOutLine)
            {
                return;
            }

            List<UIVertex> verts = new List<UIVertex>();
            vh.GetUIVertexStream(verts);
            var neededCapacity = verts.Count * 5;

            if (verts.Capacity > neededCapacity)
            {
                verts.Capacity = neededCapacity;
            }

            var start = 0;
            var end = verts.Count;
            ApplyShadowZeroAlloc(verts, m_effectColor, start, verts.Count, m_effectDistance.x, m_effectDistance.y);
            start = end;
            end = verts.Count;
            ApplyShadowZeroAlloc(verts, m_effectColor, start, verts.Count, m_effectDistance.x, -m_effectDistance.y);
            start = end;
            end = verts.Count;
            ApplyShadowZeroAlloc(verts, m_effectColor, start, verts.Count, -m_effectDistance.x, m_effectDistance.y);
            start = end;
            end = verts.Count;
            ApplyShadowZeroAlloc(verts, m_effectColor, start, verts.Count, -m_effectDistance.x, -m_effectDistance.y);
            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
        }

        private void ApplyShadowZeroAlloc(List<UIVertex> verts, Color32 color, int start, int end, float x, float y)
        {
            UIVertex vt;
            var neededCapacity = verts.Count + end - start;

            if (verts.Capacity < neededCapacity)
            {
                verts.Capacity = neededCapacity;
            }

            for (int i = start; i < end; i++)
            {
                vt = verts[i];
                Vector3 vPos = vt.position;
                vPos.y += y;
                vPos.x += x;
                vt.position = vPos;
                var newColor = color;
                newColor.a = (byte)((newColor.a * verts[i].color.a) / 255f);
                vt.color = newColor;
                verts.Add(vt);
            }
        }
    }
}