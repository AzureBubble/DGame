using Fantasy.Entitas;

namespace Fantasy;

public class AuthenticationSessionComponent : Entity
{
    /// <summary>
    /// 主要是用于检测每次请求的间隔 这里存放的是下一次能正常通讯的时间
    /// </summary>
    public long NextTime;

    /// <summary>
    /// 用来设置检查的间隔时间
    /// </summary>
    public int Interval;

    /// <summary>
    /// 用于记录计时器的计时器ID 通过这个ID及时取消任务
    /// </summary>
    public long TimerId;
}