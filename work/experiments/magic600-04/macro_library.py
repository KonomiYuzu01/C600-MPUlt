"""Detached Macro Base metadata, exact-fact facets and model-bound exchange.

No function selects a solving method, changes a Session or certifies imported
claims. The caller supplies locally verified intrinsic facts separately from
library records, and commits an entire accepted import inside its transaction.
"""
from copy import deepcopy
import json
from macro_use import infer_use

FORMAT = 'Magic600-macro-library'
MAX_ENTRIES = 256
MAX_BYTES = 2_000_000
KINDS = {'star', 'pure-position', 'pure-orientation', 'mixed', 'pure-piece-cycle', 'single-three-cycle', 'orientation', 'cross-orbit', 'identity', 'unchecked'}
METADATA = {'name', 'note', 'tags', 'pinned'}


def _text(value, label, limit, empty=False):
    if (not isinstance(value, str) or len(value) > limit
            or (not empty and not value.strip()) or '\x00' in value):
        raise ValueError(label + ' must be ' + ('at most ' if empty else '1..') + str(limit) + ' characters')
    return value


def _tags(value):
    if not isinstance(value, list) or len(value) > 24:
        raise ValueError('Use tags must be a list of at most 24 names')
    result = [_text(tag, 'Use tag', 40) for tag in value]
    if len({tag.casefold() for tag in result}) != len(result):
        raise ValueError('Use tags must be distinct, ignoring case')
    return result


def _metadata(changes):
    if not isinstance(changes, dict) or set(changes) - METADATA:
        raise ValueError('Only name, note, tags and pinned can be edited as metadata')
    result = {}
    for key, value in changes.items():
        if key == 'tags':
            result[key] = _tags(value)
        elif key == 'pinned':
            if type(value) is not bool:
                raise ValueError('Pinned must be a Boolean')
            result[key] = value
        else:
            result[key] = _text(value, 'Macro ' + key, 80 if key == 'name' else 500, key == 'note')
    return result


def edit_metadata(record, changes):
    """Return an independent record; content identity and recipe stay fixed."""
    if not isinstance(record, dict):
        raise ValueError('Macro record must be an object')
    updates = _metadata(changes)
    result = deepcopy(record)
    result.update(updates)
    return result


def facets(record, facts, query):
    """AND explicit browsing conditions, using locally verified facts only.

    `orientation` includes fixed-position and transported orientation changes
    in the certified chart. With affected_orbit, that facet and single-three-cycle are scoped
    to that same orbit. Cross-orbit always describes the complete macro body.
    This is no protection/applicability check or proof of interchangeability.
    """
    if not isinstance(record, dict) or not isinstance(query, dict):
        raise ValueError('Macro record and facet query must be objects')
    if set(query) - {'pinned', 'affected_orbit', 'kind', 'tags', 'use_orbit', 'other'}:
        raise ValueError('Unknown macro facet; use pinned, affected_orbit, kind, tags, use_orbit or other')
    if 'pinned' in query and type(query['pinned']) is not bool:
        raise ValueError('Pin facet must be a Boolean')
    orbit = query.get('affected_orbit')
    if 'affected_orbit' in query and (type(orbit) is not int or not 0 <= orbit < 35):
        raise ValueError('Affected orbit must be a canonical moving orbit in 0..34')
    kind = query.get('kind')
    if 'kind' in query and (not isinstance(kind, str) or kind not in KINDS):
        raise ValueError('Unsupported exact-effect kind')
    if 'use_orbit' in query and (type(query['use_orbit']) is not int or not 0 <= query['use_orbit'] < 35):
        raise ValueError('Likely-use orbit must be a canonical moving orbit in 0..34')
    if 'other' in query and type(query['other']) is not bool:
        raise ValueError('Other-use facet must be a Boolean')
    tags = _tags(query.get('tags', []))
    if facts is not None and not isinstance(facts, dict):
        raise ValueError('Exact facts must be a locally verified effect record or missing')
    if 'pinned' in query and bool(record.get('pinned', False)) != query['pinned']:
        return False
    uses = {tag.casefold() for tag in record.get('tags', [])}
    if not all(tag.casefold() in uses for tag in tags):
        return False
    if 'use_orbit' in query or 'other' in query:
        use = infer_use(facts)
        if use['status'] != 'Checked':
            return False
        if 'use_orbit' in query and not any(row['orbit'] == query['use_orbit'] for row in use['memberships']):
            return False
        if 'other' in query and use['other'] != query['other']:
            return False
    checked = (facts is not None and type(facts.get('slots')) is int
               and isinstance(facts.get('support'), list)
               and isinstance(facts.get('orbit_effects'), list))
    if not checked:
        return orbit is None and kind in (None, 'unchecked')
    support = {row['orbit'] for row in facts['support']}
    if orbit is not None and orbit not in support:
        return False
    scoped = [row for row in facts['orbit_effects'] if orbit is None or row['orbit'] == orbit]
    if kind == 'unchecked':
        return False
    if kind == 'star':
        star = facts.get('star')
        return (any(row.get('status') == 'Verified' and (orbit is None or row['orbit'] == orbit)
                    for row in facts.get('scoped_stars', [])) or isinstance(star, dict)
                and star.get('certificate', {}).get('legal_seed_replayed') is True
                and (orbit is None or star['orbit'] == orbit))
    if kind == 'single-three-cycle':
        return any(row.get('single_three_cycle') is True for row in scoped)
    if kind == 'pure-piece-cycle':
        return any(row.get('pure_cycle', {}).get('status') == 'Verified' for row in scoped)
    if kind in ('pure-position', 'pure-orientation', 'mixed'):
        expected = {'pure-position': 'PurePosition', 'pure-orientation': 'PureOrientation', 'mixed': 'Mixed'}[kind]
        return any(row.get('effect_class') == expected for row in scoped)
    if kind == 'orientation':
        return any((row.get('orientation_count') or 0) > 0 or row.get('known_orientation_count', 0) > 0 for row in scoped)
    if kind == 'cross-orbit':
        return len(support) > 1
    if kind == 'identity':
        return facts['slots'] == 0
    return True


