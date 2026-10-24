import type { ReactNode } from 'react';

/** A simple data table; wide tables scroll inside their own box, never the page. */
export function Table({ head, children }: { head: ReactNode[]; children: ReactNode }) {
  return (
    <div className="overflow-x-auto rounded-xl border border-line bg-surface">
      <table className="w-full min-w-[36rem] border-collapse text-sm">
        <thead>
          <tr className="border-b border-line bg-sunk text-start">
            {head.map((cell, index) => (
              <th key={index} scope="col" className="px-4 py-2.5 text-start text-xs font-semibold text-muted">
                {cell}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="[&>tr]:border-b [&>tr]:border-line [&>tr:last-child]:border-0 [&_td]:px-4 [&_td]:py-2.5">{children}</tbody>
      </table>
    </div>
  );
}
