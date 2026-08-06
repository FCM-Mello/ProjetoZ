"use client";

import {
    createContext,
    useContext,
    useEffect,
    useState
} from "react";
import { AuthContextType } from "../models/AuthContextType";
import { User } from "../models/User";
import { getCurrentUser } from "../services/authApi";

const AuthContext = createContext<AuthContextType>({
    user: null,
    loading: true,
    refreshUser: async () => {}
});


export function AuthProvider({
    children
}: {
    children: React.ReactNode
}) {

    const [user, setUser] = useState<User | null>(null);
    const [loading, setLoading] = useState(true);


    async function refreshUser() {

        const token = localStorage.getItem("token");

        if (!token) {
            setLoading(false);
            return;
        }
        
        setUser(await getCurrentUser(token));
        setLoading(false);
    }


    useEffect(() => {
        refreshUser();
    }, []);


    return (
        <AuthContext.Provider
            value={{
                user,
                loading,
                refreshUser
            }}
        >
            {children}
        </AuthContext.Provider>
    );
}


export function useAuth() {
    return useContext(AuthContext);
}