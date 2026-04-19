using System;
using System.Collections.Generic;
using Godot;
using IdyllicMultiplayerProject.Resources.ProtocolBuffers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace IdyllicMultiplayerProject.Temperance.Networking;

public partial class GRpcServer : Node
{
    public static GRpcServer Instance { get; } = new();
    
    public const string Ip = "127.0.0.1";
    public const ushort Port = 3802;
    private WebApplication? _app;
    
    public override void _Ready()
    {
        base._Ready();

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddGrpc();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.Http2.KeepAlivePingDelay = TimeSpan.FromSeconds(30);
            options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
        });
        
        var app = builder.Build();
        app.MapGet("/", () => "Well you're certainly in an odd place, aren't you?");
        app.MapGrpcService<SpawnerService>();
        // app.MapGrpcService<GreeterService>();
        
        app.RunAsync("https://" + Ip + ":" + Port);
        _app = app;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        _app?.DisposeAsync();
    }
}