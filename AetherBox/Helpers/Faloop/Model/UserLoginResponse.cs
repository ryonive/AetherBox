namespace AetherBox.Helpers.Faloop.Model;

public record UserLoginResponse(bool Success, string SessionId, string Token);
