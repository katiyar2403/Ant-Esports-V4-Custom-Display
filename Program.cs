using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using CyUSB;
using System.IO;

using Microsoft.Win32;

namespace AntEsportsV4Silent
{
    static class Program
    {
        private static CyHidDevice myHidDevice = null;
        private static NotifyIcon trayIcon;
        private static ContextMenuStrip menu;

        private static bool running = true;
        private static int displayMode = 5;
        private static bool useFahrenheit = false;
        private static int cycleInterval = 3;

        private static DateTime lastTempSwitch = DateTime.Now;
        private static bool showGpuInTempCycle = false;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            trayIcon = new NotifyIcon
            {
                Text = "Ant Esports V4 Display",
                Visible = true
            };

            // ==================== EMBEDDED ICON ====================
            try
            {
                var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AntEsportsV4Silent.app.ico");
                if (resourceStream != null)
                {
                    trayIcon.Icon = new System.Drawing.Icon(resourceStream);
                }
                else
                {
                    trayIcon.Icon = System.Drawing.SystemIcons.Shield; // fallback
                }
            }
            catch
            {
                trayIcon.Icon = System.Drawing.SystemIcons.Shield;
            }

            menu = new ContextMenuStrip();
            BuildTrayMenu();
            trayIcon.ContextMenuStrip = menu;

            USBDeviceList devices = new USBDeviceList(CyConst.DEVICES_HID);
            for (int i = 0; i < devices.Count; i++)
            {
                if (devices[i].VendorID == 0x5131 && devices[i].ProductID == 0x2007)
                {
                    myHidDevice = devices[i] as CyHidDevice;
                    break;
                }
            }

