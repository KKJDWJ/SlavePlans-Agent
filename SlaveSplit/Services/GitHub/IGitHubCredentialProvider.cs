namespace SlaveSplit.Services.GitHub
{
    public interface IGitHubCredentialProvider { string GetToken(); bool HasToken(); bool SaveToken(string token); bool DeleteToken(); }
}
