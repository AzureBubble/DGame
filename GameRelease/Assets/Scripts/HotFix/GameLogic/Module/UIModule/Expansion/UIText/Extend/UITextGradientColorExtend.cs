using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [Serializable]
    public class UITextGradientColorExtend
    {
        private const int ONE_TEXT_VERTEX = 6;

#pragma warning disable 0414
        [SerializeField]
        private bool m_isUseGradientColor;
        [SerializeField]
        private Color m_colorTop = Color.white;
        [SerializeField]
        private Color m_colorBottom = Color.white;
        [SerializeField]
        private Color m_colorLeft = Color.white;
        [SerializeField]
        private Color m_colorRight = Color.white;
        [SerializeField, Range(-1f, 1f)]
        private float m_gradientOffsetVertical = 0f;
        [SerializeField, Range(-1f, 1f)]
        private float m_gradientOffsetHorizontal = 0f;
        [SerializeField]
        private bool m_splitTextGradient = false;

        public bool isUseGradientColor
        {
            get => m_isUseGradientColor;
            set
            {
                if (m_isUseGradientColor == value)
                {
                    return;
                }
                m_isUseGradientColor = value;
                Refresh();
            }
        }

        public Color colorTop
        {
            get => m_colorTop;
            set { if (m_colorTop != value) { m_colorTop = value; Refresh(); } }
        }

        public Color colorBottom
        {
            get => m_colorBottom;
            set { if (m_colorBottom != value) { m_colorBottom = value; Refresh(); } }
        }

        public Color colorLeft
        {
            get => m_colorLeft;
            set { if (m_colorLeft != value) { m_colorLeft = value; Refresh(); } }
        }

        public Color colorRight
        {
            get => m_colorRight;
            set { if (m_colorRight != value) { m_colorRight = value; Refresh(); } }
        }

        public float gradientOffsetVertical
        {
            get => m_gradientOffsetVertical;
            set { if (m_gradientOffsetVertical != value) { m_gradientOffsetVertical = value; Refresh(); } }
        }

        public float gradientOffsetHorizontal
        {
            get => m_gradientOffsetHorizontal;
            set { if (m_gradientOffsetHorizontal != value) { m_gradientOffsetHorizontal = value; Refresh(); } }
        }

        public bool splitTextGradient
        {
            get => m_splitTextGradient;
            set
            {
                if (m_splitTextGradient != value)
                {
                    m_splitTextGradient = value;
                    Refresh();
                }
            }
        }

        private Text m_text;

#pragma warning disable 0414
        public void Initialize(Text text)
        {
            m_text = text;
        }

#if UNITY_EDITOR
        public void EditorInitialize(Text text)
        {
            m_text = text;
        }
#endif

        public void SetUseGradientColor(bool useGradientColor)
        {
            isUseGradientColor = useGradientColor;
        }

        public void SetGradientColor(Color32 colorTop, Color32 colorBottom, Color32 colorLeft = default, Color32 colorRight = default, float verticalOffset = 0f, float horizontalOffset = 0f, bool splitTextGradient = false)
        {
            SetUseGradientColor(true);
            m_colorTop = colorTop;
            m_colorBottom = colorBottom;
            m_colorLeft = colorLeft;
            m_colorRight = colorRight;
            m_splitTextGradient = splitTextGradient;
            m_gradientOffsetVertical = verticalOffset;
            m_gradientOffsetHorizontal = horizontalOffset;
            Refresh();
        }

        public void Refresh()
        {
            m_text?.SetVerticesDirty();
        }

        #region GradientColor

        public void ModifyMesh(VertexHelper vh)
        {
            if (m_text?.IsActive() == false || !m_isUseGradientColor)
            {
                return;
            }

            List<UIVertex> vList = ListPool<UIVertex>.Get();

            vh.GetUIVertexStream(vList);

            ModifyVertices(vList);

            vh.Clear();
            vh.AddUIVertexTriangleStream(vList);

            if (vList != null)
            {
                ListPool<UIVertex>.Recycle(vList);
            }
        }

        private void ModifyVertices(List<UIVertex> vList)
        {
            if (m_text?.IsActive() == false || vList == null || vList.Count == 0)
            {
                return;
            }

            float minX = 0f, minY = 0f, maxX = 0f, maxY = 0f, width = 0f, height = 0;

            UIVertex newVertex;
            for (int i = 0; i < vList.Count; i++)
            {
                if (i == 0 || (m_splitTextGradient && i % ONE_TEXT_VERTEX == 0))
                {
                    minX = vList[i].position.x;
                    minY = vList[i].position.y;
                    maxX = vList[i].position.x;
                    maxY = vList[i].position.y;

                    int vertNum = m_splitTextGradient ? i + ONE_TEXT_VERTEX : vList.Count;

                    for (int k = i; k < vertNum; k++)
                    {
                        if (k >= vList.Count)
                        {
                            break;
                        }
                        UIVertex vertex = vList[k];
                        minX = Mathf.Min(minX, vertex.position.x);
                        minY = Mathf.Min(minY, vertex.position.y);
                        maxX = Mathf.Max(maxX, vertex.position.x);
                        maxY = Mathf.Max(maxY, vertex.position.y);
                    }

                    width = maxX - minX;
                    height = maxY - minY;
                }

                newVertex = vList[i];

                Color colorOriginal = newVertex.color;
                Color colorVertical = Color.Lerp(m_colorBottom, m_colorTop, (height > 0 ? (newVertex.position.y - minY) / height : 0) + m_gradientOffsetVertical);
                Color colorHorizontal = Color.Lerp(m_colorLeft, m_colorRight, (width > 0 ? (newVertex.position.x - minX) / width : 0) + m_gradientOffsetHorizontal);

                newVertex.color = colorOriginal * colorVertical * colorHorizontal;

                vList[i] = newVertex;
            }
        }

        #endregion
    }
}