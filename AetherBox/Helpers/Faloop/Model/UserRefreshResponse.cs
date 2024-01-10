// AetherBox, Version=69.2.0.8, Culture=neutral, PublicKeyToken=null
// AetherBox.Helpers.Faloop.Model.UserRefreshResponse
namespace AetherBox.Helpers.Faloop.Model;
public record UserRefreshResponse(bool Success, string SessionId, string Token);
