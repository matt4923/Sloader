using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using Sloader.Switches;

namespace Sloader.Parsing
{
    internal class Avaya_DispStat : SwitchImport
    {
        private string[] m_AllRecords;
        public Avaya_DispStat(StreamReader switchFile)
        {
            SwitchReport = new DataTable();
            DoParsing(switchFile);                   
        }

        internal override void ParseSwitchFile(object sender, DoWorkEventArgs e)
        {
            AddKnownColumnsToTable();
            ProgBar.Text = "Parsing Display Stations Report";
            foreach (string rec in m_AllRecords)
            {
                ProgBar.NewValue = Array.IndexOf(m_AllRecords, rec);
                if (!Regex.IsMatch(rec, @"^\sdisp stat")) { continue; }
                //New Display record
                ProcessLine(rec);
            }
        }

        private void AddKnownColumnsToTable()
        {
            foreach (string s in Enum.GetNames(typeof(SwitchProperties.AvayaFields)))
            {
                SwitchReport.Columns.Add(s);
            }
        }

       private string m_FieldNamePattern = @"^\[(?<row>\d{1,2})[^;]*;\d{1,2}H\s*(?<fieldName>[\040-\071\073-\176]+)[?:]\s$";
       private string m_ValuePattern = @"^\[(?<row>\d{1,2})[^;]*;\d{1,2}H\s*(?<value>[\040-\176]+)";
       private string m_FieldValuePattern = @"^\[(?<row>\d{1,2})[^;]*;\d{1,2}H\s*(?<fieldName>[\040-\071\073-\176]+)[?:]\s(?<value>[\040-\176]+)$";
       private List<DataRow> buttons = new List<DataRow>();
       private bool isSetFeature = false;
       private string cleanExt = "";
       private DataRow associatedBtn = null;
       private int btnType = 0;
       private string lastField;
       private bool skipToNextKey = false;
       private bool addNewBtn = false;
       private bool addingNonPrimaryButtonAssignments = false;
       private int lastRow = -1;
       private bool voiceMailOption = false;

        private void ProcessLine(string line)
       {            
            string piece = ""; //error handling 
            string DispRecord = ""; //used in error handling
            try
            {
                string[] parts = Regex.Split(line, @"\e");
                //#101102B When doing button assignements, certain feature might have multiple columns depending on feature. We want
                //to added the values from the columns as options to feature not create a new column for the data to go on the main set.
                DispRecord = parts[0];
                int p = 0;
                for (p = 0; p <= (parts.Length - 1); p++)
                {
                    piece = parts[p];
                    if (Regex.IsMatch(piece, m_FieldNamePattern))
                    {
                        //FIELD NAME
                        ParseFieldNamePiece(piece);
                    }
                    else if (Regex.IsMatch(piece, m_ValuePattern))
                    {
                        //VALUE
                        ParseValuePiece(piece, parts, p);
                    }
                    else if (Regex.IsMatch(piece, m_FieldValuePattern))
                    {
                        //FIELD NAME AND VALUE TOGETHER
                        ParseField_And_ValuePiece(piece);
                    }

                    if (addNewBtn)
                        buttons.Add(associatedBtn);

                    //& rewind
                    addNewBtn = false;
                    associatedBtn = null;
                    voiceMailOption = false;
                    //explicitly set this to false just in case 
                    if (parts[p].Contains("Command aborted"))
                    {
                        //we may have grabbed an extra record, if you see this exit for sure.
                        break; // TODO: might not be correct. Was : Exit For
                    }
                }

                if (SwitchReport.Columns.Contains(SwitchProperties.AvayaFields.Extension.ToString()))
                {
                    for (p = 0; p <= (buttons.Count - 1); p++)
                    {
                        if ((!object.ReferenceEquals(buttons[p][SwitchProperties.AvayaFields.Extension.ToString()], DBNull.Value))
                            || (SwitchReport.Columns.Contains(SwitchProperties.AvayaFields.OPTIONS.ToString()) && (!object.ReferenceEquals(buttons[p][SwitchProperties.AvayaFields.OPTIONS.ToString()], DBNull.Value))))
                            SwitchReport.Rows.Add(buttons[p]);
                    }
                }

            }
            catch (Exception ex)
            {
                //throw new ApplicationException("Failed to build table while parsing the following data: " + line, ex);
                string text = string.Format("Failed to parse record: '{0}' on part: '{1}'.  Error Message: {2}", DispRecord, piece, ex.Message);
            }
            finally
            {
                buttons.Clear();
            }
        }

