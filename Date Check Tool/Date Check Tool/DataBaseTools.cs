//Very wow such great efficiency

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;
using System.Windows;

namespace Date_Check_Tool
{

    public delegate void ProgressUpdated(object sender, DBToolsProgressUpdatedEventArgs e); //Somehow this lets us callback to the mainwindow when progress changes
    public delegate void FatalError(object sender, DBToolsFatalErrorEventArgs e); //Somehow this lets us callback to the mainwindow when we get a bad error

    class DataBaseTools
    {

        bool shouldAbort = false; //This flag is checked in case we catch a bad error

        public event ProgressUpdated progressUpdated; //progressUpdated event definition
        public event FatalError didReceiveFatalError; //didReceiveFatalError definition

        const int Link_Table = 0; //Link table index
        const int STN_Date_Table = 1; //STN table index
        const int WISLR_Date_Table = 2; //WISLR table index

        //Reads in all fields from each of the tables the user has selected
        public DataTable[] getDataTables(string[] filePaths, string[] tables)
        {
            
            List<DataTable> dataTables = new List<DataTable>();

            progressUpdated(this, new DBToolsProgressUpdatedEventArgs(0, "Reading Tables")); //Report initial progress so the textblock text is set

            for (int i = 0; i < filePaths.Length; i++) //iterate the filepaths list
            {

                dataTables.Add(getDataTable(filePaths[i], tables[i])); //Gets the datatable from the table name at the filepath 
                progressUpdated(this, new DBToolsProgressUpdatedEventArgs(Convert.ToInt32(i / Convert.ToDouble(filePaths.Length - 1) * 100), "Reading Tables")); //Fire the progressUpdated event

            }

            return dataTables.ToArray(); //Return the datatables list as an array

        }

        //Returns a datatable from the filepath with the table name
        public DataTable getDataTable(string filePath, string table)
        {

            List<string> dbConnectionStrings = new List<string>();
            List<string> selectionQueries = new List<string>();
            DataTable dataTable = new DataTable();
            
            using (OleDbConnection connection = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath))
            using (OleDbCommand selectTablesCommand = new OleDbCommand("Select *FROM [" + table + "]", connection)) //When we use the "using" like this OleDBConnection and OleDBCommand will be released once we are done with it and our connection is closed
            {

                connection.Open(); //Open a connection to the database file containing the selected table
                OleDbDataReader reader = selectTablesCommand.ExecuteReader(); //Create a dbreader from the dbcommand
                dataTable.Load(reader); //Read in the opened datatable and store it as a datatable object

            }

            return dataTable; //Return the datatable

        }

        //Returns a link array with start and end valid dates
        public Link[] getLinksWithDates(string[] filePaths, string[] tables)
        {

            List<Link> links = new List<Link>();
            DataTable[] dataTables = getDataTables(filePaths, tables); //get the table the user has selected
            List<DataRow> stnRows = dataTables[STN_Date_Table].Rows.Cast<DataRow>().ToList();
            List<DataRow> wislrRows = dataTables[WISLR_Date_Table].Rows.Cast<DataRow>().ToList();
            double progress = 0.0;

            Parallel.For(0, dataTables[0].Rows.Count, (i, loopState) => //Parallel for loops are awesome because they do stuff asynchronously
            {

                switch (shouldAbort) //Check if we should abort before every iteration (switches are faster so im using it instead of an if)
                {

                    case false: //Shouldn't abort

                        try //This may fail if the user didn't select the tables in the correct sequence
                        {

                            //Get the stn and wislr id from the link_link (first) table
                            string stnId = dataTables[Link_Table].Rows[i].ItemArray[0].ToString();
                            string wislrId = dataTables[Link_Table].Rows[i].ItemArray[3].ToString();

                            Link link = new Link(dataTables, i);//Create a link from the tables and current row
                            links.Add(link); //Add the newly created link to the links list

                            //!!!! try to make the lists shorter as we go

                            progressUpdated(this, new DBToolsProgressUpdatedEventArgs(Convert.ToInt32(++progress / Convert.ToDouble(dataTables[0].Rows.Count) * 100), "Comparing Dates")); //Fire the progress updated event

                        }
                        catch (Exception e) //Oh no... this shouldn't happen
                        {

                            Console.WriteLine(e);
                            shouldAbort = true; //We have to stop this madness!
                            didReceiveFatalError(this, new DBToolsFatalErrorEventArgs("This exception is usually thrown when the link, WISLR, and STN tables are not opened in the correct order, or an STN or WISLR date cannot be found in the STN or WISLR date table. Crashed on row " + (i + 1) + ".")); //Fire the fatal error event and prepare for death

                        }

                        break;

                    default: //Noooooooooooo we have failed

                        loopState.Break(); // basically this is the same as saying break; in a normal forloop

                        break;

                }

            });

            return links.ToArray(); //We're doing great so far return the completed links array containing links with start and end valid dates

        }

