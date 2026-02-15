using Fantasy;
using Fantasy.Entitas.Interface;

namespace Hotfix.Gate;

public class GameAccountDestroySystem : DestroySystem<GameAccount>
{
    protected override void Destroy(GameAccount self)
    {
        self.CreateTime = 0;
        self.LoginTime = 0;
        self.SessionRuntimeId = 0;
    }
}