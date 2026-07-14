using System;

namespace DGame.FixedPoint
{
    /// <summary>角色控制器使用的碰撞形状。</summary>
    public enum CharacterCollider
    {
        /// <summary>球形。</summary>
        Sphere,

        /// <summary>轴对齐胶囊形。</summary>
        Capsule
    }

    /// <summary>
    /// 定点数角色控制器。纯逻辑部分由客户端和服务端共用，Unity 表现部分位于同名 Unity 分部文件。
    /// 通过继承自 <see cref="FPCollider"/> 的 <see cref="FPCollider.context"/> 获取物理上下文。
    /// </summary>
    public sealed partial class FPCharacterController : FPAACapsuleCollider
    {
        /// <summary>角色控制器当前使用的碰撞形状。</summary>
        public CharacterCollider characterColliderType = CharacterCollider.Capsule;

        /// <summary>获取角色控制器碰撞器类型。</summary>
        public override ColliderType colliderType => ColliderType.CharacterController;

        /// <summary>
        /// 根据当前碰撞形状、世界尺寸和位置更新轴对齐包围盒。
        /// </summary>
        internal override void UpdateAABB()
        {
            var width = scaledRadius;
            var halfHeight = characterColliderType == CharacterCollider.Sphere
                ? width
                : FixedPointMath.Max(width, scaledHalfHeight);
            var halfSize = new FixedPointVector3(width, halfHeight, width);
            _min = position - halfSize;
            _max = position + halfSize;
        }

        /// <summary>
        /// 将角色控制器从当前八叉树节点的角色集合中移除。
        /// </summary>
        protected override void RemoveFromImpactNotes()
        {
            targetNode?.FpCharacterColliders.Remove(this);
            targetNode = null;
        }

        /// <summary>
        /// 将角色控制器加入指定八叉树节点的角色集合。
        /// </summary>
        protected override void AddToImpactNote(FPOctreeNode node)
        {
            node.FpCharacterColliders ??= new FPOctreeColliderSet<FPCharacterController>(node);
            node.FpCharacterColliders.Add(this);
            targetNode = node;
        }

        /// <summary>角色控制器的接地状态。</summary>
        public enum CharacterColliderState
        {
            /// <summary>处于地面。</summary>
            Ground,

            /// <summary>处于离地或下落状态。</summary>
            Fall
        }

        /// <summary>角色参与碰撞检测的目标层掩码。</summary>
        public int targetLayerMask { get; set; }

        /// <summary>空间查询过程中暂存的受影响节点数组。</summary>
        public FPOctreeNode[] impactNodeArray { get; set; } = new FPOctreeNode[8];

        /// <summary>受影响节点数组的当前写入索引。</summary>
        public int impactNodeIndex { get; set; } = -1;

        /// <summary>角色当前的接地状态。</summary>
        public CharacterColliderState colliderState { get; set; } = CharacterColliderState.Ground;

        private FixedPointVector3 impulse { get; set; }

        /// <summary>当前物理步累计的穿透修正向量。</summary>
        public FixedPointVector3 constraint { get; private set; }

        private FixedPointVector3 preConstraint { get; set; }

        /// <summary>获取或设置角色质量，质量必须大于零。</summary>
        /// <exception cref="ArgumentOutOfRangeException">设置值小于等于零。</exception>
        public FixedPoint64 mass
        {
            get => _mass;
            set
            {
                if (value <= FixedPoint64.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "角色质量必须大于零。");
                }

                _mass = value;
                reverseMass = 1 / _mass;
            }
        }

        private FixedPoint64 _mass = 1;

        /// <summary>获取角色质量的倒数。</summary>
        public FixedPoint64 reverseMass { get; private set; } = 1;

        // 角色基础质量，用于计算额外重力。
        private static readonly FixedPoint64 BaseMass = 1;

        /// <summary>
        /// 设置角色交互摩擦系数，并按固定物理步长换算为单步值。
        /// </summary>
        /// <param name="friction">非负摩擦系数；负值会按零处理。</param>
        public void SetInteractiveFriction(FixedPoint64 friction)
        {
            friction = FixedPointMath.Max(0, friction);
            interactiveFriction = friction * context.DeltaTime;
        }

