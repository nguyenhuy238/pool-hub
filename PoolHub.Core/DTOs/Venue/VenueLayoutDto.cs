namespace PoolHub.Core.DTOs.Venue;

/// <summary>
/// Thông tin một bàn chơi trong sơ đồ venue, kèm trạng thái hoạt động hiện tại.
/// </summary>
public class VenueTableLayoutItem
{
    /// <summary>ID của bàn.</summary>
    public long TableId { get; set; }

    /// <summary>Mã bàn (ví dụ: T01, B02).</summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>Tên bàn.</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>ID loại bàn.</summary>
    public long TableTypeId { get; set; }

    /// <summary>Tên loại bàn.</summary>
    public string TableTypeName { get; set; } = string.Empty;

    /// <summary>Sức chứa tối đa.</summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Trạng thái vận hành lưu trong DB:
    /// 1 = Available, 2 = Occupied, 3 = Reserved, 4 = Maintenance, 5 = Inactive.
    /// </summary>
    public int OperationalStatus { get; set; }

    /// <summary>Nhãn trạng thái dạng text.</summary>
    public string OperationalStatusLabel => OperationalStatus switch
    {
        1 => "Sẵn sàng",
        2 => "Đang có khách",
        3 => "Đã đặt trước",
        4 => "Bảo trì",
        5 => "Ngừng hoạt động",
        _ => "Không xác định"
    };

    /// <summary>Tọa độ X trên sơ đồ (dùng cho frontend drag-drop layout).</summary>
    public decimal PositionX { get; set; }

    /// <summary>Tọa độ Y trên sơ đồ.</summary>
    public decimal PositionY { get; set; }

    /// <summary>Bàn có đang được kích hoạt không.</summary>
    public bool IsActive { get; set; }

    /// <summary>ID session đang chạy trên bàn này (null nếu bàn trống).</summary>
    public long? ActiveSessionId { get; set; }

    /// <summary>ID booking gần nhất trên bàn này (null nếu không có booking sắp tới).</summary>
    public long? NextBookingId { get; set; }

    /// <summary>Mã booking gần nhất trên bàn này.</summary>
    public string? NextBookingCode { get; set; }

    /// <summary>Thời điểm bắt đầu booking gần nhất.</summary>
    public DateTime? NextBookingStartTimeUtc { get; set; }
}

/// <summary>
/// Thông tin một khu vực (Zone) kèm danh sách bàn bên trong.
/// </summary>
public class VenueZoneLayoutItem
{
    /// <summary>ID của khu vực.</summary>
    public long ZoneId { get; set; }

    /// <summary>Tên khu vực.</summary>
    public string ZoneName { get; set; } = string.Empty;

    /// <summary>Mô tả khu vực.</summary>
    public string? Description { get; set; }

    /// <summary>Thứ tự hiển thị.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Danh sách bàn trong khu vực này.</summary>
    public List<VenueTableLayoutItem> Tables { get; set; } = [];
}

/// <summary>
/// Thông tin một tầng (Floor) kèm danh sách khu vực bên trong.
/// </summary>
public class VenueFloorLayoutItem
{
    /// <summary>ID của tầng.</summary>
    public long FloorId { get; set; }

    /// <summary>Tên tầng.</summary>
    public string FloorName { get; set; } = string.Empty;

    /// <summary>Mô tả tầng.</summary>
    public string? Description { get; set; }

    /// <summary>Thứ tự hiển thị.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Danh sách khu vực trên tầng này.</summary>
    public List<VenueZoneLayoutItem> Zones { get; set; } = [];
}

/// <summary>
/// Response tổng hợp sơ đồ toàn bộ venue: Floor → Zone → Table + trạng thái realtime.
/// </summary>
public class VenueLayoutResponse
{
    /// <summary>Danh sách tầng, mỗi tầng chứa các khu vực và bàn.</summary>
    public List<VenueFloorLayoutItem> Floors { get; set; } = [];

    /// <summary>Tổng số bàn.</summary>
    public int TotalTables { get; set; }

    /// <summary>Số bàn đang trống (Available).</summary>
    public int AvailableTables { get; set; }

    /// <summary>Số bàn đang có người chơi (Occupied).</summary>
    public int OccupiedTables { get; set; }

    /// <summary>Số bàn đã đặt trước.</summary>
    public int ReservedTables { get; set; }

    /// <summary>Số bàn đang bảo trì.</summary>
    public int MaintenanceTables { get; set; }

    /// <summary>Số bàn ngừng hoạt động.</summary>
    public int InactiveTables { get; set; }

    /// <summary>Thời điểm dữ liệu được lấy (UTC).</summary>
    public DateTime FetchedAtUtc { get; set; }
}
