> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

> **历史草案，已被替代（2026-09-14）**：本文件保留供追溯，不作为当前开发指令。请以 [新版完整架构 0.4-R2](03_ARCHITECTURE.md) 和用户最新消息为准。最新流程是先在隔离实验区完成 **G1 Grip/Twist 机制**与 **G2 抽象 hub 具体前端**的可操作版本，分别交给用户独立审核，两项都明确通过后再继续正式集成。旧布局、单审核门槛、旧 Prompt 和缺少 Next／每 orbit 多套键位的描述均已被替代。当前英文开发 Prompt 直接提供在对话中，不另输出文件。
# Magic 600 Cell 0.4：完整想法整理

以下是基于我原本想法整理后的开发要求。新增设计建议已经具体化，但 keybind 和抽象视角仍要先给我看，由我决定是否通过。

## 1. 这次更新到底要解决什么

这次复原的三项核心机制就是 **piece-focused、keybind、抽象视角**。Solve工作台把它们组织起来；buffer、insertion、orbit protection是它们最重要的应用场景。抽象视角的核心是基于macro base进行转动之外的操作，形成可以高度重复使用的复原操作；不要过度复杂，也不要自动化到人失去实际判断和操作。所有功能、架构与验收都围绕这三个机制展开。

程序正式改名为 **Magic 600 Cell**。0.4 要做成一个真正能给人长期用来复原的软件，让最终完成复原的可能性提高到不能忽略的程度；1.0 要成为能够发表、能够把实际改进说清楚并拿出证据的版本。

我的重点不是做一个更好看的展示器，也不是把复原交给自动 solver。人仍然决定目标、方法、macro 和执行。软件应当承担人在 177,120 个 piece、35 个 moving orbit 的规模下不适合反复完成的追踪、检索、验算和保护核查。

复原里最耗费人力的往往是 macro 前的 buffer 准备和保护确认。每处理一个 piece 都重新找位置、辨认几个 cell、重设按键、检查已完成 orbit，会使一个数学上可行的过程在现实中无法坚持下去。0.4 的所有功能要围绕减少这类重复劳动设计。

“完成概率提高”目前是目标，不是已有统计结论。开发应给出实际 preparation cycle 的耗时、误操作、上下文切换和中断恢复情况，并检查所有阶段及最后 buffer 能否收尾，不能用几次成功演示代替完整复原的可行性。

## 2. 基础行为与原生软件对齐

除了新增功能，剩余操作向原生态 HSC / MPUlt 对齐。对齐的是熟悉、连贯的 hypercubing 操作逻辑，不是机械复制它们的旧限制。

- 新建会话默认是完整 solved puzzle，不自动打乱，不自动进入演示，不把上次测试状态当作初始状态。
- 对已保存的长期复原提供 Resume；New solve 与 Resume 清楚分开。启动时的 reset 视觉状态不能成为覆盖旧进度的理由。
- Scramble 放在熟悉且可键盘访问的位置，提供常用短 scramble、指定长度、完整 scramble。展示类型、长度及可复现信息；不同软件的“Full”要核实定义，不能默认代表均匀随机状态。
- Reset puzzle、Reset view、Reset workspace 是不同动作。Puzzle reset 保留恢复点，view reset 不改 puzzle，workspace reset 不改历史或 keybind。
- 复原成功后有认真设计的 summary。区分完整 scramble、练习、导入状态和 replay；区分 face-color solved 与 exact-label solved。不能只因 reset 或导入 solved 文件就播放一次“复原成功”。
- 上次已有的 3D / 4D 拖动、Ctrl 手势、多击 twist、精确拾取、undo/redo、checkpoint、日志导入导出、计时和故障恢复都要保留。

参考对象必须写出版本和 commit。HSC 稳定版与 HSC2 开发版分别核对；HSC2 主分支的占位面板不能被当作已实现方案。MPUlt 的源码参考与本机 `MPUlt_155.exe` 也不能未经比对就宣称完全一致。

## 3. 全面变成 piece-focused mode

piece-focused mode 是全软件的组织中心，不是多加一个 inspection 页面。

我选定一个要完成的目标位置后，软件持续记住：这个位置需要谁、那个 piece 现在在哪里、当前位置是什么结构、Home 是什么结构、A/B buffer 中现在是谁、我在做哪一段 preparation、哪些成果必须保留。

必须分清四个角色：

1. 我当前查看的 piece 身份；
2. 这个 piece 当前占据的位置；
3. 我本次要复原的固定目标位置；
4. buffer 的固定位置以及它们各自的当前占用者。

