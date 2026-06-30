namespace PoolHub.Core.DTOs.Session;

public class ReopenSessionRequest
{
    public string Reason { get; set; } = string.Empty;
    public bool ReopenLastTable { get; set; } = true;
}
