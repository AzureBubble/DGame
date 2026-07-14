#if UNITY_2021_3_OR_NEWER
using UnityEngine;

namespace DGame.FixedPoint
{
    /// <summary>
    /// <see cref="FPCharacterController"/> 的 Unity 表现部分，仅在客户端编译。
    /// </summary>
    [RequireComponent(typeof(FPTransform))]
    public sealed partial class FPCharacterController
    {
        /// <summary>初始化角色控制器并注册到物理上下文。</summary>
        protected override void Awake()
        {
            base.Awake();

            if (Application.isPlaying && FPPhysicsPresenter.Instance != null)
            {
                SetFpPhysicsContext(FPPhysicsPresenter.Instance.context);
                FPPhysicsPresenter.Instance.context.AddCharacter(this);
            }
        }

        /// <summary>将定点数逻辑位置同步到 Unity Transform。</summary>
        internal void OnViewUpdate()
        {
            transform.position = fpTransform.position.ToVector3();
        }

        /// <summary>在 Unity 场景视图中绘制当前角色碰撞形状。</summary>
        protected override void OnDrawGizmosEditor()
        {
            if (characterColliderType == CharacterCollider.Sphere)
            {
                Gizmos.DrawWireSphere(position.ToVector3(), scaledRadius.AsFloat());
            }
            else
            {
                FPCapsuleCollider.DrawWireCapsule(startPos.ToVector3(), endPos.ToVector3(), scaledRadius.AsFloat());
            }
        }
    }
}
#endif