        private void ParseValuePiece(string piece, string[] parts, int p)
        {
            //*******VALUE NAME*******

            associatedBtn = GetAssociatedButton(piece, lastField, btnType, buttons, lastField);

            Match m = Regex.Match(piece, m_ValuePattern);
            string value = m.Groups["value"].Value.Trim();
            //@Matt 2015Jun - 1509005 - Voicemail is on the phone not button 8!
            if (value == "voice-mail" && btnType == 0)
            {
                voiceMailOption = true;
                addingNonPrimaryButtonAssignments = false;
                return;
            }

            if (voiceMailOption)
            {
                //this is the voicemail number; just set number on the phone itself, toggle and move on.
                //buttons(0)("voice-mail") = value
                buttons[0][SwitchProperties.AvayaFields.VoiceMail.ToString()] = value;
                voiceMailOption = false;
                return;
            }

            if ((value == "STATION") || (value == "STATION OPTIONS") || (value == "SITE DATA") || (value == "ABBREVIATED DIALING")
                || (value == "HOT LINE DESTINATION"))
            {
                isSetFeature = false;
                return;
            }

            //#121012 Cairs 1227604
            if (value == "FEATURE OPTIONS")
            {
                //isSetFeature = true;
                return;
            }
            if (value == "ENHANCED CALL FORWARDING")
            {
                isSetFeature = false;
                return;
            }
            if ((value == "SIP FEATURE OPTIONS"))
            {
                isSetFeature = true;
                //lastField = value;
                //lastRow = -1;
                //addingNonPrimaryButtonAssignments = false;
                return;
            }
            if ((value == "BUTTON ASSIGNMENTS")) { btnType = 0; return; }
            if ((value == "SOFTKEY BUTTON ASSIGNMENTS")) { btnType = 1; return; }
            if ((value == "EXPANSION MODULE BUTTON ASSIGNMENTS") | (value.Contains("BUTTON MODULE")))
            {
                //btnType = Convert.ToInt32(Schema.SetTypeButton.ButtonType.Expansion);
                return;
                //btnType = 6
                //continue;
            }
            if ((value == "DISPLAY BUTTON ASSIGNMENTS")) { btnType = 8; return; }
            //What is 8?
            if ((value == "FEATURE BUTTON ASSIGNMENTS"))
            {
                //btnType = Convert.ToInt32(Schema.SetTypeButton.ButtonType.Feature);
                return;
            }

            if (string.IsNullOrEmpty(lastField))
                return;
            //#101102A Need more accurate way of going to next Key instead of j+=4 below if a special feature.
            if (skipToNextKey)
                return;

            //& this is the value to the old field
            if (lastField == "KEY")
            {
                //Dim dn As Integer
                //#101102A Not sure why this is here this causes ABRV-DIAL feature to not build correctly.
                //If (Integer.TryParse(value, dn)) Then
                //    associatedBtn("Extension") = dn
                //ElseIf
                if (value.Trim() == "call-appr")
                {
                    //& get the extension from the primary field
                    //#110627B Gemini #7397 Some Avaya switches have a numbers with dashes. Need to filter it out
                    //cleanExt = Strings.Replace(Convert.ToString(buttons(0)(Schema.Utility.SwitchReportFields.Avaya.ConfiguredSet.Extension.ToString)), "-", "");
                    cleanExt = Convert.ToString(buttons[0][SwitchProperties.AvayaFields.Extension.ToString()]).Replace("-", "");
                    associatedBtn[SwitchProperties.AvayaFields.Extension.ToString()] = cleanExt;
                    if (string.IsNullOrEmpty(Convert.ToString(associatedBtn[SwitchProperties.AvayaFields.PrimaryExtension.ToString()])))
                    {
                        associatedBtn[SwitchProperties.AvayaFields.PrimaryExtension.ToString()] = cleanExt;
                    }
                    skipToNextKey = true;
                }
                else if (value.Trim() == "brdg-appr")
                {
                    //& ex. [20:5Hbrdg-appr [20;15H Btn:[20;20H1 [20;23HExt:[20;27H3336
                    //#10112A ex. [18;5Hbrdg-appr [18;15H B:[18;18H2 [18;21HE:[18;23H74501        [18;37HR:[18;39Hr
                    //@Matt 9/14 #1426914 - This regex will now capture 4, 7, 10 digits with/without hyphens
                    Match brgMatch = Regex.Match(parts[p + 4].Trim(), @"\[\d{1,2};\d{1,2}H(?<dn>\d+(-\d{3,4}(-\d{4})?)?)");
                    if (string.IsNullOrEmpty(brgMatch.Value))
                    {
                        brgMatch = Regex.Match(parts[p + 4].Trim(), @"\[\d{1,2};\d{1,2}H(?<dn>\d+)");
                    }
                    cleanExt = Convert.ToString(brgMatch.Groups["dn"].Value).Replace("-", "");
                    associatedBtn[SwitchProperties.AvayaFields.Extension.ToString()] = cleanExt;
                    skipToNextKey = true;
                    //j += 4
                    //j += 6
                }
                else if (value.Trim() == "abrdg-appr")
                {
                    //& ex. [17:45Habrdg-appr[17;63HExt:[17;67H3333
                    //#10112A ex. [19;5Habrdg-appr[19;16H Ext: [19;22H27927        [19;36HRg:[19;39Hr
                    //Abrdg-aapr info is 2 fields behind not 4
                    //@Matt 9/14 #1426914 - This regex will now capture 4, 7, 10 digits with/without hyphens
                    Match brgMatch = Regex.Match(parts[p + 2].Trim(), @"\[\d{1,2};\d{1,2}H(?<dn>\d+(-\d{3,4}(-\d{4})?)?)");
                    if (string.IsNullOrEmpty(brgMatch.Value))
                    {
                        brgMatch = Regex.Match(parts[p + 2].Trim(), @"\[\d{1,2};\d{1,2}H(?<dn>\d+)");
                    }
                    cleanExt = Convert.ToString(brgMatch.Groups["dn"].Value).Replace("-", "");
                    associatedBtn[SwitchProperties.AvayaFields.Extension.ToString()] = cleanExt;
                    skipToNextKey = true;
                    //j += 2
                    //j += 4
                }
                else
                {
                    //#150415 UCS 1507501 Not a directory number, remove the Name copied from main record for this row.
                    associatedBtn[SwitchProperties.AvayaFields.Name.ToString()] = "";

                    if (!SwitchReport.Columns.Contains(SwitchProperties.AvayaFields.OPTIONS.ToString()))
                    {
                        SwitchReport.Columns.Add(SwitchProperties.AvayaFields.OPTIONS.ToString());
                    }

                    //& options
                    if (ReferenceEquals(associatedBtn[SwitchProperties.AvayaFields.OPTIONS.ToString()], DBNull.Value))
                    {
                        associatedBtn[SwitchProperties.AvayaFields.OPTIONS.ToString()] = value;
                    }
                    else
                    {
                        associatedBtn[SwitchProperties.AvayaFields.OPTIONS.ToString()] = (associatedBtn[SwitchProperties.AvayaFields.OPTIONS.ToString()] + " " + value);
                    }

                }
            }
            else
            {
                //#121012 Cairs 1227604
                if (isSetFeature)
                {
                    if (ReferenceEquals(associatedBtn[SwitchProperties.AvayaFields.OPTIONS.ToString()], DBNull.Value))
                    {
                        associatedBtn[SwitchProperties.AvayaFields.OPTIONS.ToString()] = value;
                    }
                    else
                    {
                        associatedBtn[SwitchProperties.AvayaFields.OPTIONS.ToString()] = Convert.ToString(associatedBtn[SwitchProperties.AvayaFields.OPTIONS.ToString()]) + " " + value;
                    }
                }
                else
                {
                    //#110627B Gemini #7397 Some Avaya switches have a numbers with dashes. Need to filter it out
                    if (lastField == SwitchProperties.AvayaFields.Extension.ToString())
                    {
                        cleanExt = value.Replace("-", "");
                        associatedBtn[SwitchProperties.AvayaFields.Extension.ToString()] = cleanExt;
                        associatedBtn[SwitchProperties.AvayaFields.PrimaryExtension.ToString()] = cleanExt;
                        //#130301 For Display Data records number is located in "Data Extension" field not "Extension" field. 
                    }
                    else if (lastField == "Data_Extension")
                    {
                        cleanExt = value.Replace("-", "");
                        associatedBtn[lastField] = cleanExt;
                        //If "Extension" field is not populated row will not build. Force field update next.
                        associatedBtn[SwitchProperties.AvayaFields.Extension.ToString()] = cleanExt;
                        if (string.IsNullOrEmpty(Convert.ToString(associatedBtn[SwitchProperties.AvayaFields.PrimaryExtension.ToString()])))
                        {
                            associatedBtn[SwitchProperties.AvayaFields.PrimaryExtension.ToString()] = cleanExt;
                        }
                    }
                    else
                    {
                        //#121012 Cairs 1227604 Field after "ENHANCED CALL FORWARDING" should not be treated as values, they do not exist so should be skipped here.
                        if (associatedBtn.Table.Columns.Contains(lastField))
                        {
                            associatedBtn[lastField] = value;
                        }
                    }
                }
            }
        }

