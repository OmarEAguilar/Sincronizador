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

        private void btnSincronizar_Click(object sender, EventArgs e)
        {
            MessageBox.Show(" Sincronización iniciada...", "Sincronización", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Sincronizar Orders
            List<Dictionary<string, object>> orders = accessDb.GetUnsyncedOrders();
            mariaDb.InsertOrdersIntoMariaDB(orders);
            accessDb.MarkOrdersAsSynced();
            mariaDb.MarkOrdersAsSyncedInMariaDB();

            // Sincronizar OrderPayments
            List<Dictionary<string, object>> payments = accessDb.GetUnsyncedOrderPayments();
            mariaDb.InsertOrderPaymentsIntoMariaDB(payments);
            accessDb.MarkRecordsAsSynced("OrderPayments");
            mariaDb.MarkRecordsAsSyncedInMariaDB("OrderPayments");

            // Sincronizar OrderTransactions
            List<Dictionary<string, object>> transactions = accessDb.GetUnsyncedOrderTransactions();
            mariaDb.InsertOrderTransactionsIntoMariaDB(transactions);
            accessDb.MarkRecordsAsSynced("OrderTransactions");
            mariaDb.MarkRecordsAsSyncedInMariaDB("OrderTransactions");

            MessageBox.Show(" Sincronización completada.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
