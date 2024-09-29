using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ������ ����������� IP-������� ��� ������� � /admin
var allowedAdminIps = new HashSet<string> { "127.0.0.1", "::1" }; 

// ������� �������� �� IP-�������
var requestCounts = new ConcurrentDictionary<string, int>();

// Middleware ��� �������� �������� � ���������� ��� ���������� ������
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
            await context.Response.WriteAsync("������ �������� ������ ��-�� ���������� ������ ��������.");
            return;
        }
    }

    await next();
});

// Middleware ��� ��������� �������� � /admin
app.UseWhen(context => context.Request.Path.StartsWithSegments("/admin"), appBuilder =>
{
    appBuilder.Use(async (context, next) =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        if (ipAddress == null || !allowedAdminIps.Contains(ipAddress))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("������ ��������.");
            return;
        }

        await next();
    });
});

// ������� ��� /admin
app.Map("/admin", appBuilder =>
{
    appBuilder.Run(async context =>
    {
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync("����� ���������� �� �������� ��������������!");
    });
});

// ������� ��� ���� ��������� ��������
app.Run(async context =>
{
    context.Response.ContentType = "text/plain; charset=utf-8";
    await context.Response.WriteAsync("����� ���������� �� ��� ����!");
});

app.Run();
