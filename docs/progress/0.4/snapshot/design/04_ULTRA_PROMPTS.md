> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

> **历史草案，已被替代（2026-09-14）**：本文件保留供追溯，不作为当前开发指令。请以 [新版完整架构 0.4-R2](03_ARCHITECTURE.md) 和用户最新消息为准。最新流程是先在隔离实验区完成 **G1 Grip/Twist 机制**与 **G2 抽象 hub 具体前端**的可操作版本，分别交给用户独立审核，两项都明确通过后再继续正式集成。旧布局、单审核门槛、旧 Prompt 和缺少 Next／每 orbit 多套键位的描述均已被替代。当前英文开发 Prompt 直接提供在对话中，不另输出文件。
# 给 Astra Ultra 的开发输入

本文件的 prompt 用于后续开发；本轮只整理文字，不启动实施或发布。搭配同目录架构、抽象视角规格和验收指南使用。

## 1. 怎样让 Ultra 的能力用在关键处

“专家架构师”的身份提示可以保留，但它不是能力开关。更有用的是把容易误解的语义、必须继承的边界、真实输入、预期结果与停止条件提前给清楚。

建议仍用用户选择的 Astra Ultra，先集中处理对象语义、参考 frame、保护边界、最终 buffer 和前端交互这些需要判断的部分；具体接线按已经确定的契约实施，不每到一个文件重新设计系统。本轮不会替用户更改模型设置。

同一阶段保留上下文，跨阶段用短交接记录。不要每次粘贴所有历史对话、完整仓库和全部日志。先读 README、架构契约及该阶段对应章节，按需定位函数。稳定背景放文件里，用户新 steering 作为明确变更单。

