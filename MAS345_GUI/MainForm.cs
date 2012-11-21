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
                    ConnectButton.Text = "Disconnect";
                    contCheckBox.Enabled = true;
                    startButton.Enabled = true;
                    portNumberSelector.Enabled = false;
                }
                else
                {
                    ConnectButton.Text = "Connect";
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
                    clearHistoryButton.Enabled = false;
                    groupBoxHistory.Enabled = false;
                    startButton.Text = "Stop";
                }
                else
                {
                    ConnectButton.Enabled = true;
                    contCheckBox.Enabled = true;
                    groupBoxDefaults.Enabled = true;
                    clearHistoryButton.Enabled = true;
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


        const int WorkerProgressOk = 1;
        const int WorkerProgressPortClosedError = 2;
        const int WorkerProgressOtherError = 3;

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

                    backgroundWorker1.ReportProgress(WorkerProgressOk, NewMeasure);
                }
                catch (TimeoutException)
                {
                    backgroundWorker1.ReportProgress(WorkerProgressOtherError, "Can't recieve data from " + serialPort1.PortName);
                }
                catch (InvalidDataException)
                {
                    backgroundWorker1.ReportProgress(WorkerProgressOtherError, "Value out of range");
                }
                catch (ArgumentOutOfRangeException)
                {
                    backgroundWorker1.ReportProgress(WorkerProgressOtherError, "Invalid data received");
                }
                catch (InvalidOperationException)
                {
                    backgroundWorker1.ReportProgress(WorkerProgressPortClosedError, "The port has been closed");
                }
            }
            while (Port.ContinousMode);
            
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == WorkerProgressOk)
            {
                int LastRow = 0;
                MeasureListItem NewMeasure = e.UserState as MeasureListItem;
                NewMeasure.ItemColor = commonColorDialog1.Color;
                NewMeasure.ItemComment = commentTextBox.Text;

                bigLabel.Text = NewMeasure.ScaledValueWithUnit;
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
                if (e.ProgressPercentage == WorkerProgressPortClosedError)
                {
                    Connected = false;
                }
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
            saveMeasureListDialog.ShowDialog();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openMeasureListDialog.ShowDialog();
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

            measureChart.Titles.Clear();
            measureChart.Titles.Add("Measurement: " + MeasureOnGraph[0].Type.ToString() + " [" + MeasureOnGraph[0].Unit + "]");
            
            Series series = measureChart.Series[0];
            series.Points.Clear();
            
            for (int i = 0; i < MeasureOnGraph.Count; i++)
            {
                series.Points.Add(MeasureOnGraph[i].Value);
                series.Points[series.Points.Count - 1].Color = MeasureOnGraph[i].ItemColor;
                series.Points[series.Points.Count - 1].BorderColor = Color.Black;

                string MeasureText = MeasureOnGraph[i].ScaledValueWithUnit;
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
            measureChart.Visible = true;
            exportGraphToImageToolStripMenuItem.Enabled = true;
            exportHistoryToExcelToolStripMenuItem.Enabled = true;
        }

        private void initGraph()
        {
            measureChart.Titles.Clear();
            measureChart.Series[0].Points.Clear();
            exportGraphToImageToolStripMenuItem.Enabled = false;
            exportHistoryToExcelToolStripMenuItem.Enabled = false;
            measureChart.Visible = false;
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

        private void exportGraphToImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            exportGraphDialog.ShowDialog();
        }

        private void saveMeasureListDialog_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                using (Stream stream = File.Open(saveMeasureListDialog.FileName, FileMode.Create))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.Serialize(stream, MeasureList);
                }
            }
            catch (IOException)
            {
                MessageBox.Show("Save error!");
            }
        }

        private void exportGraphDialog_FileOk(object sender, CancelEventArgs e)
        {
            string a = Path.GetExtension(exportGraphDialog.FileName);
            ChartImageFormat ImageFormat = ChartImageFormat.Png;
            switch (Path.GetExtension(exportGraphDialog.FileName))
            {
                case ".png":
                    ImageFormat = ChartImageFormat.Png;
                    break;
                case ".jpg":
                    ImageFormat = ChartImageFormat.Jpeg;
                    break;
                case ".bmp":
                    ImageFormat = ChartImageFormat.Bmp;
                    break;
                case ".gif":
                    ImageFormat = ChartImageFormat.Gif;
                    break;
            }
            measureChart.SaveImage(exportGraphDialog.FileName, ImageFormat);
        }

        private void openMeasureListDialog_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                using (Stream stream = File.Open(openMeasureListDialog.FileName, FileMode.Open))
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
                MessageBox.Show("Load error!");
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool Answer = AreYouSureBox.ShowDialog("Are You sure? All unsaved measure will be lost!");
            if (Answer)
            {
                mAS345dataBindingSource.Clear();
            }
        }

        private void exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            bool Answer = (MeasureList.Count == 0);
            if (!Answer)
            {
                Answer = AreYouSureBox.ShowDialog("Are You sure? All unsaved measure will be lost!");
            }
            if (Answer)
            {
                if (serialPort1.IsOpen)
                {
                    serialPort1.Close();
                }
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void clearHistoryButton_Click(object sender, EventArgs e)
        {
            mAS345dataBindingSource.Clear();
        }

        private void exportHistoryToExcelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // creating Excel Application
            Microsoft.Office.Interop.Excel._Application app = new Microsoft.Office.Interop.Excel.Application();
            app.ScreenUpdating = false;
            // creating new WorkBook within Excel application
            Microsoft.Office.Interop.Excel._Workbook workbook = app.Workbooks.Add();
            // creating new Excelsheet in workbook
            Microsoft.Office.Interop.Excel._Worksheet worksheet = null;
            // see the excel sheet behind the program
            app.Visible = true;
            // get the reference of first sheet. By default its name is Sheet1.
            // store its reference to worksheet

            worksheet = workbook.Sheets["Sheet1"];
            worksheet = workbook.ActiveSheet;
            // changing the name of active sheet
            worksheet.Name = "Exported from MAS345 GUI";
            // storing header part in Excel
            for (int i = 1; i < dataGridView1.Columns.Count - 1; i++)
            {
                worksheet.Cells[1, i] = dataGridView1.Columns[i - 1].HeaderText;
                worksheet.Cells[1, i].Borders.Color = ColorTranslator.ToOle(Color.Black);
                worksheet.Cells[1, i].Font.Bold = true;
            }
            // storing Each row and column value to excel sheet
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                for (int j = 0; j < dataGridView1.Columns.Count - 2; j++)
                {
                    worksheet.Cells[i + 2, j + 1] = dataGridView1.Rows[i].Cells[j].Value.ToString();
                    worksheet.Cells[i + 2, j + 1].Interior.Color = ColorTranslator.ToOle((dataGridView1.Rows[i].DataBoundItem as MeasureListItem).ItemColor);
                    worksheet.Cells[i + 2, j + 1].Borders.Color = ColorTranslator.ToOle(Color.Black);
                }
            }
            // save the application
            //workbook.SaveAs("c:\\output.xls", System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlExclusive, System.Type.Missing, System.Type.Missing, System.Type.Missing, System.Type.Missing); 
            // Exit from the application
            worksheet.get_Range("A1", "D"+dataGridView1.RowCount+1).Columns.AutoFit();
            app.ScreenUpdating = true;
            //app.Quit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox Box = new AboutBox();
            Box.ShowDialog();
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
