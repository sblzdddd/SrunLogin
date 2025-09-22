using System.Text;
using System.Text.Json;
using System.Web;
using System.Security.Cryptography;
using SrunLogin.Records;
using SrunLogin.Constants;
using SrunLogin.Utilities;

namespace SrunLogin.Handlers;

public static class LoginHandler
{
    public static async Task Login(AccountInfoRecord account, string ip)
    {
        if (!await IsOnline())
        {
            Console.WriteLine("Attempting to login...");
            
            // Get challenge token
            var token = await GetChallengeToken(account.PortalUsername, ip);
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Failed to get challenge token");
                throw new Exception("Failed to get challenge token");
            }
            
            Console.WriteLine("Got challenge token, proceeding with login...");
            // var token = "db2839cba678a5af3833be9200a1fcccc584a8f18a031894f8f3a7f8730ada16";
            await PortalLoginAsync(account.PortalUsername, account.PortalPassword, ip, token);
        }
        else
        {
            Console.WriteLine("You are already online!");
        }
    }

    public static async Task<bool> IsOnline()
    {
        using var client = new HttpClient();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var ip = NetworkDetector.GetIpAddress();
        
        var response = await client.GetStringAsync(
            $"{ApiEndpoints.UserInfo}?callback={ApiEndpoints.CallbackPrefix}&ip={ip}&_={timestamp}");
        
        // Remove the callback wrapper
        var jsonStr = response.Substring(ApiEndpoints.CallbackPrefix.Length + 1);
        jsonStr = jsonStr.Substring(0, jsonStr.Length - 1);
        
        var result = JsonDocument.Parse(jsonStr);
        return result.RootElement.TryGetProperty("error", out var errorElement) && 
               errorElement.GetString() != "not_online_error";
    }

    private static async Task PortalLoginAsync(string username, string password, string ip, string token)
    {

        // Prepare login info
        var info = new
        {
            username,
            password,
            ip,
            acid = LoginConstants.AcId,
            enc_ver = LoginConstants.EncVersion
        };
        
        var encodedInfo = UserInfoEncoder.EncodeUserInfo(info, token);
        
        var md5Pass = GetMD5Hash(password + token);
        var shaStr = token + username;
        shaStr += token + md5Pass;
        shaStr += token + LoginConstants.AcId;
        shaStr += token + ip;
        shaStr += token + LoginConstants.N;
        shaStr += token + LoginConstants.Type;
        shaStr += token + encodedInfo;
        
        var checksum = GetSHA1Hash(shaStr);

        // Send login request
        using var client = new HttpClient();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var loginUrl = $"{ApiEndpoints.Portal}?callback={ApiEndpoints.CallbackPrefix}" +
                      $"&action=login&username={HttpUtility.UrlEncode(username)}" +
                      $"&password={{MD5}}{md5Pass}" +
                      $"&os=Windows+10&name=Windows&double_stack=0" +
                      $"&chksum={checksum}&info={HttpUtility.UrlEncode(encodedInfo)}" +
                      $"&ac_id={LoginConstants.AcId}&ip={ip}" +
                      $"&n={LoginConstants.N}&type={LoginConstants.Type}&_={timestamp}";

        var response = await client.GetStringAsync(loginUrl);
        
        // Remove the callback wrapper
        var jsonStr = response.Substring(ApiEndpoints.CallbackPrefix.Length + 1);
        jsonStr = jsonStr.Substring(0, jsonStr.Length - 1);
        
        using var result = JsonDocument.Parse(jsonStr);
        if (result.RootElement.TryGetProperty("error", out var errorElement) && 
            errorElement.GetString() != "ok")
        {
            Console.WriteLine($"Login failed: {result.RootElement}");
            throw new Exception($"Login failed: {result.RootElement}");
        }
        Console.WriteLine("Login successful!");
        
        // Persist credentials
        Registry.Registry.Account = new AccountInfoRecord(username, password);
        Registry.Registry.DumpAccountInfoToFile();
    }

    private static async Task<string> GetChallengeToken(string username, string ip)
    {
        using var client = new HttpClient();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        var response = await client.GetStringAsync(
            $"{ApiEndpoints.Challenge}?callback={ApiEndpoints.CallbackPrefix}" +
            $"&username={HttpUtility.UrlEncode(username)}&ip={ip}&_={timestamp}");
        
        // Remove the callback wrapper
        var jsonStr = response.Substring(ApiEndpoints.CallbackPrefix.Length + 1);
        jsonStr = jsonStr.Substring(0, jsonStr.Length - 1);
        
        using var result = JsonDocument.Parse(jsonStr);
        return result.RootElement.TryGetProperty("challenge", out var challengeElement) 
            ? challengeElement.GetString() ?? "" 
            : "";
    }


    private static string GetMD5Hash(string input)
    {
        using var md5 = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = md5.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    private static string GetSHA1Hash(string input)
    {
        using var sha1 = SHA1.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha1.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}