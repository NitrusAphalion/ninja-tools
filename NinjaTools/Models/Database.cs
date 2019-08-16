using Stylet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Linq;
using System.Windows;

namespace NinjaTools.Models
{
    public class Database : IDisposable
    {
        public SqlCeConnection              Connection  { get; set; } = null;
        public DataSet                      DisplayData { get; set; } = new DataSet();
        public BindableCollection<string>   Tables      { get; set; } = new BindableCollection<string>();

        public Database(string path)
        {
            try
            {
                Connection = new SqlCeConnection($"Data Source = {path};");
                Connection.Open();

                SqlCeCommand cmd    = Connection.CreateCommand();
                cmd.CommandText     = "SELECT table_name FROM information_schema.tables WHERE TABLE_TYPE <> 'VIEW'";

                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        Tables.Add(reader["TABLE_NAME"].ToString());
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            finally
            {
                Connection.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void QueryTable(string table)
        {
            UpdateDataSet($"SELECT * FROM {table}");
        }

        public void UpdateDataSet(string query)
        {
            try
            {
                Connection.Open();

                SqlCeCommand        cmd = Connection.CreateCommand();
                cmd.CommandText         = query;
                SqlCeDataAdapter    adp = new SqlCeDataAdapter(cmd);

                using (DataSet temp = new DataSet())
                {
                    adp.Fill(temp);

                    // Here we convert the Int64 datetime to an actual DateTime for nice display
                    // This code is ridiculous but I dont know another way to do it
                    List<int> dateTimeColumns = new List<int>() { temp.Tables[0].Columns.IndexOf("TimeUtc"),
                                                                  temp.Tables[0].Columns.IndexOf("Time"),
                                                                  temp.Tables[0].Columns.IndexOf("StatementDate"),
                                                                  temp.Tables[0].Columns.IndexOf("Gtd") };
                    IEnumerable<int> dateTimeConverts = dateTimeColumns.Where(i => i != -1);

                    if (!dateTimeConverts.Any())
                    {
                        DisplayData = temp;
                        return;
                    }

                    DisplayData = temp.Clone();

                    foreach (int i in dateTimeConverts)
                        DisplayData.Tables[0].Columns[i].DataType = typeof(DateTime);

                    foreach (DataRow row in temp.Tables[0].Rows)
                    {
                        DataRow     convertedRow    = DisplayData.Tables[0].NewRow();
                        object[]    cloneRow        = row.ItemArray.Clone() as object[];

                        foreach (int i in dateTimeConverts)
                            cloneRow[i] = new DateTime(Convert.ToInt64(cloneRow[i]), DateTimeKind.Utc);

                        DisplayData.Tables[0].Rows.Add(cloneRow);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            finally
            {
                Connection.Close();
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                DisplayData.Dispose();
        }
    }
}