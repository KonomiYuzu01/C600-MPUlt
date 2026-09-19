"""Visible, explicit key sets. Values are command IDs, never solver decisions."""
from copy import deepcopy
import hashlib
import json

LEGACY_COMMON = dict(F1='index', F2='bank', F3='macro-search', F4='operation-focus',
                     F5='filter', F6='local', F7='global', F8='keyboard', F9='bank-previous',
                     Backquote='keyboard-extra')
COMMON = dict(F1='index', F2='bank', F3='macro-search', F4='operation-focus',
              F5='filter', F8='keyboard', F9='bank-previous',
              Backquote='keyboard-extra', Backslash='functions-toggle')

# Keys are explicit: removing a utility leaves its old key empty rather than
# moving every subsequent command. Saved per-bank/user overrides are separate.
SHARED = {
    'Workspace': dict(KeyQ='orbit', KeyW='focus', KeyE='set-current', KeyR='assign-target',
        KeyT='assign-a', KeyY='assign-b', KeyU='roles', KeyI='next-pin', KeyO='next-activate',
        KeyP='next-clear', KeyA='next-locate', KeyS='block-add', KeyD='block-capture',
        KeyF='block-remove', KeyG='block-protect', KeyH='block-unprotect', KeyJ='protection',
        KeyZ='bank-next', KeyX='bank-prev', KeyC='next-menu', KeyV='solve-actions',
        KeyK='block-protect-position', KeyL='block-unprotect-position', KeyB='block-unprotect-exact',
        KeyN='block-reference', KeyM='target-capture', Digit2='inspect-actual',
        Digit3='inspect-prepare', Digit4='inspect-macro', Digit5='inspect-cleanup',
        Digit6='compare-preview', Digit7='block-capture-position'),
    'Filter': dict(KeyQ='filter', KeyW='local-center', KeyE='local-center-current',
        KeyR='protection', KeyT='block-protect', KeyY='block-unprotect', KeyU='prefix-on',
        KeyI='prefix-off', KeyO='next-locate', KeyA='local-center-grip'),
    'Macro': dict(KeyQ='macro-search', KeyW='macro-new', KeyE='macro-details',
        KeyR='macro-insert', KeyT='macro-replace', KeyY='macro-close', KeyU='reference',
        KeyI='reference-transform', KeyO='effect-body', KeyP='effect-complete',
        KeyA='bank-macro-1', KeyS='bank-macro-2', KeyD='phase-input', KeyF='edit-prepare',
        KeyG='edit-macro', KeyH='edit-cleanup', KeyJ='macro-filter', KeyK='macro-label',
        KeyL='macro-pin', KeyC='macro-compare', KeyV='macro-inverse', KeyB='solve-macros',
        KeyN='solve-prepare', KeyM='solve-protection', Digit1='macro-check-library',
        Digit2='solve-locate-a', Digit3='solve-locate-b', Digit4='solve-locate-target',
        Digit5='macro-geometry', Digit6='endgame-choices', Digit7='endgame-x', Digit8='endgame-y',
        Digit9='endgame-q', Digit0='endgame-r', Minus='endgame-check', Equal='endgame-save',
        BracketLeft='endgame-select-saved', BracketRight='endgame-details', Semicolon='macro-candidates'),
    'Operation': dict(KeyQ='phase-prepare', KeyW='phase-macro', KeyE='phase-cleanup',
        KeyR='phase-input', KeyT='cleanup-inverse', KeyY='review', KeyU='review-details',
        KeyI='review-locate', KeyO='preview', KeyP='commit', KeyA='cancel-preview',
        KeyS='cancel-analysis', KeyD='undo', KeyF='redo', KeyG='operation-new',
        KeyH='operation-reuse', KeyJ='operation-hide', KeyK='input-draft', KeyL='input-live',
        KeyZ='prefix-on', KeyX='prefix-off', KeyC='goal-prepare', KeyV='goal-insert',
        KeyB='goal-block', KeyN='goal-endgame', KeyM='solve-macros', Digit1='solve-prepare',
        Digit2='solve-protection', Digit3='solve-locate-a', Digit4='solve-locate-b',
        Digit5='solve-locate-target', Digit6='worksheet-use', Digit7='worksheet',
        Digit8='goal-place', Digit9='goal-orient', Digit0='goal-finish-buffer',
        Minus='target-home', BracketLeft='review-reason-1', BracketRight='review-reason-2',
        Semicolon='review-reason-3', Equal='residual-details', Quote='journal-delta', Slash='orbit-following'),
    'Keyboard': dict(KeyQ='key-edit', KeyW='key-edit-advanced', KeyE='capture',
        KeyR='capture-piece', KeyT='grip-hold', KeyY='grip-latch', KeyU='bank-next',
        KeyI='bank-prev', KeyO='bank-A', KeyP='bank-B', KeyA='bank-I', KeyS='bank-M',
        KeyD='bank-E', KeyF='bank-Workspace', KeyH='bank-Filter', KeyK='bank-Macro',
        KeyL='bank-Operation', KeyZ='bank-Keyboard', KeyX='grip-frame', KeyC='bank-Functions',
        KeyV='bank-Cycles'),
    'Cycles': dict(KeyQ='cycles-macro', KeyW='cycles-steps', KeyE='cycles-complete',
        KeyR='cycles-refresh', KeyA='cycles-next', KeyS='cycles-previous',
        KeyD='cycles-more', KeyF='cycles-back', KeyT='set-current', KeyY='next-pin',
        KeyU='next-locate', KeyI='next-activate', KeyO='focus', KeyP='reference',
        KeyG='local-center', KeyH='grip-frame', KeyJ='protection',
        KeyZ='cycles-current', KeyX='cycles-operation', KeyC='cycles-after'),
}
LEGACY = {
    'Views': dict(KeyQ='local-center', KeyW='local-center-current', KeyE='local-reset',
        KeyR='local-left', KeyT='local-right', KeyY='local-up', KeyU='local-down',
        KeyI='local-zoom-in', KeyO='local-zoom-out', KeyP='global-reset', KeyA='global-left',
        KeyS='global-right', KeyD='global-up', KeyF='global-down', KeyG='global-zoom-in',
        KeyH='global-zoom-out', KeyJ='puzzle', KeyK='compare-preview', KeyL='inspect-actual',
        KeyZ='inspect-prepare', KeyX='inspect-macro', KeyC='inspect-cleanup',
        KeyV='windows-hide', KeyB='views-close', KeyN='local-center-grip'),
    'Session': dict(KeyQ='checkpoint', KeyW='restore', KeyE='undo', KeyR='redo', KeyT='reset',
        KeyY='worksheet', KeyU='worksheet-save', KeyI='worksheet-use',
        KeyO='operation-new', KeyP='operation-reuse', KeyA='fixture-e1'),
}
FUNCTIONS = {
    'Control+KeyD': 'display', 'Control+Shift+KeyH': 'frame-toggle',
    'Control+Shift+KeyD': 'detail-toggle',
    'F6': 'local', 'F7': 'global', 'Control+KeyP': 'puzzle', 'Control+KeyV': 'views',
    'Control+KeyW': 'windows-hide', 'Control+Shift+KeyW': 'views-close',
    'Control+KeyF': 'workspace-fullscreen', 'Shift+F1': 'help',
    'Control+KeyC': 'copy-selection', 'Control+Shift+KeyC': 'copy-selection-canonical', 'Control+KeyS': 'checkpoint',
    'Control+KeyO': 'restore', 'Control+Shift+KeyR': 'reset',
    'Control+Shift+KeyE': 'fixture-e1', 'Control+Shift+KeyS': 'worksheet-save',
    'Control+KeyE': 'macro-export', 'Control+KeyI': 'macro-import',
    'Control+KeyL': 'local-reset', 'Control+ArrowLeft': 'local-left',
    'Control+ArrowRight': 'local-right', 'Control+ArrowUp': 'local-up',
    'Control+ArrowDown': 'local-down', 'Control+Equal': 'local-zoom-in',
    'Control+Minus': 'local-zoom-out', 'Alt+KeyG': 'global-reset',
    'Alt+ArrowLeft': 'global-left', 'Alt+ArrowRight': 'global-right',
    'Alt+ArrowUp': 'global-up', 'Alt+ArrowDown': 'global-down',
    'Alt+Equal': 'global-zoom-in', 'Alt+Minus': 'global-zoom-out',
    'Control+Shift+KeyV': 'bank-Views', 'Control+Shift+KeyO': 'bank-Session',
    'Control+KeyJ': 'session', 'Control+KeyN': 'session-new',
    'Control+KeyR': 'session-resume', 'Control+Shift+KeyJ': 'session-report',
    'Control+KeyG': 'scramble', 'Control+KeyT': 'session-timer-start',
    'Control+Shift+KeyT': 'session-timer-pause', 'Control+Shift+KeyL': 'session-save-log',
    'Control+Shift+KeyF': 'reset-view', 'Control+Alt+KeyR': 'reset-workspace',
    'Control+Alt+KeyS': 'session-summary',
    'Control+Alt+KeyI': 'keymap-import', 'Control+Alt+KeyE': 'keymap-export',
    'Control+Alt+KeyO': 'session-import-log', 'Control+Alt+KeyL': 'session-export-log',
}
FUNCTIONS_ONLY = frozenset(FUNCTIONS.values())
PHYSICAL = {
    'A': dict(KeyZ='phase-prepare', KeyX='input-draft', KeyC='local-center', KeyV='grip-frame',
        KeyB='capture-piece', KeyN='roles', KeyM='reference', Comma='review', Period='preview',
        Slash='commit', F10='cancel-preview', F11='undo', F12='bank-macro-1'),
    'B': dict(KeyZ='phase-prepare', KeyX='input-draft', KeyC='local-center', KeyV='grip-frame',
        KeyB='capture-piece', KeyN='roles', KeyM='reference', Comma='review', Period='preview',
        Slash='commit', F10='cancel-preview', F11='undo', F12='bank-macro-1'),
    'I': dict(KeyZ='phase-prepare', KeyX='phase-macro', KeyC='phase-cleanup', KeyV='macro-insert',
        KeyB='review', KeyN='preview', KeyM='commit', Comma='cancel-preview', Period='next-activate',
        Slash='block-protect', F10='cleanup-inverse', F11='undo', F12='operation-new'),
    'M': dict(KeyZ='phase-prepare', KeyX='phase-macro', KeyC='phase-cleanup', KeyV='macro-insert',
        KeyB='macro-new', KeyN='reference', KeyM='reference-transform', Comma='review',
        Period='preview', Slash='commit', F10='cancel-preview', F11='undo', F12='macro-details'),
    'E': dict(KeyZ='phase-prepare', KeyX='phase-macro', KeyC='phase-cleanup', KeyV='macro-insert',
        KeyB='review', KeyN='preview', KeyM='commit', Comma='cancel-preview', Period='roles',
        Slash='reference', F10='cleanup-inverse', F11='undo', F12='review-details'),
}