        private void ParseField_And_ValuePiece(string piece)
        {
            //*******FIELD & VALUE NAME*******
            Match m = Regex.Match(piece, m_FieldValuePattern);
            string fieldName = m.Groups["fieldName"].Value.Trim();
            string value = m.Groups["value"].Value.Trim();

            if (!SwitchReport.Columns.Contains(fieldName))
            {
                SwitchReport.Columns.Add(fieldName);
            }

            associatedBtn = GetAssociatedButton(piece, fieldName, btnType, buttons, lastField);

            associatedBtn[fieldName] = value;

        }

        private void ParseFieldNamePiece(string piece)
        {
            //*******FIELD NAME*******
            Match m = Regex.Match(piece, m_FieldNamePattern);

            string fieldName = m.Groups["fieldName"].Value.Trim();

            if (isSetFeature)
            {
                fieldName = fieldName.Replace(" ", "_");
            }
            //& get the button for this line
            associatedBtn = GetAssociatedButton(piece, fieldName, btnType, buttons, lastField);

            int key = 0;
            if (buttons.Count == 0)
            {
                associatedBtn[SwitchProperties.AvayaFields.KEY.ToString()] = 0;
                associatedBtn[SwitchProperties.AvayaFields.KEY_TYPE.ToString()] = 0;
                associatedBtn[SwitchProperties.AvayaFields.MADN.ToString()] = "Y";
                addNewBtn = true;
            }
            else if (piece.Contains(":") && (int.TryParse(fieldName, out key)))
            {
                lastRow = int.Parse(m.Groups["row"].Value.Trim());
                isSetFeature = true;
            }
            else if ((lastRow != -1) && (lastRow == int.Parse(m.Groups["row"].Value.Trim())) || addingNonPrimaryButtonAssignments)
            {
                //& option parameters ex. [13;1H 8: [13;5Habr-spchar[13;15H Char: [13;22H~p
                //& don't do anything with it
                return;
                //continue;
            }

            fieldName = fieldName.Replace(" ", "_");

            if (isSetFeature)
            {
                if (ReferenceEquals(associatedBtn[SwitchProperties.AvayaFields.OPTIONS.ToString()], DBNull.Value))
                {
                    associatedBtn[SwitchProperties.AvayaFields.OPTIONS.ToString()] = fieldName;
                }
                else
                {
                    associatedBtn[SwitchProperties.AvayaFields.OPTIONS.ToString()] = Convert.ToString(associatedBtn[SwitchProperties.AvayaFields.OPTIONS.ToString()]) + "~~~" + fieldName;
                }
            }
            else
            {
                if (!SwitchReport.Columns.Contains(fieldName))
                {
                    SwitchReport.Columns.Add(fieldName);
                }
            }

            lastField = fieldName;
        }

        private DataRow GetAssociatedButton(string reportLine, string fieldName, int btnType, List<DataRow> buttons, string lastField)
        {

            DataRow associatedBtn = null;

            int key = 0;
            if (buttons.Count == 0)
            {
                // analog
                associatedBtn = SwitchReport.NewRow();
            }
            else if (lastField == "KEY")
            {
                associatedBtn = buttons[buttons.Count - 1];
            }
            else
            {
                associatedBtn = buttons[0];
            }

            return associatedBtn;
        }

        internal override string GetDelimeter(string fullFile)
        {
            string del = "";
            if (Regex.IsMatch(fullFile, @"\[24;1H\e\[KCommand:"))
            {
                m_AllRecords = Regex.Split(fullFile, @"\[24;1H\e\[KCommand:");
                del = "\r\n";
            }
            NumberOfRecords = m_AllRecords.Length;
            return del;
        }
    }
}