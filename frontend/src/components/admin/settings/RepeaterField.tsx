"use client";

export function RepeaterField<T>({ title, items, onAdd, onRemove, children }: {
  title: string;
  items: T[];
  onAdd: () => void;
  onRemove: (index: number) => void;
  children: (item: T, index: number) => React.ReactNode;
}) {
  return (
    <div className="settings-repeater">
      <div className="repeater-head"><h2>{title}</h2><button type="button" className="primary-btn" onClick={onAdd}>Thêm item</button></div>
      {items.map((item, index) => (
        <div className="repeater-item" key={index}>
          <div className="repeater-item-head"><strong>Item {index + 1}</strong><button type="button" className="danger-btn" onClick={() => onRemove(index)}>Xóa</button></div>
          {children(item, index)}
        </div>
      ))}
    </div>
  );
}
