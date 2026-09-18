> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

> **历史草案，已被替代（2026-09-14）**：本文件保留供追溯，不作为当前开发指令。请以 [新版完整架构 0.4-R2](03_ARCHITECTURE.md) 和用户最新消息为准。最新流程是先在隔离实验区完成 **G1 Grip/Twist 机制**与 **G2 抽象 hub 具体前端**的可操作版本，分别交给用户独立审核，两项都明确通过后再继续正式集成。旧布局、单审核门槛、旧 Prompt 和缺少 Next／每 orbit 多套键位的描述均已被替代。当前英文开发 Prompt 直接提供在对话中，不另输出文件。
# 抽象视角与 piece 工作台：精细设计稿

状态：设计提案。这里定义的是要实现并审阅的行为，不宣称已经存在于 0.3。所有样例明确区分真实模型数据与尚待实现的 UI。

## 1. 先用实际复原过程决定画什么

三项核心机制是piece-focused、keybind与抽象视角：持续追踪工作对象、快速输入明确转动，以及基于macro base组织和复用人为复原操作。抽象视角的核心是转动之外的选宏、指定对象/reference、组合、验算与保存模板；Local/Global的结构解释服务这些操作。Solve是共同工作区。

长期复原的基本单位是一个 preparation cycle，不是一个窗口，也不是一次 twist。一个 cycle 可能包含多段 macro；多个目标也可能沿用同一组 buffer、grip 和检查条件。

界面持续保留以下对象：

| 对象 | 用户需要知道的内容 | 什么时候改变 |
| --- | --- | --- |
| Target | 本次要完成的固定位置及所需身份 | 用户明确换目标 |
| Piece | 要追踪的身份、当前所在位置 | 身份不变，位置随已提交操作变化 |
| Buffers | A/B 固定位置、当前占用者、参考 frame | 位置由已验证框架决定，占用者随状态变化 |
| Plan | 用户输入的 Prepare / Macro / Cleanup | 用户编辑或替换 |
| Template | 用户保存的组合方式、明确角色和输入条件 | 用户明确编辑；每次使用形成新实例 |
| Protection | 哪些 orbit / block 必须保留、按什么边界检查 | 用户明确改变策略 |
| Working frame | 按键和宏解释使用的机械参考系 | 用户明确改 frame；相机变化不影响它 |

任何视图都必须能回答它服务于哪个对象、哪个已提交状态。既不能因为切换小窗丢掉 Target，也不能因为 hover 临时覆盖正在追踪的 Piece。

### 一次典型使用

用户选择自己的macro或操作模板，再在active orbit中明确选一个目标。主视图、Local和Global标出同一个required piece的Current/Home，A/B角色保持不变。用户在抽象工作台填入本次reference和角色，检查已有macro是否适配；需要preparation时，用自己的Grip/Twist输入。软件分析完整组合，在同一处指出不匹配的frame或保护冲突。用户修改并执行后，保留模板、相机、filter、键位、block与工作上下文。先选piece再选模板也可以，不强制单一路径。

下一次循环不再重新打开 Analyzer、再输入 ID、再选保护列表、再找 macro。这才是本次抽象视角的收益。

## 2. 主视图、Local、Global、Macro 的分工

| 视图 | 主问题 | 默认内容 | 不应承担的任务 |
| --- | --- | --- | --- |
| Main | 我实际在哪里操作？ | 实际 puzzle，当前工作范围，目标与 buffer 注释 | 不塞完整关系表和所有 ID |
| Local | 当前 piece 的结构与目标 frame 怎么对应？ | Current/Home 局部结构、sticker 对应、可用 grip | 不显示全部 600-cell 的精细几何 |
| Global | 工作区域与目标区域在整体里是什么关系？ | 两组 cell 的真实投影位置及可切换拓扑关系 | 不用图上路径替代合法 setup |
| Macro workbench | 怎样把已有macro组织成本次操作，并在下次复用？ | macro base、模板、人工角色绑定、三段组合、作用与保护检查 | 不搜索解法、自动选下一piece或批量执行 |

Macro effect 是操作台内的检查视图，也是Macro base共用的效果组件，不另建第二套分析结果。Local/Global可同时打开为owned小窗，也可dock到工作区；小屏使用标签/抽屉。主视图始终可操作。

## 3. 统一对象语义，防止最常见的认知错误

内部 `state.at[position]` 给当前位置的身份，`state.where[identity]` 给身份的当前位置。UI 使用明确的 `Piece` 与 `Position` 标签，不把两个整数混写为一个 ID。

对目标位置 d：

- `Target = d`；它需要的身份也是 d，这是当前模型的 Home 身份约定。
- `Required piece = d`；其当前位置为 `where[d]`。
- `Occupant at target = at[d]`；它可能是另一个身份，不能用它替换 required piece。
- `Current cells = hosting(where[d])`；`Home cells = hosting(d)`。
- 当前 A/B 是固定位置；它们的 occupant 可能正好包含 required piece。

页面顶部的工作条示例：

`O33 · Target position 35778 · Required piece 35778 · Now at 2712 · Buffer A`

这是一条连贯的关系，不是五张状态卡。点击 Position 只检查位置，点击 Piece 只追踪身份；`Use as target` 是独立动作。Ctrl+复制等行为只复制对应的带类型 ID。

### 所有 ID 的共同入口

统一定位框接受带类型对象：`C113`、`V17`、`O33`、`piece:35778`、`pos:2712`、`slot:48927`、`macro:<name>`、`block:<name>`、`event:<id>`。这些是**拟新增的导航语法**，不改变已有 filter 或日志语法。

查到对象后先显示语义和状态，再允许 `Inspect`、`Set target`、`Add to filter`、`Bind grip` 等适用动作。错误或缺失对象就地说明，不能猜测类型或偏移一位。C1–C600、V1–V120 与原内部索引的对应保留；既有 orbit 显示规则必须先审计，示例的 O33 明确表示内部 moving orbit 33。

## 4. Local：piece 的结构与 Current/Home 对照

### 4.1 默认布局

