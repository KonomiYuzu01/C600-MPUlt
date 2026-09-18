> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

> **历史草案，已被替代（2026-09-14）**：本文件保留供追溯，不作为当前开发指令。请以 [新版完整架构 0.4-R2](03_ARCHITECTURE.md) 和用户最新消息为准。最新流程是先在隔离实验区完成 **G1 Grip/Twist 机制**与 **G2 抽象 hub 具体前端**的可操作版本，分别交给用户独立审核，两项都明确通过后再继续正式集成。旧布局、单审核门槛、旧 Prompt 和缺少 Next／每 orbit 多套键位的描述均已被替代。当前英文开发 Prompt 直接提供在对话中，不另输出文件。
# 先读这份：三项核心机制怎样一起帮助复原

这次更新的核心固定为：**piece-focused、keybind、抽象视角**。

我把它们理解为三个相互配合的能力：

- **piece-focused：始终知道自己在处理谁。** 换视图、做转动、改filter、关窗重开以后，目标与piece的关系都不会断。
- **keybind：把我已经想好的操作快速、准确地做出来。** 不反复寻找cell、重填参数或猜键位现在指向谁。
- **抽象视角：基于macro base，把我决定的复原操作组织起来并反复使用。** 这里主要做选宏、指定对象与参考、组合、检查、保存模板等转动之外的操作。结构图让这些操作容易判断；具体转动仍可用keybind完成。

Solve是这三项机制一起工作的地方；buffer、insertion、orbit protection是它们最重要的应用场景。Macro base、piece filter、progress和block building围绕它们展开。

## 一个简单例子

假设我现在要把某个piece放回目标位置：

```text
我固定这次目标
       │
       ├─ piece-focused：一直追踪这个piece，目标位置不乱跳
       │
       ├─ 抽象视角：选用已有macro或自己保存的操作模板
       │                 指定这次piece、参考关系和保护范围
       │                 用Current/Home和A/B结构检查是否适配
       │
       ├─ keybind：围绕这些明确的cell，输入我选定的grip和twist
       │
       └─ 抽象视角：检查我组合的整段操作，并保留模板
                         │
                         ├─ 是否符合我想要的piece/buffer变化
                         └─ 整段结束后是否保住已完成的成果
```

这三项会循环配合。一次完成以后，模板、视角、键位、buffer和保护条件尽量沿用。下一次主要由我指定新对象、处理实际差异和决定执行，避免从头搭建同样的操作。

## 抽象视角中的“操作模板”到底是什么

可以把它理解成我自己摆好的一张工作单。例如，我选定某个orbit的一条已有macro，约定它使用哪组buffer、需要我指定哪个piece与reference，保存我采用的Prepare/Macro/Cleanup组合方式。

下一次打开同一张工作单，软件记得重复的部分。我仍然需要选本次piece、确认reference、补上需要的人工preparation，并查看完整影响后执行。程序可以检查我给的输入、展开明确的宏和计算确定的参考变换；它不根据目标自己搜索setup、选择解法或连续处理下一批piece。

0.4只做少数固定动作与三段组合，不做流程编程语言。保存的是我的工作方式；上次的Ready状态与commit权限不会保存成下一次的通行证。

## 抽象交互图的具体概念

建议保留Main操作画面，抽象工作台以Macro base与当前操作模板为入口；Local/Global是指定对象和检查关系时可随时切换的视角。

```text
                 当前正在处理的piece / 固定目标 / A与B
┌─────────────────────┬────────────────────────┐
│                     │ Macro base / 已存模板  │
│     实际puzzle       │ [选择已有操作]          │
│     主要转动空间     │          ↓              │
│                     │ [指定piece与reference]  │
│                     │          ↓              │
│                     │ [Prepare / Macro /      │
│                     │               Cleanup] │
│                     │                        │
│                     │ Local：检查局部对应     │
│                     │ Global：检查区域关系    │
│                     │ Effect：检查整段影响    │
├─────────────────────┴────────────────────────┤
│ 当前grip与键位提示       已输入的macro / review │
│ Orbit protection始终可见；冲突不因filter而消失  │
└──────────────────────────────────────────────┘
```

这是关系示意，不是精确尺寸或成品草图。需要同时比较时，可以固定两个小窗；普通工作时不要求同时管理所有窗口。

### Local：帮我看清这个piece

左边看它现在涉及的cell，右边看它应该回到的结构。点中一片sticker，两边同时指出对应位置。选择某个grip，就看见相关cell、转动方向和按键。隐藏的对象可以留下必要参考，但不能误操作。

这比再画一张普通“邻域图”更有用：它直接解释我手里的piece与目标有什么差别。

### Global：帮我把两个地方联系起来

整体模型只作低细节参照，突出piece当前所在区域与目标区域。需要时改为buffer区域对目标区域。

我能看出它们在整体哪边、有没有共同cell、是否相邻，并从某个C ID定位到真实对象。表示结构连接的路线只帮助找地方，不表示“沿这条线转就能复原”。

### Macro workbench：把已有macro变成可重复的工作方式

我从macro base选宏，用明确的piece与reference填入角色，按需要组合前后段，检查并保存模板。这些都是我主动做的操作；模板下次保留布局与明确的组合，省掉重复设置。

选定或输入macro后，还能看到它把哪些位置上的piece送到哪里，A/B如何变化，以及哪些已完成的orbit会受到净影响。作用图是操作台的检查依据。

默认只展开当前orbit的重要关系，其他影响保留在紧邻的列表中；有保护冲突就突出具体对象。不把所有piece、所有路线和所有数字铺满屏幕。

## Keybind为什么要和图绑在一起

如果键盘只写H1、T2，用户仍然要在脑中转换它们对应哪个cell和方向。工作台应该让我同时看到：**这个键→这个grip→这个实际转动**。

编辑器仍然只有Grip和Twist两种动作，避免变成复杂脚本工具。转动画面或piece以后，已固定的键位不会悄悄换意思；要重新围绕当前piece建映射，就先展示变化再应用。

一个重要细节是：piece显示在哪几个cell中，与哪些cap能转动它并不总相同。真实模型里，有2-cell piece受到18个cap影响。所以“以piece为中心”必须保留这些操作入口，否则会在真正复原时限制自己。

## 优先审阅的三件事

1. Local的Current/Home对照，是否正好能帮我判断piece与目标的关系。
2. Grip/Twist键位与固定mapping，是否适合持续快速操作，包括20-cell piece。
3. 基于macro base的“选宏—指定对象—组合—验算—复用”是否足够简单，哪些地方仍然需要我反复设置，以及是否越过了人工决定的边界。

本稿将用户提到的index理解为主界面的统一功能索引；它不预先要求改成网页或指定`index.html`实现。最终技术选型仍按已有原生架构和实际交互验证决定。

完整要求见[想法整理](01_PRODUCT_BRIEF_ZH.md)，逐区域的细节见[精细交互设计](02_ABSTRACT_VIEW_SPEC_ZH.md)。本轮只输出文字，不把这些提案当成已批准的UI实现。

