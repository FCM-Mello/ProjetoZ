import { Product } from "./Product";
import { SteamProfile } from "./SteamProfile";

export interface User {
    id: string;
    profile: SteamProfile;
    inventario: Array<Product>;
    coins: number;
    isAdmin: boolean;
}