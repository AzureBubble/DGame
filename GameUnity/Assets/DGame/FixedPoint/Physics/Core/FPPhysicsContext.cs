using System;
using System.Collections.Generic;

namespace DGame.FixedPoint
{
    /// <summary>
    /// 保存一次物理查询所需的可复用节点列表。
    /// </summary>
    internal sealed class PhysicsSearch
    {
        /// <summary>
        /// 查询期间等待访问的八叉树节点。
        /// </summary>
        public readonly FPFastList<FPOctreeNode> openList = new ();
    }

    /// <summary>
    /// 承载一个定点物理世界的状态、固定步长更新和空间查询。
    /// </summary>
    /// <remarks>
    /// 该类型不依赖 Unity，可由客户端表现外壳和服务端共同使用。每个战斗世界应持有独立实例，
    /// 避免通过静态单例共享状态。实例会复用查询列表和对象池，因此不支持并发更新或并发查询。
    /// </remarks>
    public class FPPhysicsContext
    {
        /// <summary>
        /// 当前物理世界中的刚体更新列表。
        /// </summary>
        internal readonly FastList<FPRigidbody> fixedPointRigidbodies = new ();

        /// <summary>
        /// 当前物理世界中的角色控制器更新列表。
        /// </summary>
        internal readonly List<FPCharacterController> fixedPointCharacterControllers = new ();

        /// <summary>
        /// 当前物理世界中的定点逻辑对象更新列表。
        /// </summary>
        internal readonly FastList<FPGameObject> fixedPointGameObjectFastList = new ();

        /// <summary>
        /// 获取定点物理使用的重力加速度。
        /// </summary>
        public static readonly FixedPointVector3 GravitationalAcceleration = new (0, -9.82, 0);

        /// <summary>
        /// 当前物理世界使用的八叉树空间索引。
        /// </summary>
        internal FPOctree fpOctree { get; private set; }

        /// <summary>
        /// 低速碰撞不产生反弹的法向速度阈值。
        /// </summary>
        private static readonly FixedPoint64 CharacterReboundThreshold = -4;

        /// <summary>
        /// 将千分制配置值转换为定点数倍率的公共除数。
        /// </summary>
        public static readonly FixedPoint64 CommonDivision = 0.001;

        /// <summary>
        /// 千分制配置值使用的公共倍数。
        /// </summary>
        internal const int CommonMultiple = 1000;

        /// <summary>
        /// 地面判定高度使用的微小负偏移。
        /// </summary>
        private static readonly FixedPoint64 AdditionRadius　= -FixedPoint64.EN4;

        /// <summary>
        /// 获取每次物理更新使用的固定时间步长。
        /// </summary>
        public FixedPoint64 DeltaTime { get; private set; }

        /// <summary>
        /// 获取当前物理世界已经模拟的累计时间。
        /// </summary>
        public FixedPoint64 TimeSinceStart { get; private set; }

        /// <summary>
        /// 已完成的物理更新帧数。
        /// </summary>
        private int frame { get; set; }

        /// <summary>
        /// 获取当前物理世界独占的确定性随机源。
        /// </summary>
        public FixedPointRandom Random { get; private set; }

        /// <summary>
        /// 未指定固定步长时使用的默认秒数。
        /// </summary>
        public const double DefaultDeltaTime = 0.0333;

        /// <summary>
        /// 复用物理查询临时状态的对象池。
        /// </summary>
        private readonly ObjectPool<PhysicsSearch> searchPool = new ();
        private readonly List<int> m_meshTriangleCandidates = new();
        private readonly List<FPCollider> m_queryColliderCandidates = new(64);

        /// <summary>
        /// 角色和刚体约束求解期间复用的碰撞结果列表。
        /// </summary>
        private List<FPCollision> collisions = new ();

        /// <summary>
        /// 创建独立的定点物理世界。
        /// </summary>
        /// <param name="octreeSize">八叉树逻辑尺寸。</param>
        /// <param name="seed">确定性随机种子，两端必须传入同步种子。</param>
        /// <param name="deltaTime">固定物理步长；传入默认值时使用 <see cref="DefaultDeltaTime"/>。</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="octreeSize"/> 或最终物理步长小于等于 0。</exception>
        public FPPhysicsContext(int octreeSize = 1024, int seed = 0, FixedPoint64 deltaTime = default)
        {
            if (octreeSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(octreeSize), "八叉树逻辑尺寸必须大于 0。");
            }

            var resolvedDeltaTime = deltaTime == default ? (FixedPoint64)DefaultDeltaTime : deltaTime;
            if (resolvedDeltaTime <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "固定物理步长必须大于 0。");
            }

            fpOctree = FPOctree.Initialize(octreeSize);
            Random = new FixedPointRandom(seed);
            DeltaTime = resolvedDeltaTime;
        }

        /// <summary>
        /// 清空全部已注册对象和碰撞结果，并将模拟时间重置到初始状态。
        /// </summary>
        internal void Clear()
        {
            fixedPointRigidbodies.Clear();
            fixedPointCharacterControllers.Clear();
            fixedPointGameObjectFastList.Clear();
            fpOctree.Reset();
            collisions.Clear();
            TimeSinceStart = 0;
            frame = 0;
        }

        /// <summary>
        /// 推进一个固定物理步长，依次更新碰撞器、角色、刚体和定点逻辑对象。
        /// </summary>
        public void OnUpdate()
        {
            fpOctree.UpdateColliders();

            #region 更新角色
            CharacterApplyForces();
            CharacterInteractions();
            CharacterOnUpdates();
            //UpdateCharacterConstraints();
            #endregion

            #region 更新刚体
            for (var i = 0; i < fixedPointRigidbodies.Count; i++)
            {
                fixedPointRigidbodies[i].ApplyForces();
            }
            for (var i = 0; i < fixedPointRigidbodies.Count; i++)
            {
                fixedPointRigidbodies[i].OnUpdate();
            }

            SolveRigidbodyInteractions();

            for (var i = 0; i < fixedPointRigidbodies.Count; i++)
            {
                fixedPointRigidbodies[i].SolveConstraints();
            }
            #endregion

            for (var i = 0; i < fixedPointGameObjectFastList.Count; i++)
            {
                if (fixedPointGameObjectFastList[i] == null)
                {
                    continue;
                }
                fixedPointGameObjectFastList[i].OnLogicUpdate();
            }
            TimeSinceStart += DeltaTime;
            frame++;
        }

        /// <summary>
        /// 检测并求解当前物理世界中全部启用刚体之间的成对碰撞。
        /// </summary>
        /// <remarks>每对刚体每个物理步只处理一次。</remarks>
        private void SolveRigidbodyInteractions()
        {
            for (var i = 0; i < fixedPointRigidbodies.Count; i++)
            {
                var rigidbody = fixedPointRigidbodies[i];
                if (!rigidbody.CanSimulate)
                {
                    continue;
                }

                for (var j = i + 1; j < fixedPointRigidbodies.Count; j++)
                {
                    var targetRigidbody = fixedPointRigidbodies[j];
                    if (!targetRigidbody.CanSimulate)
                    {
                        continue;
                    }

                    FPRigidbody.ResolveCollision(rigidbody, targetRigidbody);
                }
            }
        }

#if UNITY_2021_3_OR_NEWER
        /// <summary>
        /// 由外部表现帧驱动器调用，将定点逻辑状态同步到 Unity 表现对象。
        /// </summary>
        public void OnViewUpdate()
        {
            for (var i = 0; i < fixedPointGameObjectFastList.Count; i++)
            {
                if (fixedPointGameObjectFastList[i] == null)
                {
                    continue;
                }
                fixedPointGameObjectFastList[i].OnViewUpdate();
            }

            foreach (var actor in fixedPointCharacterControllers)
            {
                if (actor == null)
                {
                    continue;
                }
                actor.OnViewUpdate();
            }
        }
