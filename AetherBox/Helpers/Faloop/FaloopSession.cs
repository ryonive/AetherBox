using System;
using System.Threading.Tasks;
using AetherBox.Helpers.Faloop;
using AetherBox.Helpers.Faloop.Model;
using Dalamud.Logging;
using ECommons.DalamudServices;

namespace AetherBox.Helpers.Faloop;

public class FaloopSession : IDisposable
{
	private readonly FaloopApiClient client = new FaloopApiClient();

	public readonly FaloopEmbedData EmbedData;

	public bool IsLoggedIn { get; private set; }

	public string? SessionId { get; private set; }

	public FaloopSession()
	{
		EmbedData = new FaloopEmbedData(client);
	}

	public async Task<bool> LoginAsync(string username, string password)
	{
		Logout();
		UserRefreshResponse initialSession;
		initialSession = await client.RefreshAsync();
		if ((object)initialSession == null || !initialSession.Success)
		{
			Svc.Log.Debug("LoginAsync: initialSession is not success");
			return false;
		}
		UserLoginResponse login;
		login = await client.LoginAsync(username, password, initialSession.SessionId, initialSession.Token);
		if ((object)login == null || !login.Success)
		{
			Svc.Log.Debug("LoginAsync: login is not success");
			return false;
		}
		try
		{
			await EmbedData.Initialize();
		}
		catch (Exception exception)
		{
			Svc.Log.Error(exception, "LoginAsync: EmbedData.Initialize failed");
			return false;
		}
		IsLoggedIn = true;
		SessionId = login.SessionId;
		return true;
	}

	private void Logout()
	{
		IsLoggedIn = false;
		SessionId = null;
	}

	public void Dispose()
	{
		client.Dispose();
	}
}
