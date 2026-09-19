"""Read-only retained cut-cell geometry and revision-bound local observations."""
import base64
import json

import numpy as np

from filter_projection import PieceFilterProjection


def geometry(model):
    """Return immutable original mesh buffers; callers may cache by model identity."""
    root = model.root
    meta = json.loads((root / 'mesh.json').read_text())
    vertices = (root / 'mesh_vertices.f32').read_bytes()
    centers = (root / 'mesh_centers.f32').read_bytes()
    transforms = (root / 'cell_frames.f32').read_bytes()
    regions = np.fromfile(root / 'mesh_sticker.u32', '<u4')
    offsets = meta['offsets']
    if (meta['base_stickers'] != 433 or len(offsets) != 434 or offsets[0] != 0
            or offsets[-1] * 16 != len(vertices) or len(centers) != 433 * 16
            or len(transforms) != 600 * 16 * 4 or len(regions) != offsets[-1]):
        raise ValueError('Retained cell mesh dimensions disagree')
    for region, (start, end) in enumerate(zip(offsets[:-1], offsets[1:])):
        if end <= start or (end - start) % 3 or not np.all(regions[start:end] == region):
            raise ValueError('Retained cell mesh region numbering disagrees')
    return dict(format='Magic600-local-geometry-v1', model=model.model_id,
                base_vertices_f32=base64.b64encode(vertices).decode('ascii'),
                base_centers_f32=base64.b64encode(centers).decode('ascii'),
                cell_frames_f32=base64.b64encode(transforms).decode('ascii'),
                offsets=list(offsets), base_normal=meta['normal'],
                triangles_per_cell=offsets[-1] // 3,
                coordinate_order=['W', 'X', 'Y', 'Z'])


def cell_state(workbench, canonical_cell, ordered_vertices=None):
    """Caller holds the existing Session lock for this and its companion reply."""
    if type(canonical_cell) is not int or not 1 <= canonical_cell <= 600:
        raise ValueError('Local center must be a canonical integer cell in 1..600')
    model, session = workbench.m, workbench.s
    table = workbench.frame_choices(canonical_cell)
    vertices = table['base_vertices'] if ordered_vertices is None else ordered_vertices
    if (not isinstance(vertices, (list, tuple)) or len(vertices) != 4
            or any(type(v) is not int for v in vertices)
            or not any(record['vertices'] == list(vertices) for record in table['frames'])):
        raise ValueError('Local frame must be a proper ordered frame of this center cell')
    slots = np.arange((canonical_cell - 1) * 433, canonical_cell * 433, dtype=np.int32)
    state = session.st
    labels = state.labels[slots]
    positions = model.sp[slots]
    pieces = model.sp[labels]
    projection = PieceFilterProjection(workbench)
    matches, metadata = projection.evaluate(state)
    protected = np.isin(model.oid[positions], session.prefs['protected'])
    locks = {record['position'] for record in workbench.position_locks()}
    protected |= np.isin(positions, list(locks))
    return dict(format='Magic600-local-cell-v1', model=model.model_id,
                state_hash=state.hash, revision=str(session.rev), cell=canonical_cell,
                slots=slots.tolist(), labels=labels.tolist(), positions=positions.tolist(),
                pieces=pieces.tolist(), color_cells=(labels // 433 + 1).tolist(),
                affecting_caps=[[c + 1 for c in model.caps(int(position))] for position in positions],
                filter_match=[None] * 433 if matches is None else matches[positions].tolist(),
                filter=metadata, current=workbench.w['current'],
                next=None if workbench.w['next'] is None else workbench.w['next']['identity'],
                protected=protected.tolist(), frame_vertices=list(vertices),
                frame_positions4=model.vertices4[np.array(vertices) - 1].tolist(),
                scope='Actual committed state; projected original cut regions; no puzzle operation')
