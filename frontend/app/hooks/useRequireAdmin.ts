"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "../contexts/AuthContext";
import { useRequireAuth } from "./useRequireAuth";

export function useRequireAdmin() {
    useRequireAuth();

    const { user, loading } = useAuth();
    const router = useRouter();

    useEffect(() => {
        if (!loading && (!user || !user.isAdmin)) {
            router.push("/Home");
        }
    }, [loading, user, router]);
}
