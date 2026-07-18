using System.Collections.Generic;

namespace DGame.FixedPoint
{
    /// <summary>
    /// 定点数碰撞系统使用的八叉树空间索引。
    /// 碰撞器会被放入能够完整容纳其 AABB 的最深节点；跨越子节点边界的碰撞器保留在父节点。
    /// 查询时先通过节点 AABB 和碰撞器 AABB 进行粗检测，再调用对应形状的精确相交算法。
    /// </summary>
    /// <remarks>
    /// 该类型复用内部遍历列表和碰撞器查询标记以减少分配，不支持并发查询。
    /// </remarks>
    internal class FPOctree
    {
        /// <summary>
        /// 八叉树根节点。
        /// </summary>
        internal FPOctreeNode root;

        /// <summary>
        /// 已注册到当前八叉树并需要参与位置更新的全部碰撞器。
        /// </summary>
        internal readonly List<FPCollider> colliders = new ();

        /// <summary>
        /// 除根节点外的全部节点，用于统一重置节点状态。
        /// </summary>
        private readonly List<FPOctreeNode> nodes = new ();

        /// <summary>
        /// 查询期间复用的待访问节点列表。
        /// </summary>
        private readonly List<FPOctreeNode> openList = new ();
        private readonly List<FPCollider> m_colliderCandidates = new();
        private readonly List<int> m_meshTriangleCandidates = new();

        /// <summary>
        /// 当前查询编号，用于避免同一个碰撞器在一次查询中被重复处理。
        /// </summary>
        private int castIndex;

        /// <summary>
        /// 获取初始化八叉树时传入的逻辑尺寸。
        /// </summary>
        public int size { get;private set; }

        /// <summary>
        /// 当前查询在 <see cref="openList"/> 中即将访问的节点索引。
        /// </summary>
        private int openListIndex;

        /// <summary>
        /// 按指定逻辑尺寸创建完整的八叉树节点层级。
        /// </summary>
        /// <param name="size">八叉树逻辑尺寸；根节点的实际 AABB 半范围由二进制层级算法计算。</param>
        /// <returns>完成节点划分的八叉树实例。</returns>
        public static FPOctree Initialize(int size)
        {
            var fixedPointOctree = new FPOctree
            {
                size = size
            };
            var exp = (int)System.Math.Log(size, 2);
            var root = new FPOctreeNode((int)System.Math.Pow(2, exp - 1), new FixedPointVector3(0, 0, 0), size);
            var openList = new List<FPOctreeNode> { root };
            const int minHalfSize = 8;
            while (openList.Count > 0)
            {
                var node = openList[0];
                openList.RemoveAt(0);
                node.nodes = new FPOctreeNode[8];
                for (var i = 0; i < 8; i++)
                {
                    var x = i % 2 == 0 ? node.halfSize / 2 : -node.halfSize / 2;
                    var y = i / 2 % 2 == 0 ? node.halfSize / 2 : -node.halfSize / 2;
                    var z = i / 4 % 2 == 0 ? node.halfSize / 2 : -node.halfSize / 2;
                    var subNode = new FPOctreeNode( node.halfSize / 2, node.pos + new FixedPointVector3(x, y, z), node.size / 2)
                        {
                            parentNode = node
                        };
                    if (node.halfSize / 2 > minHalfSize)
                    {
                        openList.Add(subNode);
                    }
                    fixedPointOctree.nodes.Add(subNode);
                    node.nodes[i] = subNode;
                }
            }
            fixedPointOctree.root = root;
            return fixedPointOctree;
        }

        /// <summary>
        /// 判断指定坐标是否超出八叉树配置的逻辑边界。
        /// </summary>
        /// <param name="position">需要检查的坐标。</param>
        /// <returns>任一坐标轴超出 <c>[-size, size]</c> 时返回 <see langword="true"/>。</returns>
        public bool IsOutOfBound(FixedPointVector3 position)
        {
            return position.x > size || position.x < -size || position.y > size || position.y < -size || position.z > size || position.z < -size;
        }

        /// <summary>
        /// 清除全部已注册碰撞器、节点碰撞器集合和节点计数，保留已经构建的树结构以供复用。
        /// </summary>
        public void Reset()
        {
            colliders.Clear();
            ClearNodeColliders(root);
            foreach (var item in nodes)
            {
                ClearNodeColliders(item);
            }

            root.colliderCount = 0;
            foreach (var item in nodes)
            {
                item.colliderCount = 0;
            }
        }

        /// <summary>
        /// 清空指定节点直接持有的全部碰撞器集合。
        /// </summary>
        /// <param name="node">需要清空的节点。</param>
        private static void ClearNodeColliders(FPOctreeNode node)
        {
            if (node == null)
            {
                return;
            }

            node.FpSphereColliders?.Clear();
            node.FpAABBColliders?.Clear();
            node.FpObbColliders?.Clear();
            node.FpCapsuleColliders?.Clear();
            node.FpCylinderColliders?.Clear();
            node.FpAACapsuleColliders?.Clear();
            node.FpMeshColliders?.Clear();
            node.FpCharacterColliders?.Clear();
        }

        /// <summary>
        /// 初始化一次独立查询使用的节点遍历状态和去重编号。
        /// </summary>
        private void BeginQuery()
        {
            openList.Clear();
            openListIndex = 0;
            openList.Add(root);
            castIndex = castIndex == int.MaxValue ? int.MinValue : castIndex + 1;
        }

        internal int CollectCandidates(
            FixedPointVector3 queryMin,
            FixedPointVector3 queryMax,
            List<FPCollider> results,
            int layerMask,
            bool includeTrigger)
        {
            results.Clear();
            BeginQuery();
            while (openListIndex < openList.Count)
            {
                var node = openList[openListIndex++];
                AddCandidateSet(node.FpSphereColliders, queryMin, queryMax,
                    results, layerMask, includeTrigger);
                AddCandidateSet(node.FpAABBColliders, queryMin, queryMax,
                    results, layerMask, includeTrigger);
                AddCandidateSet(node.FpObbColliders, queryMin, queryMax,
                    results, layerMask, includeTrigger);
                AddCandidateSet(node.FpCapsuleColliders, queryMin, queryMax,
                    results, layerMask, includeTrigger);
                AddCandidateSet(node.FpCylinderColliders, queryMin, queryMax,
                    results, layerMask, includeTrigger);
                AddCandidateSet(node.FpAACapsuleColliders, queryMin, queryMax,
                    results, layerMask, includeTrigger);
                AddCandidateSet(node.FpMeshColliders, queryMin, queryMax,
                    results, layerMask, includeTrigger);
                AddCandidateSet(node.FpCharacterColliders, queryMin, queryMax,
                    results, layerMask, includeTrigger);

                if (node.nodes == null) continue;
                foreach (var child in node.nodes)
                {
                    if (child.colliderCount <= 0 ||
                        !FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(
                            queryMin, queryMax,
                            child.fixedPointAABB.Min, child.fixedPointAABB.Max))
                    {
                        continue;
                    }

                    openList.Add(child);
                }
            }

            return results.Count;
        }

        private void AddCandidateSet<T>(
            FPOctreeColliderSet<T> colliderSet,
            FixedPointVector3 queryMin,
            FixedPointVector3 queryMax,
            List<FPCollider> results,
            int layerMask,
            bool includeTrigger) where T : FPCollider
        {
            if (colliderSet == null) return;
            for (var i = 0; i < colliderSet.Count; i++)
            {
                var item = colliderSet[i];
                if (item == null ||
                    !FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(
                        queryMin, queryMax, item.min, item.max) ||
                    !CanQueryCollider(item, layerMask, includeTrigger))
                {
                    continue;
                }

                results.Add(item);
            }
        }

        /// <summary>
        /// 使用指定碰撞器的 AABB 通过八叉树收集候选，再执行当前已实现的精确形状相交。
        /// </summary>
        public int OverlayColliderCollision(
            FPCollider queryCollider,
            ref List<FPCollision> collisions,
            int layerMask = -1,
            bool includeTrigger = false)
        {
            collisions.Clear();
            if (queryCollider == null || !queryCollider.enabled)
            {
                return 0;
            }

            CollectCandidates(queryCollider.min, queryCollider.max,
                m_colliderCandidates, layerMask, includeTrigger);

            var count = 0;
            foreach (var target in m_colliderCandidates)
            {
                if (target == null || target == queryCollider ||
                    !TryIntersectColliders(queryCollider, target, out var collision) ||
                    !collision.hit)
                {
                    continue;
                }

                collision.collider = target;
                if (collisions.Count == count)
                {
                    collisions.Add(collision);
                }
                else
                {
                    collisions[count] = collision;
                }

                count++;
            }

            return count;
        }

