using FrogTailGameServer.ControllerLogic;
using GameServer.GameTable;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Newtonsoft.Json.Converters;
using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Hosting;
using FrogTailGameServer.MiddleWare;
using Common.Redis;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Configuration;
using Common.Http;
using DataBase.AccountDB;
using DataBase.GameDB;
using DB;
using GameServer.Logic.Utils;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        // Set properties and call methods on options
        serverOptions.Listen(IPAddress.Loopback, 9000);
        serverOptions.ListenAnyIP(9000);
    }).UseKestrel();

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
    builder.Services.AddSingleton<PacketHandler>();
    builder.Services.AddSingleton<RedisClient>();

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

    builder.Services.AddSingleton<DataBaseManager>();


    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddDistributedMemoryCache();



    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.IncludeFields = true;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    //CustomService Start
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

    var packetHandler = app.Services.GetService<PacketHandler>();
    if(packetHandler == null)
    {
        throw new Exception("Packet Handler Error");
    }
    packetHandler.InitPacketHandler();
    UniqueKey.LoadUniqueKey(1);



	app.UseMiddleware<CustomMiddleWare>();


    app.UseAuthentication();
    app.UseAuthorization();

    GameTableManager.GetInstance().Init("GameJson");

    if(HttpManager.GetInstance() == null)
    {
        throw new Exception("Not Init HttpManager Instace");
    }

    //if(StoveClient.GetInstnace() == null)
    //{
    //    throw new Exception("Not Init Stove Instace");
    //}
   
    app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());


    app.MapControllers();

    app.Run();
}
catch(Exception ex)
{
    Console.WriteLine(ex.ToString());
}

