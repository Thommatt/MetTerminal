using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using NLog;
using System.IO;
using System.Threading;
using System.Windows.Forms.Design;
using System.IO.Ports;
using System.Diagnostics;
using System.Reflection;

namespace MetTerminal2
{
    public partial class Form1 : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        public const int WM_VSCROLL = 0x0115;
        public const int SB_BOTTOM = 7;

        private StringBuilder sb = new StringBuilder();
        private static Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //public DateTime cTime = new DateTime();
        public TimeSpan cSpan = new TimeSpan();
        bool isLogging = true;

        bool CanSendNextCmd = false;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImportAttribute("user32.dll")]
        public static extern bool LockWindowUpdate(IntPtr hWndLock);

        string Path;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void menuStrip1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    if (textBoxSend.SelectedText.Length > 0)
                        textBoxSend.Text = textBoxSend.Text.Replace(textBoxSend.SelectedText, "");
                    textBoxSend.AppendText((char)27 + "");
                    break;
                case Keys.Enter:
                    Send();
                    textBoxSend.SelectAll();
                    break;
            }
            //if(
            //if (e.KeyCode == Keys.Enter)
            //{
            //    Send();
            //    textBox1.SelectAll();
            //}
        }

        private void SetConnected()
        {
            toolStripStatusLabel1.Text = "Status: Connected";
            if (!timer2.Enabled)
                timer2.Enabled = true;
        }

        private void SetDisconnected()
        {
            toolStripStatusLabel1.Text = "Status: Disconnected";
        }

        private void Send()
        {
            try
            {
                sb.Clear();
                sb.Append(textBoxSend.Text);//.ToUpper());
                sb.Append((char)13);

                if (setupConnection1.isTCP)
                {
                    if (!clientSocketControl1.IsConnected)
                    {
                        clientSocketControl1.Connect();
                        if (clientSocketControl1.IsConnected)
                        {
                            SetConnected();
                            
                        }
                    }

                    clientSocketControl1.WriteAsync(sb.ToString());
                }//tcp
                else
                {
                    if (!serialPort1.IsOpen)
                    {
                        serialPort1.Open();
                        if (serialPort1.IsOpen)
                        {
                            SetConnected();
                            if (!timer2.Enabled)
                                timer2.Enabled = true;
                        }
                    }

                    serialPort1.Write(sb.ToString());
                }//serial

                richTextBox1.AppendText(textBoxSend.Text.ToUpper() + "\n");
                //richTextBox1.AppendText(textBox1.Text + "\n");
                //if (checkBox1.Checked)
                //{
                //    //logger.Info(Environment.NewLine + textBox1.Text + Environment.NewLine);
                //    logger.Info(Environment.NewLine + textBoxSend.Text.ToUpper() + Environment.NewLine);
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        StringBuilder ssbb = new StringBuilder();
        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            //string currentLine = string.Empty;
            string Data = serialPort1.ReadExisting();
            logger.Info(Data);

            //foreach (char c in Data)
            //{
            //    currentLine += c;
            //}

            this.Invoke((MethodInvoker)delegate
            {
                textBox2.AppendText(Data);
                textBox2.SelectionStart = textBox2.Text.Length;
                SendMessage(textBox2.Handle, WM_VSCROLL, SB_BOTTOM, 0);
            });

            //Data.Replace("\n", ""); //remove new lines

            //this.Invoke((MethodInvoker)delegate
            //{
            //    LockWindowUpdate(textBox2.Handle);
            //});

            //foreach (char c in Data)
            //{
            //    if (c == '\r')
            //    {
            //        //sb.Append(Environment.NewLine);

            //        currentLine = ssbb.ToString() + Environment.NewLine;
            //        ssbb.Clear();

                    
            //        this.Invoke((MethodInvoker)delegate
            //        {
            //            LockWindowUpdate(IntPtr.Zero);
            //            textBox2.Text += currentLine;
            //            SendMessage(textBox2.Handle, WM_VSCROLL, SB_BOTTOM, 0);
                        
            //        });
            //    }
            //    else
            //    {
            //        ssbb.Append(c);
            //    }
            //}
        }

        private void clientSocketControl1_OnDataAvailable(object sender, WindowsFormsApplication1.SocketPortDataEventArgs e)
        {
            Byte[] Data = new Byte[e.Argument.ClientSocketCtrl.Available];
            e.Argument.ClientSocketCtrl.Read(Data);

            string DR = "";
            foreach (char c in Data)
            {
                DR += c;
            }

            this.Invoke((MethodInvoker)delegate
            {
                textBox2.AppendText(DR);// += DR;
                textBox2.SelectionStart = textBox2.Text.Length;
                SendMessage(textBox2.Handle, WM_VSCROLL, SB_BOTTOM, 0);
                //textBox2.ScrollToCaret();
                logger.Info(DR);
            });

            e.Argument.ClientSocketCtrl.ResetDataAvailableNotification();
        }

        private void textBox2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBox2.Clear();
            SerialPort sd = new SerialPort();
        }

        private void textBox1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (serialPort1.IsOpen)
                serialPort1.Close();

            if (clientSocketControl1.IsConnected)
                clientSocketControl1.Disconnect();
        }

        private void quitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen || clientSocketControl1.IsConnected)
                SetConnected();
            else
            {
                SetDisconnected();
                timer2.Enabled = false;
                cSpan = TimeSpan.Zero;
                //cTime = DateTime.Today;
                toolStripStatusLabel2.Text = "";
            }
        }

        private void connectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //connection1.Visible = true;
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            //richTextBox1.SelectionStart = richTextBox1.Text.Length;
            //richTextBox1.ScrollToCaret();
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (setupConnection1.isTCP)
                {
                    if (!clientSocketControl1.IsConnected)
                    {
                        clientSocketControl1.Connect();
                        SetConnected();
                    }
                }
                else
                {
                    if (!serialPort1.IsOpen)
                    {
                        serialPort1.Open();
                        SetConnected();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
                SetDisconnected();
                timer2.Enabled = false;
                //cTime = DateTime.Today;
                cSpan = TimeSpan.Zero;
                toolStripStatusLabel2.Text = "";
            }
            else if (clientSocketControl1.IsConnected)
            {
                clientSocketControl1.Disconnect();
                SetDisconnected();
                timer2.Enabled = false;
                //cTime = DateTime.Today;
                cSpan = TimeSpan.Zero;
                toolStripStatusLabel2.Text = "";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Assembly assem = Assembly.GetExecutingAssembly();
            AssemblyName assemName = assem.GetName();

            string versionInfo = string.Format(" - Version {0}.{1}.{2} ({3})", assemName.Version.Major, assemName.Version.Minor, assemName.Version.Build, assemName.Version.Revision);

            this.Text += versionInfo;

            //cTime = DateTime.Today;
            cSpan = TimeSpan.Zero;

            serialPort1.PortName = "COM1";
            serialPort1.BaudRate = 9600;

            //SetupConnection SC = new SetupConnection();
            //this.panel3.Controls.Add(SC);
            //SC.Location = new Point(15, 15);
            //SC.BringToFront();
        }

        void ChangeOpacity(object sender, EventArgs e)
        {
            this.Opacity += .10; //replace.10 with whatever you want
            if (this.Opacity == 1)
                timer3.Stop();
        }

        public void SetBaudRate(int BR)
        {
            if (serialPort1.IsOpen)
                serialPort1.Close();

            serialPort1.BaudRate = BR;
        }

        public void SetCOM(string s)
        {
            if (serialPort1.IsOpen)
                serialPort1.Close();

            serialPort1.PortName = s;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            //cTime = cTime.AddSeconds(1);
            TimeSpan addSec = TimeSpan.FromSeconds(1);
            cSpan = cSpan.Add(addSec);
            //toolStripStatusLabel2.Text = cTime.ToString("HH:mm:ss");
            toolStripStatusLabel2.Text = cSpan.ToString();
        }

        private void clockToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult DR;
            DR = MessageBox.Show("Are you sure you want to set the clock to your system time?", "Set Clock", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (DR == DialogResult.Yes)
            {
                if (setupConnection1.isTCP)
                {
                    if (!clientSocketControl1.IsConnected)
                        return;

                    DateTime dt;
                    dt = DateTime.Now;

                    richTextBox1.AppendText("DT " + dt.ToString("yyyy-MM-dd HH:mm:ss"));
                    clientSocketControl1.WriteAsync((char)27 + "DT " + dt.ToString("yyyy-MM-dd HH:mm:ss") + (char)13);

                    logger.Info(Environment.NewLine + "DT " + dt.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine);
                }
                else
                {
                    if (!serialPort1.IsOpen)
                        return;

                    DateTime dt;
                    dt = DateTime.Now;

                    richTextBox1.AppendText("DT " + dt.ToString("yyyy-MM-dd HH:mm:ss"));
                    serialPort1.Write((char)27 + "DT " + dt.ToString("yyyy-MM-dd HH:mm:ss") + (char)13);

                    logger.Info(Environment.NewLine + "DT " + dt.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine);
                }

            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (textBox2.TextLength == 8000)
                textBox2.Clear();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen || clientSocketControl1.IsConnected)
            {
                toolStripProgressBar1.Visible = true;
                backgroundWorker1.RunWorkerAsync();
            }
            else
            {
                MessageBox.Show("You must connect before you can send a file");
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string Line;

            OpenFileDialog file = new OpenFileDialog();
            file.Filter = "Text File (.txt)|*.txt";
            DialogResult dr = new DialogResult();

            this.Invoke((MethodInvoker)delegate{ dr = file.ShowDialog(); });

            if (dr == DialogResult.OK)
            {
                Path = file.FileName;

                if (!string.IsNullOrEmpty(Path))
                {
                    if (setupConnection1.isTCP)
                    {
                        if (clientSocketControl1.IsConnected)
                        {
                            using (var reader = File.OpenText(Path))
                            {
                                while ((Line = reader.ReadLine()) != null)
                                {
                                    clientSocketControl1.WriteAsync(Line + (char)13);//(char)27 + Line + (char)13);
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        richTextBox1.Text += Line + (char)13;
                                    });

                                    logger.Info(Environment.NewLine + Line + Environment.NewLine);
                                    Thread.Sleep(1000);
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("You must connect to the device first!");
                            return;
                        }
                    }
                    else
                    {
                        if (serialPort1.IsOpen)
                        {
                            using (var reader = File.OpenText(Path))
                            {
                                while ((Line = reader.ReadLine()) != null)
                                {
                                    serialPort1.WriteLine(Line + (char)13);//(char)27 + Line + (char)13);
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        richTextBox1.Text += Line + (char)13;
                                    });

                                    logger.Info(Environment.NewLine + Line + Environment.NewLine);
                                    Thread.Sleep(1000);
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("You must connect to the device first!");
                            return;
                        }
                    }
                }
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripProgressBar1.Visible = false;
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            textBox2.Clear();
        }

        private void setupConnection1_VisibleChanged(object sender, EventArgs e)
        {
            if (setupConnection1.Visible)
            {
                menuStrip1.Enabled = false;
                textBoxSend.Enabled = false;
            }
            else
            {
                menuStrip1.Enabled = true;
                textBoxSend.Enabled = true;

                if (setupConnection1.isTCP)
                {
                    clientSocketControl1.RemoteIpAddress = setupConnection1.IPAddress;
                    clientSocketControl1.IpPortNumber = setupConnection1.PortNum;
                }
                else
                {
                    SetCOM(setupConnection1.COMPort);
                    SetBaudRate(setupConnection1.Baud);
                }

                this.ActiveControl = textBoxSend;
            }
        }

        private void connectionToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            setupConnection1.Visible = true;
        }

        private void loggingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //isLogging = !isLogging;

            //if (isLogging)
            //{
            //    loggingToolStripMenuItem.Checked = true;
            //    LogManager.EnableLogging();
            //}
            //else
            //{
            //    loggingToolStripMenuItem.Checked = false;
            //    LogManager.DisableLogging();
            //}
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(Application.StartupPath);
        }

        private void topToolStripMenuItem_Click(object sender, EventArgs e)
        {
            panel1.Dock = DockStyle.Top;
        }

        private void bottomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            panel1.Dock = DockStyle.Bottom;
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            DialogResult DR;
            DR = MessageBox.Show("Are you sure you want capture the screen?", "Screen Capture", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (DR == DialogResult.Yes)
            {
                if (setupConnection1.isTCP)
                {
                    if (!clientSocketControl1.IsConnected)
                        return;

                    richTextBox1.AppendText("PW" + Environment.NewLine);
                    clientSocketControl1.WriteAsync((char)27 + "PW 17111" + (char)13);

                    richTextBox1.AppendText("SCN" + Environment.NewLine);
                    clientSocketControl1.WriteAsync((char)27 + "SCN" + (char)13);

                    //logger.Info(Environment.NewLine + "PW 17111" + Environment.NewLine);
                    //logger.Info(Environment.NewLine + "SCN" + Environment.NewLine);
                }
                else
                {
                    if (!serialPort1.IsOpen)
                        return;

                    richTextBox1.AppendText("PW" + Environment.NewLine);
                    serialPort1.Write((char)27 + "PW 17111" + (char)13);

                    richTextBox1.AppendText("SCN" + Environment.NewLine);
                    serialPort1.Write((char)27 + "SCN" + (char)13);

                    //logger.Info(Environment.NewLine + "DT " + dt.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine);
                }

            }
        }

        private void enableLoggingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            isLogging = !isLogging;

            if (isLogging)
            {
                enableLoggingToolStripMenuItem.Checked = true;
                LogManager.EnableLogging();
            }
            else
            {
                enableLoggingToolStripMenuItem.Checked = false;
                LogManager.DisableLogging();
            }
        }

        private void changeLoggingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormChangeLog fcl = new FormChangeLog();
            fcl.ShowDialog();
        }

        private void timerSendFile_Tick(object sender, EventArgs e)
        {

        }

        private void toolStripMenuItemWordWrap_Click(object sender, EventArgs e)
        {
            if (toolStripMenuItemWordWrap.Checked)
            {
                textBox2.WordWrap = true;
            }
            else
            {
                textBox2.WordWrap = false;
            }
        }
    }
}
