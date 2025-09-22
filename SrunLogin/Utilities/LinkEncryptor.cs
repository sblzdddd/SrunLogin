using System.Security.Cryptography;
namespace SrunLogin.Utilities;
using Constants;

public abstract class LinkEncryptor
{
    public static string Encrypt(string plain)
    {
        using var aes = Aes.Create();
        aes.Key = Settings.Key;
        aes.IV = new byte[16];

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plain);
            }
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public static string Decrypt(string cipher)
    {
        using var aes = Aes.Create();
        aes.Key = Settings.Key;
        aes.IV = new byte[16];

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using var ms = new MemoryStream(Convert.FromBase64String(cipher));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}