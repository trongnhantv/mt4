using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace UnmanagedExportLibrary3
{
    class Database
    {
        //private static string connectionString = "user id=sa;" +
        //                               "password=999999;server=localhost\\SQL;" +
        //                               "trusted_connection=no;" +
        //                               "database=forex; " +
        //                               "connection timeout=90;" +
        //    "persist security info=true;" +
        //    "user instance=false;";
        private static string connectionString = "user id=berichadmin;password=berich;server=apps.berich.vn;trusted_connection=no;database=forex; connection timeout=90;persist security info=true;user instance=false;";
        private SqlConnection myconnection;
        //private SqlConnection myconnection = new SqlConnection("user id=sa;password=;999999;server=localhost\\SQL;trusted_connection=no;database=forex; connection timeout=90;persist security info=true;user instance=false;");
        public Database()
        {
            myconnection = new SqlConnection(connectionString);
            
        }
        public static string getCString()
        {
            return connectionString;
        }
        public void executeNonQuery(string sql)
        {
            this.myconnection.Open();
            this.log(sql + "\n");
            try
            {
                SqlCommand mycommand = new SqlCommand(sql, this.myconnection);
               mycommand.ExecuteNonQuery();
               this.myconnection.Close();
            }
            catch (Exception e)
            {
                this.log(e.ToString() + "\n" + sql + "\n");
                
            }
        }

        public void log(string input)
        {
            System.IO.File.AppendAllText(@"C:\error.txt", input);
        }

        public DataTable query(string sql)
        {
            SqlCommand mycommand = new SqlCommand(sql, this.myconnection);
            SqlDataAdapter da = new SqlDataAdapter(mycommand);
            try
            {
                da.SelectCommand = mycommand;
                DataTable dtGet = new DataTable();
                da.Fill(dtGet);
                return dtGet;
            }
            catch (Exception e)
            {
                this.log(e.ToString() + "\n" + sql + "\n");
            }
            return null;
        }
    }
}

