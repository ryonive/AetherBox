namespace AetherBox.Helpers.Faloop.Model;

public record UserRefreshResponse(bool Success, string SessionId, string Token);
