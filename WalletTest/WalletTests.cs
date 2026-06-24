global using Xunit;
global using DigitalWallet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using DigitalWallet.Controllers;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace WalletTest
{
    public class FakeRabbitMqPublisher : RabbitMqPublisher
    {
        public FakeRabbitMqPublisher() : base(null!) { }
        public override async Task PublishTransactionEvent(object message)
        {
            await Task.CompletedTask;
        }
    }

    public class WalletTests
    {
        private WalletDbContext GetContext()
        {
            var options = new DbContextOptionsBuilder<WalletDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new WalletDbContext(options);
        }

        private (WalletController controller, WalletDbContext context) GetController()
        {
            var context = GetContext();
            var publisher = new FakeRabbitMqPublisher();
            var controller = new WalletController(context, publisher);
            return (controller, context);
        }

        [Fact]
        public async Task Deposit_IncreasesBalance()
        {
            var (controller, context) = GetController();
            var user = new User { Email = "test@test.com", PasswordHash = "x" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            await controller.deposit(user.Wallet.Id, 100, Guid.NewGuid());

            var wallet = await context.Wallets.FindAsync(user.Wallet.Id);
            Assert.Equal(100, wallet!.Balance);
        }

        [Fact]
        public async Task Deposit_FrozenWallet_ReturnsBadRequest()
        {
            var (controller, context) = GetController();
            var user = new User { Email = "test@test.com", PasswordHash = "x" };
            user.Wallet.IsFrozen = true;
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var result = await controller.deposit(user.Wallet.Id, 100, Guid.NewGuid());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Deposit_DuplicateIdempotencyKey_ReturnsBadRequest()
        {
            var (controller, context) = GetController();
            var user = new User { Email = "test@test.com", PasswordHash = "x" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var key = Guid.NewGuid();
            await controller.deposit(user.Wallet.Id, 100, key);
            var result = await controller.deposit(user.Wallet.Id, 100, key);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Withdraw_DecreasesBalance()
        {
            var (controller, context) = GetController();
            var user = new User { Email = "test@test.com", PasswordHash = "x" };
            user.Wallet.Balance = 200;
            context.Users.Add(user);
            await context.SaveChangesAsync();

            await controller.withdraw(user.Wallet.Id, 100, Guid.NewGuid());

            var wallet = await context.Wallets.FindAsync(user.Wallet.Id);
            Assert.Equal(100, wallet!.Balance);
        }

        [Fact]
        public async Task Withdraw_InsufficientFunds_ReturnsBadRequest()
        {
            var (controller, context) = GetController();
            var user = new User { Email = "test@test.com", PasswordHash = "x" };
            user.Wallet.Balance = 50;
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var result = await controller.withdraw(user.Wallet.Id, 100, Guid.NewGuid());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Withdraw_FrozenWallet_ReturnsBadRequest()
        {
            var (controller, context) = GetController();
            var user = new User { Email = "test@test.com", PasswordHash = "x" };
            user.Wallet.Balance = 200;
            user.Wallet.IsFrozen = true;
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var result = await controller.withdraw(user.Wallet.Id, 100, Guid.NewGuid());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Transfer_UpdatesBothBalances()
        {
            var (controller, context) = GetController();
            var sender = new User { Email = "sender@test.com", PasswordHash = "x" };
            var receiver = new User { Email = "receiver@test.com", PasswordHash = "x" };
            sender.Wallet.Balance = 200;
            receiver.Wallet.Balance = 50;
            context.Users.AddRange(sender, receiver);
            await context.SaveChangesAsync();

            await controller.transfer(sender.Wallet.Id, receiver.Wallet.Id, 100, Guid.NewGuid());

            var senderWallet = await context.Wallets.FindAsync(sender.Wallet.Id);
            var receiverWallet = await context.Wallets.FindAsync(receiver.Wallet.Id);
            Assert.Equal(100, senderWallet!.Balance);
            Assert.Equal(150, receiverWallet!.Balance);
        }

        [Fact]
        public async Task Transfer_InsufficientFunds_ReturnsBadRequest()
        {
            var (controller, context) = GetController();
            var sender = new User { Email = "sender@test.com", PasswordHash = "x" };
            var receiver = new User { Email = "receiver@test.com", PasswordHash = "x" };
            sender.Wallet.Balance = 30;
            context.Users.AddRange(sender, receiver);
            await context.SaveChangesAsync();

            var result = await controller.transfer(sender.Wallet.Id, receiver.Wallet.Id, 100, Guid.NewGuid());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Transfer_DuplicateIdempotencyKey_ReturnsBadRequest()
        {
            var (controller, context) = GetController();
            var sender = new User { Email = "sender@test.com", PasswordHash = "x" };
            var receiver = new User { Email = "receiver@test.com", PasswordHash = "x" };
            sender.Wallet.Balance = 500;
            context.Users.AddRange(sender, receiver);
            await context.SaveChangesAsync();

            var key = Guid.NewGuid();
            await controller.transfer(sender.Wallet.Id, receiver.Wallet.Id, 100, key);
            var result = await controller.transfer(sender.Wallet.Id, receiver.Wallet.Id, 100, key);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Transfer_FrozenSender_ReturnsBadRequest()
        {
            var (controller, context) = GetController();
            var sender = new User { Email = "sender@test.com", PasswordHash = "x" };
            var receiver = new User { Email = "receiver@test.com", PasswordHash = "x" };
            sender.Wallet.Balance = 200;
            sender.Wallet.IsFrozen = true;
            context.Users.AddRange(sender, receiver);
            await context.SaveChangesAsync();

            var result = await controller.transfer(sender.Wallet.Id, receiver.Wallet.Id, 100, Guid.NewGuid());

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}