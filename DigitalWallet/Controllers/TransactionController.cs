using Microsoft.AspNetCore.Mvc;
using DigitalWallet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DigitalWallet.Controllers
{
    [Authorize(Roles="Customer")]
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly WalletDbContext _context;
        public TransactionController(WalletDbContext context)
        {
            _context = context;
        }
        [HttpGet("history")]
        public async Task<IActionResult> paginate (int page=1, int pageSize = 10)
        {
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var user= await _context.Users.Include(u=>u.Wallet).FirstOrDefaultAsync(u=>u.Email== email);
            if (user == null)
            {
                return NotFound();
            }
            var transactions=await _context.Transactions
                .Where(t=>t.SenderWalletId==user.Wallet.Id || t.ReceiverWalletId==user.Wallet.Id)
                .OrderByDescending( t=>t.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return Ok(transactions);
        }
        
    }
}
