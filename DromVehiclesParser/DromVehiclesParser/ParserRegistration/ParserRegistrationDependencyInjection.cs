using DromVehiclesParser.ParserRegistration.BackgroundServices;
using DromVehiclesParser.ParserRegistration.Database;
using DromVehiclesParser.ParserRegistration.Features.ConfirmRegistrationTicket;
using DromVehiclesParser.ParserRegistration.Features.ConfirmRegistrationTicket.Decorators;
using DromVehiclesParser.ParserRegistration.Features.PublishRegistrationTicket;
using DromVehiclesParser.ParserRegistration.Features.PublishRegistrationTicket.Decorators;
using DromVehiclesParser.ParserRegistration.Models;
using DromVehiclesParser.ParserRegistration.RabbitMq;
using RemTech.SharedKernel.Core.Handlers;

namespace DromVehiclesParser.ParserRegistration;

public static class ParserRegistrationDependencyInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterParserRegistrationContext()
        {
            services.AddPublishTicketRegistration();
            services.AddConfirmTicketRegistration();
            services.AddInfrastrucutre();
        }

        private void AddPublishTicketRegistration()
        {
            services.AddScoped<
                ICommandHandler<PublishRegistrationTicketCommand, ParserRegistrationTicket>,
                PublishRegistrationTicketCommandHandler
            >();
            
            services.Decorate<
                ICommandHandler<PublishRegistrationTicketCommand, ParserRegistrationTicket>, 
                PublishRegistrationTicketCommandHandlerLogging>();
        }

        private void AddConfirmTicketRegistration()
        {
            services.AddScoped<
                ICommandHandler<ConfirmRegistrationTicketCommand, ParserRegistrationTicket>,
                ConfirmRegistrationTicketCommandHandler>();

            services.Decorate<
                ICommandHandler<ConfirmRegistrationTicketCommand, ParserRegistrationTicket>,
                ConfirmRegistrationTicketCommandHandlerTransaction
            >();
            
            services.Decorate<
                ICommandHandler<ConfirmRegistrationTicketCommand, ParserRegistrationTicket>,
                ConfirmRegistrationTicketCommandHandlerLogging
            >();
        }

        private void AddInfrastrucutre()
        {
            services.AddScoped<NpgSqlParserRegistrationTicketsStorage>();
            services.AddScoped<ParserRegistrationTicketPublisher>();
            services.AddHostedService<ParserRegistrationTicketConfirmedListener>();
        }
    }

    extension(IServiceProvider sp)
    {
        public async Task PublishParserRegistration()
        {
            await using AsyncServiceScope scope = sp.CreateAsyncScope();
            ICommandHandler<PublishRegistrationTicketCommand, ParserRegistrationTicket> handler =
                scope.ServiceProvider.GetRequiredService<ICommandHandler<PublishRegistrationTicketCommand, ParserRegistrationTicket>>();
            await handler.Execute(new PublishRegistrationTicketCommand());
        }
    }
}