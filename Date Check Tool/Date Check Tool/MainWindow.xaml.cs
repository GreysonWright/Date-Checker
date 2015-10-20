//Created by Greyson Wright 2015
//This is just a great way to make sure the dates are right I guess. If you happen to use DataBaseTools don't get it from this proj because its outdated. The one in date pop is way better sort of. 
//Be sure to read my intro comments to date pop if you havn't already done so. It has great info in it.

using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;
using System.ComponentModel;
using System.Data;
using System.IO;
using System;
using System.Linq;

namespace Date_Check_Tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        //Global variables bother me
        BackgroundWorker backgroundWorker;
        DataTable outTable;
        string currentProcess;
        string filePath;
        string tableName;
        bool errorPresented = false;
        bool shouldClose = true;
        int errorCount;

        public MainWindow()
        {
            InitializeComponent();

            //Displays select table in combo box
            tableComboBox.Items.Add("Select Table...");
            tableComboBox.Text = "Select Table...";

        }

        //A method that returns an array that will later be written to a text file each element is a line of a text file
        private string[] getBadDates()
        {

            DataTable table;
            outTable = new DataTable();
            List<string> errorTypeCount = new List<string>();
            List<string> writeContents = new List<string>();
            string[] columNames = {"STNID", "WISLRID", "END_VALID", "START_VALID"};        

            DataBaseTools dbTools = new DataBaseTools();
            dbTools.progressUpdated += dbTools_ProgressUpdated;
            dbTools.didReceiveFatalError += dbTools_didReceiveFatalError;
            table = dbTools.getDataTable(filePath, tableName);

            string stnId;
            string wislrId;
            string recordCreated;
            string recordHistoric;
            string startValid;
            string endValid;
            int localErrorCount; //Local errorCount keeps up with errors per row while errorCount is total erros over the whole file the error count is displayed once date check is done
            int[] errors = { 0, 0, 0, 0 };

            foreach (string name in columNames)
            {

                outTable.Columns.Add(name);

            }
            
            for (int i = 0; i < table.Rows.Count; i++)
            {

                stnId = table.Rows[i].ItemArray[0].ToString();
                wislrId = table.Rows[i].ItemArray[3].ToString();   
                recordCreated = table.Rows[i].ItemArray[12].ToString();
                recordHistoric = table.Rows[i].ItemArray[13].ToString();
                startValid = table.Rows[i].ItemArray[14].ToString();
                endValid = table.Rows[i].ItemArray[15].ToString();
                localErrorCount = 0;  

                writeContents.Add("STNID: " + stnId +" WISLRID: " + wislrId + " {"); //The rows aren't always the same in our datatables and the datatable in access so we give them the stnid and wislrid

                if (string.IsNullOrEmpty(recordHistoric) && !string.IsNullOrEmpty(endValid)) //No end valid date for record historic
                {
                    
                    writeContents.Add(Environment.NewLine + "    There is no Record Historic for End Valid (" + endValid + ")" + Environment.NewLine);
                    errors[0]++;
                    localErrorCount++;

                }

                if (DateTools.getLaterDate(endValid, startValid) == startValid)
                {
                                        
                    writeContents.Add(Environment.NewLine + "    Start Valid is more recent than End Valid (" + endValid + ")" + Environment.NewLine); //End valid is earlier than the start valid
                    errors[1]++;
                    localErrorCount++;

                }

                if (DateTools.getLaterDate(recordHistoric, startValid) == startValid) //Start valid is more recent than record historic
                {

                    writeContents.Add(Environment.NewLine + "    Start Valid is more recent than Record Historic" + Environment.NewLine);
                    errors[2]++;
                    localErrorCount++;

                }

                if (DateTools.getLaterDate(recordCreated, startValid) == startValid) //Start valid is more recent than record created
                {

                    writeContents.Add(Environment.NewLine + "    Start Valid is more recent than Record Created" + Environment.NewLine);
                    errors[3]++;
                    localErrorCount++;

                }

                if (localErrorCount == 0) //If we don't have errors here remove the opening line and brace
                {

                    writeContents.RemoveAt(writeContents.Count - 1);

                }
                else //If we have local errors close it off with a brace and a new line
                {

                    errorCount += localErrorCount; //Keep track of all errors
                    writeContents.Add("}\n");

                    DataRow currentRow = outTable.NewRow();
                    currentRow["STNID"] = stnId;
                    currentRow["WISLRID"] = wislrId;
                    currentRow["END_VALID"] = endValid;
                    currentRow["START_VALID"] = startValid;
                    outTable.Rows.Add(currentRow);

                }

            }

            errorTypeCount.Add("Error types:\nThere is no Record Historic for End Valid: " + errors[0] + "\n");
            errorTypeCount.Add("Start Valid is more recent than End Valid: " + errors[1] + "\n");
            errorTypeCount.Add("Start Valid is more recent than Record Historic: " + errors[2] + "\n");
            errorTypeCount.Add("Start Valid is more recent than Record Created: " + errors[3] + "\n" + Environment.NewLine);

            return errorTypeCount.ToArray().Concat(writeContents.ToArray()).ToArray(); //Return array of lines

        }

        private void browseButton_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "Access 2007 (*.accdb)|*accdb|Access 2000-2003 (*.mdb)|*.mdb"; //Set file filter for MS Access files only
            
            if (openDialog.ShowDialog() == true) //Check if a file gets opened
            {

                filePath = openDialog.FileName; //Looks like we haven't added a filepath for this page yet so add a new one
                
                tableComboBox.Items.Clear(); //Clear out the existing items so we don't have table names from different files
                tableComboBox.Items.Add("Select Table...");
                tableComboBox.Text = "Select Table...";
                
                nameLabel.Content = System.IO.Path.GetFileName(openDialog.FileName); //Display the filename in the fileLabel
                DataBaseTools dbTools = new DataBaseTools();
                foreach (string tableName in dbTools.getTableNames(openDialog.FileName)) //Pull tables names out of the access db file and iterate over them
                {

                    tableComboBox.Items.Add(tableName); //Add the table names to the comboxbox so the user can select the one he or she wants

                }

            }
        }

        private void goButton_Click(object sender, RoutedEventArgs e)
        {

            if (filePath != null && tableComboBox.Text != "Select table...") //Check if user has selected a table
            {

                shouldClose = false; //Don't let user close us until we finish
                tableName = tableComboBox.Text; //Get the tablename from the combo box
                backgroundWorker = new BackgroundWorker(); //Initialize a background worker
                backgroundWorker.WorkerReportsProgress = true;
                backgroundWorker.WorkerSupportsCancellation = true;
                backgroundWorker.DoWork += backgroundWorker_DoWork;
                backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
                backgroundWorker.RunWorkerAsync(tableComboBox.Text); //Start background worker

            }
            else
            {

                MessageBox.Show("Select a link table before proceeding.");

            }

        }

        //#--Background Worker--#
        //Fired when backgroundWorker.startasync is called (executes on a different thread)
        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            string[] writeContents = getBadDates();
            if (errorCount > 0) //Don't worry about writing if there are no errors
            {
                using (StreamWriter streamWriter = new StreamWriter("output.txt"))
                {

                    for (int i = 0; i < writeContents.Length; i++) //Iterate over the array
                    {

                        streamWriter.WriteLine(writeContents[i]); //Write each element in the array to the file

                    }

                }

            }
        }

        //Fired when backgroundWorker is done
        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            shouldClose = true; //The user now has permission to kill date pop so don't get mad
            Dispatcher.Invoke(() => { //We have to invoke this other stuff because its crossthreaded

                if (errorCount > 0) //Don't worrry about opening the file unless we have errors
                {

                    MessageBox.Show("The Date Check Tool finished and found errors.");
                
                    DataBaseTools dbTools = new DataBaseTools();
                    dbTools.writeToTable(outTable, filePath);

                    System.Diagnostics.Process.Start("msaccess.exe", filePath); //Opens the text file in notepad
                    System.Diagnostics.Process.Start("notepad.exe", "output.txt"); //Opens the text file in notepad

                }
                else
                {

                    MessageBox.Show("The Date Check Tool finished and found no errors.");

                }

                errorCount = 0;

            });

        }
        
        //#--DB Tools--#
        //Fired when the progress changes (gives us access to the background worker from inside dbtools)
        public void dbTools_ProgressUpdated(object sender, DBToolsProgressUpdatedEventArgs e)
        {

            currentProcess = e.currentProcess; //Sets the process name
            backgroundWorker.ReportProgress(e.progress); //Sets the progress percentage of the progress bar

        }

        //Fired when we catch a fatal exception in dbtools
        public void dbTools_didReceiveFatalError(object sender, DBToolsFatalErrorEventArgs e)
        {

            Dispatcher.Invoke(() => //Cross threaded operations mean we must invoke this stuff
            {

                if (!errorPresented) //Check if error is already presented
                {

                    errorPresented = true; //This shouldn't happen more than once or we get multiple messageboxes
                    shouldClose = true; //Let the application close 
                    backgroundWorker.CancelAsync(); //Tell backgroundworker to stop its work
                    MessageBox.Show(e.error, "An unexpected exception has occured!", MessageBoxButton.OK, MessageBoxImage.Error); //Explain that the user has inflicted a fatal injury upon us as we lay on our death bead
                    Application.Current.Shutdown(); //*dies gracefully*

                }

            });

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {

            e.Cancel = !shouldClose;

        }

    }

}
