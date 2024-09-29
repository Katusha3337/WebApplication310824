using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Список разрешенных IP-адресов для доступа к /admin
var allowedAdminIps = new HashSet<string> { "127.0.0.1", "::1" }; 

// Счетчик запросов по IP-адресам
var requestCounts = new ConcurrentDictionary<string, int>();

// Middleware для подсчета запросов и блокировки при превышении лимита
app.Use(async (context, next) =>
{
    var ipAddress = context.Connection.RemoteIpAddress?.ToString();
    if (ipAddress != null)
    {
        requestCounts.AddOrUpdate(ipAddress, 1, (key, count) => count + 1);

        if (requestCounts[ipAddress] > 100)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Доступ временно закрыт из-за превышения лимита запросов.");
            return;
        }
    }

    await next();
});

// Middleware для обработки запросов к /admin
app.UseWhen(context => context.Request.Path.StartsWithSegments("/admin"), appBuilder =>
{
    appBuilder.Use(async (context, next) =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        if (ipAddress == null || !allowedAdminIps.Contains(ipAddress))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Доступ запрещен.");
            return;
        }

        await next();
    });
});

// Маршрут для /admin
app.Map("/admin", appBuilder =>
{
    appBuilder.Run(async context =>
    {
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync("Добро пожаловать на страницу администратора!");
    });
});

// Маршрут для всех остальных запросов
app.Run(async context =>
{
    context.Response.ContentType = "text/plain; charset=utf-8";
    await context.Response.WriteAsync("Добро пожаловать на наш сайт!");
});

app.Run();
