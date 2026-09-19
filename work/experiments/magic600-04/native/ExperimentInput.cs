// Isolated WinForms input adapter. It never authorizes a Session commit.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

internal sealed class ExperimentInputState {
 internal bool Enabled = true, Busy, Modal, Capture, TextEditing, Ime;
 internal string BankId = "", GripMode = "hold", Destination = "draft", Phase = "prepare", FrameKey = "";
 // Values are canonical C1..C600 caps, never slot numbers or lab cell indices.
 internal Dictionary<string,int> GripKeys = new Dictionary<string,int>();
 internal Dictionary<string,string> TwistKeys = new Dictionary<string,string>();
 internal Dictionary<string,string> CommandKeys = new Dictionary<string,string>();
}

internal sealed class ExperimentTwist {
 internal int Cell;
 internal string Axis, Destination, Phase;
 internal bool Inverse;
}

internal sealed class ExperimentInput : IMessageFilter, IDisposable {
 readonly Form host;
 readonly Func<Form,bool> ownsWindow;
 readonly Func<ExperimentInputState> getState;
 readonly Action<int?> onGrip;
 readonly Func<ExperimentTwist,Task> onTwist;
 readonly Action<string> onCommand, onFeedback;
 readonly HashSet<string> down = new HashSet<string>(), blocked = new HashSet<string>();
 readonly Dictionary<string,int> held = new Dictionary<string,int>();
 readonly Dictionary<string,string> visualDown = new Dictionary<string,string>();
 readonly HashSet<string> visualRejected = new HashSet<string>();
 readonly HashSet<Form> windows = new HashSet<Form>();
 readonly Dictionary<Control,InputWatch> watches = new Dictionary<Control,InputWatch>();
 int? pointer, latched, active;
 string latchedCode, activeCode, context, diagnosticPrefix, diagnosticSafePrefix;
 bool attached, disposed, ime, inverseShiftHeld;
 volatile bool pending;
 int epoch;

 internal ExperimentInput(Form host, Func<Form,bool> ownsWindow,
   Func<ExperimentInputState> getState, Action<int?> onGrip,
   Func<ExperimentTwist,Task> onTwist, Action<string> onCommand, Action<string> onFeedback) {
  if(host == null || getState == null) throw new ArgumentNullException("host/getState");
  this.host = host; this.ownsWindow = ownsWindow; this.getState = getState;
  this.onGrip = onGrip ?? delegate { }; this.onTwist = onTwist ?? delegate { return null; };
  this.onCommand = onCommand ?? delegate { }; this.onFeedback = onFeedback ?? delegate { };
 }

