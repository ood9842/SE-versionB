using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace MockupV1
{
    class ConnectDatabase
    {
        private string MyConnectionString = "Server=localhost;Database=rfid;Uid=root;Pwd=";
        private MySqlCommand _cmd;
        public MySqlCommand cmd
        {
            get { return _cmd; } set { _cmd = value; }
        }
        private MySqlConnection _connection;
        public MySqlConnection connection
        {
            get { return _connection; }
            set { _connection = value; }
        }
        public static ConnectDatabase Instance;

        public ConnectDatabase()
        {
            Instance = this;
            _connection = new MySqlConnection(MyConnectionString);
            _cmd = new MySqlCommand("", connection);
        }

        public void connectDB()
        {
            Instance.connection.Open();
            Console.WriteLine("Connect Complete");
        }
    }
}