        /// <summary>获取单物理步使用的交互摩擦值。</summary>
        public FixedPoint64 interactiveFriction { get; private set; } = 0.033;

        /// <summary>
        /// 设置角色交互加速度，并按固定物理步长换算为单步值。
        /// </summary>
        /// <param name="accelerate">非负加速度；负值会按零处理。</param>
        public void SetInteractiveAccelerate(FixedPoint64 accelerate)
        {
            accelerate = FixedPointMath.Max(0, accelerate);
            interactiveAccelerate = accelerate * context.DeltaTime;
        }

        /// <summary>获取单物理步使用的交互加速度。</summary>
        public FixedPoint64 interactiveAccelerate { get; private set; } = 0.033;

        /// <summary>击退速度每步允许衰减的最大比例。</summary>
        public FixedPoint64 frictionKnockBack { get; set; } = 0.1;

        /// <summary>击退阻尼系数。</summary>
        public FixedPoint64 dampKnockBackDamp { get; set; } = 1;

        /// <summary>角色可跨越的默认台阶高度。</summary>
        public static FixedPoint64 stepHeight => 1;

        /// <summary>角色连续离地的持续时间。</summary>
        public FixedPoint64 fallDuration { get; set; }

        /// <summary>角色当前是否接地。</summary>
        public bool isGround { get; set; }

        /// <summary>当前接触地面的合成法线。</summary>
        public FixedPointVector3 groundNormal { get; set; }

        private FixedPointVector3 _velocity;

        /// <summary>获取或设置角色速度，各分量会限制在定点数模拟的安全范围内。</summary>
        public FixedPointVector3 velocity
        {
            get => _velocity;
            set =>
                // 限制速度分量，避免后续平方和开方运算超出定点数安全范围。
                _velocity = new FixedPointVector3(FixedPointMath.Clamp(value.x, -5000, 5000),
                    FixedPointMath.Clamp(value.y, -5000, 5000),
                    FixedPointMath.Clamp(value.z, -5000, 5000));
        }

        private FixedPointVector3 currentForce { get; set; }

        /// <summary>当前物理步实际应用的合力。</summary>
        public FixedPointVector3 forces { get; set; }

        /// <summary>当前物理步计划移动的位移。</summary>
        public FixedPointVector3 deltaPosition { get; set; }

        /// <summary>角色落地时触发的回调。</summary>
        public Action onLand;

        /// <summary>角色离地时触发的回调。</summary>
        public Action onOffGround;

        /// <summary>角色发起跳跃时触发的回调。</summary>
        public Action onJump;

        /// <summary>角色完成单步移动时触发的回调，参数为本步位移。</summary>
        public Action<FixedPointVector3> onMove;

        /// <summary>角色输入移动逻辑维护的上一帧速度。</summary>
        public FixedPointVector3 preVelocity { get; set; }

        private const int diveForce = 200;

        /// <summary>俯冲时附加的纵向方向偏移，单位为千分之一。</summary>
        public int doubleJumpOffsetY { get; set; } = 100;

        private int doubleJumpCooldown { get; set; }

        /// <summary>距离允许再次跳跃的剩余冷却时间。</summary>
        public FixedPoint64 lastJump { get; set; }

        /// <summary>最近一次跳跃计算出的力。</summary>
        public FixedPointVector3 jumpForce { get; set; }

        /// <summary>角色处于击退状态时累计的击退速度。</summary>
        public FixedPointVector3 knockBackVelocity { get; set; }

        /// <summary>当前物理步施加到角色的击退力。</summary>
        public FixedPointVector3 knockBackForce { get; set; }

        /// <summary>角色受到击退时触发的回调。</summary>
        public Action<FixedPointVector3> onKnockBack;

        // 单步位移超过该阈值时分段检测静态碰撞器，避免跨越较薄的阻挡物。
        private static readonly FixedPoint64 ThresholdDeltaMove = 0.2;

        private static readonly FixedPoint64 JumpCooldown = 1;

