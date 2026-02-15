using Fantasy.Entitas;

namespace Fantasy;

public sealed class GameAccountManageComponent : Entity
{
    public readonly Dictionary<long, GameAccount> Accounts = new Dictionary<long, GameAccount>();
}