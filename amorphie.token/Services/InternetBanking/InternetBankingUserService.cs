using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using amorphie.token.core.Models.InternetBanking;
using amorphie.token.data;
using Microsoft.EntityFrameworkCore;

namespace amorphie.token.Services.InternetBanking
{
    public class InternetBankingUserService : IInternetBankingUserService
    {
        private readonly IbDatabaseContext _ibDatabaseContext;
        public InternetBankingUserService(IbDatabaseContext ibDatabaseContext)
        {
            _ibDatabaseContext = ibDatabaseContext;
        }

        public async Task<ServiceResponse<IBPassword>> GetPassword(Guid userId)
        {
            ServiceResponse<IBPassword> response = new();
            try
            {
                var password = await _ibDatabaseContext.Password.Where(p => p.UserId == userId).OrderByDescending(p => p.CreatedAt).FirstOrDefaultAsync();
                if (password == null)
                {
                    response.StatusCode = 404;
                    response.Detail = "User Not Found";
                }
                else
                {
                    response.StatusCode = 200;
                    response.Detail = "";
                    response.Response = password;
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.Detail = ex.ToString();
            }

            return response;
        }

        public async Task<ServiceResponse<IBUser>> GetUser(string username)
        {
            ServiceResponse<IBUser> response = new();
            try
            {
                var user = await _ibDatabaseContext.User.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == username);
                if (user == null)
                {
                    response.StatusCode = 404;
                    response.Detail = "User Not Found";
                }
                else
                {
                    response.StatusCode = 200;
                    response.Detail = "";
                    response.Response = user;
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.Detail = ex.ToString();
            }

            return response;
        }

        public PasswordVerificationResult VerifyPassword(string hashedPassword, string providedPassword, string salt)
        {
            PasswordHasher hasher = new();
            return hasher.VerifyHashedPassword(hashedPassword, providedPassword, salt);
        }
    }
}