        private bool TryIntersectColliders(
            FPCollider active,
            FPCollider target,
            out FPCollision collision)
        {
            if (TryGetSphere(active, out var sphereCenter, out var sphereRadius))
            {
                return TryIntersectSphereWithTarget(sphereCenter, sphereRadius, target, out collision);
            }

            if (TryGetSphere(target, out sphereCenter, out sphereRadius))
            {
                var supported = TryIntersectSphereWithTarget(sphereCenter, sphereRadius, active, out collision);
                FlipCollision(ref collision);
                return supported;
            }

            var activeIsCapsule = TryGetCapsule(active, out var activeStart, out var activeEnd,
                out var activeRadius);
            var targetIsCapsule = TryGetCapsule(target, out var targetStart, out var targetEnd,
                out var targetRadius);

            if (activeIsCapsule && targetIsCapsule)
            {
                collision = FixedPointIntersection.IntersectWithCapsuleAndCapsule(
                    activeStart, activeEnd, activeRadius, targetStart, targetEnd, targetRadius);
                return true;
            }

            if (activeIsCapsule && target is FPCylinderCollider targetCylinder)
            {
                collision = FixedPointIntersection.IntersectWithCapsuleAndCylinder(
                    activeStart, activeEnd, activeRadius,
                    targetCylinder.startPos, targetCylinder.endPos, targetCylinder.scaledRadius);
                return true;
            }

            if (active is FPCylinderCollider activeCylinder && targetIsCapsule)
            {
                collision = FixedPointIntersection.IntersectWithCapsuleAndCylinder(
                    targetStart, targetEnd, targetRadius,
                    activeCylinder.startPos, activeCylinder.endPos, activeCylinder.scaledRadius);
                FlipCollision(ref collision);
                return true;
            }

            if (active is FPCylinderCollider firstCylinder &&
                target is FPCylinderCollider secondCylinder)
            {
                collision = FixedPointIntersection.IntersectWithCylinderAndCylinder(
                    firstCylinder.startPos, firstCylinder.endPos, firstCylinder.scaledRadius,
                    secondCylinder.startPos, secondCylinder.endPos, secondCylinder.scaledRadius);
                return true;
            }

            if (active is FPCylinderCollider cylinderProbe && target is FPMeshCollider cylinderMesh)
            {
                collision = FixedPointIntersection.IntersectWithCylinderAndMesh(
                    cylinderProbe.startPos, cylinderProbe.endPos, cylinderProbe.scaledRadius,
                    cylinderMesh, m_meshTriangleCandidates);
                return true;
            }

            if (active is FPMeshCollider meshCylinder && target is FPCylinderCollider cylinderTarget)
            {
                collision = FixedPointIntersection.IntersectWithCylinderAndMesh(
                    cylinderTarget.startPos, cylinderTarget.endPos, cylinderTarget.scaledRadius,
                    meshCylinder, m_meshTriangleCandidates);
                FlipCollision(ref collision);
                return true;
            }

            if (activeIsCapsule && target is FPMeshCollider targetMesh)
            {
                collision = FixedPointIntersection.IntersectWithCapsuleAndMesh(
                    activeStart, activeEnd, activeRadius, targetMesh, m_meshTriangleCandidates);
                return true;
            }

            if (active is FPMeshCollider activeMesh && targetIsCapsule)
            {
                collision = FixedPointIntersection.IntersectWithCapsuleAndMesh(
                    targetStart, targetEnd, targetRadius, activeMesh, m_meshTriangleCandidates);
                FlipCollision(ref collision);
                return true;
            }

            if (active is FPBoxCollider activeObbMesh && target is FPMeshCollider targetObbMesh)
            {
                collision = FixedPointIntersection.IntersectWithOBBAndMesh(
                    activeObbMesh.position, activeObbMesh.halfSize,
                    activeObbMesh.fpTransform.fixedPointMatrix,
                    activeObbMesh.min, activeObbMesh.max,
                    targetObbMesh, m_meshTriangleCandidates);
                return true;
            }

            if (active is FPMeshCollider activeObbMeshTarget && target is FPBoxCollider targetObbMeshBox)
            {
                collision = FixedPointIntersection.IntersectWithOBBAndMesh(
                    targetObbMeshBox.position, targetObbMeshBox.halfSize,
                    targetObbMeshBox.fpTransform.fixedPointMatrix,
                    targetObbMeshBox.min, targetObbMeshBox.max,
                    activeObbMeshTarget, m_meshTriangleCandidates);
                FlipCollision(ref collision);
                return true;
            }

            if (active is FPAABBCollider activeAabbMesh && active is not FPBoxCollider &&
                target is FPMeshCollider targetAabbMesh)
            {
                collision = FixedPointIntersection.IntersectWithOBBAndMesh(
                    activeAabbMesh.position, activeAabbMesh.halfSize, FixedPointMatrix.Identity,
                    activeAabbMesh.min, activeAabbMesh.max,
                    targetAabbMesh, m_meshTriangleCandidates);
                return true;
            }

            if (active is FPMeshCollider activeAabbMeshTarget &&
                target is FPAABBCollider targetAabbMeshBox && target is not FPBoxCollider)
            {
                collision = FixedPointIntersection.IntersectWithOBBAndMesh(
                    targetAabbMeshBox.position, targetAabbMeshBox.halfSize, FixedPointMatrix.Identity,
                    targetAabbMeshBox.min, targetAabbMeshBox.max,
                    activeAabbMeshTarget, m_meshTriangleCandidates);
                FlipCollision(ref collision);
                return true;
            }

            if (activeIsCapsule && target is FPBoxCollider targetObb)
            {
                collision = FixedPointIntersection.IntersectWithAACapsuleAndOBB(
                    activeStart, activeEnd, activeRadius,
                    targetObb.position, targetObb.halfSize, targetObb.fpTransform.fixedPointMatrix,
                    targetObb.min, targetObb.max);
                return true;
            }

            if (active is FPBoxCollider activeObb && targetIsCapsule)
            {
                collision = FixedPointIntersection.IntersectWithAACapsuleAndOBB(
                    targetStart, targetEnd, targetRadius,
                    activeObb.position, activeObb.halfSize, activeObb.fpTransform.fixedPointMatrix,
                    activeObb.min, activeObb.max);
                FlipCollision(ref collision);
                return true;
            }

            if (activeIsCapsule && target is FPAABBCollider targetAabb && target is not FPBoxCollider)
            {
                collision = FixedPointIntersection.IntersectWithAACapsuleAndAABB(
                    activeStart, activeEnd, activeRadius, targetAabb.min, targetAabb.max);
                return true;
            }

            if (active is FPAABBCollider activeAabb && active is not FPBoxCollider && targetIsCapsule)
            {
                collision = FixedPointIntersection.IntersectWithAACapsuleAndAABB(
                    targetStart, targetEnd, targetRadius, activeAabb.min, activeAabb.max);
                FlipCollision(ref collision);
                return true;
            }

            if (active is FPAABBCollider activeBox && active is not FPBoxCollider &&
                target is FPAABBCollider targetBox && target is not FPBoxCollider)
            {
                collision = FixedPointIntersection.IntersectWithAABBAndAABBCollision(
                    activeBox.min, activeBox.max, targetBox.min, targetBox.max);
                return true;
            }

            if (active is FPAABBCollider cylinderAabb && active is not FPBoxCollider &&
                target is FPCylinderCollider aabbCylinder)
            {
                collision = FixedPointIntersection.IntersectWithAABBAndCylinder(
                    cylinderAabb.min, cylinderAabb.max,
                    aabbCylinder.startPos, aabbCylinder.endPos, aabbCylinder.scaledRadius);
                return true;
            }

            if (active is FPCylinderCollider cylinderAabbTarget &&
                target is FPAABBCollider aabbCylinderTarget && target is not FPBoxCollider)
            {
                collision = FixedPointIntersection.IntersectWithAABBAndCylinder(
                    aabbCylinderTarget.min, aabbCylinderTarget.max,
                    cylinderAabbTarget.startPos, cylinderAabbTarget.endPos,
                    cylinderAabbTarget.scaledRadius);
                FlipCollision(ref collision);
                return true;
            }

            if (active is FPBoxCollider cylinderObb && target is FPCylinderCollider obbCylinder)
            {
                collision = FixedPointIntersection.IntersectWithOBBAndCylinder(
                    cylinderObb.position, cylinderObb.halfSize,
                    cylinderObb.fpTransform.fixedPointMatrix,
                    obbCylinder.startPos, obbCylinder.endPos, obbCylinder.scaledRadius);
                return true;
            }

            if (active is FPCylinderCollider cylinderObbTarget &&
                target is FPBoxCollider obbCylinderTarget)
            {
                collision = FixedPointIntersection.IntersectWithOBBAndCylinder(
                    obbCylinderTarget.position, obbCylinderTarget.halfSize,
                    obbCylinderTarget.fpTransform.fixedPointMatrix,
                    cylinderObbTarget.startPos, cylinderObbTarget.endPos,
                    cylinderObbTarget.scaledRadius);
                FlipCollision(ref collision);
                return true;
            }

            if (active is FPAABBCollider axisAlignedBox && active is not FPBoxCollider &&
                target is FPBoxCollider orientedBox)
            {
                collision = FixedPointIntersection.IntersectWithAABBAndOBBCollision(
                    axisAlignedBox.min, axisAlignedBox.max,
                    orientedBox.position, orientedBox.halfSize,
                    orientedBox.fpTransform.fixedPointMatrix);
                return true;
            }

            if (active is FPBoxCollider orientedActive &&
                target is FPAABBCollider axisAlignedTarget && target is not FPBoxCollider)
            {
                collision = FixedPointIntersection.IntersectWithAABBAndOBBCollision(
                    axisAlignedTarget.min, axisAlignedTarget.max,
                    orientedActive.position, orientedActive.halfSize,
                    orientedActive.fpTransform.fixedPointMatrix);
                FlipCollision(ref collision);
                return true;
            }

            if (active is FPBoxCollider firstObb && target is FPBoxCollider secondObb)
            {
                collision = FixedPointIntersection.IntersectWithOBBAndOBBCollision(
                    firstObb.position, firstObb.halfSize, firstObb.fpTransform.fixedPointMatrix,
                    secondObb.position, secondObb.halfSize, secondObb.fpTransform.fixedPointMatrix);
                return true;
            }

            collision = default;
            return false;
        }

