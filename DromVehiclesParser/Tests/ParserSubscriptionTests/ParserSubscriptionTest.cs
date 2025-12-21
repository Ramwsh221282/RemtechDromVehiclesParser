using Microsoft.Extensions.DependencyInjection;
using ParserSubscriber.SubscribtionContext;

namespace Tests.ParserSubscriptionTests;

public sealed class ParserSubscriptionTest(IntegrationalTestsFixture fixture) : IClassFixture<IntegrationalTestsFixture>
{
    private IServiceProvider Services { get; } = fixture.Services;

    [Fact]
    private async Task Test_Subscription()
    {
        await Services.RunParserSubscription();
        bool hasSubscription = await HasParserSubscription();
        Assert.True(hasSubscription);
    }

    private async Task<bool> HasParserSubscription()
    {
        SubscriptionStorage storage = Services.GetRequiredService<SubscriptionStorage>();
        ParserSubscribtion? subscribtion = await storage.GetSubscription();
        return subscribtion != null;
    }
}