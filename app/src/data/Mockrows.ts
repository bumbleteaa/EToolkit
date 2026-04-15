import type { AnnotatedRow } from "../types/annotatedRow";

// Mock data berdasarkan data nyata dari deck.csv.
// Status disimulasikan untuk mencerminkan skenario klasifikasi yang realistis:
// - Accepted: footprint dikenal, data lengkap
// - Unknown:  footprint ada tapi belum ada di database (GenericFootprint)
// - Rejected: data tidak valid / footprint tidak dikenal sama sekali
export const MOCK_ROWS: AnnotatedRow[] = [
    // ── Capacitors (Top) ─────────────────────────────────────────────────────
    { rowIndex: 1, status: "Accepted", name: "203108", value: "100uF 63V", footprint: "CAP", desc: "Bulk capacitor", side: "Top", issues: [] },
    { rowIndex: 2, status: "Accepted", name: "227581", value: "47uF", footprint: "805", desc: "Decoupling", side: "Top", issues: [] },
    { rowIndex: 3, status: "Accepted", name: "209723", value: "100nF 100V", footprint: "805", desc: "Decoupling", side: "Top", issues: [] },
    { rowIndex: 4, status: "Accepted", name: "226788", value: "47pF/100V", footprint: "603", desc: "Filter cap", side: "Top", issues: [] },
    { rowIndex: 5, status: "Accepted", name: "204824", value: "100nF 50V", footprint: "603", desc: "Bypass cap", side: "Top", issues: [] },
    { rowIndex: 6, status: "Accepted", name: "204829", value: "1uF 16V", footprint: "603", desc: "Bypass cap", side: "Top", issues: [] },
    { rowIndex: 7, status: "Unknown", name: "226116", value: "2.2uF/100V", footprint: "SM", desc: "High voltage cap", side: "Top", issues: ["FootprintUnknown"] },
    { rowIndex: 8, status: "Unknown", name: "245809", value: "470nF 50V", footprint: "603", desc: "Filter cap", side: "Top", issues: ["FootprintUnknown"] },

    // ── Capacitors (Bottom) ──────────────────────────────────────────────────
    { rowIndex: 9, status: "Accepted", name: "204824", value: "100nF 50V", footprint: "603", desc: "Bypass cap", side: "Bottom", issues: [] },
    { rowIndex: 10, status: "Accepted", name: "204396", value: "10uF 16V", footprint: "805", desc: "Bulk cap", side: "Bottom", issues: [] },
    { rowIndex: 11, status: "Unknown", name: "226442", value: "100uF 16V", footprint: "C1210", desc: "Large bulk cap", side: "Bottom", issues: ["FootprintUnknown"] },
    { rowIndex: 12, status: "Unknown", name: "226271", value: "22uF 35V", footprint: "1206", desc: "Bulk cap", side: "Bottom", issues: ["FootprintUnknown"] },

    // ── Resistors ────────────────────────────────────────────────────────────
    { rowIndex: 13, status: "Accepted", name: "226795", value: "196K", footprint: "R0603", desc: "Feedback resistor", side: "Top", issues: [] },
    { rowIndex: 14, status: "Accepted", name: "204846", value: "100 K", footprint: "R0603", desc: "Pull-up", side: "Top", issues: [] },
    { rowIndex: 15, status: "Accepted", name: "204842", value: "10 K", footprint: "R0603", desc: "Pull-up", side: "Top", issues: [] },
    { rowIndex: 16, status: "Accepted", name: "796941A", value: "120", footprint: "805", desc: "Termination resistor", side: "Top", issues: [] },
    { rowIndex: 17, status: "Accepted", name: "245811", value: "2R2", footprint: "R0603", desc: "Current sense", side: "Top", issues: [] },
    { rowIndex: 18, status: "Rejected", name: "DNP", value: "0 R", footprint: "805", desc: "DNP", side: "Top", issues: ["ValueInvalid", "DescInvalid"] },
    { rowIndex: 19, status: "Rejected", name: "DNP", value: "DNP", footprint: "R0603", desc: "DNP", side: "Top", issues: ["ValueInvalid", "DescInvalid"] },
    { rowIndex: 20, status: "Rejected", name: "DNP", value: "DNP", footprint: "R0603", desc: "DNP", side: "Top", issues: ["ValueInvalid", "DescInvalid"] },

    // ── Diodes ───────────────────────────────────────────────────────────────
    { rowIndex: 21, status: "Accepted", name: "260073", value: "", footprint: "DO-214AA", desc: "Rectifier diode", side: "Top", issues: [] },
    { rowIndex: 22, status: "Accepted", name: "249530", value: "TVS 51V", footprint: "DO-214AA", desc: "TVS Diode 51V", side: "Top", issues: [] },
    { rowIndex: 23, status: "Unknown", name: "228656", value: "5A/100V", footprint: "TO252", desc: "Power diode", side: "Top", issues: ["FootprintUnknown"] },

    // ── Inductors ────────────────────────────────────────────────────────────
    { rowIndex: 24, status: "Unknown", name: "226005", value: "22uH 6A", footprint: "IND", desc: "Power inductor", side: "Top", issues: ["FootprintUnknown"] },
    { rowIndex: 25, status: "Rejected", name: "", value: "", footprint: "silk", desc: "Silkscreen only", side: "Top", issues: ["FootprintUnknown", "ValueInvalid"] },

    // ── Transistors ──────────────────────────────────────────────────────────
    { rowIndex: 26, status: "Unknown", name: "226230", value: "RTR030N05TL", footprint: "TSMT3", desc: "N-Channel MOSFET", side: "Top", issues: ["FootprintUnknown"] },

    // ── Connectors & Special ─────────────────────────────────────────────────
    { rowIndex: 27, status: "Rejected", name: "246492", value: "", footprint: "SHDR10W41P127_2X5", desc: "JTAG connector", side: "Top", issues: ["FootprintUnknown"] },
    { rowIndex: 28, status: "Unknown", name: "225681", value: "", footprint: "conn", desc: "Status LED connector", side: "Top", issues: ["FootprintUnknown"] },

    // ── Power test points ────────────────────────────────────────────────────
    { rowIndex: 29, status: "Rejected", name: "", value: "", footprint: "P100", desc: "Power test point", side: "Bottom", issues: ["FootprintUnknown", "ValueInvalid"] },
    { rowIndex: 30, status: "Rejected", name: "", value: "", footprint: "P100", desc: "Power test point", side: "Bottom", issues: ["FootprintUnknown", "ValueInvalid"] },
];