        private bool TryIntersectSphereWithTarget(
            FixedPointVector3 center,
            FixedPoint64 radius,
            FPCollider target,
            out FPCollision collision)
        {
            if (TryGetSphere(target, out var targetCenter, out var targetRadius))
            {
                collision = FixedPointIntersection.IntersectWithSphereAndSphere(
                    center, radius, targetCenter, targetRadius);
                return true;
            }

            if (target is FPBoxCollider obb)
            {
                collision = FixedPointIntersection.IntersectWithSphereAndOBB(
                    center, radius, obb.position, obb.halfSize, obb.fpTransform.fixedPointMatrix);
                return true;
            }

            if (target is FPAABBCollider aabb && target is not FPBoxCollider)
            {
                collision = FixedPointIntersection.IntersectWithSphereAndAABB(
                    center, radius, aabb.min, aabb.max);
                return true;
            }

            if (target is FPCylinderCollider cylinder)
            {
                collision = FixedPointIntersection.IntersectWithSphereAndCylinder(
                    center, radius, cylinder.startPos, cylinder.endPos, cylinder.scaledRadius);
                return true;
            }

            if (TryGetCapsule(target, out var start, out var end, out var capsuleRadius))
            {
                collision = FixedPointIntersection.IntersectWithSphereAndCapsule(
                    center, radius, start, end, capsuleRadius);
                return true;
            }

            if (target is FPMeshCollider mesh)
            {
                collision = FixedPointIntersection.IntersectWithSphereAndMesh(center, radius, mesh);
                return true;
            }

            collision = default;
            return false;
        }

        private static bool TryGetSphere(
            FPCollider collider,
            out FixedPointVector3 center,
            out FixedPoint64 radius)
        {
            if (collider is FPSphereCollider sphere)
            {
                center = sphere.position;
                radius = sphere.scaledRadius;
                return true;
            }

            if (collider is FPCharacterController character &&
                character.characterColliderType == CharacterCollider.Sphere)
            {
                center = character.position;
                radius = character.scaledRadius;
                return true;
            }

            center = default;
            radius = default;
            return false;
        }

        private static bool TryGetCapsule(
            FPCollider collider,
            out FixedPointVector3 start,
            out FixedPointVector3 end,
            out FixedPoint64 radius)
        {
            if (collider is FPCharacterController character &&
                character.characterColliderType == CharacterCollider.Sphere)
            {
                start = default;
                end = default;
                radius = default;
                return false;
            }

            if (collider is FPAACapsuleCollider capsule)
            {
                start = capsule.startPos;
                end = capsule.endPos;
                radius = capsule.scaledRadius;
                return true;
            }

            if (collider is FPCharacterController capsuleCharacter)
            {
                start = capsuleCharacter.startPos;
                end = capsuleCharacter.endPos;
                radius = capsuleCharacter.scaledRadius;
                return true;
            }

            start = default;
            end = default;
            radius = default;
            return false;
        }

        private static void FlipCollision(ref FPCollision collision)
        {
            collision.normal = -collision.normal;
            (collision.closestPoint, collision.outsidePoint) =
                (collision.outsidePoint, collision.closestPoint);
        }

        /// <summary>
        /// 注册需要由八叉树更新和查询的碰撞器。
        /// </summary>
        /// <param name="fpCollider">需要注册的碰撞器。</param>
        public void AddCollider(FPCollider fpCollider)
        {
            if (fpCollider == null || colliders.Contains(fpCollider))
            {
                return;
            }

            colliders.Add(fpCollider);
        }

        /// <summary>
        /// 注销碰撞器，并将其从当前所属节点及碰撞器更新列表中移除。
        /// </summary>
        /// <param name="fpCollider">需要注销的碰撞器。</param>
        /// <returns>碰撞器原本已注册时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
        public bool RemoveCollider(FPCollider fpCollider)
        {
            if (fpCollider == null)
            {
                return false;
            }

            var removed = colliders.Remove(fpCollider);
            fpCollider.DetachFromOctree();
            return removed;
        }

        /// <summary>
        /// 更新所有位置或形状已发生变化的碰撞器，并重新计算其节点归属。
        /// </summary>
        public void UpdateColliders()
        {
            openList.Clear();
            openListIndex = 0;
            foreach (var item in colliders)
            {
                if (!item.fpTransform.colliderUpdateFlag) continue;
                item.UpdateCollider();
                item.fpTransform.colliderUpdateFlag = false;
            }
        }
        /// <summary>
        /// 检测指定有向包围盒与树中球形碰撞器的碰撞。
        /// </summary>
        /// <param name="position">查询盒中心坐标。</param>
        /// <param name="halfSize">查询盒在本地坐标轴上的半尺寸。</param>
        /// <param name="orientation">查询盒的旋转矩阵。</param>
        /// <param name="layerMask">参与查询的层掩码；<c>-1</c> 或 <c>0</c> 表示不限制层。</param>
        /// <param name="includeTrigger">是否包含触发器。</param>
        /// <returns>与查询盒发生碰撞的结果列表。</returns>
        /// <remarks>当前实现处理球、胶囊体、轴对齐胶囊体和角色控制器。</remarks>
        public List<FPCollision> OverlayBoxCollision(FixedPointVector3 position,
            FixedPointVector3 halfSize, FixedPointMatrix orientation,
            int layerMask = -1, bool includeTrigger = false)
        {
            var collisions = new List<FPCollision>();
            OverlayBoxCollision(position, halfSize, orientation, ref collisions,
                layerMask, includeTrigger);
            return collisions;
        }

