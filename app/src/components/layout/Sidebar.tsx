type NavItem = {
    id: string;
    label: string;
    disabled?: boolean;
};

// `active` dihapus dari sini — kebenaran soal halaman aktif
// sekarang tinggal di App.tsx, bukan di data statik ini.
const navItems: NavItem[] = [
    { id: "csv-filter", label: "CSV Filter Service" },
];

interface SidebarProps {
    open: boolean;
    activePage: string | null;
    onNavigate: (page: string) => void;
}

export default function Sidebar({ open, activePage, onNavigate }: SidebarProps) {
    return (
        <aside
            className={[
                "shrink-0 flex flex-col bg-black border-r border-border overflow-hidden transition-all duration-200",
                open ? "w-52" : "w-0",
            ].join(" ")}
        >
            <nav className="flex-1 p-0 min-w-52">
                <ul className="flex flex-col gap-0.5">
                    {navItems.map((item) => {
                        // Aktif atau tidak sekarang ditentukan dari prop,
                        // bukan dari data hardcoded di atas.
                        const isActive = activePage === item.id;

                        return (
                            <li key={item.id}>
                                <button
                                    disabled={item.disabled}
                                    aria-current={isActive ? "page" : undefined}
                                    onClick={() => !item.disabled && onNavigate(item.id)}
                                    className={[
                                        "nav-btn flex items-center gap-2.5 w-full px-2.5 py-2 text-[13px] text-left text-white transition-colors relative group",
                                        isActive ? "bg-black text-white font-medium" : "text-muted-foreground",
                                        item.disabled ? "opacity-40 cursor-not-allowed" : "",
                                    ].join(" ")}
                                    onMouseEnter={e => { if (!isActive) (e.currentTarget as HTMLButtonElement).style.backgroundColor = "hsl(31.5 91.7% 62.6%)" }}
                                    onMouseLeave={e => { if (!isActive) (e.currentTarget as HTMLButtonElement).style.backgroundColor = "" }}
                                >
                                    <span className="etoolkit-nav-label">{item.label}</span>
                                </button>
                            </li>
                        );
                    })}
                </ul>
            </nav>
            <div className="px-4 py-2.5 border-t border-border min-w-52">
                <span className="font-mono text-[10px] text-white tracking-wider">
                    build 2026.04
                </span>
            </div>
        </aside>
    );
}