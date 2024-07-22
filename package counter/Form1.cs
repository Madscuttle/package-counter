using System;
using System.Windows.Forms;
using Npgsql;
using System.Data;
using NpgsqlTypes;
using System.Reflection.Emit;
using System.Drawing;

namespace package_counter
{
    public partial class Form1 : Form
    {
        private NpgsqlConnection npgsqlConnection;
        private NpgsqlDataAdapter dataAdapter;
        private DataSet dataSet;
        private string currentDateTime = DateTime.Now.ToString("yyyy-MM-dd");
        private Timer autoRefreshTimer;
        

        public Form1()
        {
            InitializeComponent();
            this.BackColor = Color.FromArgb(20, 20, 20); // Very dark gray
            this.TransparencyKey = Color.FromArgb(20, 20, 20);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Right - this.Width, 0);

            // Set the initial label color
            label1.ForeColor = Color.White;
            label1.Cursor = Cursors.Hand; // Change cursor to hand to indicate it's clickable

            // Handle MouseEnter and MouseLeave events for label
            label1.MouseEnter += Label1_MouseEnter;
            label1.MouseLeave += Label1_MouseLeave;

            // submenu 
            MenuItem bookmarkSubMenu = new MenuItem("Zmień wielkość licznika");
            bookmarkSubMenu.MenuItems.Add("Małe", SmallFontMenuItem_Click);
            bookmarkSubMenu.MenuItems.Add("Średnie", MediumFontMenuItem_Click);
            bookmarkSubMenu.MenuItems.Add("Duże", LargeFontMenuItem_Click);

            // Add context menu items to the NotifyIcon
            ContextMenu contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add("Pokaż", OpenMenuItem_Click);
            contextMenu.MenuItems.Add("Odśwież", RefreshMenuItem_Click);
            contextMenu.MenuItems.Add(bookmarkSubMenu); // Add the "Bookmark" submenu
            contextMenu.MenuItems.Add("Zamknij", ExitMenuItem_Click);
            notifyIcon1.ContextMenu = contextMenu;
        }

        private void OpenMenuItem_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal; // Restore the form
            this.ShowInTaskbar = true; // Show the form in the taskbar
            notifyIcon1.Visible = false; // Hide the NotifyIcon
        }

        private void SmallFontMenuItem_Click(object sender, EventArgs e)
        {
            label1.Font = new Font(label1.Font.FontFamily, 12); // Set the label font size to Small (12)
        }

        private void MediumFontMenuItem_Click(object sender, EventArgs e)
        {
            label1.Font = new Font(label1.Font.FontFamily, 18); // Set the label font size to Medium (18)
        }

        private void LargeFontMenuItem_Click(object sender, EventArgs e)
        {
            label1.Font = new Font(label1.Font.FontFamily, 24); // Set the label font size to Large (24)
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit(); // Exit the application when "Exit" is clicked
        }

        private void RefreshMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData(); // Call your refresh method when "Refresh" is clicked
        }


        // Handle MouseEnter event to change label color on hover
        private void Label1_MouseEnter(object sender, EventArgs e)
        {
            label1.ForeColor = Color.Gray; // Change the color to blue on hover
        }

        // Handle MouseLeave event to reset label color after hover
        private void Label1_MouseLeave(object sender, EventArgs e)
        {
            label1.ForeColor = Color.White; // Reset the color to white after hover
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Initialize database connection
            string connectionString = "Host=192.168.2.4;Database=test;Username=test;Password=test;SSL Mode=Disable;";
            npgsqlConnection = new NpgsqlConnection(connectionString);

            // Create the dataAdapter and load initial data
            dataAdapter = new NpgsqlDataAdapter($"SELECT SUM(pr.pra_ilosc) FROM tg_praceall AS pr " +
                              $"WHERE ((pr.pra_datastop::timestamp >= '{currentDateTime}' OR (pr.pra_flaga & 3) != 3)) " +
                              $"AND ((pr.pra_datastart::timestamp >= '{currentDateTime}' OR (pr.pra_flaga & 1) != 0)) " +
                              $"AND (pr.p_idpracownika = 131)", npgsqlConnection);
            dataSet = new DataSet();
            dataAdapter.Fill(dataSet);

            // Display the initial sum in the label
            DisplaySum();

            // Start the automatic refresh timer
            autoRefreshTimer = new Timer();
            autoRefreshTimer.Interval = 15 * 60 * 1000; // 15 minutes
            autoRefreshTimer.Tick += Timer1_Tick;
            autoRefreshTimer.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; // Cancel the form closing
                this.WindowState = FormWindowState.Minimized; // Minimize the form
                this.ShowInTaskbar = false; // Hide the form from the taskbar
                notifyIcon1.Visible = true; // Show the NotifyIcon in the system tray
            }
        }


        private void Timer1_Tick(object sender, EventArgs e)
        {
            // Perform automatic refresh
            RefreshData();

            // Log the automatic refresh event to a file
            string logMessage = "Automatic refresh at " + DateTime.Now.ToString("HH:mm:ss");
            System.Diagnostics.Trace.WriteLine(logMessage);
        }

        private void RefreshData()
        {
            // Build the SQL query with the current date and time
            string sqlQuery = $"SELECT SUM(pr.pra_ilosc) FROM tg_praceall AS pr " +
                              $"WHERE ((pr.pra_datastop::timestamp >= '{currentDateTime}' OR (pr.pra_flaga & 3) != 3)) " +
                              $"AND ((pr.pra_datastart::timestamp >= '{currentDateTime}' OR (pr.pra_flaga & 1) != 0)) " +
                              $"AND (pr.p_idpracownika = 131)";

            // Update the SQL query in the dataAdapter
            dataAdapter.SelectCommand.CommandText = sqlQuery;

            // Clear and fill the DataSet with the updated query
            dataSet.Tables[0].Clear();
            dataAdapter.Fill(dataSet);

            // Display the updated sum in the label
            DisplaySum();
        }

        private void DisplaySum()
        {
            if (dataSet.Tables[0].Rows.Count > 0)
            {
                // Get the sum value from the first row of the first column
                int sum = Convert.ToInt32(dataSet.Tables[0].Rows[0][0]);

                // Display the sum in the label
                label1.Text = $"{sum}";
            }
            else
            {
                // Handle the case when no data is available
                label1.Text = "No data available";
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            RefreshData();
        }
    }
}
