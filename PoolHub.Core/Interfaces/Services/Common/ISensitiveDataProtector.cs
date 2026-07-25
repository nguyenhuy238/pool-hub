namespace PoolHub.Core.Interfaces.Services;

public interface ISensitiveDataProtector
{
    string Protect(string plainText);
    string Unprotect(string protectedText);
}
