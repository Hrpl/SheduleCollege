using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserService.Domain.Commons.Response;
using UserService.Domain.Models;
using UserService.Infrastructure.Context;
using UserService.Infrastructure.Repositories.Interfaces;
using UserService.Infrastructure.Services.Interfaces;

namespace UserService.Infrastructure.Services.Implementations;

public class JwtService : IJwtService
{
    private readonly IAsyncRepository _asyncRepository;

    public JwtService(IAsyncRepository asyncRepository)
    {
        _asyncRepository = asyncRepository;
    }

    public async Task CreateJwtAsync(JwtModel model, CancellationToken ct)
    {
        model.CreatedAt = DateTime.Now;
        model.UpdatedAt = DateTime.Now;

        var query = _asyncRepository.GetQueryBuilder("Jwt")
            .AsInsert(model);
        await _asyncRepository.ExecuteAsync(query, ct);
    }
}