 internal int? ActiveGripCell { get { return active; } }
 internal string ActiveGripCode { get { return activeCode; } }
 internal bool Pending { get { return pending; } }
 internal bool InverseShiftHeld {get{return inverseShiftHeld;}}
 void SetInverseShift(bool value){if(inverseShiftHeld==value)return;inverseShiftHeld=value;VisualFeedback();}
 internal int Epoch { get { return epoch; } }
 // Visual receipt is not a transaction outcome; only application-owned inputs appear here.
 internal Action VisualChanged;
 internal bool IsVisuallyPressed(string code) { return visualDown.ContainsKey(code) || visualDown.ContainsValue(NormalizeChord(code)); }
 internal bool IsVisuallyRejected(string code) {
  foreach(string physical in visualRejected) { string chord; if(physical==code || visualDown.TryGetValue(physical,out chord) && chord==NormalizeChord(code))return true; }
  return false;
 }
 static string NormalizeChord(string code) { return code==null?null:code.Replace("Control+","Ctrl+"); }
 void VisualPress(string physical,string chord,bool rejected) {
  visualDown[physical]=NormalizeChord(chord);if(rejected)visualRejected.Add(physical);else visualRejected.Remove(physical);VisualFeedback();
 }
 void VisualFeedback(){var callback=VisualChanged;if(!disposed&&callback!=null)callback();}
 // Off by default. Records types and route metadata, never control text or captions.
 internal Action<string> Diagnostic { get; set; }
 static string DiagnosticToken(string value) {
  if(value == null) return "none";
  return value.Replace('\r','_').Replace('\n','_').Replace('\t','_').Replace(' ','_');
 }
 static bool DiagnosticFunctionKey(string code) {
  int number; return code != null && code.Length >= 2 && code.Length <= 3 && code[0] == 'F'
   && Int32.TryParse(code.Substring(1),out number) && number >= 1 && number <= 12;
 }
 void Trace(string detail,string decision) {
  var callback = Diagnostic;
  if(callback == null) return;
  string prefix = detail != null && detail.StartsWith("code=redacted-editing-key ",StringComparison.Ordinal) ? diagnosticSafePrefix : diagnosticPrefix;
  try { callback((prefix ?? "origin=direct") + " " + detail + " decision=" + decision); }
  catch(Exception) { /* A diagnostic observer must not interrupt keyboard routing. */ }
 }
 string RouteDetail(ExperimentInputState state,string code,string command,string axis,bool text,
   bool shift,bool ctrl,bool alt,bool meta,bool repeat,bool already) {
  if(Diagnostic == null) return null;
  bool redact = text && !DiagnosticFunctionKey(code);
  return "code=" + (redact ? "redacted-editing-key" : DiagnosticToken(code)) + " bank=" + DiagnosticToken(state.BankId)
   + " enabled=" + state.Enabled + " busy=" + state.Busy + " pending=" + pending
   + " modal=" + state.Modal + " capture=" + state.Capture + " ime=" + (ime || state.Ime)
   + " text=" + text + " shift=" + shift + " ctrl=" + ctrl + " alt=" + alt + " meta=" + meta
   + " repeat=" + repeat + " already_down=" + already + " blocked=" + blocked.Contains(code)
   + " command=" + (redact ? "redacted" : DiagnosticToken(command)) + " axis=" + (redact ? "redacted" : DiagnosticToken(axis));
 }
 internal string HeldStatus { get {
  var keys = new List<string>(down); keys.Sort(StringComparer.Ordinal);
  var release = new List<string>(blocked); release.Sort(StringComparer.Ordinal);
  return "Held: " + (keys.Count == 0 ? "none" : String.Join(", ",keys.ToArray()))
   + "; release required: " + (release.Count == 0 ? "none" : String.Join(", ",release.ToArray()));
 } }

