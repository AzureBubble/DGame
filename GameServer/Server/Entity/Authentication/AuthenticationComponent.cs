namespace Fantasy;

public sealed class AuthenticationComponent : Entitas.Entity
{
    public readonly Dictionary<string, Account> CacheAccountList = new Dictionary<string, Account>();
}