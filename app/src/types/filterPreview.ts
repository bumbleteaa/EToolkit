// * Types for the /filter-preview endpoint response from backend

// Classification status for each row produced by the filtering pipeline
export type RowStatus = "Accepted" | "Rejected" | "Unknown";

// A single CSV row that has been flattened and classified by the backend
export interface FilterPreviewRowDto {
    rowIndex: number;
    status: RowStatus;
    comp: string;
    name: string;
    value: string;
    footprint: string;
    desc: string;
    side: string;
    issues: string[]; // RejectCode for this row, empty if Accepted
}

// Container for the preview row collection along with pagination metadata
export interface FilterPreviewDataDto {
    totalCount: number;
    rows: FilterPreviewRowDto[];
    isTruncated: boolean; // true if row count exceeded limitApplied
    limitApplied: number;
}

// Severity level of an issue reported by the pipeline
export type Severity = "Info" | "Warning" | "Error";

// Contextual detail for a single issue, used for display in the warning panel
export interface IssueContext {
    footprintRaw?: string;
    footprintKey?: string;
    footprintCanonical?: string;
    name?: string; // Component name of the sample row that triggered this issue
    side?: string;
    rowNumber?: number; // Row number of the first sample that triggered this issue
    count?: number; // Total rows affected by the same issue
}

// A single pipeline issue entry, aggregated across multiple rows
export interface PipelineIssue {
    code: string;
    severity: Severity;
    message: string;
    context: IssueContext;
}

// Pipeline-level report containing issue aggregations and export readiness
export interface PipelineReport {
    stage: string;
    isExportReady: boolean;
    rulesetVersion: string | null;
    issues: PipelineIssue[];
}

// Generic wrapper for all pipeline responses from the backend
export interface PipelineResponse<TData> {
    data: TData;
    report: PipelineReport;
}