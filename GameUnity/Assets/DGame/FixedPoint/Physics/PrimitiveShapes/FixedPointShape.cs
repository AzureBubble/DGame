/*
 * 创建日期：2022/11/1
 * 作者：応彧剛（yingyugang@gmail.com）
 * 用途：定义定点数物理系统使用的几何图元基类。
 */
namespace DGame.FixedPoint
{
    /// <summary>
    /// 定点数几何图元类型。
    /// </summary>
    public enum ShapeType
    {
        /// <summary>线段。</summary>
        Line,

        /// <summary>射线。</summary>
        Ray,

        /// <summary>平面。</summary>
        Plane,

        /// <summary>球体。</summary>
        Sphere,

        /// <summary>轴对齐包围盒。</summary>
        AABB,

        /// <summary>有向包围盒。</summary>
        OBB
    }

    /// <summary>
    /// 定点数几何图元的抽象基类。
    /// </summary>
    public abstract class FixedPointShape
    {
        /// <summary>
        /// 获取当前图元的类型。
        /// </summary>
        public ShapeType shape { get; protected set; }

#if UNITY_2021_3_OR_NEWER
        /// <summary>
        /// 在 Unity 场景视图中绘制图元的调试线框。
        /// </summary>
        /// <param name="intersected">当前图元是否处于相交状态；相交时使用红色绘制。</param>
        public abstract void DrawGizmos(bool intersected);
#endif
    }
}