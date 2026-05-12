namespace Pm.Models;

public enum Roles
{
    Owner,
    Admin,
    Basic

}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    
    public Roles Role { get; set; } = Roles.Basic; 
}