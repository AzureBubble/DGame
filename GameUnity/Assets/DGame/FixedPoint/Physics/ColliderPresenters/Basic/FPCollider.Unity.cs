#if UNITY_2021_3_OR_NEWER
using UnityEngine;

namespace DGame.FixedPoint
{
    /// <summary>
    /// FPCollider 的 Unity 表现/序列化半边（MonoBehaviour）。仅客户端编译。
    /// </summary>
    [RequireComponent(typeof(FPTransform))]
    [DefaultExecutionOrder(-899)]
    [ExecuteInEditMode]
    public abstract partial class FPCollider : MonoBehaviour
    {
        [SerializeField]
        private bool isInit; // 指示碰撞器是否已初始化。

        [SerializeField]
        private bool drawAABB = true; // 在编辑器中绘制轴对齐包围盒的开关。

        [SerializeField]
        private Color gizmosColor = Color.blue; // 在Unity编辑器中绘制的gizmos的颜色。

        [SerializeField]
        private bool drawDebugInfo; // 绘制额外调试信息的开关。

        protected static readonly Vector3 DebugCubeSize = Vector3.one * 0.1f; // 调试立方体的标准大小。

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
            if (Application.isPlaying)
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
        /// 初始化碰撞器大小的占位方法。由子类实现（仅 Unity 侧需要，依赖 MeshFilter）。
        /// </summary>
        protected abstract void InitColliderSize();

        /// <summary>
        /// 在Unity编辑器中绘制gizmos以进行视觉调试。
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!enabled) return;

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
        /// 为调试目的在编辑器中绘制碰撞器的轴对齐包围盒（AABB）。
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
        /// 绘制特定于碰撞器的gizmos的抽象方法。由子类实现。
        /// </summary>
        protected abstract void OnDrawGizmosEditor();

        /// <summary>
        /// 绘制额外调试信息的可选方法。可以被子类重写。
        /// </summary>
        protected virtual void OnDrawDebugInfo() { }
    }
}
#endif
