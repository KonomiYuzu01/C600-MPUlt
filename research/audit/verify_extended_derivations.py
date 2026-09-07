"""Check the expanded report's algebra, counting bounds and retained tables.

Only declarative project assets are read. No application, solver, session,
network, or native graphics code is imported or executed. This does not repeat
geometric clipping, oriented move-invariant verification, or native tests.
"""
from __future__ import annotations
import argparse
from collections import Counter
from decimal import Decimal, localcontext
import gzip
import hashlib
import itertools
import json
import math
from pathlib import Path


def compose(a, b):
    """Chronological source-to-destination composition: a first, then b."""
    return tuple(b[a[i]] for i in range(len(a)))


def inverse(a):
    return tuple(a.index(i) for i in range(len(a)))


def cycle(n, *points):
    a = list(range(n))
    for i, j in zip(points, (*points[1:], points[0])):
        a[i] = j
    return tuple(a)


def power(a, n):
    out = tuple(range(len(a)))
    for _ in range(n):
        out = compose(out, a)
    return out


def commutator(a, b):
    return compose(compose(compose(a, b), inverse(a)), inverse(b))


def even(a):
    return sum(a[i] > a[j] for i in range(len(a)) for j in range(i + 1, len(a))) % 2 == 0


def verify(root):
    hashes = {}
    def read(name, compressed=False):
        raw = (root / name).read_bytes()
        hashes[name] = hashlib.sha256(raw).hexdigest()
        return json.loads(gzip.decompress(raw) if compressed else raw)
    census = read('assets/census.json')
    atlas = read('assets/seed_atlas.json')
    trees = read('assets/execution_trees.json.gz', True)
    assert len(census['orbits']) == len(atlas) == len(trees) == 35
    order = [6,0,17,15,2,22,21,8,23,9,29,25,13,11,10,34,28,26,24,16,14,12,3,33,32,31,30,27,20,19,18,7,5,4,1]
    where = {o:i for i,o in enumerate(order)}
    rows = []
    upper = 1
    divisor = 1
    star_total = primitive_total = 0
    for o, (c, a, t) in enumerate(zip(census['orbits'], atlas, trees)):
        assert c['id'] == a['orbit'] == o
        n, k, h = c['pieces'], c['colors'], c['orientation_order']
        assert c['stickers'] == n * k
        assert a['pieces'] == n and a['orientation_order'] == h
        assert a['word'] == t['seed'] and len(t['seed']) == a['seed_length']
        states = (n - 2) * h
        assert len(t['parent']) == len(t['positions']) == len(t['frames']) == len(t['depth']) == states
        for i, parent in enumerate(t['parent']):
            if parent < 0:
                assert i == 0 and t['depth'][i] == 0
            else:
                assert 0 <= parent < i and t['depth'][i] == t['depth'][parent] + 1
        assert all(where[o] < where[x] for x in a['collateral'])
        max_depth = max(t['depth'])
        max_word = len(t['seed']) + 2 * len(t['relocation']) + 2 * max_depth
        orientable = int(h > 1)
        nonabelian = int(h in (10, 60))
        stars = 2 * (n - 2) + orientable * (2 * (n - 2) + 3 + 8 * nonabelian)
        star_total += stars
        primitive_total += stars * max_word
        upper *= math.factorial(n) * h ** n
        divisor *= 2 * (2 if h in (2, 10) else 5 if h == 5 else 1)
        rows.append(dict(orbit=o, rank=c['rank'], stickers_per_piece=k, pieces=n,
                         local_group=c['orientation_group'], orientation_order=h,
                         seed_length=len(t['seed']), relocation_length=len(t['relocation']),
                         setup_states=states, maximum_setup_depth=max_depth,
                         maximum_star_primitives=max_word, collateral=a['collateral'],
                         star_bound=stars, primitive_bound=stars*max_word))
    assert sum(x['pieces'] for x in rows) == 176520
    assert sum(x['pieces'] * x['stickers_per_piece'] for x in rows) + 600 == 259800
    assert star_total == 405945 and primitive_total == 385797152
    assert divisor == 2**43 * 5**2 and upper % divisor == 0
    selected = {1,4,5,7,18,19,27,31,33}
    lower = 1
    for row in rows:
        if row['orbit'] in selected:
            assert row['stickers_per_piece'] == row['orientation_order'] == 1
            n = row['pieces']
            assert n % 600 == 0
            lower *= math.factorial(n) // math.factorial(n//600)**600
    assert lower > 10**151851
    alphabet = 1800
    assert alphabet**1000 < 10**3256
    assert alphabet**1000 * 10**148595 < lower
    def ball(d):
        return (alphabet ** (d + 1) - 1) // (alphabet - 1)
    lo, hi = 0, 100000
    while lo + 1 < hi:
        middle = (lo + hi) // 2
        if ball(middle) < lower:
            lo = middle
        else:
            hi = middle
    assert ball(lo) < lower <= ball(hi)
    # Independent small-domain algebra checks of every distinct target pair.
    for n in range(4, 10):
        for x, y in itertools.permutations(range(2, n), 2):
            assert compose(inverse(cycle(n,0,1,y)),cycle(n,0,1,x)) == cycle(n,0,y,x)
        for x,y,z in itertools.permutations(range(1,n),3):
            assert compose(cycle(n,0,x,y),cycle(n,0,z,x)) == cycle(n,x,y,z)
    r = cycle(5,0,1,2,3,4)
    f = tuple((-i)%5 for i in range(5))
    for k in range(5):
        assert commutator(power(r,k),f) == power(r,(2*k)%5)
    d5 = {power(r,k) for k in range(5)} | {compose(power(r,k),f) for k in range(5)}
    a5 = {p for p in itertools.permutations(range(5)) if even(p)}
    assert len(d5) == 10 and all(even(p) for p in d5)
    assert len({commutator(a,b) for a in d5 for b in d5}) == 5
    assert len(a5) == 60 and {commutator(a,b) for a in a5 for b in a5} == a5
    with localcontext() as ctx:
        ctx.prec = 45
        ln10 = Decimal(10).ln()
        logarithms = {name:str(Decimal(value).ln()/ln10) for name,value in
                      [('colored_lower',lower),('structural_labelled_upper',upper),
                       ('invariant_constrained_labelled_upper',upper//divisor),
                       ('1000_move_word_upper',alphabet**1000)]}
    return dict(passed=True,
                scope='Arithmetic and small-domain group identities, plus structural checks of retained declarative tree/seed tables; no all-orbit geometry or native replay.',
                rows=rows, triangular_order=order,
                star_bound=star_total, primitive_bound=primitive_total,
                pure_seed_count=sum(not r['collateral'] for r in rows),
                invariant_divisor='2^43 * 5^2', logarithms=logarithms,
                primitive_alphabet_size=alphabet,
                diameter_lower_bound=hi,
                exact_ball_below_colored_bound_at_depth=lo,
                exact_ball_reaches_count_bound_at_depth=hi,
                total_variation_lower_bound='1 - 10^-148595 (strict), for 1000-step scrambles from solved versus uniform reachable face-color states',
                nine_orbit_controller_evidence='See verify_color_bound.py and results.json; this script does not replace their full-word and guarded reachability checks.',
                small_domain_cycle_identities=True, d5_commutator_image_size=5,
                a5_commutator_image_size=60, input_sha256=hashes)


if __name__ == '__main__':
    if not __debug__:
        raise SystemExit('Assertions must be enabled; do not use Python -O.')
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-root', type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument('--output', type=Path, default=Path(__file__).with_name('extended-results.json'))
    args = parser.parse_args()
    result = verify(args.source_root)
    args.output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:result[k] for k in ['passed','star_bound','primitive_bound','diameter_lower_bound','logarithms']},indent=2))
