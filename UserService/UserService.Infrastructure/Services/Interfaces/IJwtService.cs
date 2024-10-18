using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserService.Domain.Commons.Response;
using UserService.Domain.Models;

namespace UserService.Infrastructure.Services.Interfaces;

public interface IJwtService 
{
    public Task CreateJwtAsync(JwtModel model, CancellationToken ct);
}
