using Microsoft.VisualBasic.ApplicationServices;
using System.Diagnostics;
using System.Windows.Forms;
using WinFormsApp1.Properties;
using Vanara.PInvoke;

namespace WinFormsApp1
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        /// \
        /// 
        static GlobalLowLevelHooks.KeyboardHook keyhook = new GlobalLowLevelHooks.KeyboardHook();
        static GlobalLowLevelHooks.MouseHook mousehook = new GlobalLowLevelHooks.MouseHook();
        public static void Main2()
        {
            using ProcessModule module = Process.GetCurrentProcess().MainModule;
            var A = Kernel32.GetModuleHandle(module.ModuleName);
            keyhook.KeyDown += Keyhook_KeyDown;
            keyhook.KeyUp += Keyhook_KeyUp;
            mousehook.MouseWheel += Mousehook_MouseWheel;
            keyhook.Install();
            mousehook.Install();

        }
        static List<GlobalLowLevelHooks.KeyboardHook.VKeys> vKeys = new();
        private static void Mousehook_MouseWheel(GlobalLowLevelHooks.MouseHook.MSLLHOOKSTRUCT mouseStruct)
        {
            if (!(vKeys.Exists(_ => _ == GlobalLowLevelHooks.KeyboardHook.VKeys.LWIN) && vKeys.Exists(_ => _ == GlobalLowLevelHooks.KeyboardHook.VKeys.LMENU)))
                return;
            bool isShift = vKeys.Exists(_ => _ == GlobalLowLevelHooks.KeyboardHook.VKeys.LSHIFT);
            if (!((isShift && vKeys.Count == 3) || vKeys.Count == 2))
                return;
            bool isUp = mouseStruct.mouseData == 0x780000;
            var h = User32.GetForegroundWindow();
            User32.SetWindowLong(h, User32.WindowLongFlags.GWL_EXSTYLE, new IntPtr(User32.GetWindowLong(h, User32.WindowLongFlags.GWL_EXSTYLE) | (int)User32.WindowStylesEx.WS_EX_LAYERED));

            User32.GetLayeredWindowAttributes(h, out COLORREF pcrKey, out byte pbAlpha, out User32.LayeredWindowAttributes pdwFlags);
            if (pdwFlags != User32.LayeredWindowAttributes.LWA_ALPHA)
                pbAlpha = 255;
            var newalpha = pbAlpha + (isUp ? 1 : -1) * (isShift ? 10 : 1);
            newalpha = newalpha > 255 ? 255 : newalpha < 0 ? 1 : newalpha;
            Console.WriteLine($"{pcrKey} | {pbAlpha} | {newalpha} | {pdwFlags}");
            if (newalpha == 255)
                User32.SetLayeredWindowAttributes(h, COLORREF.Default, 0, 0);
            else
                User32.SetLayeredWindowAttributes(h, COLORREF.Default, (byte)newalpha, User32.LayeredWindowAttributes.LWA_ALPHA);
        }


        private static void Keyhook_KeyUp(GlobalLowLevelHooks.KeyboardHook.VKeys key)
        {
            if (vKeys.Exists(_ => _ == key))
                vKeys.Remove(key);
            if (key == GlobalLowLevelHooks.KeyboardHook.VKeys.LWIN || key == GlobalLowLevelHooks.KeyboardHook.VKeys.LSHIFT)
                vKeys.Clear();
        }

        private static void Keyhook_KeyDown(GlobalLowLevelHooks.KeyboardHook.VKeys key)
        {
            if (!vKeys.Exists(_ => _ == key))
                vKeys.Add(key);
        }
        [STAThread]
        static void Main()
        {
            var cms = new ContextMenuStrip();
            var tsb = new ToolStripButton("Exit");
            tsb.Click += Exit_Click;
            cms.Items.Add(tsb);
            var ni=    new NotifyIcon();
            ni.ContextMenuStrip = cms;
            ni.Icon = Resources.a;
            ni.Visible = true;
            Main2();

            Application.Run();
        }

        private static void Exit_Click(object? sender, EventArgs e)
        {
            keyhook.Uninstall();
            mousehook.Uninstall();
            Application.Exit();
        }
    }
}