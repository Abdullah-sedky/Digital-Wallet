using Microsoft.AspNetCore.Mvc;
using DigitalWallet.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;
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

        [HttpPost("Register")]
        public async Task<IActionResult> register([FromForm] string email, [FromForm] string password)
        {
            var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            var passwordRegex = @"^(?=.*[A-Z])(?=.*\d).{8,}$";

            if (!Regex.IsMatch(email, emailRegex))
                return BadRequest("Invalid email format");

            if (!Regex.IsMatch(password, passwordRegex))
                return BadRequest("Password must be at least 8 characters, include a number and an uppercase letter");

            var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existing != null)
                return BadRequest("Email already registered");
            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok("User Registered");
        }


        [HttpPost("Login")]
        public async Task<IActionResult> login([FromForm] string email, [FromForm] string password)
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

