"""Mathematical display addresses over the retained immutable model.

Canonical IDs still resolve every operation. Rounded centroid coordinates are
location hints, never new model identities or evidence of orientation.
"""
from copy import deepcopy

import numpy as np

from grip_frames import frames

NAMING_VERSION = 'incidence-address-v1'


class MathematicalNames:
    def __init__(self, model):
        self.m = model
        phi = (1 + np.sqrt(5.0)) / 2
        positive = [(0.0, '0'), (phi - 1, 'φ−1'), (1.0, '1'), (phi, 'φ'),
                    (phi * phi, 'φ²'), (2 * phi, '2φ'), (phi + 2, 'φ+2'),
                    (phi ** 3, 'φ³')]
        values = positive + [(-value, '−(' + name + ')' if '+' in name or '−' in name
                              else '−' + name) for value, name in positive if value]
        numbers = np.asarray([value for value, _ in values])
        nearest = np.argmin(abs(model.normals[:, :, None] - numbers), axis=2)
        error = float(np.max(abs(model.normals - numbers[nearest])))
        if error > 1e-10 or len(np.unique(nearest, axis=0)) != 600:
            raise ValueError('Retained cell poles do not have unique supported golden coordinates')
        self._poles = [tuple(values[int(index)][1] for index in row) for row in nearest]
        self._pole_error = error
        self._cells = [{'canonical_id': c + 1, 'name': 'Cell ' + self._tuple(pole),
                        'pole': list(pole), 'basis': ['W', 'X', 'Y', 'Z'],
                        'coordinate_note': 'Golden-coordinate address of the retained model pole; '
                                           'floating source reconstruction checked within 1e-10'}
                       for c, pole in enumerate(self._poles)]
        # The same explicit H/T-anchored corner order is used by Grip frames.
        self._base_vertices = frames(model, 1)['base_vertices']
        vertex_lookup = {frozenset(cells): index + 1
                         for index, cells in enumerate(model.vertex_cells)}
        self._cell_frame_vertices = [[vertex_lookup[frozenset(map(int, permutation[
            model.vertex_cells[vertex - 1]]))] for vertex in self._base_vertices]
            for permutation in model.z['frameperms']]
        vertices = model.vertices4[np.asarray(self._base_vertices) - 1]
        centers = model.z['slot_centers'][:433]
        matrix = np.vstack((vertices.T, np.ones(4)))
        self._barycentric = np.linalg.lstsq(
            matrix, np.vstack((centers.T, np.ones(433))), rcond=None)[0].T
        if np.max(abs(self._barycentric @ vertices - centers)) > 1e-9:
            raise ValueError('Sticker centroids do not reconstruct in the retained tetrahedron')
        if len(np.unique(np.round(self._barycentric, 3), axis=0)) != 433:
            raise ValueError('Three-decimal region hints do not distinguish the 433 base regions')
        self._inverse_frames = np.argsort(model.z['frameperms'], axis=1)
        identity = np.arange(600, dtype=np.int16)
        rotations, known = [identity], {tuple(identity)}
        for rotation in rotations:
            for generator in model.z['rotperms'][:2]:
                candidate = generator[rotation]
                key = tuple(candidate)
                if key not in known:
                    known.add(key)
                    rotations.append(candidate)
            if len(rotations) > 12:
                raise ValueError('Retained cap stabilizer exceeds twelve proper rotations')
        if len(rotations) != 12 or any(rotation[0] != 0 for rotation in rotations):
            raise ValueError('Retained cap stabilizer is not the expected tetrahedral group')
        self._rotations = rotations
        self._signatures = {}
        regions, signature_orbits = {}, {}
        for region, position in enumerate(model.sp[:433]):
            signature = self.incidence_signature(int(position))
            orbit = int(model.oid[position])
            self._signatures[region] = signature
            regions.setdefault(orbit, []).append(region)
            signature_orbits.setdefault(signature, set()).add(orbit)
        if (len(signature_orbits) != 36 or any(len(v) != 1 for v in signature_orbits.values())
                or any(len({self._signatures[r] for r in rows}) != 1 for rows in regions.values())
                or not np.all(model.so.reshape(600, 433) == model.so[:433])):
            raise ValueError('Retained incidence signatures do not classify exactly 35 moving orbits and fixed centers')
        # Canonical signatures identify an actual region in the base cell.
        base_lookup = {(tuple(model.hosting(int(p))), tuple(model.caps(int(p)))): r
                       for r, p in enumerate(model.sp[:433])}
        self._orbits = {}
        coarse_groups = {}
        for orbit, rows in regions.items():
            signature = self._signatures[rows[0]]
            region = base_lookup[signature]
            if orbit < 0:
                caption = 'Fixed cell center'
                short = 'Center'
                group = 'fixed'
            else:
                census = model.census['orbits'][orbit]
                group = {'trivial': '1', 'C2': 'C2', 'C5': 'C5',
                         'D5 (order 10)': 'D5', 'A5 (order 60)': 'A5'}[census['orientation_group']]
                count, caps = census['colors'], len(census['mask'])
                caption = str(count) + (' sticker / ' if count == 1 else ' stickers / ') + str(caps) + ' cap domains'
                caption += ' · ' + ('trivial' if group == '1' else group) + ' orientation'
                short = str(count) + 'S·' + str(caps) + 'C·' + group
                coarse_groups.setdefault((count, caps, census['orientation_group']), []).append(orbit)
            bary = self._barycentric[region].tolist()
            self._orbits[orbit] = dict(canonical_id=orbit, name=caption, short_name=short,
                model=model.model_id, naming_version=NAMING_VERSION, structure_symbol=None,
                orientation_label=group,
                signature=dict(hosting_cells=[c + 1 for c in signature[0]],
                               affecting_caps=[c + 1 for c in signature[1]],
                               hosting_poles=[list(self._poles[c]) for c in signature[0]],
                               affecting_poles=[list(self._poles[c]) for c in signature[1]]),
                representative_region=dict(region=region, barycentric=bary, frame_vertices=list(self._base_vertices)),
                detail='Structure under all twelve proper cap rotations and every hosting-cell anchor. '
                       'The region portrait uses the ordered retained tetrahedron; it is not a handedness claim.')
        for orbits in coarse_groups.values():
            if len(orbits) > 1:
                ordered = sorted(orbits, key=lambda o: self._signatures[regions[o][0]])
                for symbol, orbit in zip('αβγδεζηθικλμνξοπρστυφχψω', ordered):
                    row = self._orbits[orbit]
                    row['name'] += ' · structure ' + symbol
                    row['short_name'] += '·' + symbol
                    row['structure_symbol'] = symbol
        if len({row['name'] for row in self._orbits.values()}) != 36:
            raise ValueError('Mathematical orbit captions are ambiguous')
        self._build_addresses(base_lookup, regions)

    def _build_addresses(self, base_lookup, regions):
        """Canonical anchored components and shortlex H/T/T^-1 location words."""
        model = self.m
        base_pairs = {region: pair for pair, region in base_lookup.items()}
        generators = [('H', model.z['rotperms'][0]), ('T', model.z['rotperms'][1]),
                      ('T^-1', np.argsort(model.z['rotperms'][1]))]
        maps = []
        for symbol, permutation in generators:
            mapped = [base_lookup[(tuple(sorted(map(int, permutation[list(host)]))),
                                   tuple(sorted(map(int, permutation[list(caps)]))))]
                      for host, caps in (base_pairs[r] for r in range(433))]
            if len(set(mapped)) != 433:
                raise ValueError('Geometric address generator does not permute all reference regions')
            maps.append((symbol, mapped))
        self._region_addresses = [None] * 433
        self._address_regions = {}
        for orbit, members in regions.items():
            remaining, components = set(members), []
            while remaining:
                representative = min(remaining, key=base_pairs.__getitem__)
                queue, words = [representative], {representative: []}
                for region in queue:
                    for symbol, permutation in maps:
                        target = permutation[region]
                        if target not in words:
                            words[target] = words[region] + [symbol]
                            queue.append(target)
                if not set(words) <= remaining:
                    raise ValueError('Anchored rotations disagree with full structure classes')
                components.append((representative, words))
                remaining -= set(words)
            profile = self._orbits[orbit]
            profile['local_representatives'] = []
            for index, (representative, words) in enumerate(components):
                marker = 'u' + str(index)
                profile['local_representatives'].append(dict(name=marker, region=representative,
                    region_count=len(words), frame_vertices=list(self._base_vertices)))
                for region, word in words.items():
                    text = (' '.join(word) if word else 'e')
                    if len(components) > 1:
                        text = marker + ': ' + text
                    entry = dict(structure_class=profile['short_name'], word=word,
                                 text=text.replace('T^-1', 'T⁻¹'),
                                 representative=marker, region=representative)
                    self._region_addresses[region] = entry
                    key = (profile['short_name'], text)
                    if key in self._address_regions:
                        raise ValueError('Geometric region address is ambiguous')
                    self._address_regions[key] = region
        if any(row is None for row in self._region_addresses) or len(self._address_regions) != 433:
            raise ValueError('Geometric addresses do not cover the 433 reference regions')
        self._cell_addresses = {self._compact_pole(pole): c for c, pole in enumerate(self._poles)}

    def _copy_address(self, kind, display):
        return '|'.join(('Magic600.' + kind, NAMING_VERSION, self.m.model_id, display))

    def _parse_address(self, text, kind):
        if not isinstance(text, str):
            raise ValueError('A copied mathematical address must be text')
        fields = text.split('|', 3)
        if len(fields) != 4:
            raise ValueError('Use a complete versioned mathematical address, or the existing canonical-ID input')
        if fields[0] != 'Magic600.' + kind:
            raise ValueError('Expected a ' + kind + ' address; object types are not interchangeable')
        if fields[1] != NAMING_VERSION:
            raise ValueError('Address naming version does not match this model display')
        if fields[2] != self.m.model_id:
            raise ValueError('Address model does not match the loaded immutable model')
        parts = fields[3].split(' / ')
        if len(parts) != 3 or parts[0] not in self._cell_addresses:
            raise ValueError('Address needs an exact cell pole, structure class and region word')
        # Only the display spelling changes; the exact shortlex token index is stable.
        word_text = ' '.join('T^-1' if token == 'T⁻¹' else token for token in parts[2].split(' '))
        key = (parts[1], word_text)
        if key not in self._address_regions:
            raise ValueError('Unknown structure or region word; copy its canonical shortlex address')
        slot = self._cell_addresses[parts[0]] * 433 + self._address_regions[key]
        return slot if kind == 'Slot' else int(self.m.sp[slot])

    def parse_slot(self, text):
        """Resolve the explicitly named hosting-cell sticker region."""
        return self._parse_address(text, 'Slot')

    def parse_address(self, text):
        """Resolve a fixed position without consulting its current occupant."""
        return self._parse_address(text, 'Position')

    def parse_identity(self, text):
        """Resolve a physical piece by its immutable Home address."""
        return self._parse_address(text, 'Piece')

    @staticmethod
    def _tuple(values):
        return '⟨' + ', '.join(values) + '⟩'

    @staticmethod
    def _compact_pole(values):
        return '⟨' + ','.join(values) + '⟩'

    @staticmethod
    def _region_text(values):
        return 'λ≈(' + ', '.join(format(float(value), '.3f') for value in values) + ')'

    def _position(self, position):
        if type(position) is not int or not 0 <= position < self.m.np:
            raise ValueError('Position must be a canonical integer in the retained model')
        return position

    def cell(self, canonical):
        if type(canonical) is not int or not 1 <= canonical <= 600:
            raise ValueError('Cell must be a canonical integer in C1..C600')
        return deepcopy(self._cells[canonical - 1])

    def orbit(self, orbit):
        if type(orbit) is not int or orbit not in self._orbits:
            raise ValueError('Orbit must be a retained moving orbit or the fixed-center sentinel')
        return deepcopy(self._orbits[orbit])

    def incidence_signature(self, position):
        """Exact canonical hosting/cap membership under actual model rotations."""
        position = self._position(position)
        hosting, caps = self.m.hosting(position), self.m.caps(position)
        candidates = []
        for anchor in hosting:
            local_host = self._inverse_frames[anchor, hosting]
            local_caps = self._inverse_frames[anchor, caps]
            for rotation in self._rotations:
                candidates.append((tuple(sorted(map(int, rotation[local_host]))),
                                   tuple(sorted(map(int, rotation[local_caps])))))
        return min(candidates)

    def slot(self, slot):
        if type(slot) is not int or not 0 <= slot < self.m.n:
            raise ValueError('Sticker slot must be a canonical integer in the retained model')
        cell, region = divmod(slot, 433)
        bary = self._barycentric[region].tolist()
        entry = self._region_addresses[region]
        pole = self._compact_pole(self._poles[cell])
        location = entry['structure_class'] + ' / ' + entry['text']
        display = pole + ' / ' + location
        return dict(canonical_id=slot, cell=cell + 1,
                    name='Cell ' + display, short_name=display,
                    lines=[pole, location], barycentric=bary,
                    model=self.m.model_id, naming_version=NAMING_VERSION,
                    structure_class=entry['structure_class'], local_representative=entry['representative'],
                    representative_region=entry['region'], address_word=list(entry['word']),
                    address_word_text=entry['text'], display_address=display,
                    copy_text=self._copy_address('Slot', display),
                    address_note='Geometric location word; not a Twist instruction or current piece orientation',
                    frame='Retained cell frame: ordered corners a, b, c, d',
                    frame_vertices=list(self._cell_frame_vertices[cell]),
                    coordinate_note='Approximate centroid location, not an orientation certificate or input ID')

    def address(self, position):
        """Fixed physical location; no occupant is used to generate this address."""
        position = self._position(position)
        hosting = self.m.hosting(position)
        # An anchor choice is presentation only; all incidence is retained below.
        anchor = min(hosting, key=lambda c: self._poles[c])
        slots = self.m.slots(position)
        slot = next(int(s) for s in slots if int(s) // 433 == anchor)
        address = self.slot(slot)
        address.update(position=position, hosting_cells=[c + 1 for c in hosting],
                       affecting_caps=[c + 1 for c in self.m.caps(position)],
                       cell_name=self._cells[anchor]['name'],
                       cell_pole=self._compact_pole(self._poles[anchor]),
                       location=address['address_word_text'],
                       barycentric_location=self._region_text(address['barycentric']),
                       copy_text=self._copy_address('Position', address['display_address']))
        return address

    def piece_record(self, record):
        """Detached display payload. Identity and Home remain immutable across turns."""
        if record is None:
            return None
        identity = self._position(record['piece'])
        position = self._position(record['position'])
        orbit = int(self.m.oid[identity])
        if int(self.m.oid[position]) != orbit or record['orbit'] != orbit:
            raise ValueError('Piece identity and physical position belong to different orbits')
        home, current = self.address(identity), self.address(position)
        profile = self._orbits[orbit]
        structure = str(int(self.m.k[identity])) + '-cell'
        census = self.m.census['orbits'][orbit] if orbit >= 0 else None
        return dict(identity_name=structure + ' piece · Home ' + home['name'],
                    identity_short=home['short_name'], identity_lines=list(home['lines']),
                    identity_copy_text=self._copy_address('Piece', home['display_address']),
                    model=self.m.model_id, naming_version=NAMING_VERSION,
                    home_name='Home ' + home['name'], current_name='At ' + current['name'],
                    home_short=home['short_name'], current_short=current['short_name'],
                    home_lines=list(home['lines']), current_lines=list(current['lines']),
                    orbit_name=profile['name'], category=profile['short_name'] if census else 'Fixed cell center',
                    symmetry=census['orientation_group'] if census else 'fixed',
                    symmetry_scope='Piece orientation group, not an orbit-defining group',
                    cell=home['cell_name'], location=home['location'], home=home, current=current,
                    detail='Identity is named by immutable Home structure. Current location follows the piece. '
                           'The region word is a geometric address in a versioned reference, not an executable setup. '
                           'λ values are approximate barycentric centroid coordinates in the retained cell frame; '
                           'exact hosting/cap incidence and canonical IDs remain available. '
                           'Neither the name nor the local centroid defines the current Grip or piece orientation.')
