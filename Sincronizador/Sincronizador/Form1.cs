using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Sincronizador
{
    public partial class Form1 : Form
    {
        private AccessDatabase accessDb;
        private MariaDBDatabase mariaDb;

        private static readonly Dictionary<string, string> CamposClavePorTabla = new Dictionary<string, string>()

        {
            { "OrderHeaders", "OrderID" },
            { "OrderPayments", "OrderID" },
            { "OrderTransactions", "OrderID" },
            { "OnAccountCharges", "OrderID" },
            { "RegisterCashiers", "CashierID" }
        };

        public Form1()
        {
            InitializeComponent();
            accessDb = new AccessDatabase();
            mariaDb = new MariaDBDatabase();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ConsultarRegistros();

            if (mariaDb.TestConnection())
            {
                Console.WriteLine("Conexión con MariaDB exitosa.");
                foreach (string tabla in GetTablasASincronizar())
                {
                    int count = mariaDb.GetTableCount(tabla);
                    Console.WriteLine($" Registros en {tabla}: {count}");
                }
            }
            else
            {
                Console.WriteLine("No se pudo conectar a MariaDB.");
            }
        }

        private void ConsultarRegistros()
        {
            if (accessDb == null)
            {
                Console.WriteLine(" No se pudo conectar a la base de datos de Access.");
                return;
            }

            foreach (string tabla in GetTablasASincronizar())
            {
                DataTable dt = accessDb.GetRecords(tabla);
                Console.WriteLine(dt.Rows.Count > 0
                    ? $" Registros encontrados en {tabla}: {dt.Rows.Count}"
                    : $" No hay registros en {tabla}.");
            }
        }

        private int GetSucursalIDDesdeConfig()
        {
            string rutaConfig = "config.ini";

            if (File.Exists(rutaConfig))
            {
                foreach (string linea in File.ReadAllLines(rutaConfig))
                {
                    if (linea.StartsWith("SucursalID="))
                    {
                        string valor = linea.Split('=')[1].Trim();
                        if (int.TryParse(valor, out int sucursalID))
                        {
                            return sucursalID;
                        }
                    }
                }
            }

            return 0;
        }

        private async void btnSincronizar_Click(object sender, EventArgs e)
        {
            btnSincronizar.Enabled = false;
            progressBarSync.Value = 0;
            progressBarSync.Enabled = true;

            string[] tablas = GetTablasASincronizar();
            int totalSteps = tablas.Length;

            await Task.Run(() =>
            {
                try
                {
                    foreach (string tabla in tablas)
                    {
                        SincronizarTabla(tabla, totalSteps);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error en la sincronización: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });

            accessDb.MarkAllAsSynced(GetTablasASincronizar());

            progressBarSync.Value = 100;
            MessageBox.Show("Sincronización completada.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

            progressBarSync.Value = 0;
            btnSincronizar.Enabled = true;
            progressBarSync.Enabled = false;
        }

        private void SincronizarTabla(string tableName, int totalSteps)
        {
            List<Dictionary<string, object>> records = accessDb.GetUnsyncedRecords(tableName);
            Console.WriteLine($"🔍 Registros no sincronizados en {tableName}: {records.Count}");

            if (records.Count > 0)
            {
                if (tableName == "OrderHeaders")
                {
                    int sucursalID = GetSucursalIDDesdeConfig();
                    foreach (var record in records)
                    {
                        if (!record.ContainsKey("sucursalid"))
                        {
                            record["sucursalid"] = sucursalID;
                        }
                    }
                }

                mariaDb.InsertRecordsIntoMariaDB(tableName, records);
                UpdateProgressBar(100 / totalSteps);

                accessDb.MarkRecordsAsSynced(tableName);
                mariaDb.MarkRecordsAsSyncedInMariaDB(tableName);
                UpdateProgressBar(100 / totalSteps);

                EscribirLog(tableName, records.Count, GetSucursalIDDesdeConfig(), records);
            }
            else
            {
                EscribirLog(tableName, 0, GetSucursalIDDesdeConfig(), new List<Dictionary<string, object>>());
            }
        }

        private void EscribirLog(string tabla, int cantidad, int sucursalId, List<Dictionary<string, object>> registros)
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sincronizacion.log");
            string fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string campoClave = CamposClavePorTabla.ContainsKey(tabla) ? CamposClavePorTabla[tabla] : null;

            List<string> ids = new List<string>();
            if (!string.IsNullOrEmpty(campoClave))
            {
                foreach (var registro in registros)
                {
                    if (registro.TryGetValue(campoClave, out object value) && value != null)
                    {
                        string id = value.ToString();
                        if (!ids.Contains(id))
                            ids.Add(id);
                    }
                }
            }

            string linea = $"[{fecha}] Tabla: {tabla}";
            if (ids.Count > 0)
                linea += $" ({campoClave}: {string.Join(", ", ids)})";

            linea += $" | SucursalID: {sucursalId} | Registros insertados: {cantidad}";

            File.AppendAllText(logPath, linea + Environment.NewLine);
        }

        private void UpdateProgressBar(int step)
        {
            if (progressBarSync.InvokeRequired)
            {
                progressBarSync.Invoke(new Action(() =>
                {
                    int newValue = progressBarSync.Value + step;
                    progressBarSync.Value = Math.Min(newValue, progressBarSync.Maximum);
                    progressBarSync.Refresh();
                }));
            }
            else
            {
                int newValue = progressBarSync.Value + step;
                progressBarSync.Value = Math.Min(newValue, progressBarSync.Maximum);
                progressBarSync.Refresh();
            }
        }

        private string[] GetTablasASincronizar()
        {
            return new[]
            {
                "OrderHeaders",
                "OrderPayments",
                "OrderTransactions",
                "OnAccountCharges",
                "RegisterCashiers"
            };
        }

        private void progressBarSync_Click(object sender, EventArgs e) { }
        private void pictureBox1_Click(object sender, EventArgs e) { }
    }
}
