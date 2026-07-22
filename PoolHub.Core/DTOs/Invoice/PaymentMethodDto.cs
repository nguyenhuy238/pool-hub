namespace PoolHub.Core.DTOs.Invoice;

public class PaymentMethodDto
{
    public long PaymentMethodId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
