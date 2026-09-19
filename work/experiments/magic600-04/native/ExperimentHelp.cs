// Contextual help is read-only and uses the existing owned-dialog input boundary.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

internal sealed partial class ExperimentShell {
 sealed class HelpTopic {
  internal readonly string Id,Title,Keywords,Body;
  internal HelpTopic(string id,string title,string keywords,string body){Id=id;Title=title;Keywords=keywords;Body=body.Replace("\r\n","\n").Replace("\n","\r\n");}
  public override string ToString(){switch(Id){case "workspace":return "Workspace";case "blocks":return "Blocks & goals";case "grips":return "Grip & Twist";case "keyboard":return "Keyboard";case "macros":return "Macro Base";case "operations":return "Operation";case "protection":return "Protection";case "views":return "Local & Global";case "worksheets":return "Work sheets";case "session":return "Session & recovery";default:return Title;}}
 }
 static readonly HelpTopic[] helpTopics=new[]{
  new HelpTopic(@"residuals",@"Residuals, buffers and repeated work",@"endgame orientation finish buffer invariant net strict next candidates reference", @"Use the existing Solve window
Position, orientation and buffer work share the same operation strips, roles, protection and graphical area. There is no separate endgame workspace.

Read the actual remainder
Current shows each physical piece's present location to its Home. These arrows describe ownership, not a legal move. Operation shows the full chosen Prepare / Macro / Cleanup action. After is independently simulated from that complete operation. Fixed-position orientation changes remain visible even without a position cycle.
Residuals in Solve lists nonbuffer and buffer position errors separately from orientation errors at Home. The suggested sequence is position cleanup, nonbuffer orientation, Buffer A, Buffer B, exact completion. You may interleave them. A new position error changes the suggestion immediately; a previously reached stage is not permanent.

Choose the goal explicitly
Place piece requires the requested identity at the destination. Orient piece and Finish buffer also require an explicit orientation arrangement: Home goal supplies the exact Home labels, while Capture goal records an explicit supported arrangement. Prepare may be legal without finishing the goal. Permission and goal completion are separate.

Known operations
Expand Endgame within Solve's Macros section. Inspect an actual nonbuffer position and assign it explicitly as X, and Y when required. Choose a family and each q/r element; no correction is chosen from the residual. Slot letters a, b, ... refer to the displayed ordered transported frame, not Grip axes. Each cycle sends a letter to the next letter.
The retained families use their displayed fixed A/B positions. A mismatch with your work roles remains visible. Check effect verifies the entire recipe, including other orbits. Save macro creates a named library entry. Select saved inspects it; Add to phase is a separate action. The chosen orbit being unchanged does not imply the whole puzzle is unchanged.

Reference variants
Map frame takes an explicitly chosen source cell/frame and destination cell/frame. It verifies the geometric correspondence and emits actual legal turns; the geometric map itself is not an executable setup. The original macro and costs remain visible. The separate Reference command still means an explicitly entered legal R word.

Position and exact protection
Hold place retains the captured occupant at a fixed position while allowing its orientation to change. Hold exact preserves every captured label. They can coexist. Free exact and Free place remove only their named requirement; Free both explicitly removes both. Net checks the complete endpoint; Strict checks every primitive. Selecting an auxiliary never removes protection.

Use the library in current work
Compare current use checks up to twelve visible existing macros, keeping your Prepare and Cleanup fixed. Results are grouped by direct benefit, potential preparation, protection blockage or missing evidence. Only the last checked batch is ordered; other entries remain available and unchecked. Filter to another group to compare it. Reasons are computed evidence, not execution permission. Select, Add, Review, Preview and Execute remain separate.

Necessary invariants
An isolated A/B swap contradicts the verified even-permutation requirement of that orbit; another orbit cannot compensate it. With every other position and orientation exact, a single nontrivial C₂ or C₅ buffer remainder violates its invariant. D₅ may retain a rotation-subgroup remainder; A₅ may retain a nonidentity full group element. Necessary checks passing does not establish a solution under current protection. Inspect the displayed element and inverse, then choose your own known operation.

Continue and resume
A successful Insert or Place may activate your locked Next, including in another orbit. Otherwise it may advance to an eligible explicit block or shared-hosting candidate. This changes work focus, never executes another operation. Orbit drafts, chosen macro revision, reference and Local center are retained; positions are resolved again from the current Session. Returning requires fresh review.
Exact orbit completion may enable automatic protection only on an unfinished-to-complete commit. New/reset do not lock all orbits, and manual release does not immediately relock them. Last step shows a small journal-derived endpoint delta and lets you locate changed positions. It does not claim Strict safety or replace the journal."),
  new HelpTopic(@"workspace",@"Workspace and piece identity",@"current next home position identity select locate target",@"Purpose
Keep one physical piece in view while its position changes. Current is the piece you are working on; locked Next is a piece you deliberately bookmarked for later. Neither selection nor a bookmark protects the mechanical state.

Choose and inspect
1. Choose the working orbit by its mathematical description.
2. Inspect a piece, then explicitly make it Current. Merely hovering, opening a macro or changing a filter does not make that assignment.
3. Choose a fixed target position. Home is the piece's original address; Now is its current address. A piece and its target need not have the same address when building a block away from Home.
4. Pin Next when you have decided which physical piece to work on later. Replace, clear, locate and activate it explicitly.

Read the result
Current and Next names identify physical pieces by their Home structure. Their current positions update after accepted moves and undo/redo. A fixed target or buffer position stays fixed even when its occupant changes. Canonical IDs remain available in details and exact lookup; a sorted list row is never the object's identity.

Mathematical addresses
A structure label such as 1S·19C·1 states sticker count, affecting-cap count and the census orientation group (1 means trivial). Alpha and beta distinguish different exact hosting/cap structures with the same counts and orientation group; they do not mean left/right or rotation direction. A cell pole, structure label and short H/T region word locate a real sticker region. Some structures need u0/u1 local representatives because cell rotations do not connect their two region groups. The word is a geometric address, not a Twist instruction. Home names the physical piece; Current names its present fixed position. Complete copied addresses include object type, naming version and model identity. Copy canonical piece ID remains available separately.

Repeat and correct
Locate Next to inspect it without changing Current. Activate Next only when ready to change the working piece; activation consumes the bookmark. If a move satisfies Next, inspect that state yourself: the application does not choose another piece. Reassign a mistaken target explicitly and check the operation again. Filtering or changing a key set does not silently replace Current or Next."),
  new HelpTopic(@"blocks",@"Blocks and work goals",@"block home capture destination reference prepare insert endgame orientation",@"Purpose
A block records exact piece requirements that you intend to establish together. A nearby drawing or adjacency alone does not prove that pieces form a rigid block.

Establish the requirement
Add a piece's Home requirement, or capture the inspected piece at its present position and exact sticker arrangement. Capturing a target arrangement requires Current to occupy that target. This distinguishes the intended orientation from merely reaching the right position.

Choose the current goal
• Prepare: check your finite preparation and protection, without claiming that the target was inserted.
• Insert: compare Current with the chosen target and its required arrangement.
• Place piece: compare identity and destination only; orientation is a separate requirement.
• Orient piece: compare the explicitly required sticker arrangement, even when Current is already at Home.
• Finish buffer: compare identity and orientation at an explicitly assigned A or B position.
• Build block: check all recorded block requirements; the block must not be empty.
• Finish orbit: require every label in the active orbit and verified orientation references. A predicted finish is not a committed completion certificate.

Specify an orientation goal
Home goal explicitly requires Current's canonical Home arrangement; the destination must be its Home position. Capture goal records Current's present complete arrangement at its target. Choosing Orient piece or Finish buffer alone does not invent a requirement. Missing input stays visible; select or capture the intended arrangement yourself.

Operate and inspect
Choose the goal in Operation, compose your own steps, then check their complete effect. Goal outcome and protection are separate: an intermediate manual operation may preserve protection without completing the selected goal. Read both results before previewing and executing.

Repeat and correct
Protect completed work explicitly. Removing a block member is not the same as removing its separate protection. A block reference applies your explicitly supplied finite word to the desired requirements; it does not move the puzzle or move old protection to new positions. Check the changed requirements before continuing.

Boundary
The application verifies chosen operations. It does not search for preparation, select a macro combination or finish the orbit for you. A successful example or a zero residual count in one orbit is not evidence that a human completed the whole puzzle."),
  new HelpTopic(@"grips",@"Grip, frame and Twist",@"grip twist frame inverse hold latch corners buffer capture cap",@"Purpose
Grip selects the affecting cap and an ordered frame. Twist selects one exact legal rotation within that frame. Buffer A and Buffer B are separate solving roles attached to fixed positions; assigning them does not choose a Grip.

Set up the input
Open Keyboard and check the visible key set, active cap, ordered frame and input destination. The twenty visible Grip slots are convenient bindings, not a limit on the model's caps or generators. Hosting cells of a piece are not the complete set of caps that can affect it.

Read the notation
The marks a, b, c and d name the four ordered corners of the selected cap, not global x/y/z/w axes. The frame readout maps these selected corner names back to the retained base corners. For example, abcd ← cabd means selected a is base c, b is base a, c is base b and d is base d. This is a reference correspondence, not an executed move. A cycle such as (abc) means a → b → c → a. The opposite cycle is its exact inverse; a half turn is its own inverse. Three half turns and four three-cycle axes with inverse choices give eleven nonidentity cap rotations.

Use and rebind
Choose Hold or Latch deliberately. Pointer selection of a Grip latches it; choosing the same Grip again releases it. Capture or change the ordered frame explicitly and compare the old and new corner correspondence before applying. Moving pieces, changing the camera or filtering does not silently remap the captured Grip.

Inspect and repeat
A Local direction guide is valid only for the same cap and ordered frame as the active Grip. Local can intentionally show another cell; use the explicit Grip-center action to compare them. Check whether Twist goes to the selected draft phase or to the live operation boundary before pressing it. Switching key sets or losing focus clears held input and requires physical release before new meanings can act."),
  new HelpTopic(@"keyboard",@"Keyboard, key sets and editing",@"keymap keybind bank focus index onscreen single key input ime edit",@"Purpose
A visible key set gives single keys explicit meanings for the current activity. Sets are changed deliberately; selecting an orbit or activating Next does not silently change them.

Discover and choose
Open Keyboard to see effective mappings in their physical positions. Open the key-set picker, type an exact set ID or search its purpose, then activate the selected set. Previous, next and previous-set actions are also available. The header keeps the active set and input destination visible.

The orbit sets
Each moving orbit has A for Buffer A preparation, B for Buffer B preparation, I for insertion/block work, M for composing macros and E for residual work. Shared solving sets cover Workspace, Filter, Macro, Operation, Cycles and Keyboard. General window, save, reset and view commands live in Functions, whose modifier combinations appear below the main onscreen keys. Backslash opens Functions and returns to the previous set unless your edited binding assigns it another meaning. Legacy Views and Session sets remain available for existing bindings. Inspect actual assignments and prerequisites; a named slot does not supply missing captures or choose a macro.

Edit and verify
Use Index to find a command and edit its key. Right-click an onscreen key for its binding editor where available. Review the displayed scope, old key, new key and any conflict before applying. The five main rows stay visible by default. Extra keys opens the remaining modified shortcuts; its command is editable in Index. Custom unmodified keys remain visible without opening Extra keys. Focus a Grip key to read its complete cap and frame; the active Grip stays in the header. Advanced record editing preserves canonical bindings; labels are not execution identities.

Focus and correction
Text boxes and IME composition own typing. Application shortcuts must not consume text. Opening or closing a tool clears held input; release keys before resuming. Preview and Execute shortcuts require focus in the Operation controls. A pressed key, a selected Grip and a successfully completed asynchronous operation are different states; inspect operation feedback before assuming a turn was committed. Busy input is not an unlimited queue."),
  new HelpTopic(@"macros",@"Macro Base",@"macro filter tags pin import export star three cycle classification reference",@"Purpose
Macro Base stores finite operations that you chose. It helps inspect their real effects and reuse them without selecting a solving method for you.

Cycle inspection
Choose Selected macro, Macro steps or All steps to inspect that exact finite action. Each ring opens an actual resulting position cycle; fixed-position orientation changes appear separately. Dashed arrows are operation effects, not geometric adjacency or a setup sequence. Solid tokens remain actual occupants. Entry slots compares the source and destination occupants at the stated entry boundary, including after Prepare, without turning the puzzle. Frame changes or missing reference evidence remain distinct; an unverified frame does not prove a pure cycle. The Cycles key set supplies scope and page controls. Complete-operation protection is always checked separately.

Find and inspect
A saved group is an organizational label. Likely use ranks existing macros by verified effect structure, affected-piece concentration, support size and original execution cost. Details shows the score and each contribution. At most three orbit uses qualify: score at least 70 and within 10 of the best. This is a heuristic, not a success probability, mathematical proof or execution permission. Star is an independent exact label, with no extra score bonus. Other retains checked operations below the thresholds; possible uses remain available. Missing reference evidence is shown as needing verification. Protection changes current suitability without changing static use scores.

Choose Check library to check up to 72 existing entries. Stop check cancels remaining work and keeps completed read-only facts. No macro is selected, added or executed. Filter by likely use, exact effect kind, actual affected orbit, all supplied tags and pin. Unchecked effects cannot satisfy a checked mathematical facet. An empty result does not prove that no useful operation exists.

Select a macro to inspect its body. Its directed cycles, arrangement changes and collateral are facts about that fixed recipe. A certified star, an orbit-local three-cycle and another macro are distinct classifications. Pure position means positions change with no added orientation in the fixed, versioned reference; multiple disjoint cycles are allowed. Pure orientation changes orientation at fixed positions. Mixed changes both, and Unknown lacks sufficient frame evidence. One pure position cycle is an additional narrow label. The reference is independent of the chosen macro, camera and Grip. Every composition is checked again; it cannot inherit purity from its components. Missing reference is unverified, not proof of impurity. Other-orbit effects are listed separately. Fixed-position orientation describes changes at positions left fixed by the body; it does not certify moved-piece orientation.

Use and repeat
Choose the intended Prepare, Macro or Cleanup destination and explicitly Add or Replace steps. Selecting an entry does not turn the puzzle or insert it secretly. Hiding the selected entry with a filter does not choose a replacement. Check the complete composed operation against the current target and protection.

Label and exchange
Edit a name, usage note, tags or pin without changing the recipe. Save or cancel those edits before export/import. Export writes your selected macro to the local file you choose. Import first compares new, identical and conflicting canonical records. Conflicts prevent the entire import; identical entries retain local metadata. The file limit is 2,000,000 bytes.

Trust boundary
Compare and save a variant
Open Details → Compare, explicitly choose the second entry, then Check comparison. Whole-puzzle equality, reverse action and same action on an affected orbit are different results. An orbit that neither entry changes is listed separately. These are net-action comparisons; they never certify intermediate motion or the complete operation's protection.
Save inverse reverses the chosen fixed steps. Save with R uses the displayed finite legal word in chronological R^-1 / Macro / R order; this is not certification of a spatial frame. A changed source or R must be inspected again. Saving creates a separate personal entry with its source revision; it does not select, insert or execute it. Use Select saved macro, then choose a phase and Add explicitly. New variants do not inherit the old entry's purpose labels or pin.
During a comparison or save, Stop check requests cancellation. Wait for the result: if saving already completed, the saved entry remains and is shown. Closing a result leaves the working draft and any staged preview intact. In the shared Macro set, Compare and Save inverse have single-key routes shown on the Keyboard.

Imported proof claims are ignored; only locally checked effects are used. A name, tag, source claim or matching diagram does not establish equivalence, inverse action or star correctness."),
  new HelpTopic(@"operations",@"Prepare, preview and execute",@"operation draft prepare macro cleanup inverse reference execute preview cancel",@"Purpose
Prepare / Macro / Cleanup is a finite operation that you assemble. It connects your manual preparation, chosen macro and restoration steps through one review and execution boundary.

Compose
1. Select the input phase and enter exact legal turns, or explicitly add an existing macro.
2. Inspect the steps in order. Inverse Cleanup may compute the inverse of the Prepare word you supplied; it does not search for preparation.
3. If using a reference, supply the reference yourself. The explicit reference transform uses R⁻¹ / Macro / R; review the resulting fixed operation and its direction.

Check, preview, execute
Check evaluates the entire sequence on the current state, including hidden objects. Read the goal outcome, final protection and temporary motion separately. Preview stages the chosen operation for inspection. Actual, after Prepare, after Macro and after Cleanup are different views; a preview is not an executed state.

Explicitly Execute only after inspecting the current staged result. A read-only review or its hash is not execution permission. The authority is the existing preview/commit transaction, with its full state and context checks.

Repeat and recover
After execution, inspect the actual result before working on another piece. New clears the operation; Reuse keeps fixed steps available for another deliberately bound context. Neither chooses the next target. Cancel a staged preview to return to actual state without committing it. Stop requests cancellation of analysis; wait for acknowledgement before assuming it ended. Edits or state changes can make an old result stale: retain the draft, cancel obsolete staging as needed and check again."),
  new HelpTopic(@"protection",@"Protection and conflicting effects",@"protection final prefix temporary net hidden stale unknown conflict",@"Purpose
Protection records the mechanical state that your operation must preserve. It is separate from Current, locked Next, a filter or a piece highlight.

Choose the policy
Final result requires the complete Prepare / Macro / Cleanup operation to preserve the protected requirement at the end. Each turn also checks protected requirements throughout the sequence. Temporary motion and final damage are reported separately, so a cancelling operation is not confused with a destructive result.

Inspect the complete operation
A macro body that moves protected work may be usable inside your explicitly chosen cancelling composition. Conversely, a harmless body cannot certify unsafe preparation. Review the complete sequence over the full label state, including objects hidden by filters.

Understand the result
Fresh verified preservation, conflict, unknown and stale are different states. Unknown or stale results are not safe. When an individual conflict position is provided, locate it and compare the actual requirement and predicted effect. An aggregate orbit count is not a piece ID and does not identify one specific conflict by itself.

Correct and repeat
Change the operation or deliberately revise the intended protection, then run a fresh check. Do not remove protection just to silence an unexplained conflict. A saved work sheet preserves settings but cannot preserve an old safety conclusion. Moves, undo/redo, restored checkpoints and relevant context edits require current verification. Opening Help, changing a drawing or filtering alone does not change the mechanical state or authorize execution."),
  new HelpTopic(@"views",@"Local, Global and Piece Filter",@"local global filter transparent sticker cell center geometry camera corners",@"Purpose
Local gives a faithful view of the selected cell's sticker structure. Global supplies the larger cell arrangement and selected relationships. These views support understanding; they do not generate a preparation sequence.

Choose the Local center
Use the cell-color index or an explicit Current-center or Grip-center action. Center changes alter observation only: they do not rebind Grip, move pieces, choose Current or activate Next. Keep the current center distinct from the active affecting cap.

Read the structure
Puzzle display
Use Display below the actual puzzle, or Functions → Display. Hide framework removes only the outer cell frame. Adaptive motion temporarily draws fewer stickers while moving; full visible detail returns when still and exact picking remains complete. Cell/sticker size, field of view and the three lighting controls use the original native renderer. These controls do not change labels, protection, Current/Next or a reviewed operation. Instant turns remain enabled: the committed result appears without a turn animation. This is not a latency guarantee.

Local preserves the actual 433-sticker arrangement, layers and piece/sticker correspondence. Its a/b/c/d corner marks describe its ordered frame. Compare an input-direction guide only when the active Grip has that same cap and ordered frame. A projection, adjacency or topology path is not proof of an orientation relation or a legal setup.

Filter and inspect
Choose Piece Filter conditions explicitly. Excluded stickers become transparent context where supported; the identities and full mechanical state are retained. Protection checks still include filtered-out regions. A hidden Current or Next remains the same physical identity and can be located without selecting a replacement.

In Local and the hub, the active Grip's pale mark identifies positions belonging to that cap and included by the current evaluated filter. Excluded or unknown matches are not highlighted. A held Grip releases with its key; a latched or onscreen Grip remains active until explicitly released or reset. This outline shows cap membership, not proof that one particular Twist moves every marked sticker or preserves protection. Local's small visual cut spacing separates sticker shapes for readability; it does not change the mathematical model.

Repeat and correct
Rotate or zoom for inspection, reset the view when needed, and compare Local and Global without reconstructing the solving roles. Their separate windows share the authoritative session; the active window and input context remain explicit. Close a view to return to the previous workspace, then release held keys before continuing. If a geometric guide appears inconsistent, compare its center and ordered frame first rather than interpreting screen direction as a global axis."),
  new HelpTopic(@"worksheets",@"Reuse work sheets",@"worksheet template saved now comparison reuse load macro revision",@"Purpose
A work sheet saves your chosen fixed steps, macro revisions, reference, goal and explicit input structure. It reduces repeated setup without solving a new target automatically.

Save a deliberate method
Name a work sheet after composing the finite operation and its settings. Its saved inputs document that use; they are not permission to replace your current solving objects later.

Compare before loading
Open the sheet and compare Saved with Now for Current, target, Buffer A/B, block, target arrangement and protection policies. Inspect the fixed reference and goal that will load, and the provenance of saved macro revisions. Missing or changed library sources do not silently replace the saved finite recipe.

Load explicitly
Load fixed steps with these inputs keeps your current solving bindings and protection, while loading the sheet's fixed operation, reference and goal. It does not choose another target, activate Next, change your key set, search for preparation or pick a replacement macro. A mismatch or missing input remains visible; you may load the fixed steps and then bind the necessary objects yourself.

Verify and repeat
Run a fresh check for every reuse. A saved successful result is not a current safety certificate. If the comparison becomes stale, reopen it and inspect the new inputs before loading. Legacy or unknown metadata cannot establish an exact match. Cancel leaves the existing work unchanged. If an older staged preview is still present, distinguish it from the loaded draft and cancel or replace it deliberately before execution."),
  new HelpTopic(@"session",@"Session, scramble and recovery",@"session new resume undo redo checkpoint restore reset recovery journal scramble seed timer summary completion log",@"Purpose
The session journal is the authority for accepted operations. Native views and previews must agree with it; they are not separate editable copies of the puzzle.

Open Session
Enter Functions with its explicit return key, then open Session. The report reads the engine's journal, timer, move totals and recorded source. Refresh updates the report; the window does not maintain a second timer. Resume continues saved work without resetting it. After reopening, inspect Current, locked Next, active orbit, key set, draft and protection before continuing.

New and reset
New solve starts at Home with the default view and a new timer attempt; previous progress remains recoverable. Reset puzzle returns labels to Home and keeps view preferences. Reset view changes cameras and projection without changing puzzle labels or work. Reset workspace clears working selections, steps and presentation while keeping puzzle state, journal, personal key sets and macros. Each state-changing reset names its scope before Apply. Cancel preserves the existing work. New and reset are not completed solves.

Stage a scramble
Choose Short (100 legal turns), Full (1000), or a custom count from 1 to 10000. A blank seed is generated and recorded; an integer seed reproduces the generated word. This is a legal turn sequence, not a claim of a uniform random puzzle state. Existing steps or a pending preview require explicit replacement permission.

Stage in Prepare loads the word into the existing Solve operation without executing it. Inspect the complete effect, run Review, create a Preview and Execute explicitly. Existing protection still applies; a scramble cannot silently remove locks. Cancel before staging keeps the existing operation. After staging, edit or clear the draft using the ordinary operation controls.

Timer and completion
Start timer and Pause timer control the engine's timer explicitly. The report retains the recorded source and assistance information; practice or assisted operations are not certified human solving. A new successful commit can show a whole-model summary only when every label is exactly at Home and the required fixed frames are verified. Local insertion or an orbit result is not whole-model completion.

The automatic summary is acknowledged once. Resume, report, undo/redo, reset and reopening a saved record do not create another completion. Last completion reopens the matching recorded result at the current journal step, including its exact predicate, timer, totals and source. Save log writes the engine's current session log; Review work returns to the existing Solve controls. A raw Home label check alone is not this certificate.

Log files
Import inspects a C600 proof log (JSON or compressed) or an MPUlt log, then waits for explicit Import. The file, current state, pending operation and preferences must still match the check. Import creates a journal branch and a Before import recovery checkpoint; it retains current preferences and work. Undo follows the imported branch. Restore the named recovery checkpoint to return to your previous branch and its preferences. A solved imported log does not create a solve summary. Export writes the selected format through the existing verified mapping. Imported source claims are not proof of a human solve. Limits are 16 MiB per file and 24 MiB expanded C600 JSON.

Undo and redo
Use the explicit commands to move through accepted operations. Physical piece identities continue to identify the same pieces, while their current positions update. Old operation reviews become stale after a state change; check again before executing a reused draft.

Checkpoint
Save a named checkpoint before a risky or lengthy change of working context. A checkpoint records full puzzle labels and session preferences. Restoring it restores those saved preferences as well as the puzzle: it can replace the current draft, Current, Next and other saved workspace settings. Inspect what was restored before continuing; it is not merely a camera bookmark.

Failures
A failed or incomplete native-state adoption is not an accepted mathematical result. Keep the reported error, avoid issuing repeated moves into an uncertain display and use the existing recovery path. Do not treat a previously displayed safe review as current after a restored state. Help describes available controls; it does not certify an untested recovery scenario or a completed human solve.")
 };
 void RegisterHelpCommands(){Register("help","Read help for the current work context",()=>ShowHelp(HelpContextTopic()));}
 string HelpContextTopic(){
  var button=FocusedOwnedControl() as Button;string command;
  if(button!=null&&commandButtons.TryGetValue(button,out command)){
   if(command.StartsWith("worksheet",StringComparison.Ordinal))return "worksheets";
   if(command.StartsWith("macro",StringComparison.Ordinal))return "macros";
   if(command.StartsWith("block",StringComparison.Ordinal)||command.StartsWith("goal",StringComparison.Ordinal)||command=="target-capture")return "blocks";
   if(command.StartsWith("next",StringComparison.Ordinal)||command=="focus")return "workspace";
   if(command.Contains("protection")||command.StartsWith("prefix",StringComparison.Ordinal)||command=="review-details")return "protection";
   if(command.Contains("filter")||command.StartsWith("local",StringComparison.Ordinal)||command.StartsWith("global",StringComparison.Ordinal))return "views";
   if(command.StartsWith("session",StringComparison.Ordinal)||command.StartsWith("checkpoint",StringComparison.Ordinal)||command.StartsWith("reset",StringComparison.Ordinal)||command=="scramble"||command=="restore"||command=="undo"||command=="redo")return "session";
   if(command.StartsWith("grip",StringComparison.Ordinal)||command.StartsWith("capture",StringComparison.Ordinal))return "grips";
  }
  string window=workWindows.Where(pair=>pair.Value==Form.ActiveForm).Select(pair=>pair.Key).FirstOrDefault();
  return window=="macro"?"macros":window=="keyboard"?"keyboard":window=="local"||window=="global"||window=="puzzle"?"views":window=="operation"?"operations":"workspace";
 }
 void ShowHelp(string initialTopic){
  using(var dialog=ToolDialog("Help · Magic 600 Cell",840,560)){
   dialog.MinimumSize=new Size(650,440);
   int line=Math.Max(form.Font.Height,TextRenderer.MeasureText("Ag",form.Font).Height);
   int contextHeight=4*line+10;
   var grid=ToolLayout(dialog,36,-1,contextHeight,42);
   var search=new TextBox{Dock=DockStyle.Fill,AccessibleName="Search help topics or feature descriptions",Margin=new Padding(0,3,0,5)};
   tips.SetToolTip(search,"Search a feature or action. Down opens the topic list; Tab moves to the article.");
   grid.Controls.Add(search,0,0);
   var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,190));body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));grid.Controls.Add(body,0,1);
   var topics=new ListBox{Dock=DockStyle.Fill,IntegralHeight=false,BorderStyle=BorderStyle.None,Margin=new Padding(0,0,8,0),AccessibleName="Help topics"};
   var article=new RichTextBox{Dock=DockStyle.Fill,ReadOnly=true,DetectUrls=false,WordWrap=true,ScrollBars=RichTextBoxScrollBars.Vertical,AcceptsTab=false,BorderStyle=BorderStyle.None,Font=form.Font,Margin=Padding.Empty,AccessibleName="Feature introduction and operating instructions"};
   body.Controls.Add(topics,0,0);body.Controls.Add(article,1,0);
   var context=new WorkReadout{Dock=DockStyle.Fill,Margin=Padding.Empty,AccessibleName="Unchanged Current, locked Next and protection"};context.Current.Margin=context.Next.Margin=context.Status.Margin=Padding.Empty;
   context.Current.Text=ReadoutPiece("Current · Home",Map(Value(work,"current")));context.Next.Text=ReadoutPiece("Next · locked · Home",Map(Value(work,"next")));
   context.Status.Text=protection.Text+" · "+(Object.Equals(Value(Workspace,"prefix"),true)?"Each turn":"Final result");
   tips.SetToolTip(context.Current,PieceCaption(Map(Value(work,"current"))));tips.SetToolTip(context.Next,PieceCaption(Map(Value(work,"next")))+"\nIdentity bookmark, not mechanical protection.");tips.SetToolTip(context.Status,context.Status.Text);
   grid.Controls.Add(context,0,2);
   var actions=ToolRow();var close=ToolButton("Close help");close.DialogResult=DialogResult.Cancel;actions.Controls.Add(close);var ownership=new Label{Text="Help owns keyboard input · Esc returns",AutoSize=true,Margin=new Padding(12,10,0,0),AccessibleName="Help input ownership"};actions.Controls.Add(ownership);grid.Controls.Add(actions,0,3);dialog.CancelButton=close;
   bool filling=false;
   Action show=delegate{
    if(filling)return;var topic=topics.SelectedItem as HelpTopic;
    article.Text=topic==null?"No matching topics.\r\n\r\nTry a feature or action such as Grip, Next, protection, filter or reuse. Your search has been kept.":topic.Title+"\r\n\r\n"+topic.Body;
    article.SelectAll();article.SelectionFont=form.Font;article.Select(0,topic==null?0:topic.Title.Length);using(var bold=new Font(form.Font,FontStyle.Bold))article.SelectionFont=bold;article.Select(0,0);article.ScrollToCaret();
   };
   Action fill=delegate{
    string selected=(topics.SelectedItem as HelpTopic)==null?initialTopic:((HelpTopic)topics.SelectedItem).Id;string term=search.Text.Trim();filling=true;topics.BeginUpdate();topics.Items.Clear();
    foreach(var topic in helpTopics)if(term.Length==0||(topic.Title+" "+topic.Keywords+" "+topic.Body).IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0)topics.Items.Add(topic);
    topics.EndUpdate();filling=false;int index=-1;for(int i=0;i<topics.Items.Count;i++)if(((HelpTopic)topics.Items[i]).Id==selected){index=i;break;}topics.SelectedIndex=index>=0?index:topics.Items.Count>0?0:-1;show();
   };
   search.TextChanged+=delegate{fill();};topics.SelectedIndexChanged+=delegate{show();};search.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Down&&topics.Items.Count>0){topics.Focus();e.Handled=true;e.SuppressKeyPress=true;}};
   fill();dialog.Shown+=delegate{search.Focus();};ShowOwned(dialog);
  }
 }
}
