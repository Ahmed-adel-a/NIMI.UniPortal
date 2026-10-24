/** The API's error envelope: { code, message, details[] } (details name the JSON field). */
export interface ApiErrorBody {
  code: string;
  message: string;
  details: { field: string; error: string }[];
}

export class ApiError extends Error {
  readonly status: number;
  readonly body: ApiErrorBody | null;

  constructor(status: number, body: ApiErrorBody | null) {
    super(body?.message ?? `Request failed (${status})`);
    this.status = status;
    this.body = body;
  }

  get code(): string | undefined {
    return this.body?.code;
  }

  /** The messages for one field, e.g. "title" or "rows[3].score". */
  fieldErrors(field: string): string[] {
    return (this.body?.details ?? []).filter((d) => d.field === field).map((d) => d.error);
  }
}

export function isApiErrorBody(value: unknown): value is ApiErrorBody {
  return typeof value === 'object' && value !== null && 'code' in value && 'message' in value;
}
