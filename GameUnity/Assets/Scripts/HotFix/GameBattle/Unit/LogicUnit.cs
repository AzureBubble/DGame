using Fantasy.Entitas;

namespace GameBattle
{
    /// <summary>
    /// 逻辑层对象
    /// </summary>
    public class LogicUnit : Entity
    {
        /// <summary>
        /// 渲染层对象
        /// </summary>
        public IRenderUnit RenderUnit { get; set; }
    }
}
