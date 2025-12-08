using Microsoft.Extensions.DependencyInjection;
using Tests.StartParsingTests;

namespace Tests;

public static class DromTestsExtensions
{
    extension(IServiceProvider sp)
    {
        public async Task PublishFakeMessage(object message)
        {
            FakeStartParserPublisher publisher = sp.GetRequiredService<FakeStartParserPublisher>();
            await publisher.Publish(message);
        }
    }
}