Local 不是只画几个漂浮的 cell center。它分成三个连贯区域：

1. **Current / Home 对照面**：并排呈现同一个 piece 在当前位置的结构与目标位置结构；使用稳定参考 frame，明确注明当前比较的是哪两个位置。
2. **Cell 与 sticker 对应行**：每一行对应这个 piece 的一个 sticker/hosting cell，列出 canonical color、current cell、home cell、frame 状态和当前 grip key。
3. **当前 grip 的结构放大**：只在选择一个 grip 时展开，显示该 tetrahedral cap 的四个顶点、七条旋转轴、当前 piece 所在的切割区域，以及本次按键的方向标记。

真实片块几何可用时，Current/Home 采用从同一 immutable 模型提取的局部 patch。若尚未具备精细片块网格映射，显示真实 cell incidence 和精确 sticker 对应，并明确标为 `Cell structure`；不能生成一个好看的近似 piece 冒充模型。

### 4.2 结构如何展开

- 对 1-cell piece：一组局部 tetrahedral patch 加上当前位置/目标位置对照。不要强行制造多瓣图。
- 对 2-cell piece：沿共同结构展开两个实际 cell 的局部 patch，共有边界明确标记。两个 sticker 的对应可以直接阅读。
- 对 5-cell piece：按真实 incidence 显示围绕共享结构的五个 cell；顺序来自模型，不按屏幕距离排序。
- 对 20-cell piece：给出以该 vertex 为中心的实际 20-cell 局部 incidence 展开/投影；默认只标 anchor、当前选择和发生差异的 cell。旁边提供 20 行虚拟化对应表，分组/折叠不会重新编号。

展开后距离不再等于原 4D 距离。标题使用 `Local incidence` 或 `Exploded cell structure`；几何真实投影模式才使用 `Geometry`。两种模式不能共用一个没有解释的距离标尺。

### 4.3 Orientation 不能统一画成一个“旋转角”

- 无 orientation 自由度的 orbit 显示 `Orientation: trivial`，不画假的旋转轮。
- C2 类以明确的 sticker 交换/未交换显示。
- 其他 orbit 使用其已验证的 frame permutation。20-cell vertex 的 orientation 是 A5、60 种；不能用“顺时针/逆时针几档”代替。
- piece 尚未到 Home 时，显示的是“相对于所选 comparison frame 的 sticker 对应”，不是现有 progress 的 `orientation_wrong`。
- 现有 `orientation_wrong` 只统计已经 position-correct 但不 exact-correct 的 piece；全局统计继续保留这个定义。
- 没有唯一 comparison frame 时，显示 `Choose frame` 和离散候选；不自行选择一个看起来最顺眼的 frame。

Current/Home 中相应的 sticker 使用同一个短编号。选一行会在两个结构中显示对应位置，并在 Global 标记相关 cell。只有当前选中的对应连线显示，默认不铺 20 条穿越全图的线。

### 4.4 Current/Home 相机

默认对齐参考 frame，便于比较，不随着主相机漂移。用户可选择 `Linked orientation` 同步旋转两边，也可分别旋转。`Align for comparison` 只改视图。

主相机、Local 相机、Global 相机与机械 frame 相互独立。Local 底部有两个极小的定位缩略图，分别只标当前位置和 Home 所在区域；点缩略图只放大 Global 对应区域。`Center main` 必须明确点击或按绑定键。

### 4.5 Filter 在 Local 中怎样表达

每个对象分成三种状态：

- `Interactive`：当前显示 filter 允许实际操作。
- `Reference only`：为理解 Current/Home/buffer 保留的注释，可在表格中检查，但不能在 puzzle 几何上 twist 或穿透拾取。
- `Hidden by <rule>`：具体被哪条规则排除；对象仍在分析中，不从保护和影响计算消失。

Local 默认保留 required piece、Home 和 A/B 的必要 reference。这是独立的 context layer，不偷偷改用户复杂 filter。`Reveal in work filter` 先展示 Union 后的表达式与计数，用户应用后才变为可操作。

Grip hover 只画该明确输入将影响的结构。它不能清空 filter、改变保护或调用自动 insertion。

### 4.6 Piece filter 工作台的具体样子

从 index 打开 Filter 后，保持同一 Target、Local 和宏草稿。左侧是已保存条件，中间是可嵌套的条件行，底部是结果预览与 Apply；所选条件对应的对象可以在 Local/Global 中检查。

每行选择对象来源、判断条件和集合运算。组合组使用 `All / Any / Not`，再以 `Replace / Intersect / Union / Subtract` 决定怎样应用到当前 work filter。组内的逻辑与“怎样应用”分开显示，避免用户以为点了 Union 就改掉整个表达式。

条件来源包括已有 orbit、cell、color、layer、完成状态，以及拟新增的 Target/Required piece、A/B occupants、block members、已审阅 macro support。新条件是设计提案，不能将下面的自然语言例子直接当作当前程序已支持的输入语法。

三个具体组合例子：

1. **处理当前 orbit**：当前 orbit 中尚未 exact solved 的位置，排除已锁定 block；Required piece 和 A/B 另外作为 reference 保留。这样找工作对象时不会把保护成果混进待办。
2. **检查自己输入的 macro**：完整 macro 的净 support 与当前 orbit 取交集，再并入 Required piece 当前所在位置。其他 orbit 的 collateral 仍在作用表中，保护检查照常覆盖全模型。
3. **挑选 block 边界**：当前 orbit 中未完成、且 hosting cells 与当前 block 共享 cell 或面相邻的候选。邻接是明确选择的条件；不因为结构相邻就声称可用某个 macro 完成。

预览展示唯一 piece 数、sticker 数、增加与移除数量，并能查看少量具体成员及被哪条条件排除。用户可从某个 ID 反查命中的条件，不必对着长表达式猜。大型集合只分页显示结果，真实计算不截断。

保存时区分：`Frozen identities` 跟着这些身份移动；`Frozen positions` 固定观察这些位置；`Live query` 随状态重新求值。像“当前 buffer occupant”这样的 live 条件在每次相关状态变更后重新计算，结果标明来源 revision；它不会自动把新的 occupant 设为 Target。

