using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using neurozen.API.IAM.Application.Internal.OutboundServices;

namespace neurozen.API.Tests.Integration.Support;

/// <summary>
///     The real API (Program.cs) on a TestServer, environment "Testing", with two test-only replacements:
///     emails are captured instead of sent, and the client IP comes from the X-Test-Client-Ip header so that
///     per-IP rate limits isolate tests from each other.
/// </summary>
public sealed class NeuroZenApiFactory : WebApplicationFactory<Program>
{
    public CapturingEmailQueue Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailQueue>();
            services.AddSingleton<IEmailQueue>(Emails);
            services.AddSingleton<IStartupFilter, TestClientIpStartupFilter>();
        });
    }
}

public sealed class CapturingEmailQueue : IEmailQueue
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public ValueTask EnqueueAsync(EmailMessage message)
    {
        _messages.Enqueue(message);
        return ValueTask.CompletedTask;
    }

    public EmailMessage? LastSentTo(string email) =>
        _messages.LastOrDefault(m => string.Equals(m.To, email, StringComparison.OrdinalIgnoreCase));

    public int CountSentTo(string email) =>
        _messages.Count(m => string.Equals(m.To, email, StringComparison.OrdinalIgnoreCase));
}

public sealed class TestClientIpStartupFilter : IStartupFilter
{
    public const string Header = "X-Test-Client-Ip";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (HttpContext context, RequestDelegate nextMiddleware) =>
        {
            if (context.Request.Headers.TryGetValue(Header, out var value) && IPAddress.TryParse(value, out var ip))
                context.Connection.RemoteIpAddress = ip;
            await nextMiddleware(context);
        });
        next(app);
    };
}
