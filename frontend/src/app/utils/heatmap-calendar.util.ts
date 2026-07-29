/**
 * heatmap-calendar.util.ts
 *
 * Pure, framework-agnostic calendar data engine for the activity heatmap.
 *
 * Two display modes — each produces the same HeatmapGrid shape:
 *
 *  'year'    — Fixed calendar year (Jan 1 → Dec 31).
 *              First column = week containing Jan 1.
 *              Cells outside the year boundaries are padding (date: null).
 *
 *  'rolling' — 52-week rolling window ending today.
 *              First column = week containing (today − 364 days).
 *              Cells after today in the current week are future (isFuture: true).
 *
 * No DOM, no Angular, no side-effects. Pure input → output.
 */

// ── Public data structures ────────────────────────────────────────────────────

export interface HeatmapCell {
  /** ISO 'YYYY-MM-DD', or null for empty padding cells outside the range */
  date: string | null;
  /** Submission count for this date; 0 for padding / future cells */
  count: number;
  /** Visual intensity level used to pick the cell colour */
  level: 0 | 1 | 2 | 3 | 4;
  /** True when this cell represents today */
  isToday: boolean;
  /** True for cells that are after today (only possible in the current week) */
  isFuture: boolean;
}

export interface HeatmapWeek {
  /** Exactly 7 cells — index 0 = Sunday, index 6 = Saturday */
  cells: HeatmapCell[];
  /**
   * Abbreviated month name ('Jan' … 'Dec') set on the first week that
   * contains a day-1 of that month; null on all other weeks.
   */
  monthLabel: string | null;
}

export interface HeatmapGrid {
  weeks: HeatmapWeek[];
  /** Total submission count across the displayed range */
  totalSubmissions: number;
  /** Count of dates in the range where count > 0 */
  activeDays: number;
  /** Longest consecutive run of active days within the displayed range */
  maxStreak: number;
}

// ── Internal helpers ──────────────────────────────────────────────────────────

const MONTH_ABBR = [
  'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
  'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
] as const;

