"""Detached fixed-method records; no binding, mechanical review or execution."""
from copy import deepcopy

PHASES = ('prepare', 'macro', 'cleanup')
INPUTS = (
    ('current', 'Current piece'), ('target', 'Destination position'),
    ('roles', 'Buffer A / B positions'), ('block', 'Working block'),
    ('target_requirement', 'Destination frame requirement'),
    ('prefix', 'Check every turn'), ('protection', 'Protected orbits'),
    ('position_locks', 'Captured position locks'),
)


def _same(a, b):
    """Compare content, including scalar types; hashes never prove equality."""
    if type(a) is not type(b):
        return False
    if isinstance(a, dict):
        return a.keys() == b.keys() and all(_same(a[k], b[k]) for k in a)
    if isinstance(a, list):
        return len(a) == len(b) and all(_same(x, y) for x, y in zip(a, b))
    return a == b


def _phases(value, label):
    if not isinstance(value, dict) or set(value) != set(PHASES):
        raise ValueError(label + ' must contain exactly Prepare, Macro and Cleanup')
    for phase in PHASES:
        if not isinstance(value[phase], list) or any(not isinstance(x, dict) for x in value[phase]):
            raise ValueError(label + ' ' + phase + ' must be a list of explicit recipe records')


def _validate(sheet, model_id):
    if not isinstance(sheet, dict):
        raise ValueError('Work sheet must be an object')
    if not isinstance(model_id, str) or not model_id:
        raise ValueError('A model identity is required')
    version = sheet.get('schema_version', 1)
    if type(version) is not int or version not in (1, 2):
        raise ValueError('Unsupported work sheet schema version')
    if 'model' in sheet and sheet['model'] != model_id:
        raise ValueError('Work sheet belongs to a different model')
    if version == 2 and sheet.get('model') != model_id:
        raise ValueError('Work sheet is missing its model identity')
    _phases(sheet.get('draft'), 'Fixed steps')
    if not isinstance(sheet.get('reference'), list) or any(type(x) is not int for x in sheet['reference']):
        raise ValueError('Fixed reference must be an explicit primitive word')
    orbit = sheet.get('orbit')
    if type(orbit) is not int or not 0 <= orbit < 35:
        raise ValueError('Work sheet orbit must be a moving orbit in 0..34')
    if version == 1:
        return True
    if sheet.get('goal') not in ('prepare', 'insert', 'place', 'orient', 'finish-buffer', 'block', 'endgame'):
        raise ValueError('Work sheet needs an explicit work goal')
    inputs = sheet.get('saved_inputs')
    keys = {k for k, _ in INPUTS}
    if not isinstance(inputs, dict) or set(inputs) not in (keys, keys - {'position_locks'}):
        raise ValueError('Work sheet saved inputs are incomplete')
    for key in ('current', 'target'):
        if inputs[key] is not None and (type(inputs[key]) is not int or inputs[key] < 0):
            raise ValueError(key + ' must be a canonical nonnegative identity or position, or missing')
    roles = inputs['roles']
    if not isinstance(roles, list) or len(roles) != 2 or any(type(p) is not int or p < 0 for p in roles) or roles[0] == roles[1]:
        raise ValueError('Saved buffers must be two distinct canonical positions')
    if not isinstance(inputs['block'], dict):
        raise ValueError('Saved block must be an explicit block record')
    for key in ('members', 'protected'):
        rows = inputs['block'].get(key, [])
        if not isinstance(rows, list) or any(not isinstance(row, dict) or
                row.get('mode', 'exact') not in ('exact', 'position') for row in rows):
            raise ValueError('Saved block ' + key + ' needs exact or position requirements')
    if inputs['target_requirement'] is not None and not isinstance(inputs['target_requirement'], dict):
        raise ValueError('Saved destination frame requirement must be a record or missing')
    if type(inputs['prefix']) is not bool:
        raise ValueError('Saved every-turn policy must be a Boolean')
    protection = inputs['protection']
    if protection is not None and (not isinstance(protection, list) or any(type(o) is not int or not 0 <= o < 35 for o in protection)):
        raise ValueError('Saved protection must list moving orbits, or be unknown')
    locks = inputs.get('position_locks')
    if locks is not None:
        if not isinstance(locks, list):
            raise ValueError('Saved position locks must be a list, or be unknown')
        for lock in locks:
            if isinstance(lock, dict) and lock.get('mode', 'exact') not in ('exact', 'position'):
                raise ValueError('Saved position lock mode must be exact or position')
            if (not isinstance(lock, dict) or type(lock.get('position')) is not int or lock['position'] < 0
                    or not isinstance(lock.get('labels'), list) or not lock['labels']
                    or any(type(label) is not int or label < 0 for label in lock['labels'])):
                raise ValueError('Saved position lock needs a canonical position and exact sticker labels')
    if sheet.get('required_inputs') != [dict(key=k, label=label) for k, label in INPUTS if k in inputs]:
        raise ValueError('Work sheet input labels do not match the supported binding fields')
    sources = sheet.get('macro_sources')
    _phases(sources, 'Macro sources')
    for phase in PHASES:
        occupied = []
        for source in sources[phase]:
            if not isinstance(source.get('id'), str) or not source['id']:
                raise ValueError('Macro source needs its library identity')
            if type(source.get('version')) is not int or source['version'] < 1:
                raise ValueError('Macro source needs its positive content revision')
            start, count = source.get('start'), source.get('count')
            if type(start) is not int or type(count) is not int or start < 0 or count < 1 or start + count > len(sheet['draft'][phase]):
                raise ValueError('Macro source range is outside the fixed ' + phase + ' steps')
            if not _same(source.get('recipe'), sheet['draft'][phase][start:start + count]):
                raise ValueError('Macro source recipe differs from its fixed ' + phase + ' steps')
            if any(start < end and first < start + count for first, end in occupied):
                raise ValueError('Macro source ranges overlap in ' + phase)
            occupied.append((start, start + count))
    return False


