using Fantasy;
using Fantasy.Entitas.Interface;

namespace Hotfix.Gate;

public class GameAccountManageComponentDestroySystem : DestroySystem<GameAccountManageComponent>
{
    protected override void Destroy(GameAccountManageComponent self)
    {
        foreach (var item in self.Accounts.Values.ToArray())
        {
            item.Dispose();
        }
        self.Accounts.Clear();
    }
}

public static class GameAccountManageComponentSystem
{
    public static void Add(this GameAccountManageComponent self, GameAccount account)
        => self.Accounts.Add(account.Id, account);

    public static GameAccount? Get(this GameAccountManageComponent self, long uid)
        => self.Accounts.GetValueOrDefault(uid);

    public static bool TryGet(this GameAccountManageComponent self, long uid, out GameAccount? account)
        => self.Accounts.TryGetValue(uid, out account);

    public static void Remove(this GameAccountManageComponent self, long uid, bool isDispose = true)
    {
        if (!self.Accounts.Remove(uid, out var account))
        {
            return;
        }

        if (!isDispose)
        {
            return;
        }
        account.Dispose();
    }
}