如果引用的 block 或 macro 被删除、分析过期或条件无效，保留原 work filter，指出失效来源。没有匹配结果时显示 Empty result，并保留 target/reference 与恢复入口；不要自动放宽条件。

## 5. Global：两组 cell 的空间位置与关系

### 5.1 默认比较对象

默认比较 `Current hosting cells` 与 `Home hosting cells`。可通过同一选择器改为 `Buffer A ↔ Target`、`Buffer B ↔ Target`、`Macro reference ↔ Chosen reference`，或用户固定的两组 cell。

比较 selector 明确显示每组来源：身份当前落点、固定位置、冻结 cell 集合或 live query。不要统一写成模糊的“Group A/B”与 Buffer A/B 混淆。

### 5.2 主画面用真实几何位置

背景是保留模型的 600 个 tetrahedral cells 的低细节投影。绝大部分只保留淡轮廓/中心点；重点两组显示真实 cell 轮廓、组标记和关键 C ID。不能默认写 600 个标签。

用户需要知道的是“两个工作区域在整体哪边、是否重叠、怎样从一个找到另一个”。为此提供：

- `Fit both`：将两组同时纳入视野，保留当前目标；不修改主相机。
- `Center current` / `Center target`：仅改此视图。
- 两组部分重叠时，把共有 cell 作为同一对象显示，边上标记双重角色，不复制成两个节点。
- 相机投影造成重合时显示 `Projection overlap`，可调整投影或展开对应表；不能判为拓扑相邻。
- 当前在切面外/被裁剪的对象仍有方位标记和文字入口，不伪造屏幕内位置。

### 5.3 拓扑关系作为可切换层

拓扑只使用 retained model 中的 cell-face adjacency：两 cell 共享一个三角面才连一条邻接边。对两组集合 U/V，给出：共有 cell 数、跨组邻接对、最小 shared-face hop distance，以及用户选中 pair 的具体关系。

默认展示最短关系的一个明确实例；存在多条最短路线时显示数量/可翻阅入口，不同时铺满所有路线。选择某条路线只是结构导航，可转成一组待预览的 filter 条件。

**该路线不是 piece 的合法转动路径，也不是待执行 setup。** UI 直接标记 `Cell adjacency · not move sequence`。把图上的路线加入 Plan 的按钮不存在。

### 5.4 方位解释的精度

普通用户看到当前/目标几何位置、selected anchor 和 cell IDs 即可。高级展开可给两个归一化 cell normals 的夹角，以及相对于固定目标 cell frame 的分量。

这仅描述 cell centers 的 4D 关系；多个 cell 的集合不存在默认唯一朝向，不用一个任意平均点给它编造 frame。反极点或参考退化时说明不唯一。比较两个 macro reference frame 时必须用已验证的离散对称映射，不拿投影后的角度判断等价。

### 5.5 关系线不能混用

| 线/标记 | 含义 | 显示策略 |
| --- | --- | --- |
| 细实线 | 真实共享三角面的 cell adjacency | 仅当前邻域与选中关系 |
| 带箭头的效果线 | 已输入 macro 的位置映射 | 只在 Macro effect 层，图例明确 |
| 对应连接 | 用户选中的 Current/Home sticker 对应 | 一次高亮一个或少量对应 |
| 虚线轮廓 | reference-only 或计划后的 ghost | 标签明确 before/after |

不要用同一种发光线同时表示邻接、目标、移动和保护。不要默认叠加所有层。

### 5.6 在 Global 中选东西

点一个 cell：只更新 Inspect focus，与 Local 的检查选择联动。

点当前 required piece 的标记：Local 回到这次工作目标，但不改变 Target。

`Use as target`、`Add cell predicate`、`Use for grip`、`Center main` 是四个不同动作，通过索引和上下文菜单也能键盘完成。

`Pin comparison` 固定两组来源；pin cell set 与 follow identity 两者要显示清楚。用户在其他面板 hover 不应跳转比较。

## 6. Macro workbench：以macro base形成可重复的人为操作

### 核心交互只保留四步

**选用 → 指定 → 组合 → 检查与复用。** 执行沿用全工作台已有的明确commit，不另造一套执行入口。

| 操作 | 用户具体做什么 | 软件可以做什么 | 界面保留什么 |
| --- | --- | --- | --- |
| 选用 | 从自己的macro base选明确的macro或已存模板 | 展示真实作用与已知适用条件，按用户选的条件过滤 | 宏名称、内容版本、reference和用途 |
| 指定 | 把本次piece/目标填入角色，明确选参考cell/frame | 检查orbit、角色、frame；执行已验证且用户明确选用的参考变换 | 实际Piece/Position/C ID与对应关系 |
| 组合 | 选择Prepare/Macro/Cleanup；需要时用keybind补人工转动 | 展开用户指定的有限操作，对选定段计算明确的逆 | 简单的三段工作单与来源 |
| 检查与复用 | 看完整影响，修订、保存模板或执行；下一次自己选新对象 | 验算目标/保护，指出缺少的输入，保留重复设置 | 模板、键位、buffer、filter与人工说明 |

“指定”只把对象填入角色，不移动piece。图上把piece放进一个角色槽，是表达“这次我要用它”；能否与所选macro对应，要看真实作用和frame，不能靠拖到位置就视为准备完成。

### 操作模板的具体边界

Macro是一段确定的合法操作。模板是用户如何调用已有macro的工作单：固定的orbit/已认证buffer框架、要由人填入的角色与reference、三段组合、明确的检查条件，以及用户选择保留的工作区配置。

模板最多使用现有Prepare/Macro/Cleanup三类段。用户可以明确指定Cleanup取已选Prepare的逆；这是一个有限、可见的关系，不引入条件分支、循环、自动候选选择或通用脚本。宏引用固定到内容版本，库条目后来改变时先显示差异，不能静默改变保存模板的意义。

再次使用时保留重复结构，但每个实例有自己的Target、明确的角色绑定和完整review。上次成功、上次绿色状态或保存名称都不构成本次可执行证据。

