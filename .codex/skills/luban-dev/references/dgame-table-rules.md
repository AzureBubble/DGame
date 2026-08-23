# DGame Luban 表规则

## 推荐策略

- 默认推荐在 `GameConfig/Datas/__tables__.xlsx` 显式注册表。
- Luban 支持 `#<value_type>-<comment>.xlsx` 文件级自动导入，DGame 可用但不默认推荐。
- DGame 扩展支持 Sheet 级拆表：sheet 名为 `#<value_type>-<comment>` 时，可手动运行 `GameConfig/Tools/split_sheets.py` 拆成临时 `#` 文件再交给 Luban 自动导入；当前默认导表脚本不会自动调用该步骤。
- 需要 `mode`、`index`、`group_by`、复杂 `tags` 或明确双端分组时，优先使用 `__tables__.xlsx`。

## 显式注册

`__tables__.xlsx` 当前字段从实际表头确认：

- `full_name`：表类名，如 `TbItemConfig`
- `value_type`：行类型，如 `ItemConfig`
- `read_schema_from_file`：是否从 Excel 表头读取字段定义
- `input`：业务 Excel 文件
- `index`：主键字段
- `mode`：`map`、`one`、`list`
- `group`：`c`、`s`、`e`
- `comment`：表注释
- `tags`：如 `group_by:GroupID`

## 自动导入

文件级：

```text
GameConfig/Datas/#SkillCfg-技能表.xlsx -> value_type=SkillCfg -> TbSkillCfg
```

Sheet 级：

```text
任意业务 Excel 的 sheet "#SkillCfg-技能表"
-> 手动运行 split_sheets.py，临时生成 GameConfig/Datas/#SkillCfg-技能表.xlsx
-> Luban 自动导入 TbSkillCfg
```

`split_sheets.py` 规则：

- 跳过 `__tables__`、`__beans__`、`__enums__`。
- 如果 Datas 根目录已有同名 `#value_type*.xlsx`，跳过同名 sheet，避免重复成表。
- 生成文件记录在 `_split_manifest.txt`，导表后清理。
- 文件名中的 `.` 会被替换，避免 Luban 误判 namespace。

## Excel 数据表

普通横表前几行：

```text
##var    字段名
##type   字段类型
##group  c/s/e 分组
##       字段说明
```

数据从表头行之后开始。当前项目使用过的扩展类型包括 `vector2`、`vector3`、`vector4`、`vector2int`、`vector3int`，定义在 `GameConfig/Defines/builtin.xml`。
