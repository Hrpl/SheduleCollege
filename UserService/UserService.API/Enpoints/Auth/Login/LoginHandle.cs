using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using UserService.Domain.Commons.Response;
using UserService.Domain.Entities;
using UserService.Domain.Models;
using UserService.Infrastructure.Services.Interfaces;

namespace UserService.API.Enpoints.Auth.Login;

public class LoginHandle : Endpoint<LoginRequest, JwtResponse>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IJwtHelper _jwtHelper;
    private readonly IMapper _mapper;
    private readonly ILogger<LoginHandle> _logger;

    public LoginHandle(UserManager<AppUser> userManager,
        IJwtHelper jwtHelper,
        IJwtService jwtService,
        IMapper mapper,
        ILogger<LoginHandle> logger)
    {
        _userManager = userManager;
        _jwtHelper = jwtHelper;
        _jwtService = jwtService;
        _mapper = mapper;
        _logger = logger;
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

        var jwt = _jwtHelper.CreateJwtAsync(user.Id, ct);
        var jwtModel = _mapper.Map<JwtModel>(jwt);
        jwtModel.UserId = user.Id;

        await _jwtService.CreateJwtAsync(jwtModel, ct);

        await SendOkAsync();
    }
}
