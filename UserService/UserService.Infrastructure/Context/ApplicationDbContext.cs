using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Context;

public class ApplicationDbContext : IdentityDbContext<AppUser, AppRole, int>
{
    public DbSet<Jwt> JwtTokens { get; set; }
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        
    }
}

