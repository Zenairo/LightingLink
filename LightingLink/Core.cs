using System.Linq;
using System.Threading.Tasks;
using RGB.NET.Core;
using RGB.NET.Devices.Asus;
using RGB.NET.Devices.Corsair;
using HidLibrary;
using System.Diagnostics;

namespace LightingLink
{
    class Core
    {
        static void Main(string[] args)
        {
            run();
        }

        static void run()
        {
            HidFastReadEnumerator fastHid = new HidFastReadEnumerator();
            HidFastReadDevice corsairLNP = (HidFastReadDevice)fastHid.Enumerate(0x1B1C, 0x0C0B).FirstOrDefault();
            corsairLNP.OpenDevice(DeviceMode.Overlapped, DeviceMode.Overlapped, ShareMode.ShareRead | ShareMode.ShareWrite);

            RGBSurface surface = RGBSurface.Instance;
            surface.Exception += args => Debug.WriteLine(args.Exception.Message);
            surface.UpdateMode = UpdateMode.Continuous;
            surface.LoadDevices(AsusDeviceProvider.Instance);
            surface.LoadDevices(CorsairDeviceProvider.Instance);

            AsusMainboardRGBDevice auraMb = surface.Devices.OfType<AsusMainboardRGBDevice>().First();
            CorsairKeyboardRGBDevice corsairKeyboard = surface.Devices.OfType<CorsairKeyboardRGBDevice>().First();
            CorsairMousepadRGBDevice corsairMousepad = surface.Devices.OfType<CorsairMousepadRGBDevice>().First();
            CorsairHeadsetStandRGBDevice corsairHeadsetStand = surface.Devices.OfType<CorsairHeadsetStandRGBDevice>().First();

            LightingNodeUtils.firstTransaction(corsairLNP);

            while (true)
            {
                byte[] boardColor = auraMb.GetColors();

                Color backIOColor = new Color(boardColor[0], boardColor[2], boardColor[1]);
                Color pchColor = new Color(boardColor[3], boardColor[5], boardColor[4]);
                Color headerOneColor = new Color(boardColor[6], boardColor[8], boardColor[7]);
                Color headerTwoColor = new Color(boardColor[9], boardColor[11], boardColor[10]);

                Task[] setColors = new Task[4];
                setColors[0] = Task.Factory.StartNew(() => SetLNP(corsairLNP, backIOColor, headerTwoColor, pchColor, headerOneColor));
                setColors[1] = Task.Factory.StartNew(() => SetKeyboard(corsairKeyboard, backIOColor, pchColor, headerOneColor, headerTwoColor));
                setColors[2] = Task.Factory.StartNew(() => SetMousepad(corsairMousepad, backIOColor, headerOneColor, pchColor, headerTwoColor));
                setColors[3] = Task.Factory.StartNew(() => SetHeadsetStand(corsairHeadsetStand, backIOColor, headerOneColor, pchColor, headerTwoColor));

                Task.WaitAll(setColors);

                System.Threading.Thread.Sleep(41);
            }
        }

        static void SetLNP(HidDevice lnp, Color c1, Color c2, Color c3, Color c4)
        {
            LightingNodeUtils.beignTransaction(lnp);

            byte[][] stripInfo = LightingNodeUtils.fourStripsFromZones(c1, c2, c3, c4);

            for (int i = 0; i < stripInfo.Length; i++)
            {
                lnp.Write(stripInfo[i]);
            }

            byte[][] fanInfo = LightingNodeUtils.sixFansFromZones(c1, c2, c3, c4);

            for (int i = 0; i < fanInfo.Length; i++)
            {
                lnp.Write(fanInfo[i]);
            }

            LightingNodeUtils.submitTransaction(lnp);
        }

        static void SetKeyboard(CorsairKeyboardRGBDevice keyboard, Color c1, Color c2, Color c3, Color c4)
        {
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
        }

        static void SetMousepad(CorsairMousepadRGBDevice mousepad, Color c1, Color c2, Color c3, Color c4)
        {
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
        }

        static void SetHeadsetStand(CorsairHeadsetStandRGBDevice headsetStand, Color c1, Color c2, Color c3, Color c4)
        {
            headsetStand.ElementAt(0).Color = ColorUtils.colorMixer(ColorUtils.colorMixer(c1, c3), ColorUtils.colorMixer(c2, c4));
            headsetStand.ElementAt(1).Color = ColorUtils.colorMixer(c4, c1);
            headsetStand.ElementAt(2).Color = c4;
            headsetStand.ElementAt(3).Color = ColorUtils.colorMixer(c3, c4);
            headsetStand.ElementAt(4).Color = c3;
            headsetStand.ElementAt(5).Color = ColorUtils.colorMixer(c2, c3);
            headsetStand.ElementAt(6).Color = c2;
            headsetStand.ElementAt(7).Color = ColorUtils.colorMixer(c1, c2);
            headsetStand.ElementAt(8).Color = c1;
        }

    }
}
