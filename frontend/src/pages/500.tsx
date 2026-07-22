import Link from "next/link";

export default function ServerErrorPage() {
  return (
    <section className="section">
      <div className="state-card error">
        <h1>500</h1>
        <p>Máy chủ gặp lỗi hoặc API chưa sẵn sàng.</p>
        <Link className="primary-btn" href="/">Về trang chủ</Link>
      </div>
    </section>
  );
}
