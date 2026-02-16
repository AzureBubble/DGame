using Fantasy.Entitas;

namespace Fantasy;

public sealed class GameAccountFlagComponent : Entity
{
    public long AccountId;
    // 有一种可能 当在Account在其他地方被销毁了
    // 这时候因为这个Account是回收到缓存池中 所以这个引用还是有效的
    // 那这时候就会在这个引用的Account可能是其他用户的了
    public EntityReference<GameAccount> GameAccount;
}