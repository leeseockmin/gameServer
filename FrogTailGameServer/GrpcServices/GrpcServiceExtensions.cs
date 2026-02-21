using Microsoft.AspNetCore.Routing;

namespace FrogTailGameServer.GrpcServices;

public static class GrpcServiceExtensions
{
    /// <summary>
    /// 새 gRPC 서비스를 추가할 때 Program.cs가 아닌 이 파일에만 한 줄 추가합니다.
    /// </summary>
    public static IEndpointRouteBuilder MapGrpcServices(this IEndpointRouteBuilder app)
    {
        app.MapGrpcService<GrpcAuthService>();

        return app;
    }
}