def build_sheet(workspace, model_id):
    """Capture only the explicit method and comparison inputs, never authority."""
    if not isinstance(workspace, dict):
        raise ValueError('Workspace must be an object')
    if workspace.get('model', model_id) != model_id:
        raise ValueError('Workspace belongs to a different model')
    saved = {k: deepcopy(workspace.get(k)) for k, _ in INPUTS if k != 'protection'}
    saved['protection'] = deepcopy(workspace.get('protected_orbits'))
    sheet = dict(schema_version=2, model=model_id,
                 draft=deepcopy(workspace.get('draft')),
                 reference=deepcopy(workspace.get('reference')),
                 goal=workspace.get('goal'), orbit=workspace.get('orbit'),
                 macro_sources=deepcopy(workspace.get('draft_sources', {p: [] for p in PHASES})),
                 saved_inputs=saved,
                 required_inputs=[dict(key=k, label=label) for k, label in INPUTS])
    _validate(sheet, model_id)
    return sheet


def inspect_sheet(sheet, workspace, library, model_id):
    """Compare saved intent with live inputs; valid does not mean safe to execute.

    Legacy fixed recipes remain usable inside the enclosing model-bound
    workspace. Missing historical provenance or policy is reported as unknown.
    Legal recipe normalization and full-state protection stay in the adapter.
    """
    legacy = _validate(sheet, model_id)
    if not isinstance(workspace, dict) or not isinstance(library, dict):
        raise ValueError('Current workspace and macro library must be objects')
    if workspace.get('model', model_id) != model_id:
        raise ValueError('Current workspace belongs to a different model')
    saved = sheet.get('saved_inputs', {}) if not legacy else {}
    rows = []

    def row(key, label, old, current, known=True):
        status = 'unknown' if not known else 'same' if _same(old, current) else 'missing' if current is None else 'changed'
        rows.append(dict(key=key, label=label, saved=deepcopy(old), current=deepcopy(current), status=status))

    row('orbit', 'Method orbit', sheet['orbit'], workspace.get('orbit'))
    row('reference', 'Fixed reference', sheet['reference'], workspace.get('reference'))
    row('goal', 'Fixed work goal', sheet.get('goal'), workspace.get('goal'), not legacy)
    for key, label in INPUTS:
        current = workspace.get('protected_orbits' if key == 'protection' else key)
        known = key in saved and not (key in ('protection', 'position_locks') and saved[key] is None)
        row(key, label, saved.get(key), current, known)
    sources = []
    for phase in PHASES:
        for source in sheet.get('macro_sources', {}).get(phase, []) if not legacy else []:
            macro = library.get(source['id'])
            if macro is None:
                status, reason = 'missing', 'Library entry is absent; the saved fixed recipe is retained.'
            elif not isinstance(macro, dict) or not _same(macro.get('version'), source['version']) or not _same(macro.get('recipe'), source['recipe']):
                status, reason = 'changed', 'Library revision or exact recipe differs; the saved fixed recipe is retained.'
            else:
                status, reason = 'match', 'Library content revision and complete recipe match exactly.'
            sources.append(dict(deepcopy(source), phase=phase, status=status, reason=reason))
    note = ('Legacy fixed steps: saved input bindings, macro revisions and protection are unknown.' if legacy else
            'Compare and explicitly confirm current inputs. Fixed steps are not retargeted; fresh review is required.')
    return dict(valid=True, legacy=legacy, schema_version=1 if legacy else 2,
                note=note, orbit_match=_same(sheet['orbit'], workspace.get('orbit')),
                rows=rows, macro_sources=sources)
