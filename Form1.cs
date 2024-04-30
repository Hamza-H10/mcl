using InclinoView.My.Resources;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using NodaTime;
using NodaTime.Text;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LiveCharts.WinForms;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Reflection;
//using System.Windows.Controls;





namespace mcl
{
    /// <summary>
    /// This class represents the main form of the application.
    /// </summary>
    /// <author>Hamza</author>
    /// <date>2023-09-10</date>
    public partial class Form1
    {
        // Class-level fields for managing data
        private List<GlobalCode.BoreHole> listBH;
        private short bhIndex = -1;
        private short boreHoleSelected = 0;
        private short _axisValue = 0;
        private string bsTextPrintData;
        private Font printFont;

        /// <summary>
        /// This method loads the main form of the application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public Form1()
        {
            InitializeComponent();
            this.AutoScaleMode = AutoScaleMode.Dpi;//for scalability issues in different DPI, windows 10,11 
        }

        //  s the list of boreholes in the user interface
        private void ReloadList()
        {
            Console.WriteLine("Inside ReloadList function");
            // Clear the list of boreholes
            lstBoreholes.Items.Clear();

            // Check if a specific borehole is selected
            if (boreHoleSelected == 0)
            {
                // Retrieve the list of boreholes from the database
                listBH = GlobalCode.GetBoreholes();

                // Populate the list box with borehole information // in this the name of the channel is forming and its details are showing in the 
                foreach (var bitem in listBH)
                    //lstBoreholes.Items.Add("[" + bitem.Id.ToString("D2") + "] " + bitem.SiteName + " - " + bitem.Location);
                    //lstBoreholes.Items.Add("[" + bitem.Id.ToString("D2") + "] " + bitem.ChNo + " - " + bitem.Unit);
                    lstBoreholes.Items.Add("channel " + bitem.Id.ToString("D2") + bitem.Unit);

                // Configure list box selection mode and toolbar
                lstBoreholes.SelectionMode = SelectionMode.One;
                bool argenb = false;
                ToolBarEnable(ref argenb);
            }
            else
            {	// get directory listing
                // Get a listing of CSV files in the selected borehole directory
                var di = new System.IO.DirectoryInfo(GlobalCode.GetBoreholeDirectory(ref boreHoleSelected));
                Console.WriteLine("di: " + di);
                System.IO.FileInfo[] aryFi = di.GetFiles("*.csv");//add an exception handling here if the files are missing then it should prompt user.

                // Populate the list box with CSV file names
                foreach (var fi in aryFi)
                    lstBoreholes.Items.Add(fi.Name);

                // Configure list box selection mode and toolbar
                lstBoreholes.SelectionMode = SelectionMode.MultiSimple;
                bool argenb1 = true;
                ToolBarEnable(ref argenb1);
            }

            // Reset labels and hide chart and DataGridView
            ResetLabels();
            CartesianChart1.Visible = false;
            DataGridView1.Visible = false;
            ToolStrip2.Enabled = false;
            //toolStripSplitButton1.Enabled = false;


            // Reset the state
            //is_MM = false; // or true, depending on what you consider the initial state

            // Reset properties - Update the button text, color based on the reset state
            //toolStripSplitButton1.Text = is_MM ? "MM": "DEG";
            toolStripSplitButton1.Text = null;
            toolStripSplitButton1.BackgroundImage = null; // or set to initial image
            toolStripSplitButton1.BackgroundImageLayout = ImageLayout.None;
            toolStripSplitButton1.BackColor = is_MM ? Color.Cyan : Color.LightGreen; // or set to initial color

            // Reset other properties and states as needed...

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Set label colors
            Label1.ForeColor = System.Drawing.Color.FromArgb(33, 149, 242);
            Label2.ForeColor = System.Drawing.Color.FromArgb(243, 67, 54);
            Label3.ForeColor = System.Drawing.Color.FromArgb(254, 192, 7);
            Label4.ForeColor = System.Drawing.Color.FromArgb(96, 125, 138);
            Label5.ForeColor = System.Drawing.Color.FromArgb(0, 187, 211);
            label7.ForeColor = System.Drawing.Color.FromArgb(255, 20, 147);
            label8.ForeColor = System.Drawing.Color.FromArgb(255, 69, 0);

            // Open the application's database
            GlobalCode.OpenDatabase();
            // _DeleteAllBoreholes() ' temporary delete all
            //tbGraphType.SelectedIndex = 0;

            // Load the list of boreholes
            ReloadList();
        }

        private void FrmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Close the application's database when the form is closed
            GlobalCode.CloseDatabase();
        }

