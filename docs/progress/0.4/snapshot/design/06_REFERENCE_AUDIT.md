> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# 参考、基线与证据边界

检查日期：2026-09-13。

## 1. 已确认的参考对象

用户在预览器打开的两页已读取：

- [cutelyaware/MPUlt](https://github.com/cutelyaware/MPUlt)
- [HactarCE/Hyperspeedcube](https://github.com/HactarCE/Hyperspeedcube/)

HSC/HSC2 是同一仓库的稳定分支和开发路线，不能把它们误写成两个无关软件。

通过仓库API核实：

| 对象 | 检查到的版本/提交 | 使用范围 |
| --- | --- | --- |
| HSC stable release | `v1.0.10`，2025-01-05发布 | 稳定版行为参考；本轮未验证本机exe与release逐字节一致 |
| HSC v1 branch | `3ad693af295c5a0d4a21635e829ef8300b0bbcd0` | 键绑定表示、piece filter源码参考 |
| HSC2 newest listed prerelease | `v2.0.0-zeta.12`，2026-03-10发布；commit `5e50ab13cbd7cf025807451f42e438315e0dae4a` | 预发布参照，区别于main |
| HSC2 main | `63737aa467ed8eccacafaae3eec97cf1f980b794`，2026-09-07提交 | 当前开发源码；不能等同用户运行的release |
| MPUlt repository master | `0e2b86cbb713b8286083fa964ea67d6582337d5a`，2018-01-18提交 | 原macro、reset/scramble/solved逻辑参考；仓库未列release |
| 本项目下一版基础 | HEAD `5e1d35de792ccc8db896b8abf74ff2b1b00e0749`，含既有dirty docs | 本轮直接读取的0.3及preparation基础 |

“最新”仅指本检查时返回的对应release/branch，不代表稳定版、开发版和本机二进制相同。后续实施须锁定实际对照对象。

## 2. 从参考源码得到的具体结论

- [HSC2 keybind representation](https://github.com/HactarCE/Hyperspeedcube/blob/63737aa467ed8eccacafaae3eec97cf1f980b794/crates/hyperprefs/src/keybinds.rs)：区分物理键与语义键，修饰键有明确表示。本设计以物理绑定与清楚显示为主，但不直接复制其所有实现。
- [HSC2 filter expressions](https://github.com/HactarCE/Hyperspeedcube/blob/63737aa467ed8eccacafaae3eec97cf1f980b794/crates/hyperprefs/src/filters/expr.rs)：存在组合与颜色类表达式。借鉴可组合体验，不替换本项目既有grammar与identity/position语义。
- [HSC1 piece filters](https://github.com/HactarCE/Hyperspeedcube/blob/3ad693af295c5a0d4a21635e829ef8300b0bbcd0/src/gui/windows/piece_filters.rs)：读取到piece集合、类型/颜色选择和显示预览机制。实际快捷键体验还需运行验证。
- [HSC2 macro tab](https://github.com/HactarCE/Hyperspeedcube/blob/63737aa467ed8eccacafaae3eec97cf1f980b794/crates/hyperspeedcube/src/gui/tabs/macros.rs) 与 [scrambler tab](https://github.com/HactarCE/Hyperspeedcube/blob/63737aa467ed8eccacafaae3eec97cf1f980b794/crates/hyperspeedcube/src/gui/tabs/scrambler.rs)：这两个独立面板仍是占位。**不能据此说HSC2没有任何scramble能力，也不能把面板假定为成熟实现。**
- [HSC2 solve summary](https://github.com/HactarCE/Hyperspeedcube/blob/63737aa467ed8eccacafaae3eec97cf1f980b794/crates/hyperspeedcube/src/gui/modals/solve_summary.rs)：有明确复原summary、记录与保存交互。只借鉴本任务需要的本地总结，不引入上游在线服务。
- [MPUlt macro storage](https://github.com/cutelyaware/MPUlt/blob/0e2b86cbb713b8286083fa964ea67d6582337d5a/src/CMacroFile.cs)：宏保存名称、参考sticker/point与操作code，按puzzle识别。
- [MPUlt reference transform](https://github.com/cutelyaware/MPUlt/blob/0e2b86cbb713b8286083fa964ea67d6582337d5a/src/PuzzleStructure.cs)：`GetBestMatrix`以参考face/point选择变换。这是宏便利复用的参考，不是本项目自动获得正确映射的证明。
- [MPUlt main UI logic](https://github.com/cutelyaware/MPUlt/blob/0e2b86cbb713b8286083fa964ea67d6582337d5a/src/rubikHT.cs)：读到短/full scramble入口、NewScene/reset、复原检测与祝贺逻辑。本机版本和完整交互仍需核对。

这些引用用于描述已查看的上游事实；本设计中的布局、交互契约、工作流和验收门槛是针对Magic 600 Cell提出的方案。

用户本轮最后补充：抽象视角核心是基于macro base进行转动之外的操作，形成高度重复可用的复原操作，复杂度适中并保持人工主导。设计据此提出简单操作模板、明确角色/reference与四步工作单；这是本项目的设计提案，不宣称上游已有同样实现。

## 3. 已继承的上次硬要求

来自同项目任务“修复600 Cell渲染报错”的0.3开发输入及后续steering，并与现有AGENTS/NEXT_UPDATE核对。这里记录要求摘要，不导出原对话。

| 上次硬要求 | 0.4处理 |
| --- | --- |
| 完整model和original binaries不可改 | 保留；品牌rename不改变model identity |
| 全259,800 labels，合法1200generators | 所有视图/filter/分析均不能削减机械范围 |
| Shift-left/Shift-right精确语义与4D drag | 保留为piece-focus统一动作的兼容入口 |
| native_to_lab、identity/position区分 | 写入FocusContext与端到端验收 |
| Annotation独立、隐藏不可交互、阻止click-through | 保留work mask/context layer/full analysis三层 |
| C1–C600与旧zero-based语法兼容 | 保留，新增typed导航不重解释旧grammar |
| graph face-adjacency、连通四正则、非度量布局 | Global分层表达，不伪造move path |
| center-origin BFS layers及whole-piece重叠 | 保留；block不替代layer定义 |
| V1–V120，20 cells/vertex，4vertices/cell | 保留真实incidence与20-cell界面压力验收 |
| Boolean filters、preview、Replace/Intersect/Union | 延伸Subtract/live query，保留原子rollback |
| 单次atomic update、分revision、delta/full恢复 | 保留并覆盖辅助视图/protection/progress |
| instant turn100ms、explorer50ms、adaptive30FPS | 保留目标，未达不能报通过 |
| full-detail1080p30FPS最低GPU不编造 | 保留独立证据要求 |
| 两个可选辅助小窗、独立camera、sharedstate、小屏drawer | 保留容器行为，内容升级为piece/目标关系 |
| 原生小窗生命周期、关闭停止绘制、hover不请求DX | 保留并扩展持久化布局 |
| 视觉像专业几何工具，无AI模板感、持续真实审查 | 精细文字状态稿与连续使用验收 |
| Experimental architecture隔离 | 本轮只在个人设计输出和reference目录工作 |
| 小而有效验证＋必须critical checks | 聚焦修改边界；本次新增真实使用审查，避免无关全矩阵 |
| 英文public text、归属、有限致谢与隐私 | 完整继承 |
| 保存/恢复/进程归属、原生Windows证据 | 完整继承 |
| 0.4宏前准备优先、1.0现代前端迁移 | 继承，并加入本次“可发表”要求 |

旧任务中针对0.2.4的Release冻结要求后来已被用户正式发布0.3的指令更新。当前方案不照搬过期版本策略，也不把历史公开发布授权扩大到本轮的新Release。

## 4. 本轮实际只读验证

使用现有 `core.Model`、`PuzzleState` 和 `grips`，未打开个人Session数据库、未写正式状态、未操作运行中的puzzle。

- Model ID：`58c83f336e451aa64147fe1b8db2d60d7d43806aa3a35122f8403d5af51253f9`。
- 模型读取到177,120 pieces与259,800slots。
- hosting-cell数量分布：1cell有107,400pieces；2cell有66,000；5cell有3,600；20cell有120。合计177,120。
- O33、node0、sign+1从solved生成的合成fixture，在明确输入inverse star后全部labels恢复solved；inverse为56primitives，net support仅该orbit3pieces/3stickers。
- O32 position14056：hosting C43/C59，affecting cap count18；O00 position6290：hosting C19/C24，cap count2。
- O34 position17810：20hostingcells，orientation group A5/order60。

可检查的个人工作证据位于本项目 `work/reference-04/verified_design_examples.json` 与生成脚本；真实recipe和关键数据已抄入精细设计稿E1–E4。该脚本仅用于本轮设计证据，不进入最终产品或发布包。

这些验证证明样例没有凭空编ID和净作用，不证明新的UI、交互、保护接线、endgame、性能或人类可完成性。

## 5. 明确未完成的观察

- 首次Windows Computer Use因无法可靠判断浏览器URL而停止，未完成原生软件界面检查。用户后来在预览器打开仓库，已成功读取两页主页；这不等于完成了原生HSC/HSC2/MPUlt操作审查。
- 未逐字节验证本机两份HSC exe的release身份，也未证明本机MPUlt_155与参考源码完全一致。
- 未在Figma等专业环境做图。根据用户steering，本轮只输出文字设计与可选文字关系示意。
- 未改0.3生产代码，未构建0.4，未跑0.4 benchmark或用户试用。
- 未验证所有35orbit的最终buffer/orientation收尾，也没有据此作完成概率或人类时间估计。

## 6. Astra 使用建议的依据

本轮已读取[官方Astra指南](https://developers.openai.com/api/docs/guides/latest-model)。它支持将自主执行范围与验证要求说明具体。设计包的分阶段材料、失败定位与预算建议是本任务的工程方案，不是官方token保证。

Codex界面的Ultra选项与API reasoning参数不能随意互相等同；后续使用用户实际选择的客户端能力。本轮不调整用户模型配置，也不宣称一段prompt可以更改模型设置。
