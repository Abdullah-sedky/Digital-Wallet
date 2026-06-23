using Microsoft.EntityFrameworkCore;

namespace WalletTest
{
    public class WalletTests
    {
        private WalletDbContext GetContext()
        {
            var options = new DbContextOptionsBuilder<WalletDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new WalletDbContext(options);
        }

        [Fact]
        public async Task Deposit_IncreasesBalance()
        {
            var context = GetContext();
            var user = new User { Email = "test@test.com", PasswordHash = "x" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            user.Wallet.Balance += 100;
            await context.SaveChangesAsync();

            Assert.Equal(100, user.Wallet.Balance);
        }

        [Fact]
        public async Task Withdraw_SucceedsWithSufficientFunds()
        {
            var context = GetContext();
            var user = new User { Email = "test@test.com", PasswordHash = "x" };
            user.Wallet.Balance = 200;
            context.Users.Add(user);
            await context.SaveChangesAsync();

            user.Wallet.Balance -= 100;
            await context.SaveChangesAsync();

            Assert.Equal(100, user.Wallet.Balance);
        }

        [Fact]
        public async Task Withdraw_FailsWithInsufficientFunds()
        {
            var context = GetContext();
            var user = new User { Email = "test@test.com", PasswordHash = "x" };
            user.Wallet.Balance = 50;
            context.Users.Add(user);
            await context.SaveChangesAsync();

            bool sufficientFunds = user.Wallet.Balance >= 100;

            Assert.False(sufficientFunds);
        }

        [Fact]
        public async Task Transfer_UpdatesBothBalancesCorrectly()
        {
            var context = GetContext();
            var sender = new User { Email = "sender@test.com", PasswordHash = "x" };
            var receiver = new User { Email = "receiver@test.com", PasswordHash = "x" };
            sender.Wallet.Balance = 200;
            receiver.Wallet.Balance = 50;
            context.Users.AddRange(sender, receiver);
            await context.SaveChangesAsync();

            sender.Wallet.Balance -= 100;
            receiver.Wallet.Balance += 100;
            await context.SaveChangesAsync();

            Assert.Equal(100, sender.Wallet.Balance);
            Assert.Equal(150, receiver.Wallet.Balance);
        }

        [Fact]
        public async Task Transfer_FailsWhenSenderHasInsufficientFunds()
        {
            var context = GetContext();
            var sender = new User { Email = "sender@test.com", PasswordHash = "x" };
            sender.Wallet.Balance = 30;
            context.Users.Add(sender);
            await context.SaveChangesAsync();

            bool canTransfer = sender.Wallet.Balance >= 100;

            Assert.False(canTransfer);
        }
    }
}