using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
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
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ConsultarRegistros();
            mariaDb = new MariaDBDatabase(); // Crear instancia de MariaDB

            // Intentar conectar con MariaDB al iniciar
            if (mariaDb.TestConnection())
            {
                int orderCount = mariaDb.GetOrderCount();
                Console.WriteLine($" Registros en OrderHeaders: {orderCount}");
            }

        }

        private void ConsultarRegistros()
        {
            if (accessDb == null)
            {
                Console.WriteLine(" No se pudo conectar a la base de datos de Access.");
                return;
            }

            DataTable dt = accessDb.GetOrderHeaders();

            if (dt.Rows.Count > 0)
            {
                Console.WriteLine(" Registros encontrados:");
                foreach (DataRow row in dt.Rows)
                {
                    Console.WriteLine($"OrderID: {row["OrderID"]}, Fecha: {row["OrderDateTime"]}");
                }
            }
            else
            {
                Console.WriteLine(" No hay registros en OrderHeaders.");
            }
        }

        private async void btnSincronizar_Click(object sender, EventArgs e)
        {
            btnSincronizar.Enabled = false; // Bloquear botón mientras sincroniza
            progressBarSync.Value = 0; // Reiniciar progreso
            progressBarSync.Enabled = true;

            int totalSteps = 6; // Número de pasos

            await Task.Run(() =>
            {
                try
                {
                    // Sincronizar Orders
                    List<Dictionary<string, object>> orders = accessDb.GetUnsyncedOrders();
                    mariaDb.InsertOrdersIntoMariaDB(orders);
                    UpdateProgressBar(100 / totalSteps);

                    accessDb.MarkOrdersAsSynced();
                    mariaDb.MarkOrdersAsSyncedInMariaDB();
                    UpdateProgressBar(100 / totalSteps);

                    // Sincronizar OrderPayments
                    List<Dictionary<string, object>> payments = accessDb.GetUnsyncedOrderPayments();
                    mariaDb.InsertOrderPaymentsIntoMariaDB(payments);
                    UpdateProgressBar(100 / totalSteps);

                    accessDb.MarkRecordsAsSynced("OrderPayments");
                    mariaDb.MarkRecordsAsSyncedInMariaDB("OrderPayments");
                    UpdateProgressBar(100 / totalSteps);

                    // Sincronizar OrderTransactions
                    List<Dictionary<string, object>> transactions = accessDb.GetUnsyncedOrderTransactions();
                    mariaDb.InsertOrderTransactionsIntoMariaDB(transactions);
                    UpdateProgressBar(100 / totalSteps);

                    accessDb.MarkRecordsAsSynced("OrderTransactions");
                    mariaDb.MarkRecordsAsSyncedInMariaDB("OrderTransactions");
                    UpdateProgressBar(100 / totalSteps);
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

        private void UpdateProgressBar(int step)
        {
            if (progressBarSync.InvokeRequired)
            {
                progressBarSync.Invoke(new Action(() =>
                {
                    progressBarSync.Value += step;
                    progressBarSync.Refresh(); // 🔹 Forzar actualización inmediata
                }));
            }
            else
            {
                progressBarSync.Value += step;
                progressBarSync.Refresh(); // 🔹 Forzar actualización inmediata
            }
        }


        private void progressBarSync_Click(object sender, EventArgs e)
        {

        }
    }

}
