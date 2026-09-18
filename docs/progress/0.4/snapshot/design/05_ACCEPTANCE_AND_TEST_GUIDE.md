> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

> **历史草案，已被替代（2026-09-14）**：本文件保留供追溯，不作为当前开发指令。请以 [新版完整架构 0.4-R2](03_ARCHITECTURE.md) 和用户最新消息为准。最新流程是先在隔离实验区完成 **G1 Grip/Twist 机制**与 **G2 抽象 hub 具体前端**的可操作版本，分别交给用户独立审核，两项都明确通过后再继续正式集成。旧布局、单审核门槛、旧 Prompt 和缺少 Next／每 orbit 多套键位的描述均已被替代。当前英文开发 Prompt 直接提供在对话中，不另输出文件。
# 0.4 验收标准与未来测试版指南

这是后续开发的验收计划，**不是本轮已经完成的测试报告**。本轮只读模型样例的验证范围见参考审计。

## 1. 怎样判断它值得叫 0.4

验收对象首先是piece-focused、keybind、抽象视角三项机制能否协同工作，再通过buffer/insertion/protection的完整流程验证。抽象视角必须能基于macro base进行简单的非转动操作、保存并反复使用人为操作模板；只有状态图与分析结果不算完成这项机制。

必须同时满足三层要求：

1. **计算可信**：完整状态、合法操作、frame、保护、预览提交、日志恢复正确，隐藏区域不会漏掉。
2. **操作连贯**：能够在同一上下文中持续处理 piece，keybind/Local/Global/Macro/Filter 相互配合，而不是靠反复复制 ID 才能工作。
3. **可以继续到最后**：不仅有中间目标 insertion，还能解释并处理各类最终 buffer、orientation residue、没有可用候选和跨阶段切换。

不接受以下替代证据：一张精致 UI 图、一次 inverse scramble、一段合成 replay、自动 solver 完成、单元测试全绿或某个孤立的 benchmark 数字。

## 2. 硬失败：任意一项存在就不能宣布完成

- 发生未授权 twist、错误的 cell/frame 解析、重复 commit、丢失进度、损坏日志或不可恢复状态。
- 把一个 piece 的身份和位置混淆；转动后 target 被偷换。
- 对隐藏 protected pieces 漏检；用户未解锁却执行净破坏。
- stale review/token 能继续执行；失败覆盖另一条 pending preview。
- 文本/IME/辅助视图中的按键触发主 puzzle 操作。
- 只在小 piece 案例可用，20-cell piece 无法完成同一流程。
- Default piece-focused 模式自动输出 target 解法或自动选择并执行下一目标。
- 模板收到新Target后擅自搜索适用宏/解法，静默更换引用，或者恢复上次的Ready/执行权限。
- 用截图/合成 fixture 声称真实 Windows/DirectX 检查通过。
- 最终 buffer/朝向有已知操作缺口，却宣称支持完整复原。

有上述问题时可以交付明确标记的开发预览供定位，但不得叫“0.4 验收完成”。性能门槛未达时也要明确区分“可测试”和“达到性能目标”。

## 3. 必要的自动验证

先按修改边界执行已有 AGENTS.md 的必要检查，不扩张成所有软件的完整测试矩阵。

| 改动边界 | 必须检查 |
| --- | --- |
| 机械/保护/持久化 | `tests/test_core.py`、`tests/test_reference_maps.py`、`tests/test_crash.py`；新增约束的独立 full-label oracle |
| preparation 接线 | `tests/test_preparation.py`，shared lock、pending 保留、完整段、epoch/head/rev/policy/intent 失效 |
| 进程/启动/退出 | 已有 lifecycle/ownership/auth/parent-pipe 测试；仅处理自己启动的进程 |
| native 控件/输入 | x86 native host 编译、`tests/native/NativeHostRegression.cs` 对应 fixture，再实际 Windows/DirectX 检查 |
| filter/结构 | canonical/legacy、identity/position、layer partition/overlap、invalid rollback、exact hidden picking |
| macro/frame | 完整 source-to-destination composition、inverse、cycle分类、orientation-only、unknown reference、有限 witness 与资源上限 |
| block/尾段 | exact block invariant、每类 residual 与合法生成的可达 fixture；不得手工伪造不可达 orientation 状态 |
| packaging/rename | 版本、hash、必要资源、依赖、Unicode 路径、旧数据迁移与回退、无个人数据 |

端到端复测以同一个当前候选 build 为准。核心断言不依赖 GUI 显示字符串；GUI 测试也不能只调用绕过真实键盘路由的内部函数。

## 4. 刁钻用户场景

每个场景记录：构建 ID、隔离 fixture、输入顺序、预期、实测、证据路径、发现的问题与修复。未运行填 `Not run`，没有设备填 `Unverified`，不要默认 Passed。

