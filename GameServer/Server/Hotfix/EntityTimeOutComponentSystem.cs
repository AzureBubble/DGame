using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;

namespace System.Authentication;

public sealed class EntityTimeOutComponentDestroySystem : DestroySystem<EntityTimeOutComponent>
{
    protected override void Destroy(EntityTimeOutComponent self)
    {
        if (self.TimerId != 0)
        {
            self.Scene.TimerComponent.Net.Remove(ref self.TimerId);
        }

        self.NextTime = 0;
        self.Interval = 0;
    }
}

public static class EntityTimeOutComponentSystem
{
    public static void SetInterval(this EntityTimeOutComponent self, int interval)
    {
        if (interval <= 0)
        {
            throw new ArgumentException("interval must be > 0", nameof(interval));
        }

        self.Interval = interval;
        self.NextTime = TimeHelper.Now + interval;
    }

    public static bool CheckInterval(this EntityTimeOutComponent self)
    {
        if (self.NextTime > TimeHelper.Now)
        {
            Log.Warning("当前连接请求的间隔过小");
            return false;
        }
        self.NextTime = TimeHelper.Now + self.Interval;
        return true;
    }

    public static void TimeOut(this EntityTimeOutComponent self, int timeout, Func<FTask>? task = null)
    {
        var scene = self.Scene;
        var parentRuntimeId = scene.Parent.RuntimeId;

        if (self.TimerId != 0)
        {
            self.Scene.TimerComponent.Net.Remove(ref self.TimerId);
        }
        // 创建一个任务计时器 用在timeout时间后执行 并且要清除掉当前鉴权服务器的缓存
        self.TimerId = scene.TimerComponent.Net.OnceTimer(timeout, () =>
        {
            self.Handler(parentRuntimeId, task).Coroutine();
        });
    }

    private static async FTask Handler(this EntityTimeOutComponent self, long parentRuntimeId, Func<FTask>? task = null)
    {
        var selfParent = self.Parent;
        if (selfParent == null || parentRuntimeId != selfParent.RuntimeId)
        {
            return;
        }

        if (task != null)
        {
            await task();
        }

        self.TimerId = 0;
        selfParent.Dispose();
    }
}