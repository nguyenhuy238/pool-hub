"use client";

import { KeyboardEvent } from "react";
import { Star } from "lucide-react";

type StarRatingInputProps = {
  value: number;
  onChange: (value: number) => void;
  disabled?: boolean;
  id?: string;
};

export function StarRatingInput({ value, onChange, disabled, id = "rating" }: StarRatingInputProps) {
  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (disabled) return;
    if (event.key === "ArrowRight" || event.key === "ArrowUp") {
      event.preventDefault();
      onChange(Math.min(5, value + 1));
    }
    if (event.key === "ArrowLeft" || event.key === "ArrowDown") {
      event.preventDefault();
      onChange(Math.max(1, value - 1));
    }
  }

  return (
    <div
      id={id}
      role="radiogroup"
      aria-label="Chọn số sao đánh giá"
      onKeyDown={handleKeyDown}
      style={{ display: "flex", gap: 6, alignItems: "center" }}
    >
      {[1, 2, 3, 4, 5].map((star) => {
        const selected = star <= value;
        return (
          <button
            key={star}
            type="button"
            role="radio"
            aria-checked={value === star}
            aria-label={`${star} sao`}
            disabled={disabled}
            onClick={() => onChange(star)}
            style={{
              width: 38,
              height: 38,
              borderRadius: 8,
              border: "1px solid var(--line)",
              background: selected ? "#fff7d6" : "white",
              color: selected ? "#b7791f" : "var(--muted)",
              display: "grid",
              placeItems: "center",
              cursor: disabled ? "not-allowed" : "pointer"
            }}
          >
            <Star size={20} fill={selected ? "currentColor" : "none"} aria-hidden="true" />
          </button>
        );
      })}
    </div>
  );
}
