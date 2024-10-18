using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using UserService.Domain.Entities;
using UserService.Infrastructure.Context;

namespace UserService.API.Extensions;

public static class DbExtensions
{
    public static void AddDataBase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration["ConnectionString:DefaultConnection"],
            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
        ));

        builder.Services.AddIdentityCore<AppUser>()
        .AddRoles<AppRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
    }
}