#endif

        /// <summary>
        /// 为全部已启用角色计算当前物理帧的内部力。
        /// </summary>
        private void CharacterApplyForces()
        {
            foreach (var actor in fixedPointCharacterControllers)
            {
                if (actor.enabled)
                {
                    actor.AddForce();
                }
            }
        }

        /// <summary>
        /// 处理角色与动态碰撞器之间的碰撞，以及角色之间的相互推挤。
        /// </summary>
        private void CharacterInteractions()
        {
            // 计算角色之间的冲量。
            for (var i = 0; i < fixedPointCharacterControllers.Count; i++)
            {
                if (!fixedPointCharacterControllers[i].enabled)
                {
                    continue;
                }

                // 重要：在此检查刚体碰撞。
                var actor = fixedPointCharacterControllers[i];
                var count = actor.characterColliderType == CharacterCollider.Sphere ?
                           fpOctree.OverlaySphereCollision(actor.position, actor.scaledRadius, ref collisions, -1, true, true) :
                           fpOctree.OverlayAACapsuleCollision(actor.startPos, actor.endPos, actor.scaledRadius, ref collisions, -1, true, true);
                for (var j = 0; j < count; j++)
                {
                    var collision = collisions[j];
                    if (!collision.hit) continue;
                    if (collision.collider.isTrigger) continue;
                    var hitCollider = collision.collider;
                    if (TryGetRigidbody(hitCollider, out var hitRigidbody))
                    {
                        ResolveCharacterRigidbodyCollision(actor, hitRigidbody, collision);
                    }
                    else
                    {
                        actor.AddImpulse(collision.normal * collision.depth * 2);
                    }
                    collision.collider = actor;
                    hitCollider.onCharacterCollide?.Invoke(collision);
                }
                // 检测角色之间的碰撞。
                for (var j = i; j < fixedPointCharacterControllers.Count; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }
                    if (!fixedPointCharacterControllers[j].enabled)
                    {
                        continue;
                    }
                    CharacterInteraction(fixedPointCharacterControllers[i],fixedPointCharacterControllers[j]);
                }
            }
        }

        /// <summary>
        /// 查找绑定指定碰撞器的刚体。
        /// </summary>
        private bool TryGetRigidbody(FPCollider collider, out FPRigidbody rigidbody)
        {
            for (var i = 0; i < fixedPointRigidbodies.Count; i++)
            {
                var candidate = fixedPointRigidbodies[i];
                if (ReferenceEquals(candidate.collider, collider))
                {
                    rigidbody = candidate;
                    return true;
                }
            }

            rigidbody = null;
            return false;
        }

        /// <summary>
        /// 按逆质量分配角色与刚体之间的位置修正，并调整双方速度。
        /// </summary>
        private static void ResolveCharacterRigidbodyCollision(FPCharacterController actor,
            FPRigidbody rigidbody, FPCollision collision)
        {
            if (!GridLayerMask.ValidateLayerMask(actor.targetLayerMask, 1 << rigidbody.collider.layer) ||
                !GridLayerMask.ValidateLayerMask(rigidbody.targetLayerMask, 1 << actor.layer))
            {
                return;
            }

            var actorInvMass = actor.mass > 0 ? actor.reverseMass : FixedPoint64.Zero;
            var rigidbodyInvMass = rigidbody.CanSimulate ? rigidbody.invMass : FixedPoint64.Zero;
            var invMassSum = actorInvMass + rigidbodyInvMass;
            if (invMassSum <= 0)
            {
                return;
            }

            var penetration = collision.depth * 2;
            var actorNormal = collision.normal;
            actor.AddImpulse(actorNormal * penetration * (actorInvMass / invMassSum));
            rigidbody.AddConstraints(-actorNormal * penetration * (rigidbodyInvMass / invMassSum));

            AdjustVelocityByCollision(actor, actorNormal, rigidbody.collider.rebound);
            rigidbody.AdjustVelocityByCollision(-actorNormal, actor.rebound);
        }

        /// <summary>
        /// 计算两个角色控制器之间的碰撞，并按质量分配分离冲量。
        /// </summary>
        /// <param name="fpCharacter">第一个角色控制器。</param>
        /// <param name="targetFpCharacter">第二个角色控制器。</param>
        private static void CharacterInteraction(FPCharacterController fpCharacter,FPCharacterController targetFpCharacter)
        {
             FPCollision collision;
            // 不同层的角色不会发生碰撞。
            if (fpCharacter.layer != targetFpCharacter.layer)
            {
                collision.hit = false;
            } else {
                if (!FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(fpCharacter.min, fpCharacter.max, targetFpCharacter.min, targetFpCharacter.max))
                {
                    return;
                }
                if (fpCharacter.characterColliderType == CharacterCollider.Capsule)
                {
                    collision = targetFpCharacter.characterColliderType == CharacterCollider.Capsule ? FixedPointIntersection.IntersectWithAACapsuleAndAACapsule(fpCharacter.startPos, fpCharacter.endPos, fpCharacter.scaledRadius, targetFpCharacter.startPos, targetFpCharacter.endPos, targetFpCharacter.scaledRadius) : FixedPointIntersection.IntersectWithAACapsuleAndSphere(fpCharacter.startPos, fpCharacter.endPos, fpCharacter.scaledRadius, targetFpCharacter.position, targetFpCharacter.scaledRadius);
                }
                else
                {
                    collision = targetFpCharacter.characterColliderType == CharacterCollider.Capsule ? FixedPointIntersection.IntersectWithSphereAndAACapsule(fpCharacter.position, fpCharacter.scaledRadius, targetFpCharacter.startPos, targetFpCharacter.endPos, targetFpCharacter.scaledRadius) : FixedPointIntersection.IntersectWithSphereAndSphere(fpCharacter.position, fpCharacter.scaledRadius, targetFpCharacter.position, targetFpCharacter.scaledRadius);
                }

                if (!collision.hit) return;
                var totalMass = fpCharacter.mass + targetFpCharacter.mass;
                if (totalMass <= 0)
                {
                    return;
                }
                // 质量较小的角色更容易被推动。
                var depth1 = collision.depth * 2 * targetFpCharacter.mass / totalMass;
                var depth2 = collision.depth * 2 * fpCharacter.mass / totalMass;
                fpCharacter.AddImpulse(collision.normal * depth1);
                targetFpCharacter.AddImpulse(-collision.normal * depth2);
            }
        }

        /// <summary>
        /// 更新全部已启用角色的移动状态。
        /// </summary>
        private void CharacterOnUpdates()
        {
            foreach (var actor in fixedPointCharacterControllers)
            {
                if (actor.enabled)
                {
                    actor.OnUpdate();
                }
            }
        }

        /// <summary>
        /// 检测并求解指定角色与静态碰撞器之间的穿透约束。
        /// </summary>
        /// <param name="actor">需要求解约束的角色控制器。</param>
        public void SolveConstraints(FPCharacterController actor)
        {
            if (!actor.enabled)
            {
                return;
            }
            var count = actor.characterColliderType == CharacterCollider.Sphere ?
                            fpOctree.OverlaySphereCollision(actor.position, actor.scaledRadius, ref collisions, -1, true) :
                            fpOctree.OverlayAACapsuleCollision(actor.startPos, actor.endPos, actor.scaledRadius, ref collisions, -1, true);
            actor.isGround = false;

            for (var i = 0; i < count; i++)
            {
                var collision = collisions[i];
                if (!collision.hit) continue;
                if (!collision.collider.isTrigger)
                {
                    if (collision.closestPoint.y < actor.fpTransform.position.y + actor.scaledRadius + AdditionRadius)
                    {
                        // （旧）低于 actor.radius * 0.29 时坡度超过 45 度，需要使角色下落。（1 - Cos45°）= 0.29
                        // （新）为允许攀爬 45 度斜坡，仅在超过 50 度时使角色下落。（1 - Cos50°）= 0.36
                        if (collision.collider.layer != LayerConstant.WallLayer)
                        {
                            actor.isGround = true;
                            actor.groundNormal = collision.normal;
                            // FixedPointVector3.Dot(actor.preVelocity, collision.normal) < 0 用于判断角色是否正在攀爬。
                            if (actor.preVelocity != FixedPointVector3.zero && FixedPointVector3.Dot(actor.preVelocity, collision.normal) < 0)
                            {
                                // 攀爬时使用 FixedPointVector3.up，防止角色下落。
                                actor.AddConstraints(FixedPointVector3.up * collision.depth * 2);
                                AdjustVelocityByCollision(actor, FixedPointVector3.up, collision.collider.rebound);
                            }
                            else
                            {
                                // 其他情况下使用 collision.normal，防止角色下落。
                                actor.AddConstraints(collision.normal * (collision.depth * 2));
                                AdjustVelocityByCollision(actor, collision.normal, collision.collider.rebound);
                            }
                            /*
                                //Debug.Log($"{collision.closestPoint.y}||{actor.fixedPointTransform.position.y }||{actor.radius * 0.71}");
                                actor.isGround = true;
                                actor.groundPhysicsMaterial = collision.collider.physicsMaterial;
                                actor.groundNormal = collision.normal;
                                var dot1 = FixedPointVector3.Dot(collision.normal, FixedPointVector3.up);
                                actor.AddConstraints(FixedPointVector3.up * (collision.depth * 2 / dot1) * 0.9);
                                AdjustVelocityByCollision(actor, FixedPointVector3.up, collision.collider.rebound);
                                */
                        }
                        else
                        {
                            actor.AddConstraints(collision.normal * (collision.depth * 2));
                            AdjustVelocityByCollision(actor, collision.normal, collision.collider.rebound);
                        }
                    }
                    else
                    {
                        actor.AddConstraints(collision.normal * (collision.depth * 2));
                        AdjustVelocityByCollision(actor, collision.normal, collision.collider.rebound);
                    }
                }
                var hitCollider = collision.collider;
                collision.collider = actor;
                hitCollider.onCharacterCollide?.Invoke(collision);
            }

            if (actor.isGround)
            {
                actor.fallDuration = 0;
                if (actor.colliderState == FPCharacterController.CharacterColliderState.Fall)
                {
                    actor.onLand?.Invoke();
                }
                actor.colliderState = FPCharacterController.CharacterColliderState.Ground;
            }
            else
            {
                if (!Raycast(actor.fpTransform.position + new FixedPointVector3(0, actor.scaledRadius, 0), FixedPointVector3.down, actor.scaledRadius + FPCharacterController.stepHeight, out var _ , 0 ,false))
                {
                    actor.fallDuration += DeltaTime;
                    if (actor.fallDuration > 0.1 && actor.colliderState == FPCharacterController.CharacterColliderState.Ground)
                    {
                        actor.colliderState = FPCharacterController.CharacterColliderState.Fall;
                        actor.onOffGround?.Invoke();
                    }
                }
                else
                {
                    actor.fallDuration = 0;
                    if (actor.colliderState == FPCharacterController.CharacterColliderState.Fall)
                    {
                        actor.onLand?.Invoke();
                    }
                    actor.colliderState = FPCharacterController.CharacterColliderState.Ground;
                }
            }
            var constraint = actor.constraint;
            /* 与其他静态碰撞器检测时，暂存地面约束并提高其优先级，避免角色陷入地面。
            if (actor.groundNormal != FixedPointVector3.zero)
            {
                var dotGroundNormal = FixedPointVector3.Dot(actor.constraint, actor.groundNormal);
                actor.constraint -= actor.groundNormal * dotGroundNormal;
            }
            */
            var normal = constraint.normalized;
            var dot = FixedPointVector3.Dot(actor.velocity.normalized, normal);
            // 当速度包含与约束方向相反的分量时，减小该反向分量。
            if ((dot <= 0 && constraint != FixedPointVector3.zero) || actor.isGround)
            {
                actor.velocity -= actor.velocity * (1 + dot) * FixedPointVector3.Dot(normal,FixedPointVector3.up);// * speed;
            }
            var speed = actor.knockBackVelocity.magnitude;
            speed = FixedPointMath.Min(speed, actor.frictionKnockBack);
            actor.knockBackVelocity -= actor.knockBackVelocity * speed;
            actor.SolveConstraints();
            actor.UpdateCollider();
        }

        /// <summary>
        /// 根据碰撞法线和反弹系数移除角色朝向障碍物的速度分量。
        /// </summary>
        /// <param name="actor">需要调整速度的角色。</param>
        /// <param name="constraintNormal">碰撞约束法线。</param>
        /// <param name="rebound">被碰撞物体的反弹系数。</param>
        private static void AdjustVelocityByCollision(FPCharacterController actor,FixedPointVector3 constraintNormal,FixedPoint64 rebound)
        {
            var dot = FixedPointVector3.Dot(actor.velocity, constraintNormal);
            if (dot < 0)
            {
                if (dot > CharacterReboundThreshold)
                {
                    rebound = 0;
                }
                actor.velocity = actor.velocity - constraintNormal * dot * (1 + rebound + actor.rebound);
                actor.knockBackVelocity -= FixedPointVector3.Project(actor.knockBackVelocity, constraintNormal);
            }
        }

        /// <summary>
        /// 注册需要参与固定步长更新的刚体。
        /// </summary>
        /// <param name="rigidbody">需要注册的刚体。</param>
        public void AddRigidbody(FPRigidbody rigidbody)
        {
            fixedPointRigidbodies.Add(rigidbody);
        }

        /// <summary>
        /// 从当前物理上下文移除刚体。
        /// </summary>
        /// <param name="rigidbody">需要移除的刚体。</param>
        public void RemoveRigidbody(FPRigidbody rigidbody)
        {
            fixedPointRigidbodies.Remove(rigidbody);
        }

        /// <summary>
        /// 注册需要参与角色更新和相互作用计算的角色控制器。
        /// </summary>
        /// <param name="controller">需要注册的角色控制器；重复注册会被忽略。</param>
        public void AddCharacter(FPCharacterController controller)
        {
            if (controller != null && !fixedPointCharacterControllers.Contains(controller))
            {
                fixedPointCharacterControllers.Add(controller);
            }
        }

        /// <summary>
        /// 从当前物理上下文移除角色控制器更新项。
        /// </summary>
        /// <param name="controller">需要移除的角色控制器。</param>
        public void RemoveCharacter(FPCharacterController controller)
        {
            if (controller != null)
            {
                fixedPointCharacterControllers.Remove(controller);
            }
        }

        /// <summary>
        /// 注册需要参与八叉树更新和空间查询的碰撞器。
        /// </summary>
        /// <param name="collider">需要注册的碰撞器。</param>
        public void AddCollider(FPCollider collider)
        {
            fpOctree.AddCollider(collider);
        }

        /// <summary>
        /// 从当前物理上下文注销碰撞器，并同步清理其八叉树节点归属。
        /// </summary>
        /// <param name="collider">需要注销的碰撞器。</param>
        /// <returns>碰撞器原本已注册时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
        public bool RemoveCollider(FPCollider collider)
        {
            if (collider is FPCharacterController controller)
            {
                RemoveCharacter(controller);
            }

            var rigidbodyIndex = 0;
            while (rigidbodyIndex < fixedPointRigidbodies.Count)
            {
                var rigidbody = fixedPointRigidbodies[rigidbodyIndex];
                if (ReferenceEquals(rigidbody.collider, collider))
                {
                    RemoveRigidbody(rigidbody);
                    continue;
                }

                rigidbodyIndex++;
            }

            return fpOctree.RemoveCollider(collider);
        }

        /// <summary>
        /// 注册需要参与逻辑更新的定点游戏对象。
        /// </summary>
        /// <param name="gameObject">需要注册的定点游戏对象。</param>
        public void AddGameObject(FPGameObject gameObject)
        {
            fixedPointGameObjectFastList.Add(gameObject);
        }

        /// <summary>
        /// 从当前物理上下文移除定点逻辑对象。
        /// </summary>
        /// <param name="gameObject">需要移除的定点逻辑对象。</param>
        public void RemoveGameObject(FPGameObject gameObject)
        {
            fixedPointGameObjectFastList.Remove(gameObject);
        }

        #region 物理查询入口

        /// <summary>
        /// 获取当前物理世界已注册的碰撞器只读视图。
        /// </summary>
        /// <remarks>返回的是内部列表实例，调用方不应直接修改。</remarks>
        public IReadOnlyList<FPCollider> Colliders => fpOctree.colliders;

        /// <summary>
        /// 查找与指定球体重叠的碰撞器，并复用调用方提供的结果列表。
        /// </summary>
        /// <param name="position">查询球体中心。</param>
        /// <param name="radius">查询球体半径。</param>
        /// <param name="colliders">接收结果的可复用列表；有效结果位于索引 <c>[0, 返回值)</c>。</param>
        /// <param name="layerMask">参与查询的层掩码；<c>-1</c> 或 <c>0</c> 表示不限制层。</param>
        /// <param name="includeTrigger">是否包含触发器。</param>
        /// <returns>写入的有效碰撞器数量。</returns>
        public int OverlapSphere(FixedPointVector3 position, FixedPoint64 radius, ref List<FPCollider> colliders, int layerMask = -1, bool includeTrigger = false)
        {
            return fpOctree.OverlapSphere(position, radius, ref colliders, layerMask, includeTrigger);
        }

        /// <summary>
        /// 检测指定胶囊体与角色控制器之间的碰撞。
        /// </summary>
        /// <param name="position">查询胶囊体中心，用于八叉树粗检测。</param>
        /// <param name="height">查询胶囊体高度。</param>
        /// <param name="startPos">查询胶囊体中心线起点。</param>
        /// <param name="endPos">查询胶囊体中心线终点。</param>
        /// <param name="radius">查询胶囊体半径。</param>
        /// <param name="collisions">接收结果的可复用列表；有效结果位于索引 <c>[0, 返回值)</c>。</param>
        /// <returns>写入的有效碰撞结果数量。</returns>
        public int OverlayCharacterWithCapsule(FixedPointVector3 position, FixedPoint64 height, FixedPointVector3 startPos, FixedPointVector3 endPos, FixedPoint64 radius, ref List<FPCollision> collisions)
        {
            return fpOctree.OverlayCharacterWithCapsule(position, height, startPos, endPos, radius, ref collisions);
        }

        /// <summary>
        /// 检测指定有向包围盒与碰撞器之间的碰撞。
        /// </summary>
        /// <param name="position">查询盒中心。</param>
        /// <param name="halfSize">查询盒半尺寸。</param>
        /// <param name="orientation">查询盒旋转矩阵。</param>
        /// <param name="layerMask">参与查询的层掩码；<c>-1</c> 或 <c>0</c> 表示不限制层。</param>
        /// <param name="includeTrigger">是否包含触发器。</param>
        /// <returns>碰撞结果列表。</returns>
        /// <remarks>当前底层实现仅处理球形碰撞器。</remarks>
        public List<FPCollision> OverlayBoxCollision(FixedPointVector3 position, FixedPointVector3 halfSize, FixedPointMatrix orientation, int layerMask = -1, bool includeTrigger = false)
        {
            return fpOctree.OverlayBoxCollision(position, halfSize, orientation, layerMask, includeTrigger);
        }

        public int OverlayBoxCollision(FixedPointVector3 position,
            FixedPointVector3 halfSize, FixedPointMatrix orientation,
            ref List<FPCollision> collisions,
            int layerMask = -1, bool includeTrigger = false)
        {
            return fpOctree.OverlayBoxCollision(
                position, halfSize, orientation, ref collisions, layerMask, includeTrigger);
        }

        /// <summary>
        /// 以已有碰撞器作为查询体，通过八叉树粗检测后执行已实现的精确碰撞组合。
        /// </summary>
        public int OverlayColliderCollision(
            FPCollider queryCollider,
            ref List<FPCollision> collisions,
            int layerMask = -1,
            bool includeTrigger = false)
        {
            return fpOctree.OverlayColliderCollision(
                queryCollider, ref collisions, layerMask, includeTrigger);
        }

        /// <summary>
        /// 计算点到无限直线的平方距离。
        /// </summary>
        /// <param name="ray">定义直线的射线；方向应为单位向量。</param>
        /// <param name="point">需要计算距离的点。</param>
        /// <returns>点到直线的平方距离。</returns>
        public static FixedPoint64 SqrDistanceToLine(FixedPointRay ray, FixedPointVector3 point)
        {
            return FixedPointVector3.Cross(ray.Dir, point - ray.Point).sqrMagnitude;
        }

        /// <summary>
        /// 判断碰撞器是否应从当前查询中排除。
        /// </summary>
        /// <param name="item">候选碰撞器。</param>
        /// <param name="layerMask">查询层掩码。</param>
        /// <param name="includeTrigger">是否包含触发器。</param>
        /// <returns>候选无效、未启用、触发器被排除或层不匹配时返回 <see langword="true"/>。</returns>
        private static bool IsNodeInValidate(FPCollider item, int layerMask, bool includeTrigger)
        {
            if (item == null)
            {
                return true;
            }
            if (!item.enabled)
            {
                return true;
            }
            if (item.isTrigger && !includeTrigger)
            {
                return true;
            }
            return layerMask != -1 && !GridLayerMask.ValidateLayerMask(layerMask, 1 << item.layer);
        }

        /// <summary>
        /// 判断碰撞器是否应从当前查询中排除，并执行两个 AABB 的粗检测。
        /// </summary>
        /// <param name="item">候选碰撞器。</param>
        /// <param name="layerMask">查询层掩码。</param>
        /// <param name="includeTrigger">是否包含触发器。</param>
        /// <param name="minA">第一个 AABB 的最小坐标。</param>
        /// <param name="maxA">第一个 AABB 的最大坐标。</param>
        /// <param name="minB">第二个 AABB 的最小坐标。</param>
        /// <param name="maxB">第二个 AABB 的最大坐标。</param>
        /// <returns>基础过滤不通过或两个 AABB 不相交时返回 <see langword="true"/>。</returns>
        private static bool IsNodeInValidate(FPCollider item, int layerMask, bool includeTrigger,FixedPointVector3 minA, FixedPointVector3 maxA, FixedPointVector3 minB, FixedPointVector3 maxB)
        {
            if (IsNodeInValidate(item,layerMask,includeTrigger))
            {
                return true;
            }
            return !FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(minA,maxA,minB,maxB);
        }

        private static bool IsPointStrictlyInsideAabb(
            FixedPointVector3 point,
            FixedPointVector3 min,
            FixedPointVector3 max)
        {
            return point.x > min.x && point.x < max.x &&
                   point.y > min.y && point.y < max.y &&
                   point.z > min.z && point.z < max.z;
        }

        private static bool IsPointStrictlyInsideObb(
            FixedPointVector3 point,
            FixedPointVector3 position,
            FixedPointVector3 halfSize,
            FixedPointMatrix orientation)
        {
            var offset = point - position;
            var axisX = new FixedPointVector3(orientation.M11, orientation.M12, orientation.M13);
            var axisY = new FixedPointVector3(orientation.M21, orientation.M22, orientation.M23);
            var axisZ = new FixedPointVector3(orientation.M31, orientation.M32, orientation.M33);
            var localPoint = new FixedPointVector3(
                FixedPointVector3.Dot(offset, axisX),
                FixedPointVector3.Dot(offset, axisY),
                FixedPointVector3.Dot(offset, axisZ));
            return FixedPointMath.Abs(localPoint.x) < halfSize.x &&
                   FixedPointMath.Abs(localPoint.y) < halfSize.y &&
                   FixedPointMath.Abs(localPoint.z) < halfSize.z;
        }

        private static bool IsPointStrictlyInsideCapsule(
            FixedPointVector3 point,
            FixedPointVector3 start,
            FixedPointVector3 end,
            FixedPoint64 radius)
        {
            var pointOnAxis = FixedPointIntersection.ClosestPointWithPointAndLine(start, end, point);
            return (point - pointOnAxis).sqrMagnitude < radius * radius;
        }

        /// <summary>
        /// 在指定长度内发射射线，并返回距离起点最近的碰撞结果。
        /// </summary>
        /// <param name="origin">射线起点。</param>
        /// <param name="direct">射线方向，应为单位向量。</param>
        /// <param name="length">射线最大长度。</param>
        /// <param name="fpRaycastHit">命中时返回碰撞器、交点和法线信息。</param>
        /// <param name="layerMask">参与查询的层掩码；<c>-1</c> 或 <c>0</c> 表示不限制层。</param>
        /// <param name="includeTrigger">是否包含触发器。</param>
        /// <returns>命中支持的碰撞器时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
        /// <remarks>当前实现检测球、AABB、OBB、胶囊体、圆柱体、轴对齐胶囊体、网格及角色控制器。</remarks>
        public bool Raycast(FixedPointVector3 origin, FixedPointVector3 direct, FixedPoint64 length, out FPRaycastHit fpRaycastHit, int layerMask, bool includeTrigger)
        {
            fpRaycastHit = null;
            if (direct.IsZero() || length < FixedPoint64.Zero)
            {
                return false;
            }

            var fixedPointRay = new FixedPointRay(origin, direct);
            FPCollider fpCollider = null;
            var intersection = FixedPointVector3.zero;
            var outPoint = FixedPointVector3.zero;
            var normal = FixedPointVector3.zero;
            var sqrDistance = FixedPoint64.MaxValue;
            var physicsSearch = searchPool.Pull();
            physicsSearch.openList.Clear();
            physicsSearch.openList.Add(fpOctree.root);
            while (physicsSearch.openList.Count > 0)
            {
                var node = physicsSearch.openList[0];
                physicsSearch.openList.Remove(node);
                FPCollision fpCollision;
                FixedPoint64 currentDistance;
                if (node.FpSphereColliders != null)
                {
                    for (var i = 0; i < node.FpSphereColliders.Count; i++)
                    {
                        var item = node.FpSphereColliders[i];
                        if (IsNodeInValidate(item, layerMask, includeTrigger))
                        {
                            continue;
                        }
                        if (FixedPointIntersection.IntersetWithRayAndSphereFixedPoint(origin, direct, length, item.position, item.scaledRadius, out fpCollision))
                        {
                            currentDistance = fpCollision.t * fpCollision.t;
                            if (currentDistance < sqrDistance)
                            {
                                sqrDistance = currentDistance;
                                intersection = fpCollision.closestPoint;
                                normal = fpCollision.normal;
                                outPoint = fpCollision.outsidePoint;
                                fpCollider = item;
                            }
                        }
                    }
                }
                if (node.FpAABBColliders != null)
                {
                    for (var i = 0; i < node.FpAABBColliders.Count; i++)
                    {
                        var item = node.FpAABBColliders[i];
                        if (IsNodeInValidate(item, layerMask, includeTrigger))
                        {
                            continue;
                        }
                        if (!IsPointStrictlyInsideAabb(origin, item.min, item.max) &&
                            FixedPointIntersection.IntersectWithRayAndAABBFixedPointA(
                                origin, direct, length, item.min, item.max, out fpCollision))
                        {
                            currentDistance = (fpCollision.closestPoint - origin).sqrMagnitude;
                            if (currentDistance < sqrDistance)
                            {
                                sqrDistance = currentDistance;
                                intersection = fpCollision.closestPoint;
                                normal = fpCollision.normal;
                                fpCollider = item;
                            }
                        }
                    }
                }
                if (node.FpObbColliders != null)
                {
                    for (var i = 0; i < node.FpObbColliders.Count; i++)
                    {
                        var item = node.FpObbColliders[i];
                        if (IsNodeInValidate(item, layerMask, includeTrigger))
                        {
                            continue;
                        }
                        if (!FixedPointIntersection.PointInAABB(
                                origin, item.min, item.max) &&
                            !FixedPointIntersection.IntersectWithRayAndAABBFixedPointA(
                                origin, direct, length, item.min, item.max, out fpCollision))
                        {
                            continue;
                        }
                        if (!IsPointStrictlyInsideObb(
                                origin, item.position, item.halfSize, item.fpTransform.fixedPointMatrix) &&
                            FixedPointIntersection.IntersectWithRayAndOBBFixedPoint(
                                fixedPointRay.Point, fixedPointRay.Dir, length, item.position,
                                item.halfSize, item.fpTransform.fixedPointMatrix, out fpCollision) >= 0)
                        {
                            currentDistance = (fpCollision.closestPoint - origin).sqrMagnitude;
                            if (currentDistance < sqrDistance)
                            {
                                sqrDistance = currentDistance;
                                intersection = fpCollision.closestPoint;
                                normal = fpCollision.normal;
                                outPoint = fpCollision.outsidePoint;
                                fpCollider = item;
                            }
                        }
                    }
                }
                if (node.FpCapsuleColliders != null)
                {
                    for (var i = 0; i < node.FpCapsuleColliders.Count; i++)
                    {
                        var item = node.FpCapsuleColliders[i];
                        if (IsNodeInValidate(item, layerMask, includeTrigger))
                        {
                            continue;
                        }
                        if (IsPointStrictlyInsideCapsule(
                                origin, item.startPos, item.endPos, item.scaledRadius))
                        {
                            continue;
                        }
                        fpCollision = FixedPointIntersection.IntersectWithRayAndCapsule(
                            origin, direct, length, item.startPos, item.endPos, item.scaledRadius);
                        if (fpCollision.hit)
                        {
                            currentDistance = fpCollision.t * fpCollision.t;
                            if (currentDistance < sqrDistance)
                            {
                                sqrDistance = currentDistance;
                                intersection = fpCollision.closestPoint;
                                normal = fpCollision.normal;
                                outPoint = fpCollision.outsidePoint;
                                fpCollider = item;
                            }
                        }
                    }
                }
                if (node.FpCylinderColliders != null)
                {
                    for (var i = 0; i < node.FpCylinderColliders.Count; i++)
                    {
                        var item = node.FpCylinderColliders[i];
                        if (IsNodeInValidate(item, layerMask, includeTrigger))
                        {
                            continue;
                        }
                        fpCollision = FixedPointIntersection.IntersectWithRayAndCylinder(
                            origin, direct, length, item.startPos, item.endPos, item.scaledRadius);
                        if (!fpCollision.hit)
                        {
                            continue;
                        }
                        currentDistance = fpCollision.t * fpCollision.t;
                        if (currentDistance < sqrDistance)
                        {
                            sqrDistance = currentDistance;
                            intersection = fpCollision.closestPoint;
                            normal = fpCollision.normal;
                            outPoint = fpCollision.outsidePoint;
                            fpCollider = item;
                        }
                    }
                }
                if (node.FpAACapsuleColliders != null)
                {
                    for (var i = 0; i < node.FpAACapsuleColliders.Count; i++)
                    {
                        var item = node.FpAACapsuleColliders[i];
                        if (IsNodeInValidate(item, layerMask, includeTrigger))
                        {
                            continue;
                        }
                        if (IsPointStrictlyInsideCapsule(
                                origin, item.startPos, item.endPos, item.scaledRadius))
                        {
                            continue;
                        }
                        fpCollision = FixedPointIntersection.IntersectWithRayAndCapsule(
                            origin, direct, length, item.startPos, item.endPos, item.scaledRadius);
                        if (fpCollision.hit)
                        {
                            currentDistance = fpCollision.t * fpCollision.t;
                            if (currentDistance < sqrDistance)
                            {
                                sqrDistance = currentDistance;
                                intersection = fpCollision.closestPoint;
                                normal = fpCollision.normal;
                                outPoint = fpCollision.outsidePoint;
                                fpCollider = item;
                            }
                        }
                    }
                }
                if (node.FpCharacterColliders != null)
                {
                    for (var i = 0; i < node.FpCharacterColliders.Count; i++)
                    {
                        var item = node.FpCharacterColliders[i];
                        if (IsNodeInValidate(item, layerMask, includeTrigger))
                        {
                            continue;
                        }
                        if (item.characterColliderType == CharacterCollider.Sphere)
                        {
                            if (!FixedPointIntersection.IntersetWithRayAndSphereFixedPoint(
                                    origin, direct, length, item.position, item.scaledRadius, out fpCollision))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (IsPointStrictlyInsideCapsule(
                                    origin, item.startPos, item.endPos, item.scaledRadius))
                            {
                                continue;
                            }
                            fpCollision = FixedPointIntersection.IntersectWithRayAndCapsule(
                                origin, direct, length, item.startPos, item.endPos, item.scaledRadius);
                            if (!fpCollision.hit)
                            {
                                continue;
                            }
                        }
                        currentDistance = fpCollision.t * fpCollision.t;
                        if (currentDistance < sqrDistance)
                        {
                            sqrDistance = currentDistance;
                            intersection = fpCollision.closestPoint;
                            normal = fpCollision.normal;
                            outPoint = fpCollision.outsidePoint;
                            fpCollider = item;
                        }
                    }
                }
                if (node.FpMeshColliders != null)
                {
                    for (var i = 0; i < node.FpMeshColliders.Count; i++)
                    {
                        var item = node.FpMeshColliders[i];
                        if (IsNodeInValidate(item, layerMask, includeTrigger))
                        {
                            continue;
                        }

                        item.CollectTriangleCandidates(
                            FixedPointVector3.Min(origin, origin + fixedPointRay.Dir * length),
                            FixedPointVector3.Max(origin, origin + fixedPointRay.Dir * length),
                            m_meshTriangleCandidates);
                        foreach (var triangleIndex in m_meshTriangleCandidates)
                        {
                            item.GetWorldTriangle(triangleIndex, out var a, out var b, out var c);
                            if (!FixedPointIntersection.IntersectWithRayAndTriangleFixedPoint(
                                    origin, fixedPointRay.Dir, FixedPointVector3.zero,
                                    a, b, c, out fpCollision) ||
                                fpCollision.t > length)
                            {
                                continue;
                            }

                            currentDistance = fpCollision.t * fpCollision.t;
                            if (currentDistance < sqrDistance)
                            {
                                sqrDistance = currentDistance;
                                intersection = fpCollision.closestPoint;
                                normal = fpCollision.normal;
                                outPoint = fpCollision.outsidePoint;
                                fpCollider = item;
                            }
                        }
                    }
                }
                if (node.nodes != null)
                {
                    foreach (var item in node.nodes)
                    {
                        if(item.colliderCount <= 0) continue;
                        if (FixedPointIntersection.PointInAABB(fixedPointRay.Point, item.fixedPointAABB.Min, item.fixedPointAABB.Max)
                            || FixedPointIntersection.IntersectWithRayAndAABBFixedPoint(fixedPointRay.Point, fixedPointRay.Dir * length, item.fixedPointAABB.Min, item.fixedPointAABB.Max, out fpCollision) != FixedPoint64.MaxValue)
                        {
                            physicsSearch.openList.Add(item);
                        }
                    }
                }
            }
            searchPool.Push(physicsSearch);
            if (fpCollider == null) return false;
            fpRaycastHit = new FPRaycastHit(
                fpCollider, intersection, normal, outPoint, FixedPointMath.Sqrt(sqrDistance));
            return true;
        }

        /// <summary>
        /// 沿指定方向连续扫描球体，并返回八叉树中距离起点最近的碰撞结果。
        /// </summary>
        /// <param name="origin">扫描球心起点。</param>
        /// <param name="radius">扫描球世界半径。</param>
        /// <param name="direct">扫描方向，无须归一化。</param>
        /// <param name="length">最大扫描距离。</param>
        /// <param name="fpRaycastHit">命中时返回目标碰撞器、目标表面接触点及法线。</param>
        /// <param name="layerMask">参与查询的层掩码；<c>-1</c> 或 <c>0</c> 表示不限制层。</param>
        /// <param name="includeTrigger">是否包含触发器。</param>
        /// <returns>扫描球在给定距离内命中支持的碰撞器时返回 <see langword="true"/>。</returns>
        /// <remarks>
        /// 查询只遍历一次八叉树，不执行固定子步进。当前精确支持球、AABB、OBB、胶囊体、
        /// 轴对齐胶囊体、网格及角色控制器；圆柱体暂不参与该连续碰撞查询。
        /// </remarks>
        public bool SphereCast(
            FixedPointVector3 origin,
            FixedPoint64 radius,
            FixedPointVector3 direct,
            FixedPoint64 length,
            out FPRaycastHit fpRaycastHit,
            int layerMask = -1,
            bool includeTrigger = false,
            FPCollider ignoreCollider = null)
        {
            fpRaycastHit = null;

            if (radius < FixedPoint64.Zero || length < FixedPoint64.Zero || direct.IsZero())
            {
            return false;
        }

            var direction = direct.normalized;
            var delta = direction * length;
            var end = origin + delta;
            var radiusVector = new FixedPointVector3(radius, radius, radius);
            var sweepMin = FixedPointVector3.Min(origin, end) - radiusVector;
            var sweepMax = FixedPointVector3.Max(origin, end) + radiusVector;
            var closestDistance = FixedPoint64.MaxValue;
            var centerAtHit = FixedPointVector3.zero;
            var hitNormal = FixedPointVector3.zero;
            FPCollider hitCollider = null;
            var physicsSearch = searchPool.Pull();
            physicsSearch.openList.Clear();
            physicsSearch.openList.Add(fpOctree.root);

            while (physicsSearch.openList.Count > 0)
            {
                var node = physicsSearch.openList[0];
                physicsSearch.openList.Remove(node);
                FPCollision collision;

                if (node.FpSphereColliders != null)
                {
                    for (var i = 0; i < node.FpSphereColliders.Count; i++)
                    {
                        var item = node.FpSphereColliders[i];
                        if (item == ignoreCollider) continue;
                        if (IsNodeInValidate(item, layerMask, includeTrigger,
                                sweepMin, sweepMax, item.min, item.max))
                        {
                            continue;
                        }

                        collision = IntersectSphereCastWithSphere(
                            origin, direction, length, radius, item.position, item.scaledRadius);
                        SelectClosestSphereCastHit(item, collision,
                            ref closestDistance, ref centerAtHit, ref hitNormal, ref hitCollider);
                    }
                }

                if (node.FpAABBColliders != null)
                {
                    for (var i = 0; i < node.FpAABBColliders.Count; i++)
                    {
                        var item = node.FpAABBColliders[i];
                        if (item == ignoreCollider) continue;
                        if (IsNodeInValidate(item, layerMask, includeTrigger,
                                sweepMin, sweepMax, item.min, item.max))
                        {
                            continue;
                        }

                        collision = FixedPointIntersection.IntersectWithRayAndRoundedAABB(
                            origin, direction, length, item.min, item.max, radius);
                        SelectClosestSphereCastHit(item, collision,
                            ref closestDistance, ref centerAtHit, ref hitNormal, ref hitCollider);
                    }
                }

                if (node.FpObbColliders != null)
                {
                    for (var i = 0; i < node.FpObbColliders.Count; i++)
                    {
                        var item = node.FpObbColliders[i];
                        if (item == ignoreCollider) continue;
                        if (IsNodeInValidate(item, layerMask, includeTrigger,
                                sweepMin, sweepMax, item.min, item.max))
                        {
                            continue;
                        }

                        collision = FixedPointIntersection.IntersectWithRayAndRoundedOBB(
                            origin,
                            direction,
                            length,
                            item.position,
                            item.halfSize,
                            item.fpTransform.fixedPointMatrix,
                            radius);
                        SelectClosestSphereCastHit(item, collision,
                            ref closestDistance, ref centerAtHit, ref hitNormal, ref hitCollider);
                    }
                }

                if (node.FpCapsuleColliders != null)
                {
                    for (var i = 0; i < node.FpCapsuleColliders.Count; i++)
                    {
                        var item = node.FpCapsuleColliders[i];
                        if (item == ignoreCollider) continue;
                        if (IsNodeInValidate(item, layerMask, includeTrigger,
                                sweepMin, sweepMax, item.min, item.max))
                        {
                            continue;
                        }

                        collision = IntersectSphereCastWithCapsule(
                            origin, direction, length, radius, item.startPos, item.endPos, item.scaledRadius);
                        SelectClosestSphereCastHit(item, collision,
                            ref closestDistance, ref centerAtHit, ref hitNormal, ref hitCollider);
                    }
                }

                if (node.FpAACapsuleColliders != null)
                {
                    for (var i = 0; i < node.FpAACapsuleColliders.Count; i++)
                    {
                        var item = node.FpAACapsuleColliders[i];
                        if (item == ignoreCollider) continue;
                        if (IsNodeInValidate(item, layerMask, includeTrigger,
                                sweepMin, sweepMax, item.min, item.max))
                        {
                            continue;
                        }

                        collision = IntersectSphereCastWithCapsule(
                            origin, direction, length, radius, item.startPos, item.endPos, item.scaledRadius);
                        SelectClosestSphereCastHit(item, collision,
                            ref closestDistance, ref centerAtHit, ref hitNormal, ref hitCollider);
                    }
                }

                if (node.FpCylinderColliders != null)
                {
                    for (var i = 0; i < node.FpCylinderColliders.Count; i++)
                    {
                        var item = node.FpCylinderColliders[i];
                        if (item == ignoreCollider) continue;
                        if (IsNodeInValidate(item, layerMask, includeTrigger,
                                sweepMin, sweepMax, item.min, item.max))
                        {
                            continue;
                        }

                        collision = FixedPointIntersection.IntersectWithSphereCastAndCylinder(
                            origin, direction, length, radius,
                            item.startPos, item.endPos, item.scaledRadius);
                        SelectClosestSphereCastHit(item, collision,
                            ref closestDistance, ref centerAtHit, ref hitNormal, ref hitCollider);
                    }
                }

                if (node.FpCharacterColliders != null)
                {
                    for (var i = 0; i < node.FpCharacterColliders.Count; i++)
                    {
                        var item = node.FpCharacterColliders[i];
                        if (item == ignoreCollider) continue;
                        if (IsNodeInValidate(item, layerMask, includeTrigger,
                                sweepMin, sweepMax, item.min, item.max))
                        {
                            continue;
                        }

                        collision = item.characterColliderType == CharacterCollider.Sphere
                            ? IntersectSphereCastWithSphere(
                                origin, direction, length, radius, item.position, item.scaledRadius)
                            : IntersectSphereCastWithCapsule(
                                origin, direction, length, radius,
                                item.startPos, item.endPos, item.scaledRadius);
                        SelectClosestSphereCastHit(item, collision,
                            ref closestDistance, ref centerAtHit, ref hitNormal, ref hitCollider);
                    }
                }

                if (node.FpMeshColliders != null)
                {
                    for (var i = 0; i < node.FpMeshColliders.Count; i++)
                    {
                        var item = node.FpMeshColliders[i];
                        if (item == ignoreCollider) continue;
                        if (IsNodeInValidate(item, layerMask, includeTrigger,
                                sweepMin, sweepMax, item.min, item.max))
                        {
                            continue;
                        }

                        item.CollectTriangleCandidates(sweepMin, sweepMax, m_meshTriangleCandidates);
                        foreach (var triangleIndex in m_meshTriangleCandidates)
                        {
                            item.GetWorldTriangle(triangleIndex, out var a, out var b, out var c);
                            collision = FixedPointIntersection.IntersectWithSphereCastAndTriangle(
                                origin, direction, length, radius, a, b, c);
                            SelectClosestSphereCastHit(item, collision,
                                ref closestDistance, ref centerAtHit, ref hitNormal, ref hitCollider);
                        }
                    }
                }

                if (node.nodes == null)
                {
                    continue;
                }

                foreach (var child in node.nodes)
                {
                    if (child.colliderCount <= 0)
                    {
                        continue;
                    }

                    var expandedMin = child.fixedPointAABB.Min - radiusVector;
                    var expandedMax = child.fixedPointAABB.Max + radiusVector;
                    if (FixedPointIntersection.PointInAABB(origin, expandedMin, expandedMax) ||
                        FixedPointIntersection.IntersectWithRayAndAABBFixedPoint(
                            origin, delta, expandedMin, expandedMax, out collision) != FixedPoint64.MaxValue)
                    {
                        physicsSearch.openList.Add(child);
                    }
                }
            }

            searchPool.Push(physicsSearch);

            if (hitCollider == null)
            {
                return false;
            }

            var contactPoint = centerAtHit - hitNormal * radius;
            fpRaycastHit = new FPRaycastHit(
                hitCollider, contactPoint, hitNormal, FixedPointVector3.zero, closestDistance);
            return true;
        }

        private static FPCollision IntersectSphereCastWithSphere(
            FixedPointVector3 origin,
            FixedPointVector3 direction,
            FixedPoint64 length,
            FixedPoint64 castRadius,
            FixedPointVector3 targetCenter,
            FixedPoint64 targetRadius)
        {
            var collision = FixedPointIntersection.IntersectWithSphereAndSphere(
                origin, castRadius, targetCenter, targetRadius);

            if (collision.hit)
            {
                collision.t = FixedPoint64.Zero;
                collision.closestPoint = origin;
                return collision;
            }

            FixedPointIntersection.IntersetWithRayAndSphereFixedPoint(
                origin, direction, length, targetCenter, castRadius + targetRadius, out collision);
            return collision;
        }

        private static FPCollision IntersectSphereCastWithCapsule(
            FixedPointVector3 origin,
            FixedPointVector3 direction,
            FixedPoint64 length,
            FixedPoint64 castRadius,
            FixedPointVector3 capsuleStart,
            FixedPointVector3 capsuleEnd,
            FixedPoint64 capsuleRadius)
        {
            var collision = FixedPointIntersection.IntersectWithSphereAndCapsule(
                origin, castRadius, capsuleStart, capsuleEnd, capsuleRadius);

            if (collision.hit)
            {
                collision.t = FixedPoint64.Zero;
                collision.closestPoint = origin;
                return collision;
            }

            return FixedPointIntersection.IntersectWithRayAndCapsule(
                origin, direction, length, capsuleStart, capsuleEnd, castRadius + capsuleRadius);
        }

        private static void SelectClosestSphereCastHit(
            FPCollider collider,
            FPCollision collision,
            ref FixedPoint64 closestDistance,
            ref FixedPointVector3 centerAtHit,
            ref FixedPointVector3 hitNormal,
            ref FPCollider hitCollider)
        {
            if (!collision.hit || collision.t >= closestDistance)
            {
                return;
            }

            closestDistance = collision.t;
            centerAtHit = collision.closestPoint;
            hitNormal = collision.normal;
            hitCollider = collider;
        }

        /// <summary>收集射线命中的全部碰撞器，并按命中距离从近到远排序。</summary>
        public int RaycastAll(
            FixedPointVector3 origin,
            FixedPointVector3 direct,
            FixedPoint64 length,
            List<FPRaycastHit> results,
            int layerMask = -1,
            bool includeTrigger = false)
        {
            if (results == null) return 0;
            results.Clear();
            if (direct.IsZero() || length < FixedPoint64.Zero) return 0;

            var direction = direct.normalized;
            var end = origin + direction * length;
            fpOctree.CollectCandidates(
                FixedPointVector3.Min(origin, end), FixedPointVector3.Max(origin, end),
                m_queryColliderCandidates, layerMask, includeTrigger);
            foreach (var collider in m_queryColliderCandidates)
            {
                if (!TryRaycastCollider(collider, origin, direction, length, out var collision))
                {
                    continue;
                }

                InsertSortedHit(results, new FPRaycastHit(
                    collider, collision.closestPoint, collision.normal,
                    collision.outsidePoint, collision.t));
            }

            return results.Count;
        }

        /// <summary>
        /// 将射线命中结果写入调用方提供的数组，并按命中距离从近到远排序。
        /// 已存在的结果对象会被复用；空槽位只在首次命中写入时创建对象。
        /// </summary>
        public int RaycastNonAlloc(
            FixedPointVector3 origin,
            FixedPointVector3 direct,
            FixedPoint64 length,
            FPRaycastHit[] results,
            int layerMask = -1,
            bool includeTrigger = false)
        {
            if (results == null || results.Length == 0 || direct.IsZero() ||
                length < FixedPoint64.Zero)
            {
                return 0;
            }

            var count = 0;
            var direction = direct.normalized;
            var end = origin + direction * length;
            fpOctree.CollectCandidates(
                FixedPointVector3.Min(origin, end), FixedPointVector3.Max(origin, end),
                m_queryColliderCandidates, layerMask, includeTrigger);
            foreach (var collider in m_queryColliderCandidates)
            {
                if (!TryRaycastCollider(collider, origin, direction, length, out var collision))
                {
                    continue;
                }

                InsertSortedHit(results, ref count, collider, collision.closestPoint,
                    collision.normal, collision.outsidePoint, collision.t);
            }

            return count;
        }

        /// <summary>收集球体扫掠命中的全部碰撞器，并按命中距离从近到远排序。</summary>
        public int SphereCastAll(
            FixedPointVector3 origin,
            FixedPoint64 radius,
            FixedPointVector3 direct,
            FixedPoint64 length,
            List<FPRaycastHit> results,
            int layerMask = -1,
            bool includeTrigger = false,
            FPCollider ignoreCollider = null)
        {
            if (results == null) return 0;
            results.Clear();
            if (radius < FixedPoint64.Zero || direct.IsZero() || length < FixedPoint64.Zero) return 0;

            var direction = direct.normalized;
            var end = origin + direction * length;
            var radiusVector = FixedPointVector3.one * radius;
            fpOctree.CollectCandidates(
                FixedPointVector3.Min(origin, end) - radiusVector,
                FixedPointVector3.Max(origin, end) + radiusVector,
                m_queryColliderCandidates, layerMask, includeTrigger);
            foreach (var collider in m_queryColliderCandidates)
            {
                if (collider == ignoreCollider ||
                    !TrySphereCastCollider(collider, origin, radius, direction, length, out var collision))
                {
                    continue;
                }

                var point = collision.closestPoint - collision.normal * radius;
                InsertSortedHit(results, new FPRaycastHit(
                    collider, point, collision.normal, FixedPointVector3.zero, collision.t));
            }

            return results.Count;
        }

        /// <summary>
        /// 将球体扫掠命中结果写入调用方提供的数组，并按命中距离从近到远排序。
        /// </summary>
        public int SphereCastNonAlloc(
            FixedPointVector3 origin,
            FixedPoint64 radius,
            FixedPointVector3 direct,
            FixedPoint64 length,
            FPRaycastHit[] results,
            int layerMask = -1,
            bool includeTrigger = false,
            FPCollider ignoreCollider = null)
        {
            if (results == null || results.Length == 0 || radius < FixedPoint64.Zero ||
                direct.IsZero() || length < FixedPoint64.Zero)
            {
                return 0;
            }

            var count = 0;
            var direction = direct.normalized;
            var end = origin + direction * length;
            var radiusVector = FixedPointVector3.one * radius;
            fpOctree.CollectCandidates(
                FixedPointVector3.Min(origin, end) - radiusVector,
                FixedPointVector3.Max(origin, end) + radiusVector,
                m_queryColliderCandidates, layerMask, includeTrigger);
            foreach (var collider in m_queryColliderCandidates)
            {
                if (collider == ignoreCollider ||
                    !TrySphereCastCollider(collider, origin, radius, direction, length, out var collision))
                {
                    continue;
                }

                var point = collision.closestPoint - collision.normal * radius;
                InsertSortedHit(results, ref count, collider, point, collision.normal,
                    FixedPointVector3.zero, collision.t);
            }

            return count;
        }

        private bool TryRaycastCollider(
            FPCollider collider,
            FixedPointVector3 origin,
            FixedPointVector3 direction,
            FixedPoint64 length,
            out FPCollision collision)
        {
            if (collider is FPSphereCollider sphere)
            {
                return FixedPointIntersection.IntersetWithRayAndSphereFixedPoint(
                    origin, direction, length, sphere.position, sphere.scaledRadius, out collision);
            }

            if (collider is FPBoxCollider obb)
            {
                if (IsPointStrictlyInsideObb(
                        origin, obb.position, obb.halfSize, obb.fpTransform.fixedPointMatrix))
                {
                    collision = default;
                    return false;
                }

                return FixedPointIntersection.IntersectWithRayAndOBBFixedPoint(
                           origin, direction, length, obb.position, obb.halfSize,
                           obb.fpTransform.fixedPointMatrix, out collision) >= FixedPoint64.Zero;
            }

            if (collider is FPAABBCollider aabb)
            {
                if (IsPointStrictlyInsideAabb(origin, aabb.min, aabb.max))
                {
                    collision = default;
                    return false;
                }

                return FixedPointIntersection.IntersectWithRayAndAABBFixedPointA(
                    origin, direction, length, aabb.min, aabb.max, out collision);
            }

            if (collider is FPCapsuleCollider capsule)
            {
                return TryRaycastCapsule(origin, direction, length,
                    capsule.startPos, capsule.endPos, capsule.scaledRadius, out collision);
            }

            if (collider is FPAACapsuleCollider aaCapsule)
            {
                return TryRaycastCapsule(origin, direction, length,
                    aaCapsule.startPos, aaCapsule.endPos, aaCapsule.scaledRadius, out collision);
            }

            if (collider is FPCylinderCollider cylinder)
            {
                collision = FixedPointIntersection.IntersectWithRayAndCylinder(
                    origin, direction, length,
                    cylinder.startPos, cylinder.endPos, cylinder.scaledRadius);
                return collision.hit;
            }

            if (collider is FPCharacterController character)
            {
                if (character.characterColliderType == CharacterCollider.Sphere)
                {
                    return FixedPointIntersection.IntersetWithRayAndSphereFixedPoint(
                        origin, direction, length, character.position, character.scaledRadius,
                        out collision);
                }

                return TryRaycastCapsule(origin, direction, length,
                    character.startPos, character.endPos, character.scaledRadius, out collision);
            }

            if (collider is FPMeshCollider mesh)
            {
                var end = origin + direction * length;
                mesh.CollectTriangleCandidates(
                    FixedPointVector3.Min(origin, end), FixedPointVector3.Max(origin, end),
                    m_meshTriangleCandidates);
                var bestDistance = FixedPoint64.MaxValue;
                var best = new FPCollision();
                foreach (var triangleIndex in m_meshTriangleCandidates)
                {
                    mesh.GetWorldTriangle(triangleIndex, out var a, out var b, out var c);
                    if (FixedPointIntersection.IntersectWithRayAndTriangleFixedPoint(
                            origin, direction, FixedPointVector3.zero, a, b, c, out var current) &&
                        current.t <= length && current.t < bestDistance)
                    {
                        bestDistance = current.t;
                        best = current;
                    }
                }

                collision = best;
                return best.hit;
            }

            collision = default;
            return false;
        }

        private static bool TryRaycastCapsule(
            FixedPointVector3 origin,
            FixedPointVector3 direction,
            FixedPoint64 length,
            FixedPointVector3 start,
            FixedPointVector3 end,
            FixedPoint64 radius,
            out FPCollision collision)
        {
            if (IsPointStrictlyInsideCapsule(origin, start, end, radius))
            {
                collision = default;
                return false;
            }

            collision = FixedPointIntersection.IntersectWithRayAndCapsule(
                origin, direction, length, start, end, radius);
            return collision.hit;
        }

        private bool TrySphereCastCollider(
            FPCollider collider,
            FixedPointVector3 origin,
            FixedPoint64 radius,
            FixedPointVector3 direction,
            FixedPoint64 length,
            out FPCollision collision)
        {
            if (collider is FPSphereCollider sphere)
            {
                collision = IntersectSphereCastWithSphere(
                    origin, direction, length, radius, sphere.position, sphere.scaledRadius);
                return collision.hit;
            }

            if (collider is FPBoxCollider obb)
            {
                collision = FixedPointIntersection.IntersectWithRayAndRoundedOBB(
                    origin, direction, length, obb.position, obb.halfSize,
                    obb.fpTransform.fixedPointMatrix, radius);
                return collision.hit;
            }

            if (collider is FPAABBCollider aabb)
            {
                collision = FixedPointIntersection.IntersectWithRayAndRoundedAABB(
                    origin, direction, length, aabb.min, aabb.max, radius);
                return collision.hit;
            }

            if (collider is FPCapsuleCollider capsule)
            {
                collision = IntersectSphereCastWithCapsule(
                    origin, direction, length, radius,
                    capsule.startPos, capsule.endPos, capsule.scaledRadius);
                return collision.hit;
            }

            if (collider is FPAACapsuleCollider aaCapsule)
            {
                collision = IntersectSphereCastWithCapsule(
                    origin, direction, length, radius,
                    aaCapsule.startPos, aaCapsule.endPos, aaCapsule.scaledRadius);
                return collision.hit;
            }

            if (collider is FPCylinderCollider cylinder)
            {
                collision = FixedPointIntersection.IntersectWithSphereCastAndCylinder(
                    origin, direction, length, radius,
                    cylinder.startPos, cylinder.endPos, cylinder.scaledRadius);
                return collision.hit;
            }

            if (collider is FPCharacterController character)
            {
                collision = character.characterColliderType == CharacterCollider.Sphere
                    ? IntersectSphereCastWithSphere(
                        origin, direction, length, radius,
                        character.position, character.scaledRadius)
                    : IntersectSphereCastWithCapsule(
                        origin, direction, length, radius,
                        character.startPos, character.endPos, character.scaledRadius);
                return collision.hit;
            }

            if (collider is FPMeshCollider mesh)
            {
                var end = origin + direction * length;
                var radiusVector = new FixedPointVector3(radius, radius, radius);
                mesh.CollectTriangleCandidates(
                    FixedPointVector3.Min(origin, end) - radiusVector,
                    FixedPointVector3.Max(origin, end) + radiusVector,
                    m_meshTriangleCandidates);
                var bestDistance = FixedPoint64.MaxValue;
                var best = new FPCollision();
                foreach (var triangleIndex in m_meshTriangleCandidates)
                {
                    mesh.GetWorldTriangle(triangleIndex, out var a, out var b, out var c);
                    var current = FixedPointIntersection.IntersectWithSphereCastAndTriangle(
                        origin, direction, length, radius, a, b, c);
                    if (current.hit && current.t < bestDistance)
                    {
                        bestDistance = current.t;
                        best = current;
                    }
                }

                collision = best;
                return best.hit;
            }

            collision = default;
            return false;
        }

        private static void InsertSortedHit(List<FPRaycastHit> results, FPRaycastHit hit)
        {
            var index = results.Count;
            while (index > 0 && results[index - 1].distance > hit.distance) index--;
            results.Insert(index, hit);
        }

        private static void InsertSortedHit(
            FPRaycastHit[] results,
            ref int count,
            FPCollider collider,
            FixedPointVector3 point,
            FixedPointVector3 normal,
            FixedPointVector3 outPoint,
            FixedPoint64 distance)
        {
            int index;
            if (count < results.Length)
            {
                index = count;
                count++;
            }
            else
            {
                index = count - 1;
                if (results[index].distance <= distance) return;
            }

            if (results[index] == null)
            {
                results[index] = new FPRaycastHit(collider, point, normal, outPoint, distance);
            }
            else
            {
                results[index].Set(collider, point, normal, outPoint, distance);
            }

            while (index > 0 && results[index - 1].distance > results[index].distance)
            {
                (results[index - 1], results[index]) = (results[index], results[index - 1]);
                index--;
            }
        }

        /// <summary>
        /// 检测指定球体与当前物理世界中碰撞器的碰撞，并复用调用方提供的结果列表。
        /// </summary>
        /// <param name="position">查询球体中心。</param>
        /// <param name="radius">查询球体半径。</param>
        /// <param name="collisions">接收结果的可复用列表；有效结果位于索引 <c>[0, 返回值)</c>。</param>
        /// <param name="layerMask">参与查询的层掩码；<c>-1</c> 或 <c>0</c> 表示不限制层。</param>
        /// <param name="includeTrigger">是否包含触发器。</param>
        /// <returns>写入的有效碰撞结果数量。</returns>
        /// <remarks>当前实现检测球、AABB、OBB、胶囊体、圆柱体、轴对齐胶囊体、网格和角色控制器。</remarks>
        public int OverlaySphereCollision(FixedPointVector3 position, FixedPoint64 radius,ref List<FPCollision> collisions, int layerMask = -1, bool includeTrigger = false)
        {
            var count = 0;
            var physicsSearch = searchPool.Pull();
            physicsSearch.openList.Clear();
            physicsSearch.openList.Add(fpOctree.root);
            var min = position - new FixedPointVector3(radius,radius,radius);
            var max = position + new FixedPointVector3(radius, radius, radius);
            while (physicsSearch.openList.Count > 0)
            {
                var node =  physicsSearch.openList[0];
                physicsSearch.openList.Remove(node);
                FPCollision fpCollision;
                if (node.FpSphereColliders != null)
                {
                    for (var i = 0; i < node.FpSphereColliders.Count; i++)
                    {
                        var item = node.FpSphereColliders[i];
                        if (IsNodeInValidate(item, layerMask, includeTrigger))
                        {
                            continue;
                        }
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
                        if (IsNodeInValidate(item, layerMask, includeTrigger))
                        {
                            continue;
                        }
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
                        if (IsNodeInValidate(item, layerMask, includeTrigger,min,max,item.min,item.max))
                        {
                            continue;
                        }
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
                        if (IsNodeInValidate(item, layerMask, includeTrigger,min,max,item.min,item.max))
                        {
                            continue;
                        }
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
                        if (IsNodeInValidate(item, layerMask, includeTrigger,min,max,item.min,item.max))
                        {
                            continue;
                        }
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
                        if (IsNodeInValidate(item, layerMask, includeTrigger,min,max,item.min,item.max))
                        {
                            continue;
                        }
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
                if (node.FpMeshColliders != null)
                {
                    for (var i = 0; i < node.FpMeshColliders.Count; i++)
                    {
                        var item = node.FpMeshColliders[i];
                        if (IsNodeInValidate(item, layerMask, includeTrigger,min,max,item.min,item.max))
                        {
                            continue;
                        }
                        fpCollision = FixedPointIntersection.IntersectWithSphereAndMesh(position, radius, item);
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
                        if (IsNodeInValidate(item, layerMask, includeTrigger, min, max, item.min, item.max))
                        {
                            continue;
                        }
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
                if (node.nodes == null) continue;
                {
                    foreach (var item in node.nodes)
                    {
                        if(item.colliderCount <= 0) continue;
                        if (FixedPointIntersection.IntersectWithAABBAndSphere(item.fixedPointAABB.Min, item.fixedPointAABB.Max, position, radius))
                        {
                            physicsSearch.openList.Add(item);
                        }
                    }
                }
            }
            searchPool.Push(physicsSearch);
            return count;
        }

        #endregion
    }
}
