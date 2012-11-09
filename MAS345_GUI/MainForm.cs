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
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace MAS345_GUI
{
    public partial class MainForm : Form
    {
        public List<MeasureListItem> MeasureList;

        private bool Connected = false;
        private bool Started = false;

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
                ConnectButton.Text = "Connect";
                Connected = false;

                contCheckBox.Enabled = false;
                startButton.Enabled = false;
                portNumberSelector.Enabled = true;

                if (serialPort1.IsOpen)
                {
                    serialPort1.Close();
                    toolStripStatusLabel1.Text = serialPort1.PortName + " closed";
                }
            }
            else
            {
                if (!serialPort1.IsOpen)
                {
                    serialPort1.PortName = "COM" + portNumberSelector.Value;
                    try
                    {
                        contCheckBox.Enabled = true;
                        startButton.Enabled = true;
                        portNumberSelector.Enabled = false;
                        serialPort1.Open();
                        ConnectButton.Text = "Disconnect";
                        Connected = true;
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
            do
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
            while (Port.ContinousMode);
            
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 1)
            {
                int LastRow = 0;
                MeasureListItem NewMeasure = e.UserState as MeasureListItem;
                NewMeasure.ItemColor = commonColorDialog1.Color;
                NewMeasure.ItemComment = commentTextBox.Text;

                bigLabel.Text = NewMeasure.ValueWithUnit;
                modeLabel.Text = NewMeasure.Type.ToString();
                LastRow = mAS345dataBindingSource.Add(NewMeasure);

                dataGridView1.Rows[LastRow].DefaultCellStyle.BackColor = NewMeasure.ItemColor;
                dataGridView1.Rows[LastRow].DefaultCellStyle.SelectionBackColor = GetBrighterColor(NewMeasure.ItemColor);
                dataGridView1.ClearSelection();
                dataGridView1.Rows[LastRow].Selected = true;

                toolStripStatusLabel1.Text = "Connected to " + serialPort1.PortName;
            }
            else
            {
                toolStripStatusLabel1.Text = e.UserState as String;
            }
        }

        private Color GetBrighterColor( Color OrigColor )
        {
            const int factor = 30;
            int R, G, B;

            R = (OrigColor.R > factor) ? (OrigColor.R - factor) : 0;
            G = (OrigColor.G > factor) ? (OrigColor.G - factor) : 0;
            B = (OrigColor.B > factor) ? (OrigColor.B - factor) : 0;
            try
            {
                return Color.FromArgb(OrigColor.A, R, G, B);
            }
            catch
            {
                return Color.LightGray;
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Started = false;
            HandleStartButton();
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
            commonColorDialog1.ShowDialog();
            colorSelectorPanel.BackColor = commonColorDialog1.Color;
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 4)
            {
                mAS345dataBindingSource.RemoveCurrent();
            }
            else if (e.ColumnIndex == 5)
            {
                gridColorDialog1.ShowDialog();
                dataGridView1.Rows[e.RowIndex].DefaultCellStyle.BackColor = gridColorDialog1.Color;
                dataGridView1.Rows[e.RowIndex].DefaultCellStyle.SelectionBackColor = GetBrighterColor(gridColorDialog1.Color);
                (dataGridView1.Rows[e.RowIndex].DataBoundItem as MeasureListItem).ItemColor = gridColorDialog1.Color;
            }
        }

        private void contCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            serialPort1.ContinousMode = contCheckBox.Checked;
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            if (Started)
            {
                Started = false;
                HandleStartButton();
                if (backgroundWorker1.IsBusy) backgroundWorker1.CancelAsync();
            }
            else
            {
                Started = true;
                HandleStartButton();
                if (!backgroundWorker1.IsBusy) backgroundWorker1.RunWorkerAsync(serialPort1);
            }
        }

        private void HandleStartButton()
        {
            if (Started)
            {
                contCheckBox.Enabled = false;
                ConnectButton.Enabled = false;
                startButton.Text = "Stop";
            }
            else
            {
                ConnectButton.Enabled = true;
                contCheckBox.Enabled = true;
                startButton.Text = "Start";
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using (Stream stream = File.Open("data.bin", FileMode.Create))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.Serialize(stream, MeasureList);
                }
            }
            catch (IOException)
            {
            }
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using (Stream stream = File.Open("data.bin", FileMode.Open))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    MeasureList = (List<MeasureListItem>)bin.Deserialize(stream);

                    mAS345dataBindingSource.DataSource = MeasureList;
                    foreach (DataGridViewRow ActualRow in dataGridView1.Rows)
                    {
                        ActualRow.DefaultCellStyle.BackColor = (ActualRow.DataBoundItem as MeasureListItem).ItemColor;
                        ActualRow.DefaultCellStyle.SelectionBackColor = GetBrighterColor(ActualRow.DefaultCellStyle.BackColor);
                    }
                }
            }
            catch (IOException)
            {
            }
        }

        private void mAS345dataBindingSource_ListChanged(object sender, ListChangedEventArgs e)
        {
            handleGraph();
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            handleGraph();
        }

        private void handleGraph()
        {
            if (MeasureList.Count > 0)
            {
                List<MeasureListItem> MeasureOnGraph;
                //determine the last measures in the same mode
                if (lastMeasureTypeRB.Checked)
                {
                    MeasureType LastType = MeasureList[MeasureList.Count - 1].Type;
                    int MeasureStarted = MeasureList.FindLastIndex(
                        delegate(MeasureListItem Item)
                        {
                            return (Item.Type != LastType);
                        }
                    );
                    MeasureStarted++;
                    MeasureOnGraph = MeasureList.GetRange(MeasureStarted, MeasureList.Count - MeasureStarted);
                    MeasureOnGraph.ForEach(delegate(MeasureListItem Item) { label3.Text += Item.ValueWithUnit + "\n"; });
                }
                else
                {
                    MeasureOnGraph = new List<MeasureListItem>();
                    foreach (DataGridViewRow Row in dataGridView1.SelectedRows)
                    {
                        MeasureOnGraph.Add((MeasureListItem)Row.DataBoundItem);
                    }
                }
                if (MeasureOnGraph.Count > 1)
                {
                    label1.Text = MeasureOnGraph.Max(obj => obj.Value).ToString();
                    label2.Text = MeasureOnGraph.Min(obj => obj.Value).ToString();
                    label3.Text = "";
                }
                else
                {
                    label1.Text = "Not enough selection";
                    label2.Text = "";
                    label3.Text = "";
                }
            }
        }
    }

    [Serializable()]
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
