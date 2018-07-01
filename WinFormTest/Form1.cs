using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;

namespace WinFormTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private static void setBluetoothConnection()
        {
            try
            {
                BluetoothClient bluetoothClient;

                //BluetoothRadio brad = new BluetoothRadio();
                 //if (brad.BluetoothRadioMode == BluetoothRadioMode.Off)
                 //                  brad.BluetoothRadioMode = BluetoothRadioMode.On;
                 //           else
                 // brad.BluetoothRadioMode = BluetoothRadioMode.Off;

                if (BluetoothRadio.IsSupported == true)
                {
                    MessageBox.Show("Bluetooth Supported", "Information", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    BluetoothRadio radio = BluetoothRadio.PrimaryRadio;
                    MessageBox.Show(radio.Mode.ToString(), "Before Bluetooth Connection", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    radio.Mode = RadioMode.Discoverable;
                    // here radio.Mode works only if the Windows Device has Bluetooth enabled otherwise gives error
                    MessageBox.Show(radio.Mode.ToString(), "RadioMode Discover", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    bluetoothClient = new BluetoothClient();
                    //Cursor.Current = Cursors.WaitCursor;
                    BluetoothDeviceInfo[] bluetoothDeviceInfo = bluetoothClient.DiscoverDevices();
                    MessageBox.Show(bluetoothDeviceInfo.Length.ToString(), "Device Name", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                    foreach (BluetoothDeviceInfo device in bluetoothDeviceInfo)
                    {
                        Cursor.Current = Cursors.Default;
                        MessageBox.Show(device.DeviceName, "Device Name", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                        //bluetoothClient.Connect(new BluetoothEndPoint(device.DeviceAddress, service));
                        MessageBox.Show("Bluetooth Connected...", "Information", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                        break;
                    }
                }
                else
                {
                    MessageBox.Show("Bluetooth Not Supported", "Information", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                }
            }
            catch (Exception ex)
            {
                //log.Error("[Bluetooth] Connection failed", ex);
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
