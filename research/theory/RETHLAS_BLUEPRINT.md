# Full 600-cell: geometry, topology, legal actions, and sound optimization

This is a candidate proof blueprint awaiting coordinator verification. The supplied geometry checker and proof-verification service were not executed here. The numerical comparisons used below were independently recomputed with exact integer arithmetic. Geometric conclusions are unconditional mathematical statements; conclusions about the retained puzzle are conditional on the explicitly stated model and certificate hypotheses.

# Definitions and mathematical sources

Put
$$
\phi=\frac{1+\sqrt5}{2},\qquad \tau=\phi^{-1}=\phi-1.
$$
Thus
$$
\phi^2=\phi+1,\qquad \tau^2=2-\phi.
$$

Identify $\mathbb R^4$ with the quaternion algebra $\mathbb H$ using the ordered orthonormal basis $1,i,j,k$. Quaternion conjugation is denoted by $x\mapsto\bar x$, and
$$
\operatorname{Sp}(1)=\{x\in\mathbb H:|x|=1\}.
$$
The binary icosahedral group $I^*$ is the inverse image of the rotation group of a regular icosahedron under
$$
\operatorname{Sp}(1)\longrightarrow SO(3),\qquad
p\longmapsto\bigl(u\mapsto pup^{-1}\bigr),
$$
where $\mathbb R^3$ is identified with the imaginary quaternions.

For a finite set $X$, write $S_X$ and $A_X$ for its symmetric and alternating groups. Write $C_m$ for the cyclic group of order $m$, and
$$
D_5=\langle R,F\mid R^5=F^2=1,\ FRF=R^{-1}\rangle
$$
for the dihedral group of order $10$.

A flag of a four-dimensional polytope is an incident chain consisting of a vertex, edge, two-face, and facet. A polytope is regular when its geometric symmetry group is transitive on flags.

Two external geometric results will be used, with the following complete statements and conventions.

**Quaternion coordinate result.** In a suitable orthonormal quaternion coordinate system, the inverse image of the icosahedron’s rotation group consists precisely of
$$
(\pm1,0,0,0),\qquad
\frac12(\pm1,\pm1,\pm1,\pm1),\qquad
\frac12(\pm\phi,\pm1,\pm\tau,0),
$$
together with their even coordinate permutations, with independent signs.