一条固定宏也不会因为换了Target就自动成为适用于新位置的宏。只有已有且已验证的reference变换，在用户明确选择后才能实例化；否则显示不匹配，等待用户选择另一条已有宏或自己提供preparation。默认不去execution tree搜索“能解决这个目标的那条star”。

### 简单的文字概念图

```text
Macro base / My templates
        │ 用户选定一项
        ▼
[本次piece与固定目标] ─ [明确reference] ─ [A/B与保护条件]
        │       Local / Global在旁边解释这些关系
        ▼
[Prepare] → [Selected macro] → [Cleanup]
        │       需要的转动由用户用Grip/Twist输入
        ▼
[完整作用与保护检查] → 用户执行
        │
        └─ 保存/保留模板 → 用户选择下一次对象后再次填写
```

这是一张有固定位置的工作单，不是任意连线的节点编辑器。常用路径不要求管理图节点、编写规则或开多个配置窗口。

### 人工操作与软件辅助的界线

允许自动完成的是明确输入的追踪、合法宏展开、已选参考变换、作用分析、保护检查、保存和恢复。确定性计算也要保留来源。

由用户决定的是本次处理谁、采用哪个macro/模板、reference如何对应、怎样补prepare/cleanup、是否调整保护、何时执行。模板可以减少重复配置，不可以自动选择下一目标、根据状态分支选宏、搜索setup或批量连续复原。

“仍有一次确认”本身不足以证明人掌握复原过程；不能在后台生成整套答案以后只留一个Execute按钮。发表时应如实描述这是人主导、使用宏与分析工具辅助的复原，是否符合具体社区对human solve的定义，需要对应规则支持。

### 6.1 顶部先说明这是什么

每个 macro 的结构记录分两层：

- **固有效果**：从完整合法 witness 求得的位置/slot 置换、每个 orbit 的 cycle 和 frame 变化、净 support。与某次 scramble 的 occupant 无关。
- **当前实例效果**：在本次 state、目标、buffer、frame 和保护条件下，哪些身份被移动、哪些目标得到改善/损坏、是否可以提交。

保存了一条“安全 macro”不表示它永远适用于任何 state、prepare 或 protection。当前实例必须重新验证。

### 6.2 默认作用图

对用户选定的 active orbit，只显示相关位置 cycle。一个 star 的三个端点显示 `A position`、`B position`、`Target position`，每个位置内部标明当前 occupant。箭头说明身份沿哪个方向去，不能把 target 的标签移到别的节点。

在每条箭头旁可展开对应的 ordered frame 映射。对 orientation-only 作用，节点位置不变，但列出实际 sticker/frame permutation；不能因为位置不动就画成“no effect”。

其余 orbit 的影响收在紧邻的 support 表中：`Orbit / Pieces / Stickers / Net change / Protected`。一条 protected 冲突足以阻止提交。隐藏区域同样列入，不由当前 filter 决定。

### 6.3 star 的判定

先做完整作用分析，再分类为：

- `Certified star`：reference、目标位置与 frame 满足已有证书契约，有限 witness 完整。
- `3-cycle on selected orbit`：这个 orbit 上存在三循环，但其 frame 或其他 orbit 作用不满足对应 star 契约。
- `Other effect`：多个循环、orientation-only、恒等或其他合法作用。
- `Unverified`：未完成计算、超出预算或缺少映射，不能当作安全。

仅出现三个点的循环，不足以证明整个操作是“纯 star”。记录 directed cycle 方向和完整 collateral；`star_count`、primitive count、preparation cycle 数分别展示。

### 6.4 分段时间轴

Plan editor 只有三种段角色：`Prepare`、`Macro`、`Cleanup`。支持明确的有限 word、已保存 macro、合法 witnessed star 的引用，嵌套有限且有完整展开成本。

段上显示短名称、输入来源、primitive 数量与状态。用户选择 `Invert selected segment` 时，结果是对选中段的确定性逆操作，并记录这个编辑动作。不能偷偷将 `Cleanup` 当作一定等于 Prepare 的逆；其实际内容由用户选择。

选择某个边界，抽象视图显示该边界的 before/after 对照。标签始终标记 `Draft preview`，不会盖掉已提交状态的统计。逐步预演默认只移动预览游标，正式 commit 仍是一整个完整 operation。

### 6.5 临时移动与净破坏

常驻保护栏默认使用 `At operation end`。选中受保护 orbit，可查看该 orbit 是否被完整段固定。

中途是否移动是另一个证据字段：`Checked: moves temporarily`、`Checked: unchanged throughout` 或 `Not checked`。大 macro 不因最终返回就自动宣称全程没动。

0.4 默认使用 draft sandbox 供人工尝试 prepare，正式状态维持原有已提交边界。sandbox 使用同一机械实现与复制数组，不产生第二个权威 Session，也不写正式历史。必要的扩展若要逐段正式执行，必须有持久化游标、已验证返回路径和恢复协议；未实现这些就不开放“暂时解除保护继续”。

### 6.6 怎样在抽象视角里形成 macro

工作台不能只会展示已经完成的 macro。建议把用户形成操作的过程具体做成以下交互：

1. 用户选择 `Record into Prepare` 或 `Record into Macro`。这是录制范围命令，不是 Grip/Twist 编辑器的第三类动作。顶部持续标明 `Draft · Prepare · step 0`，用户随时知道自己正在试验哪一段。
2. 在 Local 或 Global 中检查一个 cell，明确选择 `Use for grip`。结构图显示真实 cell/frame、轴与键位；点邻接线不会生成转动，也不会替用户选择 grip。
3. 用户按自己的 Grip/Twist。每个接受的 twist 以合法 word 追加到所选段，复制状态按同一机械代码更新。Local 显示 piece 新的 Current，Global 更新区域标记，Macro effect 更新到当前草稿边界；三者使用同一个 draft revision。
4. 用户可以撤销草稿中的最后一个输入、选择已保存 macro 加入当前段、暂停录制或检查任意已算出的段边界。草稿 Undo 明确属于草稿，不把正式 journal 同时撤回。
5. Prepare 完成后，用户明确选择自己的 Macro 和 Cleanup。可对选定的完整段使用 `Invert selected segment`；界面显示实际生成内容与来源，不擅自补全后段。
6. 用户框选明确的操作段并选择 `Save as macro`，保存其名称、reference、真实有限操作与已分析作用。保存 macro 不执行它，不默认把未选中的 Prepare/Cleanup 一起收入宏体。
7. 准备执行时，审阅整段操作在真实当前状态上的效果，再进入既有 preview/commit。若正式状态、目标或保护变过，保留草稿并要求重新分析。

