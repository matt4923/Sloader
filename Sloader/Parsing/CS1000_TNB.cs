using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Data;
using Sloader.Switches;

namespace Sloader.Parsing
{
    public class CS1000_TNB : SwitchImport  
    {
        private string[] m_Records;
        private DataTable tnbTable;

        public CS1000_TNB(System.IO.StreamReader switchFile)
        {
            // TODO: Complete member initialization
            DoParsing(switchFile);
        }

        internal override void ParseSwitchFile(object sender, DoWorkEventArgs e)
        {
            ProgBar.Text = "Processing CS1000 TNB Report";
            tnbTable = new DataTable();
            AddKnownColumnsToTable(ref tnbTable);

            List<DataRow> buttList = new List<DataRow>();
            
            foreach(string rec in m_Records)
            {
                if (!Regex.IsMatch(rec, "^DES"))
                {
                    continue;  //this is not a record
                }

                ProgBar.NewValue = Array.IndexOf(m_Records, rec);

                //new record
                string[] recordLines = Regex.Split(rec, Delimeter);
                string lastCol = "";
                bool recFinished = false;
                try
                {
                    for (int i = 0; i < recordLines.Length; i++)  //need to be able to change the counter throughout
                    {
                        if (recFinished) break;

                        DataRow associatedRecord = null;        
                        bool addToCollection = false;
                        
                        string colName = Regex.Split(recordLines[i], @"\s")[0];

                        if (colName == "DATE")
                        {
                            recFinished = true;
                        }

                        if (colName == string.Empty) 
                        { 
                            if(lastCol == SwitchProperties.CS1000Fields.Key.ToString().ToUpper())
                            {
                                //key fields
                                i = LoadCPNDRows(buttList[0], recordLines, i);
                                continue;
                            } 
                            colName = lastCol; 
                        }

                        colName = RenameColumnIfNecessary(colName);

                        if ((!tnbTable.Columns.Contains(colName) && !string.IsNullOrEmpty(colName)))
                        {
                            tnbTable.Columns.Add(colName);
                        }

                        if (colName == SwitchProperties.CS1000Fields.DN.ToString())
                        {
                            associatedRecord = buttList[0];
                            LoadDNInfo(associatedRecord, recordLines[i]);
                            i = LoadCPNDRows(associatedRecord, recordLines, i + 1);
                            lastCol = colName;
                            continue;
                        } //else DO BFTN buttons
                        else
                            if (buttList.Count == 0)
                            {
                                associatedRecord = tnbTable.NewRow();
                                
                                addToCollection = true;
                            }
                            else
                            {
                                associatedRecord = buttList[0];
                            }

                        LoadRowWithLineInformation(associatedRecord, recordLines[i], (colName == "ADDON"), colName);
                        lastCol = colName;

                        if (addToCollection)
                        {
                            buttList.Add(associatedRecord);
                        }
                    } //inner loop

                    recordLines = null;

                    foreach (DataRow button in buttList)
                    {
                     tnbTable.Rows.Add(button);
                    }
                }
                catch (Exception ex)
                {
                    string text = string.Format("Failed to parse line: '{0}'.  Error Message: {1}", rec.ToString(), ex.Message);
                }
                finally { buttList.Clear(); }

            } //outer loop
            Console.WriteLine("finished");
            SwitchReport = tnbTable;
        }

        private void AddKnownColumnsToTable(ref DataTable tnbTable)
        {
            tnbTable.Columns.Add(SwitchProperties.CS1000Fields.DN.ToString());
            tnbTable.Columns.Add(SwitchProperties.CS1000Fields.Key.ToString());
            tnbTable.Columns.Add(SwitchProperties.CS1000Fields.Buttons.ToString());
            tnbTable.Columns.Add(SwitchProperties.CS1000Fields.MADN.ToString());
            tnbTable.Columns.Add(SwitchProperties.CS1000Fields.CLID.ToString());
            tnbTable.Columns.Add(SwitchProperties.CS1000Fields.KEY_DIAL_TONE_FEATURE.ToString());
            tnbTable.Columns.Add(SwitchProperties.CS1000Fields.ACD_PILOT.ToString());
        }

        private int LoadCPNDRows(DataRow associatedRecord, string[] recordLines, int startInx)
        {
            for (int i = startInx; (i <= (recordLines.Length - 1)); i++)
            {
                if ((recordLines[i].IndexOf(' ') != 0))
                {
                    return (i - 1); // analog
                }
                //int ot = 0;
                if (Regex.IsMatch(recordLines[i], @"\s{5}\d{2}\s")) 
                {
                    //digital key appearance
                    Match m = Regex.Match(recordLines[i], @"\s{5}(?<ButtonNumber>\d{2})\s(?<InfoOnKey>.*$)");
                    if(m.Groups["InfoOnKey"].Value.Trim() != string.Empty)
                    {
                        LoadRowWithLineInformation(associatedRecord, recordLines[i], false, SwitchProperties.CS1000Fields.Buttons.ToString());
                    }
                    //return (i - 1); //digital
                    continue;
                }

                if ((recordLines[i].Trim().Split(' ').Length > 1))
                {
                    string colName = Regex.Split(recordLines[i].Trim(), @"\s")[0];
                    if (string.IsNullOrEmpty(colName.Trim()))
                    {
                        Console.WriteLine(string.Format("colName: {0} problem", colName));
                    }

                    //associatedRecord = AddColumnToTableIfNecessary(associatedRecord, colName);

                    LoadRowWithLineInformation(associatedRecord, recordLines[i], false, colName);
                }

            }

            return (startInx - 1);
        }

