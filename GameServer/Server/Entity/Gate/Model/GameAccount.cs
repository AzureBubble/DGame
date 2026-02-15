using Fantasy.Entitas;

namespace Fantasy;

public class GameAccount : Entity
{
    // 1、可以拿Token传递过来的uid做这个组件的Id
    // 2、让这个Id自动生成、在组件里做一个变量来记录Token的uid  public long AuthenticationId;
    /// <summary>
    /// 注册时间
    /// </summary>
    public long CreateTime;

    /// <summary>
    /// 登录时间
    /// </summary>
    public long LoginTime;
}