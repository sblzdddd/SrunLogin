namespace SrunLogin.Utilities;

public static class JqBase64Encoder
{
    private const string CustomAlphabet = "LVoJPiCN2R8G90yg+hmFHuacZ1OWMnrsSTXkYpUq/3dlbfKwv6xztjI7DeBE45QA";
    private const string PadChar = "=";

    public static string Encode(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new List<string>();
        int i;
        int b10;
        int imax = input.Length - (input.Length % 3);

        // Process groups of 3 characters
        for (i = 0; i < imax; i += 3)
        {
            b10 = (GetByte(input, i) << 16) |
                  (GetByte(input, i + 1) << 8) |
                  GetByte(input, i + 2);

            result.Add(CustomAlphabet[(b10 >> 18) & 63].ToString());
            result.Add(CustomAlphabet[(b10 >> 12) & 63].ToString());
            result.Add(CustomAlphabet[(b10 >> 6) & 63].ToString());
            result.Add(CustomAlphabet[b10 & 63].ToString());
        }

        // Handle remaining characters
        switch (input.Length - imax)
        {
            case 1:
                b10 = GetByte(input, i) << 16;
                result.Add(CustomAlphabet[(b10 >> 18) & 63] +
                          CustomAlphabet[(b10 >> 12) & 63].ToString() +
                          PadChar + PadChar);
                break;
            case 2:
                b10 = (GetByte(input, i) << 16) | (GetByte(input, i + 1) << 8);
                result.Add(CustomAlphabet[(b10 >> 18) & 63] +
                          CustomAlphabet[(b10 >> 12) & 63].ToString() +
                          CustomAlphabet[(b10 >> 6) & 63] + PadChar);
                break;
        }

        return string.Join("", result);
    }

    private static int GetByte(string s, int i)
    {
        if (i >= s.Length)
            return 0;
            
        int x = s[i];
        if (x > 255)
        {
            throw new InvalidOperationException("INVALID_CHARACTER_ERR: Character code exceeds 255");
        }
        return x;
    }
}
