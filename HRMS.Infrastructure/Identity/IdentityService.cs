using HRMS.Application.Interfaces;
using HRMS.Infrastructure.Data;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.Infrastructure.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public IdentityService()
        {
            var context = new ApplicationDbContext();
            _userManager = new UserManager<ApplicationUser>(
                new UserStore<ApplicationUser>(context));
        }

        public async Task<bool> UpdateResetTokenAsync(string userId, string token, DateTime expiry)
        {
            var user = await _userManager.FindByNameAsync(userId);

            if (user == null) return false;

            user.ResetToken = token;
            user.TokenExpiry = expiry;

            await _userManager.UpdateAsync(user);
            return true;
        }

        public async Task<bool> IsTokenValidAsync(string token)
        {
            var user = _userManager.Users
                .FirstOrDefault(x => x.ResetToken == token && x.TokenExpiry > DateTime.Now);

            return user != null;
        }

        public async Task<string> GetUserIdByTokenAsync(string token)
        {
            return _userManager.Users
                .Where(x => x.ResetToken == token)
                .Select(x => x.Id)
                .FirstOrDefault();
        }

        public async Task<bool> ResetPasswordAsync(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);

            user.PasswordHash = _userManager.PasswordHasher.HashPassword(newPassword);

            await _userManager.UpdateAsync(user);
            return true;
        }

        public async Task InvalidateTokenAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            user.ResetToken = null;
            user.TokenExpiry = null;

            await _userManager.UpdateAsync(user);
        }
    }
}
