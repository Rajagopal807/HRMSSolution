using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Web;

public static class IpHelper
{
    public static string GetClientIp(HttpRequest request)
    {
        // 1. Check X-Forwarded-For (proxy / load balancer)
        string ip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];

        if (!string.IsNullOrEmpty(ip))
        {
            // Can contain multiple IPs → take first
            ip = ip.Split(',').First().Trim();
            return ip;
        }

        // 2. Check real IP header
        ip = request.ServerVariables["REMOTE_ADDR"];

        if (ip.Equals("::1") || ip.Equals("127.0.0.1"))
        {
            ip = GetSystemIPAddress();
        }

            if (!string.IsNullOrEmpty(ip))
            return ip;

        // 3. Fallback
        return request.UserHostAddress;
    }

    private static string GetSystemIPAddress()
    {
        string HostIPAddress = string.Empty;
        try
        {
            string hostName = Dns.GetHostName(); // Retrive the name of host

            NetworkInterface[] intf = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface device in intf)
            {
                if (device.Name.Contains("Loopback"))
                {
                    continue;
                }
                foreach (UnicastIPAddressInformation iPAddress in device.GetIPProperties().UnicastAddresses)
                {

                    Match match = Regex.Match(iPAddress.Address.ToString(), @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
                    if (match.Success)
                    {
                        if (iPAddress.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) // Only IPv4
                        {
                            HostIPAddress = iPAddress.Address.ToString();
                            // break;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(HostIPAddress))
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) // Only IPv4
                    {
                        HostIPAddress = ip.ToString();
                        break;
                    }
                }
            }

            return HostIPAddress;
        }
        catch (Exception)
        {
            return "";
        }
    }
}
