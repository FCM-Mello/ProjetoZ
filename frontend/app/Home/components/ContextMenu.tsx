"use client";

import { ContextMenuState } from "../types";
import "../css/contextMenu.css";

interface Props {
    contextMenu: ContextMenuState;
    onEditar: () => void;
}

export default function ContextMenu({ contextMenu, onEditar }: Props) {
    return (
        <ul
            className="context-menu"
            style={{ top: contextMenu.y, left: contextMenu.x }}
            onClick={(e) => e.stopPropagation()}
        >
            <li onClick={onEditar}>Editar</li>
        </ul>
    );
}
