import { apiFetch } from "@/lib/api-client";

export type PagedResponse<T> = {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
};

export type DisclosureParseStatus =
  | "Pending"
  | "Success"
  | "Failed"
  | "NoData"
  | "Skipped";

export type DisclosureSortBy =
  | "tracingNo"
  | "symbol"
  | "companyName"
  | "publishDateTime"
    | "sentDateTime"
    | "salesParsedAt";
export type CodalMissingPeriod = {
    periodEndDate: string;
    publishDate: string | null;
};

export type CodalSymbolCoverageGap = {
    symbol: string;
    availableMonths: number;
    missingMonths: number;
    oldestAvailablePeriod: string | null;
    newestAvailablePeriod: string | null;
    missingPeriods: string[];
    missingPeriodDetails: CodalMissingPeriod[];
};
export type CodalDataQualityReport = {
    checkedAtUtc: string;

    metadata: {
        totalDisclosures: number;
        missingSymbol: number;
        missingUrl: number;
        missingPublishDate: number;
        missingLet: number;
        missingRt: number;
    };

    monthlyProcessing: {
        totalCandidates: number;
        pending: number;
        success: number;
        failed: number;
        noData: number;
        skipped: number;
    };

    historyCoverage: {
        coverageYears: number;
        requiredMonths: number;
        windowStartPeriod: string | null;
        windowEndPeriod: string | null;
        activeSymbols: number;
        completeSymbols: number;
        incompleteSymbols: number;
        gaps: CodalSymbolCoverageGap[];
    };

    summaries: {
        totalSummaries: number;
        missingSourceDisclosure: number;
        sourceIdentityMismatch: number;
        sourceSymbolMismatch: number;
        sourcePublishDateMismatch: number;
        sourceStatusNotSuccess: number;
        duplicateSymbolPeriods: number;
        missingPeriodAmount: number;
        missingYearToDateAmount: number;
        missingPreviousYearWhenHistoryExists: number;
    };
};
export type DisclosureDto = {
  id: string;
  tracingNo: number;
  symbol: string;
  companyName: string;
  title: string;
  letterCode: string;
  sentDateTimeRaw?: string | null;
  publishDateTimeRaw?: string | null;
  sentDateTime?: string | null;
  publishDateTime?: string | null;
  hasHtml: boolean;
  isEstimate: boolean;
  url: string;
  hasExcel: boolean;
  hasPdf: boolean;
  hasXbrl: boolean;
  hasAttachment: boolean;
  attachmentUrl?: string | null;
  pdfUrl?: string | null;
  excelUrl?: string | null;
  xbrlUrl?: string | null;
  tedanUrl?: string | null;
  let?: number | null;
  rt?: number | null;
  ct?: number | null;
  ft?: number | null;
  reportingTypeCode?: number | null;
  salesParseStatus: DisclosureParseStatus;
  salesParsedAt?: string | null;
};

export type SearchDisclosuresParams = {
  search?: string;
    let?: number | null;
    lets?: readonly number[];
    includeNullLet?: boolean;
  rt?: number | null;
  reportingTypeCode?: number | null;
  salesParseStatus?: DisclosureParseStatus | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: DisclosureSortBy;
  sortDir?: "asc" | "desc";
};

