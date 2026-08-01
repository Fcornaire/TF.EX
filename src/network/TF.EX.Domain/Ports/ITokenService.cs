namespace TF.EX.Domain.Ports
{
    public interface ITokenService
    {
        string Generate(string ip);
        string GetIp(string token);
    }
}