        // 质量增加时允许附加的最大向下重力。
        private static readonly FixedPoint64 MaxAdditionGravity = 2;

        /// <summary>重置角色的全部瞬时运动和碰撞状态。</summary>
        public void Reset()
        {
            knockBackVelocity = FixedPointVector3.zero;
            knockBackForce = FixedPointVector3.zero;
            velocity = FixedPointVector3.zero;
            currentForce = FixedPointVector3.zero;
            forces = FixedPointVector3.zero;
            impulse = FixedPointVector3.zero;
            constraint = FixedPointVector3.zero;
            preConstraint = FixedPointVector3.zero;
            preVelocity = FixedPointVector3.zero;
            deltaPosition = FixedPointVector3.zero;
            jumpForce = FixedPointVector3.zero;
            fallDuration = FixedPoint64.Zero;
            lastJump = FixedPoint64.Zero;
            doubleJumpCooldown = 0;
        }

        /// <summary>
        /// 按指定跳跃高度计算并施加跳跃力。
        /// </summary>
        /// <param name="jumpHigh">非负目标跳跃高度。</param>
        /// <exception cref="ArgumentOutOfRangeException">目标跳跃高度小于零。</exception>
        public void Jump(FixedPoint64 jumpHigh)
        {
            ValidateNonNegative(jumpHigh, nameof(jumpHigh));
            lastJump = JumpCooldown;
            var t = FixedPointMath.Sqrt(
                FixedPointMath.Abs(jumpHigh * reverseMass / FPPhysicsContext.GravitationalAcceleration.y * 2));
            var v = t * FPPhysicsContext.GravitationalAcceleration.y;
            // 全程使用定点数，保证客户端与服务端结果一致。
            jumpForce = new FixedPointVector3(0, -(v / context.DeltaTime), 0);
            AddForce(jumpForce);
            onJump?.Invoke();
        }

        /// <summary>判断当前跳跃冷却是否结束。</summary>
        /// <returns>可以跳跃时返回 <see langword="true"/>。</returns>
        public bool JumpAble()
        {
            return lastJump <= 0;
        }

        /// <summary>
        /// 根据角色当前接地状态处理目标移动速度。
        /// </summary>
        /// <param name="velocity">目标移动速度。</param>
        public void Move(FixedPointVector3 velocity)
        {
            if (isGround)
            {
                MoveWithAccelerateFrictionOnGround(velocity);
                /*
                return;
                #region 根据地面法线调整移动方向，使角色沿斜坡移动
                var dot = FixedPointVector3.Dot(velocity, groundNormal);
                {
                    var speed = velocity.magnitude;
                    var newVelocity = velocity - groundNormal * dot;
                    velocity = newVelocity.normalized * speed;
                }
                #endregion
                if (groundPhysicsMaterial == FixedPointPhysicsMaterial.Ice)
                {
                    MoveWithAccelerateFrictionOnGround(velocity);
                }
                else
                {
                    var speed = preVelocity.magnitude;
                    var speed1 = velocity.magnitude;
                    var direct = velocity.normalized;
                    var direct1 = preVelocity.normalized;
                    if (speed1 < 0.01)
                    {
                        direct = direct1;
                    }
                    // 输入方向反转时，可按需要清除原有速度。
                    dot = FixedPointVector3.Dot(direct, direct1);
                    if (dot < -0.5)
                    {
                        // speed = 0;
                        // speed1 = 0;
                    }
                    // preVelocity = FixedPointMath.Lerp(speed, speed1, velocityFactor) * direct;
                    var right = FixedPointVector3.Cross(direct, groundNormal);
                    var forward = FixedPointVector3.Cross(groundNormal, right);
                    var newSpeed = speed1;
                    if (speed < speed1)
                    {
                        newSpeed = FixedPointMath.Lerp(speed, speed1, velocityFactor);
                    }
                    var newVelocity = newSpeed * forward;
                    AddImpulse(preVelocity);
                    preVelocity = newVelocity;
                }*/
            }
            else
            {
                MoveWithAccelerateFrictionOffGround(velocity);
                /*
                if (groundPhysicsMaterial == FixedPointPhysicsMaterial.Ice)
                {
                    // 模拟冰面滑动。
                    var speed = preVelocity.magnitude;
                    var direct = preVelocity.normalized;
                    speed = FixedPointMath.Max(0, speed - interactiveFriction * FixedPointPhysicsPresenter.Instance.DeltaTime);
                    var targetSpeed = velocity.magnitude;
                    var targetDirect = velocity.normalized;
                    var newVelocity = direct * speed + targetDirect * FixedPointMath.Max(0, interactiveFriction * 2 * FixedPointPhysicsPresenter.Instance.DeltaTime);
                    newVelocity = FixedPointMath.Min(maxSpeed * FixedPointPhysicsPresenter.Instance.DeltaTime, newVelocity.magnitude) * newVelocity.normalized;
                    AddImpulse(preVelocity * jumpSpeedProportion);
                    preVelocity = newVelocity;
                }
                else
                {
                    AddImpulse(preVelocity * jumpSpeedProportion);
                    preVelocity = FixedPointVector3.Lerp(preVelocity, velocity, inertiaFactor);
                }*/
            }
            // currentForce += velocity * 100;
        }

