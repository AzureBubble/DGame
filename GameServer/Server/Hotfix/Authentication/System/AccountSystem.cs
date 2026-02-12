using Fantasy;
using Fantasy.Entitas.Interface;

namespace System;

public class AccountSystem : DestroySystem<Account>
{
    protected override void Destroy(Account self)
    {
        self.Username = null;
        self.Password = null;
        self.CreateTime = 0;
        self.LoginTime = 0;
    }
}