#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif
using System;

namespace DGame.FixedPoint
{
    /// <summary>
    /// 使用定点数三角形数据表示的网格碰撞器。
    /// </summary>
    public class FPMeshCollider : FPCollider
    {
#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        /// <summary>相对于碰撞器中心、已应用旋转和缩放的顶点。</summary>
        internal FixedPointVector3[] vertices = Array.Empty<FixedPointVector3>();

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        /// <summary>网格三角形索引，每三个元素定义一个三角形。</summary>
        internal int[] triangles = Array.Empty<int>();

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        /// <summary>每个三角形在世界坐标中的单位法线。</summary>
        internal FixedPointVector3[] normals = Array.Empty<FixedPointVector3>();

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        /// <summary>每个三角形平面沿世界法线到世界原点的有符号距离。</summary>
        internal FixedPoint64[] distances = Array.Empty<FixedPoint64>();

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        /// <summary>每个三角形相对于碰撞器中心的最小边界。</summary>
        internal FixedPointVector3[] minimals = Array.Empty<FixedPointVector3>();

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        /// <summary>每个三角形相对于碰撞器中心的最大边界。</summary>
        internal FixedPointVector3[] maximals = Array.Empty<FixedPointVector3>();

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        private FixedPointVector3[] localVertices = Array.Empty<FixedPointVector3>();

        /// <summary>获取网格碰撞器类型。</summary>
        public override ColliderType colliderType => ColliderType.Mesh;

        /// <summary>
        /// 重建变换后的三角形数据并更新轴对齐包围盒。
        /// </summary>
        internal override void UpdateAABB()
        {
            EnsureLocalVertices();

            if (localVertices.Length == 0)
            {
                vertices = Array.Empty<FixedPointVector3>();
                triangles = Array.Empty<int>();
                normals = Array.Empty<FixedPointVector3>();
                distances = Array.Empty<FixedPoint64>();
                minimals = Array.Empty<FixedPointVector3>();
                maximals = Array.Empty<FixedPointVector3>();
                _min = position;
                _max = position;
                return;
            }

            if (vertices.Length != localVertices.Length)
            {
                vertices = new FixedPointVector3[localVertices.Length];
            }

            var scale = fpTransform.scale;
            var rotation = fpTransform.rotation;
            _min = FixedPointVector3.one * FixedPoint64.MaxValue;
            _max = FixedPointVector3.one * FixedPoint64.MinValue;

            for (var i = 0; i < localVertices.Length; i++)
            {
                var transformedVertex = rotation * FixedPointVector3.Scale(localVertices[i], scale);
                vertices[i] = transformedVertex;
                _min = FixedPointVector3.Min(_min, transformedVertex);
                _max = FixedPointVector3.Max(_max, transformedVertex);
            }

            RebuildTriangles();
            _min += position;
            _max += position;
        }

        /// <summary>
        /// 将碰撞器从当前八叉树节点的网格碰撞器集合中移除。
        /// </summary>
        protected override void RemoveFromImpactNotes()
        {
            targetNode?.FpMeshColliders.Remove(this);
            targetNode = null;
        }

        /// <summary>
        /// 将碰撞器加入指定八叉树节点的网格碰撞器集合。
        /// </summary>
        protected override void AddToImpactNote(FPOctreeNode node)
        {
            node.FpMeshColliders ??= new FPOctreeColliderSet<FPMeshCollider>(node);
            node.FpMeshColliders.Add(this);
            targetNode = node;
        }

        /// <summary>
        /// 根据三个顶点计算三角形所在平面的单位法线和世界距离。
        /// </summary>
        private static void FromTriangle(FixedPointVector3 point, FixedPointVector3 point1,
            FixedPointVector3 point2, FixedPointVector3 worldPosition, out FixedPointVector3 normal,
            out FixedPoint64 distance)
        {
            var cross = FixedPointVector3.Cross(point1 - point, point2 - point);

            if (cross.IsZero())
            {
                normal = FixedPointVector3.zero;
                distance = FixedPoint64.Zero;
                return;
            }

            normal = cross.normalized;
            distance = FixedPointVector3.Dot(normal, point + worldPosition);
        }

#if UNITY_2021_3_OR_NEWER
        /// <summary>
        /// 从当前对象或子对象的网格筛选器读取本地三角形数据。
        /// </summary>
        protected override void InitColliderSize()
        {
            var meshFilter = GetComponentInChildren<MeshFilter>(true);

            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                return;
            }

