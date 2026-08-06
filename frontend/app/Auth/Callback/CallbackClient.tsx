"use client";

import { useEffect } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useAuth } from "@/app/contexts/AuthContext";

export default function CallbackClient() {
    const router = useRouter();
    const searchParams = useSearchParams();

    const { refreshUser } = useAuth();


    useEffect(() => {

        async function login() {
            const token = searchParams.get("token");

            if (!token) {
                router.push("/Login");
                return;
            }

            localStorage.setItem("token", token);

            await refreshUser();

            router.push("/Home");
        }


        login();

    }, [router, searchParams, refreshUser]);


    return <p>Entrando...</p>;
}