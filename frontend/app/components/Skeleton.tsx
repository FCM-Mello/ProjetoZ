import "./Skeleton.css";

interface Props {
    width?: string | number;
    height?: string | number;
    borderRadius?: string | number;
    className?: string;
}

export default function Skeleton({ width = "100%", height = 14, borderRadius = 6, className = "" }: Props) {
    return (
        <span
            className={`skeleton ${className}`}
            style={{ width, height, borderRadius }}
        />
    );
}
