import { useId, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes, type TextareaHTMLAttributes } from 'react';

const control =
  'w-full rounded-lg border border-line bg-surface px-3 py-2 text-fg placeholder:text-muted focus:border-primary focus:outline-none aria-[invalid=true]:border-danger';

function Frame({ id, label, hint, errors, children }: { id: string; label: string; hint?: ReactNode; errors?: string[]; children: ReactNode }) {
  return (
    <div className="grid gap-1.5">
      <label htmlFor={id} className="text-sm font-medium">
        {label}
      </label>
      {children}
      {hint && <p className="text-xs text-muted">{hint}</p>}
      {errors?.map((error) => (
        <p key={error} className="text-xs text-danger">
          {error}
        </p>
      ))}
    </div>
  );
}

export function TextField({ label, hint, errors, ...props }: InputHTMLAttributes<HTMLInputElement> & { label: string; hint?: ReactNode; errors?: string[] }) {
  const id = useId();
  return (
    <Frame id={id} label={label} hint={hint} errors={errors}>
      <input id={id} aria-invalid={!!errors?.length} className={control} {...props} />
    </Frame>
  );
}

export function TextArea({ label, hint, errors, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement> & { label: string; hint?: ReactNode; errors?: string[] }) {
  const id = useId();
  return (
    <Frame id={id} label={label} hint={hint} errors={errors}>
      <textarea id={id} rows={4} aria-invalid={!!errors?.length} className={control} {...props} />
    </Frame>
  );
}

export function SelectField({ label, hint, errors, children, ...props }: SelectHTMLAttributes<HTMLSelectElement> & { label: string; hint?: ReactNode; errors?: string[] }) {
  const id = useId();
  return (
    <Frame id={id} label={label} hint={hint} errors={errors}>
      <select id={id} aria-invalid={!!errors?.length} className={control} {...props}>
        {children}
      </select>
    </Frame>
  );
}
