using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AetherBox.Helpers.Faloop;
using AetherBox.Helpers.Faloop.Model;
using SocketIOClient;
using SocketIOClient.Transport;

namespace AetherBox.Helpers.Faloop;

public class FaloopSocketIOClient : IDisposable
{
    private readonly SocketIO client;

    public event Action? OnConnected;

    public event Action<string>? OnDisconnected;

    public event Action<string>? OnError;

    public event Action<MobReportData>? OnMobReport;

    public event Action<SocketIOResponse>? OnMessage;

    public event Action<string, SocketIOResponse>? OnAny;

    public event Action<int>? OnReconnected;

    public event Action<Exception>? OnReconnectError;

    public event Action<int>? OnReconnectAttempt;

    public event Action? OnReconnectFailed;

    public event Action? OnPing;

    public event Action<TimeSpan>? OnPong;

    public FaloopSocketIOClient()
    {
        client = new SocketIO("https://comms.faloop.app/mobStatus", new SocketIOOptions
        {
            EIO = EngineIO.V4,
            Transport = TransportProtocol.Polling,
            ExtraHeaders = new Dictionary<string, string>
            {
                { "Accept", "*/*" },
                { "Accept-Language", "ja" },
                { "Referer", "https://faloop.app/" },
                { "Origin", "https://faloop.app" },
                { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36" }
            }
        });
        client.OnConnected += delegate
        {
            this.OnConnected?.Invoke();
            client.EmitAsync("ack");
        };
        client.OnDisconnected += delegate (object? _, string cause)
        {
            this.OnDisconnected?.Invoke(cause);
        };
        client.OnError += delegate (object? _, string error)
        {
            this.OnError?.Invoke(error);
        };
        client.On("message", HandleOnMessage);
        client.OnAny(HandleOnAny);
        client.OnReconnected += delegate (object? _, int count)
        {
            this.OnReconnected?.Invoke(count);
        };
        client.OnReconnectError += delegate (object? _, Exception exception)
        {
            this.OnReconnectError?.Invoke(exception);
        };
        client.OnReconnectAttempt += delegate (object? _, int count)
        {
            this.OnReconnectAttempt?.Invoke(count);
        };
        client.OnReconnectFailed += delegate
        {
            this.OnReconnectFailed?.Invoke();
        };
        client.OnPing += delegate
        {
            this.OnPing?.Invoke();
        };
        client.OnPong += delegate (object? _, TimeSpan span)
        {
            this.OnPong?.Invoke(span);
        };
    }

    private void HandleOnMessage(SocketIOResponse response)
    {
        for (int index = 0; index < response.Count; index++)
        {
            FaloopEventPayload payload;
            payload = response.GetValue(index).Deserialize<FaloopEventPayload>();
            if (payload != null && payload.Type == "mob" && payload.SubType == "report")
            {
                MobReportData data;
                data = payload.Data.Deserialize<MobReportData>();
                if (data != null)
                {
                    this.OnMobReport?.Invoke(data);
                }
            }
        }
        this.OnMessage?.Invoke(response);
    }

    private void HandleOnAny(string name, SocketIOResponse response)
    {
        this.OnAny?.Invoke(name, response);
    }

    public async Task Connect(FaloopSession session)
    {
        if (!session.IsLoggedIn)
        {
            throw new ApplicationException("session is not authenticated.");
        }
        if (client.Connected)
        {
            await client.DisconnectAsync();
        }
        client.Options.Auth = new Dictionary<string, string> { { "sessionid", session.SessionId } };
        await client.ConnectAsync();
    }

    public void Dispose()
    {
        client.DisconnectAsync();
        client.Dispose();
    }
}