 internal void Attach() {
  if(disposed) throw new ObjectDisposedException("ExperimentInput");
  if(attached) return;
  attached = true; RegisterWindow(host); Application.AddMessageFilter(this);
 }
 // Call for every owned native auxiliary/dialog window, before showing it.
 internal void RegisterWindow(Form form) {
  if(disposed) throw new ObjectDisposedException("ExperimentInput");
  if(form == null || !windows.Add(form)) return;
  form.Deactivate += Deactivated; form.FormClosed += WindowClosed; Watch(form);
 }
 void Deactivated(object sender,EventArgs e) { ime = false; Reset("Window lost focus; release held keys."); }
 void WindowClosed(object sender,FormClosedEventArgs e) {
  var form = (Form)sender;
  form.Deactivate -= Deactivated; form.FormClosed -= WindowClosed; windows.Remove(form);
  Reset("Window closed; release held keys.");
 }
 void Watch(Control control) {
  if(watches.ContainsKey(control)) return;
  watches.Add(control,new InputWatch(this,control));
  control.ControlAdded += ChildAdded;
  foreach(Control child in control.Controls) Watch(child);
 }
 void ChildAdded(object sender,ControlEventArgs e) { Watch(e.Control); }
 void Unwatch(Control control) {
  InputWatch watch;
  if(!watches.TryGetValue(control,out watch)) return;
  watches.Remove(control); control.ControlAdded -= ChildAdded; watch.Dispose();
 }
 internal void SetComposition(bool value) {
  if(disposed) return;
  ime = value; Reset(value ? "IME composition owns the keyboard." : "IME composition ended; release held keys.");
 }
 internal void Reset(string reason) {
  if(disposed) return;
  foreach(string code in down) blocked.Add(code);
  bool hadVisual=visualDown.Count!=0||inverseShiftHeld;visualDown.Clear();visualRejected.Clear();inverseShiftHeld=false;
  held.Clear(); pointer = null; latched = null; latchedCode = null; epoch++;
  SetGrip(null,null);if(hadVisual)VisualFeedback(); Feedback(reason);
 }
 // Host calls after each accepted bank/mapping/destination adoption, before
 // drawing it. Observe intermediate changes even when no key event occurs.
 internal void RefreshContext() { if(!disposed) State(); }
 void Feedback(string text) { if(!disposed) onFeedback(text); }
 void SetGrip(int? cell,string code) {
  activeCode = code;
  if(active == cell) return;
  active = cell; onGrip(cell);
 }
 ExperimentInputState State() {
  var state = getState();
  var next = new StringBuilder();
  next.Append(state.BankId).Append('|').Append(state.FrameKey).Append('|').Append(state.GripMode).Append('|').Append(state.Destination)
   .Append('|').Append(state.Phase).Append('|').Append(state.Enabled).Append('|').Append(state.Modal)
   .Append('|').Append(state.Capture).Append('|').Append(state.TextEditing).Append('|').Append(state.Ime);
  AppendMap(next,state.GripKeys); AppendMap(next,state.TwistKeys); AppendMap(next,state.CommandKeys);
  string fingerprint = next.ToString();
  if(context != null && context != fingerprint) Reset("Bank or input context changed; release held keys.");
  context = fingerprint; return state;
 }
 static void AppendMap<T>(StringBuilder output,Dictionary<string,T> map) {
  var keys = new List<string>(map.Keys); keys.Sort(StringComparer.Ordinal);
  output.Append('|'); foreach(string key in keys) {
   string value = Convert.ToString(map[key]);
   output.Append(key.Length).Append(':').Append(key).Append('=').Append(value.Length).Append(':').Append(value).Append(';');
  }
 }
 void UpdateGrip(ExperimentInputState state) {
  if(held.Count > 1 || (pointer.HasValue && held.Count != 0)) { SetGrip(null,null); return; }
  if(pointer.HasValue) { SetGrip(pointer,"Pointer"); return; }
  if(state.GripMode == "latch") { SetGrip(latched,latched.HasValue ? latchedCode : null); return; }
  foreach(var pair in held) { SetGrip(pair.Value,pair.Key); return; }
  SetGrip(null,null);
 }
 string Unavailable(ExperimentInputState state) {
  if(!state.Enabled) return "Keyboard input is disabled.";
  if(state.Capture) return "Key capture owns the keyboard.";
  if(state.Modal) return "Close the modal before turning.";
  if(ime || state.Ime) return "IME composition owns the keyboard.";
  if(state.TextEditing) return "Text input owns the keyboard.";
  if(state.Busy || pending) return "Operation in progress; no turn was queued.";
  return null;
 }
 internal bool SetPointerGrip(int cell) {
  if(disposed) return false;
  var state = State(); string reason = Unavailable(state);
  if(reason != null) { Feedback(reason); return false; }
  if(cell < 1 || cell > 600) { Feedback("Choose an explicit cap C1-C600."); return false; }
  Reset("Onscreen grip selected; held physical keys require release."); pointer = cell; SetGrip(cell,"Pointer");
  Feedback("Onscreen grip C" + cell + "; " + state.Destination + " / " + state.Phase + "."); return true;
 }
 internal void ReleasePointerGrip() { if(disposed) return; pointer = null; UpdateGrip(State()); }
 internal bool Twist(string axis,bool inverse) {
  if(disposed) return false;
  var state = State(); string reason = Unavailable(state);
  if(reason != null) { Feedback(reason); return false; }
  if(axis != "H1" && axis != "H2" && axis != "H3" && axis != "T1" && axis != "T2" && axis != "T3" && axis != "T4") {
   Feedback("Choose a supported H/T axis."); return false;
  }
  if(!active.HasValue) { Feedback("Select one explicit grip before a Twist."); return false; }
  var action = new ExperimentTwist { Cell = active.Value, Axis = axis, Inverse = inverse, Destination = state.Destination, Phase = state.Phase };
  pending = true;
  try {
   Task task = onTwist(action);
   if(task == null) pending = false;
   else task.ContinueWith(delegate(Task done) {
    Exception error = done.IsFaulted ? done.Exception.GetBaseException() : null;
    pending = false;
    if(disposed || host.IsDisposed || !host.IsHandleCreated) return;
    try { host.BeginInvoke((Action)delegate {
     if(error != null) Feedback("Turn failed: " + error.Message);
     else RefreshFocusFeedback();
    }); }
    catch(ObjectDisposedException) { } catch(InvalidOperationException) { }
   },TaskScheduler.Default);
   Feedback("C" + action.Cell + " " + axis + (inverse ? " inverse" : "") + " -> " + state.Destination + " / " + state.Phase + ".");
   return true;
  } catch(Exception e) { pending = false; Feedback("Turn failed: " + e.Message); return false; }
 }
 static string Chord(string code,bool shift,bool ctrl,bool alt,bool meta,string controlName) {
  return (ctrl ? controlName + "+" : "") + (alt ? "Alt+" : "") + (meta ? "Meta+" : "") + (shift ? "Shift+" : "") + code;
 }
 // Synthetic fixtures call these same methods; native routing supplies scan-derived codes.
 internal bool HandleKeyDown(string code,bool shift,bool ctrl,bool alt,bool repeat,bool text,bool meta = false) {
  if(disposed || String.IsNullOrEmpty(code)) return false;
  var state = State(); bool already = !down.Add(code); string command,axis; int grip;
  if(!state.CommandKeys.TryGetValue(Chord(code,shift,ctrl,alt,meta,"Ctrl"),out command))
   state.CommandKeys.TryGetValue(Chord(code,shift,ctrl,alt,meta,"Control"),out command);
  bool hasGrip = state.GripKeys.TryGetValue(code,out grip); state.TwistKeys.TryGetValue(code,out axis);
  text = text || state.TextEditing;
  string detail = RouteDetail(state,code,command,axis,text,shift,ctrl,alt,meta,repeat,already);
  if(state.Capture || state.Modal || ime || state.Ime || (code == "Enter" && (ctrl || meta))) {
   Trace(detail,"pass-context-owner");
   if(active.HasValue) Reset("Focused input owns the keyboard."); blocked.Add(code); return false;
  }
  bool navigation = text && (code == "F1" || code == "F2") && !shift && !ctrl && !alt && !meta && (command == "bank" || command == "index");
  if(text && !navigation) { Trace(detail,"pass-text-owner"); if(active.HasValue) Reset("Text input owns the keyboard."); blocked.Add(code); return false; }
  if(blocked.Contains(code)) {
   if(command != null || hasGrip || axis != null) { Trace(detail,"require-release");VisualPress(code,Chord(code,shift,ctrl,alt,meta,"Ctrl"),true); Feedback("Release this key before using it in the new context."); return true; }
   Trace(detail,"pass-blocked-unmapped");
   return false;
  }
  SetInverseShift(state.Enabled&&(shift||code=="ShiftLeft"||code=="ShiftRight"));
  if(command != null) {
   if(repeat || already) { Trace(detail,"suppress-repeat-command"); return true; }
   if(!state.Enabled) { Trace(detail,"pass-disabled");VisualPress(code,Chord(code,shift,ctrl,alt,meta,"Ctrl"),true); return false; }
   Trace(detail,"dispatch-command");VisualPress(code,Chord(code,shift,ctrl,alt,meta,"Ctrl"),false); onCommand(command); return true;
  }
  if(ctrl || alt || meta) { Trace(detail,"pass-modified-input"); return false; }
  if(hasGrip && !shift) {
   if(repeat || already) { Trace(detail,"suppress-repeat-grip"); return true; }
   string reason = Unavailable(state);
   if(reason != null) { Trace(detail,"reject-unavailable-grip"); blocked.Add(code);VisualPress(code,code,true); Feedback(reason); return true; }
   if(grip < 1 || grip > 600) { Trace(detail,"reject-unassigned-cap");VisualPress(code,code,true); Feedback("This Grip needs an explicit C1-C600 capture."); return true; }
   held[code] = grip;
   if(held.Count > 1 || pointer.HasValue) {
    Trace(detail,"reject-multiple-grips");
    latched = null; latchedCode = null; SetGrip(null,null);visualDown[code]=code;foreach(string key in held.Keys)visualRejected.Add(key);VisualFeedback(); Feedback("Multiple grips are held; release them and choose one."); return true;
   }
   Trace(detail,"select-grip");
   if(state.GripMode == "latch") { latched = latched == grip ? (int?)null : grip; latchedCode = code; }
   UpdateGrip(state);VisualPress(code,code,false); Feedback(active.HasValue ? "Grip C" + grip + "; " + state.GripMode + "." : "Grip released."); return true;
  }
  if(axis != null) { Trace(detail,repeat || already ? "suppress-repeat-twist" : "dispatch-twist"); if(!repeat && !already) {VisualPress(code,code,false); bool inverse=axis.EndsWith("-",StringComparison.Ordinal); if(!Twist(inverse?axis.Substring(0,axis.Length-1):axis,inverse^shift))VisualPress(code,code,true); } return true; }
  Trace(detail,"pass-unmapped");
  return false;
 }
 internal bool HandleKeyUp(string code,bool text = false) {
  if(disposed || String.IsNullOrEmpty(code)) return false;
  var state = State(); bool owned = held.ContainsKey(code) || blocked.Contains(code);
  bool visible=visualDown.Remove(code);visualRejected.Remove(code);
  down.Remove(code); blocked.Remove(code); held.Remove(code); UpdateGrip(state);
  if(code=="ShiftLeft"||code=="ShiftRight")SetInverseShift(state.Enabled&&!text&&!state.TextEditing&&!state.Capture&&!state.Modal&&!ime&&!state.Ime&&((down.Contains("ShiftLeft")&&!blocked.Contains("ShiftLeft"))||(down.Contains("ShiftRight")&&!blocked.Contains("ShiftRight"))));
  if(held.Count==1&&active.HasValue)foreach(string key in held.Keys)visualRejected.Remove(key);
  if(visible)VisualFeedback();return owned && !text && !state.TextEditing;
 }
 static bool IsText(Control control) {
  for(Control c = control; c != null; c = c.Parent) if(c is TextBoxBase || c is ComboBox || c is NumericUpDown) return true;
  return false;
 }
 internal void RefreshFocusFeedback() {
  if(disposed) return;
  var activeForm = Form.ActiveForm;
  if(!Owned(activeForm)) return;
  Control control = activeForm;
  while(control is ContainerControl) {
   var child = ((ContainerControl)control).ActiveControl;
   if(child == null) break;
   control = child;
  }
  RefreshFocusFeedback(control);
 }
 // The focus-event path already supplies the actual receiving control.
 internal void RefreshFocusFeedback(Control control) {
  if(disposed || control == null || control.IsDisposed || !Owned(control.FindForm())) return;
  // Observe the context without State(): reporting focus must not change its epoch.
  var state = getState();
  if(!state.Enabled) Feedback("Keyboard input is disabled.");
  else if(state.Capture) Feedback("Key capture owns the keyboard.");
  else if(state.Modal) Feedback("Dialog controls own the keyboard.");
  else if(ime || state.Ime) Feedback("IME composition owns the keyboard.");
  else if(state.TextEditing || IsText(control)) Feedback("Text input owns the keyboard.");
  else if(state.Busy || pending) Feedback("Operation in progress; turn input is paused.");
  else Feedback((control.FindForm() == host ? "Work controls own" : "Owned window controls own")
   + " the keyboard; " + state.BankId + "; " + state.Destination
   + (state.Destination == "draft" ? " / " + state.Phase : "") + ".");
 }
 bool Owned(Form form) { return form != null && windows.Contains(form) && (ownsWindow == null || ownsWindow(form)); }
 public bool PreFilterMessage(ref Message message) {
  if(disposed) return false;
  bool keyDown = message.Msg == 0x100 || message.Msg == 0x104, keyUp = message.Msg == 0x101 || message.Msg == 0x105;
  if(!keyDown && !keyUp) return false;
  var control = Control.FromChildHandle(message.HWnd); var activeForm = Form.ActiveForm;
  bool activeOwned = Owned(activeForm);
  bool targetOwned = activeOwned && (control == null || Owned(control.FindForm()));
  long bits = message.LParam.ToInt64(); string previousPrefix = diagnosticPrefix, previousSafePrefix = diagnosticSafePrefix;
  if(keyDown && Diagnostic != null) {
   string owner = " active_owned=" + activeOwned + " target_owned=" + (activeOwned ? targetOwned.ToString() : "not-evaluated")
    + " active_type=" + (activeForm == null ? "none" : activeForm.GetType().FullName)
    + " target_type=" + (control == null ? "none" : control.GetType().FullName);
   diagnosticSafePrefix = "origin=native msg=0x" + message.Msg.ToString("X") + " key=redacted-editing-key" + owner;
   if(IsText(control) && !DiagnosticFunctionKey(CodeFromScanCode((int)((bits >> 16) & 255),(bits & (1L << 24)) != 0))) diagnosticPrefix = diagnosticSafePrefix;
   else diagnosticPrefix = "origin=native msg=0x" + message.Msg.ToString("X")
    + " vk=0x" + message.WParam.ToInt64().ToString("X") + " lparam=0x" + unchecked((uint)bits).ToString("X8")
    + " scan=0x" + ((bits >> 16) & 255).ToString("X2") + " extended=" + ((bits & (1L << 24)) != 0)
    + " raw_repeat=" + ((bits & (1L << 30)) != 0) + owner;
  }
  try {
   if(!activeOwned || !targetOwned) { if(keyDown) Trace("state=not-evaluated","pass-unowned-window"); return false; }
   string code = CodeFromScanCode((int)((bits >> 16) & 255),(bits & (1L << 24)) != 0);
   if(code == null) {
    if(keyDown) { Trace("state=not-evaluated","reject-unsupported-scan"); Feedback("Unsupported physical scan code; no experiment action was dispatched."); }
    return false;
   }
   bool text = IsText(control);
   if(keyUp) return HandleKeyUp(code,text);
   Keys modifiers = Control.ModifierKeys;
   bool meta = (GetKeyState(0x5B) & 0x8000) != 0 || (GetKeyState(0x5C) & 0x8000) != 0;
   return HandleKeyDown(code,(modifiers & Keys.Shift) != 0,(modifiers & Keys.Control) != 0,
    (modifiers & Keys.Alt) != 0,(bits & (1L << 30)) != 0,text,meta);
  } finally { diagnosticPrefix = previousPrefix; diagnosticSafePrefix = previousSafePrefix; }
 }
 [DllImport("user32.dll")] static extern short GetKeyState(int key);

