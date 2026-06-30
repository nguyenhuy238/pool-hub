"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { invoiceApi, sessionApi, productApi } from "@/lib/api/endpoints";
import { getTotalPages, API_BASE_URL } from "@/lib/api/client";
import { money, dateTime } from "@/lib/status";
import { ConfirmDialog, DataTable, ListControls, PageHeader, StateBlock, useList, useLoad, Modal, Pagination, SearchableSelect } from "@/components/ui";
import { useToast } from "@/components/toast";
import type { Invoice, PaymentMethod, Session, Product } from "@/types";

export default function InvoicesPage() {
  const toast = useToast();
  const router = useRouter();
  const [selectedSessionId, setSelectedSessionId] = useState("");
  const [creatingInvoice, setCreatingInvoice] = useState(false);
  const [invoice, setInvoice] = useState<Invoice | null>(null);
  const [paymentMethodId, setPaymentMethodId] = useState<number | "">("");
  const [confirmPayment, setConfirmPayment] = useState(false);
  const [cancelReason, setCancelReason] = useState("");
  const [cancelOpen, setCancelOpen] = useState(false);
  const [discountOpen, setDiscountOpen] = useState(false);
  const [discountCode, setDiscountCode] = useState("");
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [editProducts, setEditProducts] = useState<{ productId: number; name: string; quantity: number; unitPrice: number }[]>([]);
  const [allProductsList, setAllProductsList] = useState<Product[]>([]);
  const [selectedAddProductId, setSelectedAddProductId] = useState("");
  const [addQty, setAddQty] = useState(1);
  const [savingProducts, setSavingProducts] = useState(false);
  const [params, setParams] = useState({ search: "", pageNumber: 1, pageSize: 20, paymentStatus: "" });
  const { data, loading, error, reload } = useLoad(async () => {
    const [invoices, methods, sessions, allInvoices] = await Promise.all([
      invoiceApi.list({ Search: params.search, PageNumber: params.pageNumber, PageSize: params.pageSize, PaymentStatus: params.paymentStatus ? Number(params.paymentStatus) : undefined }),
      invoiceApi.paymentMethods(),
      sessionApi.list({ PageSize: 100 }),
      invoiceApi.list({ PageSize: 1000 })
    ]);
    return { invoices, methods, sessions, allInvoices };
  }, [params]);
  const invoices = useList<Invoice>(data?.invoices);
  const methods = (data?.methods || []) as PaymentMethod[];
  const sessions = useList<Session>(data?.sessions);
  const allInvoices = useList<Invoice>(data?.allInvoices);

  const paidSessionIds = new Set(
    allInvoices
      .filter((inv) => Number(inv.paymentStatus) === 3)
      .map((inv) => Number(inv.sessionId))
  );
  const unpaidSessions = sessions.filter((s) => !paidSessionIds.has(Number(s.sessionId)));

  const sessionOptions = unpaidSessions.map((s) => {
    const rawTable = s.tableName?.trim() || "";
    const tableStr = rawTable ? (/^(bàn|table)/i.test(rawTable) ? rawTable : `Bàn ${rawTable}`) : "Bàn";
    const codeStr = s.sessionCode || `#${s.sessionId}`;
    const labelStr = Number(s.status) === 1
      ? `${tableStr} - ${codeStr} - Đang chơi ${s.durationMinutes ?? 0} phút`
      : `${tableStr} - ${codeStr} - Đã kết thúc - ${dateTime(String(s.endedAtUtc || s.startedAtUtc))}`;
    return { value: String(s.sessionId), label: labelStr };
  });

  const loadDetail = useCallback(async (id: number) => {
    setInvoice(await invoiceApi.detail(id));
  }, []);

  useEffect(() => {
    const invoiceId = Number(new URLSearchParams(window.location.search).get("invoiceId"));
    if (!Number.isFinite(invoiceId) || invoiceId <= 0) return;

    loadDetail(invoiceId).catch((err) => toast(err.message || "Không tải được hóa đơn.", "error"));
  }, [loadDetail, toast]);

  async function pay() {
    if (!invoice) return;
    if (!paymentMethodId) {
      toast("Vui lòng chọn phương thức thanh toán.", "error");
      return;
    }
    try {
      await invoiceApi.pay({ invoiceId: invoice.invoiceId, paymentMethodId: Number(paymentMethodId), amount: invoice.grandTotalAmount || 0 });
      toast("Đã ghi nhận thanh toán.", "success");
      await reload();
      router.push("/operation/floor-map");
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không thể ghi nhận thanh toán.", "error");
      await loadDetail(invoice.invoiceId);
    }
  }

  async function cancelInvoice() {
    if (!invoice || !cancelReason.trim()) {
      toast("Vui lòng nhập lý do hủy hóa đơn.", "error");
      return;
    }
    await invoiceApi.cancel(invoice.invoiceId, cancelReason.trim())
      .then(async () => {
        toast("Hóa đơn đã bị hủy.", "success");
        setCancelOpen(false);
        setCancelReason("");
        await loadDetail(invoice.invoiceId);
        reload();
      })
      .catch(err => toast(err.message, "error"));
  }

  async function applyDiscount() {
    if (!invoice || !discountCode.trim()) {
      toast("Vui lòng nhập mã giảm giá.", "error");
      return;
    }
    await invoiceApi.discount(invoice.invoiceId, discountCode.trim())
      .then(async () => {
        toast("Đã áp dụng mã giảm giá.", "success");
        setDiscountOpen(false);
        setDiscountCode("");
        await loadDetail(invoice.invoiceId);
        reload();
      })
      .catch(err => toast(err.message, "error"));
  }

  async function removeDiscount() {
    if (!invoice) return;
    await invoiceApi.removeDiscount(invoice.invoiceId)
      .then(async () => {
        toast("Đã gỡ bỏ mã giảm giá.", "success");
        await loadDetail(invoice.invoiceId);
        reload();
      })
      .catch(err => toast(err.message, "error"));
  }

  useEffect(() => {
    if (editModalOpen) {
      productApi.list({ PageSize: 1000 })
        .then((res) => {
          const items = Array.isArray(res) ? res : (res as any)?.items || [];
          setAllProductsList(items);
        })
        .catch((err) => toast(err.message || "Không tải được danh sách sản phẩm.", "error"));
    }
  }, [editModalOpen, toast]);

  const updateEditQty = (productId: number, qty: number) => {
    if (qty < 0) qty = 0;
    
    const found = allProductsList.find((p) => p.productId === productId);
    const originalLine = (invoice?.lines || []).find(l => l.lineType === "PRODUCT" && l.productId === productId);
    const originalQty = originalLine ? Number(originalLine.quantity) : 0;

    if (found?.isStockTracked && (qty - originalQty) > found.stockQuantity) {
      toast(`Không đủ số lượng trong kho. Hiện chỉ còn ${found.stockQuantity} sản phẩm.`, "error");
      qty = originalQty + found.stockQuantity;
    }

    setEditProducts((prev) =>
      prev.map((p) => (p.productId === productId ? { ...p, quantity: qty } : p)).filter((p) => p.quantity > 0)
    );
  };

  const removeProductFromEdit = (productId: number) => {
    setEditProducts((prev) => prev.filter((p) => p.productId !== productId));
  };

  const addProductToEdit = () => {
    if (!selectedAddProductId) {
      toast("Vui lòng chọn sản phẩm.", "error");
      return;
    }
    const prodId = Number(selectedAddProductId);
    const found = allProductsList.find((p) => p.productId === prodId);
    if (!found) return;

    const existing = editProducts.find((p) => p.productId === prodId);
    const originalLine = (invoice?.lines || []).find(l => l.lineType === "PRODUCT" && l.productId === prodId);
    const originalQty = originalLine ? Number(originalLine.quantity) : 0;
    const currentEditQty = existing ? existing.quantity : 0;
    const newQty = currentEditQty + addQty;

    if (found.isStockTracked && (newQty - originalQty) > found.stockQuantity) {
      toast(`Không đủ số lượng trong kho. Hiện chỉ còn ${found.stockQuantity} sản phẩm.`, "error");
      return;
    }

    if (existing) {
      setEditProducts((prev) =>
        prev.map((p) => (p.productId === prodId ? { ...p, quantity: newQty } : p))
      );
    } else {
      setEditProducts((prev) => [
        ...prev,
        { productId: prodId, name: found.name, quantity: addQty, unitPrice: found.unitPrice }
      ]);
    }
    setSelectedAddProductId("");
    setAddQty(1);
    toast("Đã thêm sản phẩm vào danh sách chỉnh sửa.", "success");
  };

  const saveInvoiceProducts = async () => {
    if (!invoice) return;
    setSavingProducts(true);
    try {
      const payload = editProducts.map((p) => ({ productId: p.productId, quantity: p.quantity }));
      await invoiceApi.updateProducts(invoice.invoiceId, payload);
      toast("Đã cập nhật sản phẩm trong hóa đơn.", "success");
      setEditModalOpen(false);
      await loadDetail(invoice.invoiceId);
      await reload();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Cập nhật thất bại.", "error");
    } finally {
      setSavingProducts(false);
    }
  };

  return (
    <>
      <PageHeader title="Hóa đơn và thanh toán" description="Tạo hóa đơn, xem chi tiết và ghi nhận thanh toán." />
      <ListControls
        search={params.search}
        pageNumber={params.pageNumber}
        pageSize={params.pageSize}
        onChange={(next) => setParams((prev) => ({ ...prev, ...next }))}
        extra={
          <label>
            <span>Trạng thái</span>
            <select value={params.paymentStatus} onChange={(e) => setParams((prev) => ({ ...prev, paymentStatus: e.target.value, pageNumber: 1 }))}>
              <option value="">Tất cả</option>
              <option value="1">Chưa thanh toán</option>
              <option value="2">Đã thanh toán</option>
            </select>
          </label>
        }
      />
      <form className="card" style={{ display: "flex", flexWrap: "wrap", gap: "16px", alignItems: "flex-end", marginBottom: "18px" }} onSubmit={async (e) => {
        e.preventDefault();
        if (!selectedSessionId) {
          toast("Vui lòng chọn phiên chơi.", "error");
          return;
        }
        setCreatingInvoice(true);
        try {
          const created = await invoiceApi.generate(Number(selectedSessionId));
          setInvoice(await invoiceApi.detail(created.invoiceId));
          reload();
          toast("Thao tác thành công.", "success");
        } catch (err) {
          const message = err instanceof Error ? err.message : "Thao tác thất bại.";
          toast(message, "error");
        } finally {
          setCreatingInvoice(false);
        }
      }}>
        <h2 style={{ width: "100%", margin: "0 0 4px" }}>Tạo hóa đơn từ phiên chơi</h2>
        <div style={{ display: "flex", flexDirection: "column", gap: "6px", width: "500px", maxWidth: "100%" }}>
          <span>Chọn phiên chơi</span>
          <SearchableSelect
            options={sessionOptions}
            value={selectedSessionId}
            onChange={setSelectedSessionId}
            placeholder="-- Chọn hoặc tìm kiếm phiên chơi --"
            disabled={creatingInvoice}
          />
        </div>
        <div>
          <button type="submit" className="primary-btn" disabled={creatingInvoice || !selectedSessionId}>
            {creatingInvoice ? "Đang tạo hóa đơn..." : "Tạo hóa đơn"}
          </button>
        </div>
      </form>
      <StateBlock loading={loading} error={error} empty={!loading && !invoices.length} />
      <DataTable
        rows={invoices as unknown as Record<string, unknown>[]}
        columns={[
          { key: "invoiceCode", label: "Mã" },
          { key: "sessionId", label: "Phiên chơi" },
          { key: "grandTotalAmount", label: "Tổng tiền", render: (row) => <strong>{money(Number(row.grandTotalAmount || 0))}</strong> },
          { key: "paymentStatus", label: "Trạng thái thanh toán", render: (row) => Number(row.paymentStatus) === 3 ? <span className="badge green">Đã thanh toán</span> : <span className="badge yellow">Chưa thanh toán</span> }
        ]}
        actions={(row) => <button className="ghost-btn" onClick={() => loadDetail(Number(row.invoiceId)).catch((err) => toast(err.message, "error"))}>Chi tiết</button>}
      />
      <Pagination
        pageNumber={params.pageNumber}
        totalPages={getTotalPages(data?.invoices, params.pageSize)}
        onChange={(page) => setParams((prev) => ({ ...prev, pageNumber: page }))}
      />
      {invoice ? (
        <Modal title={invoice.invoiceCode || `Hóa đơn #${invoice.invoiceId}`} onClose={() => setInvoice(null)} size="large">
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '16px', paddingBottom: '16px', borderBottom: '1px solid var(--line)' }}>
            <div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '6px' }}>
                <span style={{ fontSize: '18px', fontWeight: 'bold' }}>{invoice.invoiceCode || `Hóa đơn #${invoice.invoiceId}`}</span>
                {Number(invoice.paymentStatus) === 3 ? <span className="badge green" style={{ fontSize: '13px', padding: '4px 10px' }}>ĐÃ THANH TOÁN</span> : Number(invoice.status) === 3 ? <span className="badge red" style={{ fontSize: '13px', padding: '4px 10px' }}>ĐÃ HỦY</span> : <span className="badge yellow" style={{ fontSize: '13px', padding: '4px 10px' }}>CHƯA THANH TOÁN</span>}
              </div>
              <p style={{ fontSize: '13px', color: 'var(--muted)', margin: 0 }}>Mã phiên chơi: #{invoice.sessionId}</p>
            </div>
            <div style={{ display: "flex", gap: "8px" }}>
              {Number(invoice.paymentStatus) !== 3 && Number(invoice.status) !== 3 && (
                <button
                  type="button"
                  className="primary-btn"
                  onClick={() => {
                    const initialProducts = (invoice.lines || [])
                      .filter((l) => l.lineType === "PRODUCT")
                      .map((l) => ({
                        productId: l.productId || 0,
                        name: l.description,
                        quantity: Number(l.quantity),
                        unitPrice: Number(l.unitPrice)
                      }));
                    setEditProducts(initialProducts);
                    setEditModalOpen(true);
                  }}
                >
                  ✏️ Chỉnh sửa dịch vụ/sản phẩm
                </button>
              )}
              <button className="ghost-btn" onClick={() => {
                const linesHtml = (invoice.lines || []).map(l => `
                  <tr>
                    <td style="padding: 8px; border-bottom: 1px solid #eee;">${l.lineType === 'TIME' ? 'Tiền giờ bàn' : 'Dịch vụ/Sản phẩm'}</td>
                    <td style="padding: 8px; border-bottom: 1px solid #eee;">${l.description}</td>
                    <td style="padding: 8px; border-bottom: 1px solid #eee; text-align: center;">${l.lineType === 'TIME' ? `${Math.round(Number(l.quantity) * 60)} phút` : l.quantity}</td>
                    <td style="padding: 8px; border-bottom: 1px solid #eee; text-align: right;">${l.lineType === 'TIME' ? `${money(l.unitPrice)}/giờ` : money(l.unitPrice)}</td>
                    <td style="padding: 8px; border-bottom: 1px solid #eee; text-align: right;">${money(l.lineTotalAmount)}</td>
                  </tr>
                `).join('');

                const printWindow = window.open("", "_blank");
                if (printWindow) {
                  printWindow.document.write(`<html><head><title>Hoa don ${invoice.invoiceCode || invoice.invoiceId}</title></head><body style="font-family: Arial, sans-serif; padding: 30px; max-width: 700px; margin: 0 auto; color: #333;">
                      <h1 style="text-align:center; margin-bottom: 4px;">HÓA ĐƠN THANH TOÁN</h1>
                      <h3 style="text-align:center; color: #666; margin-top: 0; font-weight: normal;">Mã: ${invoice.invoiceCode || invoice.invoiceId} (Phiên: #${invoice.sessionId})</h3>
                      <hr style="border: 1px dashed #ccc; margin: 20px 0;"/>
                      <table style="width: 100%; border-collapse: collapse; margin-bottom: 20px; font-size: 14px;">
                        <thead>
                          <tr style="background: #f8f9fa; text-align: left;">
                            <th style="padding: 8px; border-bottom: 2px solid #ddd;">Loại</th>
                            <th style="padding: 8px; border-bottom: 2px solid #ddd;">Diễn giải</th>
                            <th style="padding: 8px; border-bottom: 2px solid #ddd; text-align: center;">SL</th>
                            <th style="padding: 8px; border-bottom: 2px solid #ddd; text-align: right;">Đơn giá</th>
                            <th style="padding: 8px; border-bottom: 2px solid #ddd; text-align: right;">Thành tiền</th>
                          </tr>
                        </thead>
                        <tbody>${linesHtml || '<tr><td colspan="5" style="text-align:center; padding: 12px;">Không có dữ liệu chi tiết</td></tr>'}</tbody>
                      </table>
                      <hr style="border: 1px dashed #ccc; margin: 20px 0;"/>
                      <div style="display:flex; justify-content:space-between; margin-bottom: 8px; font-size: 15px;"><span>Tổng tiền giờ chơi:</span> <span>${money(invoice.timeSubtotalAmount || 0)}</span></div>
                      <div style="display:flex; justify-content:space-between; margin-bottom: 8px; font-size: 15px;"><span>Tổng tiền dịch vụ/sản phẩm:</span> <span>${money(invoice.productSubtotalAmount || 0)}</span></div>
                      ${(invoice.discountAmount || 0) > 0 ? `<div style="display:flex; justify-content:space-between; margin-bottom: 8px; font-size: 15px; color: #d9534f;"><span>Giảm giá:</span> <span>-${money(invoice.discountAmount || 0)}</span></div>` : ''}
                      <hr style="border: 1px solid #333; margin: 16px 0;"/>
                      <div style="display:flex; justify-content:space-between; font-size: 20px; font-weight: bold;"><span>TỔNG THANH TOÁN:</span> <span>${money(invoice.grandTotalAmount || 0)}</span></div>
                      <p style="text-align:center; margin-top: 40px; font-style: italic; color: #777;">Cảm ơn quý khách và hẹn gặp lại!</p>
                      <script>setTimeout(() => window.print(), 500);</script>
                    </body></html>`);
                  printWindow.document.close();
                }
              }}>🖨️ In bill chi tiết</button>
            </div>
          </div>

          <div style={{ marginBottom: '24px' }}>
            <h4 style={{ margin: '0 0 12px', fontSize: '15px', fontWeight: 600 }}>Chi tiết mục tính tiền</h4>
            <div style={{ border: '1px solid var(--line)', borderRadius: '8px', overflow: 'hidden' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
                <thead style={{ background: 'var(--soft)', borderBottom: '1px solid var(--line)', textAlign: 'left' }}>
                  <tr>
                    <th style={{ padding: '10px 14px' }}>Loại</th>
                    <th style={{ padding: '10px 14px' }}>Diễn giải</th>
                    <th style={{ padding: '10px 14px', textAlign: 'center' }}>SL</th>
                    <th style={{ padding: '10px 14px', textAlign: 'right' }}>Đơn giá</th>
                    <th style={{ padding: '10px 14px', textAlign: 'right' }}>Thành tiền</th>
                  </tr>
                </thead>
                <tbody>
                  {(invoice.lines && invoice.lines.length > 0) ? invoice.lines.map((l) => (
                    <tr key={l.invoiceLineId} style={{ borderBottom: '1px solid var(--line)' }}>
                      <td style={{ padding: '10px 14px' }}>
                        <span className={`badge ${l.lineType === 'TIME' ? 'blue' : 'purple'}`} style={{ fontSize: '12px' }}>
                          {l.lineType === 'TIME' ? 'Tiền giờ' : 'Dịch vụ'}
                        </span>
                      </td>
                      <td style={{ padding: '10px 14px', fontWeight: 500 }}>{l.description}</td>
                      <td style={{ padding: '10px 14px', textAlign: 'center' }}>{l.lineType === 'TIME' ? `${Math.round(Number(l.quantity) * 60)} phút` : l.quantity}</td>
                      <td style={{ padding: '10px 14px', textAlign: 'right', color: 'var(--muted)' }}>{l.lineType === 'TIME' ? `${money(l.unitPrice)}/giờ` : money(l.unitPrice)}</td>
                      <td style={{ padding: '10px 14px', textAlign: 'right', fontWeight: 600 }}>{money(l.lineTotalAmount)}</td>
                    </tr>
                  )) : (
                    <tr>
                      <td colSpan={5} style={{ padding: '24px', textAlign: 'center', color: 'var(--muted)' }}>Không có chi tiết dòng nào được tải</td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>

          {(invoice.discounts && invoice.discounts.length > 0) ? (
            <div style={{ marginBottom: '24px', background: '#fff9e6', border: '1px solid #ffe0b2', padding: '12px 16px', borderRadius: '8px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '6px' }}>
                <span style={{ fontWeight: 600, fontSize: '14px', color: '#b76e00' }}>🎟️ Khuyến mãi đã áp dụng:</span>
                {invoice.status !== 3 && invoice.paymentStatus !== 2 && (
                  <button type="button" className="ghost-btn" style={{ fontSize: '12px', padding: '2px 8px', color: '#d9534f', cursor: 'pointer' }} onClick={removeDiscount}>
                    🗑️ Gỡ mã
                  </button>
                )}
              </div>
              {invoice.discounts.map(d => (
                <div key={d.invoiceDiscountId} style={{ display: 'flex', justifyContent: 'space-between', fontSize: '14px', color: '#8c5400' }}>
                  <span>{d.descriptionSnapshot || `Mã giảm giá #${d.discountId}`}</span>
                  <span style={{ fontWeight: 'bold' }}>-{money(d.amountApplied)}</span>
                </div>
              ))}
            </div>
          ) : null}

          <div style={{ background: 'var(--soft)', padding: '16px 20px', borderRadius: '8px', border: '1px solid var(--line)', marginBottom: '24px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px', fontSize: '14px', color: 'var(--muted)' }}>
              <span>Tiền giờ chơi:</span>
              <span style={{ fontWeight: 500, color: 'var(--text)' }}>{money(invoice.timeSubtotalAmount || 0)}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px', fontSize: '14px', color: 'var(--muted)' }}>
              <span>Tiền dịch vụ / sản phẩm:</span>
              <span style={{ fontWeight: 500, color: 'var(--text)' }}>{money(invoice.productSubtotalAmount || 0)}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '12px', fontSize: '14px', color: 'var(--muted)' }}>
              <span>Giảm giá:</span>
              <span style={{ fontWeight: 600, color: 'var(--danger)' }}>-{money(invoice.discountAmount || 0)}</span>
            </div>
            <div style={{ borderTop: '1px dashed var(--line)', paddingTop: '12px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <span style={{ fontSize: '16px', fontWeight: 'bold' }}>TỔNG THANH TOÁN:</span>
              <span style={{ fontSize: '22px', fontWeight: 'bold', color: 'var(--brand)' }}>{money(invoice.grandTotalAmount || 0)}</span>
            </div>
          </div>

          {Number(invoice.status) === 3 ? (
            <div className="state-card" style={{ background: '#fdeded', color: '#5f2120', border: '1px solid #f4c3c2', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              Hóa đơn này đã bị hủy.
            </div>
          ) : Number(invoice.paymentStatus) !== 3 ? (
            <div style={{ background: 'var(--soft)', padding: '16px', borderRadius: '12px', border: '1px solid var(--line)', display: 'flex', flexDirection: 'column', gap: '14px' }}>
              <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap', alignItems: 'center' }}>
                <select style={{ flex: '1 1 220px' }} value={paymentMethodId} onChange={(event) => setPaymentMethodId(Number(event.target.value))}>
                  <option value="">-- Chọn phương thức thanh toán --</option>
                  {methods.map((method) => <option key={method.paymentMethodId} value={method.paymentMethodId}>{method.name}</option>)}
                </select>
                <button className="primary-btn" onClick={() => setConfirmPayment(true)}>Thanh toán ngay</button>
                <button className="ghost-btn" onClick={() => setDiscountOpen(true)}>Áp dụng mã giảm giá</button>
                <button className="danger-btn" style={{ marginLeft: "auto" }} onClick={() => setCancelOpen(true)}>Hủy hóa đơn</button>
              </div>

              {(() => {
                const selected = methods.find(m => m.paymentMethodId === Number(paymentMethodId));
                const isBank = selected && (selected.code === "BANK" || selected.name.toLowerCase().includes("chuyển khoản") || selected.name.toLowerCase().includes("bank") || selected.name.toLowerCase().includes("qr") || selected.name.toLowerCase().includes("chuyen khuan"));
                if (!isBank) return null;

                const amount = invoice.grandTotalAmount || 0;
                const addInfo = `HD${invoice.invoiceId}`;
                let accountNo = "989420048989";
                let bankCode = "MB";
                let accountName = "TRAN CONG DINH";

                try {
                  if (selected.description && selected.description.startsWith("{")) {
                    const parsed = JSON.parse(selected.description);
                    if (parsed.vietqr || parsed.accountNo) {
                      if (parsed.accountNo) accountNo = parsed.accountNo;
                      if (parsed.bankCode) bankCode = parsed.bankCode;
                      if (parsed.accountName) accountName = parsed.accountName;
                    }
                  }
                } catch { }

                const qrUrl = `${API_BASE_URL}/api/invoices/${invoice.invoiceId}/qr-code?amt=${amount}&t=${Date.now()}`;

                return (
                  <div style={{ background: '#f8fbfa', border: '1.5px solid #0f5d4b', borderRadius: '12px', padding: '20px', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '14px' }}>
                    <div style={{ fontWeight: 700, fontSize: '15px', color: '#0f5d4b', display: 'flex', alignItems: 'center', gap: '6px' }}>
                      <span>⚡</span> QUÉT MÃ VIETQR ĐỂ THANH TOÁN TỰ ĐỘNG
                    </div>

                    <div style={{ background: 'white', padding: '12px', borderRadius: '12px', boxShadow: '0 4px 16px rgba(0,0,0,0.08)', border: '1px solid var(--line)' }}>
                      <img src={qrUrl} alt="VietQR Thanh Toán" style={{ width: '100%', maxWidth: '300px', display: 'block', borderRadius: '8px' }} />
                    </div>

                    <div style={{ width: '100%', fontSize: '13px', background: 'white', padding: '14px', borderRadius: '8px', border: '1px solid var(--line)', display: 'grid', gap: '8px' }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ color: 'var(--muted)' }}>Ngân hàng:</span>
                        <strong>{bankCode}</strong>
                      </div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ color: 'var(--muted)' }}>Số tài khoản:</span>
                        <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                          <strong style={{ color: 'var(--ink)', fontSize: '15px' }}>{accountNo}</strong>
                          <button type="button" className="ghost-btn compact" style={{ padding: '2px 8px', fontSize: '12px' }} onClick={() => { navigator.clipboard.writeText(accountNo); toast("Đã sao chép số tài khoản!", "success"); }}>📋 Sao chép</button>
                        </div>
                      </div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ color: 'var(--muted)' }}>Chủ tài khoản:</span>
                        <strong>{accountName}</strong>
                      </div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ color: 'var(--muted)' }}>Số tiền thanh toán:</span>
                        <strong style={{ color: '#0f5d4b', fontSize: '16px' }}>{money(amount)}</strong>
                      </div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderTop: '1px dashed var(--line)', paddingTop: '8px', marginTop: '4px' }}>
                        <span style={{ color: 'var(--muted)' }}>Nội dung chuyển khoản:</span>
                        <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                          <strong style={{ color: '#d9534f', fontSize: '15px' }}>{addInfo}</strong>
                          <button type="button" className="ghost-btn compact" style={{ padding: '2px 8px', fontSize: '12px' }} onClick={() => { navigator.clipboard.writeText(addInfo); toast("Đã sao chép nội dung!", "success"); }}>📋 Sao chép</button>
                        </div>
                      </div>
                    </div>

                    <p style={{ fontSize: '12px', color: 'var(--muted)', margin: 0, textAlign: 'center' }}>
                      💡 Khách hàng mở ứng dụng Ngân hàng hoặc Momo/ZaloPay quét mã trên. Số tiền và nội dung sẽ được tự động điền chính xác tuyệt đối.
                    </p>
                  </div>
                );
              })()}
            </div>
          ) : (
            <div className="state-card" style={{ background: '#e4f7ec', color: '#187344', border: '1px solid #c2ebd5', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              Hóa đơn này đã được thanh toán hoàn tất. Không thể sửa đổi hay thanh toán thêm.
            </div>
          )}
        </Modal>
      ) : null}
      {confirmPayment && invoice ? <ConfirmDialog title="Ghi nhận thanh toán" message={`Xác nhận thanh toán ${money(invoice.grandTotalAmount || 0)} cho hóa đơn này?`} confirmLabel="Thanh toán" onCancel={() => setConfirmPayment(false)} onConfirm={async () => { setConfirmPayment(false); await pay(); }} /> : null}
      {discountOpen && invoice ? <Modal title="Áp dụng mã giảm giá" onClose={() => setDiscountOpen(false)}>
        <div className="form-stack">
          <label><span>Mã giảm giá</span><input value={discountCode} onChange={(event) => setDiscountCode(event.target.value)} placeholder="Nhập mã giảm giá..." autoFocus /></label>
          <div className="modal-actions"><button className="ghost-btn" onClick={() => setDiscountOpen(false)}>Hủy</button><button className="primary-btn" onClick={applyDiscount}>Áp dụng</button></div>
        </div>
      </Modal> : null}
      {cancelOpen && invoice ? <Modal title="Hủy hóa đơn" onClose={() => setCancelOpen(false)}>
        <div className="form-stack">
          <label><span>Lý do hủy</span><textarea rows={4} value={cancelReason} onChange={(event) => setCancelReason(event.target.value)} /></label>
          <div className="modal-actions"><button className="ghost-btn" onClick={() => setCancelOpen(false)}>Hủy</button><button className="danger-btn" onClick={cancelInvoice}>Xác nhận hủy hóa đơn</button></div>
        </div>
      </Modal> : null}
      {editModalOpen && invoice ? (
        <Modal title={`Chỉnh sửa dịch vụ/sản phẩm - ${invoice.invoiceCode || `Hóa đơn #${invoice.invoiceId}`}`} onClose={() => setEditModalOpen(false)} size="large">
          <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
            <div style={{ maxHeight: '350px', overflowY: 'auto', border: '1px solid var(--line)', borderRadius: '8px' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '14px' }}>
                <thead style={{ background: 'var(--soft)', borderBottom: '1px solid var(--line)', textAlign: 'left' }}>
                  <tr>
                    <th style={{ padding: '10px 14px' }}>Tên sản phẩm</th>
                    <th style={{ padding: '10px 14px', textAlign: 'right' }}>Đơn giá</th>
                    <th style={{ padding: '10px 14px', textAlign: 'center' }}>Số lượng</th>
                    <th style={{ padding: '10px 14px', textAlign: 'right' }}>Thành tiền</th>
                    <th style={{ padding: '10px 14px', textAlign: 'center' }}>Hành động</th>
                  </tr>
                </thead>
                <tbody>
                  {editProducts.length > 0 ? editProducts.map((p) => (
                    <tr key={p.productId} style={{ borderBottom: '1px solid var(--line)' }}>
                      <td style={{ padding: '10px 14px', fontWeight: 500 }}>{p.name}</td>
                      <td style={{ padding: '10px 14px', textAlign: 'right' }}>{money(p.unitPrice)}</td>
                      <td style={{ padding: '10px 14px', textAlign: 'center' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', justifyContent: 'center' }}>
                          <button className="ghost-btn compact" type="button" style={{ padding: '2px 8px', fontSize: '14px', minWidth: '24px' }} onClick={() => updateEditQty(p.productId, p.quantity - 1)}>-</button>
                          <input
                            type="number"
                            min={1}
                            style={{ width: '60px', textAlign: 'center', padding: '2px 4px', border: '1px solid var(--line)', borderRadius: '4px' }}
                            value={p.quantity}
                            onChange={(e) => updateEditQty(p.productId, Math.max(1, Number(e.target.value)))}
                          />
                          <button className="ghost-btn compact" type="button" style={{ padding: '2px 8px', fontSize: '14px', minWidth: '24px' }} onClick={() => updateEditQty(p.productId, p.quantity + 1)}>+</button>
                        </div>
                      </td>
                      <td style={{ padding: '10px 14px', textAlign: 'right', fontWeight: 600 }}>{money(p.quantity * p.unitPrice)}</td>
                      <td style={{ padding: '10px 14px', textAlign: 'center' }}>
                        <button className="ghost-btn" type="button" style={{ color: 'var(--danger)', padding: '2px 8px' }} onClick={() => removeProductFromEdit(p.productId)}>🗑️ Xóa</button>
                      </td>
                    </tr>
                  )) : (
                    <tr>
                      <td colSpan={5} style={{ padding: '24px', textAlign: 'center', color: 'var(--muted)' }}>Không có sản phẩm nào. Vui lòng thêm sản phẩm bên dưới.</td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>

            <div style={{ background: 'var(--soft)', padding: '16px', borderRadius: '8px', border: '1px solid var(--line)' }}>
              <h5 style={{ margin: '0 0 12px', fontSize: '14px', fontWeight: 600 }}>Thêm sản phẩm mới</h5>
              <div style={{ display: 'flex', gap: '12px', alignItems: 'flex-end', flexWrap: 'wrap' }}>
                <div style={{ flex: '1 1 250px' }}>
                  <span style={{ fontSize: '13px', display: 'block', marginBottom: '4px', color: 'var(--muted)' }}>Chọn sản phẩm</span>
                  <SearchableSelect
                    options={allProductsList.map((prod) => ({
                      value: String(prod.productId),
                      label: `${prod.name} - ${money(prod.unitPrice)} (Tồn: ${prod.stockQuantity})`
                    }))}
                    value={selectedAddProductId}
                    onChange={setSelectedAddProductId}
                    placeholder="-- Tìm kiếm sản phẩm để thêm --"
                  />
                </div>
                <div style={{ width: '100px' }}>
                  <span style={{ fontSize: '13px', display: 'block', marginBottom: '4px', color: 'var(--muted)' }}>Số lượng</span>
                  <input
                    type="number"
                    min={1}
                    style={{ width: '100%', padding: '6px 10px', border: '1px solid var(--line)', borderRadius: '6px', height: '38px' }}
                    value={addQty}
                    onChange={(e) => setAddQty(Math.max(1, Number(e.target.value)))}
                  />
                </div>
                <button className="primary-btn" type="button" style={{ height: '38px' }} onClick={addProductToEdit}>
                  Thêm vào list
                </button>
              </div>
            </div>

            <div className="modal-actions" style={{ marginTop: '12px', display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
              <button className="ghost-btn" onClick={() => setEditModalOpen(false)}>Hủy</button>
              <button className="primary-btn" onClick={saveInvoiceProducts} disabled={savingProducts}>
                {savingProducts ? "Đang lưu..." : "Lưu thay đổi"}
              </button>
            </div>
          </div>
        </Modal>
      ) : null}
    </>
  );
}
