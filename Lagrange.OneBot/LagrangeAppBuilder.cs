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

        var config = new BotConfig
        {
            Protocol = Configuration.GetSection("Account:Protocol").Get<BotAppInfo>(options => { options.BindNonPublicProperties = true; }) ?? throw new Exception("Account Protocol is null"),
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

        Services.AddSingleton(BotFactory.Create(config, deviceInfo, keystore));

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

        Services.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<LagrangeAppBuilder>>();

            string path = Configuration["ConfigPath:Database"] ?? $"lagrange-{Configuration["Account:Uin"]}.db";

            var db = new LiteDatabase(path)
            {
                CheckpointSize = 50
            };
            logger.LogInformation("Indexing in the database...");
            logger.LogInformation("The first indexing of the old database will load the entire database into memory.");
            logger.LogInformation("If this is the first time creating an index for the old database, please restart the application");
            var collection = db.GetCollection<MessageRecord>();
            collection.EnsureIndex(record => record.MessageId);
            collection.EnsureIndex(record => record.Sequence);
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
