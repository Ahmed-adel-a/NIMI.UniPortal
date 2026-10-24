import type { components } from './schema';

/** A response or request shape from the API, by its OpenAPI name: Schema<'TermDto'>. */
export type Schema<Name extends keyof components['schemas']> = components['schemas'][Name];
