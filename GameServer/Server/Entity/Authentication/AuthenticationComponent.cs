namespace Fantasy;

public sealed class AuthenticationComponent : Entitas.Entity
{
    public readonly Dictionary<long, Account> CacheAccountList = new Dictionary<long, Account>();
}