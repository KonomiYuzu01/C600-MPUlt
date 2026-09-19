// Experimental input only. This router never authorizes a Session commit.
const AXES = new Set(['H1', 'H2', 'H3', 'T1', 'T2', 'T3', 'T4']);
const TEXT_NAVIGATION = new Set(['bank', 'index']);

function isTextTarget(event) {
  const path = event.composedPath?.() || [event.target];
  return path.some(target => {
    if (!target) return false;
    const tag = target.tagName?.toLowerCase();
    return target.isContentEditable || ['input', 'textarea', 'select'].includes(tag)
      || target.closest?.('[contenteditable="true"], [role="textbox"]');
  });
}

function chord(event, controlName = 'Ctrl') {
  return [event.ctrlKey && controlName, event.altKey && 'Alt', event.metaKey && 'Meta',
    event.shiftKey && 'Shift', event.code].filter(Boolean).join('+');
}

function own(event) {
  event.preventDefault?.();
  event.stopPropagation?.();
  return true;
}

export class KeyboardRouter {
  constructor({getState, onGrip = () => {}, onTwist = () => {},
    onCommand = () => {}, onFeedback = () => {}}) {
    this.getState = getState;
    this.onGrip = onGrip;
    this.onTwist = onTwist;
    this.onCommand = onCommand;
    this.onFeedback = onFeedback;
    this.down = new Set();
    this.blocked = new Set();
    this.heldGrips = new Map();
    this.pointerGrip = null;
    this.latchedGrip = null;
    this.activeGrip = null;
    this.epoch = 0;
    this.ime = false;
    this.pending = false;
    this.context = null;
    this.window = null;
    this.listeners = [];
  }

  attach(window) {
    if (this.window === window) return this;
    if (this.window) this.dispose();
    this.window = window;
    const listen = (target, type, handler) => {
      if (!target?.addEventListener) return;
      target.addEventListener(type, handler, {capture: true});
      this.listeners.push(() => target.removeEventListener(type, handler, {capture: true}));
    };
    listen(window, 'keydown', event => this.handleKeyDown(event));
    listen(window, 'keyup', event => this.handleKeyUp(event));
    listen(window, 'blur', event => {
      if (event.target === window) this.reset('Window lost focus; release held keys.');
    });
    listen(window, 'focusin', event => {
      if (isTextTarget(event)) this.reset('Text input owns the keyboard.');
    });
    listen(window, 'compositionstart', () => {
      this.ime = true;
      this.reset('IME composition owns the keyboard.');
    });
    listen(window, 'compositionend', () => {
      this.ime = false;
      this.reset('IME composition ended; release held keys.');
    });
    listen(window, 'pointercancel', () => this.releasePointerGrip());
    listen(window.document, 'visibilitychange', () => {
      if (window.document.hidden) this.reset('Window hidden; release held keys.');
    });
    return this;
  }

  dispose() {
    for (const remove of this.listeners.splice(0)) remove();
    this.window = null;
    this.ime = false;
    this.reset('Keyboard detached.');
  }

  reset(reason = 'Input context changed; release held keys.') {
    for (const code of this.down) this.blocked.add(code);
    this.heldGrips.clear();
    this.pointerGrip = null;
    this.latchedGrip = null;
    this.epoch += 1;
    this.setActiveGrip(null);
    this.onFeedback(reason);
  }

  state() {
    const state = this.getState();
    // Busy rejects new turns without remapping a deliberately held grip.
    // The caller resets explicitly when busy work changes the operation context.
    const context = JSON.stringify([state.bankId, state.gripMode, state.destination,
      state.phase, state.enabled, !!state.modal, !!state.capture, !!state.textEditing,
      state.gripKeys, state.twistKeys, state.commandKeys]);
    if (this.context !== null && this.context !== context) {
      this.reset('Bank or input context changed; release held keys.');
    }
    this.context = context;
    return state;
  }

  setActiveGrip(cell) {
    if (cell === this.activeGrip) return;
    this.activeGrip = cell;
    this.onGrip(cell);
  }

  updateGrip(state) {
    if (this.heldGrips.size > 1 || (this.pointerGrip !== null && this.heldGrips.size)) {
      this.setActiveGrip(null);
      return;
    }
    const held = this.heldGrips.values().next().value;
    this.setActiveGrip(this.pointerGrip ?? (state.gripMode === 'latch'
      ? this.latchedGrip : held ?? null));
  }

  unavailable(state) {
    if (!state.enabled) return 'Keyboard input is disabled.';
    if (state.capture) return 'Key capture owns the keyboard.';
    if (state.modal) return 'Close the modal before turning.';
    if (this.ime || state.ime) return 'IME composition owns the keyboard.';
    if (state.textEditing) return 'Text input owns the keyboard.';
    if (state.busy || this.pending) return 'Operation in progress; no turn was queued.';
    return '';
  }

