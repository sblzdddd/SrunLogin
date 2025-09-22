namespace SrunLogin.Utilities;
using System.Net;
using ManagedNativeWifi;
using Constants;

public abstract class NetworkDetector
{

    public static string? GetIpAddress()
    {
        string hostName = Dns.GetHostName();
        IPHostEntry ipEntry = Dns.GetHostEntry(hostName);

        foreach (IPAddress ipAddress in ipEntry.AddressList)
        {
            if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                Console.WriteLine($"Local IP Address: {ipAddress.ToString()}");
                return ipAddress.ToString();
            }
        }
        return null;
    }

    public static bool IsConnectedToWiFi()
    {
        foreach (var interfaceId in NativeWifi.EnumerateInterfaces()
            .Where(x => x.State is InterfaceState.Connected)
            .Select(x => x.Id))
        {
            var (result, cc) = NativeWifi.GetCurrentConnection(interfaceId);
            if (result is ActionResult.Success)
            {
                Console.WriteLine($"Connected to WiFi: {cc.Ssid}");
                if (cc.Ssid.ToString() == Settings.Ssid)
                {
                    return true;
                }
            }
        }
        return false;
    }
}