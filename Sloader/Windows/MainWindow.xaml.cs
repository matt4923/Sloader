﻿using System;
using System.Windows;
using System.Windows.Media;
using System.Data;
using System.IO;
using System.ComponentModel;

namespace Sloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public abstract partial class MainWindow : Window
    {
        private string m_SwitchType;
        private string m_Ip;
        private int m_Port;
        private string m_User;
        private string m_Pass;
        private string m_TextForResultsWindow;

        public MainWindow(string type)
        {
            InitializeComponent();
            btnConnectNow.Background = Brushes.MediumSpringGreen;
            LoadStoredIpSettings();
            SetSshType(type);
            SetupAndStartResultsTimer();
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        }

        internal System.Windows.Threading.DispatcherTimer resultsTimer;
        private void SetupAndStartResultsTimer()
        {
            //This timer checks the m_TextForResultsWindow.  The tick event appends text from other threads to the results textbox
            resultsTimer = new System.Windows.Threading.DispatcherTimer();
            resultsTimer.Tick +=resultsTimer_Tick;
            resultsTimer.Interval = new TimeSpan(0,0,0,0,100); //100 millis.
            resultsTimer.Start();
        }

        private void resultsTimer_Tick(object sender, EventArgs e)
        {
            txtResult.AppendText(m_TextForResultsWindow);
            m_TextForResultsWindow = string.Empty;
            txtResult.ScrollToEnd();
            if (txtResult.LineCount > 300) { txtResult.Text = string.Empty; }
        }

        private const string SWITCHAVAYA = "AVAYA";
        private const string SWITCHCS2100 = "CS2100";
        private const string SWITCHAS5300 = "AS5300";
        private const string SWITCHCISCO = "CISCO";
        private const string SWITCHTELEBOSS = "TELEBOSS";

        private void SetSshType(string type)
        {
            m_SwitchType = type.ToUpper();
        }
        internal SwitchConnection m_Conn;

        private void btnConnectNow_Click(object sender, RoutedEventArgs e)
        {
            if (m_Conn == null || !m_Conn.Connected)
            {
                m_Conn = new SwitchConnection(m_User, m_Pass, m_Ip, m_Port, m_SwitchType);
                try
                {
                    m_Conn.Connect();
                }
                catch (Exception ex) { MessageBox.Show("Connection error, please check your settings including User and Password then try again.  \n " +
                    "If you continue to get this message, check your connection through a terminal editor like PuTTY.");
                }
                
                if (m_Conn.Connected)
                {
                    btnConnectNow.Content = "Disconnect";
                    btnConnectNow.Background = Brushes.Red;
                }
                txtResult.AppendText(m_Conn.ConnectionMessageBuffer.CurrentMessage);
            }
            else
            {
                m_Conn.Disconnect();
                if (!m_Conn.Connected)
                {
                    btnConnectNow.Content = "Connect Now";
                    btnConnectNow.Background = Brushes.MediumSpringGreen;
                }
            }
        }

        internal void UpdateResultsWindow(string str)
        {
            //Different threads update this.  
            m_TextForResultsWindow = str;
        }
        
        internal BackgroundWorker _bw;
  
        public void SendIt(string p)
        {

            m_Conn.SendCommand(p);

            txtResult.AppendText(m_Conn.ConnectionMessageBuffer.CurrentMessage);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            //exit
            Application.Current.Shutdown();
        }

        internal void ClearWindow()
        {
            txtResult.Text = string.Empty;
        }

        private void MenuItem_Click_menuSetupConnection(object sender, RoutedEventArgs e)
        {
            SetupSwitchConnectionWindow();
        }

        private void SetupSwitchConnectionWindow()
        {
            //Setup Switch Connection item
            FrmManageSwitchConnection SwitchConn = new FrmManageSwitchConnection();

            var dia = SwitchConn;
            dia.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            var diaResult = dia.ShowDialog();

            if (diaResult.HasValue && diaResult.Value)
            {
                m_Ip = SwitchConn.GetIpAddress;
                m_Port = SwitchConn.GetPort;
                m_User = SwitchConn.GetUser;
                m_Pass = SwitchConn.GetPassword;
                btnConnectNow.IsEnabled = true;
            }
        }

        private void LoadStoredIpSettings()
        {
            System.IO.FileStream connectionFile = null;
            DataSet ds = null;
            try
            {
                connectionFile = System.IO.File.Open("SSHConnection.xml", System.IO.FileMode.Open);
                connectionFile.Close();
            }
            catch (Exception ex)
            {
                //No db credentials found
                MessageBox.Show("No SSH credentials found please add them in Connection Settings");
                btnConnectNow.IsEnabled = false;
                return;
            }

            ds = new DataSet();
            ds.ReadXml(@"SSHConnection.xml");
            DataTable dt = new DataTable();
            dt = ds.Tables[0];

            DataRow dr = dt.Rows[0];
            m_Ip = (string)dr["IP"];
            m_Port = int.Parse(dr["Port"].ToString());
            m_User = (string)dr["User"];
            m_Pass = (string)dr["Password"];
        }

        internal string CheckAndAddReportFile(String FName)
        {
            string fullPath = CreateReportDirectory() + "\\" + FName;

            FileStream fs;
            try
            {
                //create if doesn't exist
                fs = File.Open(fullPath, FileMode.Create);
                fs.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return string.Empty;
            }

            return fullPath;
        }

        private string CreateReportDirectory()
        {
            string path = string.Empty;
            switch (m_SwitchType)
            {
                case SWITCHAVAYA:
                    path = @"\Report\Avaya";
                    break;
                case SWITCHTELEBOSS:
                    path = @"\Report\Teleboss";
                    break;
                default:
                    return string.Empty;
            }
            string strDestopPath = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            path = strDestopPath + path;
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            return path;
        }

        #region Abstract Buttons
        private void btnSendCommand_Click(object sender, RoutedEventArgs e)
        {
            //Send single command button
            SingleCommandButton();
        }

        private void btnCustom1_Click(object sender, RoutedEventArgs e)
        {

            ClearWindow();
            DoBtnCustom1Click();
        }

        public abstract void SingleCommandButton();
        public abstract void DoBtnCustom1Click();
        public abstract void DoBtnCustom2Click();
        public abstract void DoBtnCustom3Click();
        public abstract void DoBtnCustom4Click();
        public abstract void DoBtnCustom5Click();

        private void btnCustom2_Click(object sender, RoutedEventArgs e)
        {
            ClearWindow();
            DoBtnCustom2Click();
        }

        private void btnCustom3_Click(object sender, RoutedEventArgs e)
        {
            ClearWindow();
            DoBtnCustom3Click();
        }

        private void btnCustom4_Click(object sender, RoutedEventArgs e)
        {
            ClearWindow();
            DoBtnCustom4Click();
        }

        private void btnCustom5_Click(object sender, RoutedEventArgs e)
        {
            ClearWindow();
            DoBtnCustom5Click();
        }
        #endregion

        private void btnClearConsole_Click(object sender, RoutedEventArgs e)
        {
            txtResult.Text = string.Empty;
        }

        private void menuSwitchFileImport(object sender, RoutedEventArgs e)
        {
            WindowSwitchFileImportDialog sd = new WindowSwitchFileImportDialog();
            
            //Need to make this an actual mdi form
            sd.Owner = this;
            sd.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            sd.ShowInTaskbar = false;
            sd.ShowDialog();
        }
    }
}    
