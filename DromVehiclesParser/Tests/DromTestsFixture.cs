using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests;

public sealed class DromTestsFixture : WebApplicationFactory<DromVehiclesParser.Program>, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        
    }

    public new async Task DisposeAsync()
    {
        
    }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
    }
}