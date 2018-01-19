using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Sloader.Switches;
using System.Data;
using System.Collections.Generic;
using System;
using System.Windows.Data;
using System.Windows.Input;

namespace Sloader
{
    /// <summary>
    /// Interaction logic for WindowSwitchFileImportDialog.xaml
    /// </summary>
    public partial class WindowSwitchFileImportDialog : Window
    {
        private SwitchProperties.SwitchType m_OldSwitchType;
        private SwitchProperties.SwitchType m_NewSwitchType;

        public WindowSwitchFileImportDialog()
        {
            InitializeComponent();
            NameAllRadioButtonsCorrectly();
        }

        private void NameAllRadioButtonsCorrectly()
        {
            rad5ess.Content = FIVE_ESS;
            radCs1000.Content = CS1000;
            radEwsd.Content = EWSD;
            radSl100.Content = SL100;
            radHipath.Content = HIPATH;
            radAvaya.Content = AVAYA;
            radNewAs5300.Content = AS5300;
            radNewAvaya.Content = AVAYA;
            radNewCisco.Content = CISCO;
            //fire radio event
            radCs1000.IsChecked = true;
            radNewAvaya.IsChecked = true;
        }

        const string FIVE_ESS = "5ESS";
        const string CS1000 = "CS1000";
        const string EWSD = "EWSD";
        const string SL100 = "SL100";
        const string HIPATH = "HiPath";
        const string AVAYA = "Avaya";
        const string CISCO = "Cisco";
        const string AS5300 = "AS5300";

        private OpenFileDialog fd;
        private void btnPickFile_Click(object sender, RoutedEventArgs e)
        {

            fd = new OpenFileDialog();
            if (fd.ShowDialog() == true)
            {
                System.IO.StreamReader switchFile;
                switchFile = new System.IO.StreamReader(fd.FileName);
                switch (m_OldSwitchType)
                {
                    case SwitchProperties.SwitchType.CS1000:
                        Parsing.CS1000_TNB cs1000Parser = new Parsing.CS1000_TNB(switchFile);
                        cs1000Parser.ParsingComplete += Parser_ParsingComplete;
                        break;
                    case SwitchProperties.SwitchType.Avaya:
                        Parsing.Avaya_DispStat avayaParser = new Parsing.Avaya_DispStat(switchFile);
                        avayaParser.ParsingComplete += Parser_ParsingComplete;
                        break;
                    default:
                        //nothing
                        break;
                }
            }
        }
        private void Radio_Check(object sender, RoutedEventArgs e)
        {
            RadioButton rad = sender as RadioButton;
            switch (rad.Content.ToString())
            {
                case FIVE_ESS:
                    m_OldSwitchType = SwitchProperties.SwitchType.fiveEss;
                    btnPickFile.Content = "5ESS Raw File Import";
                    break;
                case CS1000:
                    m_OldSwitchType = SwitchProperties.SwitchType.CS1000;
                    btnPickFile.Content = "TNB to Import";
                    break;
                case EWSD:
                    m_OldSwitchType = SwitchProperties.SwitchType.Ewsd;
                    btnPickFile.Content = "SDNDAT to Import";
                    break;
                case SL100:
                    m_OldSwitchType = SwitchProperties.SwitchType.SL100;
                    btnPickFile.Content = "QCUST to Import";
                    break;
                case HIPATH:
                    m_OldSwitchType = SwitchProperties.SwitchType.Hipath;
                    btnPickFile.Content = "HiPath Raw File Import";
                    break;
                case AVAYA:
                    m_OldSwitchType = SwitchProperties.SwitchType.Avaya;
                    btnPickFile.Content = "Display Stations Import";
                    break;
                default:
                    MessageBox.Show("Switch type not handled");
                    break;
            }
        }

        #region Report Tab

        private void Parser_ParsingComplete(object sender, SwitchImport.ParsingCompleteEventArgs e)
        {
            // This will happen when the parsing is complete.
            var parser = (SwitchImport)sender;
            DataTable dt = parser.SwitchReport;
            dgSwitchImportData.ItemsSource  = dt.DefaultView;
            SetColumnWidths();
            
            //build associations tab
            BuildAssociationsTab(dt);
        }

        private void SetColumnWidths()
        {
            foreach(var col in dgSwitchImportData.Columns)
            {
                int i = col.DisplayIndex;
                string colName = col.Header.ToString();
                switch (colName.ToUpper())
                {
                    case "OPTIONS":
                        dgSwitchImportData.Columns[i].Width = 200;
                        break;
                }
            }
        }

        #endregion

        #region Associations Tab
        public List<string> m_OldSwitchHeaders;
        List<DataGridItems> m_DataGridItemsList;

        private void BuildAssociationsTab(DataTable dt)
        {
            m_OldSwitchHeaders = new List<string>();
            foreach (DataColumn col in dt.Columns)
            {
                //OLD Fields
                m_OldSwitchHeaders.Add(col.ColumnName);
            }

            //List<DataGridItems> itemsList = new List<DataGridItems>();
            m_DataGridItemsList = LoadNewSwitchFieldList(new List<DataGridItems>());

           
            //foreach (DataColumn col in dt.Columns)
            //{
            //    itemsList.Add(new DataGridItems { NewSwitchField = col.ColumnName});
            //}

            OldSwitchField.ItemsSource = m_OldSwitchHeaders;
            dgAssoc.ItemsSource = m_DataGridItemsList;
        }

        private List<DataGridItems> LoadNewSwitchFieldList(List<DataGridItems> itemsList)
        {
            switch (m_NewSwitchType)
            {
                case SwitchProperties.SwitchType.Avaya:
                    foreach (SwitchProperties.AvayaFields s in Enum.GetValues(typeof(SwitchProperties.AvayaFields)))
                    {
                        itemsList.Add(new DataGridItems { NewSwitchField = s.ToString() });
                    }
                    break;
                case SwitchProperties.SwitchType.Cisco:
                    foreach (SwitchProperties.AvayaFields s in Enum.GetValues(typeof(SwitchProperties.CiscoFields)))
                    {
                        itemsList.Add(new DataGridItems { NewSwitchField = s.ToString() });
                    }
                    break;
                case SwitchProperties.SwitchType.As5300:
                    foreach (SwitchProperties.AvayaFields s in Enum.GetValues(typeof(SwitchProperties.As5300Fields)))
                    {
                        itemsList.Add(new DataGridItems { NewSwitchField = s.ToString() });
                    }
                    break;
            }
            return itemsList;
        }

        public class DataGridItems
        {
            public string NewSwitchField { get; set; }
            public ComboBox OldSwitchCbx { get; set; }
        }

        private string FixStringName(string str)
        {
            str = str.Replace(" ", "_");
            str = str.Replace("-", "");
            str = str.Replace("/", "_");
            str = str.Replace(",", "_");
            str = str.Replace("(", "");
            str = str.Replace(")", "");
            return str;
        }

        #endregion

        private void NewSwitchRadioChange(object sender, RoutedEventArgs e)
        {
            RadioButton rad = sender as RadioButton;
            string newSwitch = rad.Content.ToString();
            switch (newSwitch)
            {
                case CISCO:
                    m_NewSwitchType = SwitchProperties.SwitchType.Cisco;
                    break;
                case AVAYA:
                    m_NewSwitchType = SwitchProperties.SwitchType.Avaya;
                    break;
                case AS5300:
                    m_NewSwitchType = SwitchProperties.SwitchType.As5300;
                    break;
                default:
                    break;
            }
            m_DataGridItemsList = LoadNewSwitchFieldList(new List<DataGridItems>());
        }

    } //WindowSwitchFileImportDialog

    }
