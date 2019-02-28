using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using System.IO;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;
using static TrendLine;

namespace least_squares
{
    public partial class Form1 : Form
    {
        string fname = ""; // Filename to use.
        string fdir = @"c:\test\"; // Initial directory in which to look for files.
        string ffilter = "All files (*.*)|*.*|CSV files (*.csv)|*.csv"; // File filter.
        ToolTip tooltip = new ToolTip();
        Point clickPosition ;

        DataTable dataTable = new DataTable();

        public Form1()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
        }



        private void chart1_MouseClick(object sender, MouseEventArgs e)
        {
            var pos = e.Location;
            clickPosition = pos;
            var results = chart1.HitTest(pos.X, pos.Y, false,
                                         ChartElementType.PlottingArea);
            foreach (var result in results)
            {
                if (result.ChartElementType == ChartElementType.PlottingArea)
                {
                    var xVal = Math.Round(result.ChartArea.AxisX.PixelPositionToValue(pos.X),1);
                    var yVal = Math.Round(result.ChartArea.AxisY.PixelPositionToValue(pos.Y),1);
                    
                    this.dataTable.Rows.Add(xVal,yVal);
                }
            }

        }
        private void showButton_Click(object sender, EventArgs e)
        {
            // Request a file.
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "Import File";
            fdlg.InitialDirectory = fdir;
            fdlg.Filter = ffilter;
            fdlg.FilterIndex = 2;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                this.fname = fdlg.FileName;
            }
            fdlg.Dispose();

            // Clear series.
            chart1.Series.Clear();

            // Set up chart properties.
            chart1.ChartAreas[0].AxisX.LabelStyle.Format = "0";
            chart1.ChartAreas[0].AxisX.RoundAxisValues();

            // Set up series 1.
            var series1 = new System.Windows.Forms.DataVisualization.Charting.Series
            {
                Name = "Original Data",
                Color = System.Drawing.Color.Green,
                IsVisibleInLegend = false,
                IsXValueIndexed = true,
                ChartType = SeriesChartType.Point
            };
            this.chart1.Series.Add(series1);

            // Set up series 2.
            var series2 = new System.Windows.Forms.DataVisualization.Charting.Series
            {
                Name = "Best Fit",
                Color = System.Drawing.Color.Red,
                IsVisibleInLegend = false,
                IsXValueIndexed = true,
                ChartType = SeriesChartType.Line
            };
            this.chart1.Series.Add(series2);

            // Set up data grid.
            dataTable.Columns.Add("x");
            dataTable.Columns.Add("y");

            StreamReader streamReader = new StreamReader(this.fname);
            string[] dataLine = new string[File.ReadAllLines(this.fname).Length];
            double x, y;

            // Read first line.
            dataLine = streamReader.ReadLine().Split(',');
            x = Convert.ToDouble(dataLine[0]);
            y = Convert.ToDouble(dataLine[1]);
            dataTable.Rows.Add(x, y);

            // Read rest of lines.
            while (!streamReader.EndOfStream)
            {
                dataLine = streamReader.ReadLine().Split(',');
                x = Convert.ToDouble(dataLine[0]);
                y = Convert.ToDouble(dataLine[1]);
                dataTable.Rows.Add(x, y);
            }
            
            // Close file
            streamReader.Close();

            // Display data grid.
            dataGridView1.DataSource = dataTable;

            // Display chart.
            refreshChart(sender, e);

        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            refreshChart(sender, e);
        }

        private void refreshChart(object sender, EventArgs e)
        {
            double x, y;

            DataTable xy = new DataTable();
            xy = (DataTable)this.dataGridView1.DataSource;

            // Reset original data.
            chart1.Series["Original Data"].Points.Clear();
            chart1.Series["Original Data"].IsVisibleInLegend = true;
            chart1.Series["Original Data"].IsXValueIndexed = false;

            // Reset best fit.
            chart1.Series["Best Fit"].Points.Clear();
            chart1.Series["Best Fit"].IsVisibleInLegend = true;
            chart1.Series["Best Fit"].IsXValueIndexed = false;

            // Refresh from data grid.
            for (int rows = 0; rows < dataGridView1.Rows.Count - 1; rows++)
            {
                var xcell = dataGridView1.Rows[rows].Cells[0].Value;
                var ycell = dataGridView1.Rows[rows].Cells[1].Value;

                if (xcell != DBNull.Value && ycell != DBNull.Value)
                {
                    // Read next data pair.
                    x = Convert.ToDouble(xcell);
                    y = Convert.ToDouble(ycell);

                    // Add to chart.
                    chart1.Series["Original Data"].Points.AddXY(x, y);
                }

            }

            // Calculate trendline.
            LineEquation Leq = LeastSquares(xy);

            // Add data for series 2.
            for (int rows = 0; rows < dataGridView1.Rows.Count - 1; rows++)
            { 
                x = Convert.ToDouble(dataGridView1.Rows[rows].Cells[0].Value);
                chart1.Series["Best Fit"].Points.AddXY(x, Leq.slope * x + Leq.intercept);
            }

            // Display equation.
            equationBox2.Clear();
            if (Leq.intercept > 0)
            {
                equationBox2.AppendText("y = " + Math.Round(Leq.slope, 4).ToString() + "x + " + Math.Round(Leq.intercept, 4).ToString());
            }
            else
            {
                equationBox2.AppendText("y = " + Math.Round(Leq.slope, 4).ToString() + "x - " + Math.Round(-Leq.intercept, 4).ToString());
            }
            // Display chart.
            chart1.Invalidate();
        }

        private void exportButton_Click(object sender, EventArgs e)
        {
            // Find file to write to.
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "Export File";
            fdlg.InitialDirectory = fdir;
            fdlg.Filter = ffilter;
            fdlg.FilterIndex = 2;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                this.fname = fdlg.FileName;
            }
            fdlg.Dispose();

            // Create the CSV file to which grid data will be exported.
            StreamWriter sw = new StreamWriter(this.fname, false);

            // Loop through all the rows.
            foreach (DataGridViewRow dr in dataGridView1.Rows)
            {
                if (!dr.IsNewRow)
                {
                    List<string> columnData = new List<string>();
                    foreach (DataGridViewCell cell in dr.Cells)
                    {
                        columnData.Add(cell.Value.ToString());
                    }
                    sw.WriteLine(string.Join(",", columnData.ToArray()));
                }
            }

            sw.Close();
        }

    }
}
