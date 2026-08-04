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