using System;
using System.Collections.Generic;

namespace DGame.FixedPoint
{
    /// <summary>
    /// 使用定点数进行线性运动、球形碰撞约束和滚动旋转计算的刚体。
    /// </summary>
    /// <remarks>
    /// 当前实现只绑定 <see cref="FPSphereCollider"/>，构造后会自动注册到指定的
    /// <see cref="FPPhysicsContext"/>。静态碰撞器由 <see cref="SolveConstraints"/> 处理，
    /// 角色和其他刚体之间的碰撞由物理上下文统一求解。
    /// </remarks>
    public sealed class FPRigidbody : FastListItem
    {
        /// <summary>
        /// 获取或设置刚体启用标记。
        /// </summary>
        public bool enable = true;

        /// <summary>
        /// 获取或设置是否在施力阶段应用重力。
        /// </summary>
        public bool useGravity { get; set; } = true;

        /// <summary>
        /// 当前质量。
        /// </summary>
        private FixedPoint64 _mass = 1;

        /// <summary>
        /// 获取质量倒数。质量为零时该值为零。
        /// </summary>
        public FixedPoint64 invMass { get; private set; } = 1;

        /// <summary>
        /// 获取或设置刚体质量，并同步更新 <see cref="invMass"/>。
        /// </summary>
        /// <remarks>质量为零时按无限质量处理。</remarks>
        /// <exception cref="ArgumentOutOfRangeException">质量小于零。</exception>
        public FixedPoint64 mass {
            get => _mass;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "刚体质量不能小于零。");
                }

