using System.Threading.Tasks;

namespace HotstarApi.Services;

public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string otpCode, string purpose);
}
