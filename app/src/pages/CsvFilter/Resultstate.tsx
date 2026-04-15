import type { AnnotatedRow, RowStatus } from "@/types/annotatedRow";
import { useState, type CSSProperties } from "react";

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

const TAB_ACTIVE_STYLE: Record<TabKey, { button: string; badge: string }> = {
    total: { button: "border-blue-500 text-blue-600", badge: "bg-blue-100 text-blue-600" },
    unknown: { button: "border-yellow-500 text-yellow-600", badge: "bg-yellow-100 text-yellow-700" },
    rejected: { button: "border-red-500 text-red-600", badge: "bg-red-100 text-red-600" },
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

    function getCellBg(status: RowStatus): CSSProperties {
        if (activeTab !== "total") return {};
        if (status === "Unknown") return { backgroundColor: "#FFF59D" }; // kuning pekat
        if (status === "Rejected") return { backgroundColor: "#EF9A9A" }; // merah pekat
        return {};
    }

    return (

        <div className="flex flex-col h-full">
            {/* Tab bar */}
            <div className="flex gap-1 border-b border-gray-200">

                {TABS.map((tab) => {
                    const count = countFor(tab.key);
                    const isActive = tab.key === activeTab;
                    const activeStyle = TAB_ACTIVE_STYLE[tab.key];
                    return (
                        <button
                            key={tab.key}
                            onClick={() => setActiveTab(tab.key)}
                            className={[
                                "flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-t-lg",
                                "border-b-2 transition-colors duration-150",
                                isActive
                                    ? activeStyle.button
                                    : "border-transparent text-gray-500 hover:text-gray-700",
                            ].join(" ")}
                        >
                            {tab.label}
                            <span
                                className={[
                                    "text-xs px-2 py-0.5 rounded-full font-normal",
                                    isActive
                                        ? activeStyle.badge
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
                    <table className="w-full text-sm text-left border-collapse [&_td]:border [&_td]:border-black [&_th]:border [&_th]:border-black">
                        <thead className="sticky top-0 bg-gray-50 border-b border-gray-200 z-10">
                            <tr>
                                {["#", "Name", "Value", "Footprint", "Desc", "Side", "Issues"].map(
                                    (col) => (
                                        <th
                                            key={col}
                                            className="border border-gray-200 px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide whitespace-nowrap"
                                        >
                                            {col}
                                        </th>
                                    )
                                )}
                            </tr>
                        </thead>
                        <tbody>
                            {visibleRows.map((row) => (
                                <tr
                                    key={row.rowIndex}
                                    className="transition-colors duration-100"
                                >
                                    <td style={getCellBg(row.status)} className="px-4 py-2.5 text-gray-400 tabular-nums">
                                        {row.rowIndex}
                                    </td>
                                    <td style={getCellBg(row.status)} className="px-4 py-2.5 text-gray-700">
                                        {row.name}
                                    </td>
                                    <td style={getCellBg(row.status)} className="px-4 py-2.5 text-gray-700">
                                        {row.value}
                                    </td>
                                    <td style={getCellBg(row.status)} className="px-4 py-2.5 font-mono text-xs text-gray-600">
                                        {row.footprint}
                                    </td>
                                    <td style={getCellBg(row.status)} className="px-4 py-2.5 text-gray-500 max-w-[180px] truncate">
                                        {row.desc}
                                    </td>
                                    <td style={getCellBg(row.status)} className="px-4 py-2.5 text-gray-600">
                                        {row.side}
                                    </td>
                                    <td style={getCellBg(row.status)} className="px-4 py-2.5 text-gray-500">
                                        {row.issues.length > 0 ? (
                                            <span className="flex flex-wrap gap-1">
                                                {row.issues.map((issue) => (
                                                    <span
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