#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif
using System;
using System.Collections.Generic;

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

        private const int BvhLeafTriangleCount = 4;
        private MeshBvhNode[] m_bvhNodes = Array.Empty<MeshBvhNode>();
        private int[] m_bvhTriangleIndices = Array.Empty<int>();
        private int m_bvhNodeCount;

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
                m_bvhNodeCount = 0;
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

            RebuildBvh(triangleCount);
        }

        /// <summary>收集世界 AABB 与查询范围重叠的三角形索引。</summary>
        internal void CollectTriangleCandidates(
            FixedPointVector3 queryMin,
            FixedPointVector3 queryMax,
            List<int> results)
        {
            results.Clear();
            if (m_bvhNodeCount == 0)
            {
                return;
            }

            var meshPosition = position;
            CollectBvhCandidates(0, queryMin - meshPosition, queryMax - meshPosition, results);
        }

        internal void GetWorldTriangle(int triangleIndex, out FixedPointVector3 a,
            out FixedPointVector3 b, out FixedPointVector3 c)
        {
            var offset = triangleIndex * 3;
            var meshPosition = position;
            a = vertices[triangles[offset]] + meshPosition;
            b = vertices[triangles[offset + 1]] + meshPosition;
            c = vertices[triangles[offset + 2]] + meshPosition;
        }

        private void RebuildBvh(int triangleCount)
        {
            if (triangleCount == 0)
            {
                m_bvhNodeCount = 0;
                return;
            }

            if (m_bvhTriangleIndices.Length != triangleCount)
            {
                m_bvhTriangleIndices = new int[triangleCount];
            }

            var requiredNodeCount = triangleCount * 2 - 1;
            if (m_bvhNodes.Length < requiredNodeCount)
            {
                m_bvhNodes = new MeshBvhNode[requiredNodeCount];
            }

            for (var i = 0; i < triangleCount; i++)
            {
                m_bvhTriangleIndices[i] = i;
            }

            m_bvhNodeCount = 0;
            BuildBvhNode(0, triangleCount);
        }

        private int BuildBvhNode(int start, int count)
        {
            var nodeIndex = m_bvhNodeCount++;
            var min = FixedPointVector3.one * FixedPoint64.MaxValue;
            var max = FixedPointVector3.one * FixedPoint64.MinValue;
            var centroidMin = min;
            var centroidMax = max;

            for (var i = start; i < start + count; i++)
            {
                var triangleIndex = m_bvhTriangleIndices[i];
                min = FixedPointVector3.Min(min, minimals[triangleIndex]);
                max = FixedPointVector3.Max(max, maximals[triangleIndex]);
                var centroid = (minimals[triangleIndex] + maximals[triangleIndex]) * FixedPoint64.Half;
                centroidMin = FixedPointVector3.Min(centroidMin, centroid);
                centroidMax = FixedPointVector3.Max(centroidMax, centroid);
            }

            if (count <= BvhLeafTriangleCount)
            {
                m_bvhNodes[nodeIndex] = new MeshBvhNode
                {
                    min = min,
                    max = max,
                    start = start,
                    count = count,
                    left = -1,
                    right = -1
                };
                return nodeIndex;
            }

            var centroidExtent = centroidMax - centroidMin;
            var splitAxis = centroidExtent.x >= centroidExtent.y && centroidExtent.x >= centroidExtent.z
                ? 0
                : centroidExtent.y >= centroidExtent.z ? 1 : 2;
            SortTriangleRangeByAxis(start, start + count - 1, splitAxis);
            var leftCount = count / 2;
            var left = BuildBvhNode(start, leftCount);
            var right = BuildBvhNode(start + leftCount, count - leftCount);
            m_bvhNodes[nodeIndex] = new MeshBvhNode
            {
                min = min,
                max = max,
                start = 0,
                count = 0,
                left = left,
                right = right
            };
            return nodeIndex;
        }

        private void SortTriangleRangeByAxis(int left, int right, int axis)
        {
            while (left < right)
            {
                var i = left;
                var j = right;
                var pivot = GetTriangleCentroidAxis(m_bvhTriangleIndices[(left + right) / 2], axis);
                while (i <= j)
                {
                    while (GetTriangleCentroidAxis(m_bvhTriangleIndices[i], axis) < pivot) i++;
                    while (GetTriangleCentroidAxis(m_bvhTriangleIndices[j], axis) > pivot) j--;
                    if (i > j) continue;
                    (m_bvhTriangleIndices[i], m_bvhTriangleIndices[j]) =
                        (m_bvhTriangleIndices[j], m_bvhTriangleIndices[i]);
                    i++;
                    j--;
                }

                if (j - left < right - i)
                {
                    if (left < j) SortTriangleRangeByAxis(left, j, axis);
                    left = i;
                }
                else
                {
                    if (i < right) SortTriangleRangeByAxis(i, right, axis);
                    right = j;
                }
            }
        }

        private FixedPoint64 GetTriangleCentroidAxis(int triangleIndex, int axis)
        {
            var centroid = (minimals[triangleIndex] + maximals[triangleIndex]) * FixedPoint64.Half;
            return axis == 0 ? centroid.x : axis == 1 ? centroid.y : centroid.z;
        }

        private void CollectBvhCandidates(
            int nodeIndex,
            FixedPointVector3 queryMin,
            FixedPointVector3 queryMax,
            List<int> results)
        {
            var node = m_bvhNodes[nodeIndex];
            if (!FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(
                    queryMin, queryMax, node.min, node.max))
            {
                return;
            }

            if (node.count > 0)
            {
                for (var i = node.start; i < node.start + node.count; i++)
                {
                    var triangleIndex = m_bvhTriangleIndices[i];
                    if (FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(
                            queryMin, queryMax, minimals[triangleIndex], maximals[triangleIndex]))
                    {
                        results.Add(triangleIndex);
                    }
                }

                return;
            }

            CollectBvhCandidates(node.left, queryMin, queryMax, results);
            CollectBvhCandidates(node.right, queryMin, queryMax, results);
        }

        private struct MeshBvhNode
        {
            internal FixedPointVector3 min;
            internal FixedPointVector3 max;
            internal int left;
            internal int right;
            internal int start;
            internal int count;
        }
    }
}
