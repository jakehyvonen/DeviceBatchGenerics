using Microsoft.Data.ConnectionUI;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using DeviceBatchGenerics.Support.Bases;
using EFDeviceBatchCodeFirst;

namespace DeviceBatchGenerics.Support
{
    public class DBConnectionManager : NotifyUIBase
    {
        public DBConnectionManager()
        {
            CheckForDBConnection();
        }
        DeviceBatchContext ctx;
        void CheckForDBConnection()
        {
            try
            {
                //check to see if connectionstring will open connection with db
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var connectionStringsSection = (ConnectionStringsSection)config.GetSection("connectionStrings");
                var connstring = connectionStringsSection.ConnectionStrings["ServerConnectionString"].ConnectionString;
                Debug.WriteLine("connectionString from configmanager is: " + connstring);

                bool dumbBool = false;
                //var efconfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None,);
                using(ctx = new DeviceBatchContext(connstring))
                {
                    Debug.WriteLine("ctx.Database.Connection.ConnectionString: " + ctx.Database.Connection.ConnectionString);
                    if(ctx.Database.Exists())
                    {
                        Debug.WriteLine("ctx.Database.Exists()==true");
                        var firstDevBatch = ctx.DeviceBatches.First();
                        Debug.WriteLine("Successfully opened DB connection. First DevBatch.Name is " + firstDevBatch.Name);
                    }
                    else
                    {
                        Debug.WriteLine("ctx.Database.Exists()==false");
                        ctx.Dispose();
                        dumbBool = true;
                        //PromptForDBCredentials();                       
                    }
                }
                if(dumbBool)
                {
                    PromptForDBCredentials();
                    CheckForDBConnection();
                }
                //ctx.Database.Connection.ConnectionString = connstring;
                //ctx.Database.Connection.Open();
               
            }
            catch (SqlException e)
            {
                Debug.WriteLine("SqlException: " + e.ToString());
                //ctx.Dispose();
                //System.Windows.MessageBox.Show(e.ToString());
                //ctx.Database.Connection.Close();
                PromptForDBCredentials();
                CheckForDBConnection();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.ToString());
                //ctx.Dispose();
                PromptForDBCredentials();
                CheckForDBConnection();
            }
        }
        void PromptForDBCredentials(string connString = "ServerConnectionString")
        {
            try
            {
                string outConnectionString;
                if (TryGetDataConnectionStringFromUser(out outConnectionString))
                {
                    Debug.WriteLine("got connection string from user");
                    //ctx.Database.Connection.ConnectionString = outConnectionString;
                }
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var connectionStringsSection = (ConnectionStringsSection)config.GetSection("connectionStrings");
                connectionStringsSection.ConnectionStrings[connString].ConnectionString = outConnectionString;
                config.Save();
                ConfigurationManager.RefreshSection("connectionStrings");
                config.Save();

                //ctx.Database.Connection.Close();
                //ctx.Database.Connection.ConnectionString = outConnectionString;
                //ctx.Database.Connection.Open();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                MessageBox.Show(e.ToString());
            }
        }
        bool TryGetDataConnectionStringFromUser(out string outConnectionString)
        {
            using (var dialog = new DataConnectionDialog())
            {
                DialogResult userChoice = DialogResult.Abort;
                try
                {
                    DataSource.AddStandardDataSources(dialog);
                    dialog.SelectedDataSource = DataSource.SqlDataSource;
                    dialog.SelectedDataProvider = DataProvider.SqlDataProvider;
                    dialog.ConnectionString = "Data Source=192.168.1.41,49694;Initial Catalog=DevelDevBatchDB;Persist Security Info=True;";
                    userChoice = DataConnectionDialog.Show(dialog);
                }
                catch (SqlException se)
                {
                    Debug.WriteLine("SqlException: " + se.ToString());
                    //MessageBox.Show(se.ToString());
                }
                if (userChoice == DialogResult.OK)
                {
                    outConnectionString = dialog.ConnectionString;
                    return true;
                }
                else
                {
                    outConnectionString = null;
                    return false;
                }
            }
        }
    }

}
