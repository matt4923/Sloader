using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using Sloader.Switches;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace Sloader
{
    class AvayaMain : MainWindow
    {
        public AvayaMain() : base("AVAYA")
        {
            SetCustomButtonText();
            this.Show();
        }

        private void SetCustomButtonText()
        {
            btnCustom1.Content = "Do Port Sync";
            btnCustom2.Content = "Do Set Sync";
        }

        public override void SingleCommandButton()
        {
            SendIt(txtCommand.Text.Trim());
            Switches.AvayaCommands avCmd = new Switches.AvayaCommands();
            SendIt(avCmd.Command(AvayaCommands.Commands.Cancel));
        }

        public override void DoBtnCustom1Click()
        {
            //Avaya port sync
            DoListConfigStations();
        }

        private List<int> NumberList;
        public override void DoBtnCustom2Click()
        {
            //Avaya Set Sync
            DoListStationThenDispStations();
        }

        public override void DoBtnCustom3Click()
        {

        }

        public override void DoBtnCustom4Click()
        {
            
        }

        public override void DoBtnCustom5Click()
        {

        }

        #region Port Sync
        private void DoListConfigStations()
        {
            _bw = new BackgroundWorker();
            _bw.WorkerSupportsCancellation = true;
            _bw.WorkerReportsProgress = true;
            _bw.DoWork += new DoWorkEventHandler(bw_DoListConfigStatWork);
            _bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ListConfigStatFinished);
            _bw.RunWorkerAsync();
        }

        private void ListConfigStatFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            //resultsTimer.Stop();
            _bw.DoWork -= bw_DoListConfigStatWork;
            _bw.RunWorkerCompleted -= ListConfigStatFinished;
        }

        private void bw_DoListConfigStatWork(object sender, DoWorkEventArgs e)
        {
            //resultsTimer.Start();
            BackgroundWorker worker = sender as BackgroundWorker;
            System.IO.StreamWriter sw = GetWriter("List_Config_Stat.txt");

            AvayaCommands av = new AvayaCommands();
            m_Conn.SendCommand(av.Command(AvayaCommands.Commands.ListConfigStation));
            System.Threading.Thread.Sleep(100);
            string inComingText = m_Conn.ConnectionMessageBuffer.CurrentMessage;
                        
            UpdateResultsWindow(inComingText);
            DoAllPagesAndCloseFile(sw);
        }
        

        #endregion

        #region Set Sync
        private void DoListStationThenDispStations()
        {
            _bw = new BackgroundWorker();
            _bw.WorkerSupportsCancellation = true;
            _bw.WorkerReportsProgress = true;
            _bw.DoWork += new DoWorkEventHandler(bw_DoListStatWork);
            _bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ListStatFinished);
            _bw.RunWorkerAsync();
        }

        private void ListStatFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            _bw.DoWork -= bw_DoListStatWork;
            //now parse the list stat file
            Parsing.Avaya_ListStat ls = new Parsing.Avaya_ListStat();
            NumberList = ls.GetNumberList();
            
            //TO DO: LIST DATA
            
            //Now do Disp Stat
            DoDisplayStation();

            //TO DO: DISP DATA's
        }

        private void bw_DoListStatWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            System.IO.StreamWriter sw = GetWriter("List_Station.txt");
            //resultsTimer.Start();
            AvayaCommands av = new AvayaCommands();
            m_Conn.SendCommand(av.Command(AvayaCommands.Commands.ListStation));

            string inComingText = m_Conn.ConnectionMessageBuffer.CurrentMessage;
            //txtResult.AppendText(inComingText);
            UpdateResultsWindow(inComingText);

            System.Threading.Thread.Sleep(100);
            DoAllPagesAndCloseFile(sw);
        }

        private void DoDisplayStation()
        {
            _bw = new BackgroundWorker();
            _bw.WorkerSupportsCancellation = true;
            _bw.WorkerReportsProgress = true;
            _bw.DoWork += new DoWorkEventHandler(bw_DoDispStatWork);
            _bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(DispStatFinished);
            _bw.RunWorkerAsync();
        }

        private void DispStatFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            //resultsTimer.Stop();
            _bw.DoWork -= bw_DoDispStatWork;
        }

        private void bw_DoDispStatWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            ScrollThroughDispStats();
        }

        private void ScrollThroughDispStats()
        {
            System.IO.StreamWriter sw = GetWriter("Display_Stations.txt");
            string inComingText = string.Empty;
            AvayaCommands av = new AvayaCommands();
            string dispStatStr = av.Command(AvayaCommands.Commands.DisplayStation);
            string cancel = av.Command(AvayaCommands.Commands.Cancel);

            foreach (int num in NumberList)
            {
                //AvayaSend(dispStatStr + num, clearBuffer: false);
                m_Conn.SendCommand(dispStatStr + num, clearBuffer: false);
                m_Conn.Wait(500);
                inComingText = m_Conn.ConnectionMessageBuffer.CurrentMessage;
                //txtResult.AppendText(inComingText);
                UpdateResultsWindow(inComingText);

                int PageCount = GetSetPages(inComingText);

                for (int i = 1; i < PageCount; i++)
                {
                    //scroll through pages
                    //AvayaSend(av.Command(AvayaCommands.Commands.NextPage), clearBuffer: false);
                    m_Conn.SendCommand(av.Command(AvayaCommands.Commands.NextPage), clearBuffer: false);
                    m_Conn.Wait(200);
                }
                m_Conn.Wait(500);
                inComingText = m_Conn.ConnectionMessageBuffer.CurrentMessage;
                sw.Write(inComingText); //write to display_station.txt
                UpdateResultsWindow(inComingText);
                m_Conn.SendCommand(cancel);
            }
            sw.Close();
        }

        private int GetSetPages(string inComingText)
        {
            string[] possPageNumbers = Regex.Split(inComingText, @"Page\s{2,3}(?<CurrentPage>\d{1,2})\sof\s{2,3}(?<MaxPage>\d{1,2})");

            return int.Parse(possPageNumbers[2]); //using above page max is in array element 2
        }
        #endregion

        private void DoAllPagesAndCloseFile(System.IO.StreamWriter fileToWrite = null)
        {
            while (!ReportIsFinished(m_Conn.ConnectionMessageBuffer.CurrentMessage))
            {
                AvayaCommands av = new AvayaCommands();
                m_Conn.SendCommand(av.Command(AvayaCommands.Commands.NextPage));
                m_Conn.Wait(100);

                string inComingText = m_Conn.ConnectionMessageBuffer.CurrentMessage;
                //txtResult.AppendText(inComingText);
                if (fileToWrite != null) { fileToWrite.Write(inComingText); }
            }

            fileToWrite.Close();
        }
        
        private bool ReportIsFinished(string reportData)
        {
            return reportData.IndexOf("Command successfully completed") > 0;
        }

        private System.IO.StreamWriter GetWriter(string p)
        {
            string path = CheckAndAddReportFile(p);
            System.IO.StreamWriter sw = new System.IO.StreamWriter(path, true);
            return sw;
        }
    }
}