            if (myHidDevice == null)
            {
                MessageBox.Show("Cooler not found!\nRun as Administrator.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Thread worker = new Thread(UpdateLoop) { IsBackground = true };
            worker.Start();

            Application.Run();
        }

        // ==================== Rest of your code (unchanged) ====================
        private static void BuildTrayMenu()
        {
            menu.Items.Clear();
            var modeMenu = new ToolStripMenuItem("Display Mode");
            modeMenu.DropDownItems.Add(new ToolStripMenuItem("CPU Temperature Only", null, (s, e) => ChangeMode(0)));
            modeMenu.DropDownItems.Add(new ToolStripMenuItem("GPU Temperature Only", null, (s, e) => ChangeMode(1)));
            modeMenu.DropDownItems.Add(new ToolStripMenuItem("CPU Usage Only", null, (s, e) => ChangeMode(2)));
            modeMenu.DropDownItems.Add(new ToolStripMenuItem("GPU Usage Only", null, (s, e) => ChangeMode(3)));
            modeMenu.DropDownItems.Add(new ToolStripMenuItem("Cycle All (Temp + Usage)", null, (s, e) => ChangeMode(4)));
            modeMenu.DropDownItems.Add(new ToolStripMenuItem("Cycle CPU ↔ GPU Temp", null, (s, e) => ChangeMode(5)));
            menu.Items.Add(modeMenu);

            var cycleMenu = new ToolStripMenuItem("Cycle Interval");
            for (int i = 1; i <= 5; i++)
            {
                int captured = i;
                var item = new ToolStripMenuItem($"{i} second{(i > 1 ? "s" : "")}", null, (s, e) => { cycleInterval = captured; UpdateChecks(); });
                cycleMenu.DropDownItems.Add(item);
            }
            menu.Items.Add(cycleMenu);

            menu.Items.Add(new ToolStripSeparator());

            var unitMenu = new ToolStripMenuItem("Temperature Unit");
            unitMenu.DropDownItems.Add(new ToolStripMenuItem("Celsius (°C)", null, (s, e) => { useFahrenheit = false; UpdateChecks(); }));
            unitMenu.DropDownItems.Add(new ToolStripMenuItem("Fahrenheit (°F)", null, (s, e) => { useFahrenheit = true; UpdateChecks(); }));
            menu.Items.Add(unitMenu);

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Exit", null, (s, e) => { running = false; trayIcon.Visible = false; Application.Exit(); }));

            UpdateChecks();
        }

        private static void ChangeMode(int mode)
        {
            displayMode = mode;
            UpdateChecks();
        }

        private static void UpdateChecks()
        {
            if (menu.Items[0] is ToolStripMenuItem modeMenu)
                for (int i = 0; i < 6; i++)
                    if (modeMenu.DropDownItems[i] is ToolStripMenuItem m)
                        m.Checked = (i == displayMode);

            if (menu.Items[1] is ToolStripMenuItem cycleMenu)
                for (int i = 0; i < 5; i++)
                    if (cycleMenu.DropDownItems[i] is ToolStripMenuItem m)
                        m.Checked = (i + 1 == cycleInterval);

            if (menu.Items[3] is ToolStripMenuItem unitMenu)
            {
                bool enableUnit = (displayMode != 4);
                unitMenu.Enabled = enableUnit;

                ((ToolStripMenuItem)unitMenu.DropDownItems[0]).Checked = !useFahrenheit;
                ((ToolStripMenuItem)unitMenu.DropDownItems[1]).Checked = useFahrenheit;
            }
        }

        // ... (UpdateLoop, ReadValue, ConvertToInt, SendUsbData remain exactly the same as your last code)
        private static void UpdateLoop()
        {
            int[] SendValueArray = new int[61];
            while (running)
            {
                try
                {
                    int cpuUsage = ReadValue(0);
                    int cpuTempRaw = ReadValue(1);
                    int gpuTempRaw = ReadValue(2);
                    int gpuUsage = ReadValue(3);

                    int cpuTemp = cpuTempRaw;
                    int gpuTemp = gpuTempRaw;

                    if (useFahrenheit && (displayMode == 0 || displayMode == 1 || displayMode == 5))
                    {
                        cpuTemp = (int)Math.Round(cpuTempRaw * 9.0 / 5.0 + 32);
                        gpuTemp = (int)Math.Round(gpuTempRaw * 9.0 / 5.0 + 32);
                    }

                    int unit = (displayMode == 4) ? 0 : (useFahrenheit ? 1 : 0);

                    SendValueArray[0] = cpuTemp; SendValueArray[1] = 0; SendValueArray[2] = unit;
                    SendValueArray[3] = cpuUsage;
                    SendValueArray[10] = gpuTemp; SendValueArray[11] = 0; SendValueArray[12] = unit;
                    SendValueArray[13] = gpuUsage;
                    SendValueArray[30] = cpuUsage;

                    SendValueArray[22] = Convert.ToInt16(DateTime.Now.Year.ToString().Substring(0, 2));
                    SendValueArray[23] = Convert.ToInt16(DateTime.Now.Year.ToString().Substring(2, 2));
                    SendValueArray[24] = DateTime.Now.Month;
                    SendValueArray[25] = DateTime.Now.Day;
                    SendValueArray[26] = DateTime.Now.Hour;
                    SendValueArray[27] = DateTime.Now.Minute;
                    SendValueArray[28] = DateTime.Now.Second;
                    SendValueArray[29] = (int)DateTime.Now.DayOfWeek;

                    if (displayMode == 0) { SendValueArray[33] = 0; SendValueArray[34] = 0; SendValueArray[35] = 0; }
                    else if (displayMode == 1) { SendValueArray[33] = 0; SendValueArray[34] = 0; SendValueArray[35] = 1; }
                    else if (displayMode == 2) { SendValueArray[33] = 1; SendValueArray[34] = 0; SendValueArray[35] = 0; }
                    else if (displayMode == 3) { SendValueArray[33] = 1; SendValueArray[34] = 0; SendValueArray[35] = 1; }
                    else if (displayMode == 4)
                    {
                        SendValueArray[33] = 2;
                        SendValueArray[34] = cycleInterval;
                        SendValueArray[35] = 0;
                    }
                    else
                    {
                        if ((DateTime.Now - lastTempSwitch).TotalSeconds >= cycleInterval)
                        {
                            showGpuInTempCycle = !showGpuInTempCycle;
                            lastTempSwitch = DateTime.Now;
                        }
                        SendValueArray[33] = 0;
                        SendValueArray[34] = 0;
                        SendValueArray[35] = showGpuInTempCycle ? 1 : 0;
                    }

                    byte[] packet = new byte[64];
                    packet[0] = 0; packet[1] = 1; packet[2] = 2;
                    for (int i = 0; i < 60; i++)
                        packet[3 + i] = (byte)SendValueArray[i];

                    SendUsbData(packet);
                }
                catch { }

                Thread.Sleep(200);
            }
        }

        private static int ReadValue(int index)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\HWiNFO64\VSB"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("ValueRaw" + index) ?? key.GetValue("Value" + index);
                        if (val != null) return ConvertToInt(val);
                    }
                }
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\HWiNFO64\VSB"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("ValueRaw" + index) ?? key.GetValue("Value" + index);
                        if (val != null) return ConvertToInt(val);
                    }
                }
            }
            catch { }
            return 45;
        }

        private static int ConvertToInt(object val)
        {
            if (val is int i) return Math.Max(0, i);
            if (val is double d) return (int)Math.Max(0, Math.Round(d));
            if (val is float f) return (int)Math.Max(0, Math.Round(f));
            if (val is string s && double.TryParse(s, out double p))
                return (int)Math.Max(0, Math.Round(p));
            return 45;
        }

        private static bool SendUsbData(byte[] data)
        {
            try
            {
                if (myHidDevice == null) return false;
                myHidDevice.Outputs.DataBuf[0] = myHidDevice.Outputs.ID;
                for (int i = 1; i <= data.Length && i < myHidDevice.Outputs.DataBuf.Length; i++)
                    myHidDevice.Outputs.DataBuf[i] = data[i - 1];
                return myHidDevice.WriteOutput();
            }
            catch { return false; }
        }
    }
}