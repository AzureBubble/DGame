#if UNITY_2021_3_OR_NEWER
using UnityEngine;

namespace DGame.FixedPoint
{
    /// <summary>
    /// 定点物理在 Unity 客户端中的场景外壳。
    /// </summary>
    /// <remarks>
    /// 该类型仅负责创建并持有 <see cref="FPPhysicsContext"/>、转发外部驱动的逻辑与表现更新，
    /// 以及绘制八叉树 Gizmo。物理状态保存在 context 中，服务端不依赖本类型。
    /// 场景中只允许存在一个有效实例。
    /// </remarks>
    public sealed class FPPhysicsPresenter
    {
        /// <summary>
        /// 获取当前 Unity 外壳持有的物理上下文。
        /// </summary>
        public FPPhysicsContext context { get; private set; }

        /// <summary>
        /// 当前场景中的有效实例。
        /// </summary>
        private static FPPhysicsPresenter m_instance;

        /// <summary>
        /// 获取当前场景中的定点物理表现外壳；场景尚未加载该组件时返回 <see langword="null"/>。
        /// </summary>
        public static FPPhysicsPresenter Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new FPPhysicsPresenter();
                    m_instance.Initialize();
                }
                return m_instance;
            }
        }

        /// <summary>
        /// 创建八叉树时使用的逻辑尺寸。
        /// </summary>
        private int octreeSize = 1024;

        /// <summary>
        /// 与服务端保持一致的固定物理步长，不读取 <see cref="Time.fixedDeltaTime"/>。
        /// </summary>
        private double fixedDeltaTime = FPPhysicsContext.DefaultDeltaTime;

        /// <summary>
        /// 当前物理世界使用的确定性随机种子。
        /// </summary>
        private int randomSeed = 0;

        /// <summary>
        /// 是否在 Scene 视图中绘制包含碰撞器的八叉树节点。
        /// </summary>
        public bool drawGizmos = true;

        /// <summary>
        /// 使用 Inspector 配置创建物理上下文；重复调用不会替换已经存在的上下文。
        /// </summary>
        public void Initialize()
        {
            context ??= new FPPhysicsContext(octreeSize, randomSeed, (FixedPoint64)fixedDeltaTime);
        }

        /// <summary>
        /// 销毁当前有效实例时释放静态引用。
        /// </summary>
        private void Destroy()
        {
            m_instance = null;
        }

        /// <summary>
        /// 清空当前物理上下文中的全部模拟对象和累计时间。
        /// </summary>
        public void Clear()
        {
            context?.Clear();
        }

        /// <summary>
        /// 由外部固定帧驱动器调用，推进一个定点物理步长。
        /// </summary>
        public void OnUpdate()
        {
            context?.OnUpdate();
        }

        /// <summary>
        /// 由外部表现帧驱动器调用，将定点逻辑状态同步到 Unity 表现对象。
        /// </summary>
        public void OnViewUpdate()
        {
            context?.OnViewUpdate();
        }

        /// <summary>
        /// Unity Scene 视图的 Gizmo 回调。
        /// </summary>
        private void OnDrawGizmos()
        {
            DrawGizmos();
        }

        /// <summary>
        /// 绘制当前八叉树中直接包含碰撞器的节点，可由其他 Gizmo 驱动器显式调用。
        /// </summary>
        public void DrawGizmos()
        {
            if (!drawGizmos)
            {
                return;
            }
            if (context?.fpOctree?.root == null)
            {
                return;
            }

            var previousColor = Gizmos.color;
            DrawNode(context.fpOctree.root);
            Gizmos.color = previousColor;
        }

        /// <summary>
        /// 递归绘制直接包含碰撞器的八叉树节点。
        /// </summary>
        /// <param name="node">当前递归节点。</param>
        private static void DrawNode(FPOctreeNode node)
        {
            if (node.FpSphereColliders is { Count: > 0 }
                || node.FpAABBColliders is { Count: > 0 }
                || node.FpObbColliders is { Count: > 0 }
                || node.FpAACapsuleColliders is { Count: > 0 }
                || node.FpCapsuleColliders is { Count: > 0 }
                || node.FpCylinderColliders is { Count: > 0 }
                || node.FpMeshColliders is { Count: > 0 }
                || node.FpCharacterColliders is { Count: > 0 }
                )
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(node.pos.ToVector3(), Vector3.one * node.halfSize * 2);

            }
            Gizmos.color = Color.white;
            if (node.nodes == null) return;
            foreach (var item in node.nodes)
            {
                DrawNode(item);
            }
        }
    }
}
#endif