export function searchDisclosures(
  params: SearchDisclosuresParams = {},
): Promise<PagedResponse<DisclosureDto>> {
  const query = new URLSearchParams();

  if (params.search) {
    query.set("search", params.search);
  }

    if (params.let !== undefined && params.let !== null) {
        query.set("let", String(params.let));
    }

    if (params.lets?.length) {
        params.lets.forEach((letCode) => {
            query.append("lets", String(letCode));
        });
    }

    if (params.includeNullLet) {
        query.set("includeNullLet", "true");
    }

    if (params.rt !== undefined && params.rt !== null) {
        query.set("rt", String(params.rt));
    }

  if (
    params.reportingTypeCode !== undefined &&
    params.reportingTypeCode !== null
  ) {
    query.set(
      "reportingTypeCode",
      String(params.reportingTypeCode),
    );
  }

  if (params.salesParseStatus) {
    query.set(
      "salesParseStatus",
      params.salesParseStatus,
    );
  }

  query.set(
    "pageNumber",
    String(params.pageNumber ?? 1),
  );

  query.set(
    "pageSize",
    String(params.pageSize ?? 20),
  );

  if (params.sortBy) {
    query.set("sortBy", params.sortBy);
  }

  if (params.sortDir) {
    query.set("sortDir", params.sortDir);
  }

  return apiFetch<PagedResponse<DisclosureDto>>(
    `/api/v1/marketintelligence/disclosures?${query.toString()}`,
    );

}
export function getCodalDataQuality(
    coverageYears = 5,
): Promise<CodalDataQualityReport> {
    return apiFetch<CodalDataQualityReport>(
        `/api/v1/marketintelligence/codal/data-quality?coverageYears=${coverageYears}`,
    );
}
export type CodalOperationResponse = {
    jobId: string;
    message: string;
}
export function collectNewCodalDisclosures(): Promise<CodalOperationResponse> {
    return apiFetch<CodalOperationResponse>(
        "/api/v1/marketintelligence/codal/newRead",
        {
            method: "POST",
        },
    );
}
export type CodalJobStatus =
    | "Enqueued"
    | "Scheduled"
    | "Processing"
    | "Succeeded"
    | "Failed"
    | "Deleted"
    | "Unknown";

export type CodalJobStatusResponse = {
    jobId: string;
    status: CodalJobStatus;
    reason?: string | null;
    createdAt: string;
};

export function getCodalJobStatus(
    jobId: string,
): Promise<CodalJobStatusResponse> {
    return apiFetch<CodalJobStatusResponse>(
        `/api/v1/marketintelligence/codal/jobs/${encodeURIComponent(jobId)}/status`,
    );
}

export function collectCodalBackfill(): Promise<CodalOperationResponse> {
    return apiFetch<CodalOperationResponse>(
        "/api/v1/marketintelligence/codal/import",
        {
            method: "POST",
        },
    );
}

export function parsePendingCodalDisclosures(): Promise<CodalOperationResponse> {
    return apiFetch<CodalOperationResponse>(
        "/api/v1/marketintelligence/codal/parse-pending",
        {
            method: "POST",
        },
    );
}
export type CodalSymbolBackfillRequest = {
    symbol: string;
    fromDate: string;
    toDate: string;
};

export type CodalSymbolBackfillResponse = {
    jobId: string;
    message: string;
};

export function queueCodalSymbolBackfill(
    request: CodalSymbolBackfillRequest,
): Promise<CodalSymbolBackfillResponse> {
    return apiFetch<CodalSymbolBackfillResponse>(
        "/api/v1/marketintelligence/codal/symbol-backfill",
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(request),
        },
    );
}

export function collectCodalSymbolBackfill(
    request: CodalSymbolBackfillRequest,
): Promise<CodalOperationResponse> {
    return apiFetch<CodalOperationResponse>(
        "/api/v1/marketintelligence/codal/symbol-backfill",
        {
            method: "POST",
            body: JSON.stringify(request),
        },
    );
}
export type FiscalYearSalesRow = {
    periodEndDate: string;
    periodAmount: number | null;
    yearToDateAmount: number | null;
    previousYearToDateAmount: number | null;
    isMissing: boolean;
};
export type DataQualityIssue = {
    symbol: string;
    yearEndDate: string | null;
    periodEndDate: string;
    publishDate: string | null;
    issueCode: string;
    previousValue: number | null;
    currentValue: number | null;
};

export type FiscalYearSales = {
    symbol: string;
    yearEndDate: string;
    previousYearEndDate: string | null;
    nextYearEndDate: string | null;
    rows: FiscalYearSalesRow[];
};
export function getDataQualityIssues(): Promise<DataQualityIssue[]> {
    return apiFetch<DataQualityIssue[]>(
        "/api/v1/marketintelligence/data-quality/issues",
    );
}
export function getFiscalYearSales(
    symbol: string,
    title: string,
    yearEndDate?: string,
): Promise<FiscalYearSales> {
    const params = new URLSearchParams({
        symbol,
        title,
    });

    if (yearEndDate) {
        params.set(
            "yearEndDate",
            yearEndDate,
        );
    }

    return apiFetch<FiscalYearSales>(
        `/api/v1/marketintelligence/fiscal-year-sales?${params.toString()}`,
    );
}