| 编号 | 用户会怎么做 | 必须看到的结果 |
| --- | --- | --- |
| U01 | 连续选择不同目标，迅速回到之前目标 | stable target/identity，晚响应不覆盖当前选择，草稿不丢 |
| U02 | Required piece 被 filter 隐藏 | 能在关系表/Local 找到，reference-only不可twist，可明确Union显示 |
| U03 | 先 review，再改 protection，再按原 commit 键 | 旧结果Stale，完整复核前不执行 |
| U04 | Hold grip 时切到文本框/IME、Alt-Tab、关闭小窗 | 清除held state，不发生ghost twist |
| U05 | 快速重复twist、宏执行busy时继续按键 | 明确接受/拒绝，不无界排队，不重复执行 |
| U06 | 在Global按Enter，而Main有pending preview | 只选中Global对象，不commit |
| U07 | 改键为已用组合，再取消编辑 | 冲突准确，原配置保持，捕获不触发动作 |
| U08 | Current piece移动后直接使用原profile | 映射仍指向可见的固定C ID，不偷偷跟随 |
| U09 | 分别选择1/2/5/20-cell类别的目标位置 | 信息能读，完整对应可达，20-cell无简化假角度 |
| U10 | 2-color piece实际有18个affecting caps | hosting和affecting清楚分开，额外cap仍可用 |
| U11 | 用户输入不符合star的合法macro | 忠实显示Other effect/真实cycle，不替换输入 |
| U12 | 输入macro本体安全，但prepare/cleanup造成损坏 | 检查整段并拒绝净保护冲突 |
| U13 | 完整段安全，中间会动protected piece | Net safe与prefix状态分开，无误报永久损坏 |
| U14 | 长macro分析途中取消，再编辑继续 | 无正式状态变更，不丢草稿，可重新review |
| U15 | Undo回到相同labels后尝试旧review | 旧head/epoch上下文不复用，重新review |
| U16 | 有pending时inspect另一个piece，再用新review | pending保留，替换需明确动作，失败不毁旧预览 |
| U17 | Filter先Union再Subtract，保存live/frozen两类并重开 | 成员语义正确，复杂表达式未被context注释覆盖 |
| U18 | 关闭/最小化两个小窗，变更屏幕，再重开 | 停止无效绘制，恢复可见布局、camera和pin |
| U19 | 连续处理一个block，再切换orbit，回来继续 | 进度与保护正确，target/frame/key/filter可恢复 |
| U20 | 只剩buffer或orientation remainder | 有明确尾段工作路径与真实限制，不自动假解锁 |
| U21 | Preview完成后引擎断开或renderer失败 | committed state不被draft覆盖，可恢复，旧token失效 |
| U22 | 用户New、Resume、Reset view、Reset puzzle混合操作 | 只改变对应状态，长期进度有恢复点 |
| U23 | 合法操作完成solve，然后undo/redo；再reset/import solved | 正确summary只对应真实新completion，区别来源/谓词 |
| U24 | 导入损坏/错误model/超长macro或profile | 明确错误，原session/library/draft不受损 |
| U25 | 同一模板更换本次piece/reference，再次使用 | 重复设置沿用，本次角色明确，完整重新review，无自动解法 |
| U26 | 模板绑定错误orbit、缺少reference或固定宏不匹配 | 指明差异、保留输入；不搜索setup、不自动换宏 |
| U27 | 修改库中被模板引用的macro，再重开模板 | 旧版本仍固定，明确更新才改内容，相关review失效 |
| U28 | Draft录制与正式状态切换，期间快速输入和undo | 各视图同一draft revision；正式progress不混入预演；undo归属与输入scope清楚 |

避免把表中每一项与所有窗口尺寸、所有orbit相乘。选最有代表性的组合；出现新的缺陷类别才扩大相应覆盖。

## 5. 连续复原过程的实用性验收

至少做一组**连续30个preparation cycles**的试用，覆盖1/2/5/20-cell类型、至少一个用户自制/录制macro、一个block、一次保护冲突、一次取消与一次重开。其中至少10个cycle复用同一人为操作模板，检查每次只改必要输入的体验，不能都做首次配置演示。这是拟定最低试用规模，不是人类完成整个puzzle的证明。

同时提供 35-orbit 收尾覆盖表，记录：普通目标、fixed buffers、orientation remainder、依赖保护、支持的输入契约、合法 witness fixture 和未覆盖情形。UI 的 30 cycles 与机械收尾覆盖是两种证据，不互相代替。

试用中记录可观察事件：目标查找、buffer/frame 重设、view切换、保护干预、输入错误、恢复时间、实际等待。计时由明确本地测试流程启动，不引入默认在线telemetry。

实用性建议门槛：

