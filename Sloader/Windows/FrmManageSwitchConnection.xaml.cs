using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data;

namespace Sloader
{
    /// <summary>
    /// Interaction logic for FrmManageSwitchConnection.xaml
    /// </summary>
    public partial class FrmManageSwitchConnection : Window
    {
        public FrmManageSwitchConnection()
        {
            InitializeComponent();
        }
        private string m_IpAddress;
        private int m_Port;
        private string m_User;
        private string m_Password;

        public string GetIpAddress
        {
            get { return m_IpAddress; }
        }

        public int GetPort
        {
            get { return m_Port; }
        }

        public string GetUser
        {
            get { return m_User; }
        }

        public string GetPassword
        {
            get { return m_Password; }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (txtIP.Text.Trim() != string.Empty && txtPort.Text.Trim() != string.Empty && txtUser.Text.Trim() != string.Empty && txtPass.Password.Trim() != string.Empty)
            {
                DialogResult = true;
                m_IpAddress = txtIP.Text.Trim();
                m_Port = int.Parse(txtPort.Text.Trim());
                m_User = txtUser.Text.Trim();
                m_Password = txtPass.Password.Trim(); //txtPass.Text.Trim();

                SaveToXML();

            }
            else
            {
                MessageBox.Show("Please populate all fields before clicking OK.");
            } 
        }

        private void SaveToXML()
        {
            using(DataTable dt = new DataTable("myTable")){
                dt.Columns.Add("IP", typeof(string));
                dt.Columns.Add("Port", typeof(int));
                dt.Columns.Add("User", typeof(string));
                dt.Columns.Add("Password", typeof(string));

                dt.Rows.Add(m_IpAddress, m_Port, m_User, m_Password);

                dt.WriteXml("SSHConnection.xml");
            }
        }
    }
}
