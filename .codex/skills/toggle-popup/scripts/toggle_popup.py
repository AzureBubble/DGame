#!/usr/bin/env python3
from __future__ import annotations

import argparse
import re
import sys
import tomllib
from pathlib import Path


HOOK_COMMAND = 'python ".codex/hooks/codex_popup.py"'

HOOK_BLOCKS = {
    "PermissionRequest": f'''[[hooks.PermissionRequest]]
matcher = "*"

[[hooks.PermissionRequest.hooks]]
type = "command"
command = '{HOOK_COMMAND}'
timeout = 600
statusMessage = "显示 Codex 权限确认弹窗"''',
}

ALIASES = {
    "": ["PermissionRequest"],
    "all": ["PermissionRequest"],
    "permission": ["PermissionRequest"],
    "permissionrequest": ["PermissionRequest"],
    "p": ["PermissionRequest"],
}

HEADER_RE = re.compile(r"^\s*(\[\[?)([^\]]+?)(\]\]?)\s*(?:#.*)?$")


def find_codex_config() -> Path:
    current = Path(__file__).resolve()
    for parent in current.parents:
        if parent.name == ".codex":
            return parent / "config.toml"
    raise RuntimeError("无法从脚本路径定位 .codex/config.toml")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="切换 .codex/config.toml 中的 Codex 权限确认弹窗 hook。",
    )
    parser.add_argument(
        "scope",
        nargs="?",
        default="",
        help="留空/all/permission/p/permissionrequest",
    )
    parser.add_argument(
        "--config",
        type=Path,
        default=None,
        help="用于测试的 config.toml 路径；正常使用不需要传。",
    )
    return parser.parse_args()


def event_header_name(line: str) -> str | None:
    match = HEADER_RE.match(line)
    if not match:
        return None
    return match.group(2).strip()


def is_event_header(header: str, event: str) -> bool:
    return header == f"hooks.{event}" or header == f"hooks.{event}.hooks"


def has_event(text: str, event: str) -> bool:
    for line in text.splitlines():
        header = event_header_name(line)
        if header == f"hooks.{event}":
            return True
    return False


def remove_events(text: str, events: list[str]) -> str:
    target_headers = {
        f"hooks.{event}"
        for event in events
    } | {
        f"hooks.{event}.hooks"
        for event in events
    }

    kept: list[str] = []
    skipping = False

    for line in text.splitlines():
        header = event_header_name(line)
        if header is not None:
            skipping = header in target_headers
            if skipping:
                continue
        if not skipping:
            kept.append(line)

    result = "\n".join(kept).rstrip()
    return result + "\n" if result else ""


def add_events(text: str, events: list[str]) -> str:
    blocks = "\n\n".join(HOOK_BLOCKS[event] for event in events)
    prefix = text.rstrip()
    comment = "# -- Codex popup hooks --"
    if prefix:
        return f"{prefix}\n\n{comment}\n{blocks}\n"
    return f"{comment}\n{blocks}\n"


def validate_toml(text: str, config_path: Path) -> None:
    try:
        tomllib.loads(text)
    except tomllib.TOMLDecodeError as exc:
        raise RuntimeError(f"生成的 TOML 无法解析，未写入 {config_path}: {exc}") from exc


def main() -> int:
    args = parse_args()
    key = args.scope.strip().lower()
    if key not in ALIASES:
        print(
            "无法识别参数："
            f"{args.scope!r}。有效值：留空、all、permission/p/permissionrequest。",
            file=sys.stderr,
        )
        return 2

    config_path = args.config or find_codex_config()
    if not config_path.exists():
        print(f"找不到配置文件：{config_path}", file=sys.stderr)
        return 1

    events = ALIASES[key]
    old_text = config_path.read_text(encoding="utf-8")
    enabled = any(has_event(old_text, event) for event in events)

    if enabled:
        new_text = remove_events(old_text, events)
        action = "关闭"
    else:
        without_stale = remove_events(old_text, events)
        new_text = add_events(without_stale, events)
        action = "开启"

    validate_toml(new_text, config_path)
    config_path.write_text(new_text, encoding="utf-8", newline="\n")

    print(f"已{action} Codex 弹窗 hook：{', '.join(events)}")
    print(f"配置文件：{config_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