录制时，Main 与相关抽象视角同步标明 `Draft`。Committed progress 单列且保持正式值；预演中的绿色不能被计入已完成成果。退出录制时恢复查看正式状态、清除 held keys、保留草稿。任何时刻都不让一扇窗拿草稿的 piece 位置、另一扇窗拿正式状态的 buffer occupant，却伪装成同一幅分析。

这一流程可以在小窗/工作台内完成输入、观察和修订。用户需要决定的仍是具体 cell、twist、macro 与组合；软件负责让这些决定的结构含义和完整后果清楚可见。

## 7. 常驻 orbit protection 与 progress

保护状态位于工作区固定边缘，在 Local / Global 关闭、Macro 扩展或 index 切换后仍可见。

紧凑态示例：`Protection: 8 orbits + 1 block · At operation end · Reviewed / Stale / Conflict`。展开时才出现 35 个 orbit 的紧凑行，不默认铺出 35 张卡。

每行区分：当前 exact 完成、显式 protected、当前段会临时涉及（若已检查）、最终存在冲突。完成不会自动等于保护；`Confirm stage complete` 验证后由用户明确加入保护。

Progress 同时提供 active orbit、当前 block、整个 puzzle 三种范围。范围始终标出，去重计算 physical pieces。Home layer 的 piece 可能跨层，不能将各层显示数字相加当作全局进度。

进度默认统计 176,520 个 moving pieces；600 个 fixed centers 单列。色块是否看起来一致与 exact labels 是否完全复原是两个谓词，不混用。

## 8. Orbit-based macro base 与 block building 的具体体验

### 8.1 macro base 每一条都能读

列表默认按当前 orbit 和用户选择的用途过滤。每行展示：名称、一个小型 effect 图、相关 buffer/frame、净 support 和当前适配状态。完整原始 word 在详情展开，不占据整个首屏。

可按 `star / orientation / buffer cleanup / block extension / other` 分类。这些标签由已分析效果和用户说明共同形成；用户自定义名称不是证书。

点击一条 macro 先进入只读详情。`Use in plan` 才写入用户草稿，不直接执行。改变 reference 后重新求合法 word 和完整效果；没有已验证的变换就给出 `Reference transform unavailable`。

同一列表可切换`Macros / My templates`，不要再建一个独立的模板管理系统。模板行只增加“每次需填写的角色”和“保留的固定设置”；详情仍复用同一个三段工作单。常用模板可以pin，但不自动替用户选中并执行。

### 8.2 下一 piece 建议是如何产生的

用户先选择当前 block 与 active orbit，软件枚举尚未完成的候选位置，然后依据可验证的条件筛选：同 orbit、是否在 block 的 cell 邻接边界、能否沿用当前已明确 reference、已有宏的结构签名是否匹配、是否触及显式保护。

默认只给少量候选并说明理由，例如：

`Candidate: position … · shares a cell with the current block · compatible reference recorded for Macro M · protection not yet reviewed for a complete operation`

不能因为结构匹配就标为 `Ready to solve`。未输入完整 operation 时，保护只能标注已知约束或未知，不能提前给绿灯。

候选不附带新生成的 setup / insertion sequence。选择候选只固定新的 target；下一步仍然是用户自己构造或选用 macro。

### 8.3 block 的边界

0.4 的 block 记录同 orbit 的明确目标位置、期望身份/slot frame、用户选定的结构关系和完成条件。已完成 block 可以被作为 exact slot/label 约束保护。

“相邻目标组成的工作 block”与“整个可刚性搬运的复合 piece”是不同性质。仅当后者有相对姿态与合法搬运见证时才使用对应表述。不要在 UI 里发明没有机械含义的 block builder。

## 9. Grip/Twist keybind 工作台：待我审阅

### 9.1 两类动作

`Grip`：选择明确的 cell/cap 与已验证的 local frame，本身不转动 puzzle。

`Twist`：对当前 grip 执行明确的合法旋转。由 retained H/T words 展开；不以画面角度猜测。

目前 `grips.py` 已枚举 tetrahedral cap 的 11 个非恒等旋转，按逆对分为七条轴：3 条 order-2 的 H 轴，4 条 order-3 的 T 轴，每条 T 有正反两个方向。UI 应显示实际轴及参考顶点关系，不仅显示 H1/H2 等名字。

UI 的七轴名称必须绑定真实 rotation ID 与 inverse word。现有返回结构中，inverse 子对象的原始 label 不一定等于外层轴名；不能依赖字符串相同来配对正反向。显示名与机械 ID 分开，按实际置换验算。

### 9.2 推荐绑定方案

先显示一份默认配置供审阅：

| 键区 | 默认动作 | 条件 |
| --- | --- | --- |
| `1…0`、`Q…P` | 当前 profile 中第 1…20 个明确 cell grip | 每格有 C ID；未绑定不执行 |
| `A S D` | H1 / H2 / H3 | 有明确 active grip |
| `F G H J` | T1 / T2 / T3 / T4 | 有明确 active grip |
| `Shift + F/G/H/J` | 对应 inverse T | 同上 |
| `Shift + A/S/D` | 同一个 H（H 为自身的逆） | onscreen 标明等价 |

这只是初始候选键位，不是强制用户改掉已有习惯。最终要做真实键盘试用，检查左右手伸展、键盘 rollover、旧快捷键冲突，再由用户决定。

