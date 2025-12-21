using Microsoft.Extensions.Options;
using ParserSubscriber.SubscribtionContext;
using ParserSubscriber.SubscribtionContext.Options;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.RabbitMq;

namespace DromVehiclesParser.DependencyInjection;

public static class ParserSubscriptionInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterParserSubscription()
        {
            services.RegisterRabbitMqResponseReplyListeningOptions();
            services.RegisterParserSubscriber<RabbitMqRequestReplySubscriber>(
                RabbitMqProvidingConfiguration,
                NpgSqlProvidingConfiguration,
                "drom_vehicles_parser"
                );
        }

        private void RegisterRabbitMqResponseReplyListeningOptions()
        {
            services.AddOptions<RabbitMqRequestReplyResponseListeningQueueOptions>()
                .BindConfiguration(nameof(RabbitMqRequestReplyResponseListeningQueueOptions));
        }
    }

    private static Action<IServiceCollection> RabbitMqProvidingConfiguration => services =>
    {
        services.AddSingleton<RabbitMqConnectionProvider>(sp =>
        {
            RabbitMqConnectionSource source = sp.GetRequiredService<RabbitMqConnectionSource>();
            RabbitMqConnectionProvider provider = async ct => await source.GetConnection(ct);
            return provider;
        });
    };

    private static Action<IServiceCollection> NpgSqlProvidingConfiguration => services =>
    {
        services.AddSingleton<NpgSqlProvider>(sp =>
        {
            IOptions<NpgSqlOptions> options = sp.GetRequiredService<IOptions<NpgSqlOptions>>();
            return new NpgSqlProvider() { ConnectionString = options.Value.ToConnectionString() };
        });
    };
}