        private void LoadRowWithLineInformation(DataRow associatedRecord, string line, bool isAddOnLine, string colName)
        {
            string pattern = @"(?<COLUMNNAME>[\w-_]+)\s+(?<VALUE>.+)$";
            Match m = Regex.Match(line, pattern);
            if (string.IsNullOrEmpty(m.Groups[0].Value))
            {
                return;
            }
            if (string.IsNullOrEmpty(colName.Trim()) && !string.IsNullOrEmpty(m.Groups["COLUMNNAME"].Value.Trim()))
            {
                colName = m.Groups["COLUMNNAME"].Value;
            }

            associatedRecord = AddColumnToTableIfNecessary(associatedRecord, colName);

            if (((colName == "RNPG")
                        || isAddOnLine))
            {
                if ((int.Parse(m.Groups["VALUE"].Value.Trim()) == 0))
                {
                    return;
                }

                // & this means there is no call-pickup associated to it
            }

            if (isAddOnLine)
            {
                associatedRecord = AddColumnToTableIfNecessary(associatedRecord, "ADDONCOUNT");
                associatedRecord = AddColumnToTableIfNecessary(associatedRecord, "ADDON");
                associatedRecord["ADDON"] = m.Groups["COLUMNNAME"].Value.Trim();
                associatedRecord["ADDONCOUNT"] = m.Groups["VALUE"].Value.Trim();
            }
            else if (colName.ToUpper() == "OPTIONS" && associatedRecord["OPTIONS"].ToString() != string.Empty)
            {
                //adding more features to CLS list

                associatedRecord[colName] += " " + line.Trim();
            }
            else if ((colName == "TN"))
            {
                string tnPattern = @"(?<COLUMNNAME>[\w_]+)\s+(?<VALUE>\d{3}\s?\d?\s\d{2}\s?\d{0,2})";
                m = Regex.Match(line, tnPattern);
                associatedRecord[colName] = m.Groups["VALUE"].Value.Trim();
            }
            else if ((colName == SwitchProperties.CS1000Fields.Key.ToString().ToUpper()))
            {
                LoadDNInfo(associatedRecord, m.Groups["VALUE"].Value.Trim());
            }
            else if ((associatedRecord[colName] == DBNull.Value))
            {
                //Add to blank cell
                if (colName == SwitchProperties.CS1000Fields.Buttons.ToString())
                {
                    associatedRecord[colName] = line;
                }
                else
                {
                    associatedRecord[colName] = m.Groups["VALUE"].Value.Trim();
                }
            }
            else
            {
                //concatenate to existing text
                if(colName == SwitchProperties.CS1000Fields.Buttons.ToString())
                {
                    //buttons are separated by tilde's instead of creating a new separate record for each 
                    associatedRecord[colName] = string.Format("{0}~~~{1}", associatedRecord[colName].ToString(), line);
                }
                else
                {
                    associatedRecord[colName] = string.Format("{0} {1}", associatedRecord[colName].ToString(), m.Groups["VALUE"].Value.Trim());
                }
            }
        }
        
