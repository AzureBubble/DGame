![DGame](https://socialify.git.ci/AzureBubble/DGame/image?font=JetBrains+Mono&forks=1&issues=1&language=1&logo=https%3A%2F%2Funity.com%2F_next%2Fimage%3Furl%3Dhttps%253A%252F%252Fcdn.sanity.io%252Fimages%252Ffuvbjjlp%252Fproduction%252Faa5ce86e0add266e09ffa222609af4cc94befc7b-250x282.png%26w%3D256%26q%3D75&name=1&owner=1&pattern=Signal&pulls=1&stargazers=1&theme=Light)

<div align="center">

**Unity 游戏框架解决方案**

> 基于 TEngine 深度优化演进，为商业级游戏开发打造

[![Unity Version](https://img.shields.io/badge/Unity-2021.3.30%2B-blue.svg?style=flat-square)](https://unity3d.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg?style=flat-square)](LICENSE)
[![Last Commit](https://img.shields.io/github/last-commit/AzureBubble/DGame?style=flat-square)](https://github.com/AzureBubble/DGame/commits)
[![Issues](https://img.shields.io/github/issues/AzureBubble/DGame?style=flat-square)](https://github.com/AzureBubble/DGame/issues)
[![Stars](https://img.shields.io/github/stars/AzureBubble/DGame?style=flat-square)](https://github.com/AzureBubble/DGame/stargazers)

</div>

---

## 📖 简介

**DGame** 是在 [TEngine](https://github.com/ALEXTANGXIAO/TEngine) 基础上深度优化演进的 Unity 游戏框架。在保留 TEngine 核心优势（模块化架构、HybridCLR 热更新、YooAsset 资源管理）的同时，针对商业游戏开发需求新增了多个核心模块，并对现有系统进行了全面优化。

> 📝 本文档由 AI（Claude）自动生成，内容可能存在偏差，如有疑问请参考源代码或提交 Issue。
>
> 新增功能已在多个商业化游戏上经过验证。

### ✨ 核心特性

| 特性                   | 描述                                                      | 状态           |
| ---------------------- | --------------------------------------------------------- | -------------- |
| 🚀 **模块化设计**      | 高内聚低耦合的模块化架构，基于接口的模块管理              | 继承自 TEngine |
| 🔥 **热更新支持**      | 集成 HybridCLR，支持全平台 C# 代码热更新                  | 继承自 TEngine |
| 📦 **资源管理**        | 集成 YooAsset，支持资源分包、边玩边下                     | 继承自 TEngine |
| 📋 **配置系统**        | 基于 Luban 的配置表解决方案                               | 继承自 TEngine |
| 🌍 **多语言支持**      | 内置 8 种语言支持（中/英/繁/韩/日/越/印尼），接口驱动设计 | ⚡ 重新实现    |
| 🧩 **流程管理**        | 基于状态机的流程系统，管理游戏生命周期                    | 继承自 TEngine |
| 🛠️ **编辑器工具**      | 图集制作、一键打包、工具栏扩展、Spine 助手等便携工具      | 🆕 新增功能    |
| ⏱️ **计时器系统**      | 优化的 GameTimer，支持循环次数限制                        | ⚡ 优化改进    |
| 🎲 **内存池**          | MemoryCollector，语义清晰，支持批量释放                   | ⚡ 优化改进    |
| 🎨 **UI 框架**         | 完整的 UI 开发框架，丰富的组件扩展                        | ⚡ 增强改进    |
| 🔴 **红点系统**        | 树状结构红点管理，配置驱动，自动传播                      | 🆕 新增功能    |
| 🎬 **序列帧动画**      | 高效的序列帧动画系统，对象池管理                          | 🆕 新增功能    |
| 🎮 **GM 系统**         | 配置驱动的调试面板，快捷命令执行                          | 🆕 新增功能    |
| 💾 **数据中心**        | 统一的游戏数据管理，生命周期事件通知                      | 🆕 新增功能    |
| 🎯 **动画模块**        | 封装 Playable API，统一的动画接口                         | 🆕 新增功能    |
| 🕹️ **输入模块**        | 统一的输入处理模块，支持多点触控                          | 🆕 新增功能    |
| 🏊 **GameObject 池**   | 专用的 GameObject 对象池模块                              | 🆕 新增功能    |
| 📜 **SuperScrollView** | 高性能循环滚动视图，支持多种布局                          | 🆕 新增功能    |

---

## 📚 目录

- [与 TEngine 的差异](#-与-tengine-的差异)
- [架构概览](#-架构概览)
- [快速开始](#-快速开始)
- [新增功能详解](#-新增功能详解)
- [优化改进详解](#-优化改进详解)
- [核心模块](#-核心模块)
- [项目结构](#-项目结构)
- [系统要求](#-系统要求)
- [开发指南](#-开发指南)
- [贡献与支持](#-贡献与支持)

---

## 🆕 与 TEngine 的差异

### 新增模块（DGame 独有）

| 模块                   | 功能描述                                             | 适用场景                  |
| ---------------------- | ---------------------------------------------------- | ------------------------- |
| 🛠️ **编辑器工具**      | 图集制作、一键打包、工具栏扩展、Spine 助手等便携工具 | 开发效率提升              |
| 🔴 **红点系统**        | 树状结构红点管理，配置驱动，自动层级传播             | RPG、卡牌等游戏的红点提示 |
| 🎬 **序列帧动画**      | 高效序列帧播放，对象池复用，异步加载                 | 角色动画、特效动画        |
| 🎮 **GM 系统**         | 可视化调试面板，配置驱动，命令历史记录               | 开发调试、测试            |
| 💾 **数据中心**        | 统一数据管理，生命周期事件，持久化支持               | 玩家数据、游戏状态管理    |
| 🎯 **动画模块**        | 封装 Playable API，与 FsmModule 深度集成             | 角色状态机与动画联动      |
| 🕹️ **输入模块**        | 统一输入处理，支持多点触控和手势                     | 移动端游戏输入            |
| 🏊 **GameObject 池**   | 专用的 GameObject 对象池                             | 频繁创建销毁的对象        |
| 📜 **SuperScrollView** | 高性能循环滚动视图                                   | 列表、背包、排行榜        |

### 优化改进模块

| 模块            | TEngine            | DGame                   | 改进点                                             |
| --------------- | ------------------ | ----------------------- | -------------------------------------------------- |
| 🌍 **多语言**   | LocalizationModule | **ILocalizationModule** | 接口驱动、8 种语言支持、事件通知机制               |
| ⏱️ **计时器**   | TimerModule        | **GameTimer**           | 链表优化、循环次数限制、更友好的 API               |
| 🎲 **内存池**   | MemoryCollection   | **MemoryCollector**     | Spawn/Release 语义、批量释放支持                   |
| 🎨 **UI 系统**  | 基础组件           | **增强版**              | 编辑器代码自动生成、组件扩展、SuperScrollView 集成 |
| 📡 **事件系统** | GameEventMgr       | **EventDispatcher**     | 分离设计、接口包装支持                             |

---

## 🏗️ 架构概览

```
┌─────────────────────────────────────────────────────────────────┐
│                        游戏业务逻辑层                              │
│              (GameLogic - 热更新 DLL)                             │
├─────────────────────────────────────────────────────────────────┤
│                      框架核心层 (DGame)                            │
│  ┌──────────┬──────────┬──────────┬──────────┬──────────┐       │
│  │ 模块系统  │ 资源管理  │ UI 系统   │ 配置系统  │ 事件系统  │       │
│  ├──────────┼──────────┼──────────┼──────────┼──────────┤       │
│  │ 流程系统  │ 状态机🔗  │ 对象池    │ ⚡多语言  │ 🆕红点系统│       │
│  │          │ 🆕动画模块│          │          │          │       │
│  ├──────────┼──────────┼──────────┼──────────┼──────────┤       │
│  │ 🆕输入模块│ 🆕数据中心│ 🆕GM系统 │ 🆕序列帧  │ 🆕SuperSV │       │
│  └──────────┴──────────┴──────────┴──────────┴──────────┘       │
│     🔗 状态机与动画模块深度集成 | BaseState 内置动画播放方法         │
│     ⚡ 优化: GameTimer | MemoryCollector | 多语言                   │
├─────────────────────────────────────────────────────────────────┤
│                    🆕 编辑器工具层                                 │
│  UIGenerator │ RedDotEditor │ AtlasMaker │ ReleaseTools │ HybridCLR │
├─────────────────────────────────────────────────────────────────┤
│                        启动器层 (Launcher)                        │
│                    (AOT 代码，热更 DLL 加载)                       │
├─────────────────────────────────────────────────────────────────┤
│                        第三方库                                   │
│     HybridCLR │ YooAsset │ UniTask │ Luban │ DOTween             │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🚀 快速开始

### 环境要求

| 项目           | 要求                                   |
| -------------- | -------------------------------------- |
| **Unity 版本** | 2021.3.30f1c1（推荐）或更高            |
| **开发环境**   | .NET 4.x / .NET Standard 2.1           |
| **支持平台**   | Windows、Android、iOS                  |
| **IDE**        | Visual Studio 2019+ 或 JetBrains Rider |

### 快速上手

1. **克隆项目**

   ```bash
   git clone https://github.com/AzureBubble/DGame.git
   ```

2. **打开项目**
   - 使用 Unity 2021.3.30f1c1 打开 `GameUnity` 目录

3. **运行项目**
   - 打开 `Assets/Scenes` 下的 `LaunchScene` 启动场景
   - 点击运行按钮启动游戏

4. **热更新设置**（可选）
   - 运行菜单 `HybridCLR/Install...` 安装 HybridCLR
   - 运行菜单 `HybridCLR/Generate/All` 生成必要的代码
   - 构建热更新 DLL 和资源包

---

## 🎁 新增功能详解

### 🛠️ 编辑器便携工具 (Editor Tools)

完整的编辑器工具集，大幅提升开发效率。

**工具清单：**

| 工具                    | 功能           | 描述                                                  |
| ----------------------- | -------------- | ----------------------------------------------------- |
| **UI Script Generator** | 🎨 UI 代码生成 | 自动生成组件绑定代码、可视化组件管理、事件自动生成    |
| **RedDot Tree Editor**  | 🔴 红点配置    | 可视化红点树编辑、拖拽排序、代码自动生成、UIBase 集成 |
| **AtlasMakerTools**     | 图集制作       | 自动图集生成、配置化管理、精灵自动保存、图片后期处理  |
| **ReleaseTools**        | 一键打包       | 多平台构建、AB 包自动构建、增量构建、Jenkins 集成     |
| **HybridCLR 工具**      | 热更新构建     | DLL 编译、AOT 程序集处理、代码混淆、自动复制          |
| **LubanTools**          | 配置转换       | 表格数据转换、跨平台脚本执行                          |
| **UnityToolBarExtend**  | 工具栏扩展     | 左右工具栏增强、场景快速启动、播放模式控制            |
| **SpineModelHelper**    | Spine 助手     | Spine 模型辅助处理                                    |
| **Inspector 系统**      | 自定义界面     | 统一的 Inspector 基类、编译状态监听                   |
| **CreateGameEntry**     | 快速创建       | 一键创建 GameEntry 对象                               |

**ReleaseTools 一键打包示例：**

```csharp
// 支持的功能：
// - 多平台构建（Android/iOS/Windows 等）
// - 自动复制 StreamingAssets
// - LZ4 压缩
// - 自动版本生成（按 10 分钟分段）
// - Jenkins 集成支持

// 菜单：DGame/ReleaseTools/Build xxx
```

**AtlasMakerTools 图集制作：**

```
// 功能特点：
// - 配置化图集管理
// - 自动精灵保存
// - 图片后期处理
// - 支持 SpriteAtlas
```

**RedDot Tree Editor 红点配置：**

```
// 菜单：DGame Tools > RedDot > Tree Editor
// 功能特点：
// - 可视化树形结构编辑
// - 拖拽排序、节点增删、折叠展开
// - 三种红点类型：Dot（圆点）、New（标签）、Number（数字）
// - 两种聚合策略：Or（任一）、And（全部）
// - 一键生成代码，路径常量自动定义
// - 重复路径自动检测警告
// - 双击配置文件直接打开编辑器

// 开发流程：
// 1. 在编辑器中配置红点树结构
// 2. 点击 "Generate Code" 生成代码
// 3. 代码中使用 CreateRedDot() 创建红点（UIBase 集成）
// 4. 使用 SetState() 更新红点状态
```

**UI Script Generator UI 代码生成：**

```
// 菜单：GameObject/ScriptGenerator/生成到剪贴板
// 或在 UIBindComponent 组件编辑器中点击 "自动生成脚本"
// 功能特点：
// - 自动扫描 UI 层级结构
// - 基于命名规则自动识别组件类型（m_btn→Button、m_img→Image）
// - 自动生成组件绑定代码（ScriptGenerator 方法）
// - 自动生成事件处理框架（partial 方法）
// - 生成 _Gen.g.cs 只读文件，防止误修改
// - 支持原生 UI 和 TextMeshPro
// - 索引查找比 Find 更快

// 开发流程：
// 1. 创建 UI 界面，使用规范命名（如 m_btnStart、m_imgAvatar）
// 2. 在 UI 根节点添加 UIBindComponent 组件
// 3. 点击 "重新绑定组件" 自动扫描子对象
// 4. 点击 "自动生成脚本" 生成绑定代码
// 5. 在实现类中编写业务逻辑（无需关注组件查找）
```

---

### 🔴 红点系统 (RedDotModule)

完整的树状结构红点管理系统，**编辑器可视化配置 + 代码自动生成**，开发者只需关注红点更新和创建。

**🛠️ 编辑器可视化工具**

- 菜单：`DGame Tools > RedDot > Tree Editor`
- 可视化树形结构编辑界面
- 拖拽排序、节点增删、折叠展开
- 支持三种红点类型：Dot（圆点）、New（标签）、Number（数字）
- 支持两种聚合策略：Or（任一）、And（全部）
- 一键生成代码，路径常量自动定义
- 重复路径自动检测警告

**配置方式：**

```
1. 在编辑器中可视化配置红点树结构
2. 点击 "Generate Code" 生成路径定义代码
3. 代码中使用生成的常量 ID
```

**开发者只需关注：**

```csharp
// 1. 在 UI 中创建红点（UIBase 集成）
var redDot = CreateRedDot(RedDotPathDefine_Gen.Main.Bag.Equipment, parentTransform);

// 2. 更新红点状态
RedDotModule.Instance.SetState(RedDotPathDefine_Gen.Main.Bag.Equipment, true);

// 3. 设置红点数量
RedDotModule.Instance.SetNumber(RedDotPathDefine_Gen.Main.Quest.Daily, 5);

// 4. 监听红点变化（可选）
AddUIEvent(RedDotPathDefine_Gen.Main.Bag, OnRedDotChanged);
```

**编辑器配置示例（树形结构）：**

```
Main（主界面）
├── Mail（邮件）- Or 聚合
│   ├── System（系统邮件）
│   └── Player（玩家邮件）
├── Bag（背包）- Or 聚合
│   ├── Equipment（装备）- New 类型
│   └── Item（道具）- Number 类型
└── Quest（任务）- Or 聚合
    ├── Main（主线任务）
    └── Daily（日常任务）- Number 类型
```

---

### 🎬 序列帧动画系统 (FrameSprite)

高效的序列帧动画管理系统，基于对象池和图集优化。

**特性：**

- 对象池管理，减少 GC
- 异步加载支持（UniTask）
- SpriteAtlas 图集支持
- 播放完成回调
- 帧率控制

```csharp
// 播放序列帧动画
var frameSprite = await FrameSpriteMgr.PlayAsync(
    parent: transform,
    atlasName: "EffectAtlas",
    spriteNames: new[] { "frame_0", "frame_1", "frame_2" },
    frameRate: 30,
    loopCount: 1
);

// 播放完成回调
frameSprite.OnComplete += () => DLogger.Info("动画播放完成");

// 停止播放
frameSprite.Stop();
```

---

### 🎮 GM 系统 (GMPanel)

配置表驱动的可视化调试系统，支持快捷命令执行。

**特性：**

- Excel 配置驱动（TbGmConfig）
- 分类管理（客户端/服务器/批量/其他）
- 快捷按钮，一键执行
- 命令历史记录
- 支持参数传递

```csharp
// GM 命令配置（TbGmConfig）
// Id | Type      | Command           | Description
// 1  | Client    | AddGold 1000      | 添加金币
// 2  | Client    | SetLevel 50       | 设置等级
// 3  | Server    | KickAll           | 踢出所有玩家

// 代码中执行 GM 命令
GMModule.ExecuteCommand("AddGold 1000");

// 快捷方式
GMModule.ExecuteClientGm(GmCommandId.AddGold);
```

---

### 💾 数据中心 (DataCenterModule)

统一的游戏数据管理模块，支持生命周期事件和持久化。

**特性：**

- 统一的数据存储接口
- 生命周期管理（登录/登出/地图切换）
- 数据变更事件通知
- 持久化支持

```csharp
// 定义数据
public class PlayerData : DataBase
{
    public int Level;
    public long Gold;

    public override void OnInit() { }
    public override void OnEnterMap() { }
    public override void OnLeaveMap() { }
}

// 使用数据中心
var playerData = DataCenterModule.GetData<PlayerData>();
playerData.Gold += 100;

// 数据变更通知
DataCenterModule.NotifyDataChanged<PlayerData>();
```

---

### 🎯 动画模块 (AnimModule)

封装 Unity Playable API，提供统一的动画接口，**与状态机模块深度集成**。

**特性：**

- Unity Playable Graph 高性能动画播放
- 支持动画混合 (AnimMixer) 和动画节点 (AnimNode)
- 淡入淡出过渡效果
- 内存池管理，避免频繁创建销毁
- **与 FsmModule 无缝集成**，状态切换自动播放动画
- BaseState 内置动画播放方法

**与状态机集成使用：**

```csharp
// 1. 定义状态类（继承 BaseState）
public class IdleState : BaseState<Character>
{
    public override void OnEnter()
    {
        // 播放待机动画（状态机自动关联 Animator）
        PlayAnimation("Idle");
    }
}

public class MoveState : BaseState<Character>
{
    public override void OnEnter()
    {
        // 播放移动动画，支持淡入淡出
        PlayAnimation("Move", 0.2f);
    }

    public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        // 状态更新逻辑
        if (m_Owner.IsStopped)
            m_Fsm.ChangeState<IdleState>();
    }
}

// 2. 创建带动画的状态机（传入 Animator）
var character = GetComponent<Character>();
var animator = GetComponent<Animator>();

var fsm = FsmModule.CreateFsm("CharacterFsm", character, animator,
    new IdleState(), new MoveState());

// 3. 状态切换时动画自动过渡
fsm.ChangeState<MoveState>();  // 自动播放 Move 动画
```

**独立使用动画模块：**

```csharp
// 获取动画模块
var animModule = ModuleSystem.GetModule<IAnimModule>();

// 播放动画
animModule.Play("Idle");
animModule.Play("Run", 0.2f); // 淡入淡出时间

// 动画混合
animModule.PlayMixer("Attack", "Walk", weight);
```

---

### 🕹️ 输入模块 (InputModule)

统一的输入处理模块，支持多点触控和手势。

**特性：**

- 统一的输入接口
- 多点触控支持
- 手势识别（点击、长按、拖拽）
- 输入阻断

```csharp
// 获取输入模块
var inputModule = ModuleSystem.GetModule<IInputModule>();

// 监听点击
inputModule.OnClick += (position) => { };

// 监听长按
inputModule.OnLongPress += (position, duration) => { };

// 监听拖拽
inputModule.OnDrag += (delta) => { };
```

---

### 🏊 GameObject 对象池 (GameObjectPoolModule)

专用的 GameObject 对象池模块。

**特性：**

- 自动扩展和收缩
- 支持预设初始化
- 对象复用统计

```csharp
// 获取对象池
var pool = ModuleSystem.GetModule<IGameObjectPoolModule>();

// 获取对象
var obj = pool.Spawn("Bullet");

// 归还对象
pool.Despawn(obj);
```

---

### 📜 SuperScrollView

高性能循环滚动视图组件。

**特性：**

- ListView、GridView、StaggeredGridView
- 虚拟滚动，仅渲染可见项
- 异步加载支持
- 丰富的回调事件

```csharp
// 创建循环列表
var loopListView = transform.GetComponent<LoopListView2>();

// 初始化
loopListView.InitListView(itemCount, (index, item) => {
    var itemLogic = item as MyItem;
    itemLogic.SetItemData(index);
    return itemLogic;
});
```

---

## ⚡ 优化改进详解

### ⏱️ 计时器系统优化

**TEngine (TimerModule)：**

```csharp
// 使用整数 ID 管理
public int AddTimer(TimerHandler callback, float time, bool isLoop = false, ...)
```

**DGame (GameTimer)：**

```csharp
// 返回 GameTimer 对象，可操作性强
public GameTimer CreateLoopGameTimer(float interval, TimerHandler handler, object[] args = null)
public GameTimer CreateOnceGameTimer(float delay, TimerHandler handler, object[] args = null)

// 支持循环次数限制
timer.LoopCount = 3;  // 只循环 3 次
timer.Paused = true;  // 暂停
timer.Stop();         // 停止
```

**改进点：**

- ✅ 使用 DGameLinkedList（优化的链表结构）
- ✅ 支持循环次数限制
- ✅ 返回对象而非 ID，操作更直观
- ✅ 更高效的 Bad Frame 处理

---

### 🎲 内存池优化

**TEngine (MemoryCollection)：**

```csharp
public T Acquire<T>() where T : class, IMemory, new()
public void Release<T>(T memory) where T : class, IMemory
```

**DGame (MemoryCollector)：**

```csharp
// 语义更清晰
public T Spawn<T>() where T : class, IMemory, new()
public void Release<T>(T memory) where T : class, IMemory

// 支持批量释放
public void Release<T>(List<T> memories) where T : class, IMemory

// 更详细的信息
public int Capacity { get; }  // 容量
```

**改进点：**

- ✅ Spawn/Release 语义更符合游戏开发习惯
- ✅ 支持批量释放，提高效率
- ✅ 新增 Capacity 属性

---

### 🎨 UI 系统增强

**🛠️ 编辑器代码自动生成：**

- 自动扫描 UI 层级结构，生成组件绑定代码
- 基于命名规则自动识别组件类型（`m_btn` → `Button`、`m_img` → `Image`）
- 自动生成 `ScriptGenerator()` 方法，无需手动编写 Find 查找
- 自动生成事件处理框架（partial 方法）
- 生成 `_Gen.g.cs` 只读文件，防止误修改
- 索引查找比 Find 更快，性能优化
- 开发者只需关注核心业务逻辑开发

**生成代码示例：**

```csharp
// 自动生成的代码（MainWindow_Gen.g.cs）- 只读
private UIBindComponent m_bindComponent;
private Text m_textTitle;
private Button m_btnStartGame;

protected override void ScriptGenerator()
{
    m_bindComponent = gameObject.GetComponent<UIBindComponent>();
    m_textTitle = m_bindComponent.GetComponent<Text>(0);
    m_btnStartGame = m_bindComponent.GetComponent<Button>(1);
    m_btnStartGame.onClick.AddListener(OnClickStartGameBtn);
}

private partial void OnClickStartGameBtn();  // 事件声明

// 开发者只需在实现类中编写业务逻辑
public partial class MainWindow
{
    protected override void BindMemberProperty()
    {
        // 初始化业务逻辑
        m_textTitle.text = "游戏标题";
    }

    private void OnClickStartGameBtn()
    {
        // 游戏逻辑
        GameModule.SceneModule.LoadScene("GameScene");
    }
}
```

**组件扩展：**

| 组件             | 功能                                 |
| ---------------- | ------------------------------------ |
| **UIButton**     | 点击保护、缩放效果、音效、长按、双击 |
| **UIImage**      | 镜像翻转、圆角、遮罩                 |
| **UIText**       | 渐变色、字间距、圆形排列、阴影       |
| **RichTextItem** | 图文混排、超链接、动画表情           |

**示例：**

```csharp
// UIButton 扩展
btn.ClickProtect = true;           // 点击保护
btn.ClickScale = 0.9f;             // 点击缩放
btn.OnLongPress += OnButtonLongPress;  // 长按事件

// UIText 渐变
text.SetGradient(new Color[] { Color.red, Color.blue });
```

---

## 🧩 核心模块

### 模块系统

完整的模块管理系统，所有核心系统都实现了标准模块接口：

```csharp
// 获取模块
var resourceModule = ModuleSystem.GetModule<IResourceModule>();
var fsmModule = ModuleSystem.GetModule<IFsmModule>();
```

| 模块             | 接口                    | 描述                   | 来源        |
| ---------------- | ----------------------- | ---------------------- | ----------- |
| 🎮 Mono 驱动     | `IMonoDriver`           | Unity 生命周期驱动     | TEngine     |
| 📦 资源管理      | `IResourceModule`       | YooAsset 资源管理      | TEngine     |
| 🔄 状态机        | `IFsmModule`            | 有限状态机管理         | TEngine     |
| 🔊 音频          | `IAudioModule`          | 背景音乐与音效管理     | TEngine     |
| 🎬 场景          | `ISceneModule`          | 场景加载与切换         | TEngine     |
| 🌍 多语言        | `ILocalizationModule`   | 8 种语言支持，接口驱动 | ⚡ 重新实现 |
| 🏊 对象池        | `IObjectPoolModule`     | 对象池管理             | TEngine     |
| ⏱️ 计时器        | `IGameTimerModule`      | 游戏计时器             | ⚡ 优化     |
| 🎲 内存池        | `MemoryPool`            | 非托管内存池           | ⚡ 优化     |
| 🎯 动画          | `IAnimModule`           | 动画控制               | 🆕 新增     |
| 🕹️ 输入          | `IInputModule`          | 输入处理               | 🆕 新增     |
| 🏊 GameObject 池 | `IGameObjectPoolModule` | GameObject 对象池      | 🆕 新增     |

---

## 📁 项目结构

```
DGame/
├── GameConfig/              # 游戏配置（Luban）
│   ├── Datas/               # Excel 配置表
│   ├── CustomTemplate/      # 自定义代码模板
│   └── Tools/               # Luban 工具
├── GameRelease/             # 发布版本输出
├── GameUnity/               # Unity 主项目
│   ├── Assets/
│   │   ├── DGame/           # DGame 框架核心
│   │   │   ├── Editor/      # 🆕 框架编辑器工具（完整便携工具集）
│   │   │   │   ├── AtlasMakerTools/     # 图集制作工具
│   │   │   │   │   ├── 图集配置管理
│   │   │   │   │   ├── 精灵自动保存
│   │   │   │   │   └── 图片后期处理
│   │   │   │   ├── ReleaseTools/        # 一键打包工具
│   │   │   │   │   ├── 多平台构建
│   │   │   │   │   ├── AB 包自动构建
│   │   │   │   │   ├── Jenkins 集成
│   │   │   │   │   └── 版本自动生成
│   │   │   │   ├── HybridCLR/           # 热更新构建工具
│   │   │   │   │   ├── DLL 编译
│   │   │   │   │   ├── AOT 程序集处理
│   │   │   │   │   └── 代码混淆
│   │   │   │   ├── LubanTools/          # 配置转换工具
│   │   │   │   ├── UnityToolBarExtend/  # 工具栏扩展
│   │   │   │   │   ├── 左右工具栏增强
│   │   │   │   │   ├── 场景快速启动
│   │   │   │   │   └── 播放模式控制
│   │   │   │   ├── SpineModelHelper/    # Spine 助手
│   │   │   │   ├── Inspector/           # 自定义 Inspector
│   │   │   │   └── Utility/             # 编辑器工具集
│   │   │   └── Runtime/     # 框架运行时代码
│   │   │       ├── Core/                # 核心系统
│   │   │       │   ├── DGameLog/        # 日志系统
│   │   │       │   ├── MemoryPool/      # 内存池（⚡ 优化）
│   │   │       │   ├── ModuleSystem/    # 模块系统
│   │   │       │   └── GameEvent/       # 事件系统
│   │   │       └── Module/              # 功能模块
│   │   │           ├── GameTimer/       # 计时器（⚡ 优化）
│   │   │           ├── AnimModule/      # 动画模块（🆕 新增）
│   │   │           ├── InputModule/     # 输入模块（🆕 新增）
│   │   │           ├── LocalizationModule/  # 多语言（⚡ 重新实现）
│   │   │           └── ...
│   │   ├── Scripts/
│   │   │   ├── HotFix/      # 热更新代码
│   │   │   │   ├── GameBase/      # 基础框架程序集 [Dll]
│   │   │   │   ├── GameProto/     # 配置协议程序集 [Dll]
│   │   │   │   └── GameLogic/     # 业务逻辑程序集 [Dll]
│   │   │   │       ├── Module/    # 游戏内模块
│   │   │   │       │   ├── RedDotModule/      # 红点系统（🆕 新增）
│   │   │   │       │   ├── DataCenter/        # 数据中心（🆕 新增）
│   │   │   │       │   └── ...
│   │   │   │       ├── UI/        # UI 界面
│   │   │   │       │   └── Module/Expansion/  # UI 组件扩展（⚡ 增强）
│   │   │   │       │       ├── SuperScrollView/  # 循环滚动（🆕 新增）
│   │   │   │       │       └── ...
│   │   │   │       ├── Common/
│   │   │   │       │   └── FrameSprite/       # 序列帧动画（🆕 新增）
│   │   │   │       └── GMPanel/   # GM 系统（🆕 新增）
│   │   │   └── Launcher/    # 启动器代码（AOT）
│   │   ├── BundleAssets/    # 可热更资源
│   │   ├── AssetArt/        # 美术资源
│   │   ├── Plugins/         # 第三方插件
│   │   └── Scenes/          # Unity 场景
│   └── Packages/            # Unity 包管理
├── Tools/                   # 开发工具
└── UnityPackage/            # Unity 包导出
```

---

## 💻 系统要求

### Unity 版本

- **推荐版本**: Unity 2021.3.30f1c1
- **支持版本**: Unity 2021.3 LTS+
- **Unity 6000+**: 已适配 Unity 6

### 平台支持

| 平台                 | 支持状态    | 备注       |
| -------------------- | ----------- | ---------- |
| Windows (Standalone) | ✅ 完全支持 |            |
| Android              | ✅ 完全支持 | 支持热更新 |
| iOS                  | ✅ 完全支持 | 支持热更新 |

---

## 📖 开发指南

### 计时器使用（⚡ 优化版）

```csharp
// 创建循环计时器（支持循环次数限制）
var timer = GameTimer.CreateLoopGameTimer(
    interval: 1f,
    handler: () => DLogger.Info("每秒执行"),
    args: null
);
timer.LoopCount = 5;  // 只执行 5 次

// 创建一次性计时器
GameTimer.CreateOnceGameTimer(
    delay: 2f,
    handler: () => DLogger.Info("2秒后执行")
);
```

### 红点系统使用（🆕 新增）

```csharp
// 刷新红点
RedDotModule.Instance.RefreshRedDot(RedDotPathDefine_Gen.Main_Bag_Equipment);

// 监听红点变化
AddUIEvent(RedDotPathDefine_Gen.Main_Bag, OnRedDotChanged);

// 设置红点数量
RedDotModule.Instance.SetRedDotCount(RedDotPathDefine_Gen.Main_Bag_Props, 5);
```

### 序列帧动画（🆕 新增）

```csharp
// 播放序列帧
var frameSprite = await FrameSpriteMgr.PlayAsync(
    parent: transform,
    atlasName: "EffectAtlas",
    spriteNames: spriteNameArray,
    frameRate: 30,
    loopCount: 1
);
```

### GM 命令（🆕 新增）

```csharp
// 执行配置好的 GM 命令
GMModule.ExecuteCommand("AddGold 1000");

// 快捷执行
GMModule.ExecuteClientGm(GmCommandId.AddGold);
```

### UI 开发

```csharp
public class MyWindow : UIWindow
{
    private Button m_btnClose;

    protected override void ScriptGenerator()
    {
        m_btnClose = FindChildComponent<Button>("m_btnClose");
        // UIButton 扩展功能（⚡ 增强）
        m_btnClose.ClickProtect = true;
        m_btnClose.ClickScale = 0.9f;
    }

    protected override void OnShow()
    {
        // 显示窗口
    }
}

// 异步显示窗口
await UIModule.Instance.ShowWindowAsync<MyWindow>(userData);
```

---

## 🤝 贡献与支持

### 贡献

欢迎提交 Issue 和 Pull Request！

### 开源协议

本项目采用 [MIT License](LICENSE) 开源协议。

### 致谢

本项目基于 [TEngine](https://github.com/ALEXTANGXIAO/TEngine) 进行深度优化演进，感谢 TEngine 项目的优秀架构设计。

---

<div align="center">

**Made with ❤️ by DGame Team**

基于 [TEngine](https://github.com/ALEXTANGXIAO/TEngine) 深度优化演进

[⭐ Star](https://github.com/AzureBubble/DGame) | [🐛 Issues](https://github.com/AzureBubble/DGame/issues) | [📖 Wiki](https://github.com/AzureBubble/DGame/wiki)

</div>