            var mesh = meshFilter.sharedMesh;

            // 将子网格顶点转换到碰撞器对象的本地坐标空间。
            localVertices = new FixedPointVector3[mesh.vertices.Length];

            for (var i = 0; i < localVertices.Length; i++)
            {
                var worldPoint = meshFilter.transform.TransformPoint(mesh.vertices[i]);
                localVertices[i] = new FixedPointVector3(transform.InverseTransformPoint(worldPoint));
            }

            triangles = mesh.triangles;
            vertices = new FixedPointVector3[localVertices.Length];
        }

        /// <summary>
        /// 在 Unity 场景视图中绘制网格碰撞器的三角形线框。
        /// </summary>
        protected override void OnDrawGizmosEditor()
        {
            UpdateAABB();

            for (var i = 0; i < triangles.Length; i += 3)
            {
                var point = vertices[triangles[i]] + position;
                var point1 = vertices[triangles[i + 1]] + position;
                var point2 = vertices[triangles[i + 2]] + position;
                Gizmos.DrawLine(point.ToVector3(), point1.ToVector3());
                Gizmos.DrawLine(point1.ToVector3(), point2.ToVector3());
                Gizmos.DrawLine(point2.ToVector3(), point.ToVector3());
            }
        }
#endif

        /// <summary>
        /// 确保三角形包围盒在每个坐标轴上具有最小厚度。
        /// </summary>
        private static void AdjustForMinimumThickness(ref FixedPointVector3 min, ref FixedPointVector3 max)
        {
            if (max.x - min.x < FixedPoint64.EN2)
            {
                min.x -= 0.01;
                max.x += 0.01;
            }

            if (max.y - min.y < FixedPoint64.EN2)
            {
                min.y -= 0.01;
                max.y += 0.01;
            }

            if (max.z - min.z < FixedPoint64.EN2)
            {
                min.z -= 0.01;
                max.z += 0.01;
            }
        }

        /// <summary>
        /// 兼容旧序列化数据，确保本地源顶点已初始化。
        /// </summary>
        private void EnsureLocalVertices()
        {
            if ((localVertices == null || localVertices.Length == 0) && vertices?.Length > 0)
            {
                localVertices = (FixedPointVector3[])vertices.Clone();
            }

            localVertices ??= Array.Empty<FixedPointVector3>();
            triangles ??= Array.Empty<int>();
        }

        /// <summary>
        /// 根据当前变换后的顶点重建三角形法线、平面距离和局部边界。
        /// </summary>
        private void RebuildTriangles()
        {
            var triangleCount = triangles.Length / 3;

            if (normals.Length != triangleCount || distances.Length != triangleCount ||
                minimals.Length != triangleCount || maximals.Length != triangleCount)
            {
                normals = new FixedPointVector3[triangleCount];
                distances = new FixedPoint64[triangleCount];
                minimals = new FixedPointVector3[triangleCount];
                maximals = new FixedPointVector3[triangleCount];
            }

            for (var i = 0; i < triangleCount; i++)
            {
                var triangleOffset = i * 3;
                var point = vertices[triangles[triangleOffset]];
                var point1 = vertices[triangles[triangleOffset + 1]];
                var point2 = vertices[triangles[triangleOffset + 2]];

                FromTriangle(point, point1, point2, position, out normals[i], out distances[i]);
                var triangleMin = FixedPointVector3.Min(point, FixedPointVector3.Min(point1, point2));
                var triangleMax = FixedPointVector3.Max(point, FixedPointVector3.Max(point1, point2));
                AdjustForMinimumThickness(ref triangleMin, ref triangleMax);
                minimals[i] = triangleMin;
                maximals[i] = triangleMax;
            }
        }
    }
}