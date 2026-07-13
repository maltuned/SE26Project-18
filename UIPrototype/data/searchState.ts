/**
 * Simple mutable shared state — the search page writes to this on selection,
 * and the homepage reads + clears it on focus. No global state lib needed.
 */
export const searchState = {
  selectedGameName: null as string | null,
  selectedTagId: null as number | null,
};
