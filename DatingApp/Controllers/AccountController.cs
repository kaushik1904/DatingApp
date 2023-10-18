using DatingApp.Data;
using DatingApp.DTOs;
using DatingApp.Entities;
using DatingApp.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace DatingApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        public AccountController(DataContext context,ITokenService tokenService)
        {
            _tokenService = tokenService;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDTO>> Register([FromBody] RegisterDTO registerDTO)
        {
            if(await UserExists(registerDTO.Username))
            {
                return BadRequest("Username is already taken");
            }
            using var hmac = new HMACSHA512();
            var user = new AppUser{
                UserName = registerDTO.Username,
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDTO.Password)),
                PasswordSalt = hmac.Key
            };

            _context.Add(user);
            await _context.SaveChangesAsync();
            return new UserDTO
            {
                Username = registerDTO.Username,
                Token = _tokenService.CreateToken(user)
            };
        }

        private async Task<bool> UserExists(string userName)
        {
            return await _context.Users.AnyAsync(u => u.UserName.ToLower() == userName.ToLower());    
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDTO)
        {
            var user = await _context.Users.SingleOrDefaultAsync(x=>x.UserName==loginDTO.Username);

            if(user == null) { return Unauthorized("Invalid Username"); }

            using var hmac = new HMACSHA512(user.PasswordSalt);

            var computeHashPassword = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDTO.Password));

            for(int i = 0;i < computeHashPassword.Length; i++)
            {
                if (computeHashPassword[i] != user.PasswordHash[i]) return Unauthorized("Invalid Password");
            }

            return new UserDTO
            {
                Username = loginDTO.Username,
                Token = _tokenService.CreateToken(user)
            };
        }

    }
}
