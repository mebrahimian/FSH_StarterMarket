import { useMutation } from "@tanstack/react-query";
import {
    Download,
    History,
    Play,
} from "lucide-react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";

import {
    collectCodalBackfill,
    collectNewCodalDisclosures,
    parsePendingCodalDisclosures,
} from "@/api/market-intelligence";
import { Button } from "@/components/ui/button";
import { describe } from "@/lib/list-helpers";

const CODAL_OPERATIONS_PERMISSION =
    "Permissions.MarketIntelligence.CodalOperations.Execute";

type CodalOperation =
    | "incremental"
    | "parsePending"
    | "backfill";

export function CodalOperationsPanel({
    onQueued,
}: {
    onQueued?: () => void;
}) {
    

    const { t } =
        useTranslation("disclosures");

    const operationMutation = useMutation({
        mutationFn: (
            operation: CodalOperation,
        ) => {
            switch (operation) {
                case "incremental":
                    return collectNewCodalDisclosures();

                case "parsePending":
                    return parsePendingCodalDisclosures();

                case "backfill":
                    return collectCodalBackfill();
            }
        },
        onSuccess: (response) => {
            toast.success(
                t("codalOperations.queued", {
                    jobId: response.jobId,
                }),
            );

            onQueued?.();
        },
        onError: (error) => {
            toast.error(
                t("codalOperations.failed"),
                {
                    description: describe(error),
                },
            );
        },
    });

    
    const runOperation = (
        operation: CodalOperation,
    ) => {
        if (
            operation === "backfill" &&
            !window.confirm(
                t("codalOperations.confirmBackfill"),
            )
        ) {
            return;
        }

        operationMutation.mutate(operation);
    };

    const activeOperation =
        operationMutation.variables;

    return (
        <section className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 shadow-xs">
            <div className="mb-3">
                <h2 className="text-sm font-semibold text-[var(--color-foreground)]">
                    {t("codalOperations.title")}
                </h2>
            </div>

            <div className="flex flex-wrap gap-2">
                <Button
                    type="button"
                    onClick={() =>
                        runOperation("incremental")
                    }
                    disabled={operationMutation.isPending}
                    className="gap-2"
                >
                    <Download
                        className="size-4"
                        aria-hidden
                    />

                    {operationMutation.isPending &&
                        activeOperation === "incremental"
                        ? t("codalOperations.queueing")
                        : t("codalOperations.collectNew")}
                </Button>

                <Button
                    type="button"
                    variant="outline"
                    onClick={() =>
                        runOperation("parsePending")
                    }
                    disabled={operationMutation.isPending}
                    className="gap-2"
                >
                    <Play
                        className="size-4"
                        aria-hidden
                    />

                    {operationMutation.isPending &&
                        activeOperation === "parsePending"
                        ? t("codalOperations.queueing")
                        : t("codalOperations.parsePending")}
                </Button>

                <Button
                    type="button"
                    variant="outline"
                    onClick={() =>
                        runOperation("backfill")
                    }
                    disabled={operationMutation.isPending}
                    className="gap-2"
                >
                    <History
                        className="size-4"
                        aria-hidden
                    />

                    {operationMutation.isPending &&
                        activeOperation === "backfill"
                        ? t("codalOperations.queueing")
                        : t("codalOperations.backfill")}
                </Button>
            </div>
        </section>
    );
}