using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sincronizador
{
    public partial class Form1 : Form
    {
        private AccessDatabase accessDb;
        private MariaDBDatabase mariaDb;

        public Form1()
        {
            InitializeComponent();
            accessDb = new AccessDatabase(); // Crear instancia de AccessDatabase
            mariaDb = new MariaDBDatabase(); // Crear instancia de MariaDB
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ConsultarRegistros();

            // Intentar conectar con MariaDB al iniciar
            if (mariaDb.TestConnection())
            {
                Console.WriteLine("Conexión con MariaDB exitosa.");

                // Listado de tablas a contar
                string[] tablas = { "OrderHeaders", "OrderPayments", "OrderTransactions" };

                foreach (string tabla in tablas)
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

            // Listado de tablas a consultar
            string[] tablas = { "OrderHeaders", "OrderPayments", "OrderTransactions" };

            foreach (string tabla in tablas)
            {
                DataTable dt = accessDb.GetRecords(tabla);
                Console.WriteLine(dt.Rows.Count > 0
                    ? $" Registros encontrados en {tabla}: {dt.Rows.Count}"
                    : $" No hay registros en {tabla}.");
            }
        }

        private async void btnSincronizar_Click(object sender, EventArgs e)
        {
            btnSincronizar.Enabled = false; // Bloquear botón mientras sincroniza
            progressBarSync.Value = 0; // Reiniciar progreso
            progressBarSync.Enabled = true;

            string[] tablas = { "OrderHeaders", "OrderPayments", "OrderTransactions" };
            int totalSteps = tablas.Length; // Número de pasos dinámico

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

            progressBarSync.Value = 100; // Asegurar que llegue al 100%
            MessageBox.Show("Sincronización completada.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

            progressBarSync.Value = 0; // Reiniciar progreso inicial
            btnSincronizar.Enabled = true; // Reactivar el botón
            progressBarSync.Enabled = false; // Deshabilitar la barra
        }

        private void SincronizarTabla(string tableName, int totalSteps)
        {
            // Obtener todos los registros no sincronizados de una vez
            List<Dictionary<string, object>> records = accessDb.GetUnsyncedRecords(tableName);
            Console.WriteLine($"🔍 Registros no sincronizados en {tableName}: {records.Count}");

            if (records.Count > 0)
            {
                // Insertar todos en MariaDB en un solo paso
                mariaDb.InsertRecordsIntoMariaDB(tableName, records);
                UpdateProgressBar(100 / totalSteps);

                // Marcar como sincronizados en ambas bases de datos
                accessDb.MarkRecordsAsSynced(tableName);
                mariaDb.MarkRecordsAsSyncedInMariaDB(tableName);
                UpdateProgressBar(100 / totalSteps);
            }
        }

        private void UpdateProgressBar(int step)
        {
            if (progressBarSync.InvokeRequired)
            {
                progressBarSync.Invoke(new Action(() =>
                {
                    int newValue = progressBarSync.Value + step;
                    progressBarSync.Value = Math.Min(newValue, progressBarSync.Maximum); // 🔹 Evita que supere el máximo
                    progressBarSync.Refresh();
                }));
            }
            else
            {
                int newValue = progressBarSync.Value + step;
                progressBarSync.Value = Math.Min(newValue, progressBarSync.Maximum); // 🔹 Evita que supere el máximo
                progressBarSync.Refresh();
            }
        }

        private void progressBarSync_Click(object sender, EventArgs e) { }
    }
}
