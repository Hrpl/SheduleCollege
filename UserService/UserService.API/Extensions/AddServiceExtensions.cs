using FastEndpoints;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;
using IMapper = MapsterMapper.IMapper;

namespace UserService.API.Extensions;

public static class AddServiceExtensions
{
    public static void AddService(this IServiceCollection services)
    {
        services.AddFastEnpoint();
        services.AddMapster();
        services.AddRegisterService();
    }

    public static void AddJwt(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["JwtConfigurations:Issuer"],
                ValidateAudience = true,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtConfigurations:Key"])),
                ValidateIssuerSigningKey = true
            };
        });
        builder.Services.AddAuthorization();
    }

    public static void AddMapster(this IServiceCollection services)
    {
        TypeAdapterConfig config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());

        Mapper mapperConf = new(config);
        services.AddSingleton<IMapper>(mapperConf);
    }

    public static void AddFastEnpoint(this IServiceCollection services)
    {
        services.AddFastEndpoints();
        services.AddSwaggerDocument();
    }


    public static void AddRegisterService(this IServiceCollection services)
    {
    } 
}