def _size(value):
    try:
        size = len(json.dumps(value, ensure_ascii=False, allow_nan=False, separators=(',', ':')).encode('utf-8'))
    except (TypeError, ValueError, RecursionError, UnicodeError) as error:
        raise ValueError('Library exchange must contain finite JSON data') from error
    if size > MAX_BYTES:
        raise ValueError('Library exchange exceeds 2,000,000 bytes; export a smaller selection')


def _derived(model, value):
    if isinstance(value, dict) and value.get('kind') == 'explicit-endgame':
        if set(value) != {'kind', 'version', 'family', 'parameters', 'roles', 'frame_version'}:
            raise ValueError('Explicit family provenance needs its fixed parameters, roles and versions')
        if value['family'] not in ('placement-star', 'transfer', 'buffer-a', 'final-b'):
            raise ValueError('Unknown explicit endgame family')
        _text(value['version'], 'Construction version', 100)
        _text(value['frame_version'], 'Construction frame version', 100)
        parameters, roles = value['parameters'], value['roles']
        keys = {'x', 'q', 'y', 'r', 'result'} if value['family'] == 'final-b' else {'x', 'q'}
        if (not isinstance(parameters, dict) or set(parameters) != keys or not isinstance(roles, list)
                or len(roles) != 2 or roles[0] == roles[1]
                or any(type(p) is not int or not 0 <= p < model.np for p in roles)):
            raise ValueError('Malformed explicit endgame parameters or buffer positions')
        for key in ('x', 'y'):
            if key not in parameters:
                continue
            p = parameters[key]
            if type(p) is not int or not 0 <= p < model.np or int(model.oid[p]) < 0:
                raise ValueError('An auxiliary must name a canonical moving position')
        for key in ('q', 'r', 'result'):
            if key in parameters:
                element = parameters[key]
                if (not isinstance(element, list) or any(type(n) is not int for n in element)
                        or sorted(element) != list(range(int(model.k[parameters['x']])))):
                    raise ValueError('A construction element must be a complete slot permutation')
        # Provenance is descriptive. Import never grants a frame or action certificate.
        return deepcopy(value)
    if isinstance(value, dict) and value.get('kind') == 'geometry':
        if set(value) != {'id', 'version', 'kind', 'reference', 'proof'}:
            raise ValueError('Geometric provenance needs its source revision and explicit mapping')
        _text(value['id'], 'Source identity', 128)
        if type(value['version']) is not int or value['version'] < 1:
            raise ValueError('Source version must be a positive integer')
        reference = value['reference']
        if not isinstance(reference, dict) or reference.get('model') != model.model_id:
            raise ValueError('Geometric reference belongs to a different model')
        from grip_frames import frames
        for key in ('source_frame', 'destination_frame'):
            frame = reference.get(key)
            if not isinstance(frame, dict) or set(frame) != {'cell', 'ordered_vertices'}:
                raise ValueError('Geometric provenance needs explicit ordered cell frames')
            vertices = frame['ordered_vertices']
            if not isinstance(vertices, list) or len(vertices) != 4 or any(type(v) is not int for v in vertices):
                raise ValueError('A geometric ordered frame needs four canonical integer vertex IDs')
            choices = frames(model, frame['cell'])
            if frame['ordered_vertices'] not in [row['vertices'] for row in choices['frames']]:
                raise ValueError('Geometric provenance has an unsupported proper frame')
        if not isinstance(value['proof'], dict):
            raise ValueError('Geometric proof metadata must be a record; it is not trusted on import')
        return deepcopy(value)
    if not isinstance(value, dict) or set(value) - {'id', 'version', 'kind', 'reference', 'convention'}:
        raise ValueError('Macro source must contain only its identity, version and explicit derivation')
    result = dict(id=_text(value.get('id'), 'Source identity', 128), version=value.get('version'))
    if type(result['version']) is not int or result['version'] < 1:
        raise ValueError('Source version must be a positive integer')
    kind = value.get('kind')
    if 'kind' in value:
        if kind not in ('inverse', 'reference', 'revision', 'copy'):
            raise ValueError('Source derivation must be inverse, reference, revision or copy')
        result['kind'] = kind
    if kind == 'reference':
        word = value.get('reference')
        model.normalize([dict(kind='word', moves=word)])
        if value.get('convention') != 'R^-1 / Macro / R':
            raise ValueError('Reference convention must be R^-1 / Macro / R')
        result.update(reference=deepcopy(word), convention=value['convention'])
    elif 'reference' in value or 'convention' in value:
        raise ValueError('A reference word and convention require reference derivation')
    return result


