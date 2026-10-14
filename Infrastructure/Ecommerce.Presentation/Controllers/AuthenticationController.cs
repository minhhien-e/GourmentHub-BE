using Ecommerce.Abstraction.Services;
using Ecommerce.Shared.DTOs.IdentityDto_s;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController(IServiceManger manager) : ControllerBase
    {
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login([FromBody] LoginDto dto)
        {
            var user = await manager.AuthenticationService.LoginAsync(dto);
            return Ok(user);
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register([FromBody] RegisterDto dto)
        {
            var user = await manager.AuthenticationService.RegisterAsync(dto);
            return Ok(user);
        }

        [Authorize]
        [HttpGet("check-email")]
        public async Task<ActionResult<bool>> CheckEmail([FromQuery] string email)
        {
            var exists = await manager.AuthenticationService.CheckEmailAsync(email);
            return Ok(exists);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await manager.AuthenticationService.GetCurrentUserAsync(email);
            return Ok(user);
        }

        [Authorize]
        [HttpGet("address")]
        public async Task<ActionResult<AddressDto>> GetCurrentUserAddress()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var address = await manager.AuthenticationService.GetCurrentUserAddressAsync(email);
            return Ok(address);
        }

        [Authorize]
        [HttpPut("address")]
        public async Task<ActionResult<AddressDto>> UpdateCurrentUserAddress([FromBody] AddressDto dto)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var updatedAddress = await manager.AuthenticationService.UpdateCurrentUserAddressAsync(email, dto);
            return Ok(updatedAddress);
        }

        // ─── Google OAuth ───────────────────────────────────────────────

        /// <summary>
        /// Initiates Google OAuth flow.
        /// GET: api/authentication/google-login
        /// </summary>
        [HttpGet("google-login")]
        [AllowAnonymous]
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action(nameof(GoogleCallback), "Authentication", null, Request.Scheme);
            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl,
                Items = { { "scheme", GoogleDefaults.AuthenticationScheme } }
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Handles Google OAuth callback.
        /// GET: api/authentication/google-callback
        /// </summary>
        [HttpGet("google-callback")]
        [AllowAnonymous]
        public async Task<ActionResult<UserDto>> GoogleCallback()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal == null)
                return BadRequest(new { error = "Google authentication failed." });

            var claims = result.Principal.Claims.ToList();
            var email    = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name     = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var googleId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(email))
                return BadRequest(new { error = "Email not provided by Google." });
            if (string.IsNullOrEmpty(googleId))
                return BadRequest(new { error = "Google ID not provided." });

            var user = await manager.AuthenticationService.GetOrCreateGoogleUserAsync(email, name, googleId);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(user);
        }
    }
}