def commands(kind):
    result = dict(LEGACY_COMMON if kind in LEGACY else COMMON)
    bindings = LEGACY[kind] if kind in LEGACY else FUNCTIONS if kind == 'Functions' else SHARED[kind] if kind in SHARED else PHYSICAL[kind]
    result.update(bindings)
    return result


def shared_banks():
    return [dict(id=name, orbit=None, name=name, purpose=('Legacy ' if name in LEGACY else '') + name + ' commands',
                 buffers=[], caps=[], slots=[], frames={}, macros=[], needs_capture=False,
                 turns_enabled=False, commands=commands(name),
                 group='legacy' if name in LEGACY else 'functions' if name == 'Functions' else 'core',
                 legacy=name in LEGACY, default_cycle=name not in LEGACY,
                 functions_only=sorted(FUNCTIONS_ONLY) if name == 'Functions' else [],
                 frame='No physical turns in this set',
                 requirements='Explicit command set; switching does not change the working orbit')
            for name in list(SHARED) + ['Functions'] + list(LEGACY)]


KEYMAP_FORMAT = 'Magic600-keymap'
KEYMAP_VERSION = 1
KEYMAP_LIMIT = 2000000
_GRIPS = ['Digit' + str(n) for n in range(1, 10)] + ['Digit0'] + ['Key' + c for c in 'QWERTYUIOP']
_TWISTS = dict(zip(['Key' + c for c in 'ASDFGHJKL'] + ['Semicolon', 'Quote'],
                   ['H1', 'H2', 'H3', 'T1', 'T2', 'T3', 'T4', 'T1-', 'T2-', 'T3-', 'T4-']))