        public int OverlayBoxCollision(FixedPointVector3 position,
            FixedPointVector3 halfSize, FixedPointMatrix orientation,
            ref List<FPCollision> collisions,
            int layerMask = -1, bool includeTrigger = false)
        {
            collisions.Clear();
            BeginQuery();
            while (openList.Count > openListIndex)
            {
                var node = openList[openListIndex];
                openListIndex++;
                if (node.FpAABBColliders != null)
                {
                    for (var i = 0; i < node.FpAABBColliders.Count; i++)
                    {
                        var item = node.FpAABBColliders[i];
                        if (!CanQueryCollider(item, layerMask, includeTrigger)) continue;
                        var collision = FixedPointIntersection.IntersectWithAABBAndOBBCollision(
                            item.min, item.max, position, halfSize, orientation);
                        AddBoxCollision(item, ref collision, collisions);
                    }
                }
                if (node.FpObbColliders != null)
                {
                    for (var i = 0; i < node.FpObbColliders.Count; i++)
                    {
                        var item = node.FpObbColliders[i];
                        if (!CanQueryCollider(item, layerMask, includeTrigger)) continue;
                        var collision = FixedPointIntersection.IntersectWithOBBAndOBBCollision(
                            item.position, item.halfSize, item.fpTransform.fixedPointMatrix,
                            position, halfSize, orientation);
                        AddBoxCollision(item, ref collision, collisions);
                    }
                }
                if (node.FpSphereColliders != null)
                {
                    for (var i = 0; i < node.FpSphereColliders.Count; i++)
                    {
                        var item = node.FpSphereColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        var fixedPointCollision = FixedPointIntersection.IntersectWithSphereAndOBB(item.position, item.scaledRadius, position, halfSize, orientation);
                        if (!fixedPointCollision.hit) continue;
                        fixedPointCollision.collider = item;
                        fixedPointCollision.normal = -fixedPointCollision.normal;
                        collisions.Add(fixedPointCollision);
                    }
                }
                if (node.FpCapsuleColliders != null)
                {
                    for (var i = 0; i < node.FpCapsuleColliders.Count; i++)
                    {
                        var item = node.FpCapsuleColliders[i];
                        if (!CanQueryCollider(item, layerMask, includeTrigger)) continue;
                        var collision = FixedPointIntersection.IntersectWithAACapsuleAndOBB(
                            item.startPos, item.endPos, item.scaledRadius,
                            position, halfSize, orientation, item.min, item.max);
                        AddBoxCollision(item, ref collision, collisions);
                    }
                }
                if (node.FpAACapsuleColliders != null)
                {
                    for (var i = 0; i < node.FpAACapsuleColliders.Count; i++)
                    {
                        var item = node.FpAACapsuleColliders[i];
                        if (!CanQueryCollider(item, layerMask, includeTrigger)) continue;
                        var collision = FixedPointIntersection.IntersectWithAACapsuleAndOBB(
                            item.startPos, item.endPos, item.scaledRadius,
                            position, halfSize, orientation, item.min, item.max);
                        AddBoxCollision(item, ref collision, collisions);
                    }
                }
                if (node.FpCharacterColliders != null)
                {
                    for (var i = 0; i < node.FpCharacterColliders.Count; i++)
                    {
                        var item = node.FpCharacterColliders[i];
                        if (!CanQueryCollider(item, layerMask, includeTrigger)) continue;
                        var collision = item.characterColliderType == CharacterCollider.Sphere
                            ? FixedPointIntersection.IntersectWithSphereAndOBB(
                                item.position, item.scaledRadius, position, halfSize, orientation)
                            : FixedPointIntersection.IntersectWithAACapsuleAndOBB(
                                item.startPos, item.endPos, item.scaledRadius,
                                position, halfSize, orientation, item.min, item.max);
                        AddBoxCollision(item, ref collision, collisions);
                    }
                }
                if (node.FpCylinderColliders != null)
                {
                    for (var i = 0; i < node.FpCylinderColliders.Count; i++)
                    {
                        var item = node.FpCylinderColliders[i];
                        if (!CanQueryCollider(item, layerMask, includeTrigger)) continue;
                        var collision = FixedPointIntersection.IntersectWithOBBAndCylinder(
                            position, halfSize, orientation,
                            item.startPos, item.endPos, item.scaledRadius);
                        AddDirectBoxCollision(item, ref collision, collisions);
                    }
                }
                if (node.FpMeshColliders != null)
                {
                    var axisX = new FixedPointVector3(
                        orientation.M11, orientation.M12, orientation.M13);
                    var axisY = new FixedPointVector3(
                        orientation.M21, orientation.M22, orientation.M23);
                    var axisZ = new FixedPointVector3(
                        orientation.M31, orientation.M32, orientation.M33);
                    var queryExtent = FixedPointVector3.Abs(axisX) * halfSize.x +
                                      FixedPointVector3.Abs(axisY) * halfSize.y +
                                      FixedPointVector3.Abs(axisZ) * halfSize.z;
                    var queryMin = position - queryExtent;
                    var queryMax = position + queryExtent;
                    for (var i = 0; i < node.FpMeshColliders.Count; i++)
                    {
                        var item = node.FpMeshColliders[i];
                        if (!CanQueryCollider(item, layerMask, includeTrigger)) continue;
                        var collision = FixedPointIntersection.IntersectWithOBBAndMesh(
                            position, halfSize, orientation, queryMin, queryMax,
                            item, m_meshTriangleCandidates);
                        AddDirectBoxCollision(item, ref collision, collisions);
                    }
                }

                if (node.nodes == null) continue;
                {
                    foreach (var item in node.nodes)
                    {
                        if(item.colliderCount <= 0) continue;
                        if (FixedPointIntersection.IntersectWithAABBAndOBBFixedPoint(item.fixedPointAABB.Min, item.fixedPointAABB.Max, position, halfSize, orientation))
                        {
                            openList.Add(item);
                            //Debug.Log(node.nodes[i]);
                        }
                    }
                }
            }
            return collisions.Count;
        }

        private bool CanQueryCollider(FPCollider item, int layerMask, bool includeTrigger)
        {
            if (item == null || !item.enabled || item.isTrigger && !includeTrigger ||
                layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer) ||
                item.castIndex == castIndex)
            {
                return false;
            }

            item.castIndex = castIndex;
            return true;
        }

        private static void AddBoxCollision(FPCollider collider, ref FPCollision collision,
            List<FPCollision> collisions)
        {
            if (!collision.hit) return;
            collision.collider = collider;
            collision.normal = -collision.normal;
            (collision.closestPoint, collision.outsidePoint) =
                (collision.outsidePoint, collision.closestPoint);
            collisions.Add(collision);
        }

        private static void AddDirectBoxCollision(FPCollider collider, ref FPCollision collision,
            List<FPCollision> collisions)
        {
            if (!collision.hit) return;
            collision.collider = collider;
            collisions.Add(collision);
        }

