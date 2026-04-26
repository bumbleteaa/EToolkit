import { useState, useRef, type DragEvent, type ChangeEvent } from "react";

interface UpdloadStateProps {
    onFileSelected: (file: File) => void;
    errorMessage?: string;
}

export function UploadState({ onFileSelected, errorMessage }: UpdloadStateProps) {
    const [isDragging, setIsDragging] = useState(false);
    const [validationError, setValidationError] = useState<string | null>(null);
    const inputRef = useRef<HTMLInputElement>(null);

    //File extension validator
    function validate(file: File): string | null {
        if (!file.name.toLowerCase().endsWith(".csv")) {
            return `File "${file.name}" bukan file CSV`;
        }
        if (file.size === 0) {
            return "File tidak boleh kosong!";
        }
        return null;
    }

    //File handler 
    function handleFile(file: File) {
        const error = validate(file);
        if (error) {
            setValidationError(error);
            return;
        }
        setValidationError(null);
        onFileSelected(file);
    }

    //Drag and drop event handler
    function onDragOver(drag: DragEvent<HTMLDivElement>) {
        drag.preventDefault(); //onDrop wajib terpicu
        setIsDragging(true);
    }

    function onDragLeave() {
        setIsDragging(false);
    }

    function onDrop(drag: DragEvent<HTMLDivElement>) {
        drag.preventDefault();
        setIsDragging(false);
        const file = drag.dataTransfer.files[0];
        if (file) handleFile(file);
    }

    //If user choose browse file explorer
    function onInputChange(event: ChangeEvent<HTMLInputElement>) {
        const file = event.target.files?.[0];
        if (file) handleFile(file);
        event.target.value = "";
    }

    //Error messages
    const displayedError = validationError ?? errorMessage;

    return (
        <div className="flex flex-col items-center justify-center h-full gap-6 p-8">
            {/* Drop zone */}
            <div
                role="button"
                tabIndex={0}
                aria-label="Upload CSV file"
                onClick={() => inputRef.current?.click()}
                onKeyDown={(e) => e.key === "Enter" && inputRef.current?.click()}
                onDragOver={onDragOver}
                onDragLeave={onDragLeave}
                onDrop={onDrop}
                className={[
                    "w-full max-w-lg border-2 border-dashed rounded-xl",
                    "flex flex-col items-center justify-center gap-3",
                    "py-16 px-8 cursor-pointer select-none",
                    "transition-colors duration-200",
                    isDragging
                        ? "border-[var(--accent)] bg-orange-50"
                        : "border-gray-300 hover:border-[var(--accent)] hover:bg-gray-50"
                ].join(" ")}
            >
                {/* Ikon upload sederhana dengan SVG inline */}
                <svg
                    className={`w-10 h-10 transition-colors duration-200 ${isDragging ? "text-[var(--accent)]" : "text-gray-400"
                        }`}
                    xmlns="http://www.w3.org/2000/svg"
                    fill="none"
                    viewBox="0 0 24 24"
                    strokeWidth={1.5}
                    stroke="currentColor"
                >
                    <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5m-13.5-9L12 3m0 0l4.5 4.5M12 3v13.5"
                    />
                </svg>

                <p className="text-sm font-medium text-gray-700">
                    Drag &amp; drop file CSV di sini
                </p>
                <p className="text-xs text-gray-400">
                    atau klik untuk memilih file
                </p>
            </div>

            {/* Input tersembunyi — hanya menerima .csv */}
            <input
                ref={inputRef}
                type="file"
                accept=".csv"
                className="hidden"
                onChange={onInputChange}
            />

            {/* Inline error — tidak memakai alert/modal sesuai spec */}
            {displayedError && (
                <p
                    role="alert"
                    className="text-sm text-red-600 text-center max-w-lg"
                >
                    {displayedError}
                </p>
            )}
        </div>
    );
}