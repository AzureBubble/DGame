#!/bin/bash

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/path_define.sh"

echo "正在复制 Luban 文件夹..."
echo "从: $SOURCE"
echo "到: $DEST"
echo

if [ ! -d "$SOURCE" ]; then
    echo "错误: 源文件夹 \"$SOURCE\" 不存在！"
    read -p "按回车键退出..."
    exit 1
fi

# DEST_PARENT defined in path_define.sh
if [ ! -d "$DEST_PARENT" ]; then
    echo "创建目标目录: $DEST_PARENT"
    mkdir -p "$DEST_PARENT"
fi

cp -R "$SOURCE" "$DEST"

echo
echo "复制完成！"
read -p "按回车键退出..."
