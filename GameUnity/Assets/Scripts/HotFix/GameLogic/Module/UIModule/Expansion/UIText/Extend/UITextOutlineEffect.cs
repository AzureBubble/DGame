using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GameLogic
{
    [DisallowMultipleComponent]
    public class UITextOutlineEffect : BaseMeshEffect
    {
        private const string OutLineShaderName = "UGUIPro/UIText";

        private bool m_initParams;

        [SerializeField, HideInInspector] private bool m_isUseTextOutline;

        public bool UseTextOutline
        {
            get => m_isUseTextOutline;
            set => m_isUseTextOutline = value;
        }

        private UITextShadowExtend m_uiTextShadow => (m_text as UIText)?.UITextShadowExtend;
        [SerializeField, HideInInspector] private bool m_isOpenShaderOutline = true;
        [FormerlySerializedAs("m_textOutlineEx")] [SerializeField, HideInInspector]
        private UITextShaderOutline textShaderOutlineEx;
        [SerializeField, HideInInspector, Range(1, 10)] private int m_outLineWidth = 1;
        [SerializeField, HideInInspector] private Color32 m_outLineColor = Color.black;
        [SerializeField, HideInInspector] private Camera m_camera;
        [SerializeField, Range(0f, 1f), HideInInspector] private float m_alpha = 1f;

        private List<UIVertex> m_vertexList;
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

        protected override void OnDestroy()
        {
            if (m_vertexList != null)
            {
                ListPool<UIVertex>.Recycle(m_vertexList);
            }
            base.OnDestroy();
        }

        protected override void Awake()
        {
            base.Awake();
            m_vertexList = ListPool<UIVertex>.Get();
            if (!string.IsNullOrEmpty(this.TextGraphic.text))
            {
                this.UpdateOutLineInfos();
            }
            this.hideFlags = HideFlags.HideInInspector;
        }

        public void SetUseOutLineColor(bool isUseOutLine)
        {
            m_isUseTextOutline = isUseOutLine;
        }
        public void SetCamera(Camera c)
        {
            if (m_camera == c) return;
            this.m_camera = c;
        }

        public void SetOutLineColor(Color outLineColor)
        {
            this.m_outLineColor = outLineColor;
            if (base.graphic && this.textShaderOutlineEx)
            {
                this.textShaderOutlineEx.SetOutLineColor(this.m_outLineColor);
                base.graphic.SetAllDirty();
            }
        }

        public void SetOutLineWidth(int outLineWidth)
        {
            this.m_outLineWidth = outLineWidth;
            if (base.graphic && this.textShaderOutlineEx)
            {
                this.textShaderOutlineEx.SetOutLineWidth(this.m_outLineWidth);
                base.graphic.SetAllDirty();
            }
        }

        public void SetAlpha(float setAlphaValue)
        {
            this.m_alpha = setAlphaValue;
            byte alphaByte = (byte)(this.m_alpha * 255);
            this. m_outLineColor.a = alphaByte;
            if (base.graphic && this.textShaderOutlineEx)
            {
                base.graphic.SetAllDirty();
            }
        }

        public void SetShaderOutLine(bool outlineUseShader)
        {
            if (!m_isUseTextOutline) return;
            if (this.textShaderOutlineEx == null)
            {
                if (!this.gameObject.TryGetComponent(out textShaderOutlineEx))
                {
                    this.textShaderOutlineEx = this.gameObject.AddComponent<UITextShaderOutline>();
                }
                this.textShaderOutlineEx.graphic = base.graphic;
            }
            else
            {
                this.textShaderOutlineEx.enabled = true;
            }
            this.textShaderOutlineEx.hideFlags = HideFlags.HideInInspector;
            this.m_isOpenShaderOutline = outlineUseShader;
            this.UpdateOutLineInfos();
        }

        public void UpdateOutLineInfos()
        {
            if (this.textShaderOutlineEx == null) return;

            if (m_isUseTextOutline)
            {
                this.textShaderOutlineEx.SwitchShaderOutLine(this.m_isOpenShaderOutline);
                this.textShaderOutlineEx.SetOutLineColor(this.m_outLineColor);
                this.textShaderOutlineEx.SetOutLineWidth(this.m_outLineWidth);
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
            if (!IsActive() || !m_isUseTextOutline)
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

            if (!this.m_isOpenShaderOutline || textShaderOutlineEx == null)
            {
                if (this.textShaderOutlineEx != null)
                {
                    this.textShaderOutlineEx.enabled = false;
                }

                for (int i = 0; i < this.m_vertexList.Count; i += 4)
                {

                    UIVertex TL = GeneralUIVertex(this.m_vertexList[i + 0]);
                    UIVertex TR = GeneralUIVertex(this.m_vertexList[i + 1]);
                    UIVertex BR = GeneralUIVertex(this.m_vertexList[i + 2]);
                    UIVertex BL = GeneralUIVertex(this.m_vertexList[i + 3]);

                    //先绘制上四个
                    // UIVertex CR = default(UIVertex);
                    // UIVertex CL = default(UIVertex);

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

                        vh.AddVert(o_BR);
                        vh.AddVert(o_BL);
                    }

                    vh.AddVert(TL);
                    vh.AddVert(TR);

                    vh.AddVert(BR);
                    vh.AddVert(BL);
                }

                for (int i = 0; i < vh.currentVertCount; i += 4)
                {
                    vh.AddTriangle(i + 0, i + 1, i + 2);
                    vh.AddTriangle(i + 2, i + 3, i + 0);
                }
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!m_isUseTextOutline) return;

            if (m_vertexList == null)
            {
                m_vertexList = ListPool<UIVertex>.Get();
            }

            this.m_vertexList.Clear();
            //if (m_Text.text.Equals("Bonus"))
            //{
            //    Debug.ColorLog(LogColor.Yellow, "Bonus>>>>>>>>>>>>>Start>>>>>>>>>>>>>>Bonus>>>>>>");
            //}

            if (m_isOpenShaderOutline && this.textShaderOutlineEx)
            {
                this.textShaderOutlineEx.ModifyMesh(vh);
            }
            else
            {
                this._ProcessVertices(vh);
            }

            if (UseTextOutline)
            {
                m_uiTextShadow?.PopulateMesh(vh, m_text.rectTransform, m_text.color);
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
            if (!m_isUseTextOutline) return;
            if (this.m_isOpenShaderOutline)
            {
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