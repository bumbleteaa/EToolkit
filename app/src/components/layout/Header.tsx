export default function Header({ onToggleSidebar }: { onToggleSidebar: () => void }) {
    return (
        <header className="h-12 px-5 flex items-center justify-between bg-black text-white border-b border-border shrink-0">
            <div className="flex items-center gap-2.5">
                <span className="text-lg leading-none">⬡</span>
                <button
                    onClick={onToggleSidebar}
                    className="font-mono font-medium text-sm tracking-wide hover:opacity-70 transition-opacity"
                >
                    EToolkit
                </button>
                <span className="text-[11px] uppercase tracking-widest text-white/80 pl-2.5 border-l border-white/20">
                    ELECTRONICS ASSEMBLY TOOLING HELPER
                </span>
            </div>
            <div className="flex items-center gap-2.5 font-mono text-[11px] text-white/50">
                <span>v1.0.0</span>
                <span className="w-px h-3.5 bg-white/20" />
                <span>API: localhost:5000</span>
                <span className="w-1.5 h-1.5 rounded-full bg-white/40" />
            </div>
        </header>
    );
}