“跟随 piece”与“固定位置观察”都要有，但切换需要明确动作。转动以后身份跟着走，目标位置保持不变。不要每次选别的 cell 就把正在处理的目标换掉。

所有信息共用这一个工作上下文。Solve、Macro、Filter、Keybind、Progress、Local、Global 之间切换时，不再重复输入 piece ID 或 buffer。界面上的 ID 都能通过一致的方式定位、查看和插入条件；不要求普通工作流一直抄数字。

## 4. Keybind convenience 是绝对核心

目标是显著减少 buffer 与 insertion 阶段的输入和视图切换时间。以 HSC 的 keyboard philosophy 做到所有有效功能能通过键盘到达。

这里有两个层次，必须保持简单：

- **piece 操作的 edit keybind 工作台只有 Grip 与 Twist 两种动作。** Grip 选择明确的 cell/cap 与 frame；Twist 在该 grip 中执行一个明确的合法旋转。这里不做宏语言、条件脚本或“自动找解”按钮。
- 软件级命令仍能通过菜单、命令索引和已有快捷键系统访问，包括切换面板、选择目标、打开 filter、预览、撤销和保存。它们不塞进 piece 的 Grip/Twist 编辑器。

工作台显示当前 piece 的 hosting cells，优先为这些 cell 组织 grip；同时区分真正能影响这个 piece 的 cap。不能错误地认为颜色所属 cell 就是全部合法作用区域。

绑定表要能看见“这个键现在指向哪个 C ID、哪个 frame、哪种 twist”。在屏幕转动、filter 改变和 piece 位移后，不得悄悄改变按键含义。需要重新以当前 piece 建立映射时，展示变化并由用户明确应用。

支持 1 / 2 / 5 / 20-cell piece。普通 piece 要少操作，20-cell piece 也不能迫使用户在层层分页里找键。onscreen keyboard 显示真实当前映射与按下状态，并能操作；它不是仅供装饰的键盘图。

必须处理文本输入、IME、按键重复、按住 grip 后切换窗口、松键丢失、busy 状态和快捷键冲突。不会因为在 macro 文本框输入一个字母就 twist，也不会因为在 Global 里按 Enter 就提交主界面的 preview。

**这部分先给我看完整设计、具体绑定表和操作样例，再实施正式交互。**

## 5. 抽象视角：围绕复原过程，而不是围绕展示结构

这次最重要的是抽象视角的具体想法。不要在已有 global/local 小窗上简单加更多颜色或更多标签。

抽象视角首先是基于macro base的操作台。我在这里选宏、指定piece与reference、安排Prepare/Macro/Cleanup、检查适配与保护，再把可重复部分保存为操作模板。它需要减少我下一个piece的重复设置，而不仅是把当前状态解释得更漂亮。

Local和Global为这些非转动操作提供结构依据；keybind承担需要我实际输入的grip与twist。模板每次保留相同的步骤和约束，目标、reference与执行仍由我明确决定。不要做节点流程编程、自动搜索setup、自动选择宏组合或批量复原；缺少人工输入时明确停在那个位置。

### Local 应当让我在原地完成判断

Local 默认以正在处理的 piece 为中心，把这个 piece 所涉及的几个 cell 以及它在这些 cell 中的局部结构讲清楚。

- 当前 piece 及其实际所属 cell，和目标/Home 结构并排对照。
- 每个 sticker 的身份、当前落点与目标 frame 的对应关系。
- 当前 grip 的 cell、合法 twist 方向、onscreen key，以及执行时会影响的局部结构。
- piece filter 隐藏了什么、哪些只是参考显示、哪些可以操作。
- 当前 piece 在 global puzzle 的大致方位，目标在什么区域；需要时一键定位，平时不强制移动主相机。

这里的“抽象”是把重复细节折叠，只保留判断所需结构；不是把实际 piece 变成一张脱离几何的状态卡。

### Global 应当说明两组 cell 为什么有关

Global 保留整个 600-cell 的低细节空间参照，以当前工作区域与目标区域两组 cell 为重点；可切换对比 A/B buffer 区域与目标区域。

显示实际 4D 几何投影和清楚标注的拓扑关系，包括共有 cell、面邻接、结构距离和对应点。图中的关系路线只是找结构的辅助，不能暗示它就是 piece 的可执行转动路径。

两组区域的 ID、filter、Local 结构和 macro 影响必须联动。图上选一个 cell，只更新检查焦点；改变目标、将它加入 filter、改 grip、定位主相机是分别明确的动作。

