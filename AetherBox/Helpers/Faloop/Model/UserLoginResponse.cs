// AetherBox, Version=69.2.0.8, Culture=neutral, PublicKeyToken=null
// AetherBox.Helpers.Faloop.Model.UserLoginResponse
namespace AetherBox.Helpers.Faloop.Model;
public record UserLoginResponse(bool Success, string SessionId, string Token);
