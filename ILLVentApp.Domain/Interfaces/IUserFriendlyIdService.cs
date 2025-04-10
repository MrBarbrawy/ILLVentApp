namespace ILLVentApp.Domain.Interfaces
{
    public interface IUserFriendlyIdService
    {
        string GenerateUserId();
        bool ValidateUserId(string userId);
    }
} 