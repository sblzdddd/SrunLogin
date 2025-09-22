using System.Text;
using System.Text.Json;

namespace SrunLogin.Utilities;

public static class UserInfoEncoder
{
    public static string EncodeUserInfo(object info, string token)
    {
        var infoStr = JsonSerializer.Serialize(info);
        var encodedBytes = XxteaEncode(infoStr, token);
        string.Concat(encodedBytes.Select(ch => (ch & 0xFF).ToString("x2")));
        var b64 = JqBase64Encoder.Encode(encodedBytes);
        return "{SRBX1}" + b64;
    }

    private static string XxteaEncode(string plainText, string key)
    {
        if (string.IsNullOrEmpty(plainText))
            return "";
        
        var v = StringToUInt32Array(plainText, includeLength: true);
        var k = StringToUInt32Array(key, includeLength: false);
        
        if (k.Length < 4)
        {
            Array.Resize(ref k, 4);
        }
        
        var n = v.Length - 1;
        if (n < 1) return "";
        
        uint z = v[n];
        uint y;
        const uint delta = 0x9E3779B9; // 0x86014019 | 0x183639a0
        var q = (uint)Math.Floor(6 + 52.0 / (n + 1));
        uint sum = 0;
        
        while (q-- > 0)
        {
            sum = (sum + delta) & 0xFFFFFFFF;
            var e = (sum >> 2) & 3;
            
            for (int p = 0; p < n; p++)
            {
                y = v[p + 1];
                var m = (z >> 5) ^ (y << 2);
                m += (y >> 3) ^ (z << 4) ^ (sum ^ y);
                m += k[(p & 3) ^ e] ^ z;
                z = v[p] = (v[p] + m) & 0xFFFFFFFF;
            }
            
            y = v[0];
            var m2 = (z >> 5) ^ (y << 2);
            m2 += (y >> 3) ^ (z << 4) ^ (sum ^ y);
            m2 += k[(n & 3) ^ e] ^ z;
            z = v[n] = (v[n] + m2) & 0xFFFFFFFF;
        }
        
        return UInt32ArrayToString(v, includeLength: false);
    }

    private static uint[] StringToUInt32Array(string input, bool includeLength)
    {
        if (string.IsNullOrEmpty(input))
        {
            return includeLength ? new uint[] { 0 } : Array.Empty<uint>();
        }
        
        var length = input.Length;
        var result = new List<uint>();
        
        for (int i = 0; i < length; i += 4)
        {
            uint value = 0;
            for (int j = 0; j < 4; j++)
            {
                if (i + j < length)
                {
                    value |= (uint)(input[i + j] & 0xFF) << (j * 8);
                }
            }
            result.Add(value);
        }
        
        if (includeLength)
        {
            result.Add((uint)length);
        }
        
        return result.ToArray();
    }

    private static string UInt32ArrayToString(uint[] data, bool includeLength)
    {
        var d = data.Length;
        var c = (d - 1) * 4;
        
        if (includeLength && d > 0)
        {
            var m = data[d - 1];
            if (m < c - 3 || m > c)
                return "";
            c = (int)m;
        }
        
        var result = new StringBuilder();
        var totalChars = includeLength ? c : d * 4;
        var charCount = 0;
        
        for (int i = 0; i < d && charCount < totalChars; i++)
        {
            var value = data[i];
            for (int j = 0; j < 4 && charCount < totalChars; j++)
            {
                result.Append((char)((value >> (j * 8)) & 0xFF));
                charCount++;
            }
        }
        
        return includeLength ? result.ToString().Substring(0, Math.Min(c, result.Length)) : result.ToString();
    }

}
