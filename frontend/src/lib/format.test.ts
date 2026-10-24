import { describe, expect, it } from 'vitest';
import { isoDate, jsDayOf, startOfWeek } from './format';

describe('format', () => {
  it("maps the API's Saturday-first weekday to JavaScript's Sunday-first one", () => {
    expect(jsDayOf(0)).toBe(6); // Saturday
    expect(jsDayOf(1)).toBe(0); // Sunday
    expect(jsDayOf(2)).toBe(1); // Monday
    expect(jsDayOf(6)).toBe(5); // Friday
  });

  it('starts weeks on Saturday', () => {
    expect(isoDate(startOfWeek(new Date(2026, 9, 25)))).toBe('2026-10-24'); // Sunday → the Saturday before
    expect(isoDate(startOfWeek(new Date(2026, 9, 24)))).toBe('2026-10-24'); // Saturday → itself
    expect(isoDate(startOfWeek(new Date(2026, 9, 30)))).toBe('2026-10-24'); // Friday → the same week
  });
});
