using Fantasy;
using Fantasy.Async;
using Fantasy.Event;

namespace System.Authentication;

public sealed class OnSceneCreate_Init : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;
        switch (scene.SceneType)
        {
            case SceneType.Authentication:
                // 用于鉴权服务器注册和登录相关逻辑的组件
                scene.AddComponent<AuthenticationComponent>().UpdatePosition();
                // 用于颁发 Token 证书相关的逻辑
                scene.AddComponent<AuthenticationJwtComponent>();
                break;

            case SceneType.Gate:
                // 用于验证Jwt是否合法的组件
                scene.AddComponent<GateJwtComponent>();
                Log.Debug($"Gate启动：{scene.SceneConfigId}");
                break;
        }

        await FTask.CompletedTask;
    }
}