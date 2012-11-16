using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using RGiesecke.DllExport;
using System.Runtime.InteropServices;
using System.Data.SqlClient;
namespace UnmanagedExportLibrary3
{
    internal static class UnmanagedExports
    {
        [DllExport("getOpen", CallingConvention = CallingConvention.StdCall)]
        public static string getOpen()
        {
            Database db = new Database();
            DataTable dt = db.query("select stock_id,portfolio_id,type,volume,avgPrice,rec,date from stockinportfolio where rec like 'OPEN%';");
            string order = "";
            foreach(DataRow  Row in dt.Rows)
            {
                order += Row["stock_id"].ToString() + ",";
                order += Row["portfolio_id"].ToString() + ",";
                order += Row["type"].ToString() + ",";
                order += Row["volume"].ToString() + ",";
                order += Row["avgPrice"].ToString() + ",";
                order += Row["rec"].ToString() + ",";
                order += Row["date"].ToString();
                order += ";";
            }
            
            return order.Substring(0, order.Length - 1);
        }
       
        [DllExport("getClose", CallingConvention = CallingConvention.StdCall)]
        public static string getClose()
        {
            Database db = new Database();
            DataTable dt = db.query(string.Format("select ticket,P.stock_id,S.portfolio_id,S.type from StockInPortfolio as S join PositionDetails as P on S.stock_id = P.stock_id and S.portfolio_id=P.portfolio_id and S.type = P.type where S.rec like '%CLOSE%'"));
            //get string of closed positions
            string order = "";
            foreach (DataRow row in dt.Rows)
            {
                order += row["ticket"].ToString() + ";";
            }
             return order.Substring(0,order.Length-1);
        }
        //delete all pending order and record
        [DllExport("cleanupPending", CallingConvention = CallingConvention.StdCall)]
        public static void cleanupPending()
        {
            Database db = new Database();
            db.execute_query("delete from pendingDetails");
            db.execute_query("delete from Stockinportfolio where rec like '%PENDING%';");
            
        }

        //  get tickets of pending orders
        [DllExport("getPending", CallingConvention = CallingConvention.StdCall)]
        public static string getPending()
        {
            Database db = new Database();
            DataTable table = db.query("select ticket,stock_id,portfolio_id,type from pendingDetails");
            string orders = "";
            foreach (DataRow row in table.Rows)
            {
                orders += row["ticket"] + ",";
                orders += row["stock_id"] + ",";
                orders += row["portfolio_id"] + ",";
                orders += row["type"] + ",";
            }
            return orders.Substring(0, orders.Length - 1);
        }

        //update the pending order
        // [DllExport("updatePending", CallingConvention = CallingConvention.StdCall)]
        //public static string updatePending(string stock_id, int portfolio_id, int ticket, int volume, double price)
        //{
        //     //TODO
        //    Database db = new Database();
        //     //copy all pendingdetiails to Positions Details
        //    db.execute_query(string.Format("insert into positionDetails select * from pendingDetails where tickket={0}",ticket));
        //     //change pending order in stockinportfolio into open 
               
        //}

        //return tickets of opening Position to check if it were autoclosed or not.
        [DllExport("getAutoClose", CallingConvention = CallingConvention.StdCall)]
        public static string getAutoClose()
        {
           string tickets="";
           string stock_id = "";
           string portfolio_id = "";
           string type = "";
          
            //return a list of closed position
            Database db = new Database();
           DataTable table = db.query("select stock_id,portfolio_id,type from StockInPortfolio where rec = ''");
            //query the ticket
            if( table.Rows.Count == 0) return "";
            DataTable ticket_table;
            foreach (DataRow row in table.Rows)
            {
         
                //make the select ticket query
                stock_id = row["stock_id"].ToString();
                portfolio_id = row["portfolio_id"].ToString();
                type = row["type"].ToString();
                ticket_table = db.query(String.Format("select ticket from PositionDetails where stock_id='{0}' and portfolio_id={1} and type = '{2}' ",stock_id,portfolio_id,type));
                
                //prepare the tickets string
                if (ticket_table.Rows.Count == 0)
                {
                    return "error";
                }
                foreach (DataRow ticket_row in ticket_table.Rows)
                {
                    tickets += ticket_row["ticket"].ToString()+";";
                }
            }
            return tickets.Substring(0,tickets.Length-1);
        }


        //cleanup all the orphan position
        [DllExport("cleanClose", CallingConvention = CallingConvention.StdCall)]
        public static void cleanClose()
        {
            Database db = new Database();
            string sql = "delete from StockInPortfolio where (rec ='' or rec like'%CLOSE%') and (select COUNT(P.ticket) from PositionDetails as P where StockInPortfolio.stock_id=P.stock_id and StockInPortfolio.portfolio_id=p.portfolio_id and StockInPortfolio.type=p.type) = 0";
             db.execute_query(sql);
        }

