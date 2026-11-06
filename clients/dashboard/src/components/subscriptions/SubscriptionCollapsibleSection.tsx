import type { ReactNode } from "react";
import { ChevronDown } from "lucide-react";

type Props = {
    title: string;
    summary: string;
    children: ReactNode;
};

export function SubscriptionCollapsibleSection({
    title,
    summary,
    children,
}: Props) {
    return (
        <details className="group rounded-xl border bg-card">
            <summary className="flex cursor-pointer list-none items-center justify-between gap-3 p-4 [&::-webkit-details-marker]:hidden">
                <div className="min-w-0 space-y-1">
                    <div className="font-semibold">
                        {title}
                    </div>
                    <div className="text-sm text-muted-foreground">
                        {summary}
                    </div>
                </div>

                <ChevronDown className="size-5 shrink-0 transition-transform group-open:rotate-180" />
            </summary>

            <div className="border-t p-4">
                {children}
            </div>
        </details>
    );
}