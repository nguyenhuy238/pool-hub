import Link from "next/link";
export default function ForbiddenPage() { return <section className="section"><div className="state-card error"><h1>403</h1><p>Bạn không có quyền truy cập màn hình này.</p><Link className="primary-btn" href="/dashboard">Về dashboard</Link></div></section>; }
