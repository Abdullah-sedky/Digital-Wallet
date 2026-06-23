using Microsoft.AspNetCore.Identity;

namespace DigitalWallet.Models;

public enum Roles
{
    customer,
    admin
}
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; } = "Customer";
    public Wallet Wallet { get; set; } = new Wallet();
}