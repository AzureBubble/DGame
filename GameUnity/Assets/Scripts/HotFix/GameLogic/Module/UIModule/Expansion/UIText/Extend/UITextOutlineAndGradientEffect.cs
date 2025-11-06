using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class UITextOutlineAndGradientEffect : BaseMeshEffect
    {
        private const string OutLineShaderName = "UGUIPro/UIText";

        private bool m_initParams;

        [SerializeField, HideInInspector] private bool m_isUseTextOutline;
        [SerializeField, HideInInspector] private bool m_isUseTextGradient;
        public bool UseTextOutline { get => m_isUseTextOutline; set => m_isUseTextOutline = value; }
        public bool UseTextGradient { get => m_isUseTextGradient; set => m_isUseTextGradient = value; }

        [SerializeField, HideInInspector] private bool m_isOpenShaderOutline = true;
        [SerializeField, HideInInspector] private float m_lerpValue = 0f;
        [SerializeField, HideInInspector] private UITextOutline m_textOutlineEx;
        [SerializeField, HideInInspector, Range(1, 10)] private int m_outLineWidth = 1;
        [SerializeField, HideInInspector] private GradientType m_gradientType = GradientType.TwoColor;
        [SerializeField, HideInInspector] private Color32 m_gradientTopColor = Color.white;
        [SerializeField, HideInInspector] private Color32 m_gradientMiddleColor = Color.white;
        [SerializeField, HideInInspector] private Color32 m_gradientBottomColor = Color.white;
        [SerializeField, HideInInspector] private Color32 m_outLineColor = Color.black;
        [SerializeField, HideInInspector] private Camera m_camera;
        [SerializeField, Range(0f, 1f), HideInInspector] private float m_alpha = 1f;
        [SerializeField, Range(0.1f, 0.9f), HideInInspector] private float m_colorOffset = 0.5f;

        private List<UIVertex> m_vertexList = new List<UIVertex>();
        private Vector3[] m_outLineDis = new Vector3[4];
        private Text m_text;

        public Text TextGraphic
        {
            get
            {
                if (!this.m_text && base.graphic)
                {
                    this.m_text = base.graphic as Text;
                }
                else
                {
                    if (!base.graphic)
                    {
                        throw new Exception("No Find base Graphic!!");
                    }
                }
                return this.m_text;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (!string.IsNullOrEmpty(this.TextGraphic.text))
            {
                this.UpdateOutLineInfos();
            }
            this.hideFlags = HideFlags.HideInInspector;
        }

        public void SetUseGradientColor(bool isUseGradient)
        {
            m_isUseTextGradient = isUseGradient;
        }

        public void SetUseOutLineColor(bool isUseOutLine)
        {
            m_isUseTextOutline = isUseOutLine;
        }

        public void SetLerpValue(float lerpValue)
        {
            m_lerpValue = lerpValue;
        }

        public void SetCamera(Camera c)
        {
            if (m_camera == c) return;
            this.m_camera = c;
        }

        public void SetGradientType(GradientType gradientType)
        {
            this.m_gradientType = gradientType;
        }

        public GradientType GetGradientType()
        {
            return m_gradientType;
        }

        public void SetTopColor(Color topColor)
        {
            this.m_gradientTopColor = topColor;
        }

        public Color GetTopColor()
        {
            return m_gradientTopColor;
        }

        public void SetMiddleColor(Color middleColor)
        {
            this.m_gradientMiddleColor = middleColor;
        }

        public void SetBottomColor(Color bottomColor)
        {
            this.m_gradientBottomColor = bottomColor;
        }

        public void SetColorOffset(float colorOffset)
        {
            this.m_colorOffset = colorOffset;
        }

        public void SetOutLineColor(Color outLineColor)
        {
            this.m_outLineColor = outLineColor;
            if (base.graphic && this.m_textOutlineEx)
            {
                this.m_textOutlineEx.SetOutLineColor(this.m_outLineColor);
                base.graphic.SetAllDirty();
            }
        }

        public void SetOutLineWidth(int outLineWidth)
        {
            this.m_outLineWidth = outLineWidth;
            if (base.graphic && this.m_textOutlineEx)
            {
                this.m_textOutlineEx.SetOutLineWidth(this.m_outLineWidth);
                base.graphic.SetAllDirty();
            }
        }

        public void SetAlpha(float setAlphaValue)
        {
            this.m_alpha = setAlphaValue;
            byte alphaByte = (byte)(this.m_alpha * 255);
            this.m_gradientTopColor.a = alphaByte;
            this.m_gradientBottomColor.a = alphaByte;
            this.m_gradientMiddleColor.a = alphaByte;
            this. m_outLineColor.a = alphaByte;
            if (base.graphic && this.m_textOutlineEx)
            {
                base.graphic.SetAllDirty();
            }
        }

        public void SetShaderOutLine(bool outlineUseShader)
        {
            if (!m_isUseTextOutline) return;
            if (!this.m_textOutlineEx)
            {
                this.m_textOutlineEx = this.gameObject.GetComponent<UITextOutline>();
                if (!this.m_textOutlineEx)
                    this.m_textOutlineEx = this.gameObject.AddComponent<UITextOutline>();
                this.m_textOutlineEx.graphic = base.graphic;
            }
            else
            {
                this.m_textOutlineEx.enabled = true;
            }
            this.m_textOutlineEx.hideFlags = HideFlags.HideInInspector;
            this.m_isOpenShaderOutline = outlineUseShader;
            this.UpdateOutLineInfos();
        }

        public void UpdateOutLineInfos()
        {
            if (!this.m_textOutlineEx) return;

            if (m_isUseTextOutline)
            {
                this.m_textOutlineEx.SwitchShaderOutLine(this.m_isOpenShaderOutline);
                this.m_textOutlineEx.SetOutLineColor(this.m_outLineColor);
                this.m_textOutlineEx.SetOutLineWidth(this.m_outLineWidth);
            }

            if (m_isUseTextGradient)
            {
                this.m_textOutlineEx.SetUseThree(this.m_gradientType == GradientType.ThreeColor);
            }

            this.UpdateOutLineMaterial();
            if (base.graphic != null)
            {
                this.OpenShaderParams();
                base.graphic.SetAllDirty();
            }
        }

        private void UpdateOutLineMaterial()
        {
            if (!m_isUseTextOutline) return;
#if !UNITY_EDITOR

            if (base.graphic && base.graphic.material == base.graphic.defaultMaterial)
            {
                Shader shader = Shader.Find(OutLineShaderName);
                if (shader)
                {
                    base.graphic.material = new Material(shader);
                }
            }

#else
            if (!Application.isPlaying)
            {
                if (base.graphic && base.graphic.material == base.graphic.defaultMaterial)
                {
                    Material material= UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Scripts/HotFix/GameLogic/Module/UIModule/Expansion/UIText/Shaders/UGUIPro_UIText.mat");

                    if (material == null)
                    {
                        Debug.LogError("Text Out Line Material Not Find Please Check Material Path!");
                    }
                    base.graphic.material = material;
                }
            }
            else
            {
                if (base.graphic && base.graphic.material == base.graphic.defaultMaterial)
                {
                    Shader shader = Shader.Find(OutLineShaderName);
                    if (shader)
                    {
                        base.graphic.material = new Material(shader);
                    }
                }
            }
#endif
            if (base.graphic)
            {
                Texture fontTexture = null;
                if (this.TextGraphic)
                {
                    if (this.graphic && this.TextGraphic.font)
                    {
                        fontTexture = this.TextGraphic.font.material.mainTexture;
                    }

                    if (base.graphic.material && base.graphic.material != base.graphic.defaultMaterial)
                        base.graphic.material.mainTexture = fontTexture;
                }
            }
        }

        private void OpenShaderParams()
        {
            if (!m_isUseTextOutline) return;
            if (base.graphic && !this.m_initParams)
            {
                if (base.graphic.canvas)
                {
                    var v1 = graphic.canvas.additionalShaderChannels;
                    var v2 = AdditionalCanvasShaderChannels.TexCoord1;
                    if ((v1 & v2) != v2)
                    {
                        base.graphic.canvas.additionalShaderChannels |= v2;
                    }

                    v2 = AdditionalCanvasShaderChannels.TexCoord2;
                    if ((v1 & v2) != v2)
                    {
                        base.graphic.canvas.additionalShaderChannels |= v2;
                    }

                    v2 = AdditionalCanvasShaderChannels.TexCoord3;
                    if ((v1 & v2) != v2)
                    {
                        base.graphic.canvas.additionalShaderChannels |= v2;
                    }

                    v2 = AdditionalCanvasShaderChannels.Tangent;
                    if ((v1 & v2) != v2)
                    {
                        base.graphic.canvas.additionalShaderChannels |= v2;
                    }

                    v2 = AdditionalCanvasShaderChannels.Normal;
                    if ((v1 & v2) != v2)
                    {
                        base.graphic.canvas.additionalShaderChannels |= v2;
                    }
                    this.m_initParams = true;
                }
            }
        }

        private void _ProcessVertices(VertexHelper vh)
        {
            if (!IsActive())
            {
                return;
            }

            var count = vh.currentVertCount;
            if (count == 0)
                return;

            /*
             *  TL--------TR
             *  |          |^
             *  |          ||
             *  CL--------CR
             *  |          ||
             *  |          |v
             *  BL--------BR
             * **/

            for (int i = 0; i < count; i++)
            {
                UIVertex vertex = UIVertex.simpleVert;
                vh.PopulateUIVertex(ref vertex, i);
                this.m_vertexList.Add(vertex);
            }
            vh.Clear();

            for (int i = 0; i < this.m_vertexList.Count; i += 4)
            {

                UIVertex TL = GeneralUIVertex(this.m_vertexList[i + 0]);
                UIVertex TR = GeneralUIVertex(this.m_vertexList[i + 1]);
                UIVertex BR = GeneralUIVertex(this.m_vertexList[i + 2]);
                UIVertex BL = GeneralUIVertex(this.m_vertexList[i + 3]);

                //先绘制上四个
                UIVertex CR = default(UIVertex);
                UIVertex CL = default(UIVertex);

                if (m_isUseTextGradient)
                {
                    //如果是OneColor模式，则颜色不做二次处理
                    if (this.m_gradientType == GradientType.NoColor)
                    {

                    }
                    else
                    {
                        TL.color = this.m_gradientTopColor;
                        TR.color = this.m_gradientTopColor;
                        BL.color = this.m_gradientBottomColor;
                        BR.color = this.m_gradientBottomColor;
                    }
                }

                if (this.m_isUseTextOutline)
                {

                    if (!this.m_isOpenShaderOutline)
                    {
                        if (this.m_textOutlineEx)
                        {
                            this.m_textOutlineEx.enabled = false;
                        }

                        this.m_outLineDis[0].Set(-this.m_outLineWidth, this.m_outLineWidth, 0); //LT
                        this.m_outLineDis[1].Set(this.m_outLineWidth, this.m_outLineWidth, 0); //RT
                        this.m_outLineDis[2].Set(-this.m_outLineWidth, -this.m_outLineWidth, 0); //LB
                        this.m_outLineDis[3].Set(this.m_outLineWidth, -this.m_outLineWidth, 0); //RB


                        for (int j = 0; j < 4; j++)
                        {
                            //四个方向
                            UIVertex o_TL = GeneralUIVertex(TL);
                            UIVertex o_TR = GeneralUIVertex(TR);
                            UIVertex o_BR = GeneralUIVertex(BR);
                            UIVertex o_BL = GeneralUIVertex(BL);


                            o_TL.position += this.m_outLineDis[j];
                            o_TR.position += this.m_outLineDis[j];
                            o_BR.position += this.m_outLineDis[j];
                            o_BL.position += this.m_outLineDis[j];

                            o_TL.color = this.m_outLineColor;
                            o_TR.color = this.m_outLineColor;
                            o_BR.color = this.m_outLineColor;
                            o_BL.color = this.m_outLineColor;

                            vh.AddVert(o_TL);
                            vh.AddVert(o_TR);

                            if (m_isUseTextGradient)
                            {
                                if (this.m_gradientType == GradientType.ThreeColor)
                                {
                                    UIVertex o_CR = default(UIVertex);
                                    UIVertex o_CL = default(UIVertex);

                                    o_CR = GeneralUIVertex(this.m_vertexList[i + 2]);
                                    o_CL = GeneralUIVertex(this.m_vertexList[i + 3]);
                                    //var New_S_Point = this.ConverPosition(o_TR.position, o_BR.position, this.m_ColorOffset);

                                    o_CR.position.y = Mathf.Lerp(o_TR.position.y, o_BR.position.y, this.m_colorOffset);
                                    o_CL.position.y = Mathf.Lerp(o_TR.position.y, o_BR.position.y, this.m_colorOffset);

                                    if (Mathf.Approximately(TR.uv0.x, BR.uv0.x))
                                    {
                                        o_CR.uv0.y = Mathf.Lerp(TR.uv0.y, BR.uv0.y, this.m_colorOffset);
                                        o_CL.uv0.y = Mathf.Lerp(TL.uv0.y, BL.uv0.y, this.m_colorOffset);
                                    }
                                    else
                                    {
                                        o_CR.uv0.x = Mathf.Lerp(TR.uv0.x, BR.uv0.x, this.m_colorOffset);
                                        o_CL.uv0.x = Mathf.Lerp(TL.uv0.x, BL.uv0.x, this.m_colorOffset);
                                    }

                                    o_CR.color = this.m_outLineColor;
                                    o_CL.color = this.m_outLineColor;


                                    vh.AddVert(o_CR);
                                    vh.AddVert(o_CL);
                                }
                            }

                            vh.AddVert(o_BR);
                            vh.AddVert(o_BL);
                        }
                    }
                }

                if (this.m_gradientType == GradientType.ThreeColor && this.m_isUseTextGradient &&
                    this.m_isOpenShaderOutline)
                {
                    UIVertex t_TL = GeneralUIVertex(TL);
                    UIVertex t_TR = GeneralUIVertex(TR);
                    UIVertex t_BR = GeneralUIVertex(BR);
                    UIVertex t_BL = GeneralUIVertex(BL);

                    // t_TL.color.a = 0;
                    // t_TR.color.a = 0;
                    // t_BR.color.a = 0;
                    // t_BL.color.a = 0;
                    //vh.AddVert(t_TL);
                    //vh.AddVert(t_TR);

                    //vh.AddVert(t_BR);
                    //vh.AddVert(t_BL);
                }

                vh.AddVert(TL);
                vh.AddVert(TR);

                if (this.m_gradientType == GradientType.ThreeColor && this.m_isUseTextGradient)
                {
                    CR = GeneralUIVertex(this.m_vertexList[i + 2]);
                    CL = GeneralUIVertex(this.m_vertexList[i + 3]);
                    //var New_S_Point = this.ConverPosition(TR.position, BR.position, this.m_ColorOffset);

                    CR.position.y = Mathf.Lerp(TR.position.y, BR.position.y - 0.1f, this.m_colorOffset);
                    CL.position.y = Mathf.Lerp(TR.position.y, BR.position.y, this.m_colorOffset);

                    if (Mathf.Approximately(TR.uv0.x, BR.uv0.x))
                    {
                        CR.uv0.y = Mathf.Lerp(TR.uv0.y, BR.uv0.y, this.m_colorOffset);
                        CL.uv0.y = Mathf.Lerp(TL.uv0.y, BL.uv0.y, this.m_colorOffset);
                    }
                    else
                    {
                        CR.uv0.x = Mathf.Lerp(TR.uv0.x, BR.uv0.x, this.m_colorOffset);
                        CL.uv0.x = Mathf.Lerp(TL.uv0.x, BL.uv0.x, this.m_colorOffset);
                    }

                    CR.color = this.m_gradientMiddleColor;
                    CL.color = this.m_gradientMiddleColor;
                    // CR.color = Color32.Lerp(this.m_MiddleColor, this.m_BottomColor, this.m_LerpValue);
                    // CL.color = Color32.Lerp(this.m_MiddleColor, this.m_BottomColor, this.m_LerpValue);
                    vh.AddVert(CR);
                    vh.AddVert(CL);
                }

                //绘制下四个
                if (this.m_gradientType == GradientType.ThreeColor && this.m_isUseTextGradient)
                {
                    vh.AddVert(CL);
                    vh.AddVert(CR);
                }

                vh.AddVert(BR);
                vh.AddVert(BL);
            }

            for (int i = 0; i < vh.currentVertCount; i += 4)
            {
                vh.AddTriangle(i + 0, i + 1, i + 2);
                vh.AddTriangle(i + 2, i + 3, i + 0);
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!m_isUseTextOutline && !m_isUseTextGradient) return;
            this.m_vertexList.Clear();
            //if (m_Text.text.Equals("Bonus"))
            //{
            //    Debug.ColorLog(LogColor.Yellow, "Bonus>>>>>>>>>>>>>Start>>>>>>>>>>>>>>Bonus>>>>>>");
            //}
            this._ProcessVertices(vh);

            if (this.m_isUseTextOutline && this.m_textOutlineEx)
            {
                this.m_textOutlineEx.ModifyMesh(vh);
            }
            //if (m_Text.text.Equals("Bonus"))
            //{
            //    Debug.ColorLog(LogColor.Cyan, "Bonus>>>>>>>>>>>>>End>>>>>>>>>>>>>>Bonus>>>>>>");
            //}
        }

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();
            if (this.m_isOpenShaderOutline)
            {
                if (!m_isUseTextOutline) return;
                this.UpdateOutLineMaterial();
                this.Refresh();
            }
        }

#endif

        private void Refresh()
        {
            if (base.graphic)
            {
                base.graphic.SetVerticesDirty();
            }
        }

        public static UIVertex GeneralUIVertex(UIVertex vertex)
        {
            UIVertex result = UIVertex.simpleVert;
            result.normal = new Vector3(vertex.normal.x, vertex.normal.y, vertex.normal.z);
            result.position = new Vector3(vertex.position.x, vertex.position.y, vertex.position.z);
            result.tangent = new Vector4(vertex.tangent.x, vertex.tangent.y, vertex.tangent.z, vertex.tangent.w);
            result.uv0 = new Vector2(vertex.uv0.x, vertex.uv0.y);
            result.uv1 = new Vector2(vertex.uv1.x, vertex.uv1.y);
            result.color = vertex.color;
            return result;
        }
    }
}