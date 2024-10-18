using UserService.API.Common;

namespace UserService.API.Enpoints.User
{
    public class UserEndpointGroup : EndpointGroupBase
    {
        public UserEndpointGroup() : base("User", "user")
        {
        }
    }
}
