/*
* Create 2022/11/1
* 応彧剛　yingyugang@gmail.com
* It's used by fixedpoint physics system.
*/
//reference: Game Physics Cookbook
#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif
namespace DGame.FixedPoint
{
    public class FixedPointLineSegment : FixedPointShape
    {
        public FixedPointVector3 Start { get { return start; } }
        public FixedPointVector3 End { get { return end; } }
        protected FixedPointVector3 start, end;
        public FixedPointLineSegment()
        {
            shape = ShapeType.Line;
        }
#if UNITY_2021_3_OR_NEWER
        public override void DrawGizmos(bool intersected)
        {
            Gizmos.color = intersected ? Color.red : Color.white;
            Gizmos.DrawLine(start.ToVector3(), end.ToVector3());
        }
#endif
    }
}
