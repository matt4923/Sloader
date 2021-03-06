﻿using System.ComponentModel;
using System.Data;


namespace Sloader
{
    public abstract class SwitchImport
    {
        internal abstract string GetDelimeter(string file);
        internal abstract void ParseSwitchFile(object sender, DoWorkEventArgs e);
        internal Windows.WindowProgressBar ProgBar;
        private DataTable m_SwitchReport;
        public DataTable SwitchReport
        {
            get { return m_SwitchReport; }
            set { m_SwitchReport = value; }
        }
        internal DataRow AddColumnToTableIfNecessary(DataRow associatedRecord, string colName)
        {
            if (!associatedRecord.Table.Columns.Contains(colName))
            {
                associatedRecord.Table.Columns.Add(colName);
            }
            return associatedRecord;
        }

        private int m_NumberOfRecords;
        internal int NumberOfRecords
        {
            get { return m_NumberOfRecords; }
            set { m_NumberOfRecords = value; }
        }

        private string m_Delimeter;
        internal string Delimeter
        {
            get { return m_Delimeter; }
            set { m_Delimeter = value; }
        }
        #region Parsing
        public class ParsingCompleteEventArgs : System.EventArgs 
        {
            //can add additional parameters to send here
            
        }

        public delegate void ParsingCompleteEventHandler(object sender, ParsingCompleteEventArgs e);
        public event ParsingCompleteEventHandler ParsingComplete;
        private void OnParsingComplete()
        {
            if (ParsingComplete != null)
            {
                ParsingComplete(this, new ParsingCompleteEventArgs());
            }
        }

        private void FinishedParsing(object sender, RunWorkerCompletedEventArgs e)
        {
            ProgBar.Finished = true;
            OnParsingComplete();
            ProgBar.Close();
        }

        private BackgroundWorker bw;
        private int m_TotalLines;
        internal void DoParsing(System.IO.StreamReader switchFile)
        {
            string fullFile = switchFile.ReadToEnd();
            m_Delimeter = GetDelimeter(fullFile);
            bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(ParseSwitchFile);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(FinishedParsing);
            ProgBar = new Windows.WindowProgressBar(m_NumberOfRecords);
            ProgBar.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen; 
            ProgBar.Show();
            bw.RunWorkerAsync();
        }
        #endregion



    }
}
