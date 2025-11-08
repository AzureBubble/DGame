using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [System.Serializable]
    public class BaseUIText : Text, IMeshModifier
    {
        [SerializeField] private UITextSpacingExtend m_uiTextSpacingExtend = new UITextSpacingExtend();
        [SerializeField] private UITextVertexColorExtend m_uiTextVertexColorExtend = new UITextVertexColorExtend();
        [SerializeField] private UITextShadowExtend m_uiTextShadowExtend = new UITextShadowExtend();
        [SerializeField] private UITextOutlineAndGradientExtend m_uiTextOutlineAndGradientExtend = new UITextOutlineAndGradientExtend();

        [SerializeField] private bool m_isUseBestFitFont;

        public UITextOutlineAndGradientExtend UITextOutlineAndGradientExtend => m_uiTextOutlineAndGradientExtend;
        public UITextShadowExtend UITextShadowExtend => m_uiTextShadowExtend;

        /// <summary>
        /// 当前可见的文字行数
        /// </summary>
        public int VisibleLines { get; private set; }
        private readonly UIVertex[] m_tmpVerts = new UIVertex[4];

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            base.OnPopulateMesh(toFill);

            if (OverrideForBestFit(toFill))
            {
                if (!UITextOutlineAndGradientExtend.UseTextOutline && !UITextOutlineAndGradientExtend.UseTextGradient)
                {
                    m_uiTextShadowExtend?.PopulateMesh(toFill, rectTransform, color);
                }

                return;
            }

            m_uiTextSpacingExtend?.PopulateMesh(toFill);
            m_uiTextVertexColorExtend?.PopulateMesh(toFill, rectTransform, color);
            if (!UITextOutlineAndGradientExtend.UseTextOutline && !UITextOutlineAndGradientExtend.UseTextGradient)
            {
                m_uiTextShadowExtend?.PopulateMesh(toFill, rectTransform, color);
            }

            // m_uiTextOutLineExtend?.PopulateMesh(toFill);
        }

        private bool OverrideForBestFit(VertexHelper toFill)
        {
            if(!m_isUseBestFitFont) return false;
            if (null == font) return false;
            m_DisableFontTextureRebuiltCallback = true;
            UseFitSettings();
            IList<UIVertex> verts = cachedTextGenerator.verts;
            float unitsPerPixel = 1 / pixelsPerUnit;
            int vertCount = verts.Count;
            // 没有要处理的对象时，直接return。
            if (vertCount <= 0)
            {
                toFill.Clear();
                return false;
            }
            Vector2 roundingOffset = new Vector2(verts[0].position.x, verts[0].position.y) * unitsPerPixel;
            roundingOffset = PixelAdjustPoint(roundingOffset) - roundingOffset;
            toFill.Clear();

            for (int i = 0; i < vertCount; ++i)
            {
                int tempVertsIndex = i & 3;
                m_tmpVerts[tempVertsIndex] = verts[i];
                m_tmpVerts[tempVertsIndex].position *= unitsPerPixel;
                if (roundingOffset != Vector2.zero)
                {
                    m_tmpVerts[tempVertsIndex].position.x += roundingOffset.x;
                    m_tmpVerts[tempVertsIndex].position.y += roundingOffset.y;
                }

                if (tempVertsIndex == 3)
                {
                    toFill.AddUIVertexQuad(m_tmpVerts);
                }
            }
            m_DisableFontTextureRebuiltCallback = false;
            VisibleLines = cachedTextGenerator.lineCount;
            return true;
        }

        private void UseFitSettings()
        {
            TextGenerationSettings settings = GetGenerationSettings(rectTransform.rect.size);
            settings.resizeTextForBestFit = false;

            if (!resizeTextForBestFit)
            {
                cachedTextGenerator.PopulateWithErrors(text, settings, gameObject);
                return;
            }

            int minSize = resizeTextMinSize;
            int txtLen = text.Length;

            //从Best Fit中最大的值开始，逐次递减，每次减小后都尝试生成文本，
            //如果生成的文本可见字符数等于文本内容的长度，则找到满足需求(可以使所有文本都可见的最大字号)的字号。
            for (int i = resizeTextMaxSize; i >= minSize; --i)
            {
                settings.fontSize = i;
                cachedTextGenerator.PopulateWithErrors(text, settings, gameObject);
                if (cachedTextGenerator.characterCountVisible == txtLen) break;
            }
        }

        public void ModifyMesh(Mesh mesh)
        {
        }

        public void ModifyMesh(VertexHelper verts)
        {
        }

        public void SetTextAlpha(float alpha)
        {
            if (m_uiTextOutlineAndGradientExtend.UseTextOutline && m_uiTextOutlineAndGradientExtend.UseTextGradient && m_uiTextOutlineAndGradientExtend.GradientType != 0)
            {
                m_uiTextOutlineAndGradientExtend.SetAlpha(alpha);
            }
            else
            {
                Color32 color32 = color;
                color32.a = (byte)(alpha * 255);
                color = color32;
            }
        }

        public void SetOutLineColor(Color32 color32)
        {
            if (!m_uiTextOutlineAndGradientExtend.UseTextOutline) return;
            m_uiTextOutlineAndGradientExtend.TextEffect.SetOutLineColor(color32);
            m_uiTextOutlineAndGradientExtend.UseTextOutline = false;
            m_uiTextOutlineAndGradientExtend.UseTextOutline = true;
        }

        public void SetGradientColor(Color32 topColor, Color32 middleColor, Color32 bottomColor)
        {
            if (!m_uiTextOutlineAndGradientExtend.UseTextGradient) return;
            m_uiTextOutlineAndGradientExtend.TextEffect.SetTopColor(topColor);
            m_uiTextOutlineAndGradientExtend.TextEffect.SetMiddleColor(middleColor);
            m_uiTextOutlineAndGradientExtend.TextEffect.SetBottomColor(bottomColor);
            m_uiTextOutlineAndGradientExtend.UseTextGradient = false;
            m_uiTextOutlineAndGradientExtend.UseTextGradient = true;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
        }
#endif
    }
}