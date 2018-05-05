using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Media;

namespace SSES_Program
{
    class Service
    {
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MONITORPOWER = 0xF170;

        public const int MONITOR_ON = -1;
        public const int MONITOR_OFF = 2;
        public const int MONITOR_STANBY = 1;

        public const int MOUSE_MOVE = 0x0001;

        public static SoundPlayer Player = new SoundPlayer();

        public static string drivepath = Environment.ExpandEnvironmentVariables("%SystemDrive%") + @"\HansCreative\nnv\SSES_Program";
        public static string fileName = @"\Alert.wav";

        [DllImport("user32.dll")]
        public static extern void mouse_event(Int32 dwFlags, Int32 dx, Int32 dy, Int32 dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern void SendMessage(int hWnd, int hMsg, int wParam, int lParam);

        public static void AlertSoundStart()
        {
            Player.SoundLocation = drivepath + fileName;
            Player.PlayLooping();
        }

        public static void AlertSoundStop()
        {
            Player.Stop();
        }
    }
}
