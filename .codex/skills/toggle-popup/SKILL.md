---
name: toggle-popup
description: Toggle DGame project Codex permission request popup hooks in .codex/config.toml. Use when the user asks to enable, disable, or switch Codex permission request popups.
---

# Toggle Popup

用于切换当前 DGame 项目的 Codex 权限确认弹窗 hook。执行时不要手写 TOML，直接运行脚本：

```powershell
python ".codex/skills/toggle-popup/scripts/toggle_popup.py" [permission]
```

## 参数

- 留空或 `all`：切换 `PermissionRequest`。
- `permission`、`p` 或 `permissionrequest`：只切换 `PermissionRequest`。

## 行为

脚本读取 `.codex/config.toml`，只处理目标集合内的 hook：

- 目标集合中任意 hook 已存在时，视为已开启，执行关闭。
- 目标集合全部不存在时，视为已关闭，执行开启。
- 不修改 `hooks` 下其他 hook，也不改 MCP、模型、sandbox 等配置。

执行后把脚本输出原样总结给用户。
