using HRMS.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.Application.Services
{
    public class SecureTokenService : IPasswordResetService
    {
        private readonly IIdentityService _identityService;

        public SecureTokenService(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<string> GenerateResetLinkAsync(string userId, string baseUrl)
        {
            string token = GenerateSecureToken();

            await _identityService.UpdateResetTokenAsync(
                userId,
                Hash(token),
                DateTime.Now.AddMinutes(15)
            );

            return $"{baseUrl}/Account/ResetPassword?token={Uri.EscapeDataString(token)}";
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            if (!await _identityService.IsTokenValidAsync(Hash(token)))
                return false;

            var userId = await _identityService.GetUserIdByTokenAsync(Hash(token));

            await _identityService.ResetPasswordAsync(userId, newPassword);
            await _identityService.InvalidateTokenAsync(userId);

            return true;
        }

        // helper methods
        private string Hash(string input)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(bytes);
            }
        }

        public string GenerateSecureToken()
        {
            byte[] data = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(data);
            }
            return Convert.ToBase64String(data);
        }
    }
}
