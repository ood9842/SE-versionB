using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace versionB
{
    public partial class Form2 : Form
    {
        private List<Panel> panel_list;
        private int panel_index;
        public Form2()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;

            //panel control
            panel_list = new List<Panel>();
            panel_list.Add(Home);
            panel_list.Add(Database);
            panel_list.Add(Analyze);
            panel_list.Add(Setting);
            //strat home page first
            panel_list[0].BringToFront();
            label2.Text = "HOME";
            panel_index = 0; //no use
        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }

        //home button
        private void button1_Click(object sender, EventArgs e)
        {
            panel_list[0].BringToFront();
            label2.Text = "HOME";
        }

        //database button
        private void button2_Click(object sender, EventArgs e)
        {
            panel_list[1].BringToFront();
            label2.Text = "DATABASE";
        }

        //analyze button
        private void button3_Click(object sender, EventArgs e)
        {
            panel_list[2].BringToFront();
            label2.Text = "ANALYZE";
        }

        //setting button
        private void button5_Click(object sender, EventArgs e)
        {
            panel_list[3].BringToFront();
            label2.Text = "SETTING";
        }

        //exit button
        private void button4_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
