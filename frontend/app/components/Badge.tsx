import "./Badge.css";

type Tom = "info" | "coin" | "accent" | "success" | "danger" | "neutral";

interface Props {
    children: React.ReactNode;
    tom?: Tom;
    flutuante?: boolean;
}

export default function Badge({ children, tom = "neutral", flutuante }: Props) {
    return (
        <span className={`badge badge-${tom} ${flutuante ? "badge-flutuante" : ""}`}>
            {children}
        </span>
    );
}