  setPointerGrip(cell) {
    const state = this.state();
    const reason = this.unavailable(state);
    if (reason) { this.onFeedback(reason); return false; }
    if (!Number.isInteger(cell) || cell < 1 || cell > 600) {
      this.onFeedback('Choose an explicit cap C1–C600.');
      return false;
    }
    this.reset('Onscreen grip selected; held physical keys require release.');
    this.pointerGrip = cell;
    this.setActiveGrip(cell);
    this.onFeedback(`Onscreen grip C${cell}; ${state.destination} / ${state.phase}.`);
    return true;
  }

  releasePointerGrip() {
    this.pointerGrip = null;
    this.updateGrip(this.state());
  }

  twist(axis, inverse = false) {
    const state = this.state();
    const reason = this.unavailable(state);
    if (reason) { this.onFeedback(reason); return false; }
    if (!AXES.has(axis)) { this.onFeedback('Choose a supported H/T axis.'); return false; }
    if (this.activeGrip === null) {
      this.onFeedback('Select one explicit grip before a Twist.');
      return false;
    }
    const action = {cell: this.activeGrip, axis, inverse: !!inverse,
      destination: state.destination, phase: state.phase};
    try {
      const result = this.onTwist(action);
      if (result && typeof result.then === 'function') {
        this.pending = true;
        Promise.resolve(result).catch(error => this.onFeedback(`Turn failed: ${error.message || error}`))
          .finally(() => { this.pending = false; });
      }
      this.onFeedback(`C${action.cell} ${axis}${inverse ? ' inverse' : ''} → ${state.destination} / ${state.phase}.`);
      return true;
    } catch (error) {
      this.onFeedback(`Turn failed: ${error.message || error}`);
      return false;
    }
  }

  handleKeyDown(event) {
    const state = this.state();
    const code = event.code;
    if (!code) return false;
    const alreadyDown = this.down.has(code);
    this.down.add(code);
    const command = state.commandKeys?.[chord(event)]
      ?? state.commandKeys?.[chord(event, 'Control')];
    const text = isTextTarget(event) || state.textEditing;
    const composing = event.isComposing || event.keyCode === 229 || this.ime || state.ime;
    const grip = state.gripKeys?.[code];
    const axis = state.twistKeys?.[code];
    // Never provide global execution shortcuts, even if accidentally configured.
    const scopedEnter = code === 'Enter' && (event.ctrlKey || event.metaKey);
    if (state.capture || state.modal || composing || scopedEnter) {
      if (this.activeGrip !== null) this.reset('Focused input owns the keyboard.');
      this.blocked.add(code);
      return false;
    }
    const textNavigation = text && /^F[12]$/.test(code) && !event.ctrlKey
      && !event.altKey && !event.metaKey && !event.shiftKey && TEXT_NAVIGATION.has(command);
    if (text && !textNavigation) {
      if (this.activeGrip !== null) this.reset('Text input owns the keyboard.');
      this.blocked.add(code);
      return false;
    }
    if (this.blocked.has(code)) {
      if (command || grip !== undefined || axis) {
        this.onFeedback('Release this key before using it in the new context.');
        return own(event);
      }
      return false;
    }
    if (command) {
      if (event.repeat || alreadyDown) return own(event);
      if (!state.enabled) return false;
      this.onCommand(command);
      return own(event);
    }
    // Modified text shortcuts must never become an unmodified physical turn.
    if (event.ctrlKey || event.altKey || event.metaKey) return false;
    if (grip !== undefined && !event.shiftKey) {
      if (event.repeat || alreadyDown) return own(event);
      const reason = this.unavailable(state);
      if (reason) { this.blocked.add(code); this.onFeedback(reason); return own(event); }
      if (!Number.isInteger(grip) || grip < 1 || grip > 600) {
        this.onFeedback('This Grip needs an explicit C1–C600 capture.');
        return own(event);
      }
      this.heldGrips.set(code, grip);
      if (this.heldGrips.size > 1 || this.pointerGrip !== null) {
        this.latchedGrip = null;
        this.setActiveGrip(null);
        this.onFeedback('Multiple grips are held; release them and choose one.');
        return own(event);
      }
      if (state.gripMode === 'latch') this.latchedGrip = this.latchedGrip === grip ? null : grip;
      this.updateGrip(state);
      this.onFeedback(this.activeGrip === null ? 'Grip released.' : `Grip C${grip}; ${state.gripMode}.`);
      return own(event);
    }
    if (axis) {
      if (event.repeat || alreadyDown) return own(event);
      this.twist(axis, !!event.shiftKey);
      return own(event);
    }
    return false;
  }

  handleKeyUp(event) {
    const state = this.state();
    const code = event.code;
    const owned = this.heldGrips.has(code) || this.blocked.has(code);
    this.down.delete(code);
    this.blocked.delete(code);
    this.heldGrips.delete(code);
    this.updateGrip(state);
    return owned && !isTextTarget(event) ? own(event) : false;
  }
}
