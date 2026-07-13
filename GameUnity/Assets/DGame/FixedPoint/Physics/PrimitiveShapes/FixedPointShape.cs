/*
* Create 2022/11/1
* 応彧剛　yingyugang@gmail.com
* It's used by fixedpoint physics system.
*/
namespace DGame.FixedPoint
{
    public enum ShapeType {
        Line,
        Ray,
        Plane,
        Sphere,
        AABB,
        OBB
    }
    public abstract class FixedPointShape
    {
        public ShapeType shape { get; protected set; }

#if UNITY_2021_3_OR_NEWER
        public abstract void DrawGizmos(bool intersected);
#endif
    }
}
