﻿using Aspire.Components.Common.Tests;
using Aspire.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.CommunityToolkit.Testing;

public class AspireIntegrationTestFixture<TEntryPoint>() : DistributedApplicationFactory(typeof(TEntryPoint), []), IAsyncLifetime where TEntryPoint : class
{
    public ResourceNotificationService ResourceNotificationService => App.Services.GetRequiredService<ResourceNotificationService>();

    public DistributedApplication App { get; private set; } = null!;

    protected override void OnBuilt(DistributedApplication application)
    {
        App = application;
        base.OnBuilt(application);
    }

    protected override void OnBuilderCreated(DistributedApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.AddLogging(builder =>
            {
                builder.AddXUnit();
                if (Environment.GetEnvironmentVariable("RUNNER_DEBUG") is not null or "1")
                    builder.SetMinimumLevel(LogLevel.Trace);
                else
                    builder.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureHttpClientDefaults(clientBuilder => clientBuilder.AddStandardResilienceHandler());

        base.OnBuilderCreated(applicationBuilder);
    }

    public async Task InitializeAsync()
    {
        if (RequiresDockerAttribute.IsSupported)
            await StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        try
        {
            await DisposeAsync();
        }
        catch (Exception)
        {
            if (RequiresDockerAttribute.IsSupported)
            {
                // GitHub Actions Windows runners don't support Linux Docker containers, which can result in a bunch of false errors, even if we try to skip the test run, so we only really want to throw
                // if we're on a non-Windows runner or if we're on a Windows runner but not in a CI environment
                throw;
            }
        }
    }
}