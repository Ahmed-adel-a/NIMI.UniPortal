import { afterEach, describe, expect, it } from 'vitest';
import { renderHook } from '@testing-library/react';
import { ApiError } from '../../api/errors';
import i18n from '../../i18n';
import { useErrorMessage } from './useErrorMessage';

const groupFull = new ApiError(409, { code: 'conflict', message: 'The group is full.', details: [] });
const blankName = new ApiError(400, { code: 'invalid_value', message: 'A value breaks a business rule.', details: [{ field: 'name', error: 'Required.' }] });

function messageFor(error: unknown) {
  const { result } = renderHook(() => useErrorMessage());
  return result.current(error);
}

afterEach(async () => {
  await i18n.changeLanguage('ar');
});

describe('useErrorMessage', () => {
  it('never shows the API’s English text in Arabic', () => {
    expect(messageFor(groupFull)).toBe(i18n.t('errors.codes.conflict'));
    expect(messageFor(blankName)).toBe(i18n.t('errors.codes.invalid_value'));
    expect(messageFor(new ApiError(418, { code: 'teapot', message: 'Short and stout.', details: [] }))).toBe(i18n.t('errors.generic'));
  });

  it('shows the API’s own message and details in English', async () => {
    await i18n.changeLanguage('en');
    expect(messageFor(groupFull)).toBe('The group is full.');
    expect(messageFor(blankName)).toBe('A value breaks a business rule. Required.');
  });

  it('reports an unreachable server in the current language', async () => {
    expect(messageFor(new ApiError(0, null))).toBe(i18n.t('errors.network'));
    await i18n.changeLanguage('en');
    expect(messageFor(new ApiError(0, null))).toBe("Can't reach the server. Make sure the backend is running.");
  });
});
