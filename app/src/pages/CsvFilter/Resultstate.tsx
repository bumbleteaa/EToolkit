import type { AnnotatedRow, RowStatus } from "@/types/annotatedRow";
import { useState } from "react";

type TabKey = "total" | "unknown" | "rejected";

interface Tab {
    key: TabKey;
    label: string;
    filter: (row: AnnotatedRow) => boolean;
}

const TABS: Tab[] = [
    { key: "total", label: "Total Rows", filter: () => true },
    { key: "unknown", label: "Unknown", filter: (r) => r.status === "Unknown" },
    { key: "rejected", label: "Rejected", filter: (r) => r.status === "Rejected" },
];

// Color grading
const ROW_COLOR: Record<RowStatus, string> = {
    Accepted: "",
    Unknown: "bg-yellow-50",
    Rejected: "bh-red-50",
};

const STATUS_BADGE: Record<RowStatus, string> = {
    Accepted: "bg-green-100 rext-green-800",
    Unknown: "bg-yellow-100 text-yellow-800",
    Rejected: "bg-red-100 text-red-800",
};

interface ResultStateProps {
    rows: AnnotatedRow[];
}

export function ResultState({ rows }: ResultStateProps) {
    const [activeTab, setActiveTab] = useState<TabKey>("total");

    //local filter
    const activeTabDef = TABS.find((t) => t.key === activeTab)!;

    const visibleRows = rows.filter(activeTabDef.filter);

    //badge count
    function countFor(key: TabKey): number {
        if (key === "total") return rows.length;

        const tab = TABS.find((t) => t.key === key)!;
        return rows.filter(tab.filter).length;
    }

    return (
        <div className="flex flex-col h-full">
            {/* Tab bar */}
            <div className="flex gap-1 border-b border-gray-200">
                {TABS.map((tab) => {
                    const count = countFor(tab.key);
                    const isActive = tab.key === activeTab;
                    return (
                        <button
                            key={tab.key}
                            onClick={() => setActiveTab(tab.key)}
                            className={[
                                "flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-t-lg",
                                "border-b-2 transition-colors duration-150",
                                isActive
                                    ? "border-blue-500 text-blue-600 "
                                    : "border-transparent text-gray-500  hover:text-gray-700",
                            ].join(" ")}
                        >
                            {tab.label}
                            {/* Badge count — membantu user langsung tahu berapa banyak baris masalah */}
                            <span
                                className={[
                                    "text-xs px-2 py-0.5 rounded-full font-normal",
                                    isActive
                                        ? "bg-blue-100 text-blue-600"
                                        : "bg-gray-100 text-gray-500",
                                ].join(" ")}
                            >
                                {count}
                            </span>
                        </button>
                    );
                })}
            </div>

            {/* Tabel — scrollable secara vertikal jika baris banyak */}
            <div className="flex-1 overflow-auto">
                {visibleRows.length === 0 ? (
                    <div className="flex items-center justify-center h-40 text-sm text-gray-400">
                        Tidak ada baris untuk tab ini.
                    </div>
                ) : (
                    <table className="w-full text-sm text-left">
                        <thead className="sticky top-0 bg-gray-50 border-b border-gray-200 z-10">
                            <tr>
                                {["#", "Name", "Value", "Footprint", "Desc", "Side", "Status", "Issues"].map(
                                    (col) => (
                                        <th
                                            key={col}
                                            className="px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide whitespace-nowrap"
                                        >
                                            {col}
                                        </th>
                                    )
                                )}
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-100">
                            {visibleRows.map((row) => (
                                <tr
                                    key={row.rowIndex}
                                    className={[
                                        ROW_COLOR[row.status],
                                        "transition-colors duration-100",
                                    ].join(" ")}
                                >
                                    <td className="px-4 py-2.5 text-gray-400 tabular-nums">
                                        {row.rowIndex}
                                    </td>
                                    <td className="px-4 py-2.5 text-gray-700 ">
                                        {row.name}
                                    </td>
                                    <td className="px-4 py-2.5 text-gray-700 ">
                                        {row.value}
                                    </td>
                                    <td className="px-4 py-2.5 font-mono text-xs text-gray-600">
                                        {row.footprint}
                                    </td>
                                    <td className="px-4 py-2.5 text-gray-500 max-w-[180px] truncate">
                                        {row.desc}
                                    </td>
                                    <td className="px-4 py-2.5 text-gray-600">
                                        {row.side}
                                    </td>
                                    <td className="px-4 py-2.5">
                                        <span
                                            className={[
                                                "px-2 py-0.5 rounded-full text-xs font-medium",
                                                STATUS_BADGE[row.status],
                                            ].join(" ")}
                                        >
                                            {row.status}
                                        </span>
                                    </td>
                                    <td className="px-4 py-2.5 text-gray-500">
                                        {row.issues.length > 0 ? (
                                            <span className="flex flex-wrap gap-1">
                                                {row.issues.map((issue) => (
                                                    <span
                                                        key={issue}
                                                        className="text-xs bg-gray-100 px-1.5 py-0.5 rounded"
                                                    >
                                                        {issue}
                                                    </span>
                                                ))}
                                            </span>
                                        ) : (
                                            <span className="text-gray-300">—</span>
                                        )}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                )}
            </div>
        </div>
    );
}