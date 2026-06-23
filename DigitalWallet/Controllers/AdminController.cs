using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigitalWallet.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DigitalWallet.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly WalletDbContext _context;
        private readonly JWTcreate _jwtcreate;
        public AdminController(WalletDbContext context, JWTcreate jwtcreate)
        {
            _context = context;
            _jwtcreate = jwtcreate;
        }

        [HttpGet("users")]
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> getAllUsers()
        {
            var users = await _context.Users.Include(u => u.Wallet)
                .Select(u => new { u.Id, u.Role, u.Email, u.Wallet })
                .ToListAsync();
            if (!users.Any())
            {
                return BadRequest("No users found");
            }
            return Ok(users);
        }

       
        [HttpPut("freeze/{email}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> freezeWallet(string email)
        {
            var UserWallet=await _context.Users.Include(u=>u.Wallet).FirstOrDefaultAsync(u=>u.Email == email);
            if (UserWallet == null)
            {
                return BadRequest("User wallet not found");
            }
            else if (UserWallet.Wallet.IsFrozen)
            {
                return BadRequest("Wallet already frozen");
            }
            UserWallet.Wallet.IsFrozen = true;
            return Ok(UserWallet);
        }

        [HttpPut("unfreeze/{email}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> unfreezeWallet(string email)
        {
            var UserWallet = await _context.Users.Include(u => u.Wallet).FirstOrDefaultAsync(u => u.Email == email);
            if (UserWallet == null)
            {
                return BadRequest("User wallet not found");
            }
            else if (!UserWallet.Wallet.IsFrozen)
            {
                return BadRequest("Wallet already active");
            }
            UserWallet.Wallet.IsFrozen = false;
            return Ok(UserWallet);
        }


    }
}
