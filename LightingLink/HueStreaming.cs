using Q42.HueApi;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.ColorConverters.HSB;
using Q42.HueApi.Interfaces;
using Q42.HueApi.Models.Bridge;
using Q42.HueApi.Streaming;
using Q42.HueApi.Streaming.Effects;
using Q42.HueApi.Streaming.Extensions;
using Q42.HueApi.Streaming.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightingLink
{
    public class HueStreaming
    {
        public StreamingGroup entGroup;


        public async Task Start()
        {

            /*IBridgeLocator locator = new HttpBridgeLocator();
            var locateBridges = await locator.LocateBridgesAsync(TimeSpan.FromSeconds(5));
            IEnumerable<LocatedBridge> bridgeIPs = locateBridges;

            if (bridgeIPs.Count() == 0)
            {
                throw new Exception("No bridges found.");
            }*/

            string ip = "192.168.50.3";
            string appName = "LightingLink";
            string deviceName = "WarMachine";
            string key = "8pJuacIOcvBN9XA34hZWLSWTI0CQ9JYoif7SyEn9";
            string entertainmentKey = "87E3D4B1CC91E68B44F4393951F838DA";

            //--------Get a new key---------
            //ILocalHueClient baseClient = new LocalHueClient(ip);
            //var appKey = await baseClient.RegisterAsync(appName, deviceName);

            //--------Get a new key and entertainment key---------
            //var entKey = await LocalHueClient.RegisterAsync(ip, appName, deviceName, true);

            //Initialize Streaming Client
            StreamingHueClient client = new StreamingHueClient(ip, key, entertainmentKey);

            //var localClient = client.LocalHueClient;
            //await localClient.SendCommandAsync(new LightCommand().SetColor(new RGBColor(255, 255, 255)), new List<string> { "1" });

            //Get the Entertainment Groups
            var all = await client.LocalHueClient.GetBridgeAsync();
            var group = all.Groups.Where(x => x.Type == Q42.HueApi.Models.Groups.GroupType.Entertainment).FirstOrDefault();

            if (group == null)
                throw new Exception("No Entertainment Group found.");
            else
                Console.WriteLine($"Using Entertainment Group {group.Id}");

            //Create a streaming group
            entGroup = new StreamingGroup(group.Locations);

            //Connect to the streaming group
            await client.Connect(group.Id);

            //Start auto updating this entertainment group
            client.AutoUpdate(entGroup, 50);

            //Optional: calculated effects that are placed in the room
            client.AutoCalculateEffectUpdate(entGroup);

            //Optional: Check if streaming is currently active
            var bridgeInfo = await client.LocalHueClient.GetBridgeAsync();
            Console.WriteLine(bridgeInfo.IsStreamingActive ? "Streaming is active" : "Streaming is not active");

            //Order lights based on position in the room
            var orderedLeft = entGroup.GetLeft().OrderByDescending(x => x.LightLocation.Y).ThenBy(x => x.LightLocation.X);
            var orderedRight = entGroup.GetRight().OrderByDescending(x => x.LightLocation.Y).ThenByDescending(x => x.LightLocation.X);
            var allLightsOrdered = orderedLeft.Concat(orderedRight.Reverse()).ToArray();
            
            var allLightsReverse = allLightsOrdered.ToList();
            allLightsReverse.Reverse();
            
        }
    }
}
