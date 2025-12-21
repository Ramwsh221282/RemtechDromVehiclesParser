using Microsoft.Extensions.DependencyInjection;

namespace Tests.ParserStartTests;

public sealed class ParserStartTest(IntegrationalTestsFixture fixture) : IClassFixture<IntegrationalTestsFixture>
{
    private IServiceProvider Services { get; } = fixture.Services;
    private const string linkUrl = "https://auto.drom.ru/spec/ponsse/forestry/all/";
    
    [Fact]
    private async Task Test_Parser_Start()
    {
        await Task.Delay(TimeSpan.FromSeconds(10));
        await PublishParserStart();
        await Task.Delay(TimeSpan.FromMinutes(10));
    }

    private async Task PublishParserStart()
    {
        FakeParserStartData data = new(Guid.NewGuid(), "Техника", "Drom");
        FakeParserLinkData[] links = [new(Guid.NewGuid(), linkUrl)];
        FakeParserPublishPayload payload = new(data, links);
        FakeParserStartPublisher publisher = Services.GetRequiredService<FakeParserStartPublisher>();
        await publisher.Publish(payload);
    }
}