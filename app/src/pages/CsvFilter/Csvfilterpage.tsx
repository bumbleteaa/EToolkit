import { useState } from "react";
import type { AnnotatedRow } from "../../types/annotatedRow";
import { UploadState } from "./Uploadstate";
import { LoadingState } from "./Loadingstate";
import { ResultState } from "./Resultstate";
import { MOCK_ROWS } from "../../data/Mockrows";

// ─── State machine ──────────────────────────────────────────────────────────
//
//  Upload ──(file valid + submit)──► Loading ──(200 OK)──► Result
//                ▲                       │
//                └───────(error)─────────┘
//
// CsvFilterPage adalah satu-satunya tempat yang boleh mengubah `phase`.
// Komponen child (UploadState, LoadingState, ResultState) hanya menerima
// props dan memanggil callback — mereka tidak tahu tentang state machine ini.
// ────────────────────────────────────────────────────────────────────────────

type Phase = "upload" | "loading" | "result";

// USE_MOCK mengontrol apakah /import akan benar-benar dipanggil atau tidak.
// Set ke false saat backend sudah siap untuk integrasi.
const USE_MOCK = true;

export function CsvFilterPage() {
    const [phase, setPhase] = useState<Phase>("upload");
    const [rows, setRows] = useState<AnnotatedRow[]>([]);
    const [uploadedFile, setUploadedFile] = useState<File | null>(null);
    const [backendError, setBackendError] = useState<string | undefined>();

    // Dipanggil oleh UploadState saat file valid dipilih.
    // Transisi: upload → loading, lalu kirim ke /import (atau pakai mock).
    async function handleFileSelected(file: File) {
        setUploadedFile(file);
        setBackendError(undefined);
        setPhase("loading");

        if (USE_MOCK) {
            // Simulasi network delay agar LoadingState dapat dilihat
            await new Promise((r) => setTimeout(r, 1200));
            setRows(MOCK_ROWS);
            setPhase("result");
            return;
        }

        // ── Integrasi nyata ke /import ──────────────────────────────────────
        // Akan diaktifkan di langkah selanjutnya (setelah mock diverifikasi).
        try {
            const formData = new FormData();
            formData.append("file", file);

            const response = await fetch(
                `${import.meta.env.VITE_API_BASE_URL}/import`,
                { method: "POST", body: formData }
            );

            if (!response.ok) {
                const body = await response.json().catch(() => ({}));
                throw new Error(body.error ?? `Server error ${response.status}`);
            }

            const data: AnnotatedRow[] = await response.json();
            setRows(data);
            setPhase("result");
        } catch (err) {
            // Apapun error-nya (network, 4xx, 5xx) — kembali ke Upload state
            // dengan pesan error yang informatif.
            const message =
                err instanceof Error ? err.message : "Terjadi kesalahan yang tidak diketahui.";
            setBackendError(message);
            setPhase("upload");
        }
    }

    // Dipanggil tombol "Upload ulang" atau saat user ingin reset.
    function handleReset() {
        setRows([]);
        setUploadedFile(null);
        setBackendError(undefined);
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
                {phase === "result" && <ResultState rows={rows} />}
            </div>

            {/* Export button — sticky bottom-right, hanya tampil di phase result */}
            {phase === "result" && (
                <div className="absolute bottom-6 right-6">
                    <button
                        onClick={() => {
                            // Placeholder — akan diimplementasi di langkah Export
                            // (stateless: kirim ulang uploadedFile ke /export)
                            console.log("Export clicked, file:", uploadedFile?.name);
                        }}
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
        </div>
    );
}