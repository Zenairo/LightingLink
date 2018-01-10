using System.Linq;
using System.Threading.Tasks;
using RGB.NET.Core;
using RGB.NET.Devices.Asus;
using RGB.NET.Devices.Corsair;
using HidSharp;
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

        static HidDevice corsairLNP;
        static HidStream lnpStream;

        static AsusMainboardRGBDevice auraMb;
        static CorsairKeyboardRGBDevice corsairKeyboard;
        static CorsairMousepadRGBDevice corsairMousepad;
        static CorsairHeadsetStandRGBDevice corsairHeadsetStand;

        static Color backIOColor;
        static Color pchColor;
        static Color headerOneColor;
        static Color headerTwoColor;

        static ILocalHueClient bridgeOne;

        static ILocalHueClient bridgeTwo;

        static void InitializeDevices()
        {
            corsairLNP = new HidDeviceLoader().GetDevices().Where(d => d.ProductID == 0x0C0B).First();
            lnpStream = corsairLNP.Open();
            LightingNodeUtils.FirstTransaction(lnpStream);

            surface = RGBSurface.Instance;
            surface.Exception += args => Debug.WriteLine(args.Exception.Message);
            surface.UpdateMode = UpdateMode.Continuous;
            surface.LoadDevices(AsusDeviceProvider.Instance, RGBDeviceType.Mainboard);
            surface.LoadDevices(CorsairDeviceProvider.Instance);

            auraMb = surface.Devices.OfType<AsusMainboardRGBDevice>().First();
            auraMb.UpdateMode = DeviceUpdateMode.SyncBack;
            corsairKeyboard = surface.Devices.OfType<CorsairKeyboardRGBDevice>().First();
            corsairMousepad = surface.Devices.OfType<CorsairMousepadRGBDevice>().First();
            corsairHeadsetStand = surface.Devices.OfType<CorsairHeadsetStandRGBDevice>().First();

            IBridgeLocator locator = new HttpBridgeLocator();
            var locateBridges = locator.LocateBridgesAsync(TimeSpan.FromSeconds(5));
            IEnumerable<LocatedBridge> bridgeIPs = locateBridges.Result;

            if (bridgeIPs.Where(B => B.BridgeId == "001788fffe678124").Count() > 0)
            {
                bridgeOne = new LocalHueClient(bridgeIPs.Where(B => B.BridgeId == "001788fffe678124").First().IpAddress);
                var registerOne = bridgeOne.RegisterAsync("LightingLink", "WarMachine");
                //string appKeyOne = registerOne.Result;
                bridgeOne.Initialize("NnmhRXVqLmBUw93kmIwi8PPCt6QHgWlHwkTYT9NC");
            }

            if (bridgeIPs.Where(B => B.BridgeId == "001788fffea04d9c").Count() > 0)
            {
                bridgeTwo = new LocalHueClient(bridgeIPs.Where(B => B.BridgeId == "001788fffea04d9c").First().IpAddress);
                var registerTwo = bridgeTwo.RegisterAsync("LightingLink", "WarMachine");
                //string appKeyTwo = registerTwo.Result;
                bridgeTwo.Initialize("2b0AIky9S2g1LgbggOgsCdNV8EzE2JS8QfBOCHHv");
            }

            GetAsusColors();
        }

        static void Main(string[] args)
        {
            Run();
        }

        static void Run()
        {

            InitializeDevices();

            running = true;


            List<Task> updaters = new List<Task>();

            updaters.Add(Task.Factory.StartNew(() => GetAsusColors()));

            if(bridgeOne != null)
            {
                updaters.Add(Task.Factory.StartNew(() => UpdateHue(bridgeOne, backIOColor, headerTwoColor, pchColor, headerOneColor)));
            }

            if (bridgeTwo != null)
            {
                updaters.Add(Task.Factory.StartNew(() => UpdateHue(bridgeTwo, backIOColor, headerTwoColor, pchColor, headerOneColor)));
            }

            updaters.Add(Task.Factory.StartNew(() => UpdateLNP(lnpStream, backIOColor, headerTwoColor, pchColor, headerOneColor)));
            updaters.Add(Task.Factory.StartNew(() => UpdateKeyboard(corsairKeyboard, backIOColor, pchColor, headerOneColor, headerTwoColor)));
            updaters.Add(Task.Factory.StartNew(() => UpdateMousepad(corsairMousepad, backIOColor, headerOneColor, pchColor, headerTwoColor)));
            updaters.Add(Task.Factory.StartNew(() => UpdateHeadsetStand(corsairHeadsetStand, backIOColor, headerOneColor, pchColor, headerTwoColor)));

            while (running)
            {
                Console.WriteLine("Press ESC to stop");
                do
                {
                    while (!Console.KeyAvailable)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                } while (Console.ReadKey(true).Key != ConsoleKey.Escape);

                Console.WriteLine("Stopped");
                running = false;
                Task.WaitAll(updaters.ToArray());
            }
        }

        static void GetAsusColors()
        {
            do
            {
                Color[] boardColor = auraMb.Select(C => C.Color).ToArray();

                backIOColor = boardColor[0];
                pchColor = boardColor[1];
                headerOneColor = boardColor[2];
                headerTwoColor = boardColor[3];

                System.Threading.Thread.Sleep(15);

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
                System.Threading.Thread.Sleep(75);

                c2 = headerTwoColor;

                var com2 = new LightCommand();
                com2.TransitionTime = TimeSpan.FromMilliseconds(150);
                com2.SetColor(new RGBColor(c2.R, c2.G, c2.B));

                var send2 = hue.SendCommandAsync(com2, new List<string> { "2" });
                System.Threading.Thread.Sleep(75);

                c3 = pchColor;

                var com3 = new LightCommand();
                com3.TransitionTime = TimeSpan.FromMilliseconds(150);
                com3.SetColor(new RGBColor(c3.R, c3.G, c3.B));

                var send3 = hue.SendCommandAsync(com3, new List<string> { "3" });
                System.Threading.Thread.Sleep(75);

                c4 = headerOneColor;

                var com4 = new LightCommand();
                com4.TransitionTime = TimeSpan.FromMilliseconds(150);
                com4.SetColor(new RGBColor(c4.R, c4.G, c4.B));

                var send4 = hue.SendCommandAsync(com4, new List<string> { "4" });
                System.Threading.Thread.Sleep(75);


            } while (running);
        }

        static void UpdateLNP(HidStream lnp, Color c1, Color c2, Color c3, Color c4)
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

                System.Threading.Thread.Sleep(15);

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

                System.Threading.Thread.Sleep(15);

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

                    System.Threading.Thread.Sleep(15);

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

                System.Threading.Thread.Sleep(15);

            } while (running);
        }
    }
}
