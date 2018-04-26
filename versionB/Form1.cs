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
using MockupV1;
using MySql.Data.MySqlClient;

namespace versionB
{
    public partial class Form1 : Form
    {
        private static ConnectDatabase databasecmd;
        private Thread tesk;
        public Form1()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            textBox2.PasswordChar = '*';

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

        private bool validate_login(string user, string pass)
        {
            databasecmd = new ConnectDatabase();
            databasecmd.connectDB();
            databasecmd.cmd.CommandText = "Select username , password from login where username=@user and password=@pass";
            databasecmd.cmd.Parameters.AddWithValue("@user", user);
            databasecmd.cmd.Parameters.AddWithValue("@pass", pass);
            databasecmd.cmd.Connection = databasecmd.connection;
            MySqlDataReader login = databasecmd.cmd.ExecuteReader();
            if (login.Read())
            {
                databasecmd.connection.Close();
                return true;
            }
            else
            {
                databasecmd.connection.Close();
                return false;
            }
        }

        //login button
        private void button2_Click(object sender, EventArgs e)
        {
            if (validate_login(textBox1.Text, textBox2.Text))
            {
                tesk = new Thread(changeOtherForm);
                this.Close();
                tesk.Start();
            }
            else
            {
                MessageBox.Show("invalid username or password");
            }
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
