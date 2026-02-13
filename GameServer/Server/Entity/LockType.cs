namespace Fantasy;

public enum LockType
{
    None = 0,

    /// <summary>
    /// 鉴权注册锁
    /// </summary>
    Authentication_RegisterLock = 1,

    /// <summary>
    /// 鉴权账号移除锁
    /// </summary>
    Authentication_RemoveLock = 2,

    /// <summary>
    /// 鉴权登录锁
    /// </summary>
    Authentication_LoginLock = 3,
}