 // Explicit Windows Set-1 physical positions. No Keys/WParam fallback is permitted.
 internal static string CodeFromScanCode(int scanCode,bool extended) {
  if(extended) {
   switch(scanCode) {
    case 28:return "NumpadEnter"; case 29:return "ControlRight"; case 53:return "NumpadDivide"; case 56:return "AltRight";
    case 71:return "Home"; case 72:return "ArrowUp"; case 73:return "PageUp"; case 75:return "ArrowLeft";
    case 77:return "ArrowRight"; case 79:return "End"; case 80:return "ArrowDown"; case 81:return "PageDown";
    case 82:return "Insert"; case 83:return "Delete"; case 91:return "MetaLeft"; case 92:return "MetaRight"; case 93:return "ContextMenu";
    default:return null;
   }
  }
  if(scanCode >= 2 && scanCode <= 11) return "Digit" + (scanCode == 11 ? 0 : scanCode - 1);
  if(scanCode >= 16 && scanCode <= 25) return "Key" + "QWERTYUIOP"[scanCode - 16];
  if(scanCode >= 30 && scanCode <= 38) return "Key" + "ASDFGHJKL"[scanCode - 30];
  if(scanCode >= 44 && scanCode <= 50) return "Key" + "ZXCVBNM"[scanCode - 44];
  if(scanCode >= 59 && scanCode <= 68) return "F" + (scanCode - 58);
  switch(scanCode) {
   case 1:return "Escape"; case 12:return "Minus"; case 13:return "Equal"; case 14:return "Backspace"; case 15:return "Tab";
   case 26:return "BracketLeft"; case 27:return "BracketRight"; case 28:return "Enter"; case 29:return "ControlLeft";
   case 39:return "Semicolon"; case 40:return "Quote"; case 41:return "Backquote"; case 42:return "ShiftLeft";
   case 43:return "Backslash"; case 51:return "Comma"; case 52:return "Period"; case 53:return "Slash"; case 54:return "ShiftRight";
   case 55:return "NumpadMultiply"; case 56:return "AltLeft"; case 57:return "Space"; case 58:return "CapsLock";
   case 69:return "NumLock"; case 70:return "ScrollLock"; case 71:return "Numpad7"; case 72:return "Numpad8";
   case 73:return "Numpad9"; case 74:return "NumpadSubtract"; case 75:return "Numpad4"; case 76:return "Numpad5";
   case 77:return "Numpad6"; case 78:return "NumpadAdd"; case 79:return "Numpad1"; case 80:return "Numpad2";
   case 81:return "Numpad3"; case 82:return "Numpad0"; case 83:return "NumpadDecimal"; case 87:return "F11"; case 88:return "F12";
   default:return null;
  }
 }
 internal static string PhysicalLabel(string code) {
  if(String.IsNullOrEmpty(code)) return "Unbound";
  if(code.StartsWith("Digit",StringComparison.Ordinal)) return code.Substring(5) + " [physical " + code + "]";
  if(code.StartsWith("Key",StringComparison.Ordinal)) return code.Substring(3) + " [physical " + code + "]";
  return code + " [physical]";
 }
 public void Dispose() {
  if(disposed) return;
  Reset("Keyboard detached."); disposed = true; ime = false;VisualChanged=null;
  if(attached) Application.RemoveMessageFilter(this); attached = false;
  foreach(Form form in windows) { form.Deactivate -= Deactivated; form.FormClosed -= WindowClosed; }
  windows.Clear();
  foreach(var pair in watches) { pair.Key.ControlAdded -= ChildAdded; pair.Value.Dispose(); }
  watches.Clear();
 }
 // Focus and IME messages may be sent directly, bypassing IMessageFilter.
 sealed class InputWatch : NativeWindow,IDisposable {
  readonly ExperimentInput router;
  readonly Control control;
  internal InputWatch(ExperimentInput router,Control control) {
   this.router = router; this.control = control;
   control.HandleCreated += Created; control.HandleDestroyed += Destroyed; control.Disposed += ControlDisposed;
   if(control.IsHandleCreated) AssignHandle(control.Handle);
  }
  void Created(object sender,EventArgs e) { if(Handle == IntPtr.Zero) AssignHandle(control.Handle); }
  void Destroyed(object sender,EventArgs e) { if(Handle != IntPtr.Zero) ReleaseHandle(); }
  void ControlDisposed(object sender,EventArgs e) { router.Unwatch(control); }
  protected override void WndProc(ref Message message) {
   if(message.Msg == 0x10D) router.SetComposition(true);
   else if(message.Msg == 0x10E) router.SetComposition(false);
   else if(message.Msg == 7) {
    if(IsText(control)) router.Reset("Text input owns the keyboard.");
    router.RefreshFocusFeedback(control);
   }
   base.WndProc(ref message);
  }
  public void Dispose() {
   control.HandleCreated -= Created; control.HandleDestroyed -= Destroyed; control.Disposed -= ControlDisposed;
   if(Handle != IntPtr.Zero) ReleaseHandle();
  }
 }
}
