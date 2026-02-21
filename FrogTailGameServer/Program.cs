using GameServer.GameTable;
using System.Text.Json.Serialization;
using Common.Redis;
using FrogTailGameServer.MiddleWare.Secret;
using Microsoft.EntityFrameworkCore;
using DataBase.AccountDB;
using DataBase.GameDB;
using DB;
using GameServer.Logic.Utils;
using FrogTailGameServer.Logic.Utils;
using FrogTailGameServer.GrpcServices;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Kestrel — gRPC는 HTTP/2 필요
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenAnyIP(9001, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
    }).UseKestrel();

    // gRPC
    builder.Services.AddGrpc(options =>
    {
        options.Interceptors.Add<AuthInterceptor>();
    });

    builder.Services.AddHttpContextAccessor();

    // Services
    builder.Services.AddSingleton<RedisClient>();
    builder.Services.AddSingleton<DataBaseManager>();
    builder.Services.AddSingleton<SecretManager>();

    // Database
    var gameDBconnection = builder.Configuration.GetConnectionString("GameDbConnection");
    builder.Services.AddDbContextFactory<GameDBContext>(option =>
    {
        option.UseMySQL(gameDBconnection);
    }, ServiceLifetime.Singleton);

    var accountDBconnection = builder.Configuration.GetConnectionString("AccountDbConnection");
    builder.Services.AddDbContextFactory<AccountDBContext>(option =>
    {
        option.UseMySQL(accountDBconnection);
    }, ServiceLifetime.Singleton);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddDistributedMemoryCache();

    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.IncludeFields = true;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

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
    var redisClient = app.Services.GetService<RedisClient>();
    if (redisClient == null || redisClient.IsConnect() == false)
    {
        throw new Exception("Redis Connect Error");
    }

    var getDataBaseManager = app.Services.GetService<DataBaseManager>();
    if (getDataBaseManager == null)
    {
        throw new Exception("DataManager Init Error");
    }

    // Firebase
    var firebaseSection = app.Configuration.GetSection("FirebaseConfig");
    var firebaseDict = firebaseSection.GetChildren()
        .ToDictionary(x => x.Key, x => x.Value);
    var fireBasejson = Newtonsoft.Json.JsonConvert.SerializeObject(firebaseDict);
    FireBase.InitFireBase(fireBasejson);

    UniqueKey.LoadUniqueKey(1);

    // GameTable
    var gameTableManager = new GameTableManager();
    gameTableManager.Init("GameJson");

    app.UseAuthentication();
    app.UseAuthorization();

    // gRPC 서비스 등록 (GrpcServices/GrpcServiceExtensions.cs 에서 관리)
    app.MapGrpcServices();

    app.Run();
}
catch (Exception ex)
{
    throw new Exception("Application Error :", ex);
}
