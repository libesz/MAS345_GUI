using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO.Ports;

namespace MAS345_GUI
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!serialPort1.IsOpen)
            {
                serialPort1.PortName = "COM" + numericUpDown1.Value;
                try
                {
                    serialPort1.Open();
                    button1.Enabled = false;
                    button2.Enabled = true;
                    backgroundWorker1.RunWorkerAsync(serialPort1);
                }
                catch
                {
                    toolStripStatusLabel1.Text = "Failed to open " + serialPort1.PortName;
                }
            }
            else
            {
                toolStripStatusLabel1.Text = serialPort1.PortName + " is already open";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            System.Text.Encoding enc = System.Text.Encoding.ASCII;
            byte[] input_data = new byte[14];
            String input_string = "";

            SerialPort port = e.Argument as SerialPort;
            while (true)
            {
                if (backgroundWorker1.CancellationPending) { e.Cancel = true; return; }
                try
                {
                    port.RtsEnable = false;
                    port.DtrEnable = true;
                    port.WriteLine("");
                    Thread.Sleep(1000);
                    port.Read(input_data, 0, 14);

                    input_string = enc.GetString(input_data);

                    backgroundWorker1.ReportProgress(1, input_string);
                }
                catch
                {
                    backgroundWorker1.ReportProgress(2, "Can't recieve data from " + serialPort1.PortName);
                }
            }
            
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 1)
            {
                String input_string = e.UserState as String;
                MAS345_data decodedData = new MAS345_data();

                decodedData.time = DateTime.Now.ToString();

                decodedData.unit = input_string.Substring(9, 4);

                if(input_string.Substring(0, 2).Equals("AC"))
                {
                    if (decodedData.unit.Substring(3, 1).Equals("A"))
                    {
                        decodedData.measureType = "AC Current";
                    }
                    else
                    {
                        decodedData.measureType = "AC Voltage";
                    }
                }
                else if (input_string.Substring(0, 2).Equals("DC"))
                {
                    if (decodedData.unit.Substring(3, 1).Equals("A"))
                    {
                        decodedData.measureType = "DC Current";
                    }
                    else
                    {
                        decodedData.measureType = "DC Voltage";
                    }
                }
                else if (input_string.Substring(0, 2).Equals("OH"))
                {
                    decodedData.measureType = "Resistance";
                }
                else if (input_string.Substring(0, 2).Equals("DI"))
                {
                    decodedData.measureType = "Diode";
                }
                else if (input_string.Substring(0, 2).Equals("TE"))
                {
                    decodedData.measureType = "Temperature";
                }
                else if (input_string.Substring(0, 2).Equals("CA"))
                {
                    decodedData.measureType = "Capacity";
                }
                else
                {
                    decodedData.measureType = "hFE";
                }

                if (input_string.Substring(3, 1).Equals("-"))
                {
                    decodedData.value = "-";
                }

                decodedData.unit = decodedData.unit.TrimStart();
                decodedData.value += input_string.Substring(4, 5);

                label1.Text = decodedData.value + " " + decodedData.unit;
                label2.Text = decodedData.measureType;
                mAS345dataBindingSource.Add(decodedData);
                toolStripStatusLabel1.Text = "Connected to " + serialPort1.PortName;
                dataGridView1.FirstDisplayedScrollingRowIndex = mAS345dataBindingSource.Count;
            }
            else
            {
                toolStripStatusLabel1.Text = e.UserState as String;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (backgroundWorker1.IsBusy) backgroundWorker1.CancelAsync();
            button1.Enabled = true;
            button2.Enabled = false;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if( serialPort1.IsOpen )
            {
                serialPort1.Close();
                toolStripStatusLabel1.Text = serialPort1.PortName + " closed";
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
            }
        }
    }
}

