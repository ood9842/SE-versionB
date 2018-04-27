using MockupV1;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace versionB
{
    public delegate void SendDataCallback(byte[] btArySendData);
    public partial class Form2 : Form
    {
        private List<Panel> panel_list;

        private static ConnectDatabase databasecmd;
        private bool isStart = false; //check button
        private bool isReset = false; //check button
        private DataTable table; //table in dataGridview
        private SerialPort iSerialPort;
        private int countDB = 0;
        private string epcS;
        private string timeS;
        private string checkP;
        private bool checkServer = false;

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

        Timer t = new Timer();

       

        byte[] m_btAryBuffer = new byte[4096];

        int m_nLenth = 0;

       
        private int m_nType = -1;

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
            table.Columns.Add(new DataColumn("bib", typeof(string)));
            dataGridView1.DataSource = table;
            dataGridView1.Columns[0].Width = 300;
            dataGridView1.Columns[1].Width = 120;
            dataGridView1.Columns[2].Width = 100;
            dataGridView1.Columns[3].Width = dataGridView1.Width - dataGridView1.Columns[0].Width - dataGridView1.Columns[1].Width - dataGridView1.Columns[2].Width;

            var headers = dataGridView1.Columns.Cast<DataGridViewColumn>();
          
     //       dataGridView1.Columns[2].Width = 100;
       //     dataGridView1.Columns[3].Width = dataGridView1.Width - dataGridView1.Columns[0].Width - dataGridView1.Columns[1].Width - dataGridView1.Columns[2].Width;
          //  dataGridView1.DataSource = table;
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
            label43.Text = getMacAddress();

            initChart();
            t.Interval = 5000;
            t.Enabled = true;
            t.Tick += new System.EventHandler(OnTimerEvent);
        }

        public string getMacAddress()
        {
            String firstMacAddress = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Select(nic => nic.GetPhysicalAddress().ToString())
            .FirstOrDefault();
            return firstMacAddress;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            comboPort.SelectedIndex = 2;
            comboBaudrate.SelectedIndex = 1;
            comboANTport.SelectedIndex = 0;

            iSerialPort = new SerialPort();
            iSerialPort.DataReceived += new SerialDataReceivedEventHandler(ReceivedComData);
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
            showDB();
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
            string bib;
            Console.WriteLine();
            if (data.Length != 36 ) return;
            if (databasecmd.connection.State == ConnectionState.Closed)
            {
                databasecmd.connectDB();
            }
            try
            {
                //String sql = "SELECT bib FROM tagbib WHERE epc = \"" + data + "\";";
                String sql = "SELECT bib FROM `tagbib` WHERE epc =\""+data+"\" ";
                Console.WriteLine("data:" + data);
                databasecmd.cmd.CommandText = sql;
                bib = (String)databasecmd.cmd.ExecuteScalar();
                Console.WriteLine("bib:" + bib);
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
            try
            {



                DataRow r;
                if (data.Equals(null) || data == "") return;
                if (isReset) return;
                dataGridView1.Invoke(new Action(() =>
                {
                    r = table.NewRow();
                    r["epc"] = data;
                    r["time"] = time;
                    r["ant"] = ant;
                    r["bib"] = bib;
                    table.Rows.InsertAt(r, 0);
                }));

               /* r = table.NewRow();
                r["epc"] = data;
                r["time"] = time;
                r["ant"] = ant;
                table.Rows.InsertAt(r, 0); // test without hardware*/

                countDB++;
                label30.Invoke(new Action(() => { label30.Text = "" + countDB; }));

                //chart1.Invoke(new Action(() => { updateChart(time, countDB.ToString()); }));
               
               // label30.Text = "" + countDB; // test without hardware
                sendLocal();
                sendFile();
                sendCloud();
                checkDB();
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
                  Updatestatus(321);
                File.WriteAllText("D:\\demo.csv", sb.ToString(), Encoding.UTF8);
            }
            catch (Exception e)
            {
                Updatestatus(322);
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
                Updatestatus(331);

            }
            catch (Exception)
            {
                Updatestatus(332);
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
            try {
                if (checkServer == false) return;
                List<DataValue> data = new List<DataValue>();

                data.Add(new DataValue() { epc = "" + dataGridView1.Rows[0].Cells["epc"].Value, time = "" + dataGridView1.Rows[0].Cells["time"].Value, ant = "" + dataGridView1.Rows[0].Cells["ant"].Value, point = "" + checkP });



                string jsonString = data.ToJSON();

                Console.WriteLine(jsonString);
                Updatestatus(311);

                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://10.10.186.197/api/add.php");
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
            catch(Exception ex){
                Updatestatus(312);
                throw;
            }
            
        }

        private void checkDB() // compare data form cloud and localdatabase
        {
            if (checkServer == false) return;
            Record dataValues = new Record();
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://10.10.186.197/api/get.php");
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

                Updatestatus(331);
            }
            catch (Exception)
            {
                Updatestatus(332);
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
        private bool start = false;
        private void button6_Click(object sender, EventArgs e)//start button
        {
            //private bool start = false;
            if (comboCheckpoint.Text == null | comboCheckpoint.Text == "")
            {

                MessageBox.Show("Choose CheckPoint,please");
                panel_list[3].BringToFront();
                label2.Text = "SETTING";
                return;
            }

            if (!start)
            {
                start = true;
                button6.Text = "STOP";
                button6.BackColor = Color.FromArgb(221, 80, 68);
                
                //stop function here
            }
            else
            {
                start = false;
                button6.Text = "START";
                button6.BackColor = Color.FromArgb(33, 220, 82);
                //start function here
                string time = string.Format("{0}:{1}:{2}:{3}", DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond);
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                try
                {
                    HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create("http://10.10.186.197/api/get.php");
                    myRequest.Timeout = 5000;
                    HttpWebResponse response = (HttpWebResponse)myRequest.GetResponse();

                    response = (HttpWebResponse)myRequest.GetResponse();

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        response.Close();
                        checkServer = true;
                        checkBox1.ForeColor = Color.Green;
                        label31.Text = "CONNECTED";
                        label31.ForeColor = Color.Green;
                    }
                    else
                    {
                        response.Close();
                        checkServer = false;
                        checkBox1.ForeColor = Color.Red;
                    }
                }
                catch (Exception)
                {
                    checkServer = false;
                    checkBox1.ForeColor = Color.Red;
                }

            }
        }

        private void showDB()
        {
            if (databasecmd.connection.State == ConnectionState.Closed)
            {
                databasecmd.connectDB();
            }
            try
            {
                MySqlDataAdapter mySqlDataAdapter = new MySqlDataAdapter("select * from checkpoint", databasecmd.connection);
                DataSet DS = new DataSet();
                mySqlDataAdapter.Fill(DS);
                dataGridView2.DataSource = DS.Tables[0];
                dataGridView2.DataSource = DS.Tables[0];
                dataGridView2.Columns[0].Width = 50;
                dataGridView2.Columns[1].Width = 370;
                dataGridView2.Columns[2].Width = 250;
                dataGridView2.Columns[3].Width = dataGridView2.Width - dataGridView2.Columns[0].Width - dataGridView2.Columns[1].Width - dataGridView2.Columns[2].Width;
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

        private void button11_Click(object sender, EventArgs e) //clear db button
        {

            DialogResult dialogResult = MessageBox.Show("This action will delete all your data.", "Warning", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                if (databasecmd.connection.State == ConnectionState.Closed)
                {
                    databasecmd.connectDB();
                }
                try
                {
                    int i = 0;
                    string StrQuery = @"TRUNCATE TABLE checkpoint";
                    databasecmd.cmd.CommandText = StrQuery;
                    Console.WriteLine(StrQuery);
                    databasecmd.cmd.ExecuteNonQuery();
                }
                catch (Exception)
                {
                    MessageBox.Show("Error : please stop reading");
                    throw;
                }
                finally
                {
                    if (databasecmd.connection.State == ConnectionState.Open)
                    {
                        databasecmd.connection.Close();
                    }
                }
                dataGridView2.DataSource = null;
            }
            else if (dialogResult == DialogResult.No)
            {
                return;
            }
            
        }

        //sound button
        private bool sound = true;
        private void button10_Click(object sender, EventArgs e)
        {
            if (sound)
            {
                sound = false;
                button10.Text = "SOUND OFF";
                button10.Image = Resource1.ic_volume_off_white_18pt_2x;
                //function here
            }
            else
            {
                sound = true;
                button10.Text = "SOUND ON";
                button10.Image = Resource1.ic_volume_up_white_18pt_2x;
                //function here
            }
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            string strException = string.Empty;
            string strComPort = comboPort.Text;
            int nBaudrate = Convert.ToInt32(comboBaudrate.Text);


            string time = string.Format("{0}:{1}:{2}:{3}", DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond);

            //updateDataGridView("010131353513513513", time, 1);
            // reset.Enabled = true;

            int nRet = OpenCom(strComPort, nBaudrate, out strException);
            //Console.WriteLine(nRet+"");


            if (strComPort == "COM15") nRet = 0;//ทดสอบที่com15

            if (nRet != 0)
            {
                string strLog = "Connection failed, failure cause: " + strException;

                MessageBox.Show(strLog);


                return;
            }
            else
            {
                string strLog = "Connect" + strComPort + "@" + nBaudrate.ToString();
                label34.Text = "CONNECTED";
                label34.ForeColor = System.Drawing.Color.Green;
               
                MessageBox.Show(strLog);
               
                //buttonConnect.Enabled = true;

                //comboBaudrate.Enabled = false;
                //comboPort.Enabled = false;
                //buttonConnect.BackColor = Color.Green;

                //buttonConnect.Enabled = false;
                //status1.ForeColor = Color.LimeGreen;
                //status1.Text = "Connected";
                return;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            panel_list[0].BringToFront();
            label2.Text = "HOME";
        }


        public int OpenCom(string strPort, int nBaudrate, out string strException)
        {
            strException = string.Empty;

            if (iSerialPort.IsOpen)
            {
                iSerialPort.Close();
            }

            try
            {
                iSerialPort.PortName = strPort;
                iSerialPort.BaudRate = nBaudrate;
                iSerialPort.ReadTimeout = 200;
                iSerialPort.Open();
            }
            catch (System.Exception ex)
            {
                strException = ex.Message;
                return -1;
            }

            m_nType = 0;
            return 0;
        }
        
        public static string ByteArrayToString(byte[] btAryHex, int nIndex, int nLen)
        {
            if (nIndex + nLen > btAryHex.Length)
            {
                nLen = btAryHex.Length - nIndex;
            }

            string strResult = string.Empty;

            for (int nloop = nIndex + 7; nloop < nIndex + nLen - 2; nloop++)
            {
                string strTemp = string.Format(" {0:X2}", btAryHex[nloop]);

                strResult += strTemp;
            }

            //Console.Write(strResult);
            //Console.WriteLine("");
            return strResult;
        }

        private void ReceivedComData(object sender, SerialDataReceivedEventArgs e)
        {

            try
            {
                int nCount = iSerialPort.BytesToRead;
                //label6.ForeColor = System.Drawing.Color.Green;
                if (nCount == 0)
                {
                    return;
                }

                byte[] btAryBuffer = new byte[nCount];
                iSerialPort.Read(btAryBuffer, 0, nCount);

                string time = string.Format("{0}:{1}:{2}:{3}", DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond);
                if (start) {
                    updateDataGridView(ByteArrayToString(btAryBuffer, 0, btAryBuffer.Length), time, 0);
                    UpdateAnt(time);

                }

               
                Console.Write(ByteArrayToString(btAryBuffer, 0, btAryBuffer.Length));
                Console.WriteLine("");
                //RunReceiveDataCallback(btAryBuffer);
                //label6.ForeColor = System.Drawing.Color.Black;
            }
            catch (System.Exception ex)

            {

            }
        }

        private void button12_Click_1(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://localhost/phpmyadmin/sql.php?server=1&db=rfid&table=checkpoint&pos=0&token=ba4ca28d71e5d229ba74d02a152ae779");


            }
            catch (Exception)
            {
                MessageBox.Show("Please fill textboxs.");
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            dataGridView1.Sort(dataGridView1.Columns[1], ListSortDirection.Ascending);
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {

            if (textBox1.Text == "" || textBox2.Text == "") return;

            try
            {
                (dataGridView1.DataSource as DataTable).DefaultView.RowFilter = string.Format("([{0}] >= {1} AND [{0}] <= {2})", "bib", textBox1.Text, textBox2.Text);


            }
            catch (Exception)
            {
                MessageBox.Show("Please fill textboxs.");
            }
           
        }


        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text == "" || textBox2.Text == "") return;
            try
            {
                (dataGridView1.DataSource as DataTable).DefaultView.RowFilter = string.Format("([{0}] >= {1} AND [{0}] <= {2})", "bib", textBox1.Text, textBox2.Text);


            }
            catch (Exception)
            {
                MessageBox.Show("Please fill textboxs.");
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text == "" || textBox2.Text == "") return;
            try
            {
                (dataGridView1.DataSource as DataTable).DefaultView.RowFilter = string.Format("([{0}] >= {1} AND [{0}] <= {2})", "bib", textBox1.Text, textBox2.Text);


            }
            catch (Exception)
            {
                MessageBox.Show("Please fill textboxs.");
            }
        }

        private void UpdateAnt(string time)
        {
            label11.ForeColor = System.Drawing.Color.Lime;
            label3.ForeColor = System.Drawing.Color.Lime;

            label11.Invoke(new Action(() => { label11.Text = time; }));
          
        }
        private void Updatestatus(int tcase)
        {

            switch (tcase)
            {
                case 311:
                    label31.ForeColor = System.Drawing.Color.Green;
                    label31.Invoke(new Action(() => { label31.Text = "CONNECTED"; }));
                    break;
                case 312:
                    label31.ForeColor = Color.FromArgb(192, 0, 0);
                    label31.Invoke(new Action(() => { label31.Text = "DISCONNECTED"; }));
                    break;
                case 321:
                    label32.ForeColor = System.Drawing.Color.Green;
                    label32.Invoke(new Action(() => { label32.Text = "CONNECTED"; }));
                    break;
                case 322:
                    label32.ForeColor = Color.FromArgb(192, 0, 0);
                    label32.Invoke(new Action(() => { label32.Text = "DISCONNECTED"; }));
                    break;
                case 331:
                    label33.ForeColor = System.Drawing.Color.Green;
                    label33.Invoke(new Action(() => { label33.Text = "CONNECTED"; }));
                    break;
                case 332:
                    label33.ForeColor = Color.FromArgb(192, 0, 0);
                    label33.Invoke(new Action(() => { label33.Text = "DISCONNECTED"; }));
                    break;
                case 341:
                    label34.ForeColor = System.Drawing.Color.Green;
                    label34.Invoke(new Action(() => { label34.Text = "CONNECTED"; }));
                    break;
                case 342:
                    label34.ForeColor = Color.FromArgb(192, 0, 0);
                    label34.Invoke(new Action(() => { label34.Text = "DISCONNECTED"; }));
                    break;
                default:
                    Console.WriteLine("Default case");
                    break;
            }
           

        }

        private void initChart()
        {

            // chart1.Series["Tag"].Points.AddXY("Ajay", "10000");
            chart1.Series["Tag"].Points.AddXY(0, 0);
            chart1.Titles.Add("Tag Count");
        }
        private void updateChart()
        {

            // chart1.Series["Tag"].Points.AddXY("Ajay", "10000");
          
           
        }

        private void OnTimerEvent(object source, EventArgs e)
        {
            chart1.Series["Tag"].Points.AddXY(string.Format("{0}:{1}:{2}:{3}", DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond), countDB);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            isReset = !isReset;
            if (isReset)
            {
                //reset.Enabled = false;
                table.Clear(); //old >dataGridView1.DataSource = new DataGridView();
                //resetAnt();

                //start.Enabled = false;

                comboBaudrate.Enabled = true;
                comboPort.Enabled = true;
                //connectLAN.BackColor = Color.White;

                //connectLAN.Enabled = true;
                //status1.ForeColor = Color.Red;
                //status1.Text = "Disconnected";
                isReset = !isReset;

            }
        }

    }
    


}