默认 `Hold grip`：按住 grip 键再按 twist。可选 `Latch grip` 是 Grip 的行为选项，用于不便同时按键或低 rollover 键盘；当前锁定 cell 必须常驻可见，一键解除。它不是第三类 action。

多个 grip 同时按下默认不猜优先级，暂停 twist 并指出冲突。相机、filter、hover 不改变 Grip。窗口失焦、对话框打开、IME 输入或模型重连时，清除暂时按住状态。

### 9.3 不能把 20 个格子做成隐藏的硬上限

20 个格子适合显示一个 piece 的 hosting cells；它不是可用 cap 的上限，更不是全模型只有 20 个操作区域。

工作台有三种明确来源：`Piece hosting cells`、`Caps affecting this piece`、`Pinned cells`。前两个从引擎读取并分别标记，第三个允许用户手动加入任意合法 C ID。

超过常用键区时，未直接绑定的 grip 仍可通过可搜索的 cell 选择与 onscreen 选择使用；不在用户按住键时翻页，不静默重用同一个键对应另一 cell。可保存不同 profile，由用户明确切换。完整 1,200 个 primitive generators 的可达性不能因为 piece-focus 被削弱。

### 9.4 profile 与当前目标的关系

`Target current`、`Target home`、`Buffer A`、`Buffer B` 是创建/更新 profile 的数据来源，不是不断重新解释按键的隐藏变量。

用户选择 `Build from current piece` 后，先看见实际 C ID 到键的映射，点击 `Use mapping` 才生效。当前 preparation cycle 中保持固定。piece 移动后，显示 `Piece moved · mapping remains pinned`，并提供显式 `Rebuild from current`。

这减少了每次重新抄 cell 的成本，同时保留肌肉记忆。若想让按键随 piece 自动走，必须作为另一个待审阅方案，不能当作默认便利功能悄悄加入。

Buffer A/B 的固定位置 profile 可以跨多个目标沿用。换 Target 后，当前工作条指出哪些 profile 仍固定、哪些来源已变；若使用新 piece 的映射，把新增、移除和改键的 cell 差异放在同一编辑区，不要求用户重新打开多个设置窗口。这个切换成本应进入连续 30 cycles 的试用记录；不能只验证首次建立 profile 很方便。

### 9.5 keybind editor 的精细内容

左列是绑定清单；中间是选中 cell 的 tetrahedral grip 图；下方是 onscreen keyboard。右侧详情只显示：Action type、Cell reference、Frame、Axis/direction、Capture key、Conflict。

按键捕获进入明确 `Recording key` 状态并暂停 puzzle twist。冲突展示冲突范围与原动作，取消保留原绑定。允许恢复单项、恢复默认和带版本的 profile 导入导出。

hover/tap 一个 onscreen twist，显示该 rotation 对四个顶点的置换与选中 piece 的作用；按下才进入对应的正式或 Draft 输入通道。onscreen 与物理键调用同一 dispatcher。

### 9.6 软件级 keyboard philosophy

菜单、index、filter composer、macro editor、Local/Global 对应表、protection、session、导入导出都可键盘到达。完整命令目录标明当前键位和启用条件。

Native 的新命令表统一按钮、菜单与 keyboard；按键事件按 `text editor → modal → focused tool → main puzzle` 路由，不让上层失焦后漏到主视图。

`Enter` 在文本框只编辑/提交字段，在 Local/Global 只选中对象，在 preview 明确拥有焦点时才可提交。`Esc` 先取消局部交互，再取消当前预览；退出 draft 不丢失草稿。

## 10. 几组可直接审阅的输入和预期显示

### E1：已有引擎验证的最小 star 工作样例

来源：本轮对 immutable full model 的只读计算。它是合成 fixture，不是人类复原证明。

初始 fixture 在 solved labels 上施加：

```json
[{"kind":"star","orbit":33,"node":0,"sign":1}]
```

工作台输入：

```json
{
  "orbit":33,
  "destination":35778,
  "segments":[{
    "phase":"macro",
    "recipe":[{"kind":"star","orbit":33,"node":0,"sign":-1}]
  }]
}
```

这里只展示目前实际 recipe 结构；它不是一个已实现的新 HTTP API。该 inverse 是用户明确输入的 fixture 内容，不是工作台自动生成答案。

预期关系：

| 角色 | 固定位置 | fixture 当前 occupant | 所在 public cell |
| --- | --- | --- | --- |
| Buffer A | 2712 | Piece 35778，即 required piece | C7 |
| Buffer B | 175618 | Piece 2712 | C594 |
| Target | 35778 | Piece 175618 | C113 |

Local 显示 required piece 从 Current position 2712 / C7 对照 Home position 35778 / C113。此 orbit orientation 为 trivial。Global 默认比较 C7 与 C113，A/B 的其他区域可以另行展开。

用户输入 macro 的位置作用应显示：`2712 → 35778 → 175618 → 2712`。净 support 为 O33 上 3 pieces / 3 stickers，展开 56 primitives。应用这条明确输入后，本轮只读计算确认所有 259,800 labels 恢复 solved。

作为对照，若用户输入 sign=+1，不得因为仍是同一条 star 就宣称目标完成。它的方向不同，要依据当前 occupant 检查结果。

### E2：保护冲突，即使三个 piece 全被 filter 隐藏

沿用 E1 的 fixture 和输入，将 O33 显式标为 protected，主视图 filter 隐藏它。

预期：图可保持 reference-only，保护栏显示 `Conflict · O33 · 3 pieces`；完整段不满足保护，commit 禁用。`Show conflict` 只检查对象，不自动 unlock，也不静默改 filter。

此处的冲突是未来 UI 验收场景，本轮只验证了真实净 support，没有操作未来 UI。

### E3：看起来只有两个颜色，不代表只有两个可用 grip

模型内 moving orbit 32 的目标 position 14056，hosting cells 为 C43、C59，cap count 为 18。Local 的主结构是两个 hosting cells；`Caps affecting this piece` 还必须能列出真实的 18 个作用 cap。

对比 moving orbit 0 的 position 6290，hosting 为 C19、C24，cap count 为 2。不能靠 sticker 数量推导可用 grip 数量。

