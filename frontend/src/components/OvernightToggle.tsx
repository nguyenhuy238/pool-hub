import "./OvernightToggle.css";

export function OvernightToggle({ checked, onChange, className = "" }: {
  checked: boolean;
  onChange: (checked: boolean) => void;
  className?: string;
}) {
  return (
    <div className={`overnight-toggle-card ${className}`}>
      <strong className="overnight-toggle-title">Đặt qua đêm</strong>
      <button
        type="button"
        role="switch"
        aria-checked={checked}
        aria-label="Đặt qua đêm"
        className={`overnight-switch${checked ? " is-on" : ""}`}
        onClick={() => onChange(!checked)}
      >
        <span className="overnight-switch-knob" />
      </button>
      <span className="overnight-toggle-help">*Bật để chọn giờ sang ngày hôm sau.</span>
    </div>
  );
}
