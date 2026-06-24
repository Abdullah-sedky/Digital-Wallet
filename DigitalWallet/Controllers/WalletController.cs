using Microsoft.AspNetCore.Mvc;
using DigitalWallet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Collections;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DigitalWallet.Controllers
{
    [Authorize(Roles="Customer")]
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly WalletDbContext _context;
        private readonly RabbitMqPublisher _publisher;
        public WalletController(WalletDbContext context, RabbitMqPublisher publisher)
        {
            _publisher = publisher;
            _context = context;
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
                var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var myAccount = await _context.Users.Include(u => u.Wallet).FirstOrDefaultAsync(u => u.Email == email);
                if (myAccount == null)
                {
                    return BadRequest("User not found");
                }
                return Ok(myAccount.Wallet.Balance);
            }
        

        [HttpPut("deposit")]
        public async Task<IActionResult> deposit( decimal amount, Guid idempotencyKey)
            
        {
            try
            {
                var existing = await _context.Transactions.FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey);
                if (existing != null)
                {
                    return BadRequest("Request already processed");
                }
                var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var user = await _context.Users.Include(u => u.Wallet).FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                    return BadRequest("User not found");

                var wallet = user.Wallet;
                if (wallet == null)
                {
                    return BadRequest("Wallet not found");
                }
                if (wallet.IsFrozen)
                {
                    return BadRequest("Can't deposit. Wallet frozen.");
                }
                wallet.Balance += amount;
                await _context.Transactions.AddAsync(new Transaction
                {
                    SenderWallet = wallet,
                    SenderWalletId = wallet.Id,
                    Amount = amount,
                    Timestamp = DateTime.UtcNow,
                    Status = "Deposit",
                    IdempotencyKey = idempotencyKey
                });
                await _context.SaveChangesAsync();
                await _publisher.PublishTransactionEvent($"Deposit of {amount} completed for wallet {wallet.Id}");
                return Ok();
            }
            catch(DbUpdateConcurrencyException)
            {
                return Conflict("Concurrent deposit. Try again.");
            }
        }

        [HttpPut("withdraw")]
        public async Task<IActionResult> withdraw(decimal amount, Guid idempotencyKey)
        {
            try
            {
                var existing = await _context.Transactions.FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey);
                if (existing != null)
                {
                    return BadRequest("Request already processed");
                }
                var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var user = await _context.Users.Include(u => u.Wallet).FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                    return BadRequest("User not found");

                var wallet = user.Wallet;
                if (wallet == null)
                {
                    return BadRequest("Wallet not found");
                }
                else if (wallet.Balance < amount)
                {
                    return BadRequest("Insufficient funds");
                }
                else if (wallet.IsFrozen)
                {
                    return BadRequest("Can't withdraw. Wallet frozen.");
                }
                wallet.Balance -= amount;
                await _context.Transactions.AddAsync(new Transaction
                {
                    SenderWallet = wallet,
                    SenderWalletId = wallet.Id,
                    Amount = amount,
                    Timestamp = DateTime.UtcNow,
                    Status = "Withdrawal",
                    IdempotencyKey= idempotencyKey
                });
                await _context.SaveChangesAsync();
                await _publisher.PublishTransactionEvent($"Withdew {amount} from wallet {wallet.Id}");

                return Ok();
            }
            catch(DbUpdateConcurrencyException)
            {
                return Conflict("Concurrent withdrawal. Try again.");
            }
        }

        [HttpPut("transfer")]
        public async Task<IActionResult> transfer(Guid senderId, Guid recieverId, decimal amount, Guid idempotencyKey)
        {
            var existing= await _context.Transactions.FirstOrDefaultAsync(u=>u.IdempotencyKey==idempotencyKey);
            if (existing != null)
            {
                return BadRequest("Transfer already processed");
            }
            var transaction= await _context.Database.BeginTransactionAsync();
            try
            {
                var sender = await _context.Wallets.FindAsync(senderId);
                var reciever = await _context.Wallets.FindAsync(recieverId);
                if (sender == null || reciever == null)
                {
                    return BadRequest("Wallet not found");
                }
                if (sender.Balance < amount)
                {
                    return BadRequest("Insufficient funds");
                }
                if (sender.IsFrozen)
                {
                    return BadRequest("Sender wallet frozen.");
                }
                if (reciever.IsFrozen)
                {
                    return BadRequest("Reciever wallet frozen.");
                }
                sender.Balance -= amount;
                reciever.Balance += amount;
                await _context.Transactions.AddAsync(new Transaction
                {
                    SenderWallet = sender,
                    SenderWalletId = senderId,
                    ReceiverWalletId = recieverId,
                    ReceiverWallet = reciever,
                    Amount = amount,
                    Timestamp = DateTime.UtcNow,
                    Status = "Transfer",
                    IdempotencyKey = idempotencyKey
                });
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                await _publisher.PublishTransactionEvent($"Transferred {amount} from wallet {senderId} for wallet {recieverId}");

                return Ok("Transfer successful");
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                return Conflict("Wallet was updated concurrently");
            }
            catch
            {
                await transaction.RollbackAsync();
                return BadRequest("Transfer failed");
            }
        }
    }
}
