const BASE_URL = import.meta.env.VITE_API_BASE_URL as string;

async function postFile(endpoint: string, file: File): Promise<Response> {
    const form = new FormData();
    form.append("file", file);

    return fetch(`${BASE_URL}${endpoint}`, {
        method: "POST",
        body: form,
    });
}

export const apiClient = {
    import: (file: File) => postFile("/api/placement/import", file),
    export: (file: File) => postFile("/api/placement/export", file),
};