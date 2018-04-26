using MockupV1;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace versionB
{
    public delegate void SendDataCallback(byte[] btArySendData);
    public partial class Form2 : Form
    {
        private List<Panel> panel_list;
        private int panel_index;

        private static ConnectDatabase databasecmd;
        private bool isStart = false; //check button
        private bool isReset = false; //check button
        private DataTable table; //table in dataGridview
        private SerialPort iSerialPort;
        private int m_nType = -1;
        private int countDB = 0;
        private string epcS;
        private string timeS;
        private string checkP;

        StringBuilder sb = new StringBuilder();

        //private ReaderSetting m_curSetting = new ReaderSetting();
        private byte btPacketType;
        private byte btDataLen;
        private byte btReadId;
        private byte btCmd;
        private byte[] btAryData;
        private byte btCheck;
        private byte[] btAryTranData;



        public SendDataCallback SendCallback;




        byte[] m_btAryBuffer = new byte[4096];

        int m_nLenth = 0;

        public Form2()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;

            databasecmd = new ConnectDatabase(); //connect db

            //table
            table = new DataTable();
            table.Columns.Add(new DataColumn("epc", typeof(string)));
            table.Columns.Add(new DataColumn("time", typeof(string)));
            table.Columns.Add(new DataColumn("ant", typeof(string)));
            dataGridView1.DataSource = table;
            var headers = dataGridView1.Columns.Cast<DataGridViewColumn>();
            sb.AppendLine(string.Join(",", headers.Select(column => "\"" + column.HeaderText + "\"").ToArray()) + ",\"" + "point" + "\"");


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


        public void updateDataGridView(string data, string time, int ant)
        {
            try
            {
                DataRow r;
                if (data.Equals(null) || data == "") return;
                if (isReset) return;
                //dataGridView1.Invoke(new Action(() =>
                //{
                //    r = table.NewRow();
                //    r["epc"] = data;
                //    r["time"] = time;
                //    r["ant"] = ant;
                //    table.Rows.InsertAt(r, 0);
                //}));

                r = table.NewRow();
                r["epc"] = data;
                r["time"] = time;
                r["ant"] = ant;
                table.Rows.InsertAt(r, 0); // test without hardware

                countDB++;
                //label30.Invoke(new Action(() => { label30.Text = "" + countDB; }));
                label30.Text = "" + countDB; // test without hardware
                sendLocal();
                sendFile();
                //sendCloud();
                //checkDB();
            }

            catch (InvalidOperationException exc)
            {
                MessageBox.Show(exc.ToString());
            }

            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }

        }

        private void sendFile() //send data to file .csv
        {
            var cells = dataGridView1.Rows[0].Cells.Cast<DataGridViewCell>();
            sb.AppendLine(string.Join(",", cells.Select(cell => "\"" + cell.Value + "\"").ToArray()) + ",\"" + checkP + "\"");
            try
            {
                File.WriteAllText("D:\\demo.csv", sb.ToString(), Encoding.UTF8);
            }
            catch (Exception e)
            {
                MessageBox.Show("File write error: " + e.Message);
            }
        }

        private void sendLocal() //send data to localdatabase
        {
            if (databasecmd.connection.State == ConnectionState.Closed)
            {
                databasecmd.connectDB();
            }
            try
            {
                int i = 0;
                string StrQuery = @"INSERT INTO checkpoint (epc,time,ant_id,point) VALUES ("
                 + "\"" + dataGridView1.Rows[i].Cells["epc"].Value + "\",\""
                    + dataGridView1.Rows[i].Cells["time"].Value + "\","
                    + dataGridView1.Rows[i].Cells["ant"].Value + ",\"" + checkP + "\"" + ");";
                databasecmd.cmd.CommandText = StrQuery;
                Console.WriteLine(StrQuery);
                databasecmd.cmd.ExecuteNonQuery();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (databasecmd.connection.State == ConnectionState.Open)
                {
                    databasecmd.connection.Close();
                }
            }
        }

        private void sendCloud() // send data to cloud
        {
            List<DataValue> data = new List<DataValue>();

            data.Add(new DataValue() { epc = "" + dataGridView1.Rows[0].Cells["epc"].Value, time = "" + dataGridView1.Rows[0].Cells["time"].Value, ant = "" + dataGridView1.Rows[0].Cells["ant"].Value, point = "" + checkP });



            string jsonString = data.ToJSON();

            Console.WriteLine(jsonString);


            var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://192.168.0.109/api/add.php");
            httpWebRequest.ContentType = "application/json; charset=utf-8";
            httpWebRequest.Method = "POST";
            httpWebRequest.Accept = "application/json; charset=utf-8";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(jsonString);
                streamWriter.Flush();
                streamWriter.Close();

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    Console.WriteLine(result);
                }
            }
        }

        private void checkDB() // compare data form cloud and localdatabase
        {
            Record dataValues = new Record();
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://192.168.0.109/api/get.php");
            httpWebRequest.ContentType = "application/json; charset=utf-8";
            httpWebRequest.Method = "GET";
            httpWebRequest.Accept = "application/json; charset=utf-8";

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                Console.WriteLine(result);
                dataValues = result.ToData();
            }

            if (databasecmd.connection.State == ConnectionState.Closed)
            {
                databasecmd.connectDB();

            }
            try
            {
                for (int i = 0; i < dataValues.records.Count; i++)
                {
                    string StrQuery = @"UPDATE checkpoint SET add_in_server = 1 WHERE epc = " + "\"" + dataValues.records[i].epc + "\"AND time = \"" + dataValues.records[i].time + "\";";
                    databasecmd.cmd.CommandText = StrQuery;
                    Console.WriteLine(StrQuery);
                    databasecmd.cmd.ExecuteNonQuery();
                }


            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (databasecmd.connection.State == ConnectionState.Open)
                {
                    databasecmd.connection.Close();
                }
            }

        }

        private void button6_Click(object sender, EventArgs e)
        {

            string time = string.Format("{0}:{1}:{2}:{3}", DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond);

            updateDataGridView("010131353513513513", time, 1);
        }
    }
}