                _mass = value;
                if (_mass == 0)
                {
                    invMass = 0;
                }
                else
                {
                    invMass = 1 / _mass;
                }
            }
        }

        /// <summary>
        /// 获取或设置刚体的世界空间线速度。
        /// </summary>
        public FixedPointVector3 velocity;

        /// <summary>
        /// 获取或设置当前物理步累积的合力。
        /// </summary>
        /// <remarks>完成一次 <see cref="OnUpdate"/> 后会自动清零。</remarks>
        public FixedPointVector3 force;

        /// <summary>
        /// 当前物理步积分位移与碰撞修正位移之和，用于计算球体滚动旋转。
        /// </summary>
        private FixedPointVector3 deltaMove;

        /// <summary>
        /// 获取或设置刚体的碰撞恢复系数，取值范围为 0 到 1。
        /// </summary>
        public FixedPoint64 cor
        {
            get => _cor;
            set => _cor = FixedPointMath.Clamp(value, FixedPoint64.Zero, FixedPoint64.One);
        }

        private FixedPoint64 _cor = 0.5;

        /// <summary>
        /// 获取或设置是否禁止根据位移自动计算球体滚动旋转。
        /// </summary>
        public bool constrain;

        /// <summary>
        /// 每次碰撞响应后应用的速度阻尼倍率。
        /// </summary>
        private readonly FixedPoint64 damping = 1;

        /// <summary>
        /// 当前物理步累计的位置修正量。
        /// </summary>
        private FixedPointVector3 constraint;

        /// <summary>
        /// 复用的碰撞查询结果列表。
        /// </summary>
        private List<FPCollision> collisions = new List<FPCollision>();

        /// <summary>
        /// 获取或设置参与碰撞求解的目标层掩码。
        /// </summary>
        public int targetLayerMask { get; set; } = 1 << 0;

        /// <summary>
        /// 获取或设置参与碰撞求解的目标层掩码。
        /// </summary>
        /// <remarks>保留该属性用于兼容旧拼写，新代码应使用 <see cref="targetLayerMask"/>。</remarks>
        [Obsolete("请使用 targetLayerMask。")]
        public int targetTargetMask
        {
            get => targetLayerMask;
            set => targetLayerMask = value;
        }

        /// <summary>
        /// 获取刚体绑定的球形碰撞器。
        /// </summary>
        public FPSphereCollider collider { get; private set; }

        /// <summary>
        /// 获取刚体驱动的定点变换。
        /// </summary>
        public FPTransform transform { get; private set; }

        /// <summary>
        /// 获取构造时注入的所属物理上下文。
        /// </summary>
        public FPPhysicsContext context { get; private set; }

        /// <summary>
        /// 获取刚体当前是否可以参与模拟。
        /// </summary>
        internal bool CanSimulate => enable && collider != null && collider.enabled;

        /// <summary>
        /// 创建球形定点刚体，并将其注册到指定物理上下文。
        /// </summary>
        /// <param name="collider">刚体使用的球形碰撞器。</param>
        /// <param name="transform">刚体驱动的定点变换。</param>
        /// <param name="context">负责更新该刚体的物理上下文。</param>
        public FPRigidbody(FPSphereCollider collider, FPTransform transform, FPPhysicsContext context)
        {
            this.collider = collider ?? throw new ArgumentNullException(nameof(collider));
            this.transform = transform ?? throw new ArgumentNullException(nameof(transform));
            this.context = context ?? throw new ArgumentNullException(nameof(context));

            collider.fpTransform = transform;
            collider.SetFpPhysicsContext(context);
            collider.isDynamic = true;
            context.AddCollider(collider);
            collider.UpdateCollider();
            transform.colliderUpdateFlag = false;
            context.AddRigidbody(this);
        }

        /// <summary>
        /// 检测并求解两个球形刚体之间的碰撞。
        /// </summary>
        /// <param name="ra">第一个刚体。</param>
        /// <param name="rb">第二个刚体。</param>
        public void FindCollisionFeatures(FPRigidbody ra,FPRigidbody rb)
        {
            ResolveCollision(ra, rb);
        }

        /// <summary>
        /// 检测并求解两个球形刚体之间的碰撞。
        /// </summary>
        /// <param name="ra">第一个刚体。</param>
        /// <param name="rb">第二个刚体。</param>
        internal static void ResolveCollision(FPRigidbody ra, FPRigidbody rb)
        {
            if (ra == null)
            {
                throw new ArgumentNullException(nameof(ra));
            }
            if (rb == null)
            {
                throw new ArgumentNullException(nameof(rb));
            }
            if (ReferenceEquals(ra, rb) || !ra.CanSimulate || !rb.CanSimulate ||
                !ReferenceEquals(ra.context, rb.context) || ra.collider.isTrigger || rb.collider.isTrigger)
            {
                return;
            }
            if (!GridLayerMask.ValidateLayerMask(ra.targetLayerMask, 1 << rb.collider.layer) ||
                !GridLayerMask.ValidateLayerMask(rb.targetLayerMask, 1 << ra.collider.layer))
            {
                return;
            }

            var radiusA = ra.collider.scaledRadius;
            var radiusB = rb.collider.scaledRadius;
            if (radiusA <= 0 || radiusB <= 0)
            {
                return;
            }

            var offset = ra.collider.position - rb.collider.position;
            var sqrDistance = offset.sqrMagnitude;
            var radiusSum = radiusA + radiusB;
            if (sqrDistance > radiusSum * radiusSum)
            {
                return;
            }

            var distance = FixedPointMath.Sqrt(sqrDistance);
            var normal = distance > 0 ? offset / distance : FixedPointVector3.up;
            var penetration = FixedPointMath.Max(FixedPoint64.Zero, radiusSum - distance);
            var collision = new FPCollision
            {
                hit = true,
                normal = normal,
                depth = penetration * FixedPoint64.Half
            };

            var invMassSum = ra.invMass + rb.invMass;
            if (penetration > 0 && invMassSum > 0)
            {
                var correction = normal * penetration;
                ra.AddConstraints(correction * (ra.invMass / invMassSum));
                rb.AddConstraints(-correction * (rb.invMass / invMassSum));
            }

            ra.ApplyImpulse(ra, rb, collision);
        }

        /// <summary>
        /// 根据碰撞法线、质量倒数和恢复系数，对两个刚体施加线性碰撞冲量。
        /// </summary>
        /// <param name="ra">碰撞中的第一个刚体。</param>
        /// <param name="rb">碰撞中的第二个刚体。</param>
        /// <param name="collision">包含碰撞法线的碰撞结果。</param>
        public void ApplyImpulse(FPRigidbody ra, FPRigidbody rb, FPCollision collision)
        {
            if (ra == null)
            {
                throw new ArgumentNullException(nameof(ra));
            }
            if (rb == null)
            {
                throw new ArgumentNullException(nameof(rb));
            }
            if (!collision.hit || collision.normal.sqrMagnitude == FixedPoint64.Zero)
            {
                return;
            }

            var invMass1 = ra.invMass;
            var invMass2 = rb.invMass;
            var invMassSum = invMass1 + invMass2;
            if (invMassSum == 0)
            {
                return;
            }

            var relativeNorm = collision.normal.normalized;
            var relativeVel = ra.velocity - rb.velocity;
            var normalVelocity = FixedPointVector3.Dot(relativeVel, relativeNorm);
            if (normalVelocity >= 0)
            {
                return;
            }

            var e = FixedPointMath.Min(ra.cor,rb.cor);
            var numerator = -(1 + e) * normalVelocity;
            var j = numerator / invMassSum;
            var impulse = relativeNorm * j;
            ra.velocity += impulse * invMass1;
            rb.velocity -= impulse * invMass2;
        }

        /// <summary>
        /// 根据碰撞法线、质量倒数和恢复系数，对两个刚体施加线性碰撞冲量。
        /// </summary>
        /// <param name="ra">碰撞中的第一个刚体。</param>
        /// <param name="rb">碰撞中的第二个刚体。</param>
        /// <param name="collision">包含碰撞法线的碰撞结果。</param>
        /// <param name="contactCount">兼容旧接口的接触点数量；当前球形刚体求解不需要该值。</param>
        [Obsolete("请使用不带 contactCount 参数的 ApplyImpulse 重载。")]
        public void ApplyImpulse(FPRigidbody ra, FPRigidbody rb, FPCollision collision, int contactCount)
        {
            ApplyImpulse(ra, rb, collision);
        }

        /// <summary>
        /// 向刚体施加线性冲量。
        /// </summary>
        /// <param name="impulse">需要施加的线性冲量。</param>
        public void AddLinearImpulse(FixedPointVector3 impulse)
        {
            velocity += impulse * invMass;
        }

        /// <summary>
        /// 向当前物理步累加一个力。
        /// </summary>
        /// <param name="additionalForce">需要累加的力。</param>
        public void AddForce(FixedPointVector3 additionalForce)
        {
            force += additionalForce;
        }

        /// <summary>
        /// 准备当前物理步使用的合力。
        /// </summary>
        /// <remarks>
        /// 启用重力时，将质量乘以重力加速度累加到 <see cref="force"/>。
        /// </remarks>
        public void ApplyForces()
        {
            if (CanSimulate && useGravity && invMass > 0)
            {
                force += FPPhysicsContext.GravitationalAcceleration * mass;
            }
        }

        /// <summary>
        /// 检测并求解刚体与静态碰撞器之间的位置约束和速度响应。
        /// </summary>
        /// <remarks>求解完成后会根据本物理步总位移更新球体滚动旋转，并清空累计约束。</remarks>
        public void SolveConstraints()
        {
            if (!CanSimulate || invMass == 0)
            {
                constraint = FixedPointVector3.zero;
                deltaMove = FixedPointVector3.zero;
                return;
            }

            var scaledRadius = collider.scaledRadius;
            var count = context.fpOctree.OverlaySphereCollision(collider.position, scaledRadius, ref collisions,
                targetLayerMask);
            for (var i = 0; i < count; i++)
            {
                if (collisions[i].collider == collider || !collisions[i].hit) continue;
                AddConstraints(collisions[i].normal * (collisions[i].depth * 2));
                AdjustVelocityByCollision(collisions[i].normal, collisions[i].collider.rebound);
            }
            transform.position += constraint;
            deltaMove += constraint;
            if (!constrain && scaledRadius > 0)
            {
                var planarMove = new FixedPointVector3(deltaMove.x, 0, deltaMove.z);
                if (planarMove.sqrMagnitude > FixedPoint64.EN8)
                {
                    var axis = FixedPointVector3.Cross(planarMove.normalized, FixedPointVector3.up);
                    var angle = planarMove.magnitude / scaledRadius * FixedPoint64.Rad2Deg;
                    transform.rotation = FixedPointQuaternion.AngleAxis(-angle, axis) * transform.rotation;
                }
            }
            constraint = FixedPointVector3.zero;
        }

        /// <summary>
        /// 将新的位置修正量合并到当前物理步的累计约束中。
        /// </summary>
        /// <param name="additionalConstraint">需要合并的位置修正向量。</param>
        public void AddConstraints(FixedPointVector3 additionalConstraint)
        {
            if (additionalConstraint == FixedPointVector3.zero)
            {
                return;
            }

            if (constraint == FixedPointVector3.zero)
            {
                constraint = additionalConstraint;
            }
            else
            {
                var constraintNormal = constraint.normalized;
                var magnitude = constraint.magnitude;
                var dot = FixedPointVector3.Dot(additionalConstraint, constraintNormal);
                if (dot > magnitude)
                {
                    constraint = additionalConstraint;
                }
                else if (dot > 0)
                {
                    constraint = constraint + additionalConstraint - constraintNormal * dot;
                }
                else
                {
                    constraint = constraint + additionalConstraint;
                }
            }
        }

        /// <summary>
        /// 根据当前合力积分速度和位置，推进一个固定物理步。
        /// </summary>
        /// <remarks>刚体当前位置超出八叉树逻辑边界时，本次更新会被跳过。</remarks>
        public void OnUpdate()
        {
            if (!CanSimulate || invMass == 0 || context.fpOctree.IsOutOfBound(transform.position))
            {
                force = FixedPointVector3.zero;
                deltaMove = FixedPointVector3.zero;
                return;
            }
            var acceleration = force * invMass;
            velocity = velocity + acceleration * context.DeltaTime;
            deltaMove = velocity * context.DeltaTime;
            transform.position += deltaMove;
            force = FixedPointVector3.zero;
        }

        /// <summary>
        /// 移除速度中朝向碰撞面的分量，并应用碰撞恢复系数和阻尼。
        /// </summary>
        /// <param name="constraintNormal">指向刚体分离方向的单位法线。</param>
        /// <param name="rebound">被碰撞对象的恢复系数。</param>
        internal void AdjustVelocityByCollision(FixedPointVector3 constraintNormal, FixedPoint64 rebound)
        {
            if (constraintNormal.sqrMagnitude == FixedPoint64.Zero)
            {
                return;
            }

            constraintNormal = constraintNormal.normalized;
            rebound = FixedPointMath.Clamp(rebound, FixedPoint64.Zero, FixedPoint64.One);
            var dot = FixedPointVector3.Dot(velocity, constraintNormal);
            if (dot < 0)
            {
                velocity = velocity - constraintNormal * dot * (1 + rebound);
            }
            velocity *= damping;
        }

        /// <summary>
        /// 获取或设置该刚体在所属 <see cref="FastList{FPRigidbody}"/> 中的索引。
        /// </summary>
        public int index { get; set; } = -1;
    }
}
