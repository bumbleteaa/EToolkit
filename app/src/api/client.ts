const BASE_URL = import.meta.env.VITE_API_BASE_URL as string;

async function postFile(endpoint: string, file: File, extra?: Record<string, string>): Promise<Response> {
    const form = new FormData();
    form.append("file", file);
    if (extra) {
        for (const [key, value] of Object.entries(extra)) {
            form.append(key, value);
        }
    }
    return fetch(`${BASE_URL}${endpoint}`, { method: "POST", body: form });
}

export const apiClient = {
    import: (file: File) => postFile("/api/placement/import", file),
    filterPreview: (file: File) => postFile("/api/placement/filter-preview", file),
    export: (file: File, approvedNames?: Set<string>) =>
        postFile("/api/placement/export", file,
            approvedNames && approvedNames.size > 0
                ? { acceptedOverrides: [...approvedNames].join(",") }
                : undefined
        ),
};