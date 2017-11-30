using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.ColorConverters.OriginalWithModel;
using Q42.HueApi.Models.Bridge;
using Q42.WinRT.Portable.Data;
using System.Linq;

namespace LightingLink
{
    class HueUtils
    {


        IBridgeLocator locator = new HttpBridgeLocator();
        LocalHueClient _hueClient;
        ILocalHueClient client;

        public HueUtils()
        {
            //LocateBridgeAction();
            ILocalHueClient client = new LocalHueClient("192.168.1.92");
            GetAppKey();

        }

        private async void LocateBridgeAction()
        {
            //IEnumerable<string> bridgeIPs = await locator.LocateBridgesAsync(TimeSpan.FromSeconds(5));

            client = new LocalHueClient("ip");

            var appKey = await client.RegisterAsync("mypersonalappname", "mydevicename");

        }

        private async void GetAppKey()
        {
            var appKey = await client.RegisterAsync("mypersonalappname", "mydevicename");
        }
    }
}
