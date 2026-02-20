using GameServer.GameTable;
using Microsoft.Extensions.Hosting;
using Common.Redis;
using FrogTailGameServer.MiddleWare.Secret;
using Microsoft.EntityFrameworkCore;
using Common.Http;
using DataBase.AccountDB;
using DataBase.GameDB;
using DB;
using FrogTailGameServer.Logic.Utils;
using GameServer.Logic.Utils;
using FrogTailGameServer.GrpcServices;
using FrogTailGameServer.MiddleWare.AccountLink;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Kestrel: gRPC 전용 (HTTP/2)
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenAnyIP(9001, o => o.Protocols = HttpProtocols.Http2);   // gRPC (h2c)
    }).UseKestrel();

    builder.Services.AddHttpContextAccessor();

    // Singleton Services
    builder.Services.AddSingleton<RedisClient>();
    builder.Services.AddSingleton<DataBaseManager>();
    builder.Services.AddSingleton<HttpManager>();
    builder.Services.AddSingleton<GameTableManager>();
    builder.Services.AddSingleton<SecretManager>();
    builder.Services.AddSingleton<AuthInterceptor>();
    builder.Services.AddSingleton<IAccountLinkValidator, MockAccountLinkValidator>();

    // gRPC
    builder.Services.AddGrpc(options =>
    {
        options.Interceptors.Add<AuthInterceptor>();
    });

    // Database
    var gameDBconnection = builder.Configuration.GetConnectionString("GameDbConnection");
    builder.Services.AddDbContextFactory<GameDBContext>(option =>
    {
        option.UseMySQL(gameDBconnection!);
    }, ServiceLifetime.Singleton);

    var accountDBconnection = builder.Configuration.GetConnectionString("AccountDbConnection");
    builder.Services.AddDbContextFactory<AccountDBContext>(option =>
    {
        option.UseMySQL(accountDBconnection!);
    }, ServiceLifetime.Singleton);

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .WriteTo.Console()
        .WriteTo.File(
            path: "Logs/log-{Date}.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        )
        .CreateLogger();

    builder.Host.UseSerilog();

    var app = builder.Build();

    // Service validation
    var redisClient = app.Services.GetRequiredService<RedisClient>();
    if (!redisClient.IsConnect())
    {
        throw new Exception("Redis Connect Error");
    }

    var dataBaseManager = app.Services.GetRequiredService<DataBaseManager>();
    Log.Information("[Startup] DataBaseManager initialized.");

    // SecretManager validation (DI로 초기화됨, 설정값 유효성은 생성자에서 검증)
    var secretManager = app.Services.GetRequiredService<SecretManager>();
    Log.Information("[Startup] SecretManager initialized.");

    // Firebase
    var firebaseSection = app.Configuration.GetSection("FirebaseConfig");
    var firebaseDict = firebaseSection.GetChildren()
        .ToDictionary(x => x.Key, x => x.Value ?? string.Empty);
    var firebaseJson = Newtonsoft.Json.JsonConvert.SerializeObject(firebaseDict);
    FireBase.InitFireBase(firebaseJson);
    Log.Information("[Startup] Firebase initialized.");

    UniqueKey.LoadUniqueKey(1);

    // GameTableManager 초기화
    var gameTableManager = app.Services.GetRequiredService<GameTableManager>();
    gameTableManager.Init("GameJson");
    Log.Information("[Startup] GameTableManager initialized.");

    // HttpManager validation
    var httpManager = app.Services.GetRequiredService<HttpManager>();
    Log.Information("[Startup] HttpManager initialized.");

    // gRPC 서비스 등록
    app.MapGrpcService<GrpcAuthService>();
    app.MapGrpcService<GrpcShopService>();
    app.MapGrpcService<GrpcAccountLinkService>();

    Log.Information("[Startup] FrogTailGameServer started. gRPC listening on port 9001.");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
