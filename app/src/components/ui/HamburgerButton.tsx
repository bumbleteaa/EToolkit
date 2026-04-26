// components/HamburgerButton.tsx
interface Props {
    open: boolean
    onClick: () => void
}

export function HamburgerButton({ open, onClick }: Props) {
    return (
        <button
            onClick={onClick}
            className="flex flex-col justify-center items-center w-8 h-8 gap-[5px] hover:opacity-70 transition-opacity"
        >
            <span className={`block h-[2.5px] w-5 bg-white rounded-sm origin-center transition-all duration-300 ease-in-out
        ${open ? "translate-y-[7.5px] rotate-45" : ""}`}
            />
            <span className={`block h-[2.5px] w-5 bg-white rounded-sm transition-all duration-300 ease-in-out
        ${open ? "opacity-0 scale-x-0" : ""}`}
            />
            <span className={`block h-[2.5px] w-5 bg-white rounded-sm origin-center transition-all duration-300 ease-in-out
        ${open ? "-translate-y-[7.5px] -rotate-45" : ""}`}
            />
        </button>
    )
}