Source identifiers: `paper_id = Baez, From the Icosahedron to E8`; `arXiv_id = 1712.06436v2`; `theorem_id = unnumbered coordinate assertion in “The Icosians”`. Here the finite group is the binary lift, not the infinite ring of integral combinations called the icosians. [Baez, “The Icosians”](https://arxiv.org/html/1712.06436v2).

**Binary-group polytope result.** The binary icosahedral subgroup of unit quaternions is one orbit of the reflection group of type $H_4$, and its elements are the vertices of a regular 600-cell.

Source identifiers: `paper_id = Choi–Lee, Binary Icosahedral Group and 600-Cell, Symmetry 10 (2018), 326`; `theorem_id = Theorem 2`; `arXiv_id = not supplied`. The paper’s binary-group notation indicates a double covering, not coordinate dilation. Its proof establishes reflection invariance in Lemma 2, then identifies the orbit of $1$ using its $H_3$ stabilizer and the $120$-element orbit count. Its quaternion norm is the ordinary Euclidean norm used here. [Choi–Lee, Theorem 2 and its proof](https://pdfs.semanticscholar.org/0fef/c9c8c31093da12262c8f6740da46c0dfb46e.pdf).

These results concern the uncut polytope. They provide no sticker census or puzzle reachability theorem.

# Lemma 1: Coordinate identification

## statement

The set $V$ in the problem has $120$ distinct elements, every element has norm $2$, and $V/2$ is a binary icosahedral subgroup of $\operatorname{Sp}(1)$. Its convex hull is a regular 600-cell.

## proof

The three coordinate families are disjoint because their patterns of zero coordinates and absolute values differ. Their cardinalities are
$$
8,\qquad 16,\qquad 12\cdot8=96.
$$
In the third family all four absolute values are distinct, so the twelve even permutations give twelve distinct arrangements for each sign choice.

The squared norms are respectively
$$
4,\qquad 4,\qquad 1+\phi^2+\tau^2=4.
$$

The permutation taking
$$
(\phi,1,\tau,0)\quad\text{to}\quad(0,1,\phi,\tau)
$$
has index sequence $(4,2,1,3)$ and is even. Thus the displayed coordinate set agrees exactly with twice the quaternion coordinate set stated above, including its permutation convention.

The coordinate result identifies $V/2$ with $I^*$. This identification uses the inverse image of an icosahedral rotation group, hence supplies multiplication closure and inverses, not merely a set of $120$ points on a sphere. One direct route to its coordinates is to lift the identity and the rotations about edge, face, and vertex axes using
$$
\cos(\theta/2)+u\sin(\theta/2)
$$
and its negative, where $u$ is the unit imaginary axis. The rotations comprise the identity, fifteen half-turns, twenty third-turns, and twenty-four fifth-turn rotations.

The binary-group polytope result now applies in the same unit-quaternion metric. Multiplication by $2$ preserves regularity and the face lattice. Therefore $P=\operatorname{conv}(V)$ is a regular 600-cell. $\square$

# Lemma 2: Radial topology of the boundary

## statement

The boundary $\partial P$ is homeomorphic to $S^3$. Consequently,
$$
H_j(\partial P;\mathbb Z)\cong
\begin{cases}
\mathbb Z,&j=0,3,\\
0,&j\ne0,3,
\end{cases}
\qquad
\pi_1(\partial P)=1.
$$

## proof

The polytope contains
$$
\operatorname{conv}\{\pm2e_1,\ldots,\pm2e_4\},
$$
which contains the Euclidean unit ball. Indeed, $\|x\|_2\le1$ implies $\|x\|_1\le2$. Thus $0$ lies in the interior of $P$. The polytope is compact and contained in the ball of radius $2$.

For every $u\in S^3$, convexity and compactness give
$$
P\cap\{tu:t\ge0\}=\{tu:0\le t\le r(u)\}
$$
for a unique $r(u)>0$. Every point $tu$ with $0\le t<r(u)$ is interior: it is a convex combination, with positive weight on the first term, of an interior point $0$ and the point $r(u)u\in P$. Hence $r(u)u$ is the unique boundary point on this ray.

The map
$$
\partial P\longrightarrow S^3,\qquad x\longmapsto\frac{x}{\|x\|}
$$
is therefore a continuous bijection. Its domain is compact and its codomain Hausdorff, so it is a homeomorphism.

The sphere $S^3$ has a CW decomposition with one zero-cell and one three-cell, giving the stated integral homology. For simple connectedness, cover $S^3$ by the complements of two distinct antipodal points. Each complement is homeomorphic to $\mathbb R^3$, and their intersection is path connected. The van Kampen theorem gives trivial fundamental group.

This proves the topology directly. Neither mod-$2$ Betti numbers nor equality of integral homology groups alone would establish the homeomorphism or fundamental group. $\square$

# Lemma 3: Faces, links, metric data, and duality

## statement

The polytope $P$ has circumradius $2$, edge length $2/\phi$, and
$$
(f_0,f_1,f_2,f_3)=(120,720,1200,600).
$$
Its facets are regular tetrahedra, five facets meet at each edge, twenty meet at each vertex, and its vertex links are icosahedral. Its Schläfli symbol is $\{3,3,5\}$, and its polar dual is a regular 120-cell of type $\{5,3,3\}$.

## proof

Every point of $V$ is an exposed vertex: the functional $x\mapsto v\cdot x$ has its unique maximum at $v$ among points of $V$, since all have norm $2$. Thus $P$ has exactly $120$ vertices and circumradius $2$.

Let $v=(2,0,0,0)$. The largest first coordinate of another vertex is $\phi$, attained at twelve vertices. Consequently the largest inner product with $v$ among distinct vertices is $2\phi$, and the minimum squared distance is
$$
8-4\phi=4\tau^2.
$$
The group $I^*$ acts transitively on $V$ by left multiplication, so this is also the global minimum distance.

A closest pair of equal-norm vertices is an edge. To see this, let their common squared norm be $R^2$ and their inner product be $m<R^2$. For any other vertex $z$, minimality of their distance gives
$$
(v+w)\cdot z\le2m<R^2+m=(v+w)\cdot v=(v+w)\cdot w.
$$
Thus the functional $v+w$ exposes precisely their segment.

Regularity makes all edges congruent. Hence the twelve closest vertices are exactly the neighbors of $v$, and the edge length is $2\tau=2/\phi$.

Their projections onto $v^\perp$ have coordinates
$$
(0,\pm1,\pm\tau)
$$
and cyclic permutations. These are a scaled, orthogonally positioned regular icosahedron. All twelve neighboring vertices have first coordinate $\phi$, so a sufficiently close hyperplane section at $v$ is similar to this projected set. It follows that the vertex figure, and hence the vertex link, is icosahedral.

An icosahedron has twelve vertices, thirty edges, and twenty triangular faces. These numbers can also be obtained directly from its coordinates: each vertex has five nearest neighbors arranged in a pentagon; the resulting triangular boundary has
$$
E=12\cdot5/2=30,\qquad F=2E/3=20.
$$
Its triangular faces correspond to facets through $v$, giving twenty tetrahedral facets through each vertex. Tetrahedrality also follows from the regular 600-cell identification in Lemma 1.

Double counting yields
$$
f_1=\frac{120\cdot12}{2}=720,
\qquad
f_3=\frac{120\cdot20}{4}=600.
$$
Each tetrahedron has four triangular faces, and each ridge of a convex four-polytope belongs to two facets. Therefore
$$
f_2=\frac{600\cdot4}{2}=1200.
$$
The five triangles incident to a vertex of an icosahedral link correspond to the five facets incident to the associated edge of $P$. Thus the Schläfli symbol is $\{3,3,5\}$.

The Euler characteristic is
$$
120-720+1200-600=0,
$$
in agreement with Lemma 2.

Because $0$ is interior, the polar
$$
P^\circ=\{y:y\cdot x\le1\text{ for every }x\in P\}
$$
is a bounded four-polytope. Polarity reverses proper face incidences and sends a $j$-face to a $(3-j)$-face. Orthogonal symmetries commute with polarity, so the dual is regular. Its facets are dual to the icosahedral vertex figures and hence are dodecahedra. It has
$$
(f_0,f_1,f_2,f_3)=(600,1200,720,120)
$$
and reversed symbol $\{5,3,3\}$: it is the regular 120-cell.

This duality is not an identification of the two polytopes or their face counts. $\square$

# Lemma 4: The finite geometric symmetry group

## statement

For $p,q\in I^*$, define
$$
L_{p,q}(x)=pxq^{-1}.
$$
Then
$$
K^+\cong (I^*\times I^*)/\langle(-1,-1)\rangle,
\qquad |K^+|=7200,
$$
and quaternion conjugation extends this to $K$ of order $14400$.

Moreover,
$$
K\cong W(H_4)
$$
with presentation
$$
\left\langle s_0,s_1,s_2,s_3\ \middle|\
\begin{array}{l}
s_i^2=1,\\
(s_0s_1)^3=(s_1s_2)^3=(s_2s_3)^5=1,\\
(s_is_j)^2=1\quad\text{if }|i-j|>1
\end{array}
\right\rangle.
$$

## proof

Quaternion norm multiplicativity makes $L_{p,q}$ orthogonal. Its determinant is positive: the same construction on the connected space $\operatorname{Sp}(1)\times\operatorname{Sp}(1)$ depends continuously on $(p,q)$ and includes the identity. Multiplication closure of $I^*$ shows that $L_{p,q}$ preserves $V/2$, and therefore $P$.

If $L_{p,q}$ is the identity, evaluating at $1$ gives $p=q$. This quaternion must then commute with every quaternion. Commuting with $i$ and $j$ forces it to be real, so it is $1$ or $-1$. The kernel is precisely
$$
\{(1,1),(-1,-1)\}.
$$
Thus these maps give $120^2/2=7200$ distinct positive-determinant symmetries.

Quaternion conjugation
$$
J(x)=\bar x
$$
preserves $V$, since signs are independent. Its real matrix is
$$
\operatorname{diag}(1,-1,-1,-1),
$$
so $\det J=-1$. The maps $L_{p,q}$ and $L_{p,q}J$ therefore give $14400$ distinct symmetries.

An upper bound is still necessary. Fix a vertex $v$. Its stabilizer acts faithfully on the icosahedral vertex figure: the vertex direction and the three-dimensional span of that figure together span $\mathbb R^4$. An orthogonal symmetry of an icosahedron is determined by the image of one ordered triangular face. There are at most
$$
20\cdot6=120
$$
such images. Orbit–stabilizer consequently gives
$$
|K|\le120\cdot120=14400.
$$
The exhibited maps attain this upper bound, proving both group orders and the description of $K^+$.

Also,
$$
J L_{p,q}J=L_{q,p},
$$
so conjugation exchanges the two quaternion factors.

For the Coxeter presentation, radially project the barycentric subdivision of $\partial P$ onto $S^3$. A flag gives a spherical tetrahedral chamber. A symmetry fixing a flag fixes the barycenters of its four faces; these four vectors are linearly independent, so the symmetry is the identity. Regularity therefore gives a simply transitive action on chambers.

For each of the four chamber walls, the symmetry taking the chosen chamber to its adjacent chamber fixes the other three barycenter rays. It is the reflection in their span. Denote these reflections by $s_0,\ldots,s_3$, indexed by face ranks.

Chamber adjacency is connected, so these reflections generate $K$. Consecutive rank changes give rank-two sections of sizes $3,3,5$, producing the displayed relations. Nonconsecutive changes commute and give square galleries.

These relations are complete. A word acting trivially determines a closed gallery in the chamber complex. The dual cell decomposition is a cell decomposition of $S^3$, which is simply connected by Lemma 2. Its two-skeleton therefore has trivial fundamental group. Closed galleries are generated by backtracking and boundaries of dual two-cells. Those boundaries are precisely the involution, square, and rank-two polygon relations above. Hence the displayed presentation presents $K$, rather than merely mapping onto it.

The Coxeter group with this chain presentation is $W(H_4)$. $\square$

# The finite checker and its scope

The supplied checker implements a finite audit in the exact ring
$$
\mathbb Z[\phi]=\{a+b\phi:a,b\in\mathbb Z\}.
$$
It represents an element by $(a,b)$ and uses
$$
(a+b\phi)(c+d\phi)
=(ac+bd)+(ad+bc+bd)\phi.
$$
Its sign test compares integers after writing
$$
2(a+b\phi)=(2a+b)+b\sqrt5,
$$
with sign cases handled before squaring. Thus its incidence tests do not depend on floating-point tolerances.

Its algorithm:

1. Enumerates the three vertex families and verifies norms.
2. Joins pairs with inner product $2\phi$.
3. Enumerates graph triangles and four-cliques.
4. For each four-clique, forms the sum $N$ of its vertices and checks a strict supporting inequality at every other vertex.
5. Counts incidences and constructs vertex links.
6. Computes boundary matrices and ranks over $\mathbb F_2$.
7. Tests quaternion multiplication closure and counts distinct linear maps by their images on a basis.

For a tetrahedron with vertices $v_1,\ldots,v_4$, the Gram matrix has diagonal entries $4$ and off-diagonal entries $2\phi$. It is positive definite. Moreover,
$$
N=\sum_i v_i,\qquad
N\cdot v_i=4+6\phi,\qquad
\|N\|^2=16+24\phi.
$$
Thus the supporting tests certify actual tetrahedral facets, not merely graph cliques.

There is also a completeness argument beyond finding some supporting facets. If every triangular ridge of the listed tetrahedra has both its incident facets in the list, the list is closed under facet adjacency. The facet adjacency graph of a convex polytope is connected, so a nonempty such list contains all facets.

Nevertheless:

- A link’s $f$-vector alone does not identify it as an icosahedron; Lemma 3 supplies its geometry.
- Ranks over $\mathbb F_2$ do not prove integral homology or simple connectedness; Lemma 2 supplies those conclusions.
- Exhibiting $14400$ maps does not exclude further symmetries; Lemma 4 supplies the upper bound.
- No part of this checker reconstructs the cut subdivision, verifies all legal sticker maps, or determines the full legal group.

The supplied JSON records a previous reported run. The unconditional proofs above do not take its `passed` field as a substitute for these arguments.

# Lemma 5: Cell-pole normalization

## statement

The radius-two model has inradius
$$
r_{\mathrm{in}}=\frac{\phi^2}{\sqrt2}.
$$
A similarity with scale factor $4/\phi$ and a suitable rotation places a chosen facet pole at
$$
n_0=(\phi^3,1,1,1),
$$
with every facet plane written as
$$
n_c\cdot x=\|n_c\|^2.
$$

## proof

For a tetrahedral facet, use $N$ from the preceding calculation. Since
$$
\|N\|^2=4(4+6\phi),
$$
the perpendicular foot of the origin on its plane is
$$
n=\frac{N}{4}.
$$
Its squared norm is
$$
\|n\|^2=\frac{4+6\phi}{4}
=\frac{\phi^4}{2}.
$$
Regularity gives the same inradius at every facet.

On the other hand,
$$
\|n_0\|^2=\phi^6+3=8\phi^2.
$$
Therefore
$$
\frac{\|n_0\|}{r_{\mathrm{in}}}=\frac4\phi.
$$
Choose $Q\in SO(4)$ taking the direction of one original pole to that of $n_0$, and put
$$
\widetilde P=\frac4\phi QP.
$$
Every original pole $n$ becomes $\widetilde n=(4/\phi)Qn$, and its plane becomes
$$
\widetilde n\cdot x=\|\widetilde n\|^2.
$$
The selected pole is exactly $n_0$.

All subsequent puzzle geometry uses $\widetilde P$, identified with $P$ by this fixed similarity. Its geometric symmetry group is the conjugate of $K$ under the similarity. The radius-two and cell-pole coordinate scales are not numerically identified. $\square$

# Retained-model definitions and hypotheses

Let $\mathcal C$ be the set of $600$ facets, and let $n_c$ denote their poles in the normalization of Lemma 5. Put
$$
\alpha=\frac{121}{125},
\qquad
U_c=\{x\in\widetilde P:n_c\cdot x>\alpha\|n_c\|^2\}.
$$
Central symmetry pairs each $n_c$ with $-n_c$. Thus there are $600$ oriented caps or, equivalently, $300$ antipodal axes. For one axis, the two outer layers satisfy
$$
n_c\cdot x>\alpha\|n_c\|^2
\quad\text{and}\quad
n_c\cdot x<-\alpha\|n_c\|^2.
$$
The middle layer lies between them and is fixed during that axis’s outer-layer turns. It is not asserted to be fixed under turns about every other axis.

A full-dimensional cut chamber is specified by choosing a strict side of every cut hyperplane and intersecting these choices with $\operatorname{int}\widetilde P$. A nonempty such intersection is convex and hence connected. A surface piece is a retained chamber whose closure meets the boundary in one or more relatively three-dimensional patches. Cut-boundary strata are attached consistently to these pieces; they are not additional sticker slots.

For a piece position $A$, define its complete cap signature
$$
M(A)=\{c:\text{the chamber interior of }A\text{ lies in }U_c\}.
$$
Define its hosting set by
$$
\operatorname{Host}(A)
=
\{h:\overline A\cap h\text{ contains a retained relatively three-dimensional patch}\}.
$$
A sticker slot is such a patch. In this full-cut description it is addressed by
$$
(M(A),h),\qquad h\in\operatorname{Host}(A).
$$

The following hypotheses specify exactly the retained evidence used below.

**R — Retained subdivision and legal action.** The retained model is the complete surface subdivision just described, with no hidden identification of distinct pieces having the same recorded address. Legal cap rotations carry whole pieces and their sticker patches to whole pieces and patches. It has $259800$ sticker slots, $177120$ surface pieces, $600$ one-sticker centers fixed pointwise by every legal move, and the following $35$ moving orbits. The frame groups are faithful groups of permutations of the stickers of a piece after coherent transport.

| Orbit | Rank | Stickers per piece | Pieces $N_o$ | Frame group $H_o$ |
|---|---:|---:|---:|---|
| O00 | 1 | 2 | 1200 | $C_2$ |
| O01 | 2 | 1 | 3600 | $1$ |
| O02 | 3 | 1 | 2400 | $1$ |
| O03 | 3 | 2 | 3600 | $C_2$ |
| O04 | 4 | 1 | 7200 | $1$ |
| O05 | 4 | 1 | 7200 | $1$ |
| O06 | 4 | 5 | 720 | $D_5$ |
| O07 | 5 | 1 | 7200 | $1$ |
| O08 | 5 | 2 | 3600 | $C_2$ |
| O09 | 6 | 2 | 7200 | $1$ |
| O10 | 7 | 1 | 7200 | $1$ |
| O11 | 7 | 2 | 3600 | $C_2$ |
| O12 | 8 | 1 | 7200 | $1$ |
| O13 | 8 | 1 | 7200 | $1$ |
| O14 | 8 | 2 | 7200 | $1$ |
| O15 | 9 | 1 | 2400 | $1$ |
| O16 | 9 | 2 | 7200 | $1$ |
| O17 | 9 | 5 | 1440 | $C_5$ |
| O18 | 10 | 1 | 7200 | $1$ |
| O19 | 10 | 1 | 7200 | $1$ |
| O20 | 10 | 2 | 7200 | $1$ |
| O21 | 11 | 1 | 7200 | $1$ |
| O22 | 11 | 2 | 3600 | $C_2$ |
| O23 | 12 | 2 | 7200 | $1$ |
| O24 | 13 | 1 | 7200 | $1$ |
| O25 | 13 | 2 | 3600 | $C_2$ |
| O26 | 14 | 1 | 7200 | $1$ |
| O27 | 14 | 1 | 7200 | $1$ |
| O28 | 14 | 5 | 1440 | $C_5$ |
| O29 | 15 | 1 | 2400 | $1$ |
| O30 | 15 | 2 | 7200 | $1$ |
| O31 | 16 | 1 | 7200 | $1$ |
| O32 | 17 | 2 | 3600 | $C_2$ |
| O33 | 18 | 1 | 2400 | $1$ |
| O34 | 19 | 20 | 120 | $A_5$ |

These data satisfy
$$
\sum_oN_o=176520,\qquad
600+\sum_ok_oN_o=259800.
$$
The $600$ fixed centers are $600$ singleton position orbits, not another moving orbit.

**F — Full local cap action.** Every cap admits its full tetrahedral rotation group $A_4$, acting on the same complete subdivision. The chosen $H_c$ is a nonidentity half-turn and $T_c$ is a third-turn in that action.

**I — Retained generator-invariant certificate.** In the transported coordinates constructed below, each chosen primitive has even positional permutation separately in every moving orbit and zero total orientation image in each orbit’s abelianization. This is a hypothesis about every generator, not an inference from sample scrambles.

**C — Nine pure-controller certificates.** For
$$
\mathcal I=\{1,4,5,7,18,19,27,31,33\},
$$
each orbit has a legal word whose complete sticker permutation is a single three-cycle in that orbit and fixes every other sticker. A verified legal relocation produces a cycle $(A_o\,B_o\,C_o)$. For every other position $X$ in that orbit, a verified legal setup fixes the two buffer sticker slots $A_o,B_o$ pointwise and sends $C_o$ to $X$. Coverage means valid transitions and coverage of every nonbuffer destination, not merely a reported search-node count.

The solved coloring has $600$ distinct home-cell symbols, with six copies of each in O01, four in O33, and twelve in each of the other seven selected orbits.

**E — Equivariant geometric realization.** For the extended symmetry action, the entire retained subdivision and its piece grouping are preserved by the geometric action of $K$. Each center slot is geometrically distinguished at its corresponding pole $n_c$, and geometric permutations act on the same labelled sticker set as the legal moves. In particular, this is a faithful geometric realization, not an action on a collapsed display quotient.

Hypotheses R, F, I, C, and E have different roles. In particular, C does not certify I, and neither certifies a reconstruction of all cuts. The supplied arithmetic and controller summaries are retained evidence for these hypotheses; their reported pass fields do not replace their full hypotheses.

No independent derivation of the $433$ retained regions per tetrahedron is claimed.

# Lemma 6: Cap actions, signatures, and piece equivariance

## statement

Under R and F, $H_c,T_c$ generate the full $A_4$ action at each cap. The sticker-to-piece map
$$
\rho:\mathcal S\longrightarrow\mathcal P
$$
is equivariant under every legal move and hence every legal word:
$$
\rho(g(s))=\bar g(\rho(s)).
$$
Complete cap signatures identify the cut-chamber positions in this model, and
$$
r(A)=|M(A)|-1,\qquad
k(A)=|\operatorname{Host}(A)|.
$$
The quantity $r(A)$ is neither a topological dimension nor a codimension.

## proof

The three nonidentity half-turns of a tetrahedron form the nonidentity elements of a normal Klein four subgroup of $A_4$. Conjugation by any third-turn cycles these three elements. Thus $H_c$ and its conjugates by $T_c$ generate the Klein four group, and adjoining $T_c$ gives all twelve elements of $A_4$.

All stickers of a physical piece travel together under a legal rigid cap rotation by R. Therefore the image piece $\bar g(A)$ is well defined independently of the choice of sticker in $A$, and the equivariance equation follows. The equation is preserved under composition and inversion.

A full cap signature specifies every strict side choice in the cut arrangement. The corresponding chamber is an intersection of open half-spaces with $\operatorname{int}\widetilde P$, so it has at most one connected component. Thus two different chamber positions cannot have the same complete signature. A hosting patch is an intersection of the chamber closure with a facet, with its lower-dimensional boundaries removed; the cut construction gives at most one such patch per hosting facet.

Consequently $(M(A),h)$ addresses a sticker slot and $\rho$ forgets $h$. This justification depends on the complete cut-chamber model. In an arbitrary grip-set representation, signature injectivity would require a separate hypothesis.

For a global geometric symmetry $k$, cap and host labels are permuted:
$$
M(kA)=kM(A),\qquad
\operatorname{Host}(kA)=k\operatorname{Host}(A).
$$
A local twist need not act on every piece signature by one common permutation of all cap labels; the destination signature is determined by the actual destination chamber.

The two numerical formulas are the definitions of application rank and sticker multiplicity. They count memberships and hosting patches. Physical cut chambers have four-dimensional interiors, and sticker interiors are three-dimensional, while the retained ranks range up to $19$. Thus rank cannot be either dimension or codimension in the ambient four-space.

Nor does rank identify a move orbit: for example, O04 and O05 have the same rank and sticker multiplicity but are distinct retained orbits. Twenty corner stickers also do not imply a cyclic orientation variable of order twenty; the specified corner frame group is $A_5$. $\square$

# Lemma 7: Transported frames and the ambient wreath product

## statement

Let
$$
G=\langle H_c,T_c:c\in\mathcal C\rangle\le S_{\mathcal S}.
$$
Under R, coherent transported frame choices give an injective homomorphism
$$
G\hookrightarrow
\prod_{o=0}^{34}\left(H_o^{N_o}\rtimes S_{N_o}\right).
$$
With chronological multiplication, the coordinates satisfy
$$
(p,a)(r,b)=(r\circ p,c),
\qquad c_i=a_i b_{p(i)}.
$$

## proof

Fix one moving orbit. Choose a reference position and a reference ordering of its stickers. Choose a legal transporter from the reference position to every other position. These transporters identify all sticker fibers with a common reference fiber.

For any legal map taking position $i$ to position $j$, composing with the chosen transporters gives a return map on the reference fiber. Its faithful permutation action belongs to the retained local frame group $H_o$. This constructs coherent coordinates; arbitrary independent screen orderings would not automatically have this property.

Write the position permutation as $p$, and the frame increment at source position $i$ as $a_i\in H_o$. In the right-action convention, an abstract frame state transforms by
$$
(i,h)\longmapsto(p(i),ha_i).
$$
Applying $(p,a)$ and then $(r,b)$ gives
$$
(i,h)\longmapsto
\bigl(r(p(i)),\,h a_i b_{p(i)}\bigr).
$$
This proves the multiplication law, including its reindexing.

Every legal move preserves each of its piece orbits, so the construction applies independently to the $35$ moving orbits. If all resulting coordinates are trivial, every moving sticker is fixed, because each fiber action is faithful. The center stickers are fixed by R. Hence the original sticker permutation is the identity, proving injectivity.

Changing the coherent reference frames conjugates the coordinate realization; it does not enlarge the legal group.

For a source-to-destination sticker permutation $g$, a labelled state array obeys
$$
L'[g(s)]=L[s].
$$
This is consistent with the chronological convention used throughout. $\square$

# Lemma 8: Necessary invariants and the conditional upper bound

## statement

Under R and I,
$$
|G|
\le
\frac{\displaystyle\prod_{o=0}^{34}N_o!\,|H_o|^{N_o}}
{2^{43}5^2}.
$$
The denominator is the exact index of the stated invariant kernel in the ambient product. Equality with $|G|$ is not asserted.

## proof

Let
$$
H_o^{\mathrm{ab}}=H_o/[H_o,H_o],
\qquad
\chi_o:H_o\to H_o^{\mathrm{ab}}.
$$
Write the abelian quotient additively and define
$$
\Phi_o(p,a)=\sum_i\chi_o(a_i).
$$
Lemma 7 gives
$$
\begin{aligned}
\Phi_o((p,a)(r,b))
&=\sum_i\chi_o(a_i b_{p(i)})\\
&=\sum_i\chi_o(a_i)+\sum_i\chi_o(b_{p(i)})\\
&=\Phi_o(p,a)+\Phi_o(r,b).
\end{aligned}
$$
Thus $\Phi_o$ is a homomorphism. Positional sign is also a homomorphism because
$$
\operatorname{sgn}(r\circ p)=\operatorname{sgn}(r)\operatorname{sgn}(p).
$$
Hypothesis I says that every generator lies in the kernels of these maps, so every legal word does.

The local abelianizations are
$$
C_2^{\mathrm{ab}}=C_2,\quad
C_5^{\mathrm{ab}}=C_5,\quad
D_5^{\mathrm{ab}}=C_2,\quad
A_5^{\mathrm{ab}}=1.
$$
For $D_5$, the quotient by $\langle R\rangle$ is $C_2$, while
$$
[R,F]=R^2
$$
generates $\langle R\rangle$. Thus $[D_5,D_5]=\langle R\rangle$.

For completeness, $A_5$ is perfect: take
$$
u=(1\,2)(4\,5),\qquad v=(2\,3)(4\,5).
$$
Their commutator is a three-cycle. The derived subgroup is normal, and all three-cycles are conjugate within $A_5$. Since three-cycles generate $A_5$, its derived subgroup is all of $A_5$.

There are thirty-five positional sign constraints, seven $C_2$ orientation constraints, one $D_5$ orientation constraint, and two $C_5$ constraints.

These constraints are independent in the ambient product. Positional parity can be prescribed using a transposition and identity frame increments. Each orientation quotient value can be prescribed using one frame coordinate and the identity positional permutation. Choices in different orbits are independent. Therefore the joint invariant map is onto a group of order
$$
2^{35}\cdot2^7\cdot2\cdot5^2=2^{43}5^2.
$$
Its kernel has precisely this index. Lemma 7 and hypothesis I place $G$ inside that kernel, proving the bound.

A reflection-type element of the natural five-point $D_5$ action exchanges two pairs and is even as a sticker permutation. Its abelianization bit therefore cannot be replaced by ordinary sticker sign. Likewise, an odd positional permutation in one orbit cannot cancel an odd positional permutation in another under I.

The bound leaves open additional restrictions and reachability sufficiency. $\square$

# Lemma 9: Pure stars generate independent alternating actions

## statement

Under R and C, $G$ contains a subgroup acting as
$$
A_{3600}\times A_{2400}\times A_{7200}^{\,7}
$$
on the nine selected single-sticker orbits and fixing every other sticker.

## proof

Words execute from left to right. Let
$$
t_C=(A\,B\,C)
$$
be a relocated pure controller. If $S$ fixes $A,B$ and sends $C$ to $X$, then the chronological word
$$
S^{-1}t_CS
$$
has ordinary functional action $S\circ t_C\circ S^{-1}$, so it is exactly
$$
t_X=(A\,B\,X).
$$
Conjugating the complete pure permutation gives a complete pure permutation. Temporary collateral during its legal witness is irrelevant to this net support statement.

For distinct nonbuffer positions $X,Y$, direct evaluation gives
$$
t_Y^{-1}t_X=(A\,Y\,X).
$$
Together with the original stars and their inverses, these supply every three-cycle containing $A$. For distinct $x,y,z\ne A$,
$$
(A\,x\,y)(A\,z\,x)=(x\,y\,z).
$$
Thus the stars generate every three-cycle.

Every even permutation is a product of three-cycles. For example, express it as an even number of transpositions and pair them; a pair sharing a point is a three-cycle, identical transpositions cancel, and a disjoint pair can be rewritten as
$$
(a\,b)(c\,d)=(a\,b\,c)(a\,d\,c)
$$
in the chronological convention. Conversely, every star is even. The supported subgroup generated on this orbit is therefore exactly $A_N$.

The nine supported subgroups have disjoint full sticker supports. They commute, and the multiplication map from their direct product is injective: restricting an identity product to one selected orbit makes that factor the identity. This proves the claim.

This identifies a supported subgroup of $G$. It does not identify the full legal group or its actions on the other twenty-six moving orbits. $\square$

# Lemma 10: Balanced-color lower bound

## statement

Under R and C, the set $\Omega$ of reachable fixed-frame face-color states satisfies
$$
|\Omega|\ge B,
$$
where
$$
B=
\frac{3600!}{(6!)^{600}}\,
\frac{2400!}{(4!)^{600}}\,
\left[\frac{7200!}{(12!)^{600}}\right]^7.
$$
These states can be attained with every unselected orbit solved at macro boundaries.

## proof

Consider one selected orbit with $N=600m$ positions and $m$ copies of each color. The stabilizer of its solved coloring in $S_N$ is
$$
Q=(S_m)^{600}.
$$
Since $m\in\{4,6,12\}$, this stabilizer contains a transposition of two equal-colored positions and hence an odd permutation. Therefore
$$
|A_N\cap Q|=\frac{|Q|}{2}.
$$
The number of colorings in the $A_N$ orbit is
$$
\frac{|A_N|}{|A_N\cap Q|}
=
\frac{N!/2}{(m!)^{600}/2}
=
\frac{N!}{(m!)^{600}}.
$$

The odd same-color transposition is used only to describe the coloring stabilizer. It need not itself be a legal move.

Lemma 9 gives independent alternating actions with full purity, so the nine factors multiply and every unselected sticker remains solved at the ends of the resulting macros. This gives $B$.

A labelled state distinguishes every home sticker identity and is determined by an element of $G$. A face-color state forgets distinctions between stickers with the same home-cell symbol. Thus $B$ is a lower bound for colored states, whereas Lemma 8 is an upper bound for labelled states.

The colors here are $600$ distinct mathematical symbols in a fixed spatial frame. Repeated display RGB values or additional quotienting by spatial and symbol symmetries would define a different state set and require a separate count. $\square$

# Lemma 11: Word-ball and total-variation bounds

## statement

Under R and C, use the unit-cost alphabet
$$
\mathcal A=\{H_c,T_c,T_c^{-1}:c\in\mathcal C\},
\qquad q=1800.
$$
Some reachable face-color state has distance at least $46648$ from solved.

If $\mu_{1000}$ is any distribution produced by $1000$ successive letters of this alphabet from solved and $\nu$ is uniform on $\Omega$, then
$$
\|\mu_{1000}-\nu\|_{\mathrm{TV}}
>1-10^{-148595}.
$$

## proof

At most $q^j$ words have length $j$. Different words may give the same state, so
$$
|\{x:d(\mathrm{solved},x)\le d\}|
\le
\sum_{j=0}^dq^j
=
\frac{q^{d+1}-1}{q-1}.
$$

The numerical comparisons needed here are the exact integer inequalities
$$
1800^{46648}-1<1799B,
\tag{1}
$$
and
$$
1800^{1000}\,10^{148595}<B.
\tag{2}
$$
They can be evaluated without logarithms: form the factorials in $B$ as integer products, perform the indicated exact multinomial divisions, and compare positive integers. These comparisons were independently recomputed for this draft. As an arithmetic cross-check,
$$
1800^{46649}-1\ge1799B.
$$
The cross-check says only when this counting argument stops excluding coverage.

By (1), the radius-$46647$ ball contains fewer than $B$ states. Lemma 10 gives at least $B$ reachable states, so some state lies outside that ball.

Let $A$ be the support of $\mu_{1000}$. Then
$$
|A|\le1800^{1000},\qquad \mu_{1000}(A)=1.
$$
Using the definition
$$
\|\mu-\nu\|_{\mathrm{TV}}
=\max_E|\mu(E)-\nu(E)|,
$$
we obtain
$$
\begin{aligned}
\|\mu_{1000}-\nu\|_{\mathrm{TV}}
&\ge1-\frac{|A|}{|\Omega|}\\
&\ge1-\frac{1800^{1000}}{B}\\
&>1-10^{-148595},
\end{aligned}
$$
where strictness follows from (2). No stationarity, independence, or uniform choice of letters is needed for this support argument.

These are statements about a specified alphabet and fixed-frame face-color states. Macro applications, simultaneous native commands, and camera motions are not single letters in this metric.

The distance bound is existential, not a lower bound for every scramble. A known $1000$-letter scramble has an inverse solution of at most $1000$ letters because $\mathcal A$ is inverse closed.

If a valid finite symmetry group $L$ acts on the chosen state set, quotienting may reduce counts: generally
$$
|\Omega/L|\ge\frac{|\Omega|}{|L|},
$$
rather than preserving $|\Omega|$. A quotient’s goals and metric must also be specified before transferring distance claims. $\square$

# Lemma 12: Geometric normalization of the legal group

## statement

Under R, F, and E, the geometric action of $K$ normalizes $G$, its intersection with $G$ is trivial, and
$$
\langle G,K\rangle\cong G\rtimes K.
$$

## proof

Let $k\in K$. Orthogonality and equivariance of poles give
$$
k(U_c)=U_{kc}.
$$
Let $R_{c,a}$ denote a legal cap rotation corresponding to $a\in A_4$: it rotates the cap and fixes its complement. On the complete subdivision,
$$
kR_{c,a}k^{-1}=R_{kc,kak^{-1}}.
$$
The restriction $kak^{-1}$ fixes the new cap axis and is a proper rotation on its perpendicular three-space. This remains true when $\det k=-1$, because determinant is preserved by conjugation. It is consequently an element of the new tetrahedron’s $A_4$ rotation group.

By F and Lemma 6, every such local rotation is a finite word in $H_{kc},T_{kc}$. Therefore
$$
kGk^{-1}\subseteq G.
$$
Applying the same argument to $k^{-1}$ gives equality.

Now suppose $g=k$ as sticker permutations, with $g\in G$ and $k\in K$. Every center slot is fixed by $g$, hence by $k$. Hypothesis E makes this mean
$$
kn_c=n_c\qquad\text{for every }c.
$$
The poles span $\mathbb R^4$. Otherwise a nonzero vector orthogonal to every pole would give an unbounded line in the intersection of the facet half-spaces, contradicting boundedness of $\widetilde P$. Thus $k$ fixes a spanning set and is the identity. Since $G$ is a subgroup of the sticker symmetric group, its action is faithful, and $g$ is also the identity.

Normalization and trivial intersection imply that every element of $\langle G,K\rangle$ has a unique form $gk$. Multiplication is governed by the conjugation action of $K$ on $G$, giving the asserted internal semidirect product.

The faithfulness requirements are essential. An action on a display quotient that identifies centers, or a geometric representation without a distinguished spanning center set, would not justify the intersection argument. $\square$

# Distinct symmetry and motion groups

The groups appearing above serve different purposes.

| Group | Action and role |
|---|---|
| $SO(4)$ | Continuous orientation-preserving changes of four-dimensional viewing frame |
| $\operatorname{Sp}(1)\times\operatorname{Sp}(1)$ | Double cover of $SO(4)$, with diagonal $\{\pm(1,1)\}$ kernel |
| $K^+$ | The $7200$ orientation-preserving symmetries preserving the finite polytope |
| $K$ | All $14400$ geometric symmetries of the polytope |
| $A_4$ at one cap | Its twelve local tetrahedral rotations |
| $G$ | The finite group of legal permutations of the retained sticker slots |

To see the continuous covering directly, take $A\in SO(4)$ and put $p=A(1)$. The map $L_{p^{-1}}\circ A$ fixes $1$ and restricts to an element of $SO(3)$ on the imaginary quaternions. Every such three-dimensional rotation is $x\mapsto qxq^{-1}$ for a unit quaternion $q$, so $A$ has a unit-quaternion-pair representation. The kernel calculation is the same as in Lemma 4.

A generic camera rotation does not preserve the finite set $V$. A local cap turn generally is not a global rigid symmetry. Under C, the lower bound $B>10^{151851}$ alone already separates the scale of the legal action from the finite geometric group.

# Lemma 13: Conditions for sound symmetry reduction

## statement

Normalization of $G$ by $K$ does not imply preservation of the chosen $1800$-symbol word metric.

A symmetry quotient preserves shortest-path distance to a goal set if the symmetries act by cost-preserving automorphisms of the actual allowed state graph, preserve the goal set and the required protection conditions, and quotient paths retain sufficient information to lift to actual labelled paths.

## proof

Normalization says that a conjugated generator is an element of $G$. It says nothing about its length in $\mathcal A$.

Already in one tetrahedral group, take
$$
H=(1\,2)(3\,4),\qquad T=(1\,2\,3).
$$
Conjugating $H$ by $T$ gives a different half-turn. This is none of the three chosen local symbols $H,T,T^{-1}$, although it is a finite word in them. Thus the local generating set is not invariant under all local conjugations. Global geometric symmetry supplies no automatic remedy for this failure. A separate audit would be needed to show preservation of the particular global alphabet and its primitive costs.

Now let $\Gamma$ be the actual allowed state graph, with edge costs, and let $L$ act by cost-preserving graph automorphisms. Suppose the goal set is $L$-invariant. The graph must already encode the relevant allowed states, protected buffers or frames, legal generators, and any restrictions on intermediate behavior.

Projection sends every path to a quotient path of the same cost. Conversely, suppose an edge of a quotient path is represented by $u\to v$, while the current lifted state is $x=\ell u$. The edge lifts to
$$
x=\ell u\longrightarrow\ell v
$$
with the same cost, because $\ell$ is a graph automorphism. Repeating this lifts the entire path. If its quotient endpoint is a goal orbit, the lifted endpoint is a goal because the goal set is invariant. Therefore the quotient and original minimum costs agree.

To implement the lift, one must retain a representative edge, its actual move, and the symmetry needed to align its source with the current lifted state. Ordered frames, labels, and buffer identities must be transported as part of this information.

For fixed-frame solved colors, a geometric permutation of positions alone need not preserve the solved coloring or even the reachable color set: it moves centers with distinct colors. A combined position action and matching permutation of home-cell symbols may preserve solved, but that is a separately defined action.

Likewise, setwise preservation of two buffers is insufficient when the task protects them individually with ordered frames. The acting subgroup must preserve the actual requirement or carry explicitly tracked buffer data.

Without these conditions, a quotient may change the goal, admit forbidden transitions, reduce costs artificially, or lose the data needed to reconstruct a legal labelled solution. $\square$

# Lemma 14: Complete endpoint transport and certified caching

## statement

Let $W$ and $S$ be verified legal sticker permutations, with chronological witnesses. Suppose the complete nonfixed map of $W$ is
$$
E_W=\{(u,W(u)):W(u)\ne u\}.
$$
Then the complete nonfixed map of the chronological conjugate $S^{-1}WS$ is
$$
E_{S^{-1}WS}
=
\{(S(u),S(W(u))):(u,W(u))\in E_W\}.
$$
Thus transporting both endpoints computes exactly the same full permutation as replaying the conjugated legal witness, including all collateral.

## proof

As an ordinary function, the chronological word $S^{-1}WS$ is
$$
S\circ W\circ S^{-1}.
$$
For each $u$,
$$
(S\circ W\circ S^{-1})(S(u))=S(W(u)).
$$
Since $S$ is bijective, $S(W(u))\ne S(u)$ exactly when $W(u)\ne u$. Every moved source of the conjugate is therefore represented, and no fixed source is included.

This proves equality on every sticker, not merely on a target orbit. Collateral cycles are transported by exactly the same formula.

Consequently a cached full map can be certified by a complete seed map, a verified setup witness, the endpoint transport calculation, and consistent permutation conventions. Cache identity must distinguish the underlying model, seed, and setup data sufficiently to avoid substituting a different map.

The result licenses exact reuse of a permutation. It gives no numerical performance guarantee and does not shorten the retained primitive witness. $\square$

# Lemma 15: Guarded setup graphs and legal inverses

## statement

Fix two buffer positions with their sticker frames pointwise protected. On the finite graph of ordered target sticker frames, enlarging the positive guarded alphabet by legal inverses cannot increase any minimum unit-cost setup length.

For nonnegative unequal edge costs, minimum cost must be computed by a suitable weighted shortest-path algorithm rather than ordinary breadth-first search.

## proof

A vertex records a complete ordered target frame
$$
f=(s_1,\ldots,s_{k_o})
$$
at a nonbuffer position. An allowed primitive $g$ induces the transition
$$
f\longmapsto(g(s_1),\ldots,g(s_{k_o})).
$$
Allowed moves fix both buffer sticker fibers pointwise. In the retained cap guard, forbidding every cap in
$$
M(A)\cup M(B)
$$
is a sufficient condition for this fixed-buffer property.

Let $E_+$ be the edges arising from allowed positive moves. Adding their legal inverses gives an edge set $E_{\pm}$ with
$$
E_+\subseteq E_{\pm}.
$$
Every positive setup path is still available with the same unit cost. Thus, with infinite distance allowed for unreachable vertices,
$$
d_{\pm}(f_0,f)\le d_+(f_0,f).
$$

In this setting inverse moves also fix the buffers. Moreover, $H_c^{-1}=H_c$ and $T_c^{-1}=T_c^2$, so inverse transitions already have positive legal witnesses. Adding inverse letters can shorten paths in the chosen alphabet without needing to enlarge its reachable component.

For unit costs, breadth-first search explores vertices in nondecreasing path length and finds minimum setup lengths in that graph. If costs are nonnegative but unequal, path length is no longer path cost. A weighted algorithm, such as Dijkstra’s algorithm, is appropriate. Adding inverse edges still cannot increase minimum cost provided old edges retain their costs.

These statements compare precisely specified graphs. They do not claim strict improvement, numerical speedup, or global optimality among different buffer choices or setups allowed to move the buffers temporarily. $\square$

# Scope tests: collateral, commutation, and quotient equality

Two elementary examples expose invalid stronger claims.

First, on six stickers the permutations
$$
u=(1\,2\,3),
\qquad
v=(1\,2\,3)(4\,5\,6)
$$
have identical restrictions to the target orbit $\{1,2,3\}$ but different complete actions. Both are even. Equality in a target-orbit quotient therefore does not justify replacing a full-state macro. One must prove equality on the whole state, or explicitly justify the collateral difference under the application’s current conditions.

Second, consider
$$
a=(1\,2)(5\,6),
\qquad
b=(3\,4)(6\,7).
$$
If only stickers $1,2,3,4$ are displayed, their visible supports are disjoint. But their full actions do not commute: applying $a$ then $b$ sends $5$ to $7$, whereas applying $b$ then $a$ sends $5$ to $6$.

By contrast, permutations with disjoint full supports commute. Each permutation fixes the support of the other pointwise, and a permutation maps its own support to itself; checking a point in either support or outside both gives equality of the two compositions.

These examples refute the stronger quotient and visible-support claims. They are consistent with, and explain the necessity of, the complete-support hypotheses in Lemmas 9 and 14.

# Theorem: Full 600-cell theory package

## statement

The following is the complete original task statement. Its retained-model clauses are interpreted with the explicit hypotheses R, F, I, C, and E above: R and F specify the retained puzzle; I supplies only the invariant upper bound; C supplies the alternating subgroup, color lower bound, and counting consequences; E supplies the geometric extended action. The geometric conclusions in parts (1) and (2) do not depend on these retained-model hypotheses.

**Full 600-cell: geometry, topology, legal actions, and sound optimization**

Prove the following theory package, with explicitly separated unconditional geometry and conditional retained-model consequences.

Let phi=(1+sqrt(5))/2. Let V be the 120 vectors in R^4 consisting of all permutations of (plus-or-minus 2,0,0,0), all sixteen (plus-or-minus 1,plus-or-minus 1,plus-or-minus 1,plus-or-minus 1), and the even coordinate permutations of (0,plus-or-minus 1,plus-or-minus phi,plus-or-minus phi^(-1)), with independent signs. Set P=conv(V). Write K=Isom(P) for its linear geometric symmetry group, and K+ for the subgroup with positive determinant.

(1) P is the regular 600-cell {3,3,5}, with circumradius 2, edge length 2/phi, f-vector (120,720,1200,600), tetrahedral facets, five facets at each edge, twenty facets at each vertex, and icosahedral vertex links. Its boundary is homeomorphic to S^3, has Euler characteristic zero, integral homology Z in dimensions 0 and 3 and zero in dimensions 1 and 2, and trivial fundamental group. Give a radial-homeomorphism argument; equality of Betti numbers alone is insufficient. Explain the duality with the 120-cell. The supplied exact arithmetic checker may support the explicit finite enumeration, but explain its finite algorithm and limitations instead of treating a PASS flag as a proof of every conclusion.

(2) Identify V/2 with the binary icosahedral group I* of 120 unit quaternions. Its left/right action x -> p x q^(-1) gives K+ isomorphic to (I* x I*)/<(-1,-1)> of order 7200; adding quaternion conjugation gives K of order 14400, the Coxeter group W(H4). State its Coxeter presentation with chain labels 3,3,5. Distinguish these finite symmetries from the continuous SO(4) camera group (double covered by unit-quaternion pairs), from a tetrahedral cap's A4 rotation group, and from the enormous legal twist group below. Justify the upper symmetry count as well as exhibiting that many maps.

(3) Consider the retained full-cut surface puzzle on P, scaled/rotated into the cell-pole convention n0=(phi^3,1,1,1), with cell hyperplanes n_c dot x=||n_c||^2 and outer caps n_c dot x>(121/125)||n_c||^2. There are 600 oriented cap poles, equivalent to 300 antipodal axes with two outer layers; the middle layer is fixed. Assume the cut subdivision and its legal cap rotations give the retained 259800 sticker slots, 177120 pieces (including 600 fixed centers), and 35 moving orbits, with the local frame groups and sizes in the reference census. At every cap the retained half-turn H_c and third-turn T_c generate the full A4 cap rotation group. Give a rigorous description of the sticker-to-piece equivariance, cap signatures and hosting sets, and explain why the application rank |M|-1 is not the topological dimension or codimension of a piece. No independent reconstruction of the 433 cut regions per tetrahedron is supplied here; retain that boundary explicitly.

(4) Let G be the finite sticker permutation group generated by the 600 H_c and 600 T_c. Show that, after coherent transported frame choices, G embeds in the product over the 35 moving orbits of H_o^(N_o) semidirect S_(N_o). Explain the chronological multiplication rule (p,a)(r,b)=(r composed with p,c), c_i=a_i b_(p(i)), orientation abelianization sums, and positional parity. Under the retained generator-invariant certificate, derive the ambient upper bound |G| <= product(N_o! |H_o|^N_o)/(2^43*5^2). This is an upper bound, not an exact legal-group order or a proof that the listed invariants are sufficient. Under the separately audited nine pure single-sticker star-controller hypotheses, prove the independent alternating actions and balanced-color lower bound B=[3600!/(6!)^600][2400!/(4!)^600][7200!/(12!)^600]^7. Do not infer the full legal group from these nine orbits.

(5) With the 1800-symbol alphabet consisting of the H_c and T_c^{+/-1}, explain the elementary word-ball bound, the conditional worst-case distance lower bound 46648, and the total-variation lower bound strictly exceeding 1-10^(-148595) for 1000-step walks from solved versus uniform reachable face-color states. The arithmetic is available separately. No lower bound is asserted for every scramble; the inverse of a known 1000-turn scramble is a solution of at most 1000 turns. State the distinction between fixed-frame colors, labelled states, and quotienting by view/symbol symmetries.

(6) Under the geometric action on the same complete sticker subdivision and the full A4 action at every cap, prove that K normalizes G, that G intersects K trivially (the centers are fixed pointwise by G), and that the generated extended action is a semidirect product G semidirect K. State all faithfulness and center-spanning assumptions explicitly. Show why this symmetry does NOT by itself imply that all elements of K preserve the specific chosen 1800-symbol word metric: a conjugated cap half-turn or third-turn can need a different finite H_c,T_c word. A symmetry quotient for shortest-path search is sound only with the required action on goals, protected buffers, allowed generators, and edge costs, and with enough reconstruction information to lift a path to the actual labelled puzzle.

(7) Prove two limited optimization facts. First, transporting both support endpoints through a verified setup computes the same complete conjugated permutation as replaying its legal witness, including collateral; this supports certified map caching. Second, on the finite graph of ordered target sticker frames with both buffers fixed, extending the positive guarded alphabet by legal inverses cannot increase minimum unit-cost setup length; nonnegative unequal costs require a weighted shortest-path algorithm instead of ordinary BFS. Do not promise a numerical speedup. Explain why a pure target-orbit equivalence does not justify replacing a full-state macro with different collateral, and why disjoint full supports suffice for commutation while disjoint visible supports do not.

The main theorem should state this complete package, including every hypothesis and scope limit above. Supporting definitions and lemmas must precede it. This is a rigorous exposition and verification task, not a request to resolve the unknown exact full legal-group order or to claim a new human solve or performance record.

## proof

Lemmas 1–3 establish the unconditional coordinate geometry, face incidences, radial homeomorphism, integral homology, fundamental group, and polar duality required in part (1). The finite-checker discussion specifies the exact enumeration algorithm, the supporting-facet completeness argument, and the conclusions outside its computational scope.

Lemma 4 identifies the quaternion symmetry action, proves its kernel, establishes both the lower and upper symmetry counts, and proves the Coxeter presentation required in part (2). The continuous covering argument and the group-role table distinguish camera motions, finite geometric symmetries, local cap rotations, and legal sticker permutations.

Lemma 5 gives the exact scale conversion for part (3). The retained-model definitions specify the $600$ oriented caps, their $300$ antipodal axes, signatures, hosting sets, and census without claiming an independent cut reconstruction. Lemma 6 proves local $A_4$ generation and piece equivariance and explains the application rank.

Lemma 7 proves the faithful ambient embedding and chronological frame law. Lemma 8 derives the conditional invariant upper bound under I, including independence of its ambient index factors. Lemmas 9 and 10 derive the independent alternating actions and balanced-color lower bound under the separate pure-controller hypothesis C. These establish part (4) without asserting full reachability or an exact legal-group order.

Lemma 11 proves part (5) from the word count, the color lower bound, and explicit integer inequalities. It distinguishes worst-case existence from individual scramble length and distinguishes fixed-frame color states from labelled and symmetry-quotiented states.

Lemma 12 proves part (6)’s normalization, trivial intersection, and semidirect product under the complete geometric realization E. Its proof uses the faithful sticker action and the spanning geometric center set explicitly. Lemma 13 shows why normalization alone is insufficient for the chosen metric and proves the conditions and reconstruction obligation for sound shortest-path symmetry reduction.

Finally, Lemmas 14 and 15 prove the two optimization statements in part (7). The complete-support examples establish the stated limitations on macro substitution and commutation. None of these arguments asserts a numerical speedup, a new solve or performance record, or a determination of the unknown full legal-group order. $\square$