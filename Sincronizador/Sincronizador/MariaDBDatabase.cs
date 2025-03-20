using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using System.Windows.Forms;
using System.Linq;
using System.IO;
namespace Sincronizador
{
    public class MariaDBDatabase
    {
        private string connectionString;

        public MariaDBDatabase()
        {
            connectionString = ReadConnectionStringFromConfig();

            if (string.IsNullOrEmpty(connectionString))
            {
                MessageBox.Show("Error: No se pudo obtener la conexión desde config.ini.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private string ReadConnectionStringFromConfig()
        {
            string iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

            if (!File.Exists(iniPath))
            {
                MessageBox.Show("No se encontró config.ini.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            Dictionary<string, string> config = new Dictionary<string, string>();
            string currentSection = "";

            foreach (var line in File.ReadAllLines(iniPath))
            {
                string trimmedLine = line.Trim();

                // Detectar secciones en el archivo INI
                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    continue;
                }

                // Solo leer datos dentro de [MariaDB]
                if (currentSection == "MariaDB" && trimmedLine.Contains("="))
                {
                    var parts = trimmedLine.Split(new[] { '=' }, 2);
                    config[parts[0].Trim()] = parts[1].Trim();
                }
            }

            // Verificar qué valores se han leído
            Console.WriteLine("Valores leídos de config.ini:");
            foreach (var kvp in config)
            {
                Console.WriteLine($"{kvp.Key} = {kvp.Value}");
            }

            // Validar que todas las claves necesarias existan
            if (!config.ContainsKey("Host") || !config.ContainsKey("Name") ||
                !config.ContainsKey("User") || !config.ContainsKey("Password") ||
                !config.ContainsKey("SslMode"))
            {
                MessageBox.Show("El archivo config.ini no contiene toda la configuración necesaria de MariaDB.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            // Construir la cadena de conexión
            return $"Server={config["Host"]};Database={config["Name"]};User ID={config["User"]};Password={config["Password"]};SslMode={config["SslMode"]};";
        }



        public bool TestConnection()
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                MessageBox.Show("No se pudo establecer la conexión porque la cadena de conexión es nula o está vacía.",
                                "Error de Conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    Console.WriteLine("Conexión exitosa a MariaDB.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al conectar con MariaDB: " + ex.Message,
                                "Error de Conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
