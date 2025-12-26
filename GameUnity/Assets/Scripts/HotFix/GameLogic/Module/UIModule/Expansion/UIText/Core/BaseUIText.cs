using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GameLogic
{
    [System.Serializable]
    public class BaseUIText : Text, IMeshModifier
    {
        [SerializeField] private UITextSpacingExtend m_uiTextSpacingExtend = new UITextSpacingExtend();
        [SerializeField] private UITextVertexColorExtend m_uiTextVertexColorExtend = new UITextVertexColorExtend();
        [SerializeField] private UITextShadowExtend m_uiTextShadowExtend = new UITextShadowExtend();
        [SerializeField] private UITextOutlineExtend m_uiTextOutlineExtend = new UITextOutlineExtend();
        [SerializeField] private UITextGradientColorExtend m_uiTextGradientColorExtend = new UITextGradientColorExtend();
        [SerializeField] private UITextCircleExtend m_uiTextCircleExtend = new UITextCircleExtend();
        [SerializeField] private UITextLocalizationExtend m_uiTextLocalizationExtend = new UITextLocalizationExtend();

        [SerializeField] private bool m_isUseBestFitFont;

        public UITextOutlineExtend UITextOutlineExtend => m_uiTextOutlineExtend;
        public UITextGradientColorExtend UITextGradientColorExtend => m_uiTextGradientColorExtend;
        public UITextShadowExtend UITextShadowExtend => m_uiTextShadowExtend;
        public UITextLocalizationExtend UITextLocalizationExtend => m_uiTextLocalizationExtend;

        /// <summary>
        /// 当前可见的文字行数
        /// </summary>
        public int VisibleLines { get; private set; }
        private readonly UIVertex[] m_tmpVerts = new UIVertex[4];

        protected override void Awake()
        {
            base.Awake();
            UITextOutlineExtend?.Initialize(this);
            UITextGradientColorExtend?.Initialize(this);
            m_uiTextLocalizationExtend?.Initialize(this);
        }

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            base.OnPopulateMesh(toFill);

            // if (OverrideForBestFit(toFill))
            // {
            //     if (!UITextOutlineExtend.UseTextOutline)
            //     {
            //         m_uiTextShadowExtend?.PopulateMesh(toFill, rectTransform, color);
            //     }
            //     return;
            // }
            //
            // m_uiTextSpacingExtend?.PopulateMesh(toFill);
            // m_uiTextVertexColorExtend?.PopulateMesh(toFill, rectTransform, color);
            // if (!UITextOutlineExtend.UseTextOutline)
            // {
            //     m_uiTextShadowExtend?.PopulateMesh(toFill, rectTransform, color);
            // }
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
            if(!IsActive() || verts.currentVertCount == 0) return;
            if (OverrideForBestFit(verts))
            {
                m_uiTextCircleExtend.ModifyMesh(verts);
                UITextOutlineExtend.NormalOutLineModifyMesh(verts);
                m_uiTextVertexColorExtend?.PopulateMesh(verts, rectTransform, color);
                UITextGradientColorExtend?.ModifyMesh(verts);
                m_uiTextShadowExtend?.PopulateMesh(verts, rectTransform, color);
                return;
            }

            m_uiTextSpacingExtend?.PopulateMesh(verts);
            m_uiTextCircleExtend.ModifyMesh(verts);
            UITextOutlineExtend.NormalOutLineModifyMesh(verts);
            m_uiTextVertexColorExtend?.PopulateMesh(verts, rectTransform, color);
            UITextGradientColorExtend?.ModifyMesh(verts);
            m_uiTextShadowExtend?.PopulateMesh(verts, rectTransform, color);
        }

        public void SetUseOutLine(bool useOutLine)
        {
            m_uiTextOutlineExtend.SetUseTextOutline(useOutLine);
        }

        public void SetOutLineAlpha(float alpha)
        {
            m_uiTextOutlineExtend.SetAlpha(alpha);
        }

        public void SetOutLineColor(Color32 color32, bool isOpenShaderOutline = true)
        {
            SetOutLineColor(color32, 1, isOpenShaderOutline);
        }

        public void SetOutLineColor(Color32 color32, int outlineWidth, bool isOpenShaderOutline = true)
        {
            if (m_uiTextOutlineExtend == null)
            {
                return;
            }
            m_uiTextOutlineExtend.SetOutLineColor(color32);
            m_uiTextOutlineExtend.SetOutLineWidth(outlineWidth);
            if (!isOpenShaderOutline)
            {
                m_uiTextOutlineExtend.isOpenShaderOutline = false;
            }
        }

        public void SetGradientColor(Color32 topColor, Color32 bottomColor, Color32 leftColor = default, Color32 rightColor = default, float verticalOffset = 0f, float horizontalOffset = 0f, bool splitTextGradient = false)
        {
            m_uiTextGradientColorExtend.SetGradientColor(topColor, bottomColor, leftColor, rightColor, verticalOffset, horizontalOffset, splitTextGradient);
        }

        public void SetGradientTop2BottomColor(Color32 topColor, Color32 bottomColor, float verticalOffset = 0f, bool splitTextGradient = false)
        {
            m_uiTextGradientColorExtend.SetGradientColor(topColor, bottomColor, Color.white, Color.white, verticalOffset, 0, splitTextGradient);
        }

        public void SetGradientLeft2RightColor(Color32 leftColor, Color32 rightColor, float horizontalOffset, bool splitTextGradient = false)
        {
            m_uiTextGradientColorExtend.SetGradientColor(Color.white, Color.white, leftColor, rightColor, 0, horizontalOffset, splitTextGradient);
        }

        public string GetParameterValue(int ParamName)
            => UITextLocalizationExtend?.GetParameterValue(ParamName);

        public string GetParameterValue(string ParamName)
            => UITextLocalizationExtend?.GetParameterValue(ParamName);

        public void SetUITextLocalizationActive(bool active)
        {
            UITextLocalizationExtend.UseI2Localization = active;
        }

        public void SetParameterValue(int ParamName, string ParamValue, bool localize = true)
            => UITextLocalizationExtend?.SetParameterValue(ParamName, ParamValue, localize);

        public void SetParameterValue(int ParamName, TextDefine ParamValue, bool localize = true)
            => UITextLocalizationExtend?.SetParameterValue(ParamName, ParamValue, localize);

        public void SetParameterValue(string ParamName, TextDefine ParamValue, bool localize = true)
            => UITextLocalizationExtend?.SetParameterValue(ParamName, ParamValue, localize);

        public void SetParameterValue(string ParamName, string ParamValue, bool localize = true)
            => UITextLocalizationExtend?.SetParameterValue(ParamName, ParamValue, localize);

        public void SetParameterValue(List<int> ParamNames, List<string> ParamValues, bool localize = true)
            => UITextLocalizationExtend?.SetParameterValue(ParamNames, ParamValues, localize);

        public void SetTerm(TextDefine textDefine)
            => UITextLocalizationExtend?.SetTerm(textDefine);


#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            UITextGradientColorExtend?.EditorInitialize(this);
            UITextOutlineExtend?.EditorInitialize(this);
            UITextOutlineExtend?.OnValidate();
            m_uiTextLocalizationExtend?.EditorInitialize(this);
            this.SetAllDirty();
        }
#endif

        protected override void OnDestroy()
        {
            UITextOutlineExtend?.OnDestroy();
            base.OnDestroy();
        }
    }
}