using System;
using System.Collections.Generic;
using System.Text;
using Travella.API.Models;
using Travella.Application.DTOs;

namespace Travella.Application.Services
{
    public interface IAuthService
    {
        Task<AuthUserDto> RegisterTravelerAsync(RegisterTravelerRequest request);
        Task<AuthUserDto?> LoginAsync(LoginRequest request);
        Task ResetPasswordAsync(string email, string newPassword);
    }
}
