import { useState } from "react";
import Sidebar from "./components/layout/Sidebar";
import Header from "./components/layout/Header";
import Footer from "./components/layout/Footer";
import { CsvFilterPage } from "./pages/CsvFilter/Csvfilterpage";

// Daftar halaman yang dikenal aplikasi.
// Nanti kalau ada halaman baru, cukup tambahkan entry di sini
// dan tambahkan case di renderPage() di bawah.
type Page = "csv-filter";

export default function App() {
    const [sidebarOpen, setSidebarOpen] = useState(false);
    // null = belum ada halaman yang dipilih (blank state seperti semula)
    const [activePage, setActivePage] = useState<Page | null>(null);

    function renderPage() {
        switch (activePage) {
            case "csv-filter":
                return <CsvFilterPage />;
            default:
                // Blank state — ditampilkan sebelum user memilih menu apapun
                return (
                    <div className="flex items-center justify-center h-full min-h-64">
                        <span className="text-xs font-mono text-muted-foreground border border-dashed border-border px-7 py-4 rounded-md">
                            Select a page from the sidebar
                        </span>
                    </div>
                );
        }
    }

    return (
        <div className="flex flex-col h-screen bg-background text-foreground overflow-hidden">
            <Header onToggleSidebar={() => setSidebarOpen(o => !o)} />
            <div className="flex flex-1 overflow-hidden">
                {/* Sidebar menerima activePage dan setter-nya supaya bisa
                    menandai item yang aktif (highlight) dan memicu navigasi */}
                <Sidebar
                    open={sidebarOpen}
                    activePage={activePage}
                    onNavigate={(page) => {
                        setActivePage(page as Page)
                        setSidebarOpen(false);
                    }}

                />
                <main className="flex-1 overflow-hidden">
                    {renderPage()}
                </main>
            </div>
            <Footer />
        </div>
    );
}