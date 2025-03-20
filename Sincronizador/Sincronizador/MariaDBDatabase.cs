using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using System.Windows.Forms;
using System.Linq;

namespace Sincronizador
{
    public class MariaDBDatabase
    {
        private string connectionString;

        public MariaDBDatabase()
        {
            connectionString = "Server=localhost;Database=RestaurantDB;User ID=root;Password=Pass123*;SslMode=none;";
        }

        public bool TestConnection()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    Console.WriteLine(" Conexión exitosa a MariaDB.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(" Error al conectar con MariaDB: " + ex.Message, "Error de Conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public void InsertOrdersIntoMariaDB(List<Dictionary<string, object>> orders)
        {
            InsertRecordsIntoMariaDB("OrderHeaders", orders);
        }

        public void InsertOrderPaymentsIntoMariaDB(List<Dictionary<string, object>> payments)
        {
            InsertRecordsIntoMariaDB("OrderPayments", payments);
        }

        public void InsertOrderTransactionsIntoMariaDB(List<Dictionary<string, object>> transactions)
        {
            InsertRecordsIntoMariaDB("OrderTransactions", transactions);
        }

        private void InsertRecordsIntoMariaDB(string tableName, List<Dictionary<string, object>> records)
        {
            if (records.Count == 0)
            {
                Console.WriteLine($" No hay datos nuevos para sincronizar en {tableName}.");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    foreach (var record in records)
                    {
                        var columns = string.Join(", ", record.Keys);
                        var parameters = string.Join(", ", record.Keys.Select(key => "@" + key));

                        string query = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

                        using (MySqlCommand cmd = new MySqlCommand(query, conn))
                        {
                            foreach (var key in record.Keys)
                            {
                                object value = record[key];
                                if (value is string && string.IsNullOrWhiteSpace((string)value))
                                {
                                    value = DBNull.Value;
                                }
                                cmd.Parameters.AddWithValue("@" + key, value);
                            }

                            cmd.ExecuteNonQuery();
                        }
                    }

                    Console.WriteLine($" {records.Count} registros sincronizados con {tableName} en MariaDB.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al insertar datos en {tableName} en MariaDB: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void MarkOrdersAsSyncedInMariaDB()
        {
            MarkRecordsAsSyncedInMariaDB("OrderHeaders");
        }

        public void MarkRecordsAsSyncedInMariaDB(string tableName)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = $"UPDATE {tableName} SET Sincronizado = TRUE WHERE Sincronizado = FALSE";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        int updatedRows = cmd.ExecuteNonQuery();
                        Console.WriteLine($" {updatedRows} registros actualizados como sincronizados en {tableName} en MariaDB.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar registros en {tableName} en MariaDB: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public int GetOrderCount()
        {
            int count = 0;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM OrderHeaders";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        count = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(" Error al obtener el conteo de órdenes: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return count;
        }
    }
}
