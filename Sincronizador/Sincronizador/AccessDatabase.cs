using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Windows.Forms;

namespace Sincronizador
{
    public class AccessDatabase
    {
        private string connectionString;

        public AccessDatabase()
        {
            string dbPath = ReadDatabasePath();

            if (string.IsNullOrEmpty(dbPath))
            {
                MessageBox.Show(" No se encontró la ruta de la base de datos en config.ini.", "Error de Conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string provider = dbPath.EndsWith(".mdb", StringComparison.OrdinalIgnoreCase)
                ? "Microsoft.Jet.OLEDB.4.0"
                : "Microsoft.ACE.OLEDB.12.0";

            connectionString = $"Provider={provider};Data Source={dbPath};Persist Security Info=False;";
        }

        private string ReadDatabasePath()
        {
            string iniPath = "config.ini";
            if (!File.Exists(iniPath))
            {
                return null;
            }

            foreach (var line in File.ReadAllLines(iniPath))
            {
                if (line.StartsWith("Path="))
                {
                    return line.Substring("Path=".Length).Trim();
                }
            }

            return null;
        }

        public DataTable GetRecords(string tableName)
        {
            DataTable dt = new DataTable();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(connectionString))
                {
                    conn.Open();
                    string query = $"SELECT * FROM {tableName}";
                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener datos de {tableName}: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return dt;
        }

        public List<Dictionary<string, object>> GetUnsyncedRecords(string tableName)
        {
            List<Dictionary<string, object>> records = new List<Dictionary<string, object>>();

            try
            {
                using (OleDbConnection conn = new OleDbConnection(connectionString))
                {
                    conn.Open();
                    string query = $"SELECT * FROM {tableName} WHERE Sincronizado = FALSE";

                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Dictionary<string, object> record = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                record[reader.GetName(i)] = reader.GetValue(i);
                            }

                            records.Add(record);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener datos no sincronizados de {tableName}: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return records;
        }

        public void MarkRecordsAsSynced(string tableName)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(connectionString))
                {
                    conn.Open();
                    string query = $"UPDATE {tableName} SET Sincronizado = TRUE WHERE Sincronizado = FALSE";

                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    {
                        int updatedRows = cmd.ExecuteNonQuery();
                        Console.WriteLine($"✔ {updatedRows} registros marcados como sincronizados en {tableName}.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al actualizar registros en {tableName} en Access: {ex.Message}");
            }
        }

        public void MarkAllAsSynced()
        {
            MarkRecordsAsSynced("OrderHeaders");
            MarkRecordsAsSynced("OrderPayments");
            MarkRecordsAsSynced("OrderTransactions");
        }

    }
}
