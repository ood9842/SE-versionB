using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace versionB
{
    public partial class Form1 : Form
    {
        private Thread tesk;
        public Form1()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        //login button
        private void button2_Click(object sender, EventArgs e)
        {
            tesk = new Thread(changeOtherForm);
            this.Close();
            tesk.Start();
        }
        private void changeOtherForm()
        {
            Application.Run(new Form2());
        }

        //exit button
        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
