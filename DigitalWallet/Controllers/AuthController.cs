using Microsoft.AspNetCore.Mvc;
using DigitalWallet.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
namespace DigitalWallet.Controllers
{
    [Route("api/[Controller]")]
    [ApiController]

    public class AuthController : ControllerBase
    {
        private readonly WalletDbContext _context;
        private readonly JWTcreate _jwtcreate;
        public AuthController(WalletDbContext context, JWTcreate jwtcreate)
        {
            _context = context;
            _jwtcreate = jwtcreate;
        }

        [HttpGet("Register")]
        public async Task<IActionResult> register(string email, string password)
        {
            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok("User Registered");
        }


        [HttpGet("Login")]
        public async Task<IActionResult> login(string email, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return BadRequest("Empty Password field");
            }
            else if (string.IsNullOrEmpty(email)) {
                return BadRequest("Empty email field");
            }
            var user= await _context.Users.FirstOrDefaultAsync(user=>user.Email == email);
            if (user == null) {
                return Unauthorized("Invalid email or password");
            }
            bool validpassword= BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if(!validpassword)
            {
                return Unauthorized("Wrong Password");
            }
            var token = _jwtcreate.createToken(user);
            return Ok(new { token });
        }
            
        }

    }

