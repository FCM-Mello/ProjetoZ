import { Product } from "./Product";
import { SteamProfile } from "./SteamProfile";

export interface User {
    id: number;
    profile: SteamProfile;
    inventario: Array<Product>;
    coins: number;
}