        /// <summary>
        /// 检测指定球体与树中碰撞器的碰撞，并将结果写入可复用列表。
        /// </summary>
        /// <param name="position">查询球体中心坐标。</param>
        /// <param name="radius">查询球体半径。</param>
        /// <param name="collisions">接收碰撞结果的可复用列表；有效结果位于索引 <c>[0, 返回值)</c>。</param>
        /// <param name="layerMask">参与查询的层掩码；<c>-1</c> 或 <c>0</c> 表示不限制层。</param>
        /// <param name="includeTrigger">是否包含触发器。</param>
        /// <param name="dynamic">需要匹配的碰撞器动态标记。</param>
        /// <returns>写入的有效碰撞结果数量。</returns>
        /// <remarks>当前实现处理球、AABB、OBB、胶囊体、圆柱体、轴对齐胶囊体和角色控制器。</remarks>
        public int OverlaySphereCollision(FixedPointVector3 position, FixedPoint64 radius,ref List<FPCollision> collisions, int layerMask = -1, bool includeTrigger = false,bool dynamic= false)
        {
            var count = 0;
            BeginQuery();
            while (openList.Count > openListIndex)
            {
                var node = openList[openListIndex];
                openListIndex++;
                FPCollision fpCollision;
                if (node.FpSphereColliders != null)
                {
                    for (var i = 0; i < node.FpSphereColliders.Count; i++)
                    {
                        var item = node.FpSphereColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (item.isDynamic != dynamic)
                        {
                            continue;
                        }
                        if (layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        fpCollision = FixedPointIntersection.IntersectWithSphereAndSphere(position, radius, item.position, item.scaledRadius);
                        if (fpCollision.hit)
                        {
                            fpCollision.collider = item;
                            if (collisions.Count == count)
                            {
                                collisions.Add(fpCollision);
                            }
                            else
                            {
                                collisions[count] = fpCollision;
                            }
                            count++;
                        }
                    }
                }
                if (node.FpAABBColliders != null)
                {
                    for (var i = 0; i < node.FpAABBColliders.Count; i++)
                    {
                        var item = node.FpAABBColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (item.isDynamic != dynamic)
                        {
                            continue;
                        }
                        if (layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        fpCollision = FixedPointIntersection.IntersectWithSphereAndAABB(position, radius, item.min, item.max);
                        if (fpCollision.hit)
                        {
                            fpCollision.collider = item;
                            if (collisions.Count == count)
                            {
                                collisions.Add(fpCollision);
                            }
                            else
                            {
                                collisions[count] = fpCollision;
                            }
                            count++;
                        }
                    }
                }
                if (node.FpObbColliders != null)
                {
                    for (var i = 0; i < node.FpObbColliders.Count; i++)
                    {
                        var item = node.FpObbColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (item.isDynamic != dynamic)
                        {
                            continue;
                        }
                        if (!GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        fpCollision = FixedPointIntersection.IntersectWithSphereAndOBB(position, radius, item.position, item.halfSize, item.fpTransform.fixedPointMatrix);
                        if (fpCollision.hit)
                        {
                            fpCollision.collider = item;
                            if (collisions.Count == count)
                            {
                                collisions.Add(fpCollision);
                            }
                            else
                            {
                                collisions[count] = fpCollision;
                            }
                            count++;
                        }
                    }
                }
                if (node.FpCapsuleColliders != null)
                {
                    for (var i = 0; i < node.FpCapsuleColliders.Count; i++)
                    {
                        var item = node.FpCapsuleColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (item.isDynamic != dynamic)
                        {
                            continue;
                        }
                        if (!GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        fpCollision = FixedPointIntersection.IntersectWithSphereAndCapsule(position, radius, item.startPos, item.endPos, item.scaledRadius);
                        if (fpCollision.hit)
                        {
                            fpCollision.collider = item;
                            if (collisions.Count == count)
                            {
                                collisions.Add(fpCollision);
                            }
                            else
                            {
                                collisions[count] = fpCollision;
                            }
                            count++;
                        }
                    }
                }
                if (node.FpCylinderColliders != null)
                {
                    for (var i = 0; i < node.FpCylinderColliders.Count; i++)
                    {
                        var item = node.FpCylinderColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (item.isDynamic != dynamic)
                        {
                            continue;
                        }
                        if (!GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        fpCollision = FixedPointIntersection.IntersectWithSphereAndCylinder(position, radius, item.startPos, item.endPos, item.scaledRadius);
                        if (fpCollision.hit)
                        {
                            fpCollision.collider = item;
                            if (collisions.Count == count)
                            {
                                collisions.Add(fpCollision);
                            }
                            else
                            {
                                collisions[count] = fpCollision;
                            }
                            count++;
                        }
                    }
                }
                if (node.FpAACapsuleColliders != null)
                {
                    for (var i = 0; i < node.FpAACapsuleColliders.Count; i++)
                    {
                        var item = node.FpAACapsuleColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (item.isDynamic != dynamic)
                        {
                            continue;
                        }
                        if (!GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        fpCollision = FixedPointIntersection.IntersectWithSphereAndAACapsule(position, radius, item.startPos, item.endPos, item.scaledRadius);
                        if (fpCollision.hit)
                        {
                            fpCollision.collider = item;
                            if (collisions.Count == count)
                            {
                                collisions.Add(fpCollision);
                            }
                            else
                            {
                                collisions[count] = fpCollision;
                            }
                            count++;
                        }
                    }
                }
                if (node.FpCharacterColliders != null)
                {
                    for (var i = 0; i < node.FpCharacterColliders.Count; i++)
                    {
                        var item = node.FpCharacterColliders[i];
                        if (item == null || !item.enabled || item.isTrigger && !includeTrigger ||
                            item.isDynamic != dynamic ||
                            layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer) ||
                            item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        fpCollision = item.characterColliderType == CharacterCollider.Sphere
                            ? FixedPointIntersection.IntersectWithSphereAndSphere(
                                position, radius, item.position, item.scaledRadius)
                            : FixedPointIntersection.IntersectWithSphereAndAACapsule(
                                position, radius, item.startPos, item.endPos, item.scaledRadius);
                        if (fpCollision.hit)
                        {
                            fpCollision.collider = item;
                            if (collisions.Count == count)
                            {
                                collisions.Add(fpCollision);
                            }
                            else
                            {
                                collisions[count] = fpCollision;
                            }
                            count++;
                        }
                    }
                }
                
                if (node.nodes != null)
                {
                    foreach (var item in node.nodes)
                    {
                        if(item.colliderCount <= 0) continue;
                        if (FixedPointIntersection.IntersectWithAABBAndSphere(item.fixedPointAABB.Min, item.fixedPointAABB.Max, position, radius))
                        {
                            openList.Add(item);
                        }
                    }
                }
            }
            return count;
        }
        /// <summary>
        /// 统计指定轴对齐包围盒与树中碰撞器发生重叠的数量。
        /// </summary>
        /// <param name="min">查询包围盒的最小坐标。</param>
        /// <param name="max">查询包围盒的最大坐标。</param>
        /// <param name="layerMask">参与查询的层掩码；<c>-1</c> 或 <c>0</c> 表示不限制层。</param>
        /// <param name="includeTrigger">是否包含触发器。</param>
        /// <returns>发生重叠的碰撞器数量。</returns>
        /// <remarks>当前实现处理球、AABB 和 OBB 碰撞器。</remarks>
        public int OverlayAABBCollisionCount(FixedPointVector3 min, FixedPointVector3 max, int layerMask = -1, bool includeTrigger = false) 
        {
            var count = 0;
            BeginQuery();
            while (openList.Count > openListIndex)
            {
                var node = openList[openListIndex];
                openListIndex++;
                if (node.FpSphereColliders != null)
                {
                    for (var i = 0; i < node.FpSphereColliders.Count; i++)
                    {
                        var item = node.FpSphereColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        if (FixedPointIntersection.IntersectWithAABBAndSphere(min, max, item.position, item.scaledRadius))
                        {
                            count++;
                        }
                    }
                }
                if (node.FpAABBColliders != null)
                {
                    for (var i = 0; i < node.FpAABBColliders.Count; i++)
                    {
                        var item = node.FpAABBColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        if (FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(item.min, item.max, min, max))
                        {
                            count++;
                        }
                    }
                }
                if (node.FpObbColliders != null)
                {
                    for (var i = 0; i < node.FpObbColliders.Count; i++)
                    {
                        var item = node.FpObbColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        if (FixedPointIntersection.IntersectWithAABBAndOBBFixedPoint(min, max, item))
                        {
                            count++;
                        }
                    }
                }
                if (node.nodes != null)
                {
                    foreach (var item in node.nodes)
                    {
                        if(item.colliderCount <= 0) continue;
                        if (FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(item.fixedPointAABB.Min, item.fixedPointAABB.Max, min, max))
                        {
                            openList.Add(item);
                        }
                    }
                }
            }
            return count;
        }
        /// <summary>
        /// 查找与指定球体重叠的碰撞器，并将结果写入可复用列表。
        /// </summary>
        /// <param name="position">查询球体中心坐标。</param>
        /// <param name="radius">查询球体半径。</param>
        /// <param name="colliders">接收碰撞器的可复用列表；有效结果位于索引 <c>[0, 返回值)</c>。</param>
        /// <param name="layerMask">参与查询的层掩码；<c>-1</c> 或 <c>0</c> 表示不限制层。</param>
        /// <param name="includeTrigger">是否包含触发器。</param>
        /// <returns>写入的有效碰撞器数量。</returns>
        /// <remarks>当前实现处理球、AABB、OBB、胶囊体、圆柱体、轴对齐胶囊体和网格。</remarks>
        public int OverlapSphere(FixedPointVector3 position, FixedPoint64 radius,ref List<FPCollider> colliders ,int layerMask = -1, bool includeTrigger = false)
        {
            int count = 0;
            BeginQuery();
            var min = position - new FixedPointVector3(radius, radius, radius);
            var max = position + new FixedPointVector3(radius, radius, radius);
            while (openList.Count > openListIndex)
            {
                var node = openList[openListIndex];
                openListIndex++;
                if (node.FpSphereColliders != null)
                {
                    for (var i = 0; i < node.FpSphereColliders.Count; i++)
                    {
                        var item = node.FpSphereColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        if ((radius + item.scaledRadius) * (radius + item.scaledRadius) > (item.position - position).sqrMagnitude)
                        {
                            if (colliders.Count == count)
                            {
                                colliders.Add(item);
                            }
                            else
                            {
                                colliders[count] = item;
                            }
                            count++;
                        }
                    }
                }
                if (node.FpAABBColliders != null)
                {
                    for (var i = 0; i < node.FpAABBColliders.Count; i++)
                    {
                        var item = node.FpAABBColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        if (FixedPointIntersection.IntersectWithAABBAndSphere(item.min, item.max, position, radius))
                        {
                            if (colliders.Count == count)
                            {
                                colliders.Add(item);
                            }
                            else
                            {
                                colliders[count] = item;
                            }
                            count++;
                        }
                    }
                }
                FPCollision fpCollision;
                if (node.FpObbColliders != null)
                {
                    for (var i = 0; i < node.FpObbColliders.Count; i++)
                    {
                        var item = node.FpObbColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (!GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        fpCollision = FixedPointIntersection.IntersectWithSphereAndOBB(position, radius, item.position, item.halfSize, item.fpTransform.fixedPointMatrix);
                        if (fpCollision.hit)
                        {
                            if (colliders.Count == count)
                            {
                                colliders.Add(item);
                            }
                            else
                            {
                                colliders[count] = item;
                            }
                            count++;
                        }
                    }
                }
                if (node.FpCapsuleColliders != null)
                {
                    for (var i = 0; i < node.FpCapsuleColliders.Count; i++)
                    {
                        var item = node.FpCapsuleColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (!GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        fpCollision = FixedPointIntersection.IntersectWithSphereAndCapsule(position, radius, item.startPos, item.endPos, item.scaledRadius);
                        if (fpCollision.hit)
                        {
                            if (colliders.Count == count)
                            {
                                colliders.Add(item);
                            }
                            else
                            {
                                colliders[count] = item;
                            }
                            count++;
                        }
                    }
                }
                if (node.FpCylinderColliders != null)
                {
                    for (var i = 0; i < node.FpCylinderColliders.Count; i++)
                    {
                        var item = node.FpCylinderColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (!GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        fpCollision = FixedPointIntersection.IntersectWithSphereAndCylinder(position, radius, item.startPos, item.endPos, item.scaledRadius);
                        if (fpCollision.hit)
                        {
                            if (colliders.Count == count)
                            {
                                colliders.Add(item);
                            }
                            else
                            {
                                colliders[count] = item;
                            }
                            count++;
                        }
                    }
                }
                if (node.FpAACapsuleColliders != null)
                {
                    for (var i = 0; i < node.FpAACapsuleColliders.Count; i++)
                    {
                        var item = node.FpAACapsuleColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (!GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        fpCollision = FixedPointIntersection.IntersectWithSphereAndAACapsule(position, radius, item.startPos, item.endPos, item.scaledRadius);
                        if (fpCollision.hit)
                        {
                            if (colliders.Count == count)
                            {
                                colliders.Add(item);
                            }
                            else
                            {
                                colliders[count] = item;
                            }
                            count++;
                        }
                    }
                }
                if (node.nodes != null)
                {
                    foreach (var item in node.nodes)
                    {
                        if(item.colliderCount <= 0) continue;
                        if (FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(item.fixedPointAABB.Min, item.fixedPointAABB.Max, min, max))
                        {
                            openList.Add(item);
                        }
                    }
                }
            }
            return count;
        }
        /// <summary>
        /// 检测指定轴对齐胶囊体与树中碰撞器的碰撞，并将结果写入可复用列表。
        /// </summary>
        /// <param name="startPos">查询胶囊体中心线起点。</param>
        /// <param name="endPos">查询胶囊体中心线终点。</param>
        /// <param name="radius">查询胶囊体半径。</param>
        /// <param name="collisions">接收碰撞结果的可复用列表；有效结果位于索引 <c>[0, 返回值)</c>。</param>
        /// <param name="layerMask">参与查询的层掩码；<c>-1</c> 或 <c>0</c> 表示不限制层。</param>
        /// <param name="includeTrigger">是否包含触发器。</param>
        /// <param name="dynamic">需要匹配的碰撞器动态标记。</param>
        /// <returns>写入的有效碰撞结果数量。</returns>
        /// <remarks>当前实现处理球、AABB、OBB、胶囊体、圆柱体和轴对齐胶囊体。</remarks>
        public int OverlayAACapsuleCollision(FixedPointVector3 startPos, FixedPointVector3 endPos, FixedPoint64 radius,ref List<FPCollision> collisions, int layerMask = -1, bool includeTrigger = false,bool dynamic= false)
        {
            var count = 0;
            BeginQuery();
            var min = startPos - new FixedPointVector3(radius,radius,radius);
            var max = endPos + new FixedPointVector3(radius, radius, radius);
            while (openList.Count > openListIndex)
            {
                var node = openList[openListIndex];
                openListIndex++;
                FPCollision fpCollision;
                if (node.FpSphereColliders != null)
                {
                    for (var i = 0; i < node.FpSphereColliders.Count; i++)
                    {
                        var item = node.FpSphereColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (item.isDynamic != dynamic)
                        {
                            continue;
                        }
                        if (layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        fpCollision = FixedPointIntersection.IntersectWithAACapsuleAndSphere(startPos, endPos, radius, item.position, item.scaledRadius);
                        if (!fpCollision.hit) continue;
                        fpCollision.collider = item;
                        if (collisions.Count == count)
                        {
                            collisions.Add(fpCollision);
                        }
                        else
                        {
                            collisions[count] = fpCollision;
                        }
                        count++;
                    }
                }
                if (node.FpAABBColliders != null)
                {
                    for (var i = 0; i < node.FpAABBColliders.Count; i++)
                    {
                        var item = node.FpAABBColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (item.isDynamic != dynamic)
                        {
                            continue;
                        }
                        if (layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        if (!FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(min, max, item.min, item.max))
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        fpCollision = FixedPointIntersection.IntersectWithAACapsuleAndAABB(startPos, endPos, radius, item.min, item.max);
                        if (!fpCollision.hit) continue;
                        fpCollision.collider = item;
                        if (collisions.Count == count)
                        {
                            collisions.Add(fpCollision);
                        }
                        else
                        {
                            collisions[count] = fpCollision;
                        }
                        count++;
                    }
                }
                if (node.FpObbColliders != null)
                {
                    for (var i = 0; i < node.FpObbColliders.Count; i++)
                    {
                        var item = node.FpObbColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (item.isDynamic != dynamic)
                        {
                            continue;
                        }
                        if (!GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        if (!FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(min, max, item.min, item.max))
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        var matrix = FixedPointMatrix.CreateFromQuaternion(item.fpTransform.rotation);
                        fpCollision = FixedPointIntersection.IntersectWithAACapsuleAndOBB(startPos, endPos, radius, item.position, item.halfSize, matrix, item.min, item.max);
                        //fixedPointCollision = FixedPointIntersection.IntersectWithSphereAndOBB(startPos,  radius, item.position, item.halfSize, matrix);
                        if (!fpCollision.hit) continue;
                        fpCollision.collider = item;
                        if (collisions.Count == count)
                        {
                            collisions.Add(fpCollision);
                        }
                        else
                        {
                            collisions[count] = fpCollision;
                        }
                        count++;
                    }
                }
                if (node.FpCapsuleColliders != null)
                {
                    for (var i = 0; i < node.FpCapsuleColliders.Count; i++)
                    {
                        var item = node.FpCapsuleColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (item.isDynamic != dynamic)
                        {
                            continue;
                        }
                        if (!GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        if (!FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(min, max, item.min, item.max))
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        fpCollision = FixedPointIntersection.IntersectWithCapsuleAndCapsule(startPos, endPos, radius, item.startPos, item.endPos, item.scaledRadius);
                        if (!fpCollision.hit) continue;
                        fpCollision.collider = item;
                        if (collisions.Count == count)
                        {
                            collisions.Add(fpCollision);
                        }
                        else
                        {
                            collisions[count] = fpCollision;
                        }
                        count++;
                    }
                }
                if (node.FpCylinderColliders != null)
                {
                    for (var i = 0; i < node.FpCylinderColliders.Count; i++)
                    {
                        var item = node.FpCylinderColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (item.isDynamic != dynamic)
                        {
                            continue;
                        }
                        if (!GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        if (!FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(min, max, item.min, item.max))
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        fpCollision = FixedPointIntersection.IntersectWithCapsuleAndCylinder(startPos, endPos, radius, item.startPos, item.endPos, item.scaledRadius);
                        if (!fpCollision.hit) continue;
                        fpCollision.collider = item;
                        if (collisions.Count == count)
                        {
                            collisions.Add(fpCollision);
                        }
                        else
                        {
                            collisions[count] = fpCollision;
                        }
                        count++;
                    }
                }
                if (node.FpAACapsuleColliders != null)
                {
                    for (var i = 0; i < node.FpAACapsuleColliders.Count; i++)
                    {
                        var item = node.FpAACapsuleColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (item.isDynamic != dynamic)
                        {
                            continue;
                        }
                        if (!GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        if (!FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(min, max, item.min, item.max))
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        fpCollision = FixedPointIntersection.IntersectWithAACapsuleAndAACapsule(startPos, endPos, radius, item.startPos, item.endPos, item.scaledRadius);
                        if (!fpCollision.hit) continue;
                        fpCollision.collider = item;
                        if (collisions.Count == count)
                        {
                            collisions.Add(fpCollision);
                        }
                        else
                        {
                            collisions[count] = fpCollision;
                        }
                        count++;
                    }
                }
                if (node.FpMeshColliders != null)
                {
                    for (var i = 0; i < node.FpMeshColliders.Count; i++)
                    {
                        var item = node.FpMeshColliders[i];
                        if (item == null || !item.enabled || item.isTrigger && !includeTrigger ||
                            item.isDynamic != dynamic ||
                            layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer) ||
                            item.castIndex == castIndex ||
                            !FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(
                                min, max, item.min, item.max))
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        fpCollision = FixedPointIntersection.IntersectWithCapsuleAndMesh(
                            startPos, endPos, radius, item, m_meshTriangleCandidates);
                        if (!fpCollision.hit) continue;
                        fpCollision.collider = item;
                        if (collisions.Count == count) collisions.Add(fpCollision);
                        else collisions[count] = fpCollision;
                        count++;
                    }
                }
                if (node.nodes != null)
                {
                    foreach (var item in node.nodes)
                    {
                        //if(item.subColliderCount <= 0) continue;
                        if (FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(item.fixedPointAABB.Min, item.fixedPointAABB.Max, min, max))
                        {
                            openList.Add(item);
                        }
                    }
                }
            }
            return count;
        }
        /// <summary>
        /// 使用调用方提供的数组查找与指定球体重叠的碰撞器，不创建结果集合。
        /// </summary>
        /// <param name="colliders">接收结果的数组；结果数量不会超过数组长度。</param>
        /// <param name="center">查询球体中心坐标。</param>
        /// <param name="radius">查询球体半径。</param>
        /// <param name="layerMask">参与查询的层掩码；<c>-1</c> 或 <c>0</c> 表示不限制层。</param>
        /// <returns>写入数组的碰撞器数量。</returns>
        /// <remarks>当前实现排除触发器，并处理球、AABB、OBB、胶囊体、圆柱体、轴对齐胶囊体和角色控制器。</remarks>
        public int OverlapSphereNonAlloc(FPCollider[] colliders, FixedPointVector3 center, FixedPoint64 radius, int layerMask)
        {
            int count = 0;
            BeginQuery();
            var min = center - new FixedPointVector3(radius, radius, radius);
            var max = center + new FixedPointVector3(radius, radius, radius);
            while (openList.Count > openListIndex && count < colliders.Length)
            {
                var node = openList[openListIndex];
                openListIndex++;
                if (node.FpSphereColliders != null)
                {
                    for (var i = 0; i < node.FpSphereColliders.Count; i++)
                    {
                        var item = node.FpSphereColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled || item.isTrigger)
                        {
                            continue;
                        }
                        if (layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        if (FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(item.min, item.max, min, max))
                        {
                            if ((radius + item.scaledRadius) * (radius + item.scaledRadius) > (item.position - center).sqrMagnitude)
                            {
                                colliders[count] = item;
                                count++;
                                if (count == colliders.Length)
                                {
                                    return count;
                                }
                            }
                        }
                    }
                }
                if (node.FpAABBColliders != null)
                {
                    for (var i = 0; i < node.FpAABBColliders.Count; i++)
                    {
                        var item = node.FpAABBColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled || item.isTrigger)
                        {
                            continue;
                        }
                        if (layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        if (FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(item.min, item.max, min, max))
                        {
                            if (FixedPointIntersection.IntersectWithAABBAndSphere(item.min, item.max, center, radius))
                            {
                                colliders[count] = item;
                                count++;
                                if (count == colliders.Length)
                                {
                                    return count;
                                }
                            }
                        }
                    }
                }
                if (node.FpObbColliders != null)
                {
                    for (var i = 0; i < node.FpObbColliders.Count; i++)
                    {
                        var item = node.FpObbColliders[i];
                        if (!CanQueryNonAlloc(item, layerMask, min, max)) continue;
                        if (FixedPointIntersection.IntersectWithSphereAndOBB(
                                center, radius, item.position, item.halfSize,
                                item.fpTransform.fixedPointMatrix).hit &&
                            TryAddNonAlloc(colliders, ref count, item)) return count;
                    }
                }
                if (node.FpCapsuleColliders != null)
                {
                    for (var i = 0; i < node.FpCapsuleColliders.Count; i++)
                    {
                        var item = node.FpCapsuleColliders[i];
                        if (!CanQueryNonAlloc(item, layerMask, min, max)) continue;
                        if (FixedPointIntersection.IntersectWithSphereAndCapsule(
                                center, radius, item.startPos, item.endPos, item.scaledRadius).hit &&
                            TryAddNonAlloc(colliders, ref count, item)) return count;
                    }
                }
                if (node.FpCylinderColliders != null)
                {
                    for (var i = 0; i < node.FpCylinderColliders.Count; i++)
                    {
                        var item = node.FpCylinderColliders[i];
                        if (!CanQueryNonAlloc(item, layerMask, min, max)) continue;
                        if (FixedPointIntersection.IntersectWithSphereAndCylinder(
                                center, radius, item.startPos, item.endPos, item.scaledRadius).hit &&
                            TryAddNonAlloc(colliders, ref count, item)) return count;
                    }
                }
                if (node.FpAACapsuleColliders != null)
                {
                    for (var i = 0; i < node.FpAACapsuleColliders.Count; i++)
                    {
                        var item = node.FpAACapsuleColliders[i];
                        if (!CanQueryNonAlloc(item, layerMask, min, max)) continue;
                        if (FixedPointIntersection.IntersectWithSphereAndAACapsule(
                                center, radius, item.startPos, item.endPos, item.scaledRadius).hit &&
                            TryAddNonAlloc(colliders, ref count, item)) return count;
                    }
                }
                if (node.FpCharacterColliders != null)
                {
                    for (var i = 0; i < node.FpCharacterColliders.Count; i++)
                    {
                        var item = node.FpCharacterColliders[i];
                        if (!CanQueryNonAlloc(item, layerMask, min, max)) continue;
                        var hit = item.characterColliderType == CharacterCollider.Sphere
                            ? FixedPointIntersection.IntersectWithSphereAndSphere(
                                center, radius, item.position, item.scaledRadius).hit
                            : FixedPointIntersection.IntersectWithSphereAndAACapsule(
                                center, radius, item.startPos, item.endPos, item.scaledRadius).hit;
                        if (hit && TryAddNonAlloc(colliders, ref count, item)) return count;
                    }
                }
                if (node.nodes != null)
                {
                    foreach (var item in node.nodes)
                    {
                        if(item.colliderCount <= 0) continue;
                        if (FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(item.fixedPointAABB.Min, item.fixedPointAABB.Max, min, max))
                            //if (FixedPointIntersection.IntersectWithAABBAndSphere(node.nodes[i].min, node.nodes[i].max, center, radius))
                        {
                            openList.Add(item);
                        }
                    }
                }
            }
            return count;
        }
        /// <summary>
        /// 查找与指定轴对齐包围盒重叠的碰撞器。
        /// </summary>
        /// <param name="center">查询包围盒的中心坐标。</param>
        /// <param name="size">查询包围盒的完整尺寸。</param>
        /// <param name="layerMask">参与查询的层掩码；<c>-1</c> 或 <c>0</c> 表示不限制层。</param>
        /// <param name="includeTrigger">是否包含触发器。</param>
        /// <returns>与查询包围盒重叠的碰撞器列表。</returns>
        /// <remarks>当前实现处理球、AABB 和 OBB 碰撞器。</remarks>
        public List<FPCollider> OverlapAABB(FixedPointVector3 center, FixedPointVector3 size, int layerMask, bool includeTrigger = false)
        {
            var intersectColliders = new List<FPCollider>();
            var calMin = center - size / 2;
            var calMax = center + size / 2;
            var min = new FixedPointVector3(FixedPointMath.Min(calMin.x, calMax.x), FixedPointMath.Min(calMin.y, calMax.y), FixedPointMath.Min(calMin.z, calMax.z));
            var max = new FixedPointVector3(FixedPointMath.Max(calMin.x, calMax.x), FixedPointMath.Max(calMin.y, calMax.y), FixedPointMath.Max(calMin.z, calMax.z));
            BeginQuery();
            while (openList.Count > openListIndex)
            {
                var node = openList[openListIndex];
                openListIndex++;
                if (node.FpSphereColliders != null)
                {
                    for (var i = 0; i < node.FpSphereColliders.Count; i++)
                    {
                        var item = node.FpSphereColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        if (FixedPointIntersection.IntersectWithAABBAndSphere(min, max, item.position, item.scaledRadius))
                        {
                            intersectColliders.Add(item);
                        }
                    }
                }
                if (node.FpAABBColliders != null)
                {
                    for (var i = 0; i < node.FpAABBColliders.Count; i++)
                    {
                        var item = node.FpAABBColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        if (FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(item.min, item.max, min, max))
                        {
                            intersectColliders.Add(item);
                        }
                    }
                }
                if (node.FpObbColliders != null)
                {
                    for (var i = 0; i < node.FpObbColliders.Count; i++)
                    {
                        var item = node.FpObbColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        if (FixedPointIntersection.IntersectWithAABBAndOBBFixedPoint(min, max, item))
                        {
                            intersectColliders.Add(item);
                        }
                    }
                }
                if (node.nodes != null)
                {
                    foreach (var item in node.nodes)
                    {
                        if(item.colliderCount <= 0) continue;
                        if (FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(item.fixedPointAABB.Min, item.fixedPointAABB.Max, min, max))
                        {
                            openList.Add(item);
                        }
                    }
                }
            }
            return intersectColliders;
        }
        /// <summary>
        /// 检测指定胶囊体与树中的角色控制器碰撞器是否发生碰撞。
        /// </summary>
        /// <param name="position">查询胶囊体的中心坐标，用于节点粗检测。</param>
        /// <param name="height">查询胶囊体高度，用于计算节点粗检测范围。</param>
        /// <param name="startPos">查询胶囊体中心线起点。</param>
        /// <param name="endPos">查询胶囊体中心线终点。</param>
        /// <param name="radius">查询胶囊体半径。</param>
        /// <param name="collisions">接收碰撞结果的可复用列表；有效结果位于索引 <c>[0, 返回值)</c>。</param>
        /// <returns>写入的有效碰撞结果数量。</returns>
        public int OverlayCharacterWithCapsule(FixedPointVector3 position, FixedPoint64 height, FixedPointVector3 startPos, FixedPointVector3 endPos, FixedPoint64 radius,ref List<FPCollision> collisions)
        {
            var count = 0;
            BeginQuery();
            var bound = height * 0.5 + radius;
            while (openList.Count > openListIndex)
            {
                var node = openList[openListIndex];
                openListIndex++;
                if (node.FpCharacterColliders != null)
                {
                    for (var i = 0; i < node.FpCharacterColliders.Count; i++)
                    {
                        var item = node.FpCharacterColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        var fixedPointCollision = FixedPointIntersection.IntersectWithSphereAndCapsule(item.position, item.scaledRadius, startPos, endPos, radius);
                        if (fixedPointCollision.hit)
                        {
                            fixedPointCollision.collider = item;
                            if (collisions.Count == count)
                            {
                                collisions.Add(fixedPointCollision);
                            }
                            else
                            {
                                collisions[count] = fixedPointCollision;
                            }
                            count++;
                        }
                    }
                }
                if (node.nodes == null) continue;
                foreach (var item in node.nodes)
                {
                    if(item.colliderCount <= 0) continue;
                    if (FixedPointIntersection.IsIntersectWithSphereAndAABB(position, bound, item.fixedPointAABB.Min, item.fixedPointAABB.Max))
                    {
                        openList.Add(item);
                    }
                }
            }
            return count;
        }

        private bool CanQueryNonAlloc(FPCollider item, int layerMask,
            FixedPointVector3 queryMin, FixedPointVector3 queryMax)
        {
            if (item == null || !item.enabled || item.isTrigger ||
                layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer) ||
                item.castIndex == castIndex)
            {
                return false;
            }

            item.castIndex = castIndex;
            return FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(
                item.min, item.max, queryMin, queryMax);
        }

        private static bool TryAddNonAlloc(FPCollider[] colliders, ref int count, FPCollider collider)
        {
            colliders[count++] = collider;
            return count == colliders.Length;
        }
        /// <summary>
        /// 检测指定球形碰撞器与树中的角色控制器碰撞器是否发生碰撞。
        /// </summary>
        /// <param name="fixedPointFpSphereCollider">作为查询源的球形碰撞器。</param>
        /// <param name="collisions">接收碰撞结果的可复用列表；有效结果位于索引 <c>[0, 返回值)</c>。</param>
        /// <param name="layerMask">参与查询的层掩码；<c>-1</c> 或 <c>0</c> 表示不限制层。</param>
        /// <param name="includeTrigger">是否包含触发器。</param>
        /// <returns>写入的有效碰撞结果数量。</returns>
        public int OverlayCharacterWithSphere(FPSphereCollider fixedPointFpSphereCollider,
            ref List<FPCollision> collisions, int layerMask = -1, bool includeTrigger = false)
        {
            var count = 0;
            var radius = fixedPointFpSphereCollider.scaledRadius;
            fixedPointFpSphereCollider.UpdateAABB();
            var min = fixedPointFpSphereCollider.min;
            var max = fixedPointFpSphereCollider.max;
            BeginQuery();
            while (openList.Count > openListIndex)
            {
                var node = openList[openListIndex];
                openListIndex++;
                if (node.FpCharacterColliders != null)
                {
                    for (var i = 0; i < node.FpCharacterColliders.Count; i++)
                    {
                        var item = node.FpCharacterColliders[i];
                        if (item == null)
                        {
                            continue;
                        }
                        if (!item.enabled)
                        {
                            continue;
                        }
                        if (item.isTrigger && !includeTrigger)
                        {
                            continue;
                        }
                        if (layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer))
                        {
                            continue;
                        }
                        if (item.castIndex == castIndex)
                        {
                            continue;
                        }
                        item.castIndex = castIndex;
                        if (!FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(min, max, item.min, item.max))
                        {
                            continue;
                        }
                        var fixedPointCollision = item.characterColliderType == CharacterCollider.Sphere
                            ? FixedPointIntersection.IntersectWithSphereAndSphere(fixedPointFpSphereCollider.position,
                                radius, item.position, item.scaledRadius)
                            : FixedPointIntersection.IntersectWithSphereAndAACapsule(fixedPointFpSphereCollider.position,
                                radius, item.startPos, item.endPos, item.scaledRadius);
                        if (fixedPointCollision.hit)
                        {
                            fixedPointCollision.collider = item;
                            if (collisions.Count == count)
                            {
                                collisions.Add(fixedPointCollision);
                            }
                            else
                            {
                                collisions[count] = fixedPointCollision;
                            }
                            count++;
                        }
                    }
                }
                if (node.nodes != null)
                {
                    foreach (var item in node.nodes)
                    {
                        if(item.colliderCount <= 0) continue;
                        if (FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(min, max, item.fixedPointAABB.Min, item.fixedPointAABB.Max))
                        {
                            openList.Add(item);
                        }
                    }
                }
            }
            return count;
        }
    }
}
