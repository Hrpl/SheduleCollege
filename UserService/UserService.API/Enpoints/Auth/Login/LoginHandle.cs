using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using UserService.Domain.Commons.Response;
using UserService.Domain.Entities;

namespace UserService.API.Enpoints.Auth.Login;

public class LoginHandle : Endpoint<LoginRequest, JwtResponse>
{
    private readonly UserManager<AppUser> _userManager;

    public LoginHandle(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public override void Configure()
    {
        Post("login");
        AllowAnonymous();
        Group<AuthEnpointsGroup>();
        Summary(sum =>
        {
            sum.Summary = "Создание jwt токена";
            sum.Description = "Эндпоинт для создания jwt";
        });
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        //TODO: создать middleware для обработки статсу кодов
        if (user == null) throw new Exception("Неверный логин");

        var result = await _userManager.CheckPasswordAsync(user, req.Password);

        if (!result) throw new Exception("Неверный пароль"); 

    }
}
