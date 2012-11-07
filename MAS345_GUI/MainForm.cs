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
        public List<MeasureListItem> MeasureList;
        private bool Connected = false;

        public MainForm()
        {
            InitializeComponent();
            MeasureList = new List<MeasureListItem>();
            mAS345dataBindingSource.DataSource = MeasureList;
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            if (Connected)
            {
                if (backgroundWorker1.IsBusy) backgroundWorker1.CancelAsync();
                ConnectButton.Text = "Connect";
                Connected = false;
                startButton.Enabled = false;
            }
            else
            {
                if (!serialPort1.IsOpen)
                {
                    serialPort1.PortName = "COM" + numericUpDown1.Value;
                    try
                    {
                        serialPort1.Open();
                        ConnectButton.Text = "Disconnect";
                        Connected = true;
                        startButton.Enabled = true;
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
                int LastRow = 0;
                MeasureListItem NewMeasure = e.UserState as MeasureListItem;
                NewMeasure.ItemColor = colorDialog1.Color;
                NewMeasure.ItemComment = commentTextBox.Text;

                label1.Text = NewMeasure.Value + " " + NewMeasure.Type;
                label2.Text = NewMeasure.Type.ToString();
                LastRow = mAS345dataBindingSource.Add(NewMeasure);
                dataGridView1.Rows[LastRow].DefaultCellStyle.BackColor = NewMeasure.ItemColor;

                try
                {
                    dataGridView1.Rows[LastRow].DefaultCellStyle.SelectionBackColor = Color.FromArgb(NewMeasure.ItemColor.A,
                                                                                                     NewMeasure.ItemColor.R - 20,
                                                                                                     NewMeasure.ItemColor.G - 20,
                                                                                                     NewMeasure.ItemColor.B - 20);
                }
                catch
                {  }
                dataGridView1.ClearSelection();
                dataGridView1.Rows[LastRow].Selected = true;

                toolStripStatusLabel1.Text = "Connected to " + serialPort1.PortName;
            }
            else
            {
                toolStripStatusLabel1.Text = e.UserState as String;
            }
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

        private void colorSelectorPanel_Click(object sender, EventArgs e)
        {
            colorDialog1.ShowDialog();
            colorSelectorPanel.BackColor = colorDialog1.Color;
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 5)
            {
                mAS345dataBindingSource.RemoveCurrent();
            }
            else if (e.ColumnIndex == 6)
            {

            }
        }
    }
    public class MeasureListItem : MeasureUnit
    {
        public string ItemComment { get; set; }
        public Color ItemColor { get; set; }

        public MeasureListItem(MeasureUnit TheOther)
            : base(TheOther)
        {
            this.ItemComment = "";
            this.ItemColor = Color.White;
        }
    }
}
