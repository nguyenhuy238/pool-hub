using System;
using System.Collections.Generic;
using PoolHub.Core.DTOs.BookingDepositRefund;

namespace PoolHub.Core.DTOs.Session;

public class SessionDetailDto
{
    public long SessionId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public long? CustomerId { get; set; }
    public long? BookingId { get; set; }
    public int Status { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public long OpenedByUserId { get; set; }
    public long? ClosedByUserId { get; set; }
    public string? Note { get; set; }
    public DepositRefundSummaryDto? DepositRefundSummary { get; set; }
    public List<SessionTableAssignmentDto> Assignments { get; set; } = [];
}
