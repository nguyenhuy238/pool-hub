import Link from "next/link";

export default function PublicLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="public-shell">
      <nav className="public-nav">
        <Link className="brand" href="/"><span>PH</span>PoolHub</Link>
        <div>
          <Link href="/tables">Bàn chơi</Link>
          <Link href="/pricing">Bảng giá</Link>
          <Link href="/booking">Đặt bàn</Link>
          <Link className="ghost-btn" href="/login">Đăng nhập</Link>
        </div>
      </nav>
      {children}
    </div>
  );
}
