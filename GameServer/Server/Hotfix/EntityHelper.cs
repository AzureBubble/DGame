using System.Authentication;
using Fantasy;
using Fantasy.Entitas;

namespace System;

public static class EntityHelper
{
    public static void SetTimeOut(this Entity entity, int timeout = 3000)
    {
        var sessionTimeOutComponent = entity.GetComponent<EntityTimeOutComponent>();

        if (sessionTimeOutComponent == null)
        {
            sessionTimeOutComponent = entity.AddComponent<EntityTimeOutComponent>();
        }
        // 3秒后自动销毁的Session
        sessionTimeOutComponent.TimeOut(timeout);
    }

    public static bool CheckInterval(this Entity entity, int interval)
    {
        var sessionTimeOutComponent = entity.GetComponent<EntityTimeOutComponent>();

        if (sessionTimeOutComponent == null)
        {
            sessionTimeOutComponent = entity.AddComponent<EntityTimeOutComponent>();
            // 如果没有这个组件 就挂载组件 设置间隔时间是 2 秒
            sessionTimeOutComponent.SetInterval(interval);
            return true;
        }
        return sessionTimeOutComponent.CheckInterval();
    }

    public static bool IsHaveTimeOutComponent(this Entity entity)
        => entity.GetComponent<EntityTimeOutComponent>() != null;

    public static void CancelTimeOut(this Entity entity)
    {
        entity.RemoveComponent<EntityTimeOutComponent>();
    }
}