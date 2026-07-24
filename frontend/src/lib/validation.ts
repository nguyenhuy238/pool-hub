export const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
export const strongPasswordPattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$/;
export const vietnamPhonePattern = /^(0|\+84)(3|5|7|8|9)[0-9]{8}$/;

export function validateEmail(email: string) {
  if (!email.trim()) return "Vui lòng nhập email.";
  if (!emailPattern.test(email.trim())) return "Email không đúng định dạng.";
  return "";
}

export function validateOptionalEmail(email: string) {
  if (!email.trim()) return "";
  if (!emailPattern.test(email.trim())) return "Email không đúng định dạng.";
  return "";
}

export function validateVietnamPhone(phoneNumber: string) {
  const value = phoneNumber.trim().replace(/\s/g, "");
  if (!value) return "Vui lòng nhập số điện thoại.";
  if (!vietnamPhonePattern.test(value)) return "Số điện thoại không hợp lệ. Ví dụ: 0987654321 hoặc +84987654321.";
  return "";
}

export function validatePassword(password: string) {
  if (!password) return "Vui lòng nhập mật khẩu.";
  if (!strongPasswordPattern.test(password)) {
    return "Mật khẩu phải có ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.";
  }
  return "";
}
