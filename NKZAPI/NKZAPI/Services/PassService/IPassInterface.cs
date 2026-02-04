using NKZAPI.Models;

namespace NKZAPI.Services.PassService
{
    public interface IPassInterface
    {
        void CreatePassHash(string Pass, out byte[] PassHash, out byte[] PassSalt);
        bool VerifyPassHash(string Pass, byte[] PassHash, byte[] PassSalt);
        string CreateToken(User user);
    }
}
