namespace DigitalWallet.Models
{
    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SenderWalletId { get; set; }
        public Wallet SenderWallet { get; set; }
        public Guid? ReceiverWalletId { get; set; }
        public Wallet? ReceiverWallet { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
        public Guid IdempotencyKey { get; set; }

    }
}
