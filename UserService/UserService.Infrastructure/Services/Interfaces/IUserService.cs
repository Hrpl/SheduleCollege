using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Services.Interfaces;

public interface IUserService
{
    public Task CreateUserAsync(AppUser user, CancellationToken ct);
}
