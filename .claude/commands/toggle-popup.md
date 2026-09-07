---
description: 开启或关闭 Claude 权限确认弹窗 hook
allowed-tools: Read, Write, Edit
argument-hint: "[permission] (留空=权限确认)"
---

切换 `.claude/settings.json` 中 `PermissionRequest` 弹窗 hook 的启用状态。

参数 `$ARGUMENTS` 决定切换范围（大小写不敏感）：

- **留空**或 **`all`** → `PermissionRequest`。
- **`permission`**（或 `p` / `permissionrequest`）→ 仅 `PermissionRequest`。
- 其他无法识别的参数 → 提示用户有效取值，不修改文件。

执行步骤：

1. 解析 `$ARGUMENTS`，确认本次切换 `PermissionRequest`（按上面的规则）。
2. 读取 `.claude/settings.json`。
3. 判断当前状态并切换：
   - **已开启 → 关闭**：从 `hooks` 中删除 `PermissionRequest`，告知用户已关闭。
   - **已关闭 → 开启**：在 `hooks` 下添加下面的固定配置，告知用户已开启。
4. 写回时保持其他 hook、配置项和缩进风格不变。

各 hook 的固定配置：

```json
"PermissionRequest": [
  {
    "matcher": "",
    "hooks": [
      { "type": "command", "command": "python \"$CLAUDE_PROJECT_DIR/.claude/hooks/claude_popup.py\"" }
    ]
  }
]
```

注意：只切换 `PermissionRequest`，不要影响 `hooks` 下的其他类型 hook。
