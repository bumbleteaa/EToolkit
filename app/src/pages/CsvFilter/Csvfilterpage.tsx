import { useState } from "react";
import type { FilterPreviewRowDto, PipelineReport, PipelineResponse, FilterPreviewDataDto } from "@/types/filterPreview";
import { UploadState } from "./Uploadstate";
import { LoadingState } from "./Loadingstate";
import { ResultState } from "./Resultstate";
import { apiClient } from "@/api/client";

type Phase = "upload" | "loading" | "result";

type Toast = {
    id: number;
    message: string;
    variant: "info" | "error";
};


export function CsvFilterPage() {
    const [phase, setPhase] = useState<Phase>("upload");
    const [rows, setRows] = useState<FilterPreviewRowDto[]>([]);
    const [uploadedFile, setUploadedFile] = useState<File | null>(null);
    const [backendError, setBackendError] = useState<string | undefined>();
    const [report, setReport] = useState<PipelineReport | null>(null);
    const [toasts, setToasts] = useState<Toast[]>([]);
    const [isExporting, setIsExporting] = useState(false);
    const [approved, setApproved] = useState<Set<string>>(new Set());

    function showToast(message: string, variant: Toast["variant"]) {
        const id = Date.now();
        setToasts((prev) => [...prev, { id, message, variant }]);
        setTimeout(() => {
            setToasts((prev) => prev.filter((t) => t.id !== id));
        }, 4000);
    }
    // Called by UploadState when a valid file is selected.
    // Transitions: upload -> loading, then sends to /filter-preview.
    async function handleFileSelected(file: File) {
        setUploadedFile(file);
        setBackendError(undefined);
        setPhase("loading");

        // Integrasi nyata ke /filter-preview
        try {
            const response = await apiClient.filterPreview(file);

            if (!response.ok) {
                const body = await response.json().catch(() => ({}));
                throw new Error(body.error ?? `Server error ${response.status}`);
            }

            const body: PipelineResponse<FilterPreviewDataDto> = await response.json();
            setRows(body.data.rows);
            setReport(body.report);
            setPhase("result");
        } catch (err) {
            // Any error (network, 4xx, 5xx) returns to Upload state with an error message.
            const message =
                err instanceof Error ? err.message : "An unknown error occurred.";
            setBackendError(message);
            setPhase("upload");
        }
    }

    async function handleExport() {
        if (!uploadedFile || isExporting) return;
        setIsExporting(true);

        try {
            const response = await apiClient.export(uploadedFile, approved);

            if (response.status === 204) {
                showToast("No Accepted rows to export.", "info");
                return;
            }

            if (!response.ok) {
                const body = await response.json().catch(() => ({}));
                throw new Error(body.error ?? `Server error ${response.status}`)
            }

            // 200 OK
            const blob = await response.blob();
            const objectUrl = URL.createObjectURL(blob);
            const anchor = document.createElement("a");
            anchor.href = objectUrl;
            anchor.download = uploadedFile.name.replace("csv", "_filtered.csv");
            anchor.click();
            URL.revokeObjectURL(objectUrl);
        } catch (err) {
            const message = err instanceof Error ? err.message : "Export gagal";
            showToast(message, "error");
        } finally {
            setIsExporting(false);
        }

    }

    // Resets all state and returns to the Upload phase.
    function handleReset() {
        setRows([]);
        setUploadedFile(null);
        setBackendError(undefined);
        setApproved(new Set());
        setPhase("upload");
    }

    return (
        <div className="relative flex flex-col h-full">
            {/* Header halaman — selalu tampil di semua state */}
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200 dark:border-gray-700">
                <div>
                    <h1 className="text-lg font-semibold text-black-900">
                        CSV Filter Service
                    </h1>
                    <p className="text-sm text-gray-500 mt-0.5">
                        Upload file pick-and-place CSV untuk menganalisis dan memfilter komponen.
                    </p>
                </div>

                {/* Tombol reset — hanya muncul saat Result agar user bisa upload ulang */}
                {phase === "result" && (
                    <button
                        onClick={handleReset}
                        className="text-sm text-black-400 hover:text-gray-700 transition-colors"
                    >
                        ← Upload ulang
                    </button>
                )}
            </div>

            {/* Area konten — diisi oleh komponen state yang aktif */}
            <div className="flex-1 overflow-hidden">
                {phase === "upload" && (
                    <UploadState
                        onFileSelected={handleFileSelected}
                        errorMessage={backendError}
                    />
                )}
                {phase === "loading" && <LoadingState />}
                {phase === "result" && <ResultState rows={rows} report={report ?? undefined} onApprovedChange={setApproved} />}
            </div>

            {/* Export button — sticky bottom-right, hanya tampil di phase result */}
            {phase === "result" && (
                <div className="absolute bottom-6 right-6">
                    <button
                        style={{ backgroundColor: "hsl(31.5 91.7% 62.6%)" }}
                        onMouseEnter={e => (e.currentTarget.style.backgroundColor = "hsl(31.5 91.7% 52%)")}
                        onMouseLeave={e => (e.currentTarget.style.backgroundColor = "hsl(31.5 91.7% 62.6%)")}
                        onClick={handleExport}
                        className={[
                            "flex items-center gap-2 px-5 py-2.5 rounded-lg text-sm font-medium",
                            "bg-orange-600 hover:bg-orange-900 active:bg-blue-800",
                            "text-white shadow-lg transition-colors duration-150",
                        ].join(" ")}
                    >
                        <svg
                            className="w-4 h-4"
                            xmlns="http://www.w3.org/2000/svg"
                            fill="none"
                            viewBox="0 0 24 24"
                            strokeWidth={2}
                            stroke="currentColor"
                        >
                            <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12M12 16.5V3"
                            />
                        </svg>
                        Export CSV
                    </button>
                </div>
            )}
            {/* Toast stack — muncul bottom-left agar tidak tabrakan dengan tombol Export */}
            <div className="absolute bottom-6 left-6 flex flex-col gap-2">
                {toasts.map((toast) => (
                    <div
                        key={toast.id}
                        className={[
                            "px-4 py-3 rounded-lg text-sm text-white shadow-lg",
                            "transition-opacity duration-300",
                            toast.variant === "error" ? "bg-red-600" : "bg-gray-800",
                        ].join(" ")}
                    >
                        {toast.message}
                    </div>
                ))}
            </div>
        </div>
    );
}