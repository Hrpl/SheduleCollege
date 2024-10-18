using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserService.Domain.Constant;

namespace UserService.Domain.Entities;

[Table(EntityInformation.User.JwtTable)]
public class Jwt : BaseEntity
{
    public int UserId { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}
