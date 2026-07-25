using Microsoft.AspNetCore.DataProtection;
using PoolHub.Core.Interfaces.Services;

namespace PoolHub.Services.Common;

public class SensitiveDataProtector(IDataProtectionProvider provider) : ISensitiveDataProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("PoolHub.BookingDepositRefund.BankData.v1");

    public string Protect(string plainText) => _protector.Protect(plainText);

    public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
}