        //============================================================================================================================ 
        private void tbImport_Click(object sender, EventArgs e)
        {
            // Initialize counters to keep track of import results
            short cnt = 0;          // Counter for successfully imported files
            short cntError = 0;     // Counter for files with incorrect format
            short cntRepeat = 0;    // Counter for files already imported

            // Prepare a message string to summarize the import process
            string msgString = "Import Summary:" + Environment.NewLine;
            string msgStringImport = "Import Info:" + Environment.NewLine;

            // Check if the user selected files using the OpenFileDialog
            if (OpenFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // Loop through each selected file
                foreach (string strFileName in OpenFileDialog1.FileNames)
                {
                    // Create a temporary file name and extract the file name
                    string tempFileName = strFileName;
                    string strFileNew = strFileName.Split('\\').Last();


                    // Check if the file extension is "csv" (case-insensitive)
                    if (CultureInfo.CurrentCulture.CompareInfo.Compare(strFileNew.Split('.').Last().ToLower(), "csv", CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0)
                    {
                        // Read the CSV file data into a two-dimensional string array
                        string[][] strData = GlobalCode.ReadCSVFile(ref tempFileName);

                        // Check if the CSV data has a minimum number of rows
                        if (strData.Length < 5)
                        {
                            cntError = (short)(cntError + 1);
                            continue; // Skip processing this file if it doesn't meet the minimum row requirement
                        }


                        // Catch 4 parameters for the new borehole
                        // Parse the borehole number, directory name, and depth from the CSV data
                        //short borehole_num;
                        //float depth;
                        //string strDirName;

                        // Initialize a dictionary to hold channel data
                        Dictionary<int, List<string[]>> channelData = new Dictionary<int, List<string[]>>();

                        short borehole_num = 0;
                        //float depth = 0.0f;
                        string strDirName = string.Empty;
                        //short channelNumber = 0;
                        string unit = "";

                        try
                        {
                            // Initialize lists to store channel numbers and their associated units
                            List<int> channelNumbers = new List<int>();
                            List<string> units = new List<string>();
                            Dictionary<int, string> channelUnits = new Dictionary<int, string>();


                            // Loop through each line in the CSV data
                            foreach (string[] row in strData)
                            {

                                // Print the current row
                                Console.WriteLine("Current Row:\n" + string.Join("\n", row));


                                // Skip the header row
                                //if (row[0] == "DATE" && row[1] == "TIME")
                                if (row[0] == "DATE   TIME")
                                    continue;


                                // Extract channel number and unit from the columns
                                //channelNumber = short.Parse(row[2].Trim());
                                //channelNumber = short.Parse(row[1].Trim());
                                //unit = row[4].Trim();

                                // Initialize channelNumber and unit variables
                                short parsedChannelNumber;
                                string parsedUnit;

                                // Extract channel number from the row
                                if (!int.TryParse(row[1].Trim(), out int channelNumber))
                                    continue; // Skip the row if channel number parsing fails

                                // Check if the channel number already exists in the dictionary
                                if (!channelData.ContainsKey(channelNumber))
                                {
                                    // If the channel number doesn't exist, add it to the dictionary with an empty list
                                    channelData[channelNumber] = new List<string[]>();
                                }

                                // Add the current row to the list of rows for the corresponding channel number
                                channelData[channelNumber].Add(row);
                                //}
                                //--------------------------------------------   

                                // Check if the values can be parsed and are not null or empty
                                //if (row.Length < 2 || !short.TryParse(row[1].Trim(), out parsedChannelNumber) ||
                                //        string.IsNullOrEmpty(row[4].Trim()))
                                //    {
                                //        // Skip adding null values and move to the next execution
                                //        continue;
                                //    }

                                //strDirName = GlobalCode.GetBoreholeDirectory(ref parsedChannelNumber);

                                // Assign the parsed values
                                //channelNumber = parsedChannelNumber;

                                //unit = row[4].Trim();// ADD EXCEPTION FOR NULL VALUES HERE
                                //unit = row[4]?.Trim() ?? "NA";
                                unit = (row.Length > 4 ? row[4]?.Trim() : null) ?? "NO INPUT";



                                // Add channel number to the list if it's not already present
                                if (!channelNumbers.Contains(channelNumber))
                                {
                                    channelNumbers.Add(channelNumber);
                                    units.Add(unit);
                                    channelUnits.Add(channelNumber, unit);
                                    Console.WriteLine("channelNumbers: ", channelNumber);
                                }
                            }

                            Console.WriteLine("Channel Numbers:\n" + string.Join("\n", channelNumbers));//string.join method internally iterates over all the elements of the list


                            Console.WriteLine("\nChannel Units:\n" + string.Join("\n", channelUnits.Select(entry => $"Channel {entry.Key}: {entry.Value}")));

                            //-------------------------------------------------------------
                            //borehole_num = short.Parse(strData[0][1]);
                            //borehole_num = short.Parse(strData[1][1]);
                            //borehole num ki jagah channel no.s use honga jo ki channelnumber list se loge foreach loop laga k 

                            //strDirName = GlobalCode.GetBoreholeDirectory(ref borehole_num);
                            //strFileNew = strDirName + @"\" + strFileNew;
                            // Iterate over the channel data dictionary
                            foreach (var kvp in channelData)
                            {
                                //short i = (short)kvp.Key;

                                short channelNumber = (short)kvp.Key;
                                List<string[]> channelRows = kvp.Value;

                                // Generate directory name based on channel number
                                string channelDirName = Path.Combine(GlobalCode.GetBoreholeDirectory(ref channelNumber));

                                // Check if the directory exists; if not, create it
                                if (!Directory.Exists(channelDirName))
                                {
                                    Directory.CreateDirectory(channelDirName);
                                }

                                // Initialize a dictionary to track existing dates for the current channel
                                HashSet<string> existingDates = new HashSet<string>();

                                // Construct full file path for the sub-file
                                //string subFileName = Path.Combine(channelDirName, $"Channel_{kvp.Key}.csv");

                                // Iterate through the rows to process date-time values and copy data to sub-files
                                foreach (string[] row in channelRows)
                                {
                                    // Extract the "DATE   TIME" value
                                    string dateTimeValue = row[0];

                                    // Extract only the date part from the "DATE   TIME" value
                                    string date = dateTimeValue.Split()[0]; // Split by space and take the first part as the date

                                    // Check if the sub-file for the current date already exists for the current channel
                                    string subFileName = Path.Combine(channelDirName, $"Channel_{channelNumber}_{date}.csv");
                                    if (!File.Exists(subFileName) && !existingDates.Contains(date))
                                    {
                                        // Write the channel data for the current date to the sub-file
                                        using (StreamWriter writer = new StreamWriter(subFileName))
                                        {
                                            // Write header row
                                            writer.WriteLine(string.Join(",", strData[0]));

                                            // Write data rows for the current date
                                            foreach (string[] rowData in channelRows)
                                            {
                                                // Check if the "DATE   TIME" value matches the current date
                                                if (rowData[0].StartsWith(date)) // Check if the date part matches
                                                {
                                                    writer.WriteLine(string.Join(",", rowData));
                                                }
                                            }
                                        }

                                        // Add the current date to the existing dates set
                                        existingDates.Add(date);
                                    }
                                }
                            }



                            foreach (int i in channelNumbers)
                            {
                                short crntChnlNmbr = (short)i; // Declare a separate variable and assign the value
                                string unit_ = channelUnits[crntChnlNmbr]; // Assuming the unit is stored as a string in the dictionary


                                // Generate directory name based on channel number
                                strDirName = Path.Combine(GlobalCode.GetBoreholeDirectory(ref crntChnlNmbr));


                                // Check if the directory exists; if not, create it
                                if (!Directory.Exists(strDirName))
                                {
                                    Directory.CreateDirectory(strDirName);
                                }

                                // Construct full file path
                                //strFileNew = Path.Combine(strDirName, Path.GetFileName(strFileName));

                                // Check if the file already exists (if imported previously)
                                if (File.Exists(strFileNew))
                                {
                                    cntRepeat++;
                                    continue;
                                }

                                // Copy the selected file to the destination directory
                                //File.Copy(strFileName, strFileNew);

                                // Create a new BoreHole object and add/update it
                                //var bh = new GlobalCode.BoreHole() { Id = borehole_num, ChNo = channelNumber.ToString(), Unit = unit, BaseFile = "" };
                                var bh = new GlobalCode.BoreHole() { Id = crntChnlNmbr, Unit = unit_, BaseFile = "" };

                                // Add or update the BoreHole in the application
                                if (!GlobalCode.AddBorehole(ref bh))
                                {
                                    GlobalCode.UpdateBorehole(ref bh);
                                }

                                cnt++;
                                msgStringImport += "Imported " + cnt + " CSV file(s) to the mcl.\n";
                                //MessageBox.Show(msgStringImport, "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                //SplitCSVDataIntoSubFiles(strData, strDirName);

                                // Reload the list
                                ReloadList();

                            }
                        }
                        catch (Exception ex)
                        {
                            // Display the error message to the user
                            MessageBox.Show("File format is not correct. Please check the format of file data. Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return; // Stop code execution
                        }
                    }
                }

                // Prepare a summary message with import results

                if (cntError > 0)
                    msgString += cntError + " file(s) were found to be incorrect format.\n";
                if (cntRepeat > 0)
                    msgString += cntRepeat + " file(s) were already imported into the application, hence ignored.\n";

                // Display the summary message to the user
                Console.WriteLine(msgString);

                // Show the MessageBox only if there are errors or repeated files
                if (cntError > 0 || cntRepeat > 0)
                    MessageBox.Show(msgString, "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }


        private void SplitCSVDataIntoSubFiles(string[][] strData, string strDirName)
        {
            
        }

        private void tbBack_Click(object sender, EventArgs e)
        {
            if (boreHoleSelected > 0)
            {
                boreHoleSelected = 0;
                bhIndex = -1;
                ReloadList();
            }
        }
        private void lstBoreholes_DoubleClick(object sender, EventArgs e)
        {
            if (lstBoreholes.SelectedIndex < 0)
                return;
            if (boreHoleSelected == 0)
            {
                bhIndex = (short)lstBoreholes.SelectedIndex;
                boreHoleSelected = listBH[bhIndex].Id;
                ReloadList();
            }
            else
            {
                DataGridView1.Visible = false;
                CartesianChart1.Visible = false;
                DisplayReport();
                // lstboreholes.selecteditem is a CSV file
            }
        }

        //-----------------------------------------------------------------------------------------------------DISPLAY REPORT FOR IMPORTED FILES old code here 

        //----------------------------------------------------------------------------------------------------------------

        private void DisplayReport(bool bnLoadText = false) //DISPLAY REPORT FUNCTION FOR THE SUBFILES
        {
            //var ds = new DataTable(); // Create a DataTable to hold the report data
            var strBaseData = default(string[][]); // Store data from a base file
            var bnBaseFilePresent = default(bool); // Flag indicating if a base file is present

            short i;

            //-----------------------------------------------------------------------------------------------------
            //FOR BASE FILE UNCOMMENT THIS WHEN IMPLEMENT FOR BASEfile

            // Reset labels and prepare for report generation
            if (listBH[bhIndex].BaseFile is null | string.IsNullOrEmpty(listBH[bhIndex].BaseFile))
            {
                bnBaseFilePresent = false; // No base file is present
                Label6.Text = "";
            }
            else
            {
                // Construct the path to the base file
                string strFile = GlobalCode.GetBoreholeDirectory(ref boreHoleSelected) + @"\" + listBH[bhIndex].BaseFile;


                if (System.IO.File.Exists(strFile))
                {
                    // Read data from the base file
                    strBaseData = GlobalCode.ReadCSVFile(ref strFile);
                    bnBaseFilePresent = true; // Base file is present
                    Label6.Text = "Base File : " + listBH[bhIndex].BaseFile.Split('.').First().Replace("_", ":");
                }
                else
                {
                    // Display a message if the base file does not exist
                    Interaction.MsgBox("Base file does not exist. It must have been deleted. Please select another file as base.", Constants.vbOKOnly | Constants.vbExclamation, "Graph");
                }
            }

            //------------------------------------------------------------------------------------------------------
            Label1.Text = lstBoreholes.SelectedItem.ToString().Split('.').First().Replace("_", ":");

            // Construct the path to the selected data file
            string argFileName = Conversions.ToString(Operators.ConcatenateObject(GlobalCode.GetBoreholeDirectory(ref boreHoleSelected) + @"\", lstBoreholes.SelectedItem));
            Console.WriteLine("argFileName: " + argFileName);

            // Read the CSV file
            string[][] strData = GlobalCode.ReadCSVFile(ref argFileName);

            //foreach (string[] row in strData)
            //{
            //    Console.WriteLine(string.Join("\t", row));
            //}

            // Create LocalDateTimePattern objects
            LocalDateTimePattern pattern1 = LocalDateTimePattern.CreateWithInvariantCulture("dd/MM/yyyy HH:mm");
            LocalDateTimePattern pattern2 = LocalDateTimePattern.CreateWithInvariantCulture("dd-MM-yyyy HH:mm");
            LocalDateTimePattern pattern3 = LocalDateTimePattern.CreateWithInvariantCulture("dd-MMM-yy HH:mm");//also tried "dd-MMM-yy HH:mm"
            
            string format = "dd-MMM-yy HH:mm"; // Custom format for "12-APR-24 12:7:54"

            // Create columns in the DataTable to hold the report data
            DataTable ds = new DataTable();
            //ds.Columns.Add("DateTime", typeof(LocalDateTime)); // Add a LocalDateTime column
            ds.Columns.Add("DateTime", typeof(string)); // Add a LocalDateTime column
            //ds.Columns.Add("Sensor", typeof(int));
            //ds.Columns.Add("Depth", typeof(float));
            //ds.Columns.Add("A", typeof(float));
            //ds.Columns.Add("B", typeof(float));

            ds.Columns.Add("ChNo", typeof(int));
            ds.Columns.Add("Value", typeof(float));
            ds.Columns.Add("Unit", typeof(string));

            
            var loopTo = (short)(strData.Length - 1);
            
            for (int index = 0; index <= loopTo; index++)
            {
                if (strData[index].Length < 5)
                {
                    Console.WriteLine($"Error at row {index + 1}: Insufficient data columns");
                    continue;
                }

                try
                {
                    
                    Console.WriteLine("Current Row:\n" + string.Join("\n", index));

                    string dateTimeString = strData[index][0];

                    Console.WriteLine($"Processing row {index + 1}: {dateTimeString}");

                    
                    //if (row[0] == "DATE" && row[1] == "TIME")
                    if (strData[index][0] == "DATE   TIME")
                        continue;

                    
                    LocalDateTime dateTimeValue;
                    //LocalDateTime parsedDateTime;
                    //IPattern<LocalDate> datePattern = NodaTime.Patterns.CreateLocalDatePattern("dd-MMM-yy"); // Pattern for date part
                    //IPattern<LocalTime> timePattern = Patterns.CreateLocalTimePattern("HH:mm");  // Pattern for time part
                    //// Parse the date and time components separately
                    //LocalDate parsedDate = datePattern.Parse(dateTimeString.Split(' ')[0]).GetValueOrException();
                    //LocalTime parsedTime = timePattern.Parse(dateTimeString.Split(' ')[1]).GetValueOrException();
                    //parsedDateTime = parsedDate + parsedTime;

                    //        pattern1.Parse(strData[index][0]).TryGetValue(default(LocalDateTime), out dateTimeValue) ||
                    //pattern2.Parse(strData[index][0]).TryGetValue(default(LocalDateTime), out dateTimeValue) ||
                    //if (pattern3.Parse(strData[index][0]).TryGetValue(default(LocalDateTime), out dateTimeValue))
                    //DateTime parsedDateTime = DateTime.ParseExact(dateTimeString, format, CultureInfo.InvariantCulture);
                    {
                        var datetimeStr = strData[index][0];
                        int intValue = int.Parse(strData[index][1]);
                        float floatValue = float.Parse(strData[index][3]);
                        var unitValue = strData[index][4];

                        // Add the row to the DataTable
                        //ds.Rows.Add(new object[] { dateTimeValue, intValue1, intValue2, floatValue1, floatValue2 });
                        ds.Rows.Add(new object[] { datetimeStr, intValue, floatValue, unitValue });
                    }
            
            {
                Console.WriteLine($"Error parsing date at row {index + 1}: {strData[index][0]}");
            }
        }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error at row {index + 1}: {ex.Message}");
                }
            }

            //--------------------------------------------------------------------------------------------------------------------------------------
            if (bnLoadText)
            {
                // Prepare text data for loading (if needed)
                int row;
                string strItem;

                var loopTo1 = (short)(ds.Columns.Count - 1);
                Console.WriteLine(loopTo1);
                for (i = 0; i <= loopTo1; i++)
                {
                    if (i > 6)
                    {
                        bsTextPrintData += ds.Columns[i].ColumnName.PadLeft(12);
                        Console.WriteLine(bsTextPrintData);
                    }
                    else
                    {
                        bsTextPrintData += ds.Columns[i].ColumnName.PadLeft(8);
                        Console.WriteLine(bsTextPrintData);
                    }
                }
                bsTextPrintData += Constants.vbCrLf;
                bsTextPrintData += "".PadRight(104, '=') + Constants.vbCrLf;

                var loopTo2 = ds.Rows.Count - 1;
                Console.WriteLine(loopTo2);
                for (row = 0; row <= loopTo2; row++)
                {
                    var loopTo3 = (short)(ds.Columns.Count - 1);
                    Console.WriteLine(loopTo3);
                    for (i = 0; i <= loopTo3; i++)
                    {
                        strItem = "";

                        if (ds.Rows[row][i] != DBNull.Value)
                        {
                            // Check if the value is a DateTime
                            if (ds.Rows[row][i] is DateTime)
                            {
                                // Handle DateTime values by formatting them as a string
                                strItem = ((DateTime)ds.Rows[row][i]).ToString("yyyy-MM-dd HH:mm:ss");
                            }
                            else if (ds.Rows[row][i] is decimal || ds.Rows[row][i] is int)
                            {
                                // Handle numeric values by formatting them as a number
                                strItem = Strings.FormatNumber(ds.Rows[row][i], 2);
                            }
                            else
                            {
                                // Handle other data types here (e.g., leave as-is or apply custom logic)
                                strItem = ds.Rows[row][i].ToString();
                            }
                        }

                        if (i > 6)
                        {
                            bsTextPrintData += "  " + strItem.PadLeft(8) + "  ";
                        }
                        else
                        {
                            bsTextPrintData += " " + strItem.PadLeft(6) + " ";
                        }
                    }
                    bsTextPrintData += Constants.vbCrLf;
                }


                /*var loopTo2 = ds.Rows.Count - 1;            
                for (row = 0; row <= loopTo2; row++)
                {
                    var loopTo3 = (short)(ds.Columns.Count - 1);                 
                    for (i = 0; i <= loopTo3; i++)
                    {
                        strItem = "";
                        if (ds.Rows[row][i] is not DBNull)//DBNull cannot be inherited
                        {
                            strItem = Strings.FormatNumber(ds.Rows[row][i], 2);
                            Console.WriteLine(strItem);
                        }
                        if (i > 6)
                        {
                            bsTextPrintData += "  " + strItem.PadLeft(8) + "  ";
                            //bsTextPrintData += "  " + FormatDateTime(ds.Rows[row][i]).PadLeft(8) + "  ";
                        }
                        else
                        {
                            bsTextPrintData += " " + strItem.PadLeft(6) + " ";
                            //bsTextPrintData += " " + FormatDateTime(ds.Rows[row][i]).PadLeft(6) + " ";
                        }
                    }
                    bsTextPrintData += Constants.vbCrLf;                
                }*/
            }
            else
            {
                // Display the report in a DataGridView (if not loading text)
                DataGridView1.DataSource = ds;
                DataGridView1.Visible = true;

                //Set column widths
                var loopTo4 = (short)(DataGridView1.Columns.Count - 1);
                Console.WriteLine(loopTo4);
                for (i = 0; i <= loopTo4; i++)
                {
                    DataGridView1.Columns[i].Width = (i > 6) ? 100 : 100;

                    //DataGridView1.Columns["Sensor"].DefaultCellStyle.Format = "D"; // "D" format for integers

                    // Set the DateTime format for the "DateTime" column                
                    if (DataGridView1.Columns[i].Name == "DateTime")
                    {
                        DataGridView1.Columns[i].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";
                    }
                }
                // Configure DataGridView appearance
                DataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 11f, FontStyle.Bold | FontStyle.Italic);
                DataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                //---------------------------
                // Change the background color of the rows
                DataGridView1.RowsDefaultCellStyle.BackColor = Color.WhiteSmoke;

                //DataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.LightGoldenrodYellow;
                DataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.LightCyan;
                //----------------------------

                // Change the background color of the entire DataGridView
                DataGridView1.BackgroundColor = Color.WhiteSmoke;

                // Change the background color of selected cells
                DataGridView1.DefaultCellStyle.SelectionBackColor = Color.LightCoral;
                DataGridView1.DefaultCellStyle.SelectionForeColor = Color.White;


                // Enable or disable ToolStrip buttons based on conditions
                if (!ToolStrip2.Enabled)
                    ToolStrip2.Enabled = true;

                //PrintToolStripButton.Enabled = True
                tbAxisX.Enabled = false;
                tbAxisY.Enabled = false;
                tbZoom.Enabled = false;
                toolStripSplitButton1.Enabled = false;
                //tbGraphType.Enabled = false;
            }
        }
        //----------------------------------------------------------------------------------------------------------------------

        //--------------------------------------------------------------------------------------------------------------------------------
        private void DisplayGraph_Channel()
        {
            // Initialize variables
            var cnt = default(short); // Counter for labels
            double maxX = 50.0d; // Maximum X value for the chart
            var strBaseData = default(string[][]); // Array to store base d
            //ResetToolStripSplitButton1(); //this will reset the state of this button


            // Create a collection to store axis sections
            var axisSectionSeries = new SectionsCollection
            {
                // Add a new axis section to the collection
                new AxisSection()
                {
                    // Set the width of the section (0d means it covers the entire axis)
                    SectionWidth = 0d,
                    // Set the thickness of the stroke (border) for the section
                    StrokeThickness = 0.5d,
                    // Set the color of the stroke (border) for the section (DarkGray in this case)
                    Stroke = System.Windows.Media.Brushes.DarkGray,
                    // Set the value at which the section is positioned on the axis (0d in this case)
                    Value = 0d
                }
            };


            // Create Y-axis with label formatter and styling
            var YAxis = new Axis()
            {
                // Specify a custom label formatter using a lambda function

                Title = "Values",

                MaxValue = 5d,
                MinValue = -5d,
                // Configure a separator for the Y-axis
                Separator = new Separator()
                {
                    // Enable the separator
                    IsEnabled = true,
                    // Set the stroke thickness (line thickness) for the separator
                    Step = 1.0d,
                    StrokeThickness = 1d
                },

                Sections = axisSectionSeries // Add axis sections
            };


            // Create X-axis with label formatter, range, and styling
            var XAxis = new Axis()
            {
                Title = "DateTime",
                //LabelFormatter = new Func<double, string>(y => Math.Round(y, 2).ToString()),

                LabelFormatter = value => DateTime.FromOADate(value).ToString(), // Convert OLE Automation Date back to DateTime

                Separator = new Separator()
                {
                    IsEnabled = true,
                    Step = 0.5d,
                    StrokeThickness = 1d
                },

            };

            // Create a collection to hold chart series
            var seriesCollection = new SeriesCollection();

            // Reset labels and configure chart
            ResetLabels();

            CartesianChart1.BackColor = System.Drawing.Color.White;
            CartesianChart1.Zoom = ZoomingOptions.Xy;
            CartesianChart1.Series.Clear();
            CartesianChart1.AxisX.Clear();
            CartesianChart1.AxisY.Clear();
            CartesianChart1.AxisY.Add(YAxis);
            CartesianChart1.AxisX.Add(XAxis);

            if (DataGridView1.Visible)
                DataGridView1.Visible = false;

            if (CartesianChart1.Visible == false)
                CartesianChart1.Visible = true;

            if (!ToolStrip2.Enabled)
                //ToolStrip2.Enabled = true;
                // PrintToolStripButton.Enabled = False

                //tbAxisX.Enabled = true;
                //tbAxisY.Enabled = true;
                //tbZoom.Enabled = true;
                tbAxisX.Enabled = false;

            foreach (string lstItem in lstBoreholes.SelectedItems)
            {
                Console.WriteLine("Inside foreach loop");
                // Get the path to the current file
                string argFileName = GlobalCode.GetBoreholeDirectory(ref boreHoleSelected) + @"\" + lstItem;
                string[][] strData = GlobalCode.ReadCSVFile(ref argFileName);
                string strFile = lstItem.Split('.').First().Replace("_", ":");
                //--------------------------

                // Find the indices of "DATE TIME" and "VALUE" columns
                int dateTimeIndex = Array.IndexOf(strData[0], "DATE   TIME");
                int valueIndex = Array.IndexOf(strData[0], "VALUE");


                // Populate line series with data points
                for (int i = 1; i < strData.Length; i++)
                {
                    // Parse the "DATE TIME" and "VALUE" from the row
                    string[] rowData = strData[i];
                    string dateTimeString = rowData[dateTimeIndex];
                    string valueString = rowData[valueIndex];

                    // Parse date time and value (assuming value is numeric)
                    DateTime dateTime;
                    double value;

                    if (DateTime.TryParse(dateTimeString, out dateTime) && double.TryParse(valueString, out value))
                    {
                        // Determine the color based on the value's sign
                        System.Windows.Media.Brush color = value >= 0 ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.Pink;

                        // Create a new line series for the chart
                        var series = new ColumnSeries()
                        {
                            //Title = "[" + strFile + "]",
                            Title = "value",
                            Values = new ChartValues<ObservablePoint>(),
                            //Fill = System.Windows.Media.Brushes.Transparent,
                            //Fill = System.Windows.Media.Brushes.LightBlue,
                            Fill = color,
                            PointGeometry = DefaultGeometries.Square,
                            //Stroke = System.Windows.Media.Brushes.Blue, // Change the line color to blue
                            StrokeThickness = 1.0 // Adjust the line thickness
                        };

                        // Add the data point to the series
                        //series.Values.Add(new ObservablePoint(dateTime.ToOADate(), value));
                        series.Values.Add(new ObservablePoint(dateTime.ToOADate(), value));

                        seriesCollection.Add(series);

                    }
                    else
                    {
                        // Handle parsing errors if necessary
                        Console.WriteLine($"Error parsing data at row {i}");
                    }
                }


                // Set labels

                Console.WriteLine(seriesCollection);
                // Set the series collection for the chart
                CartesianChart1.Series = seriesCollection;

            }
        }


        private void tbAxisX_Click(object sender, EventArgs e)
        {
            if (tbAxisY.Checked == true)
            {
                tbAxisY.Checked = false;
                _axisValue = 0;
                if (is_MM) // Check if 'is_MM' is true
                {
                    //DisplayGraph(true); // If 'is_MM' is true, call DisplayGraph_Channel with true
                }
                else
                {
                    DisplayGraph_Channel(); // If 'is_MM' is false, call DisplayGraph_Channel with no parameters
                }
            }
            else
            {
                tbAxisX.Checked = true;
            }
        }

        private void tbAxisY_Click(object sender, EventArgs e)
        {
            if (tbAxisX.Checked == true)
            {
                tbAxisX.Checked = false;
                _axisValue = 1;
                if (is_MM) // Check if 'is_MM' is true
                {
                    //DisplayGraph_Channel(true); // If 'is_MM' is true, call DisplayGraph_Channel with true
                }
                else
                {
                    DisplayGraph_Channel(); // If 'is_MM' is false, call DisplayGraph_Channel with no parameters
                }
            }
            else
            {
                tbAxisY.Checked = true;
            }
        }

        private void tbViewGraph_Click(object sender, EventArgs e)
        {
            toolStripSplitButton1.Enabled = true;
            ResetToolStripSplitButton1();
            if (boreHoleSelected == 0)
                return;
            if (lstBoreholes.SelectedItems.Count == 0)
                return;
            if (lstBoreholes.SelectedItems.Count > 7)
            {
                Interaction.MsgBox("You have selected " + lstBoreholes.SelectedItems.Count + " files. You can select maximum 7 files for plotting graph", Constants.vbOKOnly | Constants.vbExclamation, "Graph");
                return;
            }
            DataGridView1.Visible = false;
            //DisplayGraph();
            DisplayGraph_Channel();
        }

        private void ResetLabels()
        {
            Console.WriteLine("Inside ResetLabels function");
            Label1.Text = "";
            Label2.Text = "";
            Label3.Text = "";
            Label4.Text = "";
            Label5.Text = "";
            label7.Text = "";
            label8.Text = "";

            //Label6.Text = @"View Graph of one or multiple files.";
            Label6.Text = "";


            if (bhIndex >= 0)
            {
                lblBoreholeNumber.Text = "Channel : " + boreHoleSelected.ToString().PadLeft(2, '0');
                //lblDepth.Text = "Depth : " + listBH[bhIndex].Depth + "m";
                //lblSiteName.Text = "Site : " + listBH[bhIndex].SiteName;
                //lblLocation.Text = "Location : " + listBH[bhIndex].Location;
                lblLocation.Text = "Channel  : " + listBH[bhIndex].ChNo;
                lblLocation.Text = "Unit  : " + listBH[bhIndex].Unit;
                lblLocation.Text = "Unit  : " + listBH[bhIndex].Unit;

            }
            else
            {
                lblBoreholeNumber.Text = "";
                lblDepth.Text = "";
                lblSiteName.Text = "";
                lblLocation.Text = "";
            }
        }

        private void ToolBarEnable(ref bool enb)
        {
            bool bnOneFileSelected = lstBoreholes.SelectedItems.Count == 1;
            tbBack.Enabled = enb;
            if (enb & lstBoreholes.SelectedItems.Count > 0)
            {
                tbViewGraph.Enabled = true;
                tbDelete.Enabled = true;
                tbReport.Enabled = bnOneFileSelected;
                tbBaseFile.Enabled = bnOneFileSelected;
            }
            else
            {
                tbViewGraph.Enabled = false;
                tbDelete.Enabled = false;
                tbBaseFile.Enabled = false;
                tbReport.Enabled = false;
            }
        }

        private void lstBoreholes_Click(object sender, EventArgs e)
        {
            bool argenb = !(boreHoleSelected == 0);
            ToolBarEnable(ref argenb);
        }

        private void TbGraphType_SelChange(object sender, EventArgs e)
        {
            // to be implemented

        }

        private void tbZoom_Click(object sender, EventArgs e)
        {
            if (tbZoom.Checked)
            {
                CartesianChart1.Zoom = ZoomingOptions.X;
            }
            else
            {
                CartesianChart1.Zoom = ZoomingOptions.None;
            }
        }

        private void tbDelete_Click(object sender, EventArgs e)
        {
            if (lstBoreholes.SelectedItems.Count == 0)
                return;

            if (Interaction.MsgBox("Are you sure you want to delete " + lstBoreholes.SelectedItems.Count + " selected file(s)?", Constants.vbYesNo | Constants.vbQuestion, "Delete") == Constants.vbYes)
            {
                foreach (string strFile in lstBoreholes.SelectedItems)
                    System.IO.File.Delete(GlobalCode.GetBoreholeDirectory(ref boreHoleSelected) + @"\" + strFile);
                ReloadList();
            }
        }

        private void PrintToolStripButton_Click(object sender, EventArgs e)
        {
            if (PrintDialog1.ShowDialog() != DialogResult.OK)
                return;
            PrintDocument1.PrinterSettings = PrintDialog1.PrinterSettings;

            bsTextPrintData = "Borehole  : " + boreHoleSelected.ToString().PadLeft(2, '0') + Constants.vbCrLf;
            //bsTextPrintData += "Depth     : " + listBH[bhIndex].Depth + "m" + Constants.vbCrLf;
            //bsTextPrintData += "Site      : " + listBH[bhIndex].SiteName + Constants.vbCrLf;
            //bsTextPrintData += "Location  : " + listBH[bhIndex].Location + Constants.vbCrLf;
            bsTextPrintData += "Channel  : " + listBH[bhIndex].ChNo + Constants.vbCrLf;
            bsTextPrintData += "Unit  : " + listBH[bhIndex].Unit + Constants.vbCrLf;
            bsTextPrintData += "Date/Time : " + Label1.Text;
            if (CartesianChart1.Visible)
            {
                if (!string.IsNullOrEmpty(Label2.Text))
                    bsTextPrintData += ", " + Label2.Text;
                if (!string.IsNullOrEmpty(Label3.Text))
                    bsTextPrintData += ", " + Label3.Text;
                if (!string.IsNullOrEmpty(Label4.Text))
                    bsTextPrintData += ", " + Label4.Text;
                if (!string.IsNullOrEmpty(Label5.Text))
                    bsTextPrintData += ", " + Label5.Text;
                if (!string.IsNullOrEmpty(label7.Text))
                    bsTextPrintData += ", " + label7.Text;
                if (!string.IsNullOrEmpty(label8.Text))
                    bsTextPrintData += ", " + label8.Text;
                PrintDocument1.DefaultPageSettings.Margins.Left = 20;
                PrintDocument1.DefaultPageSettings.Margins.Top = 20;
                PrintDocument1.DefaultPageSettings.Margins.Right = 15;
            }
            else
            {
                PrintDocument1.DefaultPageSettings.Margins.Left = 90;
                PrintDocument1.DefaultPageSettings.Margins.Top = 90;
                PrintDocument1.DefaultPageSettings.Margins.Right = 75;
            }
            bsTextPrintData += Constants.vbCrLf;
            if (!string.IsNullOrEmpty(Label6.Text))
                bsTextPrintData += Label6.Text + Constants.vbCrLf;

            // Report Printing code below
            printFont = new Font("Courier New", 9f, FontStyle.Regular);
            PrintDocument1.DefaultPageSettings.Landscape = true;
            if (DataGridView1.Visible)
                DisplayReport(true);
            // PrintDocument1.Print()

            // Show the Print Preview Dialog.
            PrintPreviewDialog1.Document = PrintDocument1;
            PrintPreviewDialog1.PrintPreviewControl.Zoom = 1d;
            PrintPreviewDialog1.ShowDialog();
            // PrintDocument1.Dispose()
        }

        private void tbReport_Click(object sender, EventArgs e)
        {
            ResetLabels();
            DataGridView1.Visible = false;
            CartesianChart1.Visible = false;
            ResetToolStripSplitButton1();
            toolStripSplitButton1.Enabled = false;
            DisplayReport();
        }
        //private void tbBaseFile_Click(object sender, EventArgs e)
        //{
        //    listBH[bhIndex].BaseFile = Conversions.ToString(lstBoreholes.SelectedItem);
        //    var tmp = listBH;
        //    var argbh = tmp[bhIndex];
        //    GlobalCode.UpdateBorehole(ref argbh);
        //    tmp[bhIndex] = argbh;
        //    ReloadList();
        //}

        private void tbBaseFile_Click(object sender, EventArgs e)
        {
            // Get the selected item from the list
            string selectedBaseFile = Conversions.ToString(lstBoreholes.SelectedItem);
            // Create a temporary copy of the list
            var tmp = listBH;
            // Get the Borehole object at the current index
            var argbh = tmp[bhIndex];
            // Check if the selected item is the same as the current BaseFile
            if (string.Equals(argbh.BaseFile, selectedBaseFile))
            {
                // If it is the same, "deselect" the BaseFile (set it to null or an empty string)
                if (argbh.BaseFile != null) // Check if the BaseFile was previously present
                {
                    // Ask the user if they really want to remove the BaseFile
                    DialogResult dialogResult = MessageBox.Show("Are you sure you want to remove the BaseFile?", "Confirm", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        argbh.BaseFile = null; // You can also use String.Empty if you prefer
                        MessageBox.Show("The BaseFile has been removed.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); // Notify the user
                    }
                }
            }
            else
            {
                // If it is different, update the Borehole object with the new BaseFile value
                argbh.BaseFile = selectedBaseFile;
            }
            // Call the UpdateBorehole method to update the borehole information
            GlobalCode.UpdateBorehole(ref argbh);
            // Update the Borehole object in the temporary list
            tmp[bhIndex] = argbh;
            // Reload the list with the updated data
            ReloadList();
        }



        private void ListBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            // Draw the background of the ListBox control for each item.
            e.DrawBackground();
            if (e.Index < 0)
                return;

            // Define the default color of the brush as black.
            //var myBrush = System.Drawing.Brushes.Beige;
            var myBrush = System.Drawing.Brushes.IndianRed;

            if (bhIndex >= 0)
            {
                if (CultureInfo.CurrentCulture.CompareInfo.Compare(listBH[bhIndex].BaseFile ?? "", lstBoreholes.Items[e.Index].ToString() ?? "", CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0)
                {
                    myBrush = System.Drawing.Brushes.OrangeRed;
                }
                else
                {
                    myBrush = System.Drawing.Brushes.DarkTurquoise;
                }
            }

            // Draw the current item text based on the current 
            // Font and the custom brush settings.
            e.Graphics.DrawString(lstBoreholes.Items[e.Index].ToString(), e.Font, myBrush, e.Bounds, StringFormat.GenericDefault);

            // If the ListBox has focus, draw a focus rectangle around  _ 
            // the selected item.
            e.DrawFocusRectangle();

        }


        private void PrintForm1_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            var strFormat = new StringFormat();
            var rectDraw = new RectangleF(e.MarginBounds.Left, e.MarginBounds.Top, e.MarginBounds.Width, e.MarginBounds.Height);

            strFormat.Trimming = StringTrimming.Word;
            if (DataGridView1.Visible)
            {
                int numChars;
                int numLines;
                string stringForPage;
                var sizeMeasure = new SizeF(e.MarginBounds.Width, e.MarginBounds.Height - printFont.GetHeight(e.Graphics));

                e.Graphics.MeasureString(bsTextPrintData, printFont, sizeMeasure, strFormat, out numChars, out numLines);
                stringForPage = bsTextPrintData.Substring(0, numChars);
                e.Graphics.DrawString(stringForPage, printFont, System.Drawing.Brushes.Black, rectDraw, strFormat);
                if (numChars < bsTextPrintData.Length)
                {
                    bsTextPrintData = bsTextPrintData.Substring(numChars);
                    e.HasMorePages = true;
                }
                else
                {
                    e.HasMorePages = false;
                }
            }
            else
            {
                var MyChartPanel = new Bitmap(SplitContainer2.Panel1.Width, SplitContainer2.Panel1.Height);
                SplitContainer2.Panel1.DrawToBitmap(MyChartPanel, new Rectangle(0, 0, SplitContainer2.Panel1.Width, SplitContainer2.Panel1.Height));
                var p1 = default(Point);
                p1.X = 5;
                p1.Y = 110;
                if (MyChartPanel.Size.Width < e.PageBounds.Width)
                {
                    p1.X = (int)Math.Round((e.PageBounds.Width - MyChartPanel.Size.Width) / 2d);
                }
                e.Graphics.DrawString(bsTextPrintData, printFont, System.Drawing.Brushes.Black, rectDraw, strFormat);
                e.Graphics.DrawImage(MyChartPanel, p1);
                e.HasMorePages = false;
            }
        }

        private void lstBoreholes_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        // Initial state
        bool is_MM = true;
        private void toolStripSplitButton1_ButtonClick(object sender, EventArgs e)
        {
            // Toggle the state
            is_MM = !is_MM;

            // Properties - Update the button text,color based on the current state
            toolStripSplitButton1.Text = is_MM ? "MM" : "DEG";
            toolStripSplitButton1.BackgroundImage = new Bitmap(1, 1);
            toolStripSplitButton1.BackgroundImageLayout = ImageLayout.None;
            toolStripSplitButton1.BackColor = is_MM ? Color.Cyan : Color.LightGreen;//previously LightBlue instead of Cyan

            // Check if a base file is selected
            if (listBH[bhIndex].BaseFile is null || string.IsNullOrEmpty(listBH[bhIndex].BaseFile))
            {
                //Interaction.MsgBox("No base file selected for this borehole. Go back and select a base file to view deviation.", Constants.vbOKOnly | Constants.vbExclamation, "Graph");
                //DisplayGraph(); //display graph in mm 
                //return;
            }

            // Handle the behavior based on the current state
            if (is_MM)
            {
                // Reset labels and hide chart and DataGridView
                ResetLabels();
                CartesianChart1.Visible = false;
                DataGridView1.Visible = false;
                //ToolStrip2.Enabled = false;
                // Get the path to the base file
                string strFileBase = GlobalCode.GetBoreholeDirectory(ref boreHoleSelected) + @"\" + listBH[bhIndex].BaseFile;

                //if (!System.IO.File.Exists(strFileBase))//if making the BaseFile concrete function then uncomment this.
                //{
                //    Interaction.MsgBox("Base file does not exist. It must have been removed. Please select any file as a base file.", Constants.vbOKOnly | Constants.vbExclamation, "Graph");
                //    //return; //uncomment this return statment.
                //}
                //else
                //{// Handle the 'deg' state
                //    //MessageBox.Show($"Showing graph in Degree", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //    DisplayGraph(true);
                //}

                //DisplayGraph(true);
            }
            else
            {
                //MessageBox.Show($"Showing graph in mm", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Handle the 'mm' state
                DisplayGraph_Channel();

            }
        }
        private void ResetToolStripSplitButton1()
        {
            // Reset the state
            // or set to initial color

            // Reset other properties and states as needed...
        }

        private void degToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Get the path to the base file
            //string strFileBase = GlobalCode.GetBoreholeDirectory(ref boreHoleSelected) + @"\" + listBH[bhIndex].BaseFile;
            //if (!System.IO.File.Exists(strFileBase))//if making the BaseFile concrete function then uncomment this.
            //{
            //    Interaction.MsgBox("Base file does not exist. It may have been removed or deleted .Please select another file as a base to view Degree Graph.", Constants.vbOKOnly | Constants.vbExclamation, "Graph");                //return; //uncomment this return statment.
            //}
            //else

            ResetToolStripSplitButton1();

            DisplayGraph_Channel();

        }


        private void mMToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}