        // 备用的仅依靠加速度和摩擦力移动方案，用于模拟更接近人的运动方式。
        /*
        void MoveWithAccelerateFrictionOnly(FixedPointVector3 velocity, FixedPoint64 dot)
        {
            // 模拟滑动。
            var speed = preVelocity.magnitude;
            var direct = preVelocity.normalized;
            speed = FixedPointMath.Max(0, speed - interactiveFriction * FixedPointPhysicsPresenter.Instance.DeltaTime);
            // var targetSpeed = velocity.magnitude;
            var targetDirect = velocity.normalized;
            // 待处理：接触多个地面时应合并地面法线。
            // 模拟重力。
            var deltaGravityVelocity = FixedPointPhysicsPresenter.Instance.DeltaTime * FixedPointPhysicsPresenter.GravitationalAcceleration;
            dot = FixedPointVector3.Dot(deltaGravityVelocity, groundNormal);
            if (dot < 0)
            {
                deltaGravityVelocity = deltaGravityVelocity - groundNormal * dot;
            }
            // 模拟角色跑动时主动抵消沿运动方向的部分重力。
            dot = FixedPointVector3.Dot(deltaGravityVelocity, targetDirect);
            if (dot < 0)
            {
                deltaGravityVelocity = deltaGravityVelocity - targetDirect * dot;
            }
            // 使用角色内部驱动力改变速度。
            var internalPropulsion = targetDirect * FixedPointMath.Max(0, interactiveFriction * 2 * FixedPointPhysicsPresenter.Instance.DeltaTime);

            var newVelocity = direct * speed + deltaGravityVelocity * FixedPointPhysicsPresenter.Instance.DeltaTime + internalPropulsion;
            newVelocity = FixedPointMath.Min(maxSpeed * FixedPointPhysicsPresenter.Instance.DeltaTime, newVelocity.magnitude) * newVelocity.normalized;
            AddImpulse(newVelocity);
            preVelocity = newVelocity;
        }*/

        private void MoveWithAccelerateFrictionOnGround(FixedPointVector3 targetVelocity)
        {
            #region 根据地面倾角修正方向，避免斜坡移动速度下降

            var targetSpeed = targetVelocity.magnitude;
            var dotGround = FixedPointVector3.Dot(targetVelocity, groundNormal);
            var targetDirect = (targetVelocity - groundNormal * dotGround).normalized;

            #endregion

            MoveWithAccelerateFriction(targetDirect, targetSpeed);
        }

        private void MoveWithAccelerateFrictionOffGround(FixedPointVector3 targetVelocity)
        {
            var targetSpeed = targetVelocity.magnitude;
            var targetDirect = targetVelocity.normalized;
            MoveWithAccelerateFriction(targetDirect, targetSpeed);
        }

