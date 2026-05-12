using Pm.Models;
using Microsoft.EntityFrameworkCore;

namespace Pm.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
}