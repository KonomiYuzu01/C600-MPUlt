"""Exact base-polytope audit; does not reconstruct the full puzzle's cuts.

Coordinates use integer pairs a+b*phi for a radius-two 600-cell.
The coordinate construction is documented in Baez, arXiv:1712.06436v2.
Only the standard library is used. No application or personal data is read.
"""
from collections import Counter, defaultdict, deque
from itertools import combinations, permutations, product
from pathlib import Path
import json

ZERO = (0, 0)


def add(x, y):
    return x[0] + y[0], x[1] + y[1]


def neg(x):
    return -x[0], -x[1]


def sub(x, y):
    return add(x, neg(y))


def mul(x, y):
    a, b = x
    c, d = y
    return a*c+b*d, a*d+b*c+b*d


def total(xs):
    out = ZERO
    for x in xs:
        out = add(out, x)
    return out


def positive(x):
    # 2(a+b*phi) = (2a+b) + b*sqrt(5); compare exactly.
    a, b = 2*x[0]+x[1], x[1]
    if not b:
        return a > 0
    if b > 0:
        return a >= 0 or 5*b*b > a*a
    return a > 0 and a*a > 5*b*b


def dot(v, w):
    return total(mul(a,b) for a,b in zip(v,w))


def qmul(v, w):
    """Twice the product of two unit quaternions stored at scale two."""
    a,b,c,d = v
    e,f,g,h = w
    raw = (total((mul(a,e),neg(mul(b,f)),neg(mul(c,g)),neg(mul(d,h)))),
           total((mul(a,f),mul(b,e),mul(c,h),neg(mul(d,g)))),
           total((mul(a,g),neg(mul(b,h)),mul(c,e),mul(d,f))),
           total((mul(a,h),mul(b,g),neg(mul(c,f)),mul(d,e))))
    assert all(x%2 == 0 and y%2 == 0 for x,y in raw)
    return tuple((x//2,y//2) for x,y in raw)


def conjugate(v):
    return (v[0],) + tuple(neg(x) for x in v[1:])


def rank_mod2(columns):
    pivots = {}
    for column in columns:
        while column:
            i = column.bit_length()-1
            if i in pivots:
                column ^= pivots[i]
            else:
                pivots[i] = column
                break
    return len(pivots)


def distance_histogram(adjacency, start):
    distance = {start:0}
    queue = deque([start])
    while queue:
        i = queue.popleft()
        for j in adjacency[i]:
            if j not in distance:
                distance[j] = distance[i]+1
                queue.append(j)
    assert len(distance) == len(adjacency)
    count = Counter(distance.values())
    return [count[i] for i in range(max(count)+1)]


def verify():
    vertices = set()
    for axis in range(4):
        for sign in (-1,1):
            v = [ZERO]*4
            v[axis] = (2*sign,0)
            vertices.add(tuple(v))
    vertices.update(tuple((s,0) for s in signs) for signs in product((-1,1),repeat=4))
    even_orders = [p for p in permutations(range(4)) if sum(p[i]>p[j] for i in range(4) for j in range(i+1,4))%2 == 0]
    for signs in product((-1,1),repeat=3):
        a,b,c = signs
        v = (ZERO,(a,0),(0,b),(-c,c))
        vertices.update(tuple(v[i] for i in order) for order in even_orders)
    assert len(vertices) == 120 and all(dot(v,v)==(4,0) for v in vertices)
    V = sorted(vertices)
    adjacency = [set() for _ in V]
    edges = []
    for i,j in combinations(range(120),2):
        if dot(V[i],V[j]) == (0,2):
            adjacency[i].add(j)
            adjacency[j].add(i)
            edges.append((i,j))
    assert len(edges)==720 and all(len(a)==12 for a in adjacency)
    triangles = sorted({tuple(sorted((i,j,k))) for i,j in edges for k in adjacency[i]&adjacency[j]})
    tetrahedra = sorted({tuple(sorted((i,j,k,l))) for i,j,k in triangles for l in adjacency[i]&adjacency[j]&adjacency[k]})
    assert len(triangles)==1200 and len(tetrahedra)==600
    incidence = defaultdict(list)
    for i,t in enumerate(tetrahedra):
        normal = tuple(total(V[v][d] for v in t) for d in range(4))
        height = (4,6)
        assert dot(normal,normal)==(16,24)
        assert all(dot(normal,V[v])==height for v in t)
        assert all(positive(sub(height,dot(normal,V[v]))) for v in range(120) if v not in t)
        for face in combinations(t,3):
            incidence[face].append(i)
    assert set(incidence)==set(triangles) and all(len(t)==2 for t in incidence.values())
    assert set(Counter(e for t in tetrahedra for e in combinations(t,2)).values())=={5}
    assert set(Counter(v for t in tetrahedra for v in t).values())=={20}
    for v in range(120):
        faces = [tuple(x for x in t if x!=v) for t in tetrahedra if v in t]
        link_edges = Counter(e for f in faces for e in combinations(f,2))
        link_vertices = set(x for f in faces for x in f)
        assert (len(link_vertices),len(link_edges),len(faces))==(12,30,20)
        assert set(link_edges.values())=={2}
    E = {e:i for i,e in enumerate(edges)}
    F = {f:i for i,f in enumerate(triangles)}
    d1 = [(1<<i)^(1<<j) for i,j in edges]
    d2 = [sum(1<<E[e] for e in combinations(f,2)) for f in triangles]
    d3 = [sum(1<<F[f] for f in combinations(t,3)) for t in tetrahedra]
    for f in triangles:
        result=0
        for e in combinations(f,2):
            result ^= d1[E[e]]
        assert result==0
    for t in tetrahedra:
        result=0
        for f in combinations(t,3):
            result ^= d2[F[f]]
        assert result==0
    ranks = [rank_mod2(d) for d in (d1,d2,d3)]
    assert ranks==[119,601,599]
    betti = [120-ranks[0],720-ranks[0]-ranks[1],1200-ranks[1]-ranks[2],600-ranks[2]]
    assert betti==[1,0,0,1]
    hist = distance_histogram(adjacency,0)
    assert hist==[1,12,32,42,32,1]
    assert all(distance_histogram(adjacency,v)==hist for v in range(120))
    dual = [set() for _ in tetrahedra]
    for i,j in incidence.values():
        dual[i].add(j)
        dual[j].add(i)
    assert all(len(x)==4 for x in dual)
    dual_hist = distance_histogram(dual,0)
    assert all(distance_histogram(dual,t)==dual_hist for t in range(600))
    for p in V:
        for q in V:
            assert qmul(p,q) in vertices
    basis = [tuple((2 if i==j else 0,0) for i in range(4)) for j in range(4)]
    rotation_maps = {tuple(qmul(qmul(p,x),conjugate(q)) for x in basis) for p in V for q in V}
    reflection_maps = {tuple(qmul(qmul(p,conjugate(x)),conjugate(q)) for x in basis) for p in V for q in V}
    assert len(rotation_maps)==len(reflection_maps)==7200 and not rotation_maps&reflection_maps
    return dict(passed=True, scope='Exact regular base polytope and its mod-2 simplicial homology; not a cut-sticker census or a full puzzle-group order computation.',
                coordinate_field='Z[phi], phi^2=phi+1; vertices have circumradius 2',
                f_vector=[120,720,1200,600], euler_characteristic=0,
                exact_supporting_tetrahedra=600, tetrahedra_per_edge=5,tetrahedra_per_vertex=20,
                vertex_link_f_vector=[12,30,20], boundary_ranks_mod2=ranks,betti_numbers_mod2=betti,
                vertex_graph_distance_histogram=hist,vertex_graph_diameter=len(hist)-1,
                dual_graph_distance_histogram=dual_hist,dual_graph_diameter=len(dual_hist)-1,
                unit_quaternion_group_order=120,orientation_preserving_symmetry_order=7200,full_symmetry_order=14400,
                notes=['Integral homology and simple connectedness follow from the convex-body radial homeomorphism; mod-2 Betti numbers alone do not prove either.',
                       'Quaternion multiplication closure and distinct basis-image maps are checked exactly; geometric symmetries are not the local twist group.',
                       'No claim of a new performance result or a change to immutable application assets.'])


if __name__=='__main__':
    if not __debug__:
        raise SystemExit('Run without Python -O.')
    result=verify()
    Path(__file__).with_name('regular-geometry-results.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(result,indent=2))
