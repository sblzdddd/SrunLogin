namespace SrunLogin.Constants;

public static class Settings
{
    public static readonly string Ssid = new("Satori Chan Is Endearing".Where(char.IsUpper).ToArray());
    public static readonly byte[] Key = "satori is an angel and i mean it"u8.ToArray();
    public static readonly string Host = "10.0.0.9";
    public static readonly Dictionary<string, string> Headers = new() {
        {"Accept-Encoding", "gzip, deflate, br, zstd"},
        {"Accept", "text/html,application/xhtml+xml,application/xml;q=0.9," +
                    "image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7"},
        {"User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                       "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 " +
                       "Safari/537.36 Edg/128.0.0.0"},
        {"Referer", $"http://{Host}/srun_portal_pc?ac_id=4&theme=pro"},
        {"X-Requested-With", "XMLHttpRequest"},
        {"Connection", "keep-alive"},
        {"Host", Host}
    };
}

public static class ApiEndpoints
{
    public const string BaseUrl = "http://10.0.0.9";
    public const string UserInfo = $"{BaseUrl}/cgi-bin/rad_user_info";
    public const string Challenge = $"{BaseUrl}/cgi-bin/get_challenge";
    public const string Portal = $"{BaseUrl}/cgi-bin/srun_portal";
    public const string CallbackPrefix = "jQuery112404329083514341273_1758074503010";
}

public static class LoginConstants
{
    public const int Type = 1;
    public const int N = 200;
    public const string AcId = "4";
    public const string EncVersion = "srun_bx1";
}
