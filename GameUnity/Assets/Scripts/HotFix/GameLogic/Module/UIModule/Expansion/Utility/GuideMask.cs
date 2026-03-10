using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [RequireComponent(typeof(RectTransform))]
    public class GuideMask : MaskableGraphic, ICanvasRaycastFilter
    {
        [SerializeField] private RectTransform m_target;

        private Vector3 m_targetMin = Vector3.zero;
        private Vector3 m_targetMax = Vector3.zero;
        private bool m_canRefresh = true;
        private Transform m_cacheTrans = null;
        private readonly Vector3[] m_corners = new Vector3[4];

        /// <summary>
        /// 设置镂空的目标
        /// </summary>
        public void SetTarget(RectTransform target)
        {
            m_canRefresh = true;
            m_target = target;
            _RefreshView();
        }

        private void _SetTarget(Vector3 tarMin, Vector3 tarMax)
        {
            if (tarMin == m_targetMin && tarMax == m_targetMax)
                return;
            m_targetMin = tarMin;
            m_targetMax = tarMax;
            SetAllDirty();
        }

        private void _RefreshView()
        {
            if (!m_canRefresh) return;
            m_canRefresh = false;

            if (null == m_target)
            {
                _SetTarget(Vector3.zero, Vector3.zero);
            }
            else
            {
                // 改为立即计算，不使用协程，确保布局已重建
                RefreshImmediate();
            }
        }

        private void RefreshImmediate()
        {
            if (m_target == null || !m_target.gameObject.activeInHierarchy)
            {
                _SetTarget(Vector3.zero, Vector3.zero);
                return;
            }

            // 检查m_cacheTrans是否就绪（应对Prefab实例化时序问题）
            if (m_cacheTrans == null)
            {
                m_cacheTrans = GetComponent<RectTransform>();
            }

            // 强制重建目标布局，确保位置/大小正确
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_target);

            // 获取世界坐标系的四个角（左下、右下、右上、左上）

            m_target.GetWorldCorners(m_corners);

            // 转换到当前遮罩的本地坐标系
            for (int i = 0; i < 4; i++)
            {
                m_corners[i] = m_cacheTrans.InverseTransformPoint(m_corners[i]);
            }

            // 计算轴对齐包围盒（考虑旋转情况）
            float minX = Mathf.Min(m_corners[0].x, m_corners[1].x, m_corners[2].x, m_corners[3].x);
            float minY = Mathf.Min(m_corners[0].y, m_corners[1].y, m_corners[2].y, m_corners[3].y);
            float maxX = Mathf.Max(m_corners[0].x, m_corners[1].x, m_corners[2].x, m_corners[3].x);
            float maxY = Mathf.Max(m_corners[0].y, m_corners[1].y, m_corners[2].y, m_corners[3].y);

            Vector3 targetMin = new Vector3(minX, minY, 0);
            Vector3 targetMax = new Vector3(maxX, maxY, 0);

            _SetTarget(targetMin, targetMax);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (m_targetMin == Vector3.zero && m_targetMax == Vector3.zero)
            {
                base.OnPopulateMesh(vh);
                return;
            }

            vh.Clear();

            UIVertex vert = UIVertex.simpleVert;
            vert.color = color;

            Vector2 selfPivot = rectTransform.pivot;
            Rect selfRect = rectTransform.rect;

            float outerLx = -selfPivot.x * selfRect.width;
            float outerBy = -selfPivot.y * selfRect.height;
            float outerRx = (1 - selfPivot.x) * selfRect.width;
            float outerTy = (1 - selfPivot.y) * selfRect.height;

            // 确保内框在外框内（防止三角形交叉）
            float innerLx = Mathf.Max(m_targetMin.x, outerLx);
            float innerBy = Mathf.Max(m_targetMin.y, outerBy);
            float innerRx = Mathf.Min(m_targetMax.x, outerRx);
            float innerTy = Mathf.Min(m_targetMax.y, outerTy);

            // 0 - Outer:LT
            vert.position = new Vector3(outerLx, outerTy);
            vh.AddVert(vert);
            // 1 - Outer:RT
            vert.position = new Vector3(outerRx, outerTy);
            vh.AddVert(vert);
            // 2 - Outer:RB
            vert.position = new Vector3(outerRx, outerBy);
            vh.AddVert(vert);
            // 3 - Outer:LB
            vert.position = new Vector3(outerLx, outerBy);
            vh.AddVert(vert);

            // 4 - Inner:LT
            vert.position = new Vector3(innerLx, innerTy);
            vh.AddVert(vert);
            // 5 - Inner:RT
            vert.position = new Vector3(innerRx, innerTy);
            vh.AddVert(vert);
            // 6 - Inner:RB
            vert.position = new Vector3(innerRx, innerBy);
            vh.AddVert(vert);
            // 7 - Inner:LB
            vert.position = new Vector3(innerLx, innerBy);
            vh.AddVert(vert);

            // 四个区域，每个区域两个三角形（顺时针顺序，确保正面朝向相机）
            // 上方区域
            vh.AddTriangle(0, 1, 5);
            vh.AddTriangle(0, 5, 4);
            // 右方区域
            vh.AddTriangle(1, 2, 6);
            vh.AddTriangle(1, 6, 5);
            // 下方区域
            vh.AddTriangle(2, 3, 7);
            vh.AddTriangle(2, 7, 6);
            // 左方区域
            vh.AddTriangle(3, 0, 4);
            vh.AddTriangle(3, 4, 7);
        }

        bool ICanvasRaycastFilter.IsRaycastLocationValid(Vector2 screenPos, Camera eventCamera)
        {
            if (null == m_target) return true;
            // 将目标对象范围内的事件镂空（使其穿过）
            return !RectTransformUtility.RectangleContainsScreenPoint(m_target, screenPos, eventCamera);
        }

        protected override void Awake()
        {
            base.Awake();
            m_cacheTrans = GetComponent<RectTransform>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // 启用时强制刷新
            m_canRefresh = m_target != null;
            _RefreshView();
        }

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();

            // 编辑器模式下实时刷新，方便调试
            if (m_target != null && Application.isPlaying)
            {
                m_canRefresh = true;
                _RefreshView();
            }
        }

#endif
    }
}