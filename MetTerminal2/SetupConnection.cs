using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace MetTerminal2
{
    public partial class SetupConnection : UserControl
    {
        public bool     isTCP;
        public string   IPAddress;
        public int      PortNum;
        public string   COMPort;
        public int      Baud;

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        public SetupConnection()
        {
            InitializeComponent();
        }

        public void GetCOMPorts()
        {
            comboBoxCOM.Items.Clear();

            string[] theSerialPortNames = System.IO.Ports.SerialPort.GetPortNames();

            foreach (String s in theSerialPortNames)
            {
                comboBoxCOM.Items.Add(s);
            }
        }

        private void SetupConnection_Load(object sender, EventArgs e)
        {
            isTCP       = true;
            IPAddress   = "192.168.5.250";
            PortNum     = 4001;
            COMPort     = "COM1";
            Baud        = 9600;

            GetCOMPorts();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            GetCOMPorts();
        }

        private void radioButtonTCP_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonTCP.Checked)
            {
                isTCP = true;
                panelSerial.Enabled = false;
                panelTCP.Enabled = true;
            }
            else
            {
                isTCP = false;
                panelSerial.Enabled = true;
                panelTCP.Enabled = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (isTCP)
            {
                IPAddress = textBoxIP.Text;
                PortNum = (int)numericUpDownPort.Value;
            }
            else
            {
                COMPort = comboBoxCOM.SelectedItem.ToString();
                Baud = (int)numericUpDownBaud.Value;
            }

            this.Visible = false;
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
    }
}