官方 Astra 指南也强调要具体说明自主执行边界和合适的验证范围，避免轻量改动触发过度测试。[OpenAI model guidance](https://developers.openai.com/api/docs/guides/latest-model)

### 建议预算，作为后续校准用的规划值

- 首次加载项目索引、硬约束和当前阶段材料，尽量控制在约 15k–25k tokens 的必要上下文；不要为了达标截掉关键规则。
- 每阶段常规进度说明控制在几百字；失败日志先定位后仅返回相关部分，完整证据保留文件。
- 在环境能读取真实使用计数时，以 **200k 新生成 tokens（含可计量 reasoning）**作为完整 0.4 首轮的粗略规划点，校准范围 **120k–300k**。这不是保证，也不包括每次重复输入、缓存输入、图片或工具的全部成本。
- 分配建议：基线与设计 15%，实现 45%，集成和真实使用检查 30%，包装与交接 10%。先用第一个垂直流程的实际消耗调整后续估算。
- 如果环境只提供账户限额百分比，不把它转换成 token 数；如果没有可观测 reasoning/usage，就明确写 unavailable。文本长度或代码行数不是实际消耗。
- 有成本上限需求时，用户应给具体额度与计量口径。接近预算时留下完整可恢复工作，不降低正确性、不跳过硬门槛、不假装完成。

降低试错的关键是一次明确歧义、一份证据只读一次、每个风险有对应检查，以及失败后先改假设再重跑。不是要求 Ultra 无限制“再想十遍”。不默认增加多个代理；如果后续用户要求并行，才按独立边界分工并计入总预算。

## 2. 总 prompt：可以直接输入

```text
你是我的专家架构师与 AI pair programmer。用顶级 senior engineer 的判断力接手 Magic 600 Cell 0.4。先弄清状态、空间和人为复原流程，再写代码；不从想象重建已有应用。认真推敲但把表达集中在结论、取舍、证据和需要我决定的事项，不输出冗长思维过程。

先读取本项目AGENTS.md、PROJECT_MEMORY.md、本设计包README.md与03_ARCHITECTURE.md，用01_PRODUCT_BRIEF_ZH.md核对完整用户要求。02_ABSTRACT_VIEW_SPEC_ZH.md和05_ACCEPTANCE_AND_TEST_GUIDE.md按当前阶段读取相关章节；设计审阅前完整掌握02，最终验收前完整掌握05，不在每个阶段重复加载全部文件。先确认实际源码目录与现有未提交修改，再读取对应函数、测试和docs/NEXT_UPDATE.md。不要覆盖其他任务的工作。

这次的主要求就是我的原始框架：0.4真正服务于人为完成完整600-cell；程序正式名Magic 600 Cell；1.0是能够发表的现代前端版本。新增功能以外向原生态HSC/MPUlt的正常使用流程对齐。复原的三项核心机制是piece-focused mode、keybind convenience、实用的抽象视角。抽象视角核心是基于macro base进行转动之外的操作，形成高度重复可用的人为复原操作。大型Solve工作台把它们组织起来，重点用于buffer analysis、insertion与orbit protection。

我研究复原理论的目的是弄清流程、人力成本和如何减少它。瓶颈是每次 macro 前的 buffer 准备和保护操作/核查。不要为 approachable 而削弱专家能力。也不要把工作方向变成自动求解器：人决定目标、方法、macro 和执行；软件追踪、分析、匹配结构与完整验算。

版本边界：本次目标是完成 0.4 实施和本地可测试包。1.0 写出明确迁移与发表门槛，不把本轮扩张为同时重写渲染器和完成 1.0。公开推送、Release、安装到我的个人会话等动作只有在当前任务另有授权时执行。

硬性基础完整继承：177,120 physical positions、259,800 sticker slots、35 moving orbits、600 fixed centers、全部 1,200 primitive generators。保留 immutable assets/model ID、合法生成元、seed words、execution trees、原 MPUlt binaries。所有操作保留有限合法 witness、完整 collateral、source-to-destination 和从左到右顺序。唯一 Model/PuzzleState/Session、共享锁、journal、EngineProcess。filter/相机/annotation/抽样绝不改机械状态。

一、先给我具体设计再接核心交互

按 02_ABSTRACT_VIEW_SPEC_ZH.md 给出精细的 Local、Global、Macro effect、Solve 与 Grip/Twist keybind 方案，优先做 E1/E2/E3/E4 的状态说明。解释每个区域为什么能减少一次 preparation cycle 的寻找、记忆、核查和切换。列出键位表、对象联动、before/after、隐藏/冲突/stale/失焦，以及 20-cell piece 的处理。

这部分先给我看，由我决定是否通过；未收到同意不能把提案当成正式交互规范。可以继续独立的源码核对、数据契约与只读验证。本次若我要求只输出文字，就不要制作草图、UI 或程序实现。

二、piece-focused mode

选定 Target position d 后固定它；Required identity d 的当前位置由 where[d] 得到；Target occupant 是 at[d]，不能互换。Buffer A/B 是已认证位置，occupant 会改变。Inspect、Track identity、Pin position、Use as target 是明确不同动作。所有工作台和小窗共用上下文，hover 不替换 Target。

保留精确 Shift-left/Shift-right inspection 与 Shift-left 4D drag，普通 3D/4D/Ctrl/multi-click twist 不退化。ID 查询与旧高亮能力可以合并入口，不能删掉稳定身份检索和 annotation。native hit 经确切 native_to_lab 映射，拒绝 stale 或 filtered hits；reference-only 不可 twist、不可 click-through。

三、以复原实用性为核心的抽象视角

抽象视角首先是基于macro base的操作台，核心动作限制为选用、指定对象/reference、三段组合、检查与复用。用户把自己选的macro和工作方式保存为操作模板，下次保留重复配置，但仍明确选择本次piece、参考与执行。Local/Global给这些非转动操作提供结构依据；不能把核心做成只有图表和状态说明的查看器。

模板复用现有Prepare/Macro/Cleanup，不做通用节点语言、条件分支、循环或自动批处理。每次形成新的PlanDraft，宏引用固定内容版本。固定macro不会因输入新Target自动重定向；已验证的reference变换必须由用户明确指定，未匹配时保留输入并指出差异。禁止借填模板的名义调用旧planner或按目标搜索tree中的解法。明确选择的已有star/witness仍可按原机械代码展开。

Main 是实际 puzzle 操作区。Local 以同一个 required piece 的 Current/Home cell structure、sticker 对应与参考 frame 为中心，显示与 keybind、filter 的关系和整体方位。覆盖 1/2/5/20-cell piece。hosting cells 与 affecting caps 分开，不能按颜色数量限制合法 grip。

Global 用真实 4D cell geometry 的低细节投影，重点显示两组区域：Current/Home、Buffer/Target 或明确选定集合。说明真实邻接、共有 cell、结构距离和选中对应。共享三角面的拓扑路线不是转动序列，不能转成自动 setup；投影重叠不是邻接。

Macro effect 基于完整输入操作，显示 active-orbit position cycles、ordered-frame maps、A/B/Target occupants、完整 collateral 和 protected conflicts。固有效果与当前 session 适用性分开；不能把任意三循环标成 certified star。

orientation 以各 orbit 的真实群与离散 frame 表示。20-cell vertex 是 A5/order60，不得画成假想单角度滑块。位置尚不正确时的 frame comparison，与现有 position-correct-only orientation_wrong 统计分开。

Local、Global、Macro、Filter、Keys、Progress 由同一个 index 快速切换/固定，可 dock、浮动或小屏 drawer。小窗独立相机、共享对象、可保存布局；关闭/最小化停止绘制。不要每选对象就移动主相机或改变复杂 filter。

四、keybind convenience

piece edit keybind 工作台只有 Grip 和 Twist 两类动作。Grip 选明确 cell/cap/frame，本身不 twist；Twist 执行该 grip 下的合法旋转。使用现有 grips.py 的七轴/十一非恒等旋转，不按投影视角推导机械转动。

图形化改键、真实键捕获、scope/conflict、单项恢复、导入导出、onscreen keyboard 都要完整。20-cell piece 可操作，所有 1,200 generators 仍可达。绑定映射在 preparation cycle 中固定；piece 移动或相机/filter 改变不能静默改键义。重建 mapping 先显示真实 C IDs，再明确应用。

软件级命令全部能键盘到达，但不塞进两类 piece action 编辑器。菜单、按钮、物理键和 onscreen 控件共用一个 dispatcher。认真处理 text/IME/modal/辅助视图/main scope、held grip、key repeat、丢失 key-up、busy 和冲突。Enter 在其他控件里绝不能提交主 preview。

五、piece filter 工作台

复用现有完整 Boolean grammar、canonical C1–C600 与 legacy syntax；延续 cell/color、layer/home_layer、identity/position 的区别。新增 role/block/macro support 等来源必须有明确 typed semantics。

预览表达式和 unique piece/sticker counts，提供 Replace/Intersect/Union/Subtract。Frozen identities、Frozen positions、Live query 分开。Local/Global 的 reference context layer 不修改 work filter；隐藏仍包含在全状态分析和 protection 中。无效语法、过期结果和循环查询拒绝且保留原状态。

六、Solve、macro base 和 block building

接通明确目标→buffer inspection→用户输入/录制/选取 macro→完整 Prepare/Macro/Cleanup review→Session.preview→用户 Commit。复用 preparation.py，但不把 review hash 当作执行 token。

输入包含 orbit、piece/目标位置、已选 macro、A/B 角色、reference frame、保护和可选前后段。展示 star/cycle 分类、位置/朝向对应、全 support、目标/buffer 的预测结果和具体冲突，默认不用原始 JSON 当 UI。

切忌自动输出如何复原该 piece。默认不调用旧 Planner.suggest、自动 setup 或 Next insertion；不自动选后续 target。对我选定的段做确定性 inversion/组合可以，但标明来源。保留高级检验能力，不用“人工模式”阻止明确合法的操作编辑。

借鉴 MPUlt 的参考 frame 宏复用、正反向与 insertion 便利性，建立 orbit-based macro base。每条有原 word/witness、结构作用图、frame、collateral、用途和证据状态；未知变换拒绝，不猜映射。HSC2 的占位 panel 不能算参考已实现。

在同一macro base内管理My templates，区分固定宏效果、用户保存的工作单、本次角色绑定和本次review。允许用户在模板中明确指定Cleanup是所选Prepare的逆；展开结果必须可见且有界。模板不能保留旧Ready/token，不能静默跟随库中后来改写的宏，也不能只留一个确认按钮、后台却自动选完整解法。

block building 从同 orbit 的明确 target/frame 集合和结构边界开始，显示真实完成条件。下一 piece 建议只按当前 block、已有用户 macro 结构与上下文复用理由提出；不附生成解法，不编 AI score/成功率。相邻工作集合不是自动获得刚性搬运性质的 block。

七、orbit protection 与尾段

protection 常驻 on screen，区分 complete、protected、net conflict、prefix unknown。检查全 prepare→macro→cleanup，包括隐藏区域。完成不自动保护，冲突不自动解锁。

草稿试验在标记清楚的 snapshot sandbox 内使用同一机械代码。默认整段正式提交。分段正式执行如果没有持久化 cursor、恢复点和完整返回/继续契约，不开放。

覆盖最终 A/B buffer、orientation residue、当前方法无可用候选与阶段切换。现有 preparation 对固定 buffer destination 的拒绝不能被隐藏。以用户提供的有限合法操作进行分析和执行；缺乏收尾契约时标记阻断，不能宣布软件已经支持完整复原。

八、原生流程与持续前端优化

新建 solved/reset 状态；已有工作明确 Resume，绝不覆盖长期存档。Scramble 界面、短/自定义/full 语义、Reset puzzle/view/workspace、真正复原后的 summary 分别核实。Full scramble 不是均匀随机状态的证明。

summary 区分 face-color/exact-label、人工输入/宏辅助/导入replay、scramble counts 与 solve counts，保留准确 timer 语义。reset/import/replay 不误报新的人类复原。不要引入未要求的在线上传/排行榜。

几何是视觉中心，工具紧凑、精确、可重复使用，有极客感但减少 AI 模板感。不要 dashboard 卡片墙、紫蓝渐变、玻璃、大圆角、发光线框、口号和无意义动效。文字/颜色/图形必须服务对象关系或输入反馈。持续检查真实尺寸、DPI 和长列表，完成可用流程后再根据真实画面迭代；不能只改一轮颜色。

九、架构与性能

复用既有核心、单一 API/worker/shared lock/journal。允许必要的架构变化，但禁止第二状态引擎、未经证明的兼容层、通用插件框架和无关重构。现代 renderer 候选在独立 worktree/prototype 验证，0.4 不依赖未验证迁移。

保留 atomic revisioned updates、小 delta 与验证后的 full fallback，颜色/可见/交互/annotation 各自 revision。一次 commit 后返回一致状态，禁止以预览动画冒充提交。

延续 instant-turn p95 ≤100ms、结构交互 p95 <50ms、Intel HD620 adaptive 1080p/30FPS 目标；全细节30FPS另行测量。记录输入→计算→持久化→同步→正确帧。达不到就给实测与限制，不减弱 journal，不编 GPU 下限。

十、验证、调整、测试版交付

先执行改动边界对应的必要测试：机械/持久化变更跑 test_core、test_reference_maps、test_crash；进程变更跑 lifecycle；native 修改先编译 fixture，再做真实 Windows/DirectX 检查。使用新隔离 session，绝不对个人进度做破坏试验。

按验收指南从刁钻用户角度检查完整流程，尤其新功能。覆盖快按键、隐藏目标、误改 frame、同 orbit 冲突、连续多次 insertion、长宏取消、busy切视图、undo/reopen、最后buffer、20-cell piece。写具体问题→修改→针对性复测，直到硬失败为零。合理限定场景，避免重复全仓测试和无目标 benchmark。

最后输出真正能方便测试的 Windows 版本、清晰启动入口、隔离样例、版本与 SHA256、English 测试指南、从0.3迁移/回退说明、详细改进、实际测试结果、未达目标、limitations/roadmap。保持 Andrey Astrelin 的主作者归属及原有有限致谢，不发 private logs、绝对路径、conversation、个人数据库或 Microsoft DirectX DLL。

发表说明比较上一版的真实改进，并区分 synthetic fixtures、真实原生运行、人类使用和完整复原证据。新功能没实现、没测试、没跑过完整收尾就不能宣称完成。

执行方式：先建立简短阶段计划与验收点。已经批准的普通可逆实现自主完成；只有我明确要求审阅的 keybind/交互设计与额外发布授权需要等待。不要不停提问，也不要越过这些明确门槛。

每阶段结束维护 HANDOFF.md：已批准决定、当前代码/数据版本、已实现/未实现、真实检查、未解决问题、下一步。失败先定位一个具体假设再修改，不反复盲跑。没有新变更或新风险就不重复检查。预算不足时诚实交接，不偷减范围或假装达到最终目标。
```

## 3. 分阶段追加 prompt

总 prompt 固定后，每次只补当前阶段；阶段通过才进入下一阶段，不重新粘贴全部历史。

### P0：基线、参考与设计评审

```text
只执行 P0。核对源码 HEAD、dirty diff、immutable assets/model ID、0.3 已有能力和 NEXT_UPDATE 的隔离 preparation 基础。以本设计包参考记录为起点，只更新发生变化的上游材料。

制作 inherited requirements checklist，指出当前设计对旧自动 suggestion、新的只分析模式、冗余 inspection 合并、初始reset/Resume 和保护边界的明确处理。把尚未验证的参考软件行为标为待核对。

输出Local/Global/Macro/Solve/Grip-Twist的精细文字设计，包含真实E1、双cell/多cap、20-cell与E8模板复用案例，给出正常、隐藏、冲突、stale与模板不匹配状态。重点解释macro base上的非转动操作如何少设置、可反复用，同时保留人工选择。说明固定key mapping与自动跟随、整段draft commit与分段正式执行的取舍。给出推荐与理由。

到设计审阅门槛停止依赖交互的实现。向我提交具体设计，不要问泛泛的“是否继续”。不要实现0.4 UI，不改我的运行会话。
```

### P1：批准后的对象与只读联动

```text
仅在我已批准相关设计后执行 P1。先做一个完整但小的垂直流程：选固定target→追踪required identity→A/B状态→Local Current/Home→Global两组关系→filter reference状态。复用 preparation.py 和现有结构数据。

验收 identity/position区分、hosting/affecting caps、20-cell、pin/follow、晚响应、相机不失效机械review、隐藏不可twist、纯hover零DirectX请求。页面切换不丢target，不产生新的Session。更新真实证据和HANDOFF后结束本阶段。
```

### P2：Grip/Twist 与全键盘闭环

```text
执行批准的 keybind 方案，不重新创造额外 action类型。完成编辑、捕获、冲突、profile持久化、onscreen keyboard、四种piece规模、明确frame和固定mapping。接到统一dispatcher。

用真实窗口焦点验证 text/IME/Modal/Local/Global/Main、held key失焦、repeat、busy、布局变化和旧gesture。完成仅用keyboard的选择目标→选择grip→明确twist/draft→查看影响→undo/checkpoint。记录实际操作成本，不以测试入口调用成功代替keyboard可用。
```

### P3：用户 macro、完整 review 与 protection

```text
完成用户word/录制/选库macro→明确reference与角色→三段组合/操作模板→全作用分类→完整review→既有preview/commit。保存orbit-based macro base与简单的人为模板，固有效果、模板定义和本次适配分离。保护常驻、隐藏collateral照常检查。

重点用同一模板连续指定不同实例，检查固定设置复用、缺少人工输入、错orbit、错reference、宏版本变化、再次调用与过期结果。明确选择现有宏才能加入plan，不根据target搜索或自动替换输入。不把交互扩张为通用流程编程。

先通过E1全label样例，再做wrong direction、other effect、orientation-only、protected conflict、已有pending保留、取消/重连/undo后的stale。默认流程不能自动生成target解法。大macro前缀未验证时正确显示unknown。
```

### P4：block、尾段与持续会话

```text
完成同orbit的明确block成员/完成条件/保护，以及只有结构理由的下一piece建议。保持buffer/grip/filter/frame上下文，跨重开恢复，但不恢复旧执行token。

建立35个orbit的尾段覆盖表，检查固定A/B、orientation residual、阶段切换和无候选情况。能由用户提供操作检验/执行的路径写清楚，理论与操作契约缺口明确列为阻断。不要用自动solver补洞，也不要把一条reverse scramble当做人类可复原性的证据。
```

### P5：基础流程、视觉质量与性能

```text
对照指定版本核查 startup/New/Resume、Scramble、Reset puzzle/view/workspace、completion summary。完成品牌一致与兼容迁移。合并冗余入口，保留原功能语义。

持续检查真实运行布局、辅助小窗、DPI、长列表、20-cell信息密度和长时间重复操作。按关键延迟拆分优化，复用revision/delta/cache；在隔离试验验证收益后才引入变化。先报告影响准备周期的具体问题，再自主修正，别做纯装饰一轮即停。
```

### P6：刁钻用户审查与本地测试版

```text
执行05_ACCEPTANCE_AND_TEST_GUIDE中的高风险场景、必要回归和一轮完整用户流程审查。对发现的问题写影响、修复、复测证据。没有真实运行条件的检查标blocked/unverified，不能说passed。

输出独立测试包、隔离样例、English测试指南、hash、启动/恢复/回退、0.3对照、已知限制。用准确的结果区分0.4实现完成、可测试prerelease和仍被尾段/正确性阻断。除非当前任务另有授权，不公开推送Release。
```

### 发生失败或中断时的续接 prompt

```text
读取HANDOFF和最近失败记录，只接着未完成阶段做。先核对当前HEAD、dirty diff、状态与上次证据适用范围，不重新浏览全部仓库/聊天。为当前失败写一个可验证假设，做最小定位；相同原因连续失败两次后重新审视假设，不第三次照抄运行。保留所有通过的有效证据，修复后只复测相关边界与必需依赖。普通问题自主解决；确需我决定时给出具体结果和影响。
```

