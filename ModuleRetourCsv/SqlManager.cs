using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.IO;

namespace ModuleRetourCsv
{

    class SqlManager : IDisposable
    {
        SqlConnection connection;

        public SqlManager(string serverName, string database, string username, string pwd)
        {
            string connectionString = null;
            if (!(username.Equals("") || pwd.Equals("")))
            {
                Console.WriteLine("connection par user et pwd");
                connectionString = "Data Source=" + serverName + ";Initial Catalog=" + database + ";User ID=" + username + ";Password=" + pwd;
            }
            else
            {
                Console.WriteLine("connexion par authentification windows");
                connectionString = "Data Source=" + serverName + ";Initial Catalog=" + database + ";Integrated Security=SSPI";
            }
            connection = new SqlConnection(connectionString);
            try
            {
                connection.Open();
                Console.WriteLine("La connexion à la base de données a été établie avec succès.");
            }
            catch (Exception e)
            {
                Console.WriteLine("La connexion à la base de données a échoué : " + e.Message);
                Dispose();
            }
        }

        /**
         * exécute les requêtes SQL et rempli un DataSet
         */
        public DataSet ExecuteSqlQueries(Dictionary<string, string> queries)
        {
            if (connection != null && connection.State == ConnectionState.Open)
            {
                DataSet dataSet = new DataSet();
                foreach (var query in queries)
                {
                    using (SqlCommand command = new SqlCommand(query.Key, connection))
                    {
                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                        adapter.Fill(dataSet, query.Key);
                    }
                }
                return dataSet;
            }
            else
            {
                throw new Exception("La connexion n'est pas ouverte.");
            }
        }
        /**
         * génére des fichiers CSV à partir des données d'un DataSet 
         */
        public void GenerateCsvFiles(DataSet dataSet, Dictionary<string, string> filesPath)
        {
            Console.WriteLine("***************Export en CSV******************");
            foreach (var file in filesPath)
            {
                DataTable dataTable = dataSet.Tables[file.Key];
                //Console.WriteLine("datatable:" + dataTable);
                DataView dataView = dataTable.DefaultView;
                string filePath = file.Value;
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    // En-tête CSV
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        writer.Write($"{column.ColumnName};");
                    }
                    writer.WriteLine();

                    // Lignes CSV
                    foreach (DataRowView rowView in dataView)
                    {
                        DataRow row = rowView.Row;
                        for (int i = 0; i < dataTable.Columns.Count; i++)
                        {
                            string valueString = row.IsNull(i) ? "" : row[i].ToString();
                            writer.Write($"{valueString};");
                        }
                        writer.WriteLine();
                    }
                }
            }
        }
        public void Dispose()
        {
            if (connection != null && connection.State == ConnectionState.Open)
            {
                connection.Close();
            }
        }
        /************************** old***************************/
        public void SqlQueryToCsv(String req, String filePath)
        {
            if (connection != null && connection.State == ConnectionState.Open)
            {
                using (SqlCommand command = new SqlCommand(req, connection))
                {
                    //exécuter la commande et obtenir un DataReader
                    SqlDataReader reader = command.ExecuteReader();

                    using (StreamWriter writer = new StreamWriter(filePath))
                    {
                        //ecrire l'en-tête du fichier CSV avec les noms des colonnes
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            writer.Write($"{reader.GetName(i)};");
                        }
                        writer.WriteLine();

                        //parcourir les lignes du DataReader
                        while (reader.Read())
                        {
                            //ecrire les données de chaque ligne dans le fichier CSV
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                object value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                string valueString = value == null ? "" : value.ToString();
                                //Console.WriteLine(valueString);
                                writer.Write($"{valueString};");
                            }
                            writer.WriteLine();
                        }
                        //fermer le DataReader
                        reader.Close();
                    }
                }
            }
        }

        public void SqlQueriesToCsv2(Dictionary<string, string> queriesAndFiles)
        {
            if (connection != null && connection.State == ConnectionState.Open)
            {
                using (DataSet dataSet = new DataSet())
                {
                    foreach (var queryAndFile in queriesAndFiles)
                    {
                        using (SqlCommand command = new SqlCommand(queryAndFile.Key, connection))
                        {
                            //exécuter la requete sql et remplir le DataSet
                            SqlDataAdapter adapter = new SqlDataAdapter(command);
                            adapter.Fill(dataSet, queryAndFile.Key);
                        }
                    }

                    //parcourir sur les données du DataSet et écrire les fichiers CSV
                    foreach (var queryAndFile in queriesAndFiles)
                    {
                        DataTable dataTable = dataSet.Tables[queryAndFile.Key];
                       // Console.WriteLine("donnée dataset:" + queryAndFile.Value);
                        DataView dataView = dataTable.DefaultView;
                        //dataView.RowFilter = queryAndFile.Value;      
                        string filePath = queryAndFile.Value;
                        //Console.WriteLine("query:" + filePath);
                        using (StreamWriter writer = new StreamWriter(filePath))
                        {
                            //entete CSV
                            foreach (DataColumn column in dataTable.Columns)
                            {
                                writer.Write($"{column.ColumnName};");
                            }
                            writer.WriteLine();

                            //parcourir les lignes de la vue 
                            foreach (DataRowView rowView in dataView)
                            {
                                DataRow row = rowView.Row;
                                //lignes CSV
                                for (int i = 0; i < dataTable.Columns.Count; i++)
                                {
                                    string valueString = row.IsNull(i) ? "" : row[i].ToString();
                                    writer.Write($"{valueString};");
                                }
                                writer.WriteLine();
                            }
                        }
                    }
                }
            }
        }
    }
}
