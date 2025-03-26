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

            // Listado de tablas a consultar
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

            // Valor por defecto si no está definido
            return 0;
        }

        private async void btnSincronizar_Click(object sender, EventArgs e)
        {
            btnSincronizar.Enabled = false; // Bloquear botón mientras sincroniza
            progressBarSync.Value = 0; // Reiniciar progreso
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

            progressBarSync.Value = 100; // Asegurar que llegue al 100%
            MessageBox.Show("Sincronización completada.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

            progressBarSync.Value = 0; // Reiniciar progreso inicial
            btnSincronizar.Enabled = true; // Reactivar el botón
            progressBarSync.Enabled = false; // Deshabilitar la barra
        }

        private void SincronizarTabla(string tableName, int totalSteps)
        {
            List<Dictionary<string, object>> records = accessDb.GetUnsyncedRecords(tableName);
            Console.WriteLine($"🔍 Registros no sincronizados en {tableName}: {records.Count}");

            if (records.Count > 0)
            {
                // Injectar sucursalid solo en OrderHeaders
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

        private string[] GetTablasASincronizar()
        {
            return new[]
            {
                "OrderHeaders",
                "OrderPayments",
                "OrderTransactions",
                "OnAccountCharges",
                "RegisterCashiers"

                /*"MenuCategories",
                "MenuExplosion",
                "MenuGroups",
                "MenuGroupSchedule",
                "MenuItemIngredients",
                "MenuItemPrices",
                "MenuItems",
                "MenuModifierPopUps",
                "MenuModifiers",
                "ModBuilderDetails",
                "ModBuilderTemplates",
                "EmployeeFiles",
                "Discounts"*/

            };
        }


        private void progressBarSync_Click(object sender, EventArgs e) { }
    }
}
