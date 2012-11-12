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

        private Bitmap GraphBitmap;
        const int GraphGrids = 10;
        const int LeftMargin = 80;
        const int RightMargin = 20;
        const int TopMargin = 15;
        const int BottomMargin = 15;
        const int MeasurePointSize = 7;

        public MainForm()
        {
            InitializeComponent();
            MeasureList = new List<MeasureListItem>();
            mAS345dataBindingSource.DataSource = MeasureList;

            initGraph();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
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
                    //MessageBox.Show((MeasureList.Count - MeasureStarted).ToString());
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
            initGraph();
            Graphics gr = Graphics.FromImage(GraphBitmap);

            //gray chart background
            gr.FillRectangle(Brushes.LightGray, new Rectangle(LeftMargin, TopMargin, pictureBox1.Size.Width - LeftMargin - RightMargin, pictureBox1.Size.Height - TopMargin - BottomMargin));

            //left margin
            gr.DrawLine(Pens.Black, new Point(LeftMargin, TopMargin),
                                    new Point(LeftMargin, (pictureBox1.Size.Height - BottomMargin)));
            //right margin
            gr.DrawLine(Pens.Black, new Point(pictureBox1.Size.Width - RightMargin, TopMargin),
                                    new Point(pictureBox1.Size.Width - RightMargin, (pictureBox1.Size.Height - BottomMargin)));

            //grids
            for (int i = 0; i <= GraphGrids; i++)
            {
                using (Pen the_pen = new Pen(Color.Gray, 1))
                {
                    the_pen.DashStyle = DashStyle.Dash;
                    gr.DrawLine(the_pen, new Point(LeftMargin, TopMargin + ((pictureBox1.Size.Height) - BottomMargin - TopMargin) * i / GraphGrids),
                                         new Point(pictureBox1.Size.Width - RightMargin, TopMargin + ((pictureBox1.Size.Height) - BottomMargin - TopMargin) * i / GraphGrids));
                }
            }

            double GridMaxValue = MeasureOnGraph.Max(obj => obj.Value) * 1.2;
            if( GridMaxValue == 0 ) GridMaxValue += 0.1;
            double GridMinValue = MeasureOnGraph.Min(obj => obj.Value) - ( MeasureOnGraph.Max(obj => obj.Value) * 0.2);

            for (int i = 0; i <= GraphGrids; i++)
            {
                //grid values
                using (Font the_font = new Font("Courier New", 8, FontStyle.Bold, GraphicsUnit.Point))
                {
                    string tempText = (((GridMaxValue - GridMinValue)
                                      * (GraphGrids - i) / GraphGrids)
                                      + GridMinValue).ToString("0.####");
                    tempText +=  " " + MeasureOnGraph[0].Unit;

                    SizeF TextSize = gr.MeasureString(tempText.ToString(), the_font);
                    gr.DrawString(tempText.ToString(), the_font, Brushes.Blue,
                                    LeftMargin - TextSize.Width,
                                    (TopMargin + ((pictureBox1.Size.Height) - BottomMargin - TopMargin) * i / GraphGrids) - (TextSize.Height / 2));
                }
            }
            double tempValueDomain = (GridMaxValue - GridMinValue);

            int lastX = 0;
            int lastY = 0;
            for (int i = 0; i < MeasureOnGraph.Count; i++)
            {
                int tempX = (int)(LeftMargin + ((pictureBox1.Width - LeftMargin - RightMargin) * (i + 1) / (MeasureOnGraph.Count + 1)));
                int tempY = (int)(TopMargin + ((pictureBox1.Height - TopMargin - BottomMargin) * (GridMaxValue - MeasureOnGraph[i].Value) / tempValueDomain));
                if (lastX != 0 && lastY != 0)
                {
                    gr.DrawLine(Pens.Blue, new Point(lastX, lastY), new Point(tempX, tempY));
                }
                lastX = tempX;
                lastY = tempY;
            }
            for (int i = 0; i < MeasureOnGraph.Count; i++)
            {
                int tempX = (int)(LeftMargin + ((pictureBox1.Width - LeftMargin - RightMargin) * (i+1) / (MeasureOnGraph.Count+1))) - (MeasurePointSize/2);
                int tempY = (int)(TopMargin + ((pictureBox1.Height - TopMargin - BottomMargin) * (GridMaxValue - MeasureOnGraph[i].Value) / tempValueDomain)) - (MeasurePointSize / 2);

                using (SolidBrush MeasurePointBrush = new SolidBrush(MeasureOnGraph[i].ItemColor) )
                {
                    gr.FillEllipse(MeasurePointBrush, tempX, tempY, MeasurePointSize, MeasurePointSize);
                }
                
                gr.DrawEllipse(Pens.Black, tempX, tempY, 6, MeasurePointSize);

                using (Font the_font = new Font("Courier New", 8, FontStyle.Bold, GraphicsUnit.Point))
                {
                    string MeasureText = MeasureOnGraph[i].ValueWithUnit;
                    if (MeasureOnGraph[i].ItemComment.Length > 0)
                    {
                        MeasureText += " (" + MeasureOnGraph[i].ItemComment + ")";
                    }
                    gr.DrawString( MeasureText, the_font, Brushes.Blue, tempX + 5, tempY + 5);
                }
            }
            gr.Dispose();
            pictureBox1.Image = GraphBitmap;
        }

        private void initGraph()
        {
            GraphBitmap = new Bitmap(pictureBox1.Size.Width, pictureBox1.Size.Height);
            Graphics gr = Graphics.FromImage(GraphBitmap);
            //bright bg
            gr.Clear(this.BackColor);

            gr.Dispose();
            pictureBox1.Image = GraphBitmap;
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
            drawGraph();
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
