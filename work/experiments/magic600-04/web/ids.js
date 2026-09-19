// Presentation boundaries for the retained full model. These never relabel it.
export const SLOTS_PER_CELL = 433;

const TYPES = Object.freeze({
  identity: {prefix: 'I', name: 'piece identity', count: 177120, first: 0, example: 35778},
  position: {prefix: 'P', name: 'physical position', count: 177120, first: 0, example: 2712},
  cell: {prefix: 'C', name: 'canonical cell', count: 600, first: 1, example: 113},
  slot: {prefix: 'S', name: 'physical sticker slot', count: 259800, first: 0, example: 48927},
  label: {prefix: 'Sticker', name: 'sticker identity label', count: 259800, first: 0, example: 48927},
  orbit: {prefix: 'O', name: 'moving orbit', count: 35, first: 0, example: 33}
});

function specification(kind) {
  if (!Object.hasOwn(TYPES, kind)) {
    throw new TypeError(`Unsupported ID type ${JSON.stringify(kind)}. A frame node needs its orbit scope; it is not a global piece ID.`);
  }
  return TYPES[kind];
}

function printed(value, kind, type) {
  return type.prefix + (kind === 'orbit' ? String(value).padStart(2, '0') : value);
}

function invalid(value, kind, type, count) {
  const last = type.first + count - 1;
  const example = Math.min(last, Math.max(type.first, type.example));
  const received = typeof value === 'string' ? JSON.stringify(value) : String(value);
  return new TypeError(`Cannot interpret ${received} as a ${type.name}. Expected ${printed(type.first, kind, type)}–${printed(last, kind, type)} (${type.first}–${last}); use the matching prefix or bare decimal in this field, e.g. ${printed(example, kind, type)}.`);
}

function checkedNumber(value, kind, count) {
  const type = specification(kind);
  count ??= type.count;
  if (!Number.isSafeInteger(value) || value < type.first || value >= type.first + count) {
    throw invalid(value, kind, type, count);
  }
  return value;
}

function formatted(value, kind) {
  const type = specification(kind);
  return printed(checkedNumber(value, kind), kind, type);
}

export const formatIdentity = value => formatted(value, 'identity');
export const formatPosition = value => formatted(value, 'position');
export const formatCell = value => formatted(value, 'cell');
export const formatSlot = value => formatted(value, 'slot');
export const formatLabel = value => formatted(value, 'label');
// Fixed is a display sentinel, never an active moving-orbit input.
export const formatOrbit = value => value === -1 ? 'Fixed' : formatted(value, 'orbit');

export function formatFrameNode(orbit, node) {
  checkedNumber(orbit, 'orbit');
  if (!Number.isSafeInteger(node) || node < 0) {
    throw new TypeError('A frame node must be a nonnegative integer within its orbit tree; e.g. O33/node11. It is not a global piece position.');
  }
  // The tree-specific upper bound belongs to model validation, not this formatter.
  return `${formatOrbit(orbit)}/node${node}`;
}

/** Parse a documented, typed text field. Limits override counts, not maximum IDs.
 * Example: parseId('I3', 'identity', {identity: 4}). Prefixes are case-sensitive.
 * Bare decimal is accepted only in the explicit kind supplied by the caller.
 */
export function parseId(text, kind, limits = {}) {
  const type = specification(kind);
  if (!limits || typeof limits !== 'object' || Array.isArray(limits)) {
    throw new TypeError('ID limits must be an object of per-kind counts.');
  }
  const count = Object.hasOwn(limits, kind) ? limits[kind] : type.count;
  if (!Number.isSafeInteger(count) || count < 1 || !Number.isSafeInteger(type.first + count - 1)) {
    throw new TypeError(`The ${kind} limit must be a positive safe integer count.`);
  }
  if (typeof text !== 'string') throw invalid(text, kind, type, count);
  const match = text.trim().match(new RegExp(`^(?:${type.prefix})?([0-9]+)$`));
  if (!match) throw invalid(text, kind, type, count);
  return checkedNumber(Number(match[1]), kind, count);
}

export function cellFromSlot(slot) {
  return Math.floor(checkedNumber(slot, 'slot') / SLOTS_PER_CELL) + 1;
}

export function labCellIndex(canonicalCell) {
  return checkedNumber(canonicalCell, 'cell') - 1;
}
