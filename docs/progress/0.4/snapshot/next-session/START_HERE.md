> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Magic 600 Cell — 下次继续开发清单

记录日期：2026-09-16。用户要求今晚完成一个有限阶段后停工；不加定时安排，等待下一次明确提醒再继续。本清单与共享 PROJECT_MEMORY.md 一起读取，不能凭旧对话的 Pending 状态重开审批。

## 接续位置

- 源码：`<source>`。
- 当前实验：源码下 `work/experiments/magic600-04`。先读 `HANDOFF.md` 的最上方、今晚阶段证据及现行 `03_ARCHITECTURE.md`，检查未提交修改，不重启项目、不清理他人工作。
- 用户已明确批准 a156 原生样本的 G1/G2，后续 P2–P6 已授权。普通隔离开发自主推进；改变架构大方向、模型身份或持久化迁移的未决选择仍需确认。
- 今晚限定阶段已完成：显式宏比较／逆向／用户指定 R 变体接入草稿、完整保护、执行和工作单。原生 G2 085608 通过339项断言，G1 085932通过168项，两份45文件清单均匹配最终源码。12项新模型检查、14项宏库回归及core/reference/crash、NativeHost/layout通过；详见实验区 `evidence/macro-variants-20260916/REVIEW.md`。这不是完整 reference、endgame 或0.4完成。

## 下次先做

1. 先核对今晚最终测试结果。若有未收尾失败，接着修该具体失败；否则从完整人工复原链里选择当前最大障碍，不能又转去只改容易的装饰。
2. 已有明确候选：补齐 reference／最后 buffer 和 orientation 的真实工作流；测出命令采纳延迟的实际来源；解决小屏同时开 Keyboard 与 Macro Base 对核心图形的遮挡。按复现结果和操作代价确定先后，不猜测瓶颈。

## 不得遗漏的后续工作

- **复原链**：buffer/frame 准备、真实 block 要求与保留、完整 Prepare／Macro／Cleanup、Orbit Protection、插入、Current／锁定 Next、工作单重复使用和恢复。所有方法、宏、参考与执行仍由用户决定；无隐藏 setup 搜索、宏组合搜索或自动选目标。
- **Macro Base／参考／收尾**：三层分类及有范围的可验证关系；保留原配方、版本、成本和中途运动差异。用户指定合法字序共轭不等于几何参考映射认证。覆盖 orientation、20-cell、最后 A/B buffer 及所有实际可达残余类别，不能拿 endgame 按钮当完成证据。
- **独立窗口**：保留全屏工作台，Local／Global／Keyboard／功能工具共享状态并明确焦点；位置、大小、展开、关闭重开、手动布局和离屏恢复。Actual Puzzle 分离是已授权候选，现有 patch 尚未正式验证；必须复用一个 renderer 和 Session，不能偷偷更换框架。
- **键盘**：compact、真实按压反馈、Grip 高亮按 Piece Filter、可读键帽与精确 frame／实际置换一致；Local 中心色彩选择。所有操作在可见的当前集合中有可编辑单键入口；35×5 orbit banks 使用真实数据。失焦、IME、按住切组、旧键位兼容和停止检查不能留下错误输入。
- **忠实几何／命名**：Local 的实际 cell 433 stickers、邻接、层次与 piece 对应；20-cell 和透明 sticker filter。数学名称区分物理身份、Home 和当前位置，底层 ID 保持稳定；检查四组粗略摘要冲突及跨窗口、日志、输入的正确对应。
- **前端质量**：继续 compact、整齐、精细的小圆角和真实色彩；必要状态就地明确，冗余文字移入详细 Help。不得靠缩小字体或隐藏功能省空间。修复实测高影响缺陷，避免无休止审美改动。
- **参考软件对齐**：核实当前 HSC／HSC2／MPUlt／mc7d-kb 的实际版本与原生行为，包括键盘反馈、默认状态、New／Resume／reset、scramble、宏录制／参考／逆向／保存、filter、timer 和复原总结。源码审查不是原生行为验收。
- **进入程序与重置（单独验收，尚未完成全面对齐）**：按架构 S12/F01/U25 实际检查新会话进入后的 solved/reset 状态、默认 camera／projection／显示；不自动 scramble 或加载开发 fixture。Resume 保留长时间复原进度。Reset puzzle、Reset view、Reset workspace 各自作用明确；重置 puzzle 可恢复，视角重置不改数学状态，工作区重置不删除历史、个人宏或键位。进入程序不能默默重置已有存档。
- **完整复原后的弹窗（单独验收，尚未完成全面对齐）**：实际核查 HSC／MPUlt 的计时、完成总结、保存和回看流程，接入统一原生 UI。仅在真实满足明确完成条件时触发；区分 exact-label／face-color、练习／回放／导入与 scramble solve。每次局部插入不冒充完整复原；reset/import solved 不冒充一次复原，undo/redo 或重开总结不重复记成绩。必须实际触发、关闭和回看测试，不能只有按钮或静态示意。
- **完整验收**：至少 30 次连续混合工作循环，覆盖 1/2/5/20-cell，其中至少 10 次真实工作单复用并切换键位；另覆盖全部 35 个 moving orbits 和必要残余类别。建立 U01–U29、F01–F12 的当前构建证据映射。Agent 断言数不等于人类场景或人类完整复原。
- **性能与鲁棒性**：测实际 input→正确显示、结构导航、取消、恢复、密集状态、窗口遮挡和资源生命周期。已有约 1.3–1.8 秒命令采纳样本包含夹具等待，未证明达标。`tests/profile_compact_transport.py` 尚待执行；仅记录真实可用分辨率、DPI、GPU，不声称未测硬件通过。
- **整合与清理**：按旧功能→统一替代→独有能力保留→删除冗余→迁移验证更新 inventory。`docs/CONSOLIDATION.md` 含历史旧状态，需与真实实现核对；移除实验 probe／重复状态和过期帮助，保留不可变资产、单一 Session／journal、兼容存档与私有会话。
- **最终交付**：不再给用户逐次中间测试版；内部验证不能省略。完成后准备隔离 Windows 成品、英文指南、真实增量 changelog、manifest／hash、依赖与回退说明，并录制一段连续 solver 操作素材，连接改良 G1/G2 和后续功能，保留错误恢复与实际限制。
- **发行后的事项**：公开 0.4 发布需单独授权；说明仅写相对上一版真实更新。正式应用发行之后更新已有研究报告；0.4 确实完成之后重做完整迁移备份、逐文件 hash／恢复清单，并单独核查云同步完成。今晚阶段完成不触发上述完成条件。

独立的详细覆盖审查位于实际实验区 `docs/reviews/TOMORROW_COVERAGE_20260916.md`，含需求出处、Open／Unproved 区分和具体下一步。保留其审查时间边界，之后以新的 HANDOFF 与证据补充，不能把未验证项直接写成代码缺陷。
