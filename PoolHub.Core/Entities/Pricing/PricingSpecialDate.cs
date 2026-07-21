namespace PoolHub.Core.Entities;

public class PricingSpecialDate : BaseEntity
{
    public long PricingSpecialDateId { get; set; }
    public DateTime Date { get; set; }
    public int DayType { get; set; } // 3 = Ngày lễ, 4 = Ngày đặc biệt
    public string Description { get; set; } = string.Empty;
}
