import { useTranslation } from 'react-i18next';
import { ApiError } from '../../api/errors';

/**
 * The message to show for a failed request, always in the app's language. The API writes its messages in English, so
 * English shows them as sent; Arabic shows the translated text for the error code instead.
 */
export function useErrorMessage() {
  const { t, i18n } = useTranslation();
  return (error: unknown): string => {
    if (error instanceof ApiError) {
      if (error.status === 0) return t('errors.network');
      if (i18n.language === 'en' && error.body?.message) {
        const details = error.body.details?.map((d) => d.error).join(' ');
        return details ? `${error.body.message} ${details}` : error.body.message;
      }
      if (error.code && i18n.exists(`errors.codes.${error.code}`)) return t(`errors.codes.${error.code}`);
      if (error.status === 403) return t('errors.forbidden');
      if (error.status === 404) return t('errors.notFound');
    }
    return t('errors.generic');
  };
}
