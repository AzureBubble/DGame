using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace GameLogic
{
    [Flags]
    public enum MatEffectType
    {
        None = 0,
        Gray = 1 << 0,
        Circle = 1 << 1,
    }

    public class UIImageEffect : BaseMeshEffect
    {
        private const string MATERIAL_LOCATION = "UIMat";
        private const string GRAY_KEY_WORD_NAME = "GRAY_ON";
        private const string CIRCLE_KEY_WORD_NAME = "_IsCircle";
        private static readonly int IS_CIRCLE = Shader.PropertyToID(CIRCLE_KEY_WORD_NAME);

        [SerializeField] private bool m_debugRefresh = false;

        public bool Dirty { get; set; } = false;

        [SerializeField] private bool m_isGray = false;

        public bool IsGray
        {
            get => m_isGray;
            set
            {
                if (m_isGray != value)
                {
                    m_isGray = value;
                    Dirty = true;
                }
            }
        }

        [SerializeField] private bool m_isCircle = false;

        public bool IsCircle
        {
            get => m_isCircle;
            set
            {
                if (m_isCircle != value)
                {
                    m_isCircle = value;
                    Dirty = true;
                }
            }
        }

        private Image m_cacheImage;

        public Image CacheImage => m_cacheImage == null ? m_cacheImage = GetComponent<Image>() : m_cacheImage;

        private static bool m_init = false;
        private static Dictionary<MatEffectType, Material> m_matDict = new Dictionary<MatEffectType, Material>();

        protected override void Awake()
        {
            Dirty = true;
        }

        private void LateUpdate()
        {
            if (m_debugRefresh || Dirty)
            {
                m_debugRefresh = false;
                Dirty = false;
                ApplyChange();
            }
        }

        private void ApplyChange()
        {
            if (CacheImage != null)
            {
                var mainTexture = CacheImage.mainTexture;

                if (mainTexture != null)
                {
                    CacheImage.material = GetMat(IsGray, IsCircle);
                }
            }
        }

        private static void InitMatDict()
        {
            if (m_init)
            {
                return;
            }

            m_init = true;
            var mat = GameModule.ResourceModule.LoadAsset<Material>(MATERIAL_LOCATION);

            if (mat != null)
            {
                m_matDict.Add(MatEffectType.None, null);
                AddNewMatToDict(mat, true, true);
                AddNewMatToDict(mat, false, true);
                AddNewMatToDict(mat, true, false);
            }
        }

        private static void AddNewMatToDict(Material mat, bool isGray, bool isCircle)
        {
            var newMat = new Material(mat);

            if (isGray)
            {
                newMat.EnableKeyword(GRAY_KEY_WORD_NAME);
            }
            else
            {
                newMat.DisableKeyword(GRAY_KEY_WORD_NAME);
            }

            newMat.SetInt(IS_CIRCLE, isCircle ? 1 : 0);
            m_matDict.Add(GetMatKey(isGray, isCircle), newMat);
        }

        private static MatEffectType GetMatKey(bool isGray, bool isCircle)
        {
            MatEffectType key = MatEffectType.None;
            if (isGray) key |= MatEffectType.Gray;
            if (isCircle) key |= MatEffectType.Circle;
            return key;
        }

        private static Material GetMat(bool isGray, bool isCircle)
        {
            InitMatDict();
            m_matDict.TryGetValue(GetMatKey(isGray, isCircle), out var mat);
            return mat;
        }
        
        public static void ClearCache()
        {
            foreach (var kvp in m_matDict)
            {
                if (kvp.Value != null)
                {
                    GameObject.Destroy(kvp.Value);
                }
            }
            m_matDict.Clear();
            m_init = false;
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (CacheImage == null || !IsActive())
            {
                return;
            }

            if (IsCircle)
            {
                Vector4 uv = m_cacheImage.sprite != null
                    ? DataUtility.GetOuterUV(m_cacheImage.sprite)
                    : Vector4.zero;
                float uvWidth = uv.z - uv.x;
                float uvHeight = uv.w - uv.y;

                if (uvWidth == 0 || uvHeight == 0)
                {
                    return;
                }

                int vertCount = vh.currentVertCount;
                var vert = new UIVertex();

                for (int i = 0; i < vertCount; ++i)
                {
                    vh.PopulateUIVertex(ref vert, i);
                    vert.uv1.x = (vert.uv0.x - uv.x) / uvWidth;
                    vert.uv1.y = (vert.uv0.y - uv.y) / uvHeight;
                    vh.SetUIVertex(vert, i);
                }
            }
        }
    }
}
