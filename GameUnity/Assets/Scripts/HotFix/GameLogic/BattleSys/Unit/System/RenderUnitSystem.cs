using Fantasy.Entitas.Interface;

namespace GameLogic
{
    /// <summary>
    /// 渲染层对象销毁触发事件
    /// </summary>
    public sealed class RenderUnitDestroySystem : DestroySystem<RenderUnit>
    {
        protected override void Destroy(RenderUnit self)
        {
            if (self.IsDestroyed)
            {
                return;
            }
        }
    }
    
    public sealed class RenderUnitSystem
    {
        
    }
}