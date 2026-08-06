const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://projetoz.local/api";

export function authHeaders() {

    const token = localStorage.getItem("token");

    return {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`
    };
}