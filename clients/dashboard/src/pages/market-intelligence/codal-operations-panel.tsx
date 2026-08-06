import {
    useEffect,
    useRef,
    useState,
} from "react";
import { useMutation, useQuery, } from "@tanstack/react-query";

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
    getCodalJobStatus,
    parsePendingCodalDisclosures,
} from "@/api/market-intelligence"; 

import { Button } from "@/components/ui/button";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import { describe } from "@/lib/list-helpers";

type CodalOperation =
    | "incremental"
    | "parsePending"
    | "backfill";

export function CodalOperationsPanel({
    onCompleted,
}: {
    onCompleted?: () => void;
}) {
    const [activeJob, setActiveJob] =
        useState<{
            jobId: string;
            operation: CodalOperation;
        } | null>(null);

    const [
        operationToConfirm,
        setOperationToConfirm,
    ] = useState<CodalOperation | null>(
        null,
    );

    const cancelButtonRef =
        useRef<HTMLButtonElement>(null);

    const completedJobRef =
        useRef<string | null>(null);

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
        onSuccess: (response, operation) => {
            setActiveJob({
                jobId: response.jobId,
                operation,
            });

            toast.success(
                t("codalOperations.queued", {
                    jobId: response.jobId,
                }),
            );
                      
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
    const jobStatusQuery = useQuery({
        queryKey: [
            "market-intelligence",
            "codal-job",
            activeJob?.jobId,
        ],
        queryFn: () =>
            getCodalJobStatus(activeJob!.jobId),
        enabled: activeJob !== null,
        refetchInterval: (query) => {
            const status =
                query.state.data?.status;

            if (
                status === "Succeeded" ||
                status === "Failed" ||
                status === "Deleted"
            ) {
                return false;
            }

            return 3000;
        },
    });

    const jobStatus =
        jobStatusQuery.data?.status ??
        (activeJob ? "Enqueued" : null);

    const isJobRunning =
        jobStatus === "Enqueued" ||
        jobStatus === "Scheduled" ||
        jobStatus === "Processing";
    const jobStatusLabel = jobStatus
        ? {
            Enqueued: t(
                "codalOperations.status.enqueued",
            ),
            Scheduled: t(
                "codalOperations.status.scheduled",
            ),
            Processing: t(
                "codalOperations.status.processing",
            ),
            Succeeded: t(
                "codalOperations.status.succeeded",
            ),
            Failed: t(
                "codalOperations.status.failed",
            ),
            Deleted: t(
                "codalOperations.status.deleted",
            ),
            Unknown: t(
                "codalOperations.status.unknown",
            ),
        }[jobStatus]
        : "";
    useEffect(() => {
        const isFinished =
            jobStatus === "Succeeded" ||
            jobStatus === "Failed" ||
            jobStatus === "Deleted";

        if (!isFinished || !activeJob) {
            return;
        }

        if (
            jobStatus === "Succeeded" &&
            completedJobRef.current !==
            activeJob.jobId
        ) {
            completedJobRef.current =
                activeJob.jobId;

            onCompleted?.();
        }

        const timer = window.setTimeout(() => {
            setActiveJob(null);
        }, 3000);

        return () => {
            window.clearTimeout(timer);
        };
    }, [
        activeJob,
        jobStatus,
        onCompleted,
    ]);

    const runOperation = (
        operation: CodalOperation,
    ) => {
        setOperationToConfirm(operation);
    };

    const confirmOperation = () => {
        if (!operationToConfirm) {
            return;
        }

        const operation = operationToConfirm;

        setOperationToConfirm(null);
        operationMutation.mutate(operation);
    };
    const activeOperation =
        operationMutation.variables;

    const confirmationMessage =
        operationToConfirm
            ? {
                incremental: t(
                    "codalOperations.confirmIncremental",
                ),
                parsePending: t(
                    "codalOperations.confirmParsePending",
                ),
                backfill: t(
                    "codalOperations.confirmBackfill",
                ),
            }[operationToConfirm]
            : "";

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
                    disabled={operationMutation.isPending ||
                        isJobRunning}
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
                    disabled={operationMutation.isPending ||
                        isJobRunning}
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
                    disabled={operationMutation.isPending ||
                        isJobRunning}
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
            {activeJob && jobStatus && (
                <div className="mt-4 rounded-lg border border-[var(--color-border)] bg-[var(--color-muted)] p-3">
                    <div className="mb-2 flex items-center justify-between gap-3">
                        <span className="text-sm font-medium text-[var(--color-foreground)]">
                            {jobStatusLabel}
                        </span>

                        <span className="font-mono text-[11px] text-[var(--color-muted-foreground)]">
                            {t("codalOperations.jobNumber", {
                                jobId: activeJob.jobId,
                            })}
                        </span>
                    </div>

                    <div
                        role="progressbar"
                        aria-label={t(
                            "codalOperations.progressLabel",
                        )}
                        className="h-2 overflow-hidden rounded-full bg-[var(--color-border)]"
                    >
                        {jobStatus === "Succeeded" ? (
                            <div className="h-full w-full rounded-full bg-[var(--color-success)]" />
                        ) : jobStatus === "Failed" ||
                            jobStatus === "Deleted" ? (
                            <div className="h-full w-0 rounded-full bg-[var(--color-destructive)]" />
                        ) : (
                            <div className="h-full w-1/2 animate-pulse rounded-full bg-[var(--color-primary)]" />
                        )}
                    </div>

                    {jobStatusQuery.isFetching &&
                        isJobRunning && (
                            <p className="mt-2 text-xs text-[var(--color-muted-foreground)]">
                            {t("codalOperations.checkingStatus")}                            </p>
                        )}

                    {jobStatusQuery.isError && (
                        <p className="mt-2 text-xs text-[var(--color-destructive)]">
                            {t(
                                "codalOperations.statusUnavailable",
                            )}
                        </p>
                    )}
                </div>
            )}
            <Dialog
                open={operationToConfirm !== null}
                onOpenChange={(open) => {
                    if (!open) {
                        setOperationToConfirm(null);
                    }
                }}
            >
                <DialogContent
                    onOpenAutoFocus={(event) => {
                        event.preventDefault();
                        cancelButtonRef.current?.focus();
                    }}
                >
                    <DialogHeader className="text-start">
                        <DialogTitle>
                            {t(
                                "codalOperations.confirmTitle",
                            )}
                        </DialogTitle>

                        <DialogDescription>
                            {confirmationMessage}
                        </DialogDescription>
                    </DialogHeader>

                    <DialogFooter>
                        <Button
                            ref={cancelButtonRef}
                            type="button"
                            autoFocus
                            onClick={() =>
                                setOperationToConfirm(null)
                            }
                        >
                            {t("codalOperations.cancel")}
                        </Button>

                        <Button
                            type="button"
                            variant="outline"
                            onClick={confirmOperation}
                        >
                            {t("codalOperations.confirm")}
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </section>
    );
   }