### Macro 视角应当形成并复用一段操作

用户从macro base选择、输入或录制macro，在抽象视角里指定它本次的对象与参考，组合并检查。除了看active orbit的cycle、buffer、目标frame和其他orbit影响，还要能保存这套明确的组合方式，在下一次由我选定的新piece上再次使用。

主视图告诉我“在哪里操作”，Local 告诉我“piece 与 frame 是否对上”，Global 告诉我“工作区域彼此什么关系”，Macro effect 告诉我“这一整段实际上做了什么”。四者分工明确，切换时共享对象和状态。

orbit protection 持续 on screen，即使关闭这些小窗也不能消失。macro base 必须可视化，默认按照 orbit 和结构用途查找，而不是一堆名字相似的文本。

## 6. Solve 变成大型但连贯的工作台

Buffer analysis、Insertion、Orbit protection 是本次最重要的更新。它们应当组成一个完整工作区，而不是互相不认识的三个弹窗。

一次工作流程是：

**选择目标 → 查看 piece 和 buffer → 自己输入、录制或选择 macro → 检查 star/frame/orientation 与完整影响 → 修改自己的 preparation → 预览完整操作 → 明确执行 → 检查成果与后续工作。**

输入至少包括：orbit、piece/目标位置、已选 macro、明确的 A/B 角色、参考 frame、保护范围，以及可选的 prepare / cleanup。

软件负责判定：

- 操作是否是所选 orbit 上的 star 或其他 cycle；
- 具体是哪一方向的 cycle，所有 sticker frame 如何变化；
- 所需 piece、buffer occupants、朝向条件是否吻合；
- 整段操作是否产生隐藏 collateral 或破坏保护；
- 哪些判断已验证，哪些尚不能确定。

**切忌自动输出如何复原该 piece。** 默认不调用旧的 suggest/next insertion 自动解法，不自动生成 setup 路径，不自动跳到下一个目标。允许软件自动追踪、分析输入、检验用户选择的操作，并建议结构上适合继续处理的 piece；建议不能附带自动生成的解法。

用户自己选择反向 macro、组合已选段、生成明确段的逆操作，属于操作编辑能力。自动计算的等价变换必须写明来源和范围，不以“手工复原”掩盖软件辅助。

## 7. Orbit-based macro base 与 block building

macro base 不只是录制列表。每条 macro 保存原始合法 witness、参考结构、作用 orbit、cycle 与 frame、净影响、临时影响的验证状态、适用条件和来源。

macro与操作模板分清：macro是确定的合法操作及其作用；模板是我如何调用一个或几个已有macro、哪些角色要在每次使用时由我填写、采用什么保护与检查条件。0.4只需固定的三段编辑和明确角色，不引入通用自动化脚本。每次实例重新分析，保存模板不保存过期的安全结论。

向 MPUlt 借鉴参考 frame 下的宏复用、正反向调用和 insertion 便利性；把这些隐含关系变成可检查的结构。不要把相机朝向当作宏的机械参考 frame，也不要照抄不报错的文件加载行为。

在一个 orbit 中，可以把人为确认的目标集合组织成 block，并显示它相对于当前 buffer 和既有 macro 的接口。建议下一个 piece 时优先：

- 可以延续当前人工工作上下文；
- 位于当前 block 的结构边界；
- 有用户已有 macro 可以匹配的作用结构；
- 不需要改变已确认的保护范围。

这些都是可以解释的条件。先逐项显示，不编造“AI 最优分数”、完成概率或剩余秒数。

block 必须记录真实成员和可验证的完成条件。0.4 的基本 block 是同 orbit 中若干明确目标位置及其 Home/frame 约束；不能把任意绿色区域直接称为可刚性搬动的 block。后者需要额外的等变或相对姿态证明。

工作台还必须覆盖尾段：最后 buffer 的处理、orientation residue、当前方法无法继续、切换人工方法和回到已保存的上下文。没有这些，就不能把中段 insertion 的演示叫作完整复原工具。

## 8. 前端的持续大幅优化

希望有很强的极客感，但这个体验来自精确、稳定、响应清晰和可反复使用的工具，而不是深色、发光、随机颜色和许多信息卡。