        //The moment we've all been waiting for! Writes dates to the first table in the tables array (remember thats the array in mainwindow.cs that gets passed into every method above this)
        public void writeDatesToTable(Link[] links, string filePath, string table)
        {

            string[] columns = { "Start_Valid", "End_Valid" }; //Define our start and end valid column names

            try { //Something may break here but we hope not

                using (OleDbConnection connection = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath)) //Create a connection!
                {

                    connection.Open();

                    foreach (string column in columns) //Iterate over the column array these next several is absolutely hideous but it must be done or we crash =/
                    {

                        using (OleDbCommand insertCommand = new OleDbCommand("UPDATE " + table + " SET " + column + " = @date WHERE STNid = @stnId AND WISLRid = @wislrId", connection)) //When we use the "using" like this OleDBConnection should be discarded once it's done
                        {

                            OleDbDataAdapter dataAdapter = new OleDbDataAdapter(insertCommand);
                            //Add parameters to write values in tables the date is written while the stn and wislr ids are used to lookup rows
                            insertCommand.Parameters.AddWithValue("@date", "");
                            insertCommand.Parameters.AddWithValue("@stnId", "");
                            insertCommand.Parameters.AddWithValue("@wislrId", "");

                            for (int i = 0; i < links.Length; i++) //Begin iteration over links
                            {

                                Link link = links[i];

                                switch (column == columns[0]) //This kinda looks bad probably could've been done better
                                {

                                    case true: //If is start valid

                                        //Set values of sql parameters (this all probably should be in a method)
                                        insertCommand.Parameters["@date"].Value = link.usedStartDate;
                                        insertCommand.Parameters["@stnId"].Value = link.stnId;
                                        insertCommand.Parameters["@wislrId"].Value = link.wislrId;
                                        insertCommand.ExecuteNonQuery(); //Executes the query built from the insertCommand and these params

                                        break;

                                    default: //end valid

                                        switch (string.IsNullOrEmpty(link.usedEndDate)) //Check if we have a date
                                        {

                                            case false: //Write the date if we have one

                                                //Set values of sql parameters (this all probably should be in a method)
                                                insertCommand.Parameters["@date"].Value = link.usedEndDate;
                                                insertCommand.Parameters["@stnId"].Value = link.stnId;
                                                insertCommand.Parameters["@wislrId"].Value = link.wislrId;
                                                insertCommand.ExecuteNonQuery(); //Executes the query built from the insertCommand and these params

                                                break;

                                            default: //If both dates were null do nothing (if we try to write a blank date here it crashes because its not in date format...
                                                break;

                                        }

                                        break;

                                }

                                progressUpdated(this, new DBToolsProgressUpdatedEventArgs(Convert.ToInt32(i / Convert.ToDouble(links.Length - 1) * 100), "Writing to " + column)); //Fire the progressUpdated event

                            }

                        }

                    }

                }
            }
            catch(Exception ex) //Maybe this won't happen so late in the process
            {

                Console.WriteLine(ex);
                shouldAbort = true; //Let us not continue for we have failed
                didReceiveFatalError(this, new DBToolsFatalErrorEventArgs("An error occurred during the write process. Please make sure the Microsoft Access file being modified was not moved or deleted during the process.")); //Heres our pretty general error message

            }

        }

        public void writeToTable(DataTable table, string filePath)
        {

            using (OleDbConnection connection = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath))
            using (OleDbCommand createCommand = new OleDbCommand("CREATE TABLE [Date_Errors]([STNID] number, [WISLRID] number, [START_VALID] text, [END_VALID] text)", connection))
            using (OleDbCommand deleteCommand = new OleDbCommand("DROP TABLE [Date_Errors]", connection))
            using (OleDbDataAdapter dataAdapter = new OleDbDataAdapter("Select *FROM [Date_Errors]", "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath))
            using (OleDbCommandBuilder commandBuilder = new OleDbCommandBuilder(dataAdapter))
            {
                Console.WriteLine("rows" + table.Rows.Count);
                connection.Open();

                try
                {

                    createCommand.ExecuteNonQuery();

                } 
                catch(Exception ex)
                {

                    deleteCommand.ExecuteNonQuery();
                    createCommand.ExecuteNonQuery();

                    Console.WriteLine(ex);

                }
                
                dataAdapter.AcceptChangesDuringFill = true;
                dataAdapter.Fill(table);
                dataAdapter.Update(table);
            }
            

        }

        //Returns an array containing table names for each of the tables in the hopefully existing access file (usually called to populate comboboxes)
        public string[] getTableNames(string filePath)
        {
            //I don't think this method needs to be asynchronous but I don't know

            List<string> tableNames = new List<string>();

            try {

                using (OleDbConnection connection = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath)) //When we use the "using" like this OleDBConnection will not be disposed once its done we hope
                {

                    string[] restrictions = new string[4]; //I'm not really sure what this does or how this works but if we don't put restrictions it doesn't return our table names correctly
                    restrictions[3] = "Table"; //Again I have no idea what's going on here or why we'r setting the last index to table

                    connection.Open();
                    DataTable tables = connection.GetSchema("Tables", restrictions); //Somehow returns a datatable containing rows containing table names
                    connection.Close();

                    foreach (DataRow row in tables.Rows) //Iterate over the rows in the table we got from the access file
                    {

                        tableNames.Add(row.ItemArray[2].ToString()); //In this case (and hopefully every case) the table names where in the third column so this adds them to the tablename list

                    }

                }

            }

            catch(Exception ex) //This won't happen normally unless the users attempts to open that annoying temporary file access puts on the desktop when a file is open otherwise this shouldn't be possible
            {
                
                Console.WriteLine(ex);

                MessageBox.Show("Make sure that the file you are attempting to open is a Microsoft Access file.", "The file could not be opened.", MessageBoxButton.OK, MessageBoxImage.Error); //This isn't fatal! display the messagebox
                    
            }

            return tableNames.ToArray(); //Everything worked lets return our tableNames

        }

        //Writes the start and end valid dates to the first table in the first file
        public void populateDates(string[] filePaths, string[] tables)
        {

            Link[] links = getLinksWithDates(filePaths, tables); //Creates a link array with links that have start and end valid dates

            if (!shouldAbort) //Check if we should continue
            {

                writeDatesToTable(links, filePaths[0], tables[0]); //Write the dates to the first table in the first file specified by the user

            }

        }
        
    }

    //These EventArgs subclasses allow us to pass things to the event
    public class DBToolsProgressUpdatedEventArgs: EventArgs
    {

        public int progress { get; } //Define our progress readonly property (displayed via progress bar)
        public string currentProcess { get; } //Define our process readonly property (displayed via label above progress bar)

        public DBToolsProgressUpdatedEventArgs(int progress, string process)
        {

            this.progress = progress;
            currentProcess = process;

        }

    }

    public class DBToolsFatalErrorEventArgs : EventArgs
    {

        public string error { get; } //Deine our error readonly property (displayed in body of messagebox)

        public DBToolsFatalErrorEventArgs(string errorText)
        {

            error = errorText;

        }

    }

}
