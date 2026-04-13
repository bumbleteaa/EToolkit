export function LoadingState() {
    return (
        <div
            role="status"
            aria-label="Memproses file CSV"
            className="flex flex-col items-center justify-center h-full gap-5"
        >
            {/* Spinner indeterminate menggunakan Tailwind animate-spin */}
            <svg
                className="w-12 h-12 text-blue-500 animate-spin"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
                aria-hidden="true"
            >
                {/* Track lingkaran (abu-abu) */}
                <circle
                    className="opacity-25"
                    cx="12"
                    cy="12"
                    r="10"
                    stroke="currentColor"
                    strokeWidth="4"
                />
                {/* Arc berputar (biru) */}
                <path
                    className="opacity-75"
                    fill="currentColor"
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
                />
            </svg>

            <p className="text-sm text-gray-600">
                Menganalisis file CSV…
            </p>
        </div>
    );
}