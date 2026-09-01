import { cn } from "@/lib/cn";

export function BrandMark({ className }: { className?: string }) {
    return (
        <div className={cn("inline-flex select-none items-center gap-2.5", className)}>
            <img
                src="/branding/sadaf/sadaf-mark-primary.svg"
                alt="صدف بورس"
                className="size-9 object-contain"
            />

            <div className="flex flex-col">
                <span className="whitespace-nowrap font-display text-[15px] font-bold leading-none tracking-tight text-[var(--color-foreground)]">
                    صدف بورس
                </span>

                <span className="mt-0.5 text-[10px] font-semibold uppercase tracking-wider text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.7)]">
                    Admin
                </span>
            </div>
        </div>
    );
}

export function BrandMarkXL({ className }: { className?: string }) {
    return (
        <div className={cn("space-y-3", className)}>
            <div className="flex items-center gap-2.5">
                <img
                    src="/branding/sadaf/sadaf-mark-primary.svg"
                    alt="صدف بورس"
                    className="size-8 object-contain"
                />

                <span
                    dir="rtl"
                    className="font-display text-[18px] font-semibold tracking-tight text-[var(--color-foreground)]"
                >
                    صدف بورس
                </span>

                <span className="font-mono text-[10px] font-medium uppercase tracking-wider text-[var(--color-muted-foreground)]">
                    · platform admin
                </span>
            </div>

            <h1 className="font-display text-[clamp(3rem,7vw,5.5rem)] font-semibold leading-[0.95] tracking-[var(--tracking-display)]">
                Admin<span className="text-[var(--color-primary)]">.</span>
            </h1>

            <p className="max-w-md text-sm leading-relaxed text-[var(--color-muted-foreground)]">
                Manage users, tenants, permissions, and platform operations from one place.
            </p>
        </div>
    );
}