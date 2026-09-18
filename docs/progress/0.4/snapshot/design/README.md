> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Magic 600 Cell：0.4 开发架构

更新：2026-09-14。当前交付为架构与对话内可复制的英文开发 Prompt；本轮没有实现或构建 0.4。

## 当前唯一架构入口

[完整英文架构 0.4-R2](03_ARCHITECTURE.md) 已从头重写，整合原始完整框架和全部后续补充。方法是 **Orbit First Block Building Solving**，三项核心是 **piece-focused、Keybind、抽象图形 hub**。

架构具体规定了三层 Macro Base 分类、完整保护与 star 语义、Block/Operation/Reference 主界面、Current/Next 身份追踪、35 个 orbit 各五套标准键位、可编辑全软件命令、Local/Global/Filter 联动、人工模板、尾段流程、冗余功能移除、参考软件对齐、验收与测试包。

## 最新开发审核流程

1. P0 核对基线、原有修改、参考证据和功能迁移清单。
2. P1 在隔离实验区完成两个可操作版本：**G1 Grip/Twist 机制**、**G2 抽象视角具体前端**。分别提供测试入口和简短审核指南，不能只交文字方案或静态图。
3. 两项分别由用户明确审核。两项都通过后才继续 P2–P6 正式集成、完整开发、刁钻用户验收、调整复测和测试版交付。

这一流程来自用户最后的明确补充，替代此前“仅 Grip/Twist 需要审核”的要求。当前架构不代表这两项实验已经获批。

## Prompt 与历史材料

当前可直接启动开发的英文 Prompt 在本次对话最终回复中，不新建 Prompt 文件。旧 [04_ULTRA_PROMPTS.md](04_ULTRA_PROMPTS.md) 仅作历史记录，不再作为执行入口。

[参考版本与证据审计](06_REFERENCE_AUDIT.md) 保留已检查的源码、版本和事实边界；版本观察日期是 2026-09-13。源码读取不能当成已完成 HSC/HSC2/MPUlt 原生交互检查。

00、01、02、04、05 文件已标记为历史草案；其中旧布局、审核门槛和阶段顺序不覆盖新版架构。未来实施通过根目录 `PROJECT_MEMORY.md` 定位实际源码，并重新检查其 `AGENTS.md` 和已有未提交修改。
