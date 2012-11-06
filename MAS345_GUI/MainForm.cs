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
    public class MeasureListItem : MeasureUnit
    {
        public string ItemComment{ get; set; }
        public Color ItemColor { get; set; }

        public MeasureListItem(MeasureUnit TheOther)
            : base(TheOther)
        {
            this.ItemComment = "";
            this.ItemColor = Color.Aqua;
        }
    }

    public partial class MainForm : Form
    {
        private MasSerialPort serialPort1;
        public List<MeasureListItem> MeasureList;

        public MainForm()
        {
            InitializeComponent();
            serialPort1 = new MasSerialPort();
            MeasureList = new List<MeasureListItem>();
            mAS345dataBindingSource.DataSource = MeasureList;
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            if (!serialPort1.IsOpen)
            {
                serialPort1.PortName = "COM" + numericUpDown1.Value;
                try
                {
                    serialPort1.Open();
                    ConnectButton.Enabled = false;
                    DisconnectButton.Enabled = true;
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

        void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            MasSerialPort Port = e.Argument as MasSerialPort;
            MeasureListItem NewMeasure;
            while (true)
            {
                if (backgroundWorker1.CancellationPending) { e.Cancel = true; return; }
                try
                {
                    NewMeasure = new MeasureListItem(Port.ReadData());

                    backgroundWorker1.ReportProgress(1, NewMeasure);
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
                MeasureListItem NewMeasure = e.UserState as MeasureListItem;

                label1.Text = NewMeasure.Value + " " + NewMeasure.Type;
                label2.Text = NewMeasure.Type.ToString();
                mAS345dataBindingSource.Add(NewMeasure);
                toolStripStatusLabel1.Text = "Connected to " + serialPort1.PortName;
            }
            else
            {
                toolStripStatusLabel1.Text = e.UserState as String;
            }
        }

        private void DisconnectButton_Click(object sender, EventArgs e)
        {
            if (backgroundWorker1.IsBusy) backgroundWorker1.CancelAsync();
            ConnectButton.Enabled = true;
            DisconnectButton.Enabled = false;
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