def _record(model, value):
    if not isinstance(value, dict):
        raise ValueError('Each library entry must be an object')
    identity = _text(value.get('id'), 'Macro identity', 128)
    version = value.get('version')
    if type(version) is not int or version < 1:
        raise ValueError('Macro ' + identity + ' needs a positive content version')
    orbit = value.get('orbit')
    if type(orbit) is not int or not 0 <= orbit < 35:
        raise ValueError('Macro ' + identity + ' saved group must be a moving orbit in 0..34')
    metadata = _metadata(dict(name=value.get('name'), note=value.get('note', ''),
                              tags=value.get('tags', []), pinned=value.get('pinned', False)))
    try:
        normalized, _ = model.normalize(value.get('recipe'))
    except (ValueError, TypeError, KeyError) as error:
        raise ValueError('Macro ' + identity + ' recipe: ' + str(error)) from error
    # Keep the explicit recipe structure, including an originally omitted +1;
    # normalization is for validation/equality, never net-action substitution.
    recipe = deepcopy(normalized)
    for raw, step in zip(value['recipe'], recipe):
        if step['kind'] == 'star' and 'sign' not in raw:
            del step['sign']
    result = dict(id=identity, version=version, orbit=orbit, recipe=recipe, **metadata)
    if 'source' in value:
        result['source'] = _text(value['source'], 'Macro source', 500, True)
    if 'derived_from' in value:
        result['derived_from'] = _derived(model, value['derived_from'])
    return result, normalized


def export_selected(model, library, ids):
    """Export only explicit identities; effect/proof/applicability data is omitted."""
    if not isinstance(library, dict) or not isinstance(ids, list) or not 1 <= len(ids) <= MAX_ENTRIES:
        raise ValueError('Select 1..256 explicit macro identities')
    selected = [_text(value, 'Selected macro identity', 128) for value in ids]
    if len(set(selected)) != len(selected):
        raise ValueError('Selected macro identities must be distinct')
    entries = []
    for identity in selected:
        if identity not in library:
            raise ValueError('Selected macro is missing: ' + identity)
        value, _ = _record(model, library[identity])
        if value['id'] != identity:
            raise ValueError('Library key differs from its canonical macro identity: ' + identity)
        entries.append(value)
    document = dict(format=FORMAT, schema_version=1, model=model.model_id, entries=entries)
    _size(document)
    return document


def inspect_import(model, library, document):
    """Validate the whole batch and return a detached preview, never mutate.

    Caller must reject can_import=False before writing any added records.
    Identical entries retain local metadata. Conflicting content is never
    assigned a new identity implicitly. Imported source is a claim, not proof.
    """
    if not isinstance(library, dict) or not isinstance(document, dict):
        raise ValueError('Library and exchange document must be objects')
    _size(document)
    if document.get('format') != FORMAT or type(document.get('schema_version')) is not int or document['schema_version'] != 1:
        raise ValueError('Expected Magic600-macro-library schema version 1')
    if document.get('model') != model.model_id:
        raise ValueError('Library exchange belongs to a different or missing model identity')
    values = document.get('entries')
    if not isinstance(values, list) or not 1 <= len(values) <= MAX_ENTRIES:
        raise ValueError('Library exchange must contain 1..256 entries')
    records, added, identical, conflict, seen = [], [], [], [], set()
    for value in values:
        record, normalized = _record(model, value)
        identity = record['id']
        if identity in seen:
            raise ValueError('Duplicate macro identity in import batch: ' + identity)
        seen.add(identity)
        if identity not in library:
            added.append(identity)
        else:
            existing, old_recipe = _record(model, library[identity])
            if existing['id'] != identity:
                raise ValueError('Existing library key differs from its canonical identity: ' + identity)
            if existing['version'] != record['version'] or old_recipe != normalized:
                conflict.append(dict(id=identity, reason='Existing content version or exact normalized recipe differs'))
            else:
                identical.append(identity)
                record = existing
        records.append(record)
    return dict(records=records, added=added, identical=identical,
                conflict=conflict, can_import=not conflict)
