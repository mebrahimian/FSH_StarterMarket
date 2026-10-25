
import { useState, type FormEvent } from "react";
import { apiFetch } from "@/lib/api-client";
import { Button } from "@/components/ui/button";

export function DisclosuresOnlyPage() {
    const [fromDate, setFromDate] = useState("");
    const [toDate, setToDate] = useState("");
    const [running, setRunning] = useState(false);
    const [message, setMessage] = useState("");

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();

        const datePattern = /^\d{4}\/\d{2}\/\d{2}$/;

        if (
            !datePattern.test(fromDate) ||
            !datePattern.test(toDate) ||
            fromDate > toDate
        ) {
            setMessage("بازه تاریخ شمسی معتبر وارد کن.");
            return;
        }

        setRunning(true);
        setMessage("");

        try {
            const result = await apiFetch<{ jobId: string }>(
                "/api/v1/marketintelligence/codal/disclosures-only",
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify({
                        fromDate,
                        toDate,
                    }),
                },
            );

            setMessage(
                `Job در Hangfire ثبت شد. JobId: ${result.jobId}`,
            );
        } catch (error) {
            setMessage(
                error instanceof Error
                    ? error.message
                    : "خطا در ثبت Job.",
            );
        } finally {
            setRunning(false);
        }
    }

    return (
        <section className="mx-auto max-w-xl space-y-5 p-6">
            <h1 className="text-xl font-semibold">
                جمع‌آوری Disclosureهای منتخب
            </h1>

            <p className="text-sm text-muted-foreground">
                فقط ذخیره اطلاعات اولیه گزارش‌ها؛ بدون پردازش فروش و پرتفوی.
            </p>

            <form onSubmit={handleSubmit} className="space-y-4">
                <label className="block space-y-1">
                    <span>از تاریخ انتشار</span>
                    <input
                        dir="ltr"
                        value={fromDate}
                        onChange={(e) => setFromDate(e.target.value)}
                        placeholder="1400/01/01"
                        className="w-full rounded-md border p-2"
                        required
                    />
                </label>

                <label className="block space-y-1">
                    <span>تا تاریخ انتشار</span>
                    <input
                        dir="ltr"
                        value={toDate}
                        onChange={(e) => setToDate(e.target.value)}
                        placeholder="1400/12/29"
                        className="w-full rounded-md border p-2"
                        required
                    />
                </label>

                <Button type="submit" disabled={running}>
                    {running ? "در حال ثبت..." : "شروع جمع‌آوری"}
                </Button>
            </form>

            {message && (
                <p className="break-all text-sm">{message}</p>
            )}
        </section>
    );
}
