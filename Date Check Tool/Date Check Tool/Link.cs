//I didn't know what else to call this class so i just named it link basically its sort of just the rows that we're putting the start and end valid dates on

using System.Data;
using System.Collections.Generic;
using System.Linq;

namespace Date_Check_Tool
{
    class Link
    {
        
        public string stnId { get; set; }
        public string wislrId { get; set; }
        public string usedStartDate { get; set; } //Holds latest start valid
        public string usedEndDate { get; set; } //Holds latest end valid

        public Link(DataTable[] dataTables, int row)
        {
            
            //Get the stn and wislr id from the link_link (first) table
            stnId = dataTables[0].Rows[row].ItemArray[0].ToString();
            wislrId = dataTables[0].Rows[row].ItemArray[3].ToString();

            //Find stn link in stn date table
            List<DataRow> stnRows = dataTables[1].Rows.Cast<DataRow>().ToList();
            DataRow stnDateRow = stnRows.Find(index => index.ItemArray[0].ToString() == stnId);

            //Find wislr link in wislr date table
            List<DataRow> wislrRows = dataTables[2].Rows.Cast<DataRow>().ToList();
            DataRow wislrDateRow = wislrRows.Find(index => index.ItemArray[0].ToString() == wislrId);

            //Compare wislr and stn dates then get larger date via datetools
            usedStartDate = DateTools.getLaterDate(stnDateRow.ItemArray[1].ToString(), wislrDateRow.ItemArray[1].ToString());
            usedEndDate = DateTools.getLaterDate(stnDateRow.ItemArray[2].ToString(), wislrDateRow.ItemArray[2].ToString());

            ////Find stn link in stn date table
            //DataRow stnDateRow = stnDates.Find(index => index.ItemArray[0].ToString() == stnId);

            ////Find wislr link in wislr date table
            //DataRow wislrDateRow = wislrDates.Find(index => index.ItemArray[0].ToString() == wislrId);

            ////Compare wislr and stn dates then get larger date via datetools
            //usedStartDate = DateTools.getLaterDate(stnDateRow.ItemArray[1].ToString(), wislrDateRow.ItemArray[1].ToString());
            //usedEndDate = DateTools.getLaterDate(stnDateRow.ItemArray[2].ToString(), wislrDateRow.ItemArray[2].ToString());

        }

    }

}
