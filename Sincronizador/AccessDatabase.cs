using System;
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
                MessageBox.Show("⚠️ No se encontró la ruta de la base de datos en config.ini.", "Error de Conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Determinar el provider correcto según la extensión del archivo
            string provider = dbPath.EndsWith(".mdb", StringComparison.OrdinalIgnoreCase)
                ? "Microsoft.Jet.OLEDB.4.0"
                : "Microsoft.ACE.OLEDB.12.0";

            // Crear la cadena de conexión a Access
            connectionString = $@"Provider={provider};Data Source={dbPath};Persist Security Info=False;";
        }

        // Método para leer la ruta de la base de datos desde config.ini
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

        // Método para obtener registros de la tabla OrderHeaders
        public DataTable GetOrderHeaders()
        {
            DataTable dt = new DataTable();

            try
            {
                using (OleDbConnection conn = new OleDbConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM OrderHeaders"; // Ajusta la consulta si es necesario

                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                    {
                        adapter.Fill(dt); // Llena el DataTable con los datos
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Error al obtener datos de Access: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return dt;
        }
    }
}
