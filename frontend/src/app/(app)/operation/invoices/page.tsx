"use client";

import { useCallback, useEffect, useMemo, useState, useRef } from "react";
import { useRouter } from "next/navigation";
import { customerApi, invoiceApi, sessionApi, productApi, discountApi } from "@/lib/api/endpoints";
import { getTotalPages, API_BASE_URL } from "@/lib/api/client";
import { customerReviewsApi } from "@/lib/api/customerReviewsApi";
import { money, dateTime } from "@/lib/status";
import { formatDateTimeLocal } from "@/lib/dateTime";
import { parseBankTransferConfig } from "@/lib/paymentQr";
import { PaymentQrCard } from "@/components/payments/PaymentQrCard";
import { DepositRefundSummaryPanel } from "@/components/refunds/DepositRefundSummaryPanel";
import { ConfirmDialog, DataTable, ListControls, PageHeader, StateBlock, useList, useLoad, Modal, Pagination, SearchableSelect } from "@/components/ui";
import { useToast } from "@/components/toast";
import { openInvoiceDisplay } from "@/lib/invoiceDisplay";
import type { Invoice, PaymentMethod, Product, ReviewInvitationLink, Session, Discount } from "@/types";

const EDIT_PRODUCT_PAGE_SIZE = 6;

export default function InvoicesPage() {
  const toast = useToast();
  const [selectedSessionId, setSelectedSessionId] = useState("");
  const [creatingInvoice, setCreatingInvoice] = useState(false);
  const [invoice, setInvoice] = useState<Invoice | null>(null);
  const invoiceRef = useRef<Invoice | null>(null);
  useEffect(() => { invoiceRef.current = invoice; }, [invoice]);
  const [reviewInvitation, setReviewInvitation] = useState<ReviewInvitationLink | null>(null);
  const [paymentMethodId, setPaymentMethodId] = useState<number | "">("");
  const [confirmPayment, setConfirmPayment] = useState(false);
  const [qrExpiresAt, setQrExpiresAt] = useState<number | null>(null);
  const [qrCountdown, setQrCountdown] = useState<string>("");
  const [cancelReason, setCancelReason] = useState("");
  const [cancelOpen, setCancelOpen] = useState(false);
  const [discountOpen, setDiscountOpen] = useState(false);
  const [discountCode, setDiscountCode] = useState("");
  const [discountMessage, setDiscountMessage] = useState<{ text: string; type: "error" | "success" } | null>(null);
  const [availableDiscounts, setAvailableDiscounts] = useState<Discount[]>([]);
  const [loadingDiscounts, setLoadingDiscounts] = useState(false);
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [editProducts, setEditProducts] = useState<{ productId: number; name: string; quantity: number; unitPrice: number }[]>([]);
  const [editProductSearch, setEditProductSearch] = useState("");
  const [editProductPage, setEditProductPage] = useState(1);
  const [allProductsList, setAllProductsList] = useState<Product[]>([]);
  const [selectedAddProductId, setSelectedAddProductId] = useState("");
  const [addQty, setAddQty] = useState(1);
  const [savingProducts, setSavingProducts] = useState(false);
  const [loyaltyPhone, setLoyaltyPhone] = useState("");
  const [loyaltyName, setLoyaltyName] = useState("");
  const [updatingCustomer, setUpdatingCustomer] = useState(false);
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
  const filteredEditProducts = useMemo(() => {
    const keyword = editProductSearch.trim().toLocaleLowerCase("vi");
    if (!keyword) return editProducts;
    return editProducts.filter((product) =>
      product.name.toLocaleLowerCase("vi").includes(keyword) ||
      String(product.productId).includes(keyword)
    );
  }, [editProductSearch, editProducts]);
  const editProductTotalPages = Math.max(1, Math.ceil(filteredEditProducts.length / EDIT_PRODUCT_PAGE_SIZE));
  const visibleEditProducts = filteredEditProducts.slice(
    (editProductPage - 1) * EDIT_PRODUCT_PAGE_SIZE,
    editProductPage * EDIT_PRODUCT_PAGE_SIZE
  );
  const editProductTotalQuantity = editProducts.reduce((total, product) => total + product.quantity, 0);
  const editProductTotalAmount = editProducts.reduce((total, product) => total + product.quantity * product.unitPrice, 0);

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
    const detail = await invoiceApi.detail(id);
    let customerPhone = detail.customerPhone || "";
    let customerName = detail.customerName || "";

    if (detail.customerId && (!customerPhone || !customerName)) {
      try {
        const customer = await customerApi.detail(detail.customerId);
        customerPhone ||= customer.phoneNumber || "";
        customerName ||= customer.fullName || "";
      } catch {
        // Vẫn hiển thị chi tiết hóa đơn nếu không tải được hồ sơ khách hàng.
      }
    }

    setInvoice({ ...detail, customerPhone, customerName });
    setReviewInvitation(null);
    setLoyaltyPhone(customerPhone);
    setLoyaltyName(customerName);
    setQrExpiresAt(null);
  }, []);

  useEffect(() => {
    const invoiceId = Number(new URLSearchParams(window.location.search).get("invoiceId"));
    if (!Number.isFinite(invoiceId) || invoiceId <= 0) return;

    loadDetail(invoiceId).catch((err) => toast(err.message || "Không tải được hóa đơn.", "error"));
  }, [loadDetail, toast]);

  useEffect(() => {
    const current = invoiceRef.current;
    if (!current || Number(current.paymentStatus) === 3 || Number(current.status) === 3) {
      return;
    }
    const hasQrSession = Boolean(qrExpiresAt) || Boolean(current.note && current.note.includes("PayOS_OrderCode:")) || Boolean(paymentMethodId);
    if (!hasQrSession) {
      return;
    }
    const checkInvoicePayment = async () => {
      const inv = invoiceRef.current;
      if (!inv || Number(inv.paymentStatus) === 3 || Number(inv.status) === 3) return;
      try {
        const updated = await invoiceApi.detail(inv.invoiceId);
        if (Number(updated.paymentStatus) === 3 || (Number(updated.paidAmount || 0) > 0 && Number(updated.paidAmount || 0) >= Number(updated.grandTotalAmount || 0))) {
          setInvoice(updated);
          setQrExpiresAt(null);
          setPaymentMethodId("");
          setConfirmPayment(false);
          toast("🎉 Chuyển khoản thành công! Hóa đơn đã được kiểm tra và hoàn tất thanh toán.", "success");
          reload(true);
        } else if (updated.paidAmount !== undefined && inv.paidAmount !== undefined && updated.paidAmount > inv.paidAmount) {
          setInvoice(updated);
          reload(true);
          toast(`Đã nhận được thanh toán chuyển khoản: ${money((updated.paidAmount || 0) - (inv.paidAmount || 0))}`, "success");
        } else if (updated.note !== inv.note || updated.status !== inv.status) {
          setInvoice(updated);
        }
      } catch {
        // Ignore polling errors during background check
      }
    };
    const timer = window.setInterval(checkInvoicePayment, 1000);
    return () => window.clearInterval(timer);
  }, [qrExpiresAt, paymentMethodId, reload, toast]);

  async function pay() {
    if (!invoice) return;
    if (!paymentMethodId) {
      toast("Vui lòng chọn phương thức thanh toán.", "error");
      return;
    }
    try {
      const amountDue = Math.max(0, Number(invoice.remainingAmount ?? ((invoice.grandTotalAmount || 0) - (invoice.paidAmount || 0))));
      const payment = await invoiceApi.pay({
        invoiceId: invoice.invoiceId,
        paymentMethodId: Number(paymentMethodId),
        amount: amountDue,
        phoneNumber: loyaltyPhone.trim() || undefined,
        customerName: loyaltyName.trim() || undefined
      });
      toast("Đã ghi nhận thanh toán.", "success");
      const detail = await invoiceApi.detail(invoice.invoiceId);
      setReviewInvitation(payment.reviewInvitation || null);
      setInvoice({ ...detail, reviewInvitation: payment.reviewInvitation });
      await reload(true);
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
        await reload(true);
      })
      .catch(err => toast(err.message, "error"));
  }

  const openDiscountModal = async () => {
    setDiscountOpen(true);
    setDiscountMessage(null);
    setDiscountCode("");
    setLoadingDiscounts(true);
    try {
      let personal: Discount[] = [];
      if (invoice?.customerId) {
        const res = await discountApi.list({ CustomerId: invoice.customerId, IsActive: true, PageSize: 50 });
        personal = Array.isArray(res) ? res : (res as any)?.items || [];
      }

      const now = new Date();
      const validVouchers = personal.filter((d: Discount) => {
        if (!d.isActive) return false;
        if (!d.isVoucher || !d.customerId || (d.pointsRequired || 0) > 0) return false;
        if (d.endsAtUtc && new Date(String(d.endsAtUtc)) < now) return false;
        if (d.maxUsage && (d.usageCount || 0) >= d.maxUsage) return false;
        if ((d.usageCount || 0) > 0) return false;
        return true;
      });
      setAvailableDiscounts(validVouchers);
    } catch (err) {
      console.error("Failed to load discounts", err);
    } finally {
      setLoadingDiscounts(false);
    }
  };

  async function applyDiscount(codeToApply?: string) {
    const targetCode = (typeof codeToApply === "string" ? codeToApply : discountCode).trim();
    if (!invoice || !targetCode) {
      setDiscountMessage({ text: "Vui lòng nhập hoặc chọn mã giảm giá.", type: "error" });
      toast("Vui lòng nhập hoặc chọn mã giảm giá.", "error");
      return;
    }
    if (typeof codeToApply === "string") {
      setDiscountCode(codeToApply);
    }
    setDiscountMessage(null);
    await invoiceApi.discount(invoice.invoiceId, targetCode)
      .then(async () => {
        setDiscountMessage({ text: `Đã áp dụng mã "${targetCode}" thành công!`, type: "success" });
        toast("Đã áp dụng mã giảm giá.", "success");
        setTimeout(async () => {
          setDiscountOpen(false);
          setDiscountCode("");
          setDiscountMessage(null);
        }, 1000);
        await loadDetail(invoice.invoiceId);
        reload();
      })
      .catch(err => {
        const msg = err.message || "Áp dụng mã giảm giá thất bại.";
        setDiscountMessage({ text: msg, type: "error" });
        toast(msg, "error");
      });
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

  useEffect(() => {
    setEditProductPage((current) => Math.min(current, editProductTotalPages));
  }, [editProductTotalPages]);

  const updateEditQty = (productId: number, qty: number) => {
    if (qty < 0) qty = 0;

    const found = allProductsList.find((p) => p.productId === productId);
    const originalLine = (invoice?.lines || []).find(l => l.lineType === "PRODUCT" && l.productId === productId);
    const originalQty = originalLine ? Number(originalLine.quantity) : 0;

    if (found && (qty - originalQty) > found.stockQuantity) {
      toast(`Không đủ số lượng trong kho. Bạn chỉ có thể tăng thêm tối đa ${found.stockQuantity} sản phẩm.`, "error");
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

    if (found && (newQty - originalQty) > found.stockQuantity) {
      const maxAdd = found.stockQuantity + originalQty - currentEditQty;
      if (maxAdd > 0) {
        toast(`Không đủ số lượng trong kho. Bạn chỉ có thể thêm tối đa ${maxAdd} sản phẩm nữa.`, "error");
      } else {
        toast(`Không đủ số lượng trong kho. Bạn đã dùng hết tồn kho cho sản phẩm này.`, "error");
      }
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
            <option value="2">Đã thanh toán một phần</option>
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
          await loadDetail(created.invoiceId);
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
          { key: "paymentStatus", label: "Trạng thái thanh toán", render: (row) => Number(row.paymentStatus) === 3
            ? <span className="badge green">Đã thanh toán</span>
            : Number(row.paymentStatus) === 2
              ? <span className="badge blue">Đã thanh toán một phần</span>
              : <span className="badge yellow">Chưa thanh toán</span> }
        ]}
        actions={(row) => (
          <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
            <button className="ghost-btn" onClick={() => loadDetail(Number(row.invoiceId)).catch((err) => toast(err.message, "error"))}>Chi tiết</button>
            <button className="secondary-btn" onClick={() => openInvoiceDisplay(Number(row.invoiceId))}>Mở màn hình khách</button>
          </div>
        )}
      />
      <Pagination
        pageNumber={params.pageNumber}
        totalPages={getTotalPages(data?.invoices, params.pageSize)}
        onChange={(page) => setParams((prev) => ({ ...prev, pageNumber: page }))}
      />
      {invoice ? (
        <Modal title={invoice.invoiceCode || `Hóa đơn #${invoice.invoiceId}`} onClose={() => { setInvoice(null); setQrExpiresAt(null); }} size="large">
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '16px', paddingBottom: '16px', borderBottom: '1px solid var(--line)' }}>
            <div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '6px' }}>
                <span style={{ fontSize: '18px', fontWeight: 'bold' }}>{invoice.invoiceCode || `Hóa đơn #${invoice.invoiceId}`}</span>
              {Number(invoice.paymentStatus) === 3
                ? <span className="badge green" style={{ fontSize: '13px', padding: '4px 10px' }}>ĐÃ THANH TOÁN</span>
                : Number(invoice.status) === 3
                  ? <span className="badge red" style={{ fontSize: '13px', padding: '4px 10px' }}>ĐÃ HỦY</span>
                  : Number(invoice.paymentStatus) === 2
                    ? <span className="badge blue" style={{ fontSize: '13px', padding: '4px 10px' }}>ĐÃ THANH TOÁN MỘT PHẦN</span>
                    : <span className="badge yellow" style={{ fontSize: '13px', padding: '4px 10px' }}>CHƯA THANH TOÁN</span>}
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
                    setEditProductSearch("");
                    setEditProductPage(1);
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
              <button className="secondary-btn" type="button" onClick={() => openInvoiceDisplay(invoice.invoiceId)}>
                Mở màn hình khách
              </button>
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
                {Number(invoice.status) !== 3 && Number(invoice.paymentStatus) !== 3 && (
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
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px', fontSize: '14px', color: 'var(--muted)' }}>
              <span>Đã đặt cọc:</span>
              <span style={{ fontWeight: 600, color: '#0f766e' }}>{money(invoice.depositAppliedAmount || 0)}</span>
            </div>
            {(invoice.depositRefundAmount || 0) > 0 ? (
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px', fontSize: '14px', color: 'var(--muted)' }}>
                <span>Hoàn lại do cọc dư:</span>
                <span style={{ fontWeight: 600, color: '#7c3aed' }}>{money(invoice.depositRefundAmount || 0)}</span>
              </div>
            ) : null}
            <DepositRefundSummaryPanel summary={invoice.depositRefundSummary} />
            <div style={{ borderTop: '1px dashed var(--line)', paddingTop: '12px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <span style={{ fontSize: '16px', fontWeight: 'bold' }}>TỔNG THANH TOÁN:</span>
              <span style={{ fontSize: '22px', fontWeight: 'bold', color: 'var(--brand)' }}>{money(invoice.grandTotalAmount || 0)}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: '10px', fontSize: '15px' }}>
              <span>Còn phải trả:</span>
              <strong>{money(invoice.remainingAmount ?? Math.max(0, (invoice.grandTotalAmount || 0) - (invoice.paidAmount || 0)))}</strong>
            </div>
          </div>

          <div style={{ background: '#f0fdf4', border: '1px solid #bbf7d0', padding: '14px 16px', borderRadius: '10px', marginBottom: '20px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
              <span style={{ fontWeight: 600, fontSize: '14px', color: '#166534' }}>🪙 Tích điểm & Khách hàng:</span>
              {invoice.customerId ? (
                <span className="badge green" style={{ fontSize: '12px' }}>Đã gắn ID Khách: #{invoice.customerId}</span>
              ) : (
                <span className="badge yellow" style={{ fontSize: '12px' }}>Chưa gắn khách</span>
              )}
            </div>
            {Number(invoice.paymentStatus) !== 3 && Number(invoice.status) !== 3 && (
              <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap', alignItems: 'flex-end', marginTop: '10px' }}>
                <label style={{ flex: '1 1 180px' }}>
                  <span style={{ fontSize: '12px', color: '#15803d', display: 'block', marginBottom: '4px' }}>Số điện thoại (Tích điểm / Dùng Voucher)</span>
                  <input
                    type="text"
                    placeholder="Nhập SĐT..."
                    value={loyaltyPhone}
                    onChange={(e) => setLoyaltyPhone(e.target.value)}
                    style={{ width: '100%', padding: '6px 10px', borderRadius: '6px', border: '1px solid #86efac', background: '#fff' }}
                  />
                </label>
                <label style={{ flex: '1 1 180px' }}>
                  <span style={{ fontSize: '12px', color: '#15803d', display: 'block', marginBottom: '4px' }}>Tên khách hàng</span>
                  <input
                    type="text"
                    placeholder="Tên khách hàng..."
                    value={loyaltyName}
                    onChange={(e) => setLoyaltyName(e.target.value)}
                    style={{ width: '100%', padding: '6px 10px', borderRadius: '6px', border: '1px solid #86efac', background: '#fff' }}
                  />
                </label>
                <button
                  type="button"
                  className="secondary-btn"
                  style={{ height: '36px', background: '#16a34a', color: '#fff', border: 'none' }}
                  disabled={updatingCustomer || !loyaltyPhone.trim()}
                  onClick={async () => {
                    if (!loyaltyPhone.trim()) return;
                    setUpdatingCustomer(true);
                    try {
                      const updated = await invoiceApi.updateCustomer(invoice.invoiceId, { phoneNumber: loyaltyPhone.trim(), fullName: loyaltyName.trim() || undefined });
                      setInvoice(updated);
                      setLoyaltyPhone(updated.customerPhone || loyaltyPhone.trim());
                      setLoyaltyName(updated.customerName || loyaltyName.trim());
                      toast("Đã lưu thông tin khách hàng cho hóa đơn.", "success");
                    } catch (err) {
                      toast(err instanceof Error ? err.message : "Cập nhật thất bại.", "error");
                    } finally {
                      setUpdatingCustomer(false);
                    }
                  }}
                >
                  {updatingCustomer ? "Đang xử lý..." : "Lưu thông tin khách"}
                </button>
              </div>
            )}
          </div>

          {Number(invoice.status) === 3 ? (
            <div className="state-card" style={{ background: '#fdeded', color: '#5f2120', border: '1px solid #f4c3c2', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              Hóa đơn này đã bị hủy.
            </div>
          ) : Number(invoice.paymentStatus) !== 3 ? (
            <div style={{ background: 'var(--soft)', padding: '16px', borderRadius: '12px', border: '1px solid var(--line)', display: 'flex', flexDirection: 'column', gap: '14px' }}>
              <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap', alignItems: 'center' }}>
                <select style={{ flex: '1 1 220px' }} value={paymentMethodId} onChange={(event) => {
                  const val = Number(event.target.value);
                  setPaymentMethodId(val ? val : "");
                }}>
                  <option value="">-- Chọn phương thức thanh toán --</option>
                  {methods.map((method) => <option key={method.paymentMethodId} value={method.paymentMethodId}>{method.name}</option>)}
                </select>
                <button className="primary-btn" onClick={() => setConfirmPayment(true)}>Thanh toán ngay</button>
                <button className="ghost-btn" onClick={openDiscountModal}>Áp dụng mã giảm giá</button>
                <button className="danger-btn" style={{ marginLeft: "auto" }} onClick={() => setCancelOpen(true)}>Hủy hóa đơn</button>
              </div>

              {(() => {
                const selected = methods.find(m => m.paymentMethodId === Number(paymentMethodId));
                const bankConfig = parseBankTransferConfig(selected);
                if (!bankConfig) return null;

                const amount = Math.max(0, Number(invoice.remainingAmount ?? ((invoice.grandTotalAmount || 0) - (invoice.paidAmount || 0))));
                const addInfo = `HD${invoice.invoiceId}`;
                const qrUrl = `${API_BASE_URL}/api/invoices/${invoice.invoiceId}/qr-code?amt=${amount}&_ts=${amount}_${invoice.discountAmount || 0}`;

                return (
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                    <PaymentQrCard
                      title="Quét mã QR để thanh toán tự động"
                      qrUrl={qrUrl}
                      bankName={bankConfig.bankName}
                      bankCode={bankConfig.bankCode}
                      accountNumber={bankConfig.accountNumber}
                      accountName={bankConfig.accountName}
                      amount={amount}
                      transferContent={addInfo}
                      note="Khách hàng mở ứng dụng ngân hàng quét mã PayOS. Khi tiền vào tài khoản, hóa đơn sẽ tự động hoàn tất và đóng thông báo."
                      onCopy={(message) => toast(message, "success")}
                    />
                  </div>
                );
              })()}
            </div>
          ) : (
            <div style={{ animation: 'fadeIn 0.4s ease-out' }}>
              <div
                style={{
                  background: 'linear-gradient(135deg, #ecfdf5 0%, #d1fae5 100%)',
                  padding: '32px 24px',
                  borderRadius: '16px',
                  border: '1px solid #10b981',
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: 'center',
                  justifyContent: 'center',
                  textAlign: 'center',
                  gap: '16px',
                  boxShadow: '0 10px 30px -10px rgba(16, 185, 129, 0.25)',
                  minHeight: '280px',
                  transition: 'all 0.5s cubic-bezier(0.16, 1, 0.3, 1)'
                }}
              >
                <div style={{
                  width: '68px',
                  height: '68px',
                  borderRadius: '50%',
                  background: '#10b981',
                  color: '#ffffff',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: '36px',
                  boxShadow: '0 0 0 12px rgba(16, 185, 129, 0.2)',
                  fontWeight: 'bold',
                  animation: 'popIn 0.5s cubic-bezier(0.34, 1.56, 0.64, 1)'
                }}>
                  ✓
                </div>
                <div>
                  <h3 style={{ margin: '0 0 8px', color: '#065f46', fontSize: '20px', fontWeight: 700 }}>
                    Hóa đơn này đã được thanh toán hoàn tất!
                  </h3>
                  <p style={{ margin: 0, color: '#047857', fontSize: '15px', lineHeight: '1.5', maxWidth: '450px' }}>
                    Hóa đơn đã được ghi nhận thanh toán thành công. Bạn có thể in hóa đơn chi tiết hoặc gửi link đánh giá trải nghiệm dịch vụ cho khách hàng ngay bên dưới.
                  </p>
                </div>
              </div>
              <ReviewInvitationPanel
                invoiceId={invoice.invoiceId}
                invitation={reviewInvitation || invoice.reviewInvitation || null}
                onCreated={(value) => {
                  setReviewInvitation(value);
                  setInvoice((current) => current ? { ...current, reviewInvitation: value } : current);
                }}
              />
            </div>
          )}
        </Modal>
      ) : null}
      {confirmPayment && invoice ? <ConfirmDialog title="Ghi nhận thanh toán" message={`Xác nhận thanh toán ${money(invoice.remainingAmount ?? Math.max(0, (invoice.grandTotalAmount || 0) - (invoice.paidAmount || 0)))} cho hóa đơn này?`} confirmLabel="Thanh toán" onCancel={() => setConfirmPayment(false)} onConfirm={async () => { setConfirmPayment(false); await pay(); }} /> : null}
      {discountOpen && invoice ? <Modal title="Áp dụng mã giảm giá / Voucher" onClose={() => { setDiscountOpen(false); setDiscountMessage(null); }} size="large">
        <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
          <div style={{ background: '#f8fafc', padding: '16px', borderRadius: '10px', border: '1px solid var(--line)' }}>
            <h4 style={{ margin: '0 0 10px', fontSize: '15px', color: '#334155' }}>⌨️ Nhập mã thủ công</h4>
            <div style={{ display: 'flex', gap: '10px' }}>
              <input
                value={discountCode}
                onChange={(event) => { setDiscountCode(event.target.value); setDiscountMessage(null); }}
                placeholder="Nhập mã giảm giá hoặc mã voucher..."
                autoFocus
                style={{ flex: 1, padding: '10px 14px', borderRadius: '8px', border: '1px solid #cbd5e1' }}
                onKeyDown={(e) => { if (e.key === 'Enter') applyDiscount(); }}
              />
              <button className="primary-btn" onClick={() => applyDiscount()}>Áp dụng</button>
            </div>
          </div>

          {discountMessage ? (
            <div style={{
              padding: '12px 16px',
              borderRadius: '8px',
              background: discountMessage.type === 'error' ? '#feecec' : '#e4f7ec',
              color: discountMessage.type === 'error' ? '#b33939' : '#187344',
              border: `1px solid ${discountMessage.type === 'error' ? '#fcd5d5' : '#c2ebd5'}`,
              fontSize: '14px',
              fontWeight: 600,
              display: 'flex',
              alignItems: 'center',
              gap: '8px',
              margin: '0'
            }}>
              <span style={{ fontSize: '18px' }}>{discountMessage.type === 'error' ? '⚠️' : '✅'}</span>
              <span>{discountMessage.text}</span>
            </div>
          ) : null}

          <div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
              <h4 style={{ margin: 0, fontSize: '16px', color: '#0f172a' }}>
                🎁 Danh sách Voucher đã đổi {invoice.customerName ? `của khách hàng ${invoice.customerName}` : ""}
              </h4>
              <span style={{ fontSize: '13px', color: 'var(--muted)' }}>{availableDiscounts.length} voucher khả dụng</span>
            </div>

            {!invoice.customerId && (
              <div style={{ padding: '10px 14px', background: '#fffbeb', color: '#92400e', border: '1px solid #fde68a', borderRadius: '8px', fontSize: '13px', marginBottom: '12px' }}>
                💡 Hóa đơn chưa gắn với Khách hàng. Vui lòng quay lại gắn SĐT/Tên khách hàng trước để xem danh sách Voucher đã đổi của khách!
              </div>
            )}

            {loadingDiscounts ? (
              <div style={{ padding: '30px', textAlign: 'center', color: 'var(--muted)' }}>Đang tải danh sách Voucher...</div>
            ) : availableDiscounts.length === 0 ? (
              <div style={{ padding: '30px', textAlign: 'center', color: 'var(--muted)', background: '#f9fafb', borderRadius: '10px', border: '1px dashed var(--line)' }}>
                {invoice.customerId ? "Khách hàng này hiện không có Voucher đổi điểm nào khả dụng." : "Vui lòng chọn khách hàng cho hóa đơn để hiển thị Voucher."}
              </div>
            ) : (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: '14px', maxHeight: '350px', overflowY: 'auto', padding: '4px' }}>
                {availableDiscounts.map((d) => {
                  const isPersonal = Boolean(d.customerId);
                  const isSelected = discountCode.trim().toUpperCase() === d.discountCode.toUpperCase();
                  return (
                    <div
                      key={d.discountId}
                      onClick={() => setDiscountCode(d.discountCode)}
                      style={{
                        border: isSelected ? '2px solid #2563eb' : '1px solid var(--line)',
                        borderRadius: '10px',
                        padding: '14px',
                        background: isSelected ? '#eff6ff' : '#ffffff',
                        cursor: 'pointer',
                        transition: 'all 0.15s ease',
                        display: 'flex',
                        flexDirection: 'column',
                        justifyContent: 'space-between',
                        boxShadow: isSelected ? '0 4px 12px rgba(37, 99, 235, 0.1)' : 'none'
                      }}
                    >
                      <div>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '8px' }}>
                          <span style={{
                            fontSize: '13px',
                            fontWeight: 'bold',
                            color: isPersonal ? '#7c3aed' : '#2563eb',
                            background: isPersonal ? '#f5f3ff' : '#eff6ff',
                            padding: '4px 10px',
                            borderRadius: '6px',
                            border: `1px solid ${isPersonal ? '#ddd6fe' : '#bfdbfe'}`
                          }}>
                            {d.discountCode}
                          </span>
                          <span style={{ fontSize: '11px', color: isPersonal ? '#6d28d9' : '#0369a1', fontWeight: 600 }}>
                            {isPersonal ? "🎁 Voucher cá nhân" : "🎟️ Khuyến mãi chung"}
                          </span>
                        </div>
                        <h5 style={{ margin: '6px 0', fontSize: '15px', color: '#1e293b' }}>{d.name}</h5>
                        <div style={{ fontSize: '14px', color: '#059669', fontWeight: 'bold', marginBottom: '6px' }}>
                          Giảm {d.discountType === 'PERCENTAGE' ? `${d.value}%${d.maxAmount ? ` (Tối đa ${money(d.maxAmount)})` : ''}` : money(d.value || 0)}
                        </div>
                        {d.minTimeSubtotal ? (
                          <div style={{ fontSize: '12px', color: 'var(--muted)' }}>Đơn giờ chơi từ: {money(d.minTimeSubtotal)}</div>
                        ) : null}
                        <div style={{ fontSize: '12px', color: 'var(--muted)', marginTop: '4px' }}>
                          HSD: {d.endsAtUtc ? dateTime(String(d.endsAtUtc)) : "Vô thời hạn"}
                        </div>
                      </div>
                      <button
                        type="button"
                        className={isSelected ? "primary-btn" : "ghost-btn"}
                        style={{ width: '100%', marginTop: '12px', padding: '8px' }}
                        onClick={(e) => {
                          e.stopPropagation();
                          applyDiscount(d.discountCode);
                        }}
                      >
                        {isSelected ? "⚡ Áp dụng ngay" : "Chọn & Áp dụng"}
                      </button>
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', borderTop: '1px solid var(--line)', paddingTop: '14px' }}>
            {invoice.discounts && invoice.discounts.length > 0 && Number(invoice.status) !== 3 && Number(invoice.paymentStatus) !== 3 ? (
              <button type="button" className="danger-btn" onClick={async () => { await removeDiscount(); setDiscountOpen(false); }}>🗑️ Gỡ khuyến mãi đang dùng</button>
            ) : null}
            <button type="button" className="ghost-btn" onClick={() => { setDiscountOpen(false); setDiscountMessage(null); }}>Đóng</button>
          </div>
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
                  Thêm món
                </button>
              </div>
            </div>

            <section className="invoice-editor-list" aria-label="Danh sách món trong hóa đơn">
              <header className="invoice-editor-toolbar">
                <div>
                  <div className="invoice-editor-title">
                    <h5>Danh sách món</h5>
                    <span className="badge blue">{editProducts.length} sản phẩm</span>
                  </div>
                  <p>{editProductTotalQuantity} món · {money(editProductTotalAmount)}</p>
                </div>
                <label className="invoice-editor-search">
                  <span>Tìm trong danh sách</span>
                  <input
                    type="search"
                    placeholder="Tên hoặc ID sản phẩm"
                    value={editProductSearch}
                    onChange={(event) => {
                      setEditProductSearch(event.target.value);
                      setEditProductPage(1);
                    }}
                  />
                </label>
              </header>

              <div className="invoice-editor-items">
                {visibleEditProducts.length ? visibleEditProducts.map((product) => (
                  <article className="invoice-editor-item" key={product.productId}>
                    <div className="invoice-editor-product">
                      <small>ID #{product.productId}</small>
                      <strong>{product.name}</strong>
                      <span>{money(product.unitPrice)} / món</span>
                    </div>
                    <div className="invoice-editor-quantity">
                      <button
                        className="ghost-btn compact"
                        type="button"
                        aria-label={`Giảm số lượng ${product.name}`}
                        onClick={() => updateEditQty(product.productId, product.quantity - 1)}
                      >
                        −
                      </button>
                      <input
                        type="number"
                        min={1}
                        aria-label={`Số lượng ${product.name}`}
                        value={product.quantity}
                        onChange={(event) => updateEditQty(product.productId, Math.max(1, Number(event.target.value)))}
                      />
                      <button
                        className="ghost-btn compact"
                        type="button"
                        aria-label={`Tăng số lượng ${product.name}`}
                        onClick={() => updateEditQty(product.productId, product.quantity + 1)}
                      >
                        +
                      </button>
                    </div>
                    <div className="invoice-editor-line-total">
                      <small>Thành tiền</small>
                      <strong>{money(product.quantity * product.unitPrice)}</strong>
                    </div>
                    <button
                      className="ghost-btn invoice-editor-remove"
                      type="button"
                      aria-label={`Xóa ${product.name}`}
                      onClick={() => removeProductFromEdit(product.productId)}
                    >
                      🗑️ Xóa
                    </button>
                  </article>
                )) : (
                  <div className="invoice-editor-empty">
                    {editProducts.length ? "Không tìm thấy món phù hợp." : "Chưa có món nào trong hóa đơn."}
                  </div>
                )}
              </div>

              {editProductTotalPages > 1 ? (
                <nav className="invoice-editor-pagination" aria-label="Phân trang danh sách món">
                  <button className="ghost-btn compact" type="button" disabled={editProductPage <= 1} onClick={() => setEditProductPage((page) => page - 1)}>Trước</button>
                  <span>Trang {editProductPage}/{editProductTotalPages}</span>
                  <button className="ghost-btn compact" type="button" disabled={editProductPage >= editProductTotalPages} onClick={() => setEditProductPage((page) => page + 1)}>Sau</button>
                </nav>
              ) : null}
            </section>

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

function ReviewInvitationPanel({ invoiceId, invitation, onCreated }: {
  invoiceId: number;
  invitation: ReviewInvitationLink | null;
  onCreated: (value: ReviewInvitationLink) => void;
}) {
  const toast = useToast();
  const [creating, setCreating] = useState(false);

  async function createInvitation() {
    setCreating(true);
    try {
      const value = await customerReviewsApi.createInvitationForInvoice(invoiceId);
      onCreated(value);
      toast("Đã tạo link đánh giá.", "success");
    } catch (err) {
      toast(err instanceof Error ? err.message : "Không tạo được link đánh giá.", "error");
    } finally {
      setCreating(false);
    }
  }

  if (!invitation) {
    return (
      <div className="card" style={{ marginTop: 16 }}>
        <h3 style={{ marginTop: 0 }}>Mời khách đánh giá</h3>
        <p className="muted-text">Tạo link đánh giá dùng một lần cho hóa đơn đã thanh toán này.</p>
        <button className="primary-btn" onClick={createInvitation} disabled={creating}>{creating ? "Đang tạo..." : "Tạo link đánh giá"}</button>
      </div>
    );
  }

  const qrUrl = `https://api.qrserver.com/v1/create-qr-code/?size=220x220&data=${encodeURIComponent(invitation.reviewUrl)}`;
  return (
    <div className="card" style={{ marginTop: 16, display: "grid", gridTemplateColumns: "180px 1fr", gap: 18, alignItems: "center" }}>
      <img src={qrUrl} alt="QR đánh giá" style={{ width: 180, height: 180, borderRadius: 8, border: "1px solid var(--line)", padding: 8, background: "white" }} />
      <div>
        <h3 style={{ marginTop: 0 }}>Mời khách đánh giá</h3>
        <p className="muted-text">Link đánh giá dùng một lần, hết hạn lúc {formatDateTimeLocal(invitation.expiresAtUtc)}.</p>
        <div style={{ wordBreak: "break-all", padding: 10, border: "1px solid var(--line)", borderRadius: 8, background: "var(--soft)", marginBottom: 12 }}>{invitation.reviewUrl}</div>
        <div className="actions">
          <button className="secondary-btn" onClick={() => navigator.clipboard.writeText(invitation.reviewUrl).then(() => toast("Đã sao chép link đánh giá.", "success"))}>Copy link</button>
          <a className="primary-btn" href={invitation.reviewUrl} target="_blank">Mở form đánh giá</a>
        </div>
      </div>
    </div>
  );
}
