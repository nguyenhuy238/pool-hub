import type { PaymentMethod } from "@/types";

export type BankTransferConfig = {
  paymentMethodCode?: string;
  paymentMethodName?: string;
  bankName?: string;
  bankCode?: string;
  accountNumber?: string;
  accountName?: string;
  qrImageUrl?: string;
};

export function isBankTransferMethod(method?: PaymentMethod | null) {
  if (!method || method.isActive === false) return false;
  const code = (method.code || "").toUpperCase();
  const name = (method.name || "").toLowerCase();
  return code !== "DEPOSIT" && (
    ["BANK", "BANK_TRANSFER", "TRANSFER", "VIETQR"].includes(code) ||
    name.includes("chuyển khoản") ||
    name.includes("bank") ||
    name.includes("qr") ||
    name.includes("chuyen khoan")
  );
}

export function parseBankTransferConfig(method?: PaymentMethod | null): BankTransferConfig | null {
  if (!isBankTransferMethod(method)) return null;
  const config: BankTransferConfig = {
    paymentMethodCode: method?.code,
    paymentMethodName: method?.name
  };

  const description = method?.description?.trim();
  if (description?.startsWith("{")) {
    try {
      const parsed = JSON.parse(description) as Record<string, string | boolean | undefined>;
      config.bankName = typeof parsed.bankName === "string" ? parsed.bankName : undefined;
      config.bankCode = typeof parsed.bankCode === "string" ? parsed.bankCode : undefined;
      config.accountNumber = typeof parsed.accountNo === "string"
        ? parsed.accountNo
        : typeof parsed.accountNumber === "string" ? parsed.accountNumber : undefined;
      config.accountName = typeof parsed.accountName === "string" ? parsed.accountName : undefined;
      config.qrImageUrl = typeof parsed.qrImageUrl === "string" ? parsed.qrImageUrl : undefined;
    } catch {
      return null;
    }
  }

  return config;
}

export function buildVietQrUrl(config: BankTransferConfig, amount: number, transferContent: string) {
  if (!config.bankCode || !config.accountNumber || !config.accountName) return "";
  const roundedAmount = Math.max(0, Math.round(amount || 0));
  return `https://img.vietqr.io/image/${config.bankCode}-${config.accountNumber}-compact2.png?amount=${roundedAmount}&addInfo=${encodeURIComponent(transferContent)}&accountName=${encodeURIComponent(config.accountName)}`;
}
