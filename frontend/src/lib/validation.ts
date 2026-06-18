export const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
export const strongPasswordPattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$/;

export function validateEmail(email: string) {
  if (!email.trim()) return "Vui lòng nhập email.";
  if (!emailPattern.test(email.trim())) return "Email không đúng định dạng.";
  return "";
}

export function validatePassword(password: string) {
  if (!password) return "Vui lòng nhập mật khẩu.";
  if (!strongPasswordPattern.test(password)) {
    return "Mật khẩu phải có ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.";
  }
  return "";
}