        /// <summary>
        /// 根据摩擦和加速度计算角色输入速度。
        /// </summary>
        private void MoveWithAccelerateFriction(FixedPointVector3 targetDirect, FixedPoint64 targetSpeed)
        {
            #region 前一帧存在反向约束时跳过会继续进入静态碰撞器的移动

            var correctVelocity = targetDirect * targetSpeed;

            if (preConstraint != FixedPointVector3.zero)
            {
                var preConstraintDirect = preConstraint.normalized;
                var dot = FixedPointVector3.Dot(correctVelocity, preConstraintDirect);

                if (dot < 0)
                {
                    return;
                }
            }

            #endregion

            #region 计算现有速度在目标方向上的分量

            var preTargetDirectSpeed = FixedPointVector3.Dot(preVelocity, targetDirect);
            var directVelocity = targetDirect * preTargetDirectSpeed;
            var lateralVelocity = preVelocity - directVelocity;
            var invMass = reverseMass * context.DeltaTime;

            if (preTargetDirectSpeed < targetSpeed)
            {
                var deltaNewSpeed =
                    FixedPointMath.Min(targetSpeed - preTargetDirectSpeed, interactiveAccelerate * invMass);
                directVelocity += targetDirect * deltaNewSpeed;

                if (preTargetDirectSpeed < 0)
                {
                    directVelocity += targetDirect * interactiveFriction * invMass;
                }
            }
            else if (preTargetDirectSpeed > targetSpeed)
            {
                var deltaNewSpeed =
                    FixedPointMath.Min(preTargetDirectSpeed - targetSpeed, interactiveFriction * invMass);
                directVelocity -= targetDirect * deltaNewSpeed;
            }

            #endregion

            #region 使用摩擦衰减垂直于当前输入方向的横向速度

            var deltaFrictionSpeed = interactiveFriction * invMass;
            lateralVelocity = lateralVelocity.normalized *
                              FixedPointMath.Max(0, lateralVelocity.magnitude - deltaFrictionSpeed);

            #endregion

            var newVelocity = lateralVelocity + directVelocity;
            AddImpulse(newVelocity);
            preVelocity = newVelocity;
        }

        /// <summary>
        /// 沿指定方向施加俯冲力并清除当前速度。
        /// </summary>
        /// <param name="orientation">俯冲方向。</param>
        public void Dive(FixedPointVector3 orientation)
        {
            AddForce((orientation + new FixedPointVector3(0, doubleJumpOffsetY / 1000f, 0)) * diveForce);
            velocity = FixedPointVector3.zero;
            doubleJumpCooldown = 60;
        }

        /// <summary>
        /// 累加角色主动产生的内部力。
        /// </summary>
        /// <param name="force">需要累加的力。</param>
        public void AddForce(FixedPointVector3 force)
        {
            currentForce += force;
        }

        /// <summary>
        /// 施加外部击退力并清除原有移动速度。
        /// </summary>
        /// <param name="force">击退力。</param>
        public void KnockBack(FixedPointVector3 force)
        {
            preVelocity = FixedPointVector3.zero;
            knockBackForce = force;
            velocity = FixedPointVector3.zero;
            onKnockBack?.Invoke(force);
        }

        /// <summary>
        /// 汇总内部力、重力和击退力，并积分到角色速度。
        /// </summary>
        public void AddForce()
        {
            if (isGround)
            {
                forces = currentForce;
            }
            else
            {
                // 质量高于基础值时增加向下加速度，并限制最大附加值。
                forces = currentForce + FPPhysicsContext.GravitationalAcceleration - new FixedPointVector3(
                    0, FixedPointMath.Min(MaxAdditionGravity, mass - BaseMass), 0);
            }

            knockBackVelocity += knockBackForce * context.DeltaTime;
            velocity += forces * context.DeltaTime;
            currentForce = FixedPointVector3.zero;
            knockBackForce = FixedPointVector3.zero;
        }

        /// <summary>
        /// 合并一次直接位移冲量，保留不同方向上的有效分量。
        /// </summary>
        /// <param name="additionalImpulse">需要合并的位移冲量。</param>
        public void AddImpulse(FixedPointVector3 additionalImpulse)
        {
            if (impulse == FixedPointVector3.zero)
            {
                impulse = additionalImpulse;
            }
            else
            {
                var impulseNormal = impulse.normalized;
                var magnitude = impulse.magnitude;
                var dot = FixedPointVector3.Dot(additionalImpulse, impulseNormal);

                if (dot > magnitude)
                {
                    impulse = additionalImpulse;
                }
                else if (dot > 0)
                {
                    impulse += additionalImpulse - impulseNormal * dot;
                }
                else
                {
                    impulse += additionalImpulse;
                }
            }
        }

