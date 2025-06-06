using Adnc.Shared.WebApi.Registrar;

namespace Adnc.Demo.Whse.Api;

public sealed class DependencyRegistrar(IServiceCollection services, IServiceInfo serviceInfo, IConfiguration configuration) : AbstractWebApiDependencyRegistrar(services, serviceInfo, configuration)
{
    public override void AddAdncServices()
    {
        var registrar = new Application.DependencyRegistrar(Services, ServiceInfo, Configuration);
        registrar.AddApplicationServices();

        AddWebApiDefaultServices();

        var connectionString = Configuration.GetValue<string>(NodeConsts.SqlServer_ConnectionString) ?? throw new ArgumentNullException(nameof(NodeConsts.SqlServer_ConnectionString));
        var redisSecton = Configuration.GetRequiredSection(NodeConsts.Redis);
        var rabbitSecton = Configuration.GetRequiredSection(NodeConsts.RabbitMq);
        var clientProvidedName = ServiceInfo.Id;
        Services.AddHealthChecks(checksBuilder =>
        {
            checksBuilder
                    .AddSqlServer(connectionString)
                    .AddRedis(redisSecton)
                    .AddRabbitMQ(rabbitSecton, clientProvidedName);
        });
    }
}
