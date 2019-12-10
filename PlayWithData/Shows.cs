using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlayWithData
{
    public partial class Shows : Form
    {
        Process p = new Process();

        public Shows()
        {
            InitializeComponent();

            p.MainLogic();

            //Load the Listbox
            this.listBox1.DataSource = p.interested.Keys.ToList();


            var res = p.alls.Where(item => item.Value.TrailingPE != 0 && item.Value.TrailingPE < 15).OrderBy(item => item.Value.TrailingPE);
            StringBuilder sb = new StringBuilder();
            sb.Append("Low PE Ratio \r\n");
            foreach (var item in res)
            {
                sb.Append(item.Key + ":" + item.Value.TrailingPE + "\r\n");
            }
            textBox3.Text = sb.ToString();

            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn("Symbol", typeof(string)));//0
            dt.Columns.Add(new DataColumn("Days", typeof(int)));//1
            dt.Columns.Add(new DataColumn("Profit", typeof(double)));//2
            dt.Columns.Add(new DataColumn("Change From Bought", typeof(string)));//3
            dt.Columns.Add(new DataColumn("Today", typeof(string)));//4
            dt.Columns.Add(new DataColumn("Yesterday", typeof(string)));//5
            dt.Columns.Add(new DataColumn("Yesterday-1", typeof(string)));//6
            dt.Columns.Add(new DataColumn("Yesterday-2", typeof(string)));//7
            dt.Columns.Add(new DataColumn("Yesterday-3", typeof(string)));//8
            dt.Columns.Add(new DataColumn("Yesterday-4", typeof(string)));//9
            dt.Columns.Add(new DataColumn("Yesterday-5", typeof(string)));//10


            foreach (var item in p.bought)
            {
                DataRow dr = dt.NewRow();
                dr[0] = item.Key;
                dr[1] = (DateTime.Now - item.Value.IBoughtDate).Days;
                dr[2] = item.Value.CurrentProfitAmount;
                string mark = item.Value.CurrentProfitPercentage > 0 ? "+" : "";
                dr[3] = string.Format("{0}{1}%", mark, item.Value.CurrentProfitPercentage);
                for (int i = 0; i < 7; i++)
                {
                    mark = item.Value.ProfitPercentageComparedToBefore[i] > 0 ? "+" : "";
                    dr[i + 4] = string.Format("{0}{1}%", mark, item.Value.ProfitPercentageComparedToBefore[i]);                    
                }
                dt.Rows.Add(dr);
            }
            dataGridView1.DataSource = dt;
        }

        private void listBox1_MouseClick(object sender, MouseEventArgs e)
        {
            var now = sender as ListBox;
            string symbol = now.SelectedItem.ToString();

            formsPlot1.plt.Clear();

            for (int i = 0; i < p.interested[symbol].xs.Count; i++)
            {
                if (i == 0 || i == 11)
                {
                    formsPlot1.plt.PlotScatter(p.interested[symbol].xs[i], p.interested[symbol].ys[i]);
                    formsPlot1.Render();
                    formsPlot1.Refresh();
                }
            }

            if (p.interested[symbol].Bought)
            {
                formsPlot1.plt.PlotPoint(365 - (DateTime.Now - p.interested[symbol].IBoughtDate).Days, p.interested[symbol].IBoughtPrice, Color.Red, 15);
            }

            formsPlot1.plt.AxisAuto();

            StringBuilder sb = new StringBuilder();
            sb.Append("过去三年\r\n年周期: ");
            sb.Append(p.interested[symbol].CyclePerYear);
            sb.Append(" 个 \r\n");

            sb.Append("年平均涨跌比: ");
            sb.Append(p.interested[symbol].CycleAvgPercentage);
            sb.Append(" %\r\n");

            sb.Append("周期天数: ");
            sb.Append(p.interested[symbol].DaysPerCycle);
            sb.Append(" 天 \r\n");

            sb.Append("上升周期数: ");
            sb.Append(p.interested[symbol].UpCycleCount);
            sb.Append(" 次 \r\n");

            sb.Append("上升周期比率: ");
            sb.Append(p.interested[symbol].UpCycleAvgPercentage);
            sb.Append(" %\r\n");

            sb.Append("下降周期数: ");
            sb.Append(p.interested[symbol].DownCycleCount);
            sb.Append(" 次 \r\n");

            sb.Append("下降周期比率: ");
            sb.Append(p.interested[symbol].DownCycleAvgPercentage);
            sb.Append(" %\r\n");

            sb.Append("所有价高于今的均涨幅: ");
            sb.Append(p.interested[symbol].HigherThanTodayPercentage);
            sb.Append(" %\r\n");

            sb.Append("所有价低于今的均跌幅: ");
            sb.Append(p.interested[symbol].LowerThanTodayPercentage);
            sb.Append(" %\r\n");

            sb.Append("市盈率: ");
            sb.Append(p.alls[symbol].TrailingPE);
            sb.Append(" \r\n");

            textBox1.Text = sb.ToString();
        }
    }
}