这两类的 orientation group 都是 C2。示例界面应展示两个离散 frame 状态；没有输入具体当前状态和 macro 时，不伪造一个 orientation 修正答案。

### E4：20-cell vertex 的实际布局压力

moving orbit 34，目标 position 17810；真实 hosting cells：

`C55 C62 C66 C93 C98 C109 C125 C131 C143 C151 C180 C193 C196 C201 C216 C234 C236 C247 C255 C298`

其 orientation group 是 A5，order 60。Local 显示 vertex 局部结构、20 个稳定对应行和所选 frame；不要生成“转 18°”的滑块。Keybind 的 20 个 hosting grip 在同一明确 profile 中有稳定 cell 标签，不能只做到 2-cell 演示可用。

### E5：分析后目标或保护变化

用户输入并 review E1，在另一个工具里换 Target 或增加 protected orbit，然后返回 Macro 页面。

预期：保留原草稿，旧结果标记 `Stale`，明确变化原因。换回相同 target 或 undo 回相同 labels 也不能复用旧 token。重新检查完整上下文后才能创建新的 preview。

相反，单纯旋转 Global 相机或展开说明，不应无故废弃机械分析。

### E6：不是 star 的 macro 与不完整输入

用户输入一个完整合法 word，其 active orbit 作用不符合 A/B/Target 三循环契约。

预期：显示 `Other effect` 或 `3-cycle on selected orbit`，保留真实 full support，不把 macro 拒绝为“无用”，也不自动替换为软件生成的 star。用户可以保存其真实用途并继续编辑。

若只给 macro 名称而库中没有对应 witness，显示 `Macro not found`；若 reference 不明确，显示 `Choose reference`；若计算取消，显示 `Not reviewed`。这三种都不能落到同一个“安全”图标。

### E7：连续处理与尾段

人工完成一个 target 后，当前 grip profile、buffer、filter 和 block 保持不变。Next candidates 只呈现可解释的结构建议，不自动选择、不自动执行。

当目标变成固定 buffer，当前 preparation foundation 已知不支持这类 insertion review。UI 必须进入明确的 `Buffer cleanup` 工作阶段，支持用户提供的完整合法操作及效果检查；在该阶段契约尚未实现之前，显示具体不可用原因，并在交付说明中标为阻断完整复原目标的问题。

### E8：基于macro base复用工作单，而不生成解法

这组是拟实现的UI输入与预期，使用E1已验证的数据。模板名称和字段是设计示例，不是已存在的文件格式。

用户先明确把E1的inverse star保存为`M33-Example`，内容固定为`orbit=33, node=0, sign=-1`。再保存操作模板`O33 manual insertion`：

```text
Macro: M33-Example（固定到这个内容版本）
Buffers: retained O33 framework，A=2712，B=175618
Roles for this use: Target position=35778，Required piece=35778
Reference: retained node 0，明确显示实际slot/frame
Prepare: 用户明确留空
Cleanup: 用户明确留空
Checks: required piece当前在A；本次角色/frame与宏一致；全段保护通过
```

在E1初始fixture中，界面显示Required piece在A、方向一致、净support为3pieces/3stickers，用户review后决定是否执行。可以保存和重开这张工作单；不能将旧review一起恢复。

执行完后，若直接再次调用同一实例，原条件“required piece当前在A”已不成立，必须显示实际差异，不能按模板名再次报Ready。若将目标改成E3的14056，明确指出目标属于O32、模板属于O33；不自动挑一条O32宏补救。

换成另一个O33目标时，模板可以保留buffer、组合位置、filter和键位设置；固定的M33-Example仍须匹配实际作用。用户需要明确选择合适的已有宏/已认证reference，或者输入自己的preparation。软件不能只收到新Target就自动找出对应star。

此例让用户审阅“哪些重复操作真正省下了，哪些决定仍在自己手里”。它不证明该固定macro能处理所有O33目标，也不证明一条模板已经足以完成整个orbit。

## 11. 精细界面排版与视觉状态

### 11.1 1920×1080 主设计基准

以下尺寸为 100% 缩放下的设计基准，最终使用 logical units 与布局约束；不能把绝对像素写死到 150%/200% DPI。

- 顶部应用标题 32 高，主工具行 40 高。
- 左侧 index rail 48 宽，显示短文本/图标与键位提示，可展开名称。
- 工作区顶部固定 context line 56 高，跨 Main 与 Solve，始终显示 Target / Required piece / Buffers / Draft state。
- 下方 Main 约 59% 宽，Solve 约 41% 宽，中间可调 divider。主 canvas 贯穿大部分剩余高度，不加装饰外框。
- 底部 protection line 32 高、状态行 32 高。高度不足时状态合并，保护不能被滚动移出。
- Solve首先保留macro/template选择与本次角色工作单；Local / Global / Effect是同一区域的关系检查切换，可pin。下方保留三段plan与review/action区。切换视角不切换target、不丢draft，也不要求重新选宏。
- Local/Global 可拆为同风格的小窗。浮动窗默认避让主操作区域；小窗关闭停止绘制，重开恢复视角；应用重启也恢复有效布局。

### 11.2 1366×768 与小窗

1366×768 下 Main 保持可用，Solve 使用约 420–480 logical px 的侧区；Current/Home 改为上下对照，相关行留在同一滚动区。底部固定操作栏不能遮挡最后一条输入。

更窄时，Local/Global/Macro 使用 drawer 标签；用户可暂时最大化工作台再一键回 Main，不把所有窗口压成不可读的小格子。避免横向滚动整个应用。

小窗的最小尺寸由最小可用结构决定；20-cell 对应表可滚动，关键 Target/A/B/header 不滚出。125%/150%/200% DPI 都检查截断和 input focus。

### 11.3 视觉规范

