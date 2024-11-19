using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Utility.Sign;
using Lagrange.OneBot.Core.Network;
using Lagrange.OneBot.Core.Network.Service;
using Lagrange.OneBot.Core.Notify;
using Lagrange.OneBot.Core.Operation;
using Lagrange.OneBot.Database;
using Lagrange.OneBot.Message;
using Lagrange.OneBot.Utility;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Lagrange.OneBot;

public sealed class LagrangeAppBuilder
{
    private IServiceCollection Services => _hostAppBuilder.Services;

    private ConfigurationManager Configuration => _hostAppBuilder.Configuration;

    private readonly HostApplicationBuilder _hostAppBuilder;

    public LagrangeAppBuilder(string[] args)
    {
        _hostAppBuilder = new HostApplicationBuilder(args);
    }

    public LagrangeAppBuilder ConfigureConfiguration(string path, bool optional = false, bool reloadOnChange = false)
    {
        Configuration.AddJsonFile(path, optional, reloadOnChange);
        Configuration.AddEnvironmentVariables(); // use this to configure appsettings.json with environment variables in docker container
        return this;
    }

    public LagrangeAppBuilder ConfigureBots()
    {
        string keystorePath = Configuration["ConfigPath:Keystore"] ?? "keystore.json";
        string deviceInfoPath = Configuration["ConfigPath:DeviceInfo"] ?? "device.json";
        string appInfoPath = Configuration["ConfigPath:AppInfo"] ?? "appinfo.json";

        bool isSuccess = Enum.TryParse<Protocols>(Configuration["Account:Protocol"], out var protocol);
        var config = new BotConfig
        {
            Protocol = isSuccess ? protocol : Protocols.Linux,
            AutoReconnect = bool.Parse(Configuration["Account:AutoReconnect"] ?? "true"),
            UseIPv6Network = bool.Parse(Configuration["Account:UseIPv6Network"] ?? "false"),
            GetOptimumServer = bool.Parse(Configuration["Account:GetOptimumServer"] ?? "true"),
            AutoReLogin = bool.Parse(Configuration["Account:AutoReLogin"] ?? "true"),
        };

        BotKeystore keystore;
        if (!File.Exists(keystorePath))
        {
            keystore = Configuration["Account:Uin"] is { } uin && Configuration["Account:Password"] is { } password
                    ? new BotKeystore(uint.Parse(uin), password)
                    : new BotKeystore();
            string? directoryPath = Path.GetDirectoryName(keystorePath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
        else
        {
            keystore = JsonSerializer.Deserialize<BotKeystore>(File.ReadAllText(keystorePath)) ?? new BotKeystore();
        }

        BotDeviceInfo deviceInfo;
        if (!File.Exists(deviceInfoPath))
        {
            deviceInfo = BotDeviceInfo.GenerateInfo();
            string json = JsonSerializer.Serialize(deviceInfo);
            string? directoryPath = Path.GetDirectoryName(deviceInfoPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            File.WriteAllText(deviceInfoPath, json);
        }
        else
        {
            deviceInfo = JsonSerializer.Deserialize<BotDeviceInfo>(File.ReadAllText(deviceInfoPath)) ?? BotDeviceInfo.GenerateInfo();
        }

        BotAppInfo appInfo;
        if (!File.Exists(appInfoPath))
        {
            appInfo = BotAppInfo.ProtocolToAppInfo[config.Protocol];
            string json = JsonSerializer.Serialize(appInfo);
            string? directoryPath = Path.GetDirectoryName(appInfoPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            File.WriteAllText(appInfoPath, json);
        }
        else
        {
            appInfo = JsonSerializer.Deserialize<BotAppInfo>(File.ReadAllText(appInfoPath)) ?? BotAppInfo.ProtocolToAppInfo[config.Protocol];
        }
        
        Services.AddSingleton(BotFactory.Create(config, deviceInfo, keystore, appInfo));

        return this;
    }

    public LagrangeAppBuilder ConfigureOneBot()
    {
        Services.AddSingleton<LagrangeWebSvcCollection>();
        Services.AddOptions();

        Services.AddScoped<ILagrangeWebServiceFactory<ForwardWSService>, ForwardWSServiceFactory>();
        Services.AddScoped<ForwardWSService>();
        Services.AddScoped<ILagrangeWebServiceFactory<ReverseWSService>, ReverseWSServiceFactory>();
        Services.AddScoped<ReverseWSService>();
        Services.AddScoped<ILagrangeWebServiceFactory<HttpService>, HttpServiceFactory>();
        Services.AddScoped<HttpService>();
        Services.AddScoped<ILagrangeWebServiceFactory<HttpPostService>, HttpPostServiceFactory>();
        Services.AddScoped<HttpPostService>();
        Services.AddScoped<ILagrangeWebServiceFactory, DefaultLagrangeWebServiceFactory>();
        Services.AddScoped(services =>
        {
            return services.GetRequiredService<ILagrangeWebServiceFactory>().Create() ?? throw new Exception("Invalid conf detected");
        });

        // Database
        Services.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<LagrangeAppBuilder>>();

            BsonMapper.Global.TrimWhitespace = false;
            BsonMapper.Global.EmptyStringToNull = false;

            // Specify ctor for some classes
            BsonMapper.Global.RegisterType(
                LiteDbUtility.IMessageEntitySerialize,
                LiteDbUtility.IMessageEntityDeserialize
            );

            string path = Configuration["ConfigPath:Database"] ?? $"lagrange-{Configuration["Account:Uin"]}.db";

            bool isFirstCreate = false;
            if (!File.Exists(path)) isFirstCreate = true;

            var db = new LiteDatabase(path)
            {
                CheckpointSize = 50
            };

            string[] expressions = ["$.Sequence", "$.MessageId", "$.FriendUin", "$.GroupUin"];

            bool hasFirstIndex = false;
            var indexes = db.GetCollection("$indexes");
            foreach (var expression in expressions)
            {
                if (indexes.Exists(Query.EQ("expression", expression))) continue;

                logger.LogWarning("In the database index");
                logger.LogWarning("Depending on the size of the database will consume some time and memory");
                logger.LogWarning("Not yet finished, please wait...");

                hasFirstIndex = true;
                break;
            }

            var records = db.GetCollection<MessageRecord>();
            foreach (var expression in expressions)
            {
                records.EnsureIndex(BsonExpression.Create(expression));
            }

            // Skipping the first database creation is a restart after indexing
            if (!isFirstCreate && hasFirstIndex)
            {
                db.Dispose(); // Ensure that the database is written correctly
                logger.LogInformation("Indexing Complete! Press any key to close and restart the program manually!");
                Console.ReadKey(true);
                Environment.Exit(0);
            }
            return db;
        });
        Services.AddSingleton<SignProvider, OneBotSigner>();

        Services.AddSingleton<MusicSigner>();
        Services.AddSingleton<NotifyService>();
        Services.AddSingleton<MessageService>();
        Services.AddSingleton<OperationService>();
        return this;
    }

    public LagrangeAppBuilder ConfigureLogging(Action<ILoggingBuilder> configureLogging)
    {
        Services.AddLogging(configureLogging);
        return this;
    }

    public LagrangeApp Build() => new(_hostAppBuilder.Build());
}