- 高危误操作、丢失工作、隐性解锁：0 次。
- 核心流程的 keyboard 可达率：100%。
- 在支持的操作条件下，每个测试 cycle 都能到达明确的执行/拒绝/恢复结果，没有无提示死路。
- 同一工作上下文处理多个目标时，不需反复输入相同 buffer、保护和宏参考信息。
- 用户能说明模板保留了什么、这次自己选择了什么；未指定目标、宏或必要reference时，系统没有自动替自己完成决定。
- 用户能从当前界面正确指出 target、required piece、A/B occupant、作用范围和保护状态，无须到原始 JSON 寻找答案。

以 0.3 做相同 fixture、相同用户任务的配对对照，交错顺序并进行少量练习。预先记录目标：减少可避免的重复配置与上下文切换，且不增加严重错误。若要量化，可将这些事件的中位数下降 ≥30% 作为拟定产品目标；未达到则定位原因，而非挑选最好几个样本。

耗时以分布和任务类别报告，勿把思考时间推定成计算时间。没有基线或人数不足时只报告描述性结果，不宣称总体成功率提升。由AI操纵软件的试用必须标记为agent-driven，不能替代人类试用。

## 6. Benchmark 的必要范围

保留上一版硬性目标及其证据边界：instant turn p95≤100ms；结构/辅助导航p95<50ms；adaptive 1080p30FPS；full-detail1080p30FPS另测。

每条关键短交互建议至少100个自动测量样本，注明warm/cold、分辨率、DPI、可见贴纸数、render mode、前后端耗时和硬件。长macro检查采用少量有代表性的固定recipe，避免把测试时间浪费在重复展开同一证书。

重点比较：键盘输入到正确帧、Local选择到对应更新、Global两组切换、filter preview/apply、完整review、commit、关窗后的idle绘制、重开恢复。不可用仅render FPS掩盖commit延迟。

对比预算固定：一次基线、一次候选；只有代码变动或发现新问题才重复相关项。未达到目标必须写明阶段瓶颈、已尝试优化和剩余限制。只能称“可测试候选”，不能说目标全部通过。

## 7. 审查后的调整闭环

对每个问题写四行即可：

```text
Trigger: 用户连续做什么会遇到它。
Impact: 造成找错对象、增加几次输入，或破坏哪条安全/恢复边界。
Change: 修改哪个交互或实现边界，为什么。
Evidence: 相关场景和必要回归的实际复测结果。
```

至少完成一轮有真实使用记录的检查；如果确实没有发现问题，报告检查范围与证据，不编造缺陷凑数。发现阻断问题后继续调整，不能把“优化建议”留给用户自行处理就宣布完成。

## 8. 未来测试包必须包含什么

- `Magic600Cell-0.4-test-<build>.zip` 或等价清楚名称的 Windows 便携包。
- 一个明显的程序入口和明确的 `Isolated test session` 入口；不得默认连接用户个人0.3数据库。
- 同build的manifest、SHA256、依赖说明、旧版本数据兼容和回退说明。
- 无需修改个人存档的 E1 等真实合法 fixture，附预期状态、来源与生成方法。
- `TESTING_GUIDE.md`、`CHANGELOG.md`、`KNOWN_ISSUES.md`，public文案全部English。
- 实际验证记录及未运行项目。排除个人日志、数据库、用户名/绝对路径、聊天记录、实验框架、benchmark hooks、编译器/fixture执行文件及不可再分发依赖。

本轮未制作该测试包；它是后续开发 prompt 的必交付物。

## 9. 给用户的测试路线（未来版本）

1. 解压后从隔离测试入口启动，确认标题、版本、solved初始状态和session来源。
2. 打开E1 fixture，选target35778，检查required piece确实在A2712，Home为C113。
3. 只用键盘打开Local、Global、Keys；选择grip，确认键盘上显示真实C ID，移动相机后按键含义不变。
4. 输入E1提供的macro，查看方向、56primitives、3piece/3sticker support，review后明确commit。
5. Undo，保护O33并隐藏它，再review相同macro，确认阻止提交且能定位隐藏冲突。
6. 换双cell/18cap与20-cell样例，确认结构和frame表达易读，能够查到所有对象与grip。
7. 录制自己的短macro，保存到相应orbit；查看真实effect，明确加入plan，而不是按库条目就执行。
8. 保存一个简单操作模板并重复使用，检查E8的角色不匹配与旧review失效；再建小block、保存filter/profile/workspace，退出并Resume，确认上下文恢复。
9. 依指南检查尾段案例；记录最难理解和最费操作的地方，尤其是Local/Global是否真正帮上忙。
10. 用Reset view验证只重置视角，再在隔离会话Reset puzzle并恢复checkpoint，最后导出测试结果。

遇到实际结果与指南不一致，记录步骤与可见状态即可；不要公开个人session或整机诊断。