        /// <summary>
        /// 合并一次穿透修正约束，保留不同方向上的有效分量。
        /// </summary>
        /// <param name="additionalConstraint">需要合并的约束向量。</param>
        internal void AddConstraints(FixedPointVector3 additionalConstraint)
        {
            if (constraint == FixedPointVector3.zero)
            {
                constraint = additionalConstraint;
            }
            else
            {
                var constraintNormal = constraint.normalized;
                var magnitude = constraint.magnitude;
                var dot = FixedPointVector3.Dot(additionalConstraint, constraintNormal);

                // 同方向分量大于现有约束时，使用新的更大约束。
                if (dot > magnitude)
                {
                    constraint = additionalConstraint;
                }
                // 同方向分量较小时，仅合并垂直方向的新增分量。
                else if (dot > 0)
                {
                    constraint += additionalConstraint - constraintNormal * dot;
                }
                else
                {
                    // 方向相反时直接叠加，由向量相消得到最终约束。
                    constraint += additionalConstraint;
                }
            }
        }

        /// <summary>
        /// 在当前轮静态碰撞检测完成后应用累计位置修正。
        /// </summary>
        internal void SolveConstraints()
        {
            fpTransform.position += constraint;
            preConstraint = constraint;
            constraint = FixedPointVector3.zero;
        }

        /// <summary>
        /// 保持水平速度大小不变并切换其朝向。
        /// </summary>
        /// <param name="forward">新的水平前进方向。</param>
        internal void ChangeVelocityDirection(FixedPointVector2 forward)
        {
            var y = velocity.y;
            var magnitude = new FixedPointVector2(velocity.x, velocity.z).magnitude;
            velocity = new FixedPointVector3(forward.x * magnitude, y, forward.y * magnitude);
        }

        /// <summary>
        /// 推进角色控制器一个固定物理步长。
        /// </summary>
        internal void OnUpdate()
        {
            if (!enabled)
            {
                return;
            }

            lastJump -= context.DeltaTime;

            // 接地且速度没有离地分量时清零，避免在上方斜面附近跳跃产生滑移。
            if (isGround && velocity != FixedPointVector3.zero)
            {
                if (FixedPointVector3.Dot(velocity, groundNormal) <= FixedPoint64.EN3)
                {
                    velocity = FixedPointVector3.zero;
                }
            }

            deltaPosition = impulse + (velocity + knockBackVelocity) * context.DeltaTime;

            /*
            // 前一帧存在反向约束时，可跳过会继续进入静态碰撞器的移动。
            if (preConstraint != FixedPointVector3.zero)
            {
                var preConstraintDirect = preConstraint.normalized;
                var dot = FixedPointVector3.Dot(deltaPosition, preConstraintDirect);
                if (dot < 0)
                {
                    deltaPosition = FixedPointVector3.zero;
                }
            }*/

            impulse = FixedPointVector3.zero;

            if (doubleJumpCooldown > 0)
            {
                doubleJumpCooldown--;
            }

            var magnitude = deltaPosition.magnitude;

            if (magnitude > ThresholdDeltaMove)
            {
                var count = (int)(magnitude / ThresholdDeltaMove) + 1;
                var reverse = 1 / new FixedPoint64(count);

                for (var i = 0; i < count; i++)
                {
                    fpTransform.position += deltaPosition * reverse;
                    context.SolveConstraints(this);
                }

                if (deltaPosition != FixedPointVector3.zero)
                {
                    onMove?.Invoke(deltaPosition);
                }

                return;
            }

            fpTransform.position += deltaPosition;
            context.SolveConstraints(this);

            if (deltaPosition != FixedPointVector3.zero)
            {
                onMove?.Invoke(deltaPosition);
            }
        }
    }
}