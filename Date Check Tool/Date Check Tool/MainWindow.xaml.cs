using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Resources;
using System.ComponentModel;
using System.Data;
using System.IO;

namespace Date_Check_Tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        BackgroundWorker backgroundWorker;
        ProgressDialog progressDialog;
        string currentProcess;
        string filePath;
        string tableName;
        bool errorPresented = false;
        bool shouldClose = false;
        int errorCount;

        public MainWindow()
        {
            InitializeComponent();
        }

        private string[] getBadDates()
        {

            DataTable table;
            List<string> writeContents = new List<string>();               

            DataBaseTools dbTools = new DataBaseTools();
            dbTools.progressUpdated += dbTools_ProgressUpdated;
            dbTools.didReceiveFatalError += dbTools_didReceiveFatalError;
            table = dbTools.getDataTable(filePath, tableName);
            //13 rc
            for (int i = 0; i < table.Rows.Count; i++)
            {
                
                string recordCreated = table.Rows[i].ItemArray[12].ToString();
                string recordHistoric = table.Rows[i].ItemArray[13].ToString();
                string startValid = table.Rows[i].ItemArray[14].ToString();
                string endValid = table.Rows[i].ItemArray[15].ToString();
                int localErrorCount = 0;

                writeContents.Add("Row " + (i + 1) + " {");

                if (string.IsNullOrEmpty(recordHistoric) && !string.IsNullOrEmpty(endValid))
                {
                    
                    writeContents.Add("    There is no Record Historic for End Valid");
                    errorCount++;
                    localErrorCount++;

                }

                if (DateTools.getLaterDate(endValid, startValid) == startValid)
                {
                                        
                    writeContents.Add("    Start Valid is more recent than End Valid");
                    errorCount++;
                    localErrorCount++;

                }

                if (DateTools.getLaterDate(recordHistoric, startValid) == startValid)
                {

                    writeContents.Add("    Start Valid is more recent than Record Historic");
                    errorCount++;
                    localErrorCount++;

                }

                if (DateTools.getLaterDate(recordCreated, startValid) == startValid)
                {

                    writeContents.Add("    Start Valid is more recent than Record Created");
                    errorCount++;
                    localErrorCount++;

                }

                if (localErrorCount == 0)
                {

                    writeContents.RemoveAt(writeContents.Count - 1);

                }
                else
                {

                    writeContents.Add("}\n");

                }

            }

            return writeContents.ToArray();

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

            tableName = tableComboBox.Text;
            progressDialog = new ProgressDialog();
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.DoWork += backgroundWorker_DoWork;
            backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
            backgroundWorker.ProgressChanged += backgroundWorker_ProgressChanged;
            backgroundWorker.RunWorkerAsync(tableComboBox.Text); //Start background worker

        }

        //#--Background Worker--#
        //Fired when backgroundWorker.startasync is called (executes on a different thread)
        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            string[] writeContents = getBadDates();
            using (StreamWriter streamWriter = new StreamWriter("output.txt"))
            {
                if (errorCount > 0)
                {

                    for (int i = 0; i < writeContents.Length; i++)
                    {

                        streamWriter.WriteLine(writeContents[i]);

                    }

                }

            }
        }

        //Fired when backgroundWorker is done
        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            shouldClose = true; //The user now has permission to kill date pop so don't get mad
            progressDialog.closeDialog(); //Progress dialog should go away we don't need it anymore
            Dispatcher.Invoke(() => { //We have to invoke this other stuff because its crossthreaded

                if (errorCount > 0)
                    System.Diagnostics.Process.Start("notepad.exe", "output.txt"); //Opens the text file in notepad

                MessageBox.Show("The Date Check Tool finished and found " + errorCount + " errors.");

            });

        }

        //Fired when backgroundworker.reportProgress is called
        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

            progressDialog.setProgress(e.ProgressPercentage, currentProcess); //Sets the progress of the progress bar

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

    }

}
