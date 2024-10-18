using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserService.Domain.Commons.Response;

namespace UserService.Infrastructure.Services.Interfaces;

public interface IJwtHelper
{
    public JwtResponse CreateJwtAsync(int userId, CancellationToken ct);
}
