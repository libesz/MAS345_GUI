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
using System.Drawing.Drawing2D;
using System.Windows.Forms.DataVisualization.Charting;

namespace MAS345_GUI
{
    public partial class MainForm : Form
    {
        private List<MeasureListItem> MeasureList;
        private List<MeasureListItem> MeasureOnGraph;

        private bool _Connected = false;
        private bool Connected
        {
            get
            {
                return _Connected;
            }
            set
            {
                if (value == true)
                {
                    contCheckBox.Enabled = true;
                    startButton.Enabled = true;
                    portNumberSelector.Enabled = false;
                }
                else
                {
                    contCheckBox.Enabled = false;
                    startButton.Enabled = false;
                    portNumberSelector.Enabled = true;
                }
                _Connected = value;
            }
        }

        private bool _Started = false;
        private bool Started
        {
            get
            {
                return _Started;
            }
            set
            {
                if (value == true)
                {
                    contCheckBox.Enabled = false;
                    ConnectButton.Enabled = false;
                    groupBoxDefaults.Enabled = false;
                    groupBoxHistory.Enabled = false;
                    startButton.Text = "Stop";
                }
                else
                {
                    ConnectButton.Enabled = true;
                    contCheckBox.Enabled = true;
                    groupBoxDefaults.Enabled = true;
                    groupBoxHistory.Enabled = true;
                    startButton.Text = "Start";
                }
                _Started = value;
            }
        }

        public MainForm()
        {
            InitializeComponent();
            MeasureList = new List<MeasureListItem>();
            mAS345dataBindingSource.DataSource = MeasureList;

            initGraph();
            lastMeasureTypeRB.Checked = true;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadSettings();
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
        }

        private void LoadSettings()
        {
            dateToolStripMenuItem.Checked = Properties.Settings.Default.GraphShowDate;
            timeToolStripMenuItem.Checked = Properties.Settings.Default.GraphShowTime;
            commentToolStripMenuItem.Checked = Properties.Settings.Default.GraphShowComment;
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            if (Connected)
            {
                ConnectButton.Text = "Connect";
                Connected = false;

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
                        serialPort1.Open();
                        ConnectButton.Text = "Disconnect";
                        Connected = true;
                        toolStripStatusLabel1.Text = "Connected to " + serialPort1.PortName;
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

                if (mAS345dataBindingSource.Count == 1) dataGridView1.ClearSelection();
                dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.RowCount - 1;
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
                drawGraph();
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
                if (backgroundWorker1.IsBusy) backgroundWorker1.CancelAsync();
            }
            else
            {
                Started = true;
                if (!backgroundWorker1.IsBusy) backgroundWorker1.RunWorkerAsync(serialPort1);
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
            updateGraph();
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            updateGraph();
        }

        private void lastMeasureTypeRB_CheckedChanged(object sender, EventArgs e)
        {
            updateGraph();
        }

        private void updateGraph()
        {
            if (MeasureList.Count > 0)
            {
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
                }
                else
                {
                    MeasureOnGraph = new List<MeasureListItem>();
                    List<DataGridViewRow> SelectedRows = new List<DataGridViewRow>();
                    foreach (DataGridViewRow Row in dataGridView1.SelectedRows)
                    {
                        SelectedRows.Add(Row);
                    }
                    SelectedRows.Sort(DataGridViewRowIndexCompare);
                    foreach (DataGridViewRow Row in SelectedRows)
                    {
                        MeasureOnGraph.Add((MeasureListItem)Row.DataBoundItem);
                    }
                }
                if (MeasureOnGraph.Count > 0)
                {
                    bool NotSameType = false;
                    MeasureOnGraph.ForEach(delegate(MeasureListItem Item) { if (MeasureOnGraph[0].Type != Item.Type) NotSameType = true; });
                    if (NotSameType)
                    {
                        toolStripStatusLabel1.Text = "Not all the selected measures are from the same type.";
                        initGraph();
                    }
                    else
                    {
                        toolStripStatusLabel1.Text = "";
                        drawGraph();
                    }
                }
                else
                {
                    toolStripStatusLabel1.Text = "Nothing to display on chart.";
                    initGraph();
                }
            }
            else
            {
                initGraph();
            }
        }