        //update pending order
        [DllExport("updatePending", CallingConvention = CallingConvention.StdCall)]
        public static void updatePending(string stock_id, int portfolio_id, int ticket, int volume, double price, string order_date, string rec_date, string rec, string type)
        {
            Database db = new Database();
            log("called");
            //if ticket =-1 aka position is NOT successfully opened then just set stock_ticket=-1 in StockRecommend table
            if (ticket == -1)
            {
                db.execute_query(string.Format("update StockRecommend set stock_ticket = -1 where  portfolio_id={0} and stock_id='{1}' and date ='{2}' and type ='{3}'",
                    portfolio_id, stock_id, rec_date, type));
                return;
            }
            //else
            //1. change rec in Stockinportfolio to pending
            //2. create new record in pendingDetails             
            //3. change stock recommend status to pending
            else
            {
                //1. change rec in Stockinportfolio to pending
                db.execute_query(string.Format("update StockInPortfolio set rec = 'PENDING' where portfolio_id={0} and stock_id='{1}' and type like '%{2}%' and rec like '%OPEN%';", portfolio_id, stock_id, type));                  
                //2. create new record in pendingDetails  
                db.execute_query(string.Format("insert into PendingDetails(portfolio_id,stock_id,type,volume,price,ticket) values ('{0}','{1}','{2}',{3},{4},{5});",
                    portfolio_id, stock_id, type, volume, price, ticket));
                //3. change stock recommend status to pending
                db.execute_query(string.Format("update StockRecommend set stock_ticket='PENDING', rec ='{0}' where  portfolio_id={1} and stock_id like '{2}' and date = '{3}';",
                    rec, portfolio_id, stock_id, rec_date));
                return;
            }
            
        }
        //update the ticket and the record
        [DllExport("updateOpen", CallingConvention = CallingConvention.StdCall)]
        public static void updateOpen(string stock_id, int portfolio_id, int ticket, int volume, double price, string order_date, string rec_date, string rec, string type)
        {

            //set up connection
            SqlConnection myConnection = new SqlConnection(getCString());
            string sql = "";
            try
            {
                myConnection.Open();
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            try
            {
                
                //if ticket =-1 aka position is NOT successfully opened then just set stock_ticket=-1 in StockRecommend table
                if (ticket < 0)
                {
                    sql += string.Format("update StockRecommend set stock_ticket = -1 where  portfolio_id={0} and stock_id='{1}' and date ='{2}' and type ='{3}'",
                        portfolio_id, stock_id, rec_date, type);

                }
                //else
                //1. insert order into Stock record
                //2. Create new record in PositionDetails
                //2. make new record in stockStockInPortfolio with real price date and volume
                //if the currency exist then stack up the volume, and update the new price

                // 3. update the stock ticket in Stockrecommended  
                else
                {
                    //1. insert order into stock Record
                    sql += string.Format("insert into StockRecord(portfolio_id,stock_id,stock_ticket,date,volume,price,status) values({0},'{1}',{2},'{3}',{4},{5},'{6}');",
                    portfolio_id, stock_id, ticket, order_date, volume, price, type);

                    //2. Create new record in PositionDetails
                    sql += string.Format("insert into PositionDetails(portfolio_id,stock_id,type,volume,price,ticket) values ('{0}','{1}','{2}',{3},{4},{5});",
                    portfolio_id, stock_id, type, volume, price, ticket);

                    //3.make new record in stockStockInPortfolio with real price date and volume
                    //check if the current position  exists in the portfolio
                    SqlDataReader myreader = null;
                    //date is excluded because there cannot exist two records of the same type in Stockinportfolio
                    string query = string.Format("select volume,avgprice from StockInPortfolio where portfolio_id={0} and stock_id='{1}' and type ='{2}' and rec ='';", portfolio_id, stock_id, type);
                    SqlCommand mycommand = new SqlCommand(query, myConnection);
                    myreader = mycommand.ExecuteReader();

                    //if exist stack up the volume and update the price
                    if (myreader.Read())
                    {
                        int old_volume = int.Parse(myreader["volume"].ToString());
                        double old_price = Double.Parse(myreader["avgprice"].ToString());
                        //calculate new price and volume
                        int new_volume = old_volume + volume;
                        double new_price = (price * volume + old_price * old_volume) / new_volume;
                        sql += string.Format("update StockInPortfolio set volume={0}, avgprice={1} where portfolio_id={2} and stock_id='{3}' and type like '%{4}%' and rec ='';", new_volume, new_price, portfolio_id, stock_id, type);
                    }

                    //else, create new record
                    else
                    {
                        sql += string.Format("insert into StockInPortfolio(stock_id,portfolio_id,type,volume,date,avgPrice,rec)  values('{0}',{1},'{2}',{3},'{4}',{5},'');",
                            stock_id, portfolio_id, type, volume, order_date, price);
                    }
                    myreader.Close();
                    //4. update the stock ticket in Stockrecommended
                    sql += string.Format("update StockRecommend set stock_ticket={0}, rec ='{1}' where  portfolio_id={2} and stock_id like '{3}' and date = '{4}';",
                    ticket, rec, portfolio_id, stock_id, rec_date);
                }
                SqlCommand myCommand = new SqlCommand(sql, myConnection);
                myCommand.ExecuteNonQuery();
                myConnection.Close();

            }
            catch (System.Exception e)
            {
                log(e.ToString() + "\n\n"+sql);
            }


        }

        
        [DllExport("updateClose", CallingConvention = CallingConvention.StdCall)]
        public static void updateClose(int ticket, string date, double volume, double price)
        {
            
            Database db = new Database();
            string sql = "";
            DataTable temp;
            try
            {
                //extract information from PositionDetails
                temp = db.query(String.Format("select portfolio_id,stock_id,type from PositionDetails where ticket ={0}",ticket));
                temp.Rows[0]["portfolio_id"].ToString();
                string portfolio_id = temp.Rows[0]["portfolio_id"].ToString();
                string stock_id = temp.Rows[0]["stock_id"].ToString();
                string status = temp.Rows[0]["type"].ToString();
                //delete the record in PositionsDetails
                string delete = string.Format("delete from PositionDetails where ticket={0};", ticket);   
                db.execute_query(delete);
                //update volume and price and volume in Stockinportfolio to reflect the change                        
                 
                    //if the last record is reach, no update is needed because the record in Stockinportfolio will be deleted eventually
                string nvolume = db.query("select SUM(volume) as volume from PositionDetails").Rows[0]["volume"].ToString();
                if (nvolume.Length>0)
                {
                    int new_volume = Int32.Parse(nvolume);
                    double new_price = Double.Parse(db.query("select SUM(volume*price)/SUM(volume) as avgPrice from PositionDetails").Rows[0]["avgPrice"].ToString());
                
                
                 //update new volume and price
                sql += string.Format("update Stockinportfolio set volume={3} , avgPrice = {4} where portfolio_id = {0} AND stock_id = '{1}' AND type = '{2}' and rec ='' ;",portfolio_id, stock_id
                        ,status, new_volume, new_price);
               
                }

                //insert in StockRecord                        
                sql += String.Format("insert into stockrecord(portfolio_id,stock_id,stock_ticket,date,volume,price,status) values({0},'{1}',{2},'{3}',{4},{5},'{6}');",
                       portfolio_id, stock_id, ticket, date, -volume, price, status);
                //log(sql.Replace(";","\n") + "\n");
                db.execute_query(sql);

            }
            catch (System.Exception e)
            {
                log(e.ToString() + "\n" + sql);
            }
            
        }

        [DllExport("updateCash", CallingConvention = CallingConvention.StdCall)]
         public static void updateCash(int portfolio_id, double cash, double capital)
        {
            Database db = new Database();
           string sql = string.Format("update Portfolio set cash={0},capital={1} where portfolio_id={2};", cash, capital, portfolio_id);
           db.execute_query(sql);
        }

        [DllExport("updateprice", CallingConvention = CallingConvention.StdCall)]
        public static void updatePrice(string symbol, string date_time, double open, double close, double high, double low, int volume)
        {

            SqlConnection myConnection = new SqlConnection(getCString());
            myConnection.Open();            
            //SqlCommand myCommand = new SqlCommand("insert into Price values ('aba',GetDate(),4,3)", myConnection);
            string sql = string.Format("insert into Stock(stock_id,date,openPrice, closedPrice,maxPrice,minPrice,matchedVolume ) values('{0}','{1}',{2},{3},{4},{5},{6});", symbol, date_time,
                open, close, high, low, volume);
            
            SqlCommand myCommand = new SqlCommand(sql, myConnection);
            //SqlCommand myCommand = new SqlCommand("insert into Stock(stock_id,date,openPrice, closedPrice,maxPrice,minPrice,matchedVolume ) values ('" + symbol + "','" + date_time + "',"
            // + open + "," + close + "," + high + "," + low + ","+ volume + ")", myConnection);
            
            try
            {
                myCommand.ExecuteNonQuery();
            }

            catch (System.Exception e)
            {

                log(sql + "\n" + e.ToString());
            }

            myConnection.Close();
        }

        //log the error
        public static void log(string input)
        {
            System.IO.File.AppendAllText(@"C:\WriteLines.txt", input);
        }

        public static string getCString()
        {
            return "user id=berichadmin;" +
                                    "password=berich;server=apps.berich.vn;" +
                                    "trusted_connection=no;" +
                                    "database=forex; " +
                                    "connection timeout=90;" +
         "persist security info=true;" +
         "user instance=false;";
            //       myconnection = new SqlConnection("user id=sa;" +
            //                           "password=999999;server=localhost\\SQL;" +
            //                           "trusted_connection=no;" +
            //                           "database=forex; " +
            //                           "connection timeout=90;" +
            //"persist security info=true;" +
            //"user instance=false;");
        }
    }   
}
