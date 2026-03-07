using Fantasy.Entitas.Interface;

namespace GameBattle
{
    public sealed class LogicUnitDestroySystem : DestroySystem<LogicUnit>
    {
        protected override void Destroy(LogicUnit self)
        {
            self.RenderUnit = null;
        }
    }
    
    public sealed class LogicUnitSystem
    {
        
    }
}