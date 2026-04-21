import type { FilterPreviewRowDto, PipelineReport } from "@/types/filterPreview";
import { useState, type CSSProperties } from "react";

type TabKey = "total" | "unknown" | "rejected";

interface Tab {
    key: TabKey;
    label: string;
    filter: (row: FilterPreviewRowDto) => boolean;
}

const TABS: Tab[] = [
    { key: "total", label: "Total Rows", filter: () => true },
    { key: "unknown", label: "Unknown", filter: (r) => r.status === "Unknown" },
    { key: "rejected", label: "Rejected", filter: (r) => r.status === "Rejected" },
];

const TAB_ACTIVE_STYLE: Record<TabKey, { button: string; badge: string }> = {
    total: { button: "border-blue-500 text-blue-600", badge: "bg-blue-100 text-blue-600" },
    unknown: { button: "border-yellow-500 text-yellow-600", badge: "bg-yellow-100 text-yellow-700" },
    rejected: { button: "border-red-500 text-red-600", badge: "bg-red-100 text-red-600" },
};

interface ResultStateProps {
    rows: FilterPreviewRowDto[];
    report?: PipelineReport;                                       // existing prop
    onApprovedChange?: (names: Set<string>) => void;         // NEW
}

export function ResultState({ rows, report, onApprovedChange: onApprovedNamesChange }: ResultStateProps) {
    const [activeTab, setActiveTab] = useState<TabKey>("total");

    // ── Approval state (Unknown tab) ──────────────────────────────────────────
    // Keyed by rowIndex agar unik meskipun nama komponen duplikat.
    const [approvedIndices, setApprovedIndices] = useState<Set<number>>(new Set());

    const unknownRows = rows.filter(r => r.status === "Unknown");

    function notifyParent(next: Set<number>) {
        const names = new Set(
            [...next]
                .map(i => rows.find(r => r.rowIndex === i)?.name ?? "")
                .filter(Boolean)
        );
        onApprovedNamesChange?.(names);
    }

    function toggleRow(rowIndex: number) {
        setApprovedIndices(prev => {
            const next = new Set(prev);
            if (next.has(rowIndex)) next.delete(rowIndex); else next.add(rowIndex);
            notifyParent(next);
            return next;
        });
    }

    function toggleAll() {
        const allIndices = unknownRows.map(r => r.rowIndex);
        const allSelected = allIndices.every(i => approvedIndices.has(i));
        const next = new Set(approvedIndices);
        if (allSelected) {
            allIndices.forEach(i => next.delete(i));
        } else {
            allIndices.forEach(i => next.add(i));
        }
        setApprovedIndices(next);
        notifyParent(next);
    }

    const allUnknownSelected =
        unknownRows.length > 0 && unknownRows.every(r => approvedIndices.has(r.rowIndex));
    const someUnknownSelected =
        unknownRows.some(r => approvedIndices.has(r.rowIndex)) && !allUnknownSelected;

    // ── Tab helpers ───────────────────────────────────────────────────────────
    const activeTabDef = TABS.find(t => t.key === activeTab)!;
    const visibleRows = rows.filter(activeTabDef.filter);

    function countFor(key: TabKey): number {
        if (key === "total") return rows.length;
        return rows.filter(TABS.find(t => t.key === key)!.filter).length;
    }

    function getCellBg(status: string): CSSProperties {
        if (activeTab !== "total") return {};
        if (status === "Unknown") return { backgroundColor: "#FFF59D" };
        if (status === "Rejected") return { backgroundColor: "#EF9A9A" };
        return {};
    }

    // ── Column headers ────────────────────────────────────────────────────────
    const BASE_COLS = ["#", "Name", "Value", "Footprint", "Desc", "Side", "Issues"];
    const columns = activeTab === "unknown"
        ? ["✓", ...BASE_COLS]   // checkbox column prepended
        : BASE_COLS;

    return (
        <div className="flex flex-col h-full">
            {/* Tab bar */}
            <div className="flex gap-1 border-b border-gray-200">
                {TABS.map(tab => {
                    const count = countFor(tab.key);
                    const isActive = tab.key === activeTab;
                    const s = TAB_ACTIVE_STYLE[tab.key];
                    return (
                        <button
                            key={tab.key}
                            onClick={() => setActiveTab(tab.key)}
                            className={[
                                "flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-t-lg",
                                "border-b-2 transition-colors duration-150",
                                isActive
                                    ? `${s.button} border-current`
                                    : "border-transparent text-gray-500 hover:text-gray-700",
                            ].join(" ")}
                        >
                            {tab.label}
                            <span className={[
                                "text-xs px-1.5 py-0.5 rounded-full font-semibold",
                                isActive ? s.badge : "bg-gray-100 text-gray-500",
                            ].join(" ")}>
                                {count}
                            </span>
                        </button>
                    );
                })}
            </div>

            {/* Unknown tab hint */}
            {activeTab === "unknown" && unknownRows.length > 0 && (
                <div className="px-4 py-2 text-xs text-yellow-700 bg-yellow-50 border-b border-yellow-200">
                    Centang baris yang ingin diloloskan ke export. Baris yang tidak dicentang akan dilewati.
                    {approvedIndices.size > 0 && (
                        <span className="ml-2 font-semibold">
                            ({approvedIndices.size} dipilih)
                        </span>
                    )}
                </div>
            )}

            {/* Table */}
            <div className="flex-1 overflow-auto">
                {visibleRows.length === 0 ? (
                    <div className="flex items-center justify-center h-40 text-sm text-gray-400">
                        Tidak ada baris untuk tab ini.
                    </div>
                ) : (
                    <table className="w-full text-sm text-left border-collapse [&_td]:border [&_td]:border-black [&_th]:border [&_th]:border-black">
                        <thead className="sticky top-0 bg-gray-50 border-b border-gray-200 z-10">
                            <tr>
                                {columns.map(col => (
                                    <th
                                        key={col}
                                        className="border border-gray-200 px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide whitespace-nowrap"
                                    >
                                        {col === "✓" ? (
                                            /* Select-all checkbox */
                                            <input
                                                type="checkbox"
                                                className="w-4 h-4 accent-yellow-500 cursor-pointer"
                                                checked={allUnknownSelected}
                                                ref={el => {
                                                    if (el) el.indeterminate = someUnknownSelected;
                                                }}
                                                onChange={toggleAll}
                                                title="Pilih semua Unknown"
                                            />
                                        ) : col}
                                    </th>
                                ))}
                            </tr>
                        </thead>
                        <tbody>
                            {visibleRows.map(row => {
                                const isApproved = approvedIndices.has(row.rowIndex);
                                const bg = getCellBg(row.status);
                                const approvedBg: CSSProperties = activeTab === "unknown" && isApproved
                                    ? { backgroundColor: "#DCFCE7" }   // hijau muda = disetujui
                                    : {};

                                return (
                                    <tr
                                        key={row.rowIndex}
                                        className="transition-colors duration-100"
                                        onClick={activeTab === "unknown"
                                            ? () => toggleRow(row.rowIndex)
                                            : undefined}
                                        style={activeTab === "unknown" ? { cursor: "pointer" } : {}}
                                    >
                                        {/* Checkbox cell — hanya di Unknown tab */}
                                        {activeTab === "unknown" && (
                                            <td style={approvedBg} className="px-4 py-2.5 text-center">
                                                <input
                                                    type="checkbox"
                                                    className="w-4 h-4 accent-yellow-500 cursor-pointer"
                                                    checked={isApproved}
                                                    onChange={() => toggleRow(row.rowIndex)}
                                                    onClick={e => e.stopPropagation()}
                                                />
                                            </td>
                                        )}
                                        <td style={{ ...bg, ...approvedBg }} className="px-4 py-2.5 text-gray-400 tabular-nums">
                                            {row.rowIndex}
                                        </td>
                                        <td style={{ ...bg, ...approvedBg }} className="px-4 py-2.5 text-gray-700">{row.name}</td>
                                        <td style={{ ...bg, ...approvedBg }} className="px-4 py-2.5 text-gray-700">{row.value}</td>
                                        <td style={{ ...bg, ...approvedBg }} className="px-4 py-2.5 font-mono text-xs text-gray-600">{row.footprint}</td>
                                        <td style={{ ...bg, ...approvedBg }} className="px-4 py-2.5 text-gray-500 max-w-[180px] truncate">{row.desc}</td>
                                        <td style={{ ...bg, ...approvedBg }} className="px-4 py-2.5 text-gray-600">{row.side}</td>
                                        <td style={{ ...bg, ...approvedBg }} className="px-4 py-2.5 text-gray-500">
                                            {(row.issues?.length ?? 0) > 0 ? (
                                                <span className="flex flex-wrap gap-1">
                                                    {row.issues!.map(issue => (
                                                        <span key={issue}>{issue}</span>
                                                    ))}
                                                </span>
                                            ) : (
                                                <span className="text-gray-300">—</span>
                                            )}
                                        </td>
                                    </tr>
                                );
                            })}
                        </tbody>
                    </table>
                )}
            </div>
        </div>
    );
}