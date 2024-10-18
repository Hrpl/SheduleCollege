using FastEndpoints;

namespace UserService.API.Common;

public class EndpointGroupBase : FastEndpoints.Group
{
    protected EndpointGroupBase(string groupName, string route)
    {
        this.Configure(route,( System.Action<EndpointDefinition>) (ed => ed.Options((System.Action<RouteHandlerBuilder>)(opt =>
        {
            opt.WithGroupName<RouteHandlerBuilder>(groupName);
            opt.WithTags(groupName);
        }))));
    }
}
