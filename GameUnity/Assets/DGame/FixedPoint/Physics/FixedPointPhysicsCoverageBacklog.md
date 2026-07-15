# 定点物理碰撞与射线检测缺口清单

> 记录日期：2026-07-16  
> 适用目录：`Assets/DGame/FixedPoint/Physics`  
> 目的：记录当前已经实现、尚未实现、以及几何算法已存在但没有接入公共八叉树查询的能力，供后续补充时核对。

## 1. 范围与判定标准

本文把能力分成三层，避免把测试场景中的辅助检测误认为完整的运行时能力：

1. **基础几何算法**：`Intersection/FixedPointIntersection*.cs` 中两个几何体的直接相交计算。
2. **公共空间查询**：`FPPhysicsContext` / `FPOctree` 对八叉树中全部碰撞器执行的 Raycast、Overlap、Overlay、SphereCast。
3. **运行时求解**：`FPRigidbody`、`FPCharacterController` 是否会在正常物理更新中自动调用检测并处理碰撞响应。

符号说明：

- ✅：已有精确检测及 `FPCollision` 接触信息。
- ◐：只能判断是否重叠，缺少统一的接触点、法线、穿透深度或碰撞流形。
- ❌：当前没有对应的专用算法。

## 2. 基础普通碰撞检测矩阵

以下矩阵只表示基础几何算法是否存在，不代表所有算法都已经接入公共八叉树查询或自动碰撞求解。

| A \ B | Sphere | AABB | OBB | Capsule | Cylinder | AACapsule | Mesh |
|---|---:|---:|---:|---:|---:|---:|---:|
| Sphere | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| AABB | ✅ | ◐ | ◐ | ✅ | ❌ | ✅ | ❌ |
| OBB | ✅ | ◐ | ❌ | ✅ | ❌ | ✅ | ❌ |
| Capsule | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Cylinder | ✅ | ❌ | ❌ | ✅ | ❌ | ✅ | ❌ |
| AACapsule | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Mesh | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |

说明：

- AABB ↔ AABB、AABB ↔ OBB 当前主要提供布尔相交结果，测试场景会临时构造代表性法线和接触点，不等于生产级接触流形。
- Capsule 和 AACapsule 共用线段加半径的胶囊几何算法；AACapsule 只是中心轴固定为世界 Y 轴。
- CharacterController 的 Sphere 模式复用 Sphere 行；Capsule 模式复用 AACapsule 行。但不同公共查询是否遍历 `FpCharacterColliders` 仍需单独确认。
- Plane、Triangle、Circle 当前属于辅助几何体，不是八叉树碰撞器。已有 Sphere ↔ Plane、Sphere ↔ Triangle、Sphere ↔ Circle、Capsule ↔ Plane 等直接算法，但没有完整的碰撞器注册和空间查询链路。

## 3. 尚缺的普通碰撞检测

### 3.1 基础几何算法缺口

建议根据实际玩法需求依次补充：

| 缺失组合 | 当前影响 | 建议实现 |
|---|---|---|
| OBB ↔ OBB | 两个旋转盒不能进行精确普通碰撞 | 使用 15 轴 SAT，补充最小穿透轴、接触法线和接触点 |
| AABB ↔ Cylinder | 盒体与有限圆柱体不能直接检测 | 先做 AABB 粗筛，再实现有限圆柱与盒体最近点/SAT |
| OBB ↔ Cylinder | 旋转盒与有限圆柱体不能直接检测 | 转入 OBB 局部空间后检测 Cylinder ↔ AABB |
| Cylinder ↔ Cylinder | 两个有限圆柱体不能检测 | 处理侧面、端面和边缘组合，不能用胶囊近似替代精确圆柱 |
| AABB/OBB ↔ Mesh | 盒体不能与三角网格进行精确检测 | 对网格三角形做 BVH/八叉树粗筛，再执行 Box ↔ Triangle SAT |
| Capsule/AACapsule ↔ Mesh | 角色胶囊不能直接与 MeshCollider 精确碰撞 | 实现 Segment/Capsule ↔ Triangle 最近点并合并接触约束 |
| Cylinder ↔ Mesh | 圆柱不能与网格碰撞 | 实现有限圆柱 ↔ Triangle 或先明确允许的近似方案 |
| Mesh ↔ Mesh | 两个网格碰撞器不能互相检测 | 若确有动态网格需求，再实现 BVH ↔ BVH；否则明确禁止动态 Mesh ↔ Mesh |

### 3.2 已有布尔检测但接触信息不完整

- `IntersectWithAABBAndAABBFixedPoint` 只返回 `bool`。
- `IntersectWithAABBAndOBBFixedPoint` 只返回 `bool`。
- 后续若进入碰撞响应，需要统一返回：最小分离轴、法线、穿透深度、双方表面点和代表性接触点。

### 3.3 公共 Overlap / Overlay 查询覆盖不完整

