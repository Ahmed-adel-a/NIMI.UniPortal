import { useTranslation } from 'react-i18next';
import { ApiError } from '../../api/errors';

/** The message to show for a failed request: the API's own message when it sent one. */
export function useErrorMessage() {
  const { t } = useTranslation();
  return (error: unknown): string => {
    if (error instanceof ApiError) {
      if (error.status === 0) return t('errors.network');
      if (error.body?.message) {
        const details = error.body.details?.map((d) => d.error).join(' ');
        return details ? `${error.body.message} ${details}` : error.body.message;
      }
      if (error.status === 403) return t('errors.forbidden');
      if (error.status === 404) return t('errors.notFound');
    }
    return t('errors.generic');
  };
}
