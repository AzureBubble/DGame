#if UNITY_2021_3_OR_NEWER
using UnityEngine;

namespace DGame.FixedPoint
{
    /// <summary>
    /// <see cref="FPCollider"/> 的 Unity 表现与序列化部分，仅在客户端编译。
    /// </summary>
    [RequireComponent(typeof(FPTransform))]
    [DefaultExecutionOrder(-899)]
    [ExecuteInEditMode]
    public abstract partial class FPCollider : MonoBehaviour
    {
        [SerializeField] private bool isInit;

        [SerializeField] private bool drawAABB = true;

        [SerializeField] private Color gizmosColor = Color.blue;

        [SerializeField] private bool drawDebugInfo;

        /// <summary>调试标记立方体的统一尺寸。</summary>
        protected static readonly Vector3 DebugCubeSize = Vector3.one * 0.1f;

        /// <summary>
        /// 初始化碰撞器，将其添加到物理系统中并设置必要的属性。
        /// </summary>
        protected virtual void Awake()
        {
            fpTransform = GetComponent<FPTransform>();

            if (!isInit)
            {
                InitColliderSize();
                isInit = true;
            }

            if (Application.isPlaying && FPPhysicsPresenter.Instance?.context != null)
            {
                context = FPPhysicsPresenter.Instance.context;
                context.AddCollider(this);
                UpdateCollider();
            }
        }

        /// <summary>
        /// 销毁时从物理上下文和八叉树节点中注销当前碰撞器。
        /// </summary>
        protected virtual void OnDestroy()
        {
            context?.RemoveCollider(this);
            context = null;
        }

        /// <summary>
        /// 根据关联的 Unity 网格初始化碰撞器尺寸。
        /// </summary>
        protected abstract void InitColliderSize();

        /// <summary>
        /// 在 Unity 场景视图中绘制碰撞器调试图形。
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!enabled)
            {
                return;
            }

            if (fpTransform == null)
            {
                fpTransform = GetComponent<FPTransform>();

                if (fpTransform == null)
                {
                    return;
                }
            }

            var color = Gizmos.color;
            Gizmos.color = gizmosColor;
            OnDrawGizmosEditor();

            if (drawAABB)
            {
                DrawAABBEditor();
            }

            if (drawDebugInfo)
            {
                OnDrawDebugInfo();
            }

            Gizmos.color = color;
        }

        /// <summary>
        /// 在场景视图中绘制碰撞器的轴对齐包围盒。
        /// </summary>
        private void DrawAABBEditor()
        {
            UpdateAABB();
            var color = Gizmos.color;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(((max + min) * 0.5).ToVector3(), (max - min).ToVector3());
            Gizmos.color = color;
        }

        /// <summary>
        /// 绘制当前具体碰撞器的调试图形。
        /// </summary>
        protected abstract void OnDrawGizmosEditor();

        /// <summary>
        /// 绘制由子类提供的额外调试信息。
        /// </summary>
        protected virtual void OnDrawDebugInfo()
        {
        }
    }
}
#endif