| 公共查询 | 当前支持 | 尚缺 |
|---|---|---|
| `FPPhysicsContext.OverlayBoxCollision` | 仅 Sphere | AABB、OBB、Capsule、Cylinder、AACapsule、Mesh、Character |
| `FPPhysicsContext.OverlaySphereCollision` | Sphere、AABB、OBB、Capsule、Cylinder、AACapsule、Mesh | Character Sphere/Capsule |
| `FPOctree.OverlaySphereCollision` | Sphere、AABB、OBB、Capsule、Cylinder、AACapsule | Mesh、Character |
| `FPOctree.OverlayAACapsuleCollision` | Sphere、AABB、OBB、Capsule、Cylinder、AACapsule | Mesh、Character |
| `FPOctree.OverlayAABBCollisionCount` | Sphere、AABB、OBB | Capsule、Cylinder、AACapsule、Mesh、Character |
| `FPOctree.OverlapSphereNonAlloc` | Sphere、AABB | OBB、Capsule、Cylinder、AACapsule、Mesh、Character |

### 3.4 运行时碰撞求解限制

- `FPRigidbody` 当前只能绑定 `FPSphereCollider`，因此动态刚体只支持 Sphere ↔ Sphere 和动态 Sphere ↔ 已接入静态形状。
- AABB、OBB、Capsule、Cylinder、AACapsule、Mesh 尚不能作为通用动态刚体形状参与质量、冲量和位置约束求解。
- 测试场景 `FixedPointPhysicsTestController.TryIntersect` 支持的形状组合只用于可视化验证，不是生产环境的通用碰撞分派器。
- CharacterController 虽然具有 Sphere/Capsule 两种形态，但角色移动查询没有覆盖 Mesh；复杂场景地面若使用 `FPMeshCollider`，仍需补充角色胶囊与三角网格的生产查询和约束合并。

## 4. 射线检测现状与缺口

### 4.1 已有基础射线算法

- Ray ↔ Sphere
- Ray ↔ AABB（有限射线）
- 位移线段 ↔ AABB
- Ray ↔ OBB
- Ray ↔ Capsule
- Ray ↔ Plane
- Ray ↔ Triangle
- Ray ↔ Rounded AABB / Rounded OBB（供 SphereCast CCD 使用）

### 4.2 `FPPhysicsContext.Raycast` 已接入

- Sphere
- AABB
- OBB
- Capsule
- AACapsule
- CharacterController Sphere/Capsule

### 4.3 尚未接入或尚未实现

| 项目 | 当前状态 | 后续工作 |
|---|---|---|
| Ray ↔ Cylinder | 没有有限圆柱体射线算法 | 实现圆柱侧面与两个端盖的最近合法交点、法线和起点在内部语义 |
| Ray ↔ MeshCollider | 已有 Ray ↔ Triangle，但未遍历 Mesh 三角形 | 先用 Mesh AABB/BVH 粗筛，再遍历候选三角形并返回最近命中 |
| PlaneCollider / TriangleCollider | 只有直接几何算法，没有碰撞器和八叉树节点集合 | 确认是否需要正式碰撞器；需要时补注册、AABB、Raycast 分支和测试 |
| RaycastAll | 公共 API 只返回最近命中 | 增加可复用结果列表版本，并按距离排序或明确无序约定 |
| RaycastNonAlloc | 每次命中会创建 `FPRaycastHit` | 增加调用方提供数组/结果对象的零分配版本 |

### 4.4 射线边界语义尚未统一

- Ray 起点位于 Sphere、AABB、OBB 内部时，公共查询通常不报告命中。
- `IntersectWithRayAndCapsule` 当前可返回从内部离开 Capsule 的交点。
- 起点恰好位于表面、零距离命中和切线命中的行为在不同形状间也不完全一致。
- `FPRaycastHit.outPoint` 当前主要由 OBB 路径填写，其他形状通常保持零向量。

后续应先确定统一规范，再补回归测试：

1. 起点在内部时返回 `false`、返回 `t = 0`，还是返回离开点。
2. 起点在表面且方向朝外/朝内时的返回规则。
3. 相切是否算命中。
4. `point`、`normal`、`outPoint` 和距离字段在所有形状上的一致含义。

## 5. 高速连续碰撞检测（CCD）缺口

### 5.1 当前 `SphereCast` 已支持

- Sphere
- AABB（精确 Rounded AABB）
- OBB（精确 Rounded OBB）
- Capsule
- AACapsule
- CharacterController Sphere/Capsule

### 5.2 尚缺

