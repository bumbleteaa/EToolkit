// * AnnotatedRow merepresentasikan satu baris dari response /import.

export type RowStatus = "Accepted" | "Rejected" | "Unknown";

export interface AnnotatedRow {
    rowIndex: number;
    status: RowStatus;
    name: string;
    value: string;
    footprint: string;
    desc: string;
    side: string;
    issues: string[];
}