        private void LoadDNInfo(DataRow associatedRecord, string line)
        {
            string pattern = "";
            string MainDigKeyPattern = "";
            string pattern3 = "";
            string pattern4 = "";
            Match m;

            if ((line.IndexOf("DN") == 0))
            {
                pattern = @"DN\s{3}(?<DN>\d{1,10})\s{1}(?<CLID>\w+)\s*(?<MADN>MARP)?";
                associatedRecord["KEY"] = 0;
                m = Regex.Match(line, pattern);
            }
            else
            {
                MainDigKeyPattern = @"(?<KEY>\d{2})\s(?<KEY_DIAL_TONE_FEATURE>\D{3})\s(?<DN>\d{1,10})\s{1}(?<CLID>\w+)(\s{3,}(?<MADN>MARP))?$";
                pattern = @"((KEY\s{2})|(\s{5}))(?<KEY>\d{2})\s(?<KEY_DIAL_TONE_FEATURE>\D{3})\s(?<DN>\d{1,10})\s{1}(?<CLID>\w+)\" +
                "s*$";
                pattern3 = @"((KEY\s{2})|(\s{5}))(?<KEY>\d{2})\s(?<KEY_DIAL_TONE_FEATURE>ACD)\s(?<ACD_PILOT>\d{1,10})\s{1}(?<CLID>" +
                @"\w+)\s{2,}(?<DN>\d{1,10})$";
                pattern4 = @"\s{5}(?<KEY>[0-9]){2}\s[DIG]{3}\s(?<INT_GRP>[0-9]+)\s(?<INT_GRP_DATA>[0-9]+\s[V,R]?)";

                if (Regex.IsMatch(line, MainDigKeyPattern))
                {
                    m = Regex.Match(line, MainDigKeyPattern);
                    associatedRecord["KEY"] = int.Parse(m.Groups["KEY"].Value);
                    associatedRecord["KEY_DIAL_TONE_FEATURE"] = m.Groups["KEY_DIAL_TONE_FEATURE"].Value;
                }
                else if (Regex.IsMatch(line, pattern))
                {
                    m = Regex.Match(line, pattern);
                    associatedRecord["KEY"] = int.Parse(m.Groups["KEY"].Value);
                    associatedRecord["KEY_DIAL_TONE_FEATURE"] = m.Groups["KEY_DIAL_TONE_FEATURE"].Value;
                }
                else if (Regex.IsMatch(line, pattern3))
                {
                    m = Regex.Match(line, pattern3);
                    associatedRecord["KEY"] = int.Parse(m.Groups["KEY"].Value);
                    associatedRecord["KEY_DIAL_TONE_FEATURE"] = m.Groups["KEY_DIAL_TONE_FEATURE"].Value;
                }
                else if (Regex.IsMatch(line, pattern4))
                {
                    m = Regex.Match(line, pattern4);
                    associatedRecord["KEY"] = int.Parse(m.Groups["KEY"].Value);
                    associatedRecord["INT_GRP"] = m.Groups["INT_GRP"].Value;
                    associatedRecord["INT_GRP_DATA"] = m.Groups["INT_GRP_DATA"].Value;
                    
                    pattern = @"(\s{5})(?<KEY>\d{2})\s(?<OPTIONS>[\w ]+)";
                    if (Regex.IsMatch(line, pattern))
                    {
                        m = Regex.Match(line, pattern);
                        associatedRecord["KEY"] = int.Parse(m.Groups["KEY"].Value);
                        associatedRecord["OPTIONS"] = m.Groups["OPTIONS"].Value.Trim();
                        return;
                    }
                    else
                    {
                        // Exit Sub
                    }

                }
                else
                {
                    pattern = @"(\s{5})(?<KEY>\d{2})\s(?<OPTIONS>[\w ]+)";
                    if (Regex.IsMatch(line, pattern))
                    {
                        m = Regex.Match(line, pattern);
                        associatedRecord["KEY"] = int.Parse(m.Groups["KEY"].Value);
                        associatedRecord["OPTIONS"] = m.Groups["OPTIONS"].Value.Trim();
                        return;
                    }
                    else
                    {
                        return;
                    }

                }

            }

            associatedRecord["DN"] = m.Groups["DN"].Value;
            associatedRecord[SwitchProperties.CS1000Fields.ACD_PILOT.ToString()] = m.Groups[SwitchProperties.CS1000Fields.ACD_PILOT.ToString()].Value;
            associatedRecord[SwitchProperties.CS1000Fields.CLID.ToString()] = m.Groups[SwitchProperties.CS1000Fields.CLID.ToString()].Value;
            
            if ((m.Groups[SwitchProperties.CS1000Fields.MADN.ToString()].Value.Trim() != ""))
            {
                associatedRecord[SwitchProperties.CS1000Fields.MADN.ToString()] = "Y";
            }
        }

        private string RenameColumnIfNecessary(string colName)
        {
            if (((colName == "CLS") || (colName == "FTR")))
            {
                colName = SwitchProperties.CS1000Fields.OPTIONS.ToString();
            }
            else if (((colName == "KEM") || ((colName == "DBA") || ((colName == "AOM") || (colName == "KBA")))))
            {
                colName = "ADDON";
            }
            return colName;
        }

        internal override string GetDelimeter(string fullFile)
        {
            string del = "";
            fullFile = fullFile.Replace("DES  ", "DESDES  ");

            if (Regex.IsMatch(fullFile, "\r\n\r\nDES"))
            {
                m_Records = Regex.Split(fullFile, "\r\n\r\nDES");
                del = "\r\n";
            }
            else if (Regex.IsMatch(fullFile, "\n\r\n\rDES"))
            {
                m_Records = m_Records = Regex.Split(fullFile, "\n\r\n\rDES");
                del = "\n\r";
            }
            else if (Regex.IsMatch(fullFile, "\n\nDES"))
            {
                m_Records = m_Records = Regex.Split(fullFile, "\n\nDES");
                del = "\n";
            }
            else if (Regex.IsMatch(fullFile, "\r\r\nDES"))
            {
                m_Records = m_Records = Regex.Split(fullFile, "\r\r\n\r\r\nDES");
                del = "\r\r\n";
            }
            else
            {
                // Parsing for switch data not correct if we do not look for the "DES" at the begining of each record because some record do have empty rows in the middle and should not be split there.
                m_Records = m_Records = Regex.Split(fullFile, "\r\r");
                del = "\r";
            }
            NumberOfRecords = m_Records.Length;
            return del;
        }
    }
}
