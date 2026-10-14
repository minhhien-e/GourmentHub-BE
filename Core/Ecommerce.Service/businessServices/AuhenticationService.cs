using AutoMapper;
using Ecommerce.Abstraction.Services;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Domain.Models.Identity;
using Ecommerce.Shared.DTOs.IdentityDto_s;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Ecommerce.Service.businessServices
{
    public class AuhenticationService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        IMapper mapper) : IAuthenticationService
    {
        public async Task<UserDto> LoginAsync(LoginDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email)
                ?? throw new UserNotFound(dto.Email);

            if (!await userManager.CheckPasswordAsync(user, dto.Password))
                throw new UnAuthorizedException();

            return new UserDto
            {
                DisplayName = user.DisplayName,
                Email       = user.Email,
                Token       = await CreateTokenAsync(user)
            };
        }

        public async Task<UserDto> RegisterAsync(RegisterDto dto)
        {
            var user = new ApplicationUser
            {
                DisplayName = dto.DisplayName,
                Email       = dto.Email,
                PhoneNumber = dto.phoneNumber,
                UserName    = dto.Username
            };

            var result = await userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                throw new BadRequestException(errors);
            }

            return new UserDto
            {
                DisplayName = user.DisplayName,
                Email       = user.Email,
                Token       = await CreateTokenAsync(user)
            };
        }

        public async Task<AddressDto> UpdateCurrentUserAddressAsync(string email, AddressDto dto)
        {
            var user = await userManager.Users
                .Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.Email == email)
                ?? throw new UserNotFound(email);

            if (user.Address is not null)
            {
                user.Address.Street    = dto.Street;
                user.Address.City      = dto.City;
                user.Address.Country   = dto.Country;
                user.Address.FirstName = dto.FirstName;
                user.Address.LastName  = dto.LastName;
            }
            else
            {
                user.Address = mapper.Map<AddressDto, Address>(dto);
            }

            await userManager.UpdateAsync(user);
            return mapper.Map<Address, AddressDto>(user.Address);
        }

        public async Task<bool> CheckEmailAsync(string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            return user is not null;
        }

        public async Task<AddressDto> GetCurrentUserAddressAsync(string email)
        {
            var user = await userManager.Users
                .Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.Email == email)
                ?? throw new UserNotFound(email);

            if (user.Address is null)
                throw new AddressNotFound(user.DisplayName);

            return mapper.Map<Address, AddressDto>(user.Address);
        }

        public async Task<UserDto> GetCurrentUserAsync(string email)
        {
            var user = await userManager.FindByEmailAsync(email)
                ?? throw new UserNotFound(email);

            return new UserDto
            {
                DisplayName = user.DisplayName,
                Email       = user.Email,
                Token       = await CreateTokenAsync(user)
            };
        }

        /// <summary>
        /// Finds or creates a user account linked to a Google OAuth identity,
        /// then returns a JWT token for that user.
        /// </summary>
        public async Task<UserDto> GetOrCreateGoogleUserAsync(string email, string displayName, string googleId)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user is null)
            {
                user = new ApplicationUser
                {
                    Email       = email,
                    UserName    = $"{email.Split('@')[0]}_{Guid.NewGuid().ToString()[..4]}",
                    DisplayName = displayName ?? email.Split('@')[0],
                    PhoneNumber = string.Empty
                };

                var result = await userManager.CreateAsync(user);
                if (!result.Succeeded)
                    throw new FailedToCreateGoogleAccount("Failed to create user from Google account.");

                await userManager.AddLoginAsync(user, new UserLoginInfo("Google", googleId, "Google"));
            }

            return new UserDto
            {
                Email       = user.Email,
                DisplayName = user.DisplayName,
                Token       = await CreateTokenAsync(user)
            };
        }

        // ─── Token Factory ────────────────────────────────────────────────

        private async Task<string> CreateTokenAsync(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Email,          user.Email),
                new(ClaimTypes.Name,           user.UserName),
                new(ClaimTypes.NameIdentifier, user.Id)
            };

            var roles = await userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var securityKey = configuration["JWTOptions:SecurityKey"]
                ?? throw new InvalidOperationException("JWT SecurityKey is not configured.");

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey)),
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                issuer:             configuration["JWTOptions:Issuer"],
                audience:           configuration["JWTOptions:Audience"],
                claims:             claims,
                expires:            DateTime.UtcNow.AddDays(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