/** Format a Date as 'YYYY-MM-DD' without timezone conversion */
function toISO(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

/** Return a new Date with the given number of days added (negative = subtract) */
function addDays(d: Date, n: number): Date {
  const r = new Date(d);
  r.setDate(r.getDate() + n);
  return r;
}

/**
 * Walk a date backwards to the nearest Sunday.
 * If it is already Sunday, returns a copy of the same date.
 */
function prevSunday(d: Date): Date {
  const dow = d.getDay(); // 0 = Sun
  return addDays(d, -dow);
}

/**
 * Walk a date forward to the nearest Saturday.
 * If it is already Saturday, returns a copy of the same date.
 */
function nextSaturday(d: Date): Date {
  const dow = d.getDay(); // 6 = Sat
  return addDays(d, (6 - dow + 7) % 7);
}

/** Map a submission count to a colour intensity level */
function countToLevel(count: number): 0 | 1 | 2 | 3 | 4 {
  if (count === 0)  return 0;
  if (count <= 2)   return 1;
  if (count <= 5)   return 2;
  if (count <= 9)   return 3;
  return 4;
}

// ── Month label logic ─────────────────────────────────────────────────────────
/**
 * Given a week's 7 cells, returns the abbreviated month name to display above
 * the week, or null if no month starts in this week.
 *
 * Rules:
 * - A month label is shown on the first week that contains the 1st of the month.
 * - If two months start in the same week (only when a 31-day month ends on Sat),
 *   the later month (closer to Saturday) wins.
 * - The very first week is exempt if its first non-null cell is NOT in column 0
 *   (i.e. the week is a partial padding week at the start of the grid).
 */
function weekMonthLabel(
  cells: HeatmapCell[],
  isFirstWeek: boolean,
): string | null {
  // Collect all day-1 entries in this week (null cells are padding — skip them)
  const monthStarts: Array<{ monthIdx: number; colInWeek: number }> = [];

  for (let i = 0; i < cells.length; i++) {
    const cell = cells[i];
    if (cell.date === null) continue;
    const dayOfMonth = parseInt(cell.date.slice(8, 10), 10);
    if (dayOfMonth === 1) {
      monthStarts.push({ monthIdx: new Date(cell.date + 'T00:00:00').getMonth(), colInWeek: i });
    }
  }

  if (monthStarts.length === 0) return null;

  // If this is the very first week, only show a label when the month-1 falls
  // on Sunday (col 0) — i.e. the week is not a padding week.
  if (isFirstWeek) {
    // Find the first non-null cell position
    const firstRealCol = cells.findIndex(c => c.date !== null);
    // If the month start is before the first real date, it is a padding overlap
    const earliestMonthStart = monthStarts[0];
    if (earliestMonthStart.colInWeek < firstRealCol) return null;
  }

  // When two months start in the same week, pick the later one (higher colInWeek)
  const winner = monthStarts.reduce((a, b) =>
    b.colInWeek > a.colInWeek ? b : a,
  );

  return MONTH_ABBR[winner.monthIdx];
}

// ── Statistics helpers ────────────────────────────────────────────────────────

function computeStats(
  cells: HeatmapCell[],
): { totalSubmissions: number; activeDays: number; maxStreak: number } {
  let totalSubmissions = 0;
  let activeDays = 0;
  let maxStreak = 0;
  let currentStreak = 0;

  for (const cell of cells) {
    if (cell.date === null || cell.isFuture) continue;
    totalSubmissions += cell.count;
    if (cell.count > 0) {
      activeDays++;
      currentStreak++;
      if (currentStreak > maxStreak) maxStreak = currentStreak;
    } else {
      currentStreak = 0;
    }
  }

  return { totalSubmissions, activeDays, maxStreak };
}

// ── Main builder ──────────────────────────────────────────────────────────────

/**
 * Builds the complete HeatmapGrid for the given mode.
 *
 * @param mode         'year' for a fixed calendar year; 'rolling' for a 52-week
 *                     rolling window ending today.
 * @param year         The calendar year to display (only used when mode === 'year').
 *                     Pass null when mode === 'rolling'.
 * @param submissionMap Map of 'YYYY-MM-DD' → submission count for that date.
 * @param today        The reference date for "today" (enables deterministic tests).
 */
export function buildHeatmapGrid(
  mode: 'year' | 'rolling',
  year: number | null,
  submissionMap: Map<string, number>,
  today: Date,
): HeatmapGrid {
  // ── 1. Determine the logical date range (rangeStart … rangeEnd) ──────────
  const todayISO = toISO(today);

  let rangeStart: Date;
  let rangeEnd: Date;

  if (mode === 'year') {
    if (year === null) {
      throw new Error('buildHeatmapGrid: year must be provided when mode is "year"');
    }
    rangeStart = new Date(year, 0, 1);   // Jan 1
    rangeEnd   = new Date(year, 11, 31); // Dec 31
  } else {
    // Rolling: 52 weeks = 364 days. Start 364 days before today.
    rangeEnd   = new Date(today);
    rangeStart = addDays(today, -364);
  }

  // ── 2. Pad out to full Sunday→Saturday weeks ──────────────────────────────
  const gridStart = prevSunday(rangeStart);
  const gridEnd   = nextSaturday(rangeEnd);

  // ── 3. Build flat cell array, then slice into weeks ───────────────────────
  const allCells: HeatmapCell[] = [];
  let cursor = new Date(gridStart);

  while (cursor <= gridEnd) {
    const iso = toISO(cursor);

    // Padding: before rangeStart or after rangeEnd
    const isPadding = cursor < rangeStart || cursor > rangeEnd;

    // Future: after today (only relevant in rolling mode's current week)
    const isFuture = !isPadding && iso > todayISO;

    if (isPadding) {
      allCells.push({ date: null, count: 0, level: 0, isToday: false, isFuture: false });
    } else if (isFuture) {
      allCells.push({ date: iso, count: 0, level: 0, isToday: false, isFuture: true });
    } else {
      const count = submissionMap.get(iso) ?? 0;
      allCells.push({
        date:    iso,
        count,
        level:   countToLevel(count),
        isToday: iso === todayISO,
        isFuture: false,
      });
    }

    cursor = addDays(cursor, 1);
  }

  // ── 4. Slice into 7-cell weeks ────────────────────────────────────────────
  const totalWeeks = allCells.length / 7;
  const weeks: HeatmapWeek[] = [];

  // Track which months have already had their label placed, so we never
  // place the same month twice (guards against edge cases with very short months).
  const labelledMonths = new Set<number>();

  for (let w = 0; w < totalWeeks; w++) {
    const cells = allCells.slice(w * 7, w * 7 + 7);
    const isFirstWeek = w === 0;

    // Find month label candidates
    let monthLabel: string | null = null;
    const candidate = weekMonthLabel(cells, isFirstWeek);

    if (candidate !== null) {
      // Map the abbreviation back to an index to detect duplicates
      const monthIdx = MONTH_ABBR.indexOf(candidate as typeof MONTH_ABBR[number]);
      if (!labelledMonths.has(monthIdx)) {
        labelledMonths.add(monthIdx);
        monthLabel = candidate;
      }
    }

    weeks.push({ cells, monthLabel });
  }

  // ── 5. Compute aggregate stats ────────────────────────────────────────────
  const { totalSubmissions, activeDays, maxStreak } = computeStats(allCells);

  return { weeks, totalSubmissions, activeDays, maxStreak };
}

// ── Convenience: build a submissionMap from ActivityDay[] ────────────────────

/**
 * Converts an ActivityDay array (the shape the backend returns) into the
 * Map<string, number> that buildHeatmapGrid expects.
 *
 * This is a thin adapter — the heavy lifting stays in buildHeatmapGrid.
 */
export function activityDaysToMap(
  days: Array<{ date: string; count: number }>,
): Map<string, number> {
  const map = new Map<string, number>();
  for (const d of days) {
    if (d.count > 0) map.set(d.date, d.count);
  }
  return map;
}