- 600-cell 几何仍是视觉中心。Solve 工作区可以展开，但要保留足够的主要操作空间。
- 统一紧凑的控件、字体、行高、快捷键标记、图例和状态表达；public UI 和文档继续用 English。
- index 是所有功能的统一入口，可键盘搜索、切换或固定 Local / Global / Macro / Filter / Keys / Progress；小窗可以成为工作区内的 dock，不强迫用户管理一堆独立窗。
- 默认只显示当前准备周期所需信息。完整数据随时可达，不为了“简洁”删掉专家能力。
- 旧 highlight、ID-based inspection 的重复入口可合并到 piece focus；ID 检索、精确查找和 annotation 能力继续保留。
- 多次执行后面板位置、选择、filter、buffer 和 frame 保持可预期。主动显示变化与冲突，避免让用户再次检查一切。
- 错误要出现在导致错误的位置，保留草稿和焦点；常规成功用短反馈。复杂操作的审阅应是紧凑差异，不是每一步都弹通用确认框。

改架构之前，先说明布局变化怎样降低真实操作成本，给我可审阅的 Local / Global / Solve / Keybind 具体状态。不要只描述风格词。

## 9. 隔离、验收、交付与发表

实验架构、benchmark instrumentation、失败候选和测试数据继续隔离。不要为了架构整洁重写已经可信的机械状态、journal 或 MPUlt 模型。

开发必须持续优化并验证。benchmark 通过之后，还要以一个会连续复原、快速按键、临时换目标、隐藏重要 piece、反复 undo、做错 preparation、关闭重开、操作最后 buffer 的刁钻用户来审查。

审查后要列出具体问题、改进和复测结果，再决定是否达到交付标准。不能因为单元测试全绿，就宣称用户可以用它完成复原。

最后交付方便测试的 Windows 版本、清晰启动入口、隔离示例会话、版本与 hash、测试指南、变更说明、已知问题和回到 0.3 的办法。发表时比较 0.3 与新版本的真实变化，明确上游贡献、辅助方式、性能与人力测量边界；不将目标写成已实现成果。

## 10. 对原始文本的逐条覆盖

| 原始要求 | 本方案落点 |
| --- | --- |
| Astra ultra 充分发挥、token 合理 | 分阶段 prompt、按需上下文、复用证据、明确失败停止条件 |
| 0.4 可实际复原、1.0 可发表、正式改名、比较上一版 | 产品目标、尾段覆盖、迁移门槛、发布与证据要求 |
| 全项目完整架构，继承上次硬要求 | 架构契约＋继承表＋高标准验收 |
| 原生 scramble、初始 reset、复原弹窗，仔细检查 | 基础行为表及单独验收，不宣称本轮已操作验证 |
| piece-focus、buffer 的具体 piece keybind 是绝对核心 | 持续目标上下文＋Grip/Twist editor |
| 全 keyboard、index 小窗、onscreen keyboard | 统一命令入口、面板生命周期、输入上下文 |
| MPUlt macro 与 insertion 便利性 | 参考 frame、正反向、完整段编辑与作用图 |
| HSC 复合 piece filter、更复杂机制 | 明确 identity/position 集合、组合、live query、context layer |
| 只有 Grip 和 Twist 的 edit keybind，先给我看 | D2 设计审阅门槛及 1/2/5/20-cell 样例 |
| Local 讲 piece 的 cell 结构、按键、filter 与全局目标 | Local 精细规格与双定位缩略图 |
| 最新steering：抽象视角基于macro base做非转动操作，高度重复但保持人工主导 | 简单操作模板、明确角色/reference、四步操作、E8复用与错配样例 |
| Global 两组 cell 方位、联系、filter、ID 联动 | 全局双区域与分类型关系边 |
| Macro 视角整合 progress、protection、buffer、insertion | 单一 Solve 上下文与完整操作 effect sheet |
| Protection 常驻，macro base 可视化 | 常驻保护栏与按 orbit 检索的宏作用图列表 |
| 大型 Solve 工作台，判定 star/frame/orientation | 数据契约、输入分析、角色与失效状态 |
| 不自动输出该 piece 解法 | 默认产品边界与自动化越界验收 |
| Orbit-based block building，下一个 piece 建议 | 已有人为 macro 的匹配与结构边界建议 |
| 必须有具体输入样例给我检查 | 真实 O33 fixture＋O00/O32/O34 结构样例＋错误情形 |
| 冗余高亮、ID inspection 可优化 | 合并入口、保留能力与兼容动作 |
| 持续大幅优化、极客感、先详细沟通 | 精细文字设计、状态帧 prompt、交互审阅 |
| 实验隔离 | 源码/原型/测试数据边界、有限迁移 |
| 刁钻真实使用审查、建议后重调、高标准 | 场景验收、缺陷修正与复测闭环 |
| 方便测试的最终版本与测试指南 | 后续实施阶段的必交付清单 |

