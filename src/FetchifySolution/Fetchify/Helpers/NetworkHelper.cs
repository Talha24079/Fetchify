using System.Net.NetworkInformation;

namespace Fetchify.Helpers
{
    public static class NetworkHelper
    {
        public static bool IsInternetAvailable()
        {
            try
            {
                return NetworkInterface.GetIsNetworkAvailable();
            }
            catch
            {
                return false;
            }
        }
    }
}
