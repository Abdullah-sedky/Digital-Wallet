using System.ComponentModel.DataAnnotations;

namespace DigitalWallet.Models
{
    public class Wallet
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public decimal Balance { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
        public bool IsFrozen { get; set; } =false;
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