        private void drawGraph()
        {
            bool ShowComment = Properties.Settings.Default.GraphShowComment;
            bool ShowDate = Properties.Settings.Default.GraphShowDate;
            bool ShowTime = Properties.Settings.Default.GraphShowTime;

            chart1.Titles.Clear();
            chart1.Titles.Add(MeasureOnGraph[0].Type.ToString());
            
            Series series = chart1.Series[0];
            series.Points.Clear();
            
            for (int i = 0; i < MeasureOnGraph.Count; i++)
            {
                series.Points.Add(MeasureOnGraph[i].Value);
                series.Points[series.Points.Count - 1].Color = MeasureOnGraph[i].ItemColor;
                series.Points[series.Points.Count - 1].BorderColor = Color.Black;

                string MeasureText = MeasureOnGraph[i].ValueWithUnit;
                if (((ShowComment && (MeasureOnGraph[i].ItemComment.Length > 0)) || ShowTime || ShowDate))
                {
                    MeasureText += " (";
                    if (ShowComment && (MeasureOnGraph[i].ItemComment.Length > 0))
                    {
                        MeasureText += MeasureOnGraph[i].ItemComment;
                        if (ShowDate || ShowTime) MeasureText += ", ";
                    }
                    if (ShowDate)
                    {
                        MeasureText += MeasureOnGraph[i].Time.ToString("d");
                        if (ShowTime) MeasureText += " ";
                    }
                    if (ShowTime)
                    {
                        MeasureText += MeasureOnGraph[i].Time.ToString("T");
                    }
                    MeasureText += ")";
                }
                series.Points[series.Points.Count - 1].Label = MeasureText;
                series.Points[series.Points.Count - 1].ToolTip = MeasureOnGraph[i].Time.ToString("G"); ;
            }
            chart1.Visible = true;
        }

        private void initGraph()
        {
            chart1.Titles.Clear();
            chart1.Series[0].Points.Clear();
            chart1.Visible = false;
        }

        private static int DataGridViewRowIndexCompare(DataGridViewRow x, DataGridViewRow y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're
                    // equal. 
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y
                    // is greater. 
                    return -1;
                }
            }
            else
            {
                // If x is not null...
                //
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    // ...and y is not null, compare the 
                    // lengths of the two strings.
                    //
                    int retval = x.Index.CompareTo(y.Index);

                    if (retval != 0)
                    {
                        // If the strings are not of equal length,
                        // the longer string is greater.
                        //
                        return retval;
                    }
                    else
                    {
                        // If the strings are of equal length,
                        // sort them with ordinary string comparison.
                        //
                        return x.Index.CompareTo(y.Index);
                    }
                }
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            updateGraph();
        }

        private void SettingsChanged()
        {
            updateGraph();
            Properties.Settings.Default.Save();
        }

        private void commentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.GraphShowComment)
            {
                Properties.Settings.Default.GraphShowComment = false;
                commentToolStripMenuItem.Checked = false;
            }
            else
            {
                Properties.Settings.Default.GraphShowComment = true;
                commentToolStripMenuItem.Checked = true;
            }
            SettingsChanged();
        }

        private void dateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.GraphShowDate)
            {
                Properties.Settings.Default.GraphShowDate = false;
                dateToolStripMenuItem.Checked = false;
            }
            else
            {
                Properties.Settings.Default.GraphShowDate = true;
                dateToolStripMenuItem.Checked = true;
            }
            SettingsChanged();
        }

        private void timeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.GraphShowTime)
            {
                Properties.Settings.Default.GraphShowTime = false;
                timeToolStripMenuItem.Checked = false;
            }
            else
            {
                Properties.Settings.Default.GraphShowTime = true;
                timeToolStripMenuItem.Checked = true;
            }
            SettingsChanged();
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
