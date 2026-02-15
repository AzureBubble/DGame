using Fantasy;
using Fantasy.Network;

namespace System.Authentication;

public static class SessionHelper
{
    public static void SetTimeOut(this Session session, int timeout)
    {
        var authenticationSessionComponent = session.GetComponent<SessionTimeOutComponent>();

        if (authenticationSessionComponent == null)
        {
            authenticationSessionComponent = session.AddComponent<SessionTimeOutComponent>();
        }
        // 3秒后自动销毁的Session
        authenticationSessionComponent.TimeOut(timeout);
    }

    public static bool CheckInterval(this Session session, int interval)
    {
        var authenticationSessionComponent = session.GetComponent<SessionTimeOutComponent>();

        if (authenticationSessionComponent == null)
        {
            authenticationSessionComponent = session.AddComponent<SessionTimeOutComponent>();
            // 如果没有这个组件 就挂载组件 设置间隔时间是 2 秒
            authenticationSessionComponent.SetInterval(interval);
            return true;
        }
        return authenticationSessionComponent.CheckInterval();
    }
}