using GameServer.GameTable;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using FrogTailGameServer.MiddleWare;
using Common.Redis;
using FrogTailGameServer.MiddleWare.Secret;
using Microsoft.EntityFrameworkCore;
using Common.Http;
using DataBase.AccountDB;
using DataBase.GameDB;
using DB;
using GameServer.Logic.Utils;
using FrogTailGameServer.Logic.Utils;
using FrogTailGameServer.Swagger;
using FrogTailGameServer.Services;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Kestrel
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenAnyIP(9000);
    }).UseKestrel();

    builder.Services.AddHttpContextAccessor();

    // Services
    builder.Services.AddSingleton<RedisClient>();
    builder.Services.AddSingleton<DataBaseManager>();
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<ShopService>();
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

	builder.Services.AddSwaggerGen(c =>
	{
		c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "FrogTail Game Server", Version = "v1" });
		c.OperationFilter<AddCustomHeaders>();
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

    // Swagger (Development only)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Service validation
    var redisClient = app.Services.GetService<RedisClient>();
    if (redisClient == null || redisClient.IsConnect() == false)
    {
        throw new Exception("Redis Connect Error");
    }

    var getDataBaseManager = app.Services.GetService<DataBaseManager>();
    if(getDataBaseManager == null)
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

    // Security
	var encryptionKey = app.Configuration.GetSection("Security")["EncryptionKey"];
	if (string.IsNullOrEmpty(encryptionKey))
	{
		throw new Exception("Security:EncryptionKey is not configured in appsettings.json");
	}
	SecretManager.Initialize(encryptionKey);

    // Middleware
	app.UseMiddleware<CustomMiddleWare>();

    app.UseAuthentication();
    app.UseAuthorization();

    GameTableManager.GetInstance().Init("GameJson");

    if(HttpManager.GetInstance() == null)
    {
        throw new Exception("Not Init HttpManager Instance");
    }

    // CORS
    var allowedOrigins = app.Configuration.GetSection("Security:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    app.UseCors(x =>
    {
        if (app.Environment.IsDevelopment())
        {
            x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            x.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
        }
    });

    app.MapControllers();

    app.Run();
}
catch(Exception ex)
{
	throw new Exception("Application Error :" , ex);
}