_CODES = (set('Escape Minus Equal Backspace Tab BracketLeft BracketRight Enter Semicolon Quote '
              'Backquote Backslash Comma Period Slash Space CapsLock NumLock ScrollLock '
              'Home End PageUp PageDown Insert Delete ArrowUp ArrowDown ArrowLeft ArrowRight '
              'NumpadEnter NumpadDivide NumpadMultiply NumpadSubtract NumpadAdd NumpadDecimal '
              'ContextMenu'.split())
          | {'Key' + c for c in 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'}
          | {'Digit' + str(n) for n in range(10)} | {'Numpad' + str(n) for n in range(10)}
          | {'F' + str(n) for n in range(1, 13)})


def keymap_command_ids():
    """Catalog authority; the native registry coverage test detects omissions."""
    return {value for kind in [*SHARED, *LEGACY, 'Functions', *PHYSICAL]
            for value in commands(kind).values()}


def _shortcut(value, physical=False):
    if not isinstance(value, str):
        raise ValueError('A shortcut must be a physical key name')
    parts = value.split('+')
    parts = ['Control' if p == 'Ctrl' else p for p in parts]
    modifiers, code = parts[:-1], parts[-1]
    if (code not in _CODES or any(p not in ('Control', 'Alt', 'Shift') for p in modifiers)
            or modifiers != [p for p in ('Control', 'Alt', 'Shift') if p in modifiers]
            or physical and modifiers):
        raise ValueError('Unsupported or repeated shortcut: ' + value)
    return '+'.join(parts)


def _merged(initial, *overrides):
    result = dict(initial)
    for override in overrides:
        for key, value in override.items():
            key = _shortcut(key)
            if value is None:
                result.pop(key, None)
            else:
                result[key] = value
    return result


def _checked_bindings(bindings, allowed_command_ids, bank_ids):
    if not isinstance(bindings, dict):
        raise ValueError('Bindings must be an object')
    if not isinstance(bank_ids, dict) or any(type(n) is not int or not 0 <= n <= 600 for n in bank_ids.values()):
        raise ValueError('Current bank slot counts are required for exact key conflict checks')
    allowed = set(allowed_command_ids)

    def scope(group, label, root=False):
        permitted = {'grips', 'twists', 'commands'} | ({'banks'} if root else set())
        if not isinstance(group, dict) or set(group) - permitted:
            raise ValueError(label + ' contains unsupported saved data; no keys were omitted or reinterpreted')
        grips = group.get('grips')
        if grips is None:
            grips = []
        if not isinstance(grips, list):
            raise ValueError(label + ' grips must be a list')
        checked = [_shortcut(code, physical=True) for code in grips]
        if len(set(checked)) != len(checked):
            raise ValueError(label + ' repeats a Grip key')
        for section in ('twists', 'commands'):
            mapping = group.get(section)
            if mapping is None:
                mapping = {}
            if not isinstance(mapping, dict):
                raise ValueError(label + ' ' + section + ' must be an object')
            seen = set()
            for key, value in mapping.items():
                normalized = _shortcut(key, physical=section == 'twists')
                if normalized in seen:
                    raise ValueError(label + ' repeats shortcut ' + normalized)
                seen.add(normalized)
                if value is None:
                    continue
                valid = set(_TWISTS.values()) | {'H1-', 'H2-', 'H3-'} if section == 'twists' else allowed
                if not isinstance(value, str) or value not in valid:
                    raise ValueError(label + ' has an unknown ' + section + ' action: ' + repr(value))
                if section == 'commands':
                    parts = normalized.split('+')
                    if parts[-1] == 'Enter' or normalized in ('Escape', 'Tab', 'Shift+Tab') or ('Alt' in parts and parts[-1] in ('Tab', 'F4')):
                        raise ValueError('Shortcut belongs to native navigation: ' + normalized)
    scope(bindings, 'Shared bindings', True)
    banks = bindings.get('banks')
    if banks is None:
        banks = {}
    if not isinstance(banks, dict) or set(banks) - set(bank_ids):
        raise ValueError('Bank overrides name an unknown bank or are not an object')
    for bank, group in banks.items():
        scope(group, bank)
    for bank, count in bank_ids.items():
        kind = bank.rsplit('-', 1)[-1]
        if not count and kind not in PHYSICAL:
            continue
        local = banks.get(bank, {})
        grips = (local.get('grips') or bindings.get('grips') or _GRIPS)[:count]
        twists = _merged(_TWISTS, bindings.get('twists') or {}, local.get('twists') or {})
        shared_commands, local_commands = bindings.get('commands') or {}, local.get('commands') or {}
        if kind not in PHYSICAL:
            raise ValueError('Unknown physical bank kind: ' + bank)
        command_map = _merged(commands(kind), shared_commands, local_commands)
        for key in ('Backquote', 'Backslash'):
            if (key in grips or key in twists) and key not in shared_commands and key not in local_commands:
                command_map.pop(key, None)  # Same existing convenience precedence as InputState.
        overlap = set(grips) & set(twists)
        if overlap:
            raise ValueError(bank + ' uses a key for both Grip and Twist: ' + sorted(overlap)[0])
        physical = set(grips) | set(twists)
        for key in command_map:
            if key in physical or key.startswith('Shift+') and key[6:] in physical:
                raise ValueError(bank + ' command conflicts with Grip/Twist: ' + key)
    return deepcopy(bindings)


def _keymap_basis(bindings):
    return hashlib.sha256(json.dumps(bindings, ensure_ascii=False, sort_keys=True,
                                    separators=(',', ':'), allow_nan=False).encode('utf-8')).hexdigest()


def export_keymap(model_id, bindings, allowed_command_ids, bank_ids):
    if not isinstance(model_id, str) or not model_id:
        raise ValueError('An exact model identity is required')
    checked = _checked_bindings(bindings, allowed_command_ids, bank_ids)
    document = dict(format=KEYMAP_FORMAT, schema_version=KEYMAP_VERSION, model=model_id,
                    scope='keybindings-only', bindings=checked)
    if len(json.dumps(document, ensure_ascii=False).encode('utf-8')) > KEYMAP_LIMIT:
        raise ValueError('The keymap exceeds the 2,000,000-byte file limit')
    return document


def inspect_keymap(text, model_id, current_bindings, allowed_command_ids, bank_ids):
    if not isinstance(text, str) or len(text.encode('utf-8')) > KEYMAP_LIMIT:
        raise ValueError('Choose a UTF-8 keymap file no larger than 2,000,000 bytes')
    def unique(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError('Duplicate JSON property: ' + key)
            result[key] = value
        return result
    try:
        document = json.loads(text, object_pairs_hook=unique,
                              parse_constant=lambda value: (_ for _ in ()).throw(ValueError('Non-finite JSON value')))
    except (TypeError, RecursionError, UnicodeError) as error:
        raise ValueError('Invalid keymap JSON: ' + str(error)) from error
    fields = {'format', 'schema_version', 'model', 'scope', 'bindings'}
    if not isinstance(document, dict) or set(document) != fields:
        raise ValueError('Expected a versioned keybinding-only file')
    if document['format'] != KEYMAP_FORMAT or type(document['schema_version']) is not int or document['schema_version'] != KEYMAP_VERSION:
        raise ValueError('Unsupported keymap format or schema version')
    if document['model'] != model_id or document['scope'] != 'keybindings-only':
        raise ValueError('Keymap model or scope does not match this workspace')
    bindings = _checked_bindings(document['bindings'], allowed_command_ids, bank_ids)
    scopes = [bindings] + list((bindings.get('banks') or {}).values())
    summary = dict(bank_overrides=len(bindings.get('banks') or {}),
        grip_keys=sum(len(s.get('grips') or []) for s in scopes),
        twist_bindings=sum(len(s.get('twists') or {}) for s in scopes),
        command_bindings=sum(len(s.get('commands') or {}) for s in scopes),
        unbindings=sum(v is None for s in scopes for section in ('commands', 'twists') for v in (s.get(section) or {}).values()))
    digest = hashlib.sha256(text.encode('utf-8')).hexdigest()
    return dict(bindings=bindings, summary=summary,
                basis=_keymap_basis(dict(model=model_id, current=current_bindings, file_sha256=digest)),
                file_sha256=digest)


def import_keymap(text, basis, model_id, current_bindings, allowed_command_ids, bank_ids):
    result = inspect_keymap(text, model_id, current_bindings, allowed_command_ids, bank_ids)
    if not isinstance(basis, str) or basis != result['basis']:
        raise ValueError('Saved keybindings changed after inspection; reopen Import before replacing them')
    return result['bindings']