| 缺口 | 说明 |
|---|---|
| SphereCast ↔ Cylinder | 需要实现扫掠球与有限圆柱体的首次接触时间，不能直接把圆柱当胶囊，否则端盖区域会误报 |
| SphereCast ↔ Mesh | 可转换为扫掠球 ↔ Triangle，并通过 Mesh BVH 只检测候选三角形 |
| CapsuleCast | 高速角色或胶囊物体目前没有连续扫掠查询 |
| BoxCast / OBB Cast | 高速盒体没有连续扫掠查询 |
| CylinderCast | 高速圆柱体没有连续扫掠查询 |
| 动态目标相对运动 CCD | 当前 SphereCast 检测的是查询时刻的目标快照，没有使用双方相对速度 |
| 自动接入物理步 | `FPRigidbody.OnUpdate` 仍先离散积分位置，未根据速度自动调用 SphereCast；测试发射器是手动调用 |
| SphereCastAll / NonAlloc | 当前只返回最近命中并创建一个 `FPRaycastHit` |

## 6. 已发现但尚未修复的缩放一致性问题

以下 `FPOctree` 查询仍存在使用碰撞器本地 `radius`、没有使用世界缩放后 `scaledRadius` 的调用点。后续补充检测时应一并修复并增加非均匀/负缩放测试：

- `OverlayBoxCollision` 检测 Sphere。
- `OverlayAABBCollisionCount` 检测 Sphere。
- `OverlapSphere` 检测 Sphere。
- `OverlayAACapsuleCollision` 检测 Cylinder。
- `OverlapSphereNonAlloc` 检测 Sphere。
- 文件内其他 `item.radius` 调用也应逐一确认是有意使用本地尺寸还是遗漏缩放。

另外，所有新算法应统一使用：

- Sphere：`scaledRadius`
- Capsule/AACapsule/Character：`scaledRadius`、`startPos`、`endPos`
- Cylinder：`scaledRadius`、`startPos`、`endPos`
- AABB/OBB：缩放后的 `halfSize`、`min`、`max` 和世界旋转矩阵

## 7. 建议实施顺序

### P0：先修正确性和公共查询一致性

1. 修复 `FPOctree` 中仍使用本地 `radius` 的缩放问题。
2. 统一 Raycast 起点在内部、表面和相切时的语义。
3. 为已有布尔盒体检测补完整 `FPCollision` 接触信息。
4. 明确测试场景辅助分派与生产查询支持范围，避免 UI 显示“支持”但运行时未接入。

### P1：补齐当前碰撞器的射线和高速检测

1. Ray ↔ Cylinder，并接入 `FPPhysicsContext.Raycast`。
2. Ray ↔ MeshCollider，复用 Ray ↔ Triangle 并加入候选三角形加速结构。
3. SphereCast ↔ Cylinder。
4. SphereCast ↔ MeshCollider。
5. 把 Sphere 刚体的高速移动按阈值接入 SphereCast CCD。

### P2：补普通碰撞形状矩阵

1. OBB ↔ OBB。
2. AABB/OBB ↔ Cylinder。
3. Cylinder ↔ Cylinder。
4. Capsule/AACapsule ↔ Mesh。
5. AABB/OBB/Cylinder ↔ Mesh。

### P3：扩展查询和动态刚体

1. CapsuleCast、BoxCast、CylinderCast。
2. RaycastAll、SphereCastAll 及 NonAlloc 版本。
3. 让 `FPRigidbody` 支持非球形动态碰撞器。
4. 只有明确存在玩法需求时再实现动态 Mesh ↔ Mesh。

## 8. 每项新增能力的最低测试要求

每个新组合至少补充以下用例：

- 正向命中与反向未命中。
- 相切。
- 起点位于形状内部和表面。
- 退化尺寸：零高度胶囊、零高度圆柱、零厚度盒等。
- 非均匀缩放和负缩放。
- 旋转 OBB/任意方向 Capsule/Cylinder。
- 八叉树父节点、子节点边界和跨节点大物体。
- 多目标时只返回最近命中；All 查询验证完整集合。
- 高速查询验证薄墙命中和圆角区域不误报。
- 定点确定性：同一输入多次运行结果完全一致。

## 9. 主要源码索引

- `Core/FPPhysicsContext.cs`：公共 Raycast、SphereCast、Overlay 查询。
- `Core/FPOctree.cs`：八叉树查询覆盖范围。
- `Core/FPRigidbody.cs`：当前仅球形刚体的离散积分与碰撞求解。
- `Intersection/FixedPointIntersectionRay.cs`：Ray、Rounded AABB/OBB。
- `Intersection/FixedPointIntersectionSphere.cs`：Sphere 与各形状。
- `Intersection/FixedPointIntersectionCapsule.cs`：Capsule、Cylinder、Ray。
- `Intersection/FixedPointIntersectionAACapsule.cs`：AACapsule 与盒体。
- `Intersection/FixedPointIntersectionCylinder.cs`：有限圆柱体当前仅有 Sphere 检测入口。
- `Samples/FixedPointPhysicsTestController.cs`：测试场景普通碰撞可视化分派，仅供测试。
- `Samples/FixedPointRayAndHighSpeedTestController.cs`：射线和高速发射器测试。
- `Tests/Editor/FixedPointRaycastAndCcdTests.cs`：Raycast 与 SphereCast 回归测试。