- 使用系统无衬线 UI 字体，ID/word 采用单一等宽字体；正文建议 13–14 logical px，次要文字不低于 12，控件 28–32 高。
- 间距以 4/8/12/16 为主，短标题、紧凑工具栏、对齐的列，少容器；不做一排大数字 KPI。
- 背景中性，提供明暗主题；不同角色使用标签、形状与少量颜色。puzzle 原始 swatch 与界面状态色分开。
- Current 用实心定位标，Home 用空心目标标，A/B 使用写有 A/B 的菱形标；Protected 使用锁标与文本，Conflict 使用明确警示文本。颜色只是补充。
- 合法按键反馈包括 `Accepted / Busy / No grip / Conflict` 等短状态，出现在键盘与输入位置附近。
- 动效只解释被选 cell、对应 sticker、已提交变化或短操作反馈。无循环发光、无启动炫技动画、无人工生成的“AI analysing”表演。

### 11.4 至少制作的状态帧

| 帧 | 必须呈现的内容 |
| --- | --- |
| F01 | 新建 solved 会话；未选 target；原生式 Scramble 入口；没有虚构进度或庆祝 |
| F02 | E1 的 Current/Home、A/B、完整 macro effect 与 ready review |
| F03 | E2 隐藏冲突；reference-only；保护栏阻止提交 |
| F04 | E3 双 hosting cell 与额外 affecting caps 的不同列表；改键捕获 |
| F05 | E4 20-cell vertex；60-frame 意义正确；无标签拥挤 |
| F06 | 长 preparation 的 Draft + pending review + stale 状态与可恢复编辑 |
| F07 | Global 两组区域比较、拓扑模式和 selected correspondence |
| F08 | 同一工作区的 1366×768 / 150% DPI 重排；所有关键动作可达 |
| F09 | 真正 solve 事件后的 summary，以及同一 solved 状态经 reset 进入时不弹 summary |
| F10 | E8的模板首次填写、执行后再次调用、换orbit失败与保留重复设置；不自动生成新宏 |

## 12. 可直接交给专业设计环境的 prompt

```text
为 Windows 桌面几何交互工具 Magic 600 Cell 设计 0.4 的 piece-focused solve 工作台。
先阅读本文件的对象语义、E1–E8样例和F01–F10状态表，再建立设计文件。设计输出服务于反复进行人为buffer preparation、macro insertion和orbit protection的高级用户。

以macro base上的非转动操作为抽象工作台核心：选用、指定对象/reference、三段组合、检查与复用。重点呈现E8/F10“同一模板换一次使用”的流程与缺少人工输入的状态。Local/Global给这些操作提供结构依据，具体转动使用Grip/Twist。不要设计通用节点编程、自动寻找setup或批量执行。

先制作F10模板复用、F02正常实例、F03保护冲突、F05二十cell、F07区域关系，再扩展F01/F04/F06/F08/F09。以1920×1080、100% DPI为主，补1366×768与150%/200%重排。使用auto layout、可复用组件、可编辑文本/图形和真实状态variants；不要把一张raster screenshot当作最终交付。

布局：主 canvas 是视觉中心，约占工作区 59% 宽；Solve 约占 41%。顶部一条持续 context line；底部 orbit protection 常驻。index 可切 Local、Global、Macro、Filter、Keys、Progress、Session。Local/Global 可 dock 或成为 owned 小窗；小屏用 drawer。不要做 dashboard/KPI 卡片墙。

Local：同一个 required piece 的 Current/Home 局部结构对照、sticker-to-cell 对应、明确的 comparison frame、所选 cell/cap 的七轴 twist 与 onscreen key。分别处理 1、2、5、20-cell piece。20-cell 模式用真实 incidence 与可检索对应行，不铺满所有交叉连线。没有可信精细片块网格时以真实 cell structure 表示，不捏造片块形状。

Global：低细节真实 600-cell 投影；突出 Current hosting cells 与 Home hosting cells 两组；可切 Buffer A/B 对 Target。Topology 层只表示共享三角面的 cell adjacency，标记 Cell adjacency · not move sequence。Projection overlap 与真实相邻分开。选 cell 只是检查，不自动换 Target、相机、filter 或 grip。

Macro effect：对用户输入的完整 Prepare/Macro/Cleanup 展示 active-orbit directed cycles、buffer occupants、ordered-frame mapping、完整 collateral 和保护差异。固有效果与当前实例效果分开。Unknown/Stale/Conflict 不得出现绿色 Ready。不可生成目标的 setup 或插入答案。

F02 使用真实 E1 数据：O33；Target position 35778；Required piece 35778 currently at 2712/C7；A=2712，occupant 35778；B=175618/C594，occupant 2712；target occupant 175618，Home C113。用户输入 inverse star 后，位置作用为 2712→35778→175618→2712，56 primitives，net support 3 pieces/3 stickers，orientation trivial。只把这些作为合成 fixture 展示。

F05 使用 O34 position 17810 的 20-cell 列表与 A5 order 60。不要给它虚假的单角度 orientation 滑块。

piece keybind editor 仅有 Grip 和 Twist。两排明确 cell grip key 与七条 twist axis；每格列 C ID；Hold/Latch 状态可见；Capture key 时 puzzle 输入被暂停。原型中模拟 No grip、Busy、Conflict、Stale、Reference only 和 Lost focus，不能只展示理想状态。

采用克制的专业几何工具视觉：紧凑系统字体、ID/word 等宽、清楚层次、4/8/12/16 间距、少边框、小圆角或直角。无紫蓝渐变、玻璃拟态、大卡片、发光线框、口号、AI 徽章或无意义动效。强调色必须对应真实角色/状态，并配文字或形状。

建立可点按 prototype flow：选 Target→检查 Current/Home→选择 grip→输入用户 macro→review→显示冲突→修改明确输入→重新 review→commit→保持上下文→查看下一 piece 的结构候选。另做 Esc、undo、关闭小窗、重开、改 filter、键位冲突和 preview stale 的分支。

交付：设计文件/组件清单、上述状态帧、完整 keyboard focus order、交互转移表、minimum window/DPI 检查结果，以及至少三条根据连续复原过程发现并修正的问题。正式 UI 文案全部使用 English；设计讲解用中文。标明尚未验证的几何与后端行为。
```

