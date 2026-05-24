using System;
using Godot;
using Game.Server.Services.GRpc.Spawn;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Game.Temperance.Network;

public partial class GRpcServer : Node
{
    public static GRpcServer Instance { get; } = new();
    
    public const string Ip = "127.0.0.1";
    public const ushort Port = 3802;
    private WebApplication? _app;
    
    public override void _Ready()
    {
        base._Ready();

        _app = ConfigureWebApplication();
        MapServices(ref _app);
        _app.RunAsync("https://" + Ip + ":" + Port);
    }
    
    public override void _ExitTree()
    {
        base._ExitTree();

        _ = _app?.DisposeAsync().AsTask();
    }

    /// <summary>
    /// Required configurations to keep the connection with clients alive even during periods with no messages
    /// </summary>
    private WebApplication ConfigureWebApplication()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddGrpc();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.Http2.KeepAlivePingDelay = TimeSpan.FromSeconds(30);
            options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
        });
        
        return builder.Build();
    }
    
    /// <summary>
    /// Servces mapped here are enabled to communicate over the web app
    /// </summary>
    private void MapServices(ref WebApplication app)
    {
        app.MapGet("/", () => "Well you're certainly in an odd place, aren't you?");
        app.MapGrpcService<NodeSpawnerServer>();
    }
}