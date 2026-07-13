#if UNITY_2021_3_OR_NEWER
using UnityEngine;

namespace DGame.FixedPoint
{
    /// <summary>
    /// FPCharacterController 的 Unity 表现半边。仅客户端编译。
    /// </summary>
    [RequireComponent(typeof(FPTransform))]
    public sealed partial class FPCharacterController
    {
        protected override void Awake()
        {
            base.Awake();
            if (Application.isPlaying && FPPhysicsPresenter.Instance != null)
            {
                SetFpPhysicsContext(FPPhysicsPresenter.Instance.context);
                FPPhysicsPresenter.Instance.context.AddCharacter(this);
            }
        }

        internal void OnViewUpdate()
        {
            transform.position = fpTransform.position.ToVector3();
        }

        protected override void OnDrawGizmosEditor()
        {
            if (characterColliderType == CharacterCollider.Sphere)
            {
                Gizmos.DrawWireSphere(position .ToVector3(), scaledRadius.AsFloat());
            }
            else
            {
                FPCapsuleCollider.DrawWireCapsule(startPos.ToVector3(), endPos.ToVector3(), scaledRadius.AsFloat());
            }
        }
    }
}
#endif
