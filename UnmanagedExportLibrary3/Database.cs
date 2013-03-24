using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace UnmanagedExportLibrary3
{
    class Database
    {
        //private static string connectionString = "user id=berichadmin;" +
        //                               "password=berich;server=testing.berich.vn" +
        //                               "trusted_connection=no;" +
        //                               "database=Forex; " +
        //                               "connection timeout=90;" +
        //    "persist security info=true;" +
        //    "user instance=false;";
        private const string dbName = "AlpariH4";
        private const string connectionString = "user id=berichadmin;password=berich;server=localhost;trusted_connection=no;database=" + dbName + "; connection timeout=90;persist security info=true;user instance=false;";
        //private static string connectionString = "user id=berichadmin;password=berich;server=testing.berich.vn;trusted_connection=no;database=forex; connection timeout=90;persist security info=true;user instance=false;";
        private SqlConnection myconnection;
        //private SqlConnection myconnection = new SqlConnection("user id=sa;password=;999999;server=localhost\\SQL;trusted_connection=no;database=forex; connection timeout=90;persist security info=true;user instance=false;");
        public Database(string conn = connectionString)
        {
            myconnection = new SqlConnection(conn);
        }
        public static string getCString()
        {
            return connectionString;
        }
        public void executeNonQuery(string sql, bool toLog = false)
        {
            if (toLog == true)
                log_query(sql);
            try
            {
                this.myconnection.Open();
                SqlCommand mycommand = new SqlCommand(sql, this.myconnection);
                mycommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                string errorMessage = e.ToString();
                if (!errorMessage.Contains(@"Violation of PRIMARY KEY constraint 'PK__stock__35FB8A7D7FB5F314'"))
                    log_sql_error(e, sql);
            }
            this.myconnection.Close();
        }

   
        public DataTable query(string sql)
        {
            //  log(sql);
            try
            {
                SqlCommand mycommand = new SqlCommand(sql, this.myconnection);
                SqlDataAdapter da = new SqlDataAdapter(mycommand);
                da.SelectCommand = mycommand;
                DataTable dtGet = new DataTable();
                da.Fill(dtGet);
                return dtGet;
            }
            catch (Exception e)
            {
               log_sql_error(e,sql);
            }
            return null;
        }

        public static void log_query(string input)
        {
            System.IO.File.AppendAllText(@"C:\metatrader\query.txt", input + "\n\n");

        }

        public static void log_sql_error(Exception e,string sql)
        {
            String destination = string.Format(@"C:\metatrader\error.txt");
            string error = String.Format("----------------\n{0}\n{1}\nQuery:{2}\n", DateTime.Now.ToString(), e.ToString(),sql);
            System.IO.File.AppendAllText(destination, error);
        }

    }
}

