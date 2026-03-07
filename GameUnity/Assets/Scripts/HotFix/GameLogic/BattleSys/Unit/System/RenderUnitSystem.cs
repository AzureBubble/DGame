using Fantasy.Entitas.Interface;

namespace GameLogic
{
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