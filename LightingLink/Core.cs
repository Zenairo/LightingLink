using System.Linq;
using System.Threading.Tasks;
using RGB.NET.Core;
using RGB.NET.Devices.Asus;
using RGB.NET.Devices.Corsair;
using HidLibrary;
using System.Diagnostics;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System.Collections.Generic;
using System;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.ColorConverters.HSB;
using Q42.HueApi.NET;
using Q42.HueApi.Models.Bridge;

namespace LightingLink
{
    class Core
    {
        static bool running = false;

        static RGBSurface surface;

        static HidFastReadDevice corsairLNP;

        static AsusMainboardRGBDevice auraMb;
        static CorsairKeyboardRGBDevice corsairKeyboard;
        static CorsairMousepadRGBDevice corsairMousepad;
        static CorsairHeadsetStandRGBDevice corsairHeadsetStand;

        static Color backIOColor;
        static Color pchColor;
        static Color headerOneColor;
        static Color headerTwoColor;

        static void Main(string[] args)
        {
            Run();
        }

        static void Run()
        {

            corsairLNP = (HidFastReadDevice)new HidFastReadEnumerator().Enumerate(0x1B1C, 0x0C0B).FirstOrDefault();
            corsairLNP.OpenDevice(DeviceMode.Overlapped, DeviceMode.Overlapped, ShareMode.ShareRead | ShareMode.ShareWrite);

            surface = RGBSurface.Instance;
            surface.Exception += args => Debug.WriteLine(args.Exception.Message);
            surface.UpdateMode = UpdateMode.Continuous;
            surface.LoadDevices(AsusDeviceProvider.Instance);
            surface.LoadDevices(CorsairDeviceProvider.Instance);

            auraMb = surface.Devices.OfType<AsusMainboardRGBDevice>().First();
            corsairKeyboard = surface.Devices.OfType<CorsairKeyboardRGBDevice>().First();
            corsairMousepad = surface.Devices.OfType<CorsairMousepadRGBDevice>().First();
            corsairHeadsetStand = surface.Devices.OfType<CorsairHeadsetStandRGBDevice>().First();

            LightingNodeUtils.FirstTransaction(corsairLNP);

            IBridgeLocator locator = new SSDPBridgeLocator();
            var locateBridges = locator.LocateBridgesAsync(TimeSpan.FromSeconds(5));
            IEnumerable<LocatedBridge> bridgeIPs = locateBridges.Result;
            ILocalHueClient client = new LocalHueClient(bridgeIPs.First().IpAddress);
            //var register = client.RegisterAsync("LightingLink", "WarMachine");
            //string appKey = register.Result;
            client.Initialize("NnmhRXVqLmBUw93kmIwi8PPCt6QHgWlHwkTYT9NC");

            GetAsusColors();

            running = true;


            Task[] updaters = new Task[6];
            updaters[0] = Task.Factory.StartNew(() => GetAsusColors());
            updaters[1] = Task.Factory.StartNew(() => UpdateHue(client, backIOColor, headerTwoColor, pchColor, headerOneColor));
            updaters[2] = Task.Factory.StartNew(() => UpdateLNP(corsairLNP, backIOColor, headerTwoColor, pchColor, headerOneColor));
            updaters[3] = Task.Factory.StartNew(() => UpdateKeyboard(corsairKeyboard, backIOColor, pchColor, headerOneColor, headerTwoColor));
            updaters[4] = Task.Factory.StartNew(() => UpdateMousepad(corsairMousepad, backIOColor, headerOneColor, pchColor, headerTwoColor));
            updaters[5] = Task.Factory.StartNew(() => UpdateHeadsetStand(corsairHeadsetStand, backIOColor, headerOneColor, pchColor, headerTwoColor));

            while (running)
            {
                Console.WriteLine("Press ESC to stop");
                do
                {
                    while (!Console.KeyAvailable)
                    {
                        // Do something
                    }
                } while (Console.ReadKey(true).Key != ConsoleKey.Escape);

                Console.WriteLine("Stopped");
                running = false;
                Task.WaitAll(updaters);
            }
        }

        static void GetAsusColors()
        {
            do
            {
                byte[] boardColor = auraMb.GetColors();

                backIOColor = new Color(boardColor[0], boardColor[2], boardColor[1]);
                pchColor = new Color(boardColor[3], boardColor[5], boardColor[4]);
                headerOneColor = new Color(boardColor[6], boardColor[8], boardColor[7]);
                headerTwoColor = new Color(boardColor[9], boardColor[11], boardColor[10]);

                System.Threading.Thread.Sleep(10);

            } while (running);
        }

        static void UpdateHue(ILocalHueClient hue, Color c1, Color c2, Color c3, Color c4)
        {
            do
            {
                c1 = backIOColor;

                var com1 = new LightCommand();
                com1.TransitionTime = TimeSpan.FromMilliseconds(150);
                com1.SetColor(new RGBColor(c1.R, c1.G, c1.B));

                var send1 = hue.SendCommandAsync(com1, new List<string> { "1" });
                System.Threading.Thread.Sleep(65);

                c2 = headerTwoColor;

                var com2 = new LightCommand();
                com2.TransitionTime = TimeSpan.FromMilliseconds(150);
                com2.SetColor(new RGBColor(c2.R, c2.G, c2.B));

                var send2 = hue.SendCommandAsync(com2, new List<string> { "2" });
                System.Threading.Thread.Sleep(65);

                c3 = pchColor;

                var com3 = new LightCommand();
                com3.TransitionTime = TimeSpan.FromMilliseconds(150);
                com3.SetColor(new RGBColor(c3.R, c3.G, c3.B));

                var send3 = hue.SendCommandAsync(com3, new List<string> { "3" });
                System.Threading.Thread.Sleep(65);


            } while (running);
        }

        static void UpdateLNP(HidDevice lnp, Color c1, Color c2, Color c3, Color c4)
        {
            do
            {
                c1 = backIOColor;
                c2 = headerTwoColor;
                c3 = pchColor;
                c4 = headerOneColor;

                LightingNodeUtils.BeignUpdate(lnp);

                byte[][] stripInfo = LightingNodeUtils.UpdateFourStrips(c1, c2, c3, c4);

                for (int i = 0; i < stripInfo.Length; i++)
                {
                    lnp.Write(stripInfo[i]);
                }

                byte[][] fanInfo = LightingNodeUtils.UpdateSixFans(c1, c2, c3, c4);

                for (int i = 0; i < fanInfo.Length; i++)
                {
                    lnp.Write(fanInfo[i]);
                }

                LightingNodeUtils.SubmitUpdate(lnp);

                System.Threading.Thread.Sleep(5);

            } while (running);
        }

        static void UpdateKeyboard(CorsairKeyboardRGBDevice keyboard, Color c1, Color c2, Color c3, Color c4)
        {
            do
            {
                c1 = backIOColor;
                c2 = headerTwoColor;
                c3 = pchColor;
                c4 = headerOneColor;

                foreach (Led led in keyboard)
                {
                    if (led.LedRectangle.Location.X < 450 / 4)
                    {
                        led.Color = c1;
                    }
                    else if (led.LedRectangle.Location.X < (450 / 4) * 2)
                    {
                        led.Color = c2;
                    }
                    else if (led.LedRectangle.Location.X < (450 / 4) * 3)
                    {
                        led.Color = c3;
                    }
                    else
                    {
                        led.Color = c4;
                    }
                }

                System.Threading.Thread.Sleep(41);

            } while (running);
        }

    static void UpdateMousepad(CorsairMousepadRGBDevice mousepad, Color c1, Color c2, Color c3, Color c4)
        {
            do
            {
                c1 = backIOColor;
                c2 = headerTwoColor;
                c3 = pchColor;
                c4 = headerOneColor;

                for (int i = 0; i < mousepad.Count(); i++)
                {
                    if (i < 4)
                    {
                        mousepad.ElementAt(i).Color = c1;
                    }
                    else if (i < 7)
                    {
                        mousepad.ElementAt(i).Color = c2;
                    }
                    else if (7 < i && i < 11)
                    {
                        mousepad.ElementAt(i).Color = c3;
                    }
                    else if (10 < i && i < 15)
                    {
                        mousepad.ElementAt(i).Color = c4;
                    }
                    else
                    {
                        mousepad.ElementAt(i).Color = ColorUtils.colorMixer(c2, c3);
                    }
                }

                System.Threading.Thread.Sleep(41);

            } while (running);
        }

        static void UpdateHeadsetStand(CorsairHeadsetStandRGBDevice headsetStand, Color c1, Color c2, Color c3, Color c4)
        {
            do
            {
                c1 = backIOColor;
                c2 = headerTwoColor;
                c3 = pchColor;
                c4 = headerOneColor;

                headsetStand.ElementAt(0).Color = ColorUtils.colorMixer(ColorUtils.colorMixer(c1, c3), ColorUtils.colorMixer(c2, c4));
                headsetStand.ElementAt(1).Color = ColorUtils.colorMixer(c4, c1);
                headsetStand.ElementAt(2).Color = c4;
                headsetStand.ElementAt(3).Color = ColorUtils.colorMixer(c3, c4);
                headsetStand.ElementAt(4).Color = c3;
                headsetStand.ElementAt(5).Color = ColorUtils.colorMixer(c2, c3);
                headsetStand.ElementAt(6).Color = c2;
                headsetStand.ElementAt(7).Color = ColorUtils.colorMixer(c1, c2);
                headsetStand.ElementAt(8).Color = c1;

                System.Threading.Thread.Sleep(41);

            } while (running);
        }
    }
}
