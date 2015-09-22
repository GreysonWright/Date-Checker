//This does nothing like Matt Yorks DateTools library for iOS that several large apps use. Date tools takes 2 dates in (as strings) and returns the latest date of the 2.

using System;

namespace Date_Check_Tool
{
    sealed class DateTools
    {

        //Returns greater date of the 2 passed in
        public static string getLaterDate(string date1, string date2) //Who needs singletons when we can just have static methods
        {

            if (!string.IsNullOrEmpty(date1) && !string.IsNullOrEmpty(date2)) //Make sure the date strings aren't null
            {

                //convert date strings to DateTime
                DateTime convertedDate1 = Convert.ToDateTime(date1);
                DateTime convertedDate2 = Convert.ToDateTime(date2);

                if (DateTime.Compare(convertedDate1, convertedDate2) < 0) //Check if date1 is earlier
                {

                    return date2;

                }

                return date1; //happens if dates are same or date1 is bigger

            }

            //return string.IsNullOrEmpty(date2) ? date1: date2; //Return the date that isn't empty (doesn't this look so pretty)
            return "NULL";
        }

    }

}
