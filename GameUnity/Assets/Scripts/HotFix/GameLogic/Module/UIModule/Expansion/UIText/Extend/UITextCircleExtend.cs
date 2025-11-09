using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GameLogic
{
    [Serializable]
    public class UITextCircleExtend
    {
        [SerializeField] private bool m_useTextCircle;
        [SerializeField, Range(100, 1000)] private float m_radius = 100;
        [SerializeField, Range(0, 10)] private float m_spaceCoff = 1f;
        [SerializeField, Range(0, 360)] private float m_angleOffset = 0;

         // 缓存计算用的变量，避免在循环中重复创建
        private readonly UIVertex[] m_vertices = new UIVertex[4];
        private Matrix4x4 m_transformMatrix;

        public void ModifyMesh(VertexHelper vh)
        {
            if (!m_useTextCircle || m_radius == 0 || vh.currentVertCount == 0)
            {
                return;
            }

            // 预先计算常用值
            float radiusReciprocal = 1f / m_radius;
            int quadCount = vh.currentVertCount >> 2; // vh.currentVertCount / 4

            for (int i = 0; i < quadCount; i++)
            {
                // 批量读取顶点数据
                int vertexIndex = i << 2; // i * 4
                for (int j = 0; j < 4; j++)
                {
                    vh.PopulateUIVertex(ref m_vertices[j], vertexIndex + j);
                }

                // 计算中心点
                Vector3 center = Vector3.Lerp(m_vertices[0].position, m_vertices[2].position, 0.5f);

                // 预先计算角度和位置
                float angle = Mathf.PI * 0.5f - (center.x * m_spaceCoff * radiusReciprocal) + m_angleOffset * Mathf.Deg2Rad;
                float cosAngle = Mathf.Cos(angle);
                float sinAngle = Mathf.Sin(angle);

                // 计算目标位置和旋转
                Vector3 targetPosition = new Vector3(cosAngle, sinAngle, 0f) * m_radius;
                Quaternion rotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg - 90f);

                // 构建变换矩阵
                BuildTransformMatrix(center, targetPosition, rotation);

                // 应用变换并调整高度
                float verticalOffset = -m_radius + center.y;

                for (int j = 0; j < 4; j++)
                {
                    Vector3 transformedPosition = m_transformMatrix.MultiplyPoint(m_vertices[j].position);
                    transformedPosition.y += verticalOffset;
                    m_vertices[j].position = transformedPosition;
                    vh.SetUIVertex(m_vertices[j], vertexIndex + j);
                }
            }
        }

        /// <summary>
        /// 构建变换矩阵：place * rotate * move
        /// </summary>
        private void BuildTransformMatrix(Vector3 center, Vector3 targetPosition, Quaternion rotation)
        {
            // move: 平移到原点
            Matrix4x4 moveMatrix = Matrix4x4.Translate(center * -1f);

            // rotate: 旋转
            Matrix4x4 rotateMatrix = Matrix4x4.Rotate(rotation);

            // place: 平移到目标位置
            Matrix4x4 placeMatrix = Matrix4x4.Translate(targetPosition);

            // 组合变换矩阵
            m_transformMatrix = placeMatrix * rotateMatrix * moveMatrix;
        }
    }
}