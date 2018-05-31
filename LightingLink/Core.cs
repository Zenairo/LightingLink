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
using Q42.HueApi.Streaming;
using Q42.HueApi.Streaming.Effects;
using Q42.HueApi.Streaming.Extensions;
using Q42.HueApi.Streaming.Models;
using System.Threading;

namespace LightingLink
{
    class Core
    {
        static bool running = false;

        static RGBSurface surface;

        static HidDevice corsairLNP;
        static HidStream lnpStream;

        static OpenConfiguration exclusive;

        static AsusMainboardRGBDevice auraMb;
        static CorsairKeyboardRGBDevice corsairKeyboard;
        static CorsairMousepadRGBDevice corsairMousepad;
        static CorsairHeadsetStandRGBDevice corsairHeadsetStand;

        static HueStreaming hueStream;

        //static Color backIOColor;
        //static Color pchColor;
        //static Color headerOneColor;
        //static Color headerTwoColor;

        static Color[] colors;

        static void InitializeDevices()
        {

            exclusive = new OpenConfiguration();
            exclusive.SetOption(OpenOption.Exclusive, true);
            exclusive.SetOption(OpenOption.Interruptible, false);

            corsairLNP = DeviceList.Local.GetHidDevices().Where(d => d.ProductID == 0x0C0B).First();
            lnpStream = corsairLNP.Open(exclusive);
            LightingNodeUtils.FirstTransaction(lnpStream);

            surface = RGBSurface.Instance;
            surface.Exception += args => Debug.WriteLine(args.Exception.Message);
            surface.LoadDevices(AsusDeviceProvider.Instance, RGBDeviceType.Mainboard);
            surface.LoadDevices(CorsairDeviceProvider.Instance);

            auraMb = surface.Devices.OfType<AsusMainboardRGBDevice>().First();
            auraMb.UpdateMode = DeviceUpdateMode.SyncBack;
            corsairKeyboard = surface.Devices.OfType<CorsairKeyboardRGBDevice>().First();
            corsairMousepad = surface.Devices.OfType<CorsairMousepadRGBDevice>().First();
            corsairHeadsetStand = surface.Devices.OfType<CorsairHeadsetStandRGBDevice>().First();

            GetAsusColors();
        }

        public static async Task Main(string[] args)
        {
            hueStream = new HueStreaming();
            await hueStream.Start();

            Run();
        }

        static void Run()
        {

            InitializeDevices();

            running = true;

            List<Task> updaters = new List<Task>();

            updaters.Add(Task.Factory.StartNew(() => GetAsusColors()));

            updaters.Add(Task.Factory.StartNew(() => UpdateHue(hueStream.entGroup)));
            updaters.Add(Task.Factory.StartNew(() => UpdateLNP(lnpStream)));
            updaters.Add(Task.Factory.StartNew(() => UpdateKeyboard(corsairKeyboard)));
            updaters.Add(Task.Factory.StartNew(() => UpdateMousepad(corsairMousepad)));
            updaters.Add(Task.Factory.StartNew(() => UpdateHeadsetStand(corsairHeadsetStand)));

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
                surface.Update();
                auraMb.SyncBack();
                Color[] AScolors = auraMb.Select(C => C.Color).ToArray();

                colors = AScolors;
                //backIOColor = boardColor[0];
                //pchColor = boardColor[1];
                //headerOneColor = boardColor[2];
                //headerTwoColor = boardColor[3];

                System.Threading.Thread.Sleep(34);

            } while (running);
        }

        static void UpdateHue(StreamingGroup streamingLights)
        {
            do
            {
                var lights = streamingLights.OrderBy(x => new Guid());
                foreach (StreamingLight light in lights)
                {
                    int colInt = streamingLights.IndexOf(light) % 4;
                    light.SetState(new RGBColor(colors[colInt].R, colors[colInt].G, colors[colInt].B), 1);
                    Thread.Sleep(5);
                }
                //Thread.Sleep(50);
            } while (running);
        }

        static void UpdateLNP(HidStream lnp)
        {
            do
            {
                Color c1 = colors[0];
                Color c2 = colors[3];
                Color c3 = colors[1];
                Color c4 = colors[2];

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

                System.Threading.Thread.Sleep(34);

            } while (running);
        }

        static void UpdateKeyboard(CorsairKeyboardRGBDevice keyboard)
        {
            do
            {
                Color c1 = colors[0];
                Color c2 = colors[3];
                Color c3 = colors[1];
                Color c4 = colors[2];

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

                System.Threading.Thread.Sleep(34);

            } while (running);
        }

        static void UpdateMousepad(CorsairMousepadRGBDevice mousepad)
            {
                do
                {
                    Color c1 = colors[0];
                    Color c2 = colors[3];
                    Color c3 = colors[1];
                    Color c4 = colors[2];

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

                    System.Threading.Thread.Sleep(34);

                } while (running);
            }

        static void UpdateHeadsetStand(CorsairHeadsetStandRGBDevice headsetStand)
        {
            do
            {
                Color c1 = colors[0];
                Color c2 = colors[3];
                Color c3 = colors[1];
                Color c4 = colors[2];

                headsetStand.ElementAt(0).Color = ColorUtils.colorMixer(ColorUtils.colorMixer(c1, c3), ColorUtils.colorMixer(c2, c4));
                headsetStand.ElementAt(1).Color = ColorUtils.colorMixer(c4, c1);
                headsetStand.ElementAt(2).Color = c4;
                headsetStand.ElementAt(3).Color = ColorUtils.colorMixer(c3, c4);
                headsetStand.ElementAt(4).Color = c3;
                headsetStand.ElementAt(5).Color = ColorUtils.colorMixer(c2, c3);
                headsetStand.ElementAt(6).Color = c2;
                headsetStand.ElementAt(7).Color = ColorUtils.colorMixer(c1, c2);
                headsetStand.ElementAt(8).Color = c1;

                System.Threading.Thread.Sleep(34);

            } while (running);
        }

        static void UpdateHueLegacy(ILocalHueClient hue)
        {
            /*            IBridgeLocator locator = new HttpBridgeLocator();
            var locateBridges = locator.LocateBridgesAsync(TimeSpan.FromSeconds(5));
            IEnumerable<LocatedBridge> bridgeIPs = locateBridges.Result;

            if (bridgeIPs.Where(B => B.BridgeId == "001788fffe678124").Count() > 0)
            {
                bridge = new LocalHueClient(bridgeIPs.Where(B => B.BridgeId == "001788fffe678124").First().IpAddress);
                var registerOne = bridge.RegisterAsync("LightingLink", "WarMachine");
                //string appKeyOne = registerOne.Result;
                bridge.Initialize("NnmhRXVqLmBUw93kmIwi8PPCt6QHgWlHwkTYT9NC");
            }*/

            do
            {
                for (int lightNum = 1; lightNum <= 13; lightNum++)
                {
                    Color color = colors[lightNum % 4];

                    var com1 = new LightCommand();
                    com1.TransitionTime = TimeSpan.FromMilliseconds(150);
                    com1.SetColor(new RGBColor(color.R, color.G, color.B));

                    var send1 = hue.SendCommandAsync(com1, new List<string> { lightNum.ToString() });
                    System.Threading.Thread.Sleep(70);

                }

            } while (running);
        }

    }
}
