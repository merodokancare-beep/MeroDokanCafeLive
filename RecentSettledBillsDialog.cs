using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace MeroDokan
{
    public class RecentSettledBillsDialog : Form
    {
        private Panel headerPanel;
        private Panel filterPanel;
        private Panel actionPanel;
        private DataGridView dgvBills;
        private TextBox txtSearch;
        private ComboBox comboScope;
        private Label lblStats;
        private Button btnReprint;
        private Button btnPreview;
        private Button btnRefresh;
        private Button btnClose;

        public RecentSettledBillsDialog()
        {
            InitializeComponent();
            LoadBills();

            this.Shown += (s, e) => {
                txtSearch?.Focus();
            };
        }

        private void InitializeComponent()
        {
            this.Text = "Recent Settled Bills - Quick Thermal Reprint";
            this.Size = new Size(880, 580);
            this.MinimumSize = new Size(760, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor = Theme.CardBg;
            this.ForeColor = Theme.TextLight;
            this.KeyPreview = true;

            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape)
                {
                    this.Close();
                }
            };

            // ================= 1. HEADER PANEL =================
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.Primary,
                Padding = new Padding(16, 10, 16, 10)
            };
            headerPanel.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.CardBorder, 1))
                    e.Graphics.DrawLine(p, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
            };

            Label lblTitle = new Label
            {
                Text = "📜 Recent Settled Bills",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Theme.TextWhite,
                Location = new Point(16, 10),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblTitle);

            Label lblSubtitle = new Label
            {
                Text = "Instant thermal reprinting • Default: Today's orders only (capped to 100 max for instant speed)",
                Font = Theme.SmallFont,
                ForeColor = Theme.TextMuted,
                Location = new Point(18, 34),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblSubtitle);
            this.Controls.Add(headerPanel);

            // ================= 2. FILTER & SEARCH BAR =================
            filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Theme.Secondary,
                Padding = new Padding(16, 8, 16, 8)
            };
            filterPanel.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.CardBorder, 1))
                    e.Graphics.DrawLine(p, 0, filterPanel.Height - 1, filterPanel.Width, filterPanel.Height - 1);
            };

            Label lblSearchIcon = new Label
            {
                Text = "🔍 Search:",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(16, 16),
                AutoSize = true
            };
            filterPanel.Controls.Add(lblSearchIcon);

            txtSearch = new TextBox
            {
                Location = new Point(90, 12),
                Size = new Size(220, 28),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                BackColor = Theme.InputBg,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.TextChanged += (s, e) => LoadBills();
            filterPanel.Controls.Add(txtSearch);

            Label lblScope = new Label
            {
                Text = "Show:",
                Font = Theme.BoldFont,
                ForeColor = Theme.TextLight,
                Location = new Point(325, 16),
                AutoSize = true
            };
            filterPanel.Controls.Add(lblScope);

            comboScope = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(375, 12),
                Size = new Size(225, 28),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                BackColor = Theme.InputBg,
                ForeColor = Theme.TextLight,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            comboScope.Items.AddRange(new object[] {
                "📅 Today's Bills Only (Safe)",
                "⚡ Last 50 Settled Bills",
                "⚡ Last 100 Settled Bills",
                "📅 Yesterday & Today"
            });
            comboScope.SelectedIndex = 0;
            comboScope.SelectedIndexChanged += (s, e) => LoadBills();
            filterPanel.Controls.Add(comboScope);

            lblStats = new Label
            {
                Text = "Showing 0 bills",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Theme.Accent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleRight,
                Location = new Point(610, 15),
                Size = new Size(245, 22)
            };
            filterPanel.Controls.Add(lblStats);
            this.Controls.Add(filterPanel);

            // ================= 3. BOTTOM ACTION PANEL =================
            actionPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.Primary,
                Padding = new Padding(16, 10, 16, 10)
            };
            actionPanel.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.CardBorder, 1))
                    e.Graphics.DrawLine(p, 0, 0, actionPanel.Width, 0);
            };

            btnReprint = new Button
            {
                Text = "🖨️ Reprint Thermal Receipt",
                Size = new Size(220, 38),
                Location = new Point(16, 11),
                BackColor = Theme.Success,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnReprint.FlatAppearance.BorderSize = 0;
            btnReprint.Click += (s, e) => ExecuteReprint(false);
            actionPanel.Controls.Add(btnReprint);

            btnPreview = new Button
            {
                Text = "👁️ Preview",
                Size = new Size(110, 38),
                Location = new Point(244, 11),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Theme.TextLight,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnPreview.FlatAppearance.BorderSize = 1;
            btnPreview.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btnPreview.Click += (s, e) => ExecuteReprint(true);
            actionPanel.Controls.Add(btnPreview);

            btnRefresh = new Button
            {
                Text = "🔄 Refresh",
                Size = new Size(100, 38),
                Location = new Point(362, 11),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Theme.TextLight,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 1;
            btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btnRefresh.Click += (s, e) => LoadBills();
            actionPanel.Controls.Add(btnRefresh);

            btnClose = new Button
            {
                Text = "✖ Close",
                Size = new Size(95, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(760, 11),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Theme.TextLight,
                Font = Theme.BoldFont,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 1;
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btnClose.Click += (s, e) => this.Close();
            actionPanel.Controls.Add(btnClose);

            this.Controls.Add(actionPanel);

            // ================= 4. DATA GRID VIEW =================
            dgvBills = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.Secondary,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(30, 41, 59),
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                RowTemplate = { Height = 34 }
            };

            Theme.SetDoubleBuffered(dgvBills);

            // Header Style
            dgvBills.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dgvBills.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextWhite;
            dgvBills.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvBills.ColumnHeadersHeight = 36;
            dgvBills.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // Row Style
            dgvBills.DefaultCellStyle.BackColor = Theme.CardBg;
            dgvBills.DefaultCellStyle.ForeColor = Theme.TextLight;
            dgvBills.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            dgvBills.DefaultCellStyle.SelectionBackColor = Theme.Accent;
            dgvBills.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvBills.AlternatingRowsDefaultCellStyle.BackColor = Theme.AlternateRow;
            dgvBills.AlternatingRowsDefaultCellStyle.ForeColor = Theme.TextLight;

            // Define Columns
            dgvBills.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", Visible = false });

            dgvBills.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "InvoiceNumber",
                HeaderText = "Invoice #",
                DataPropertyName = "InvoiceNumber",
                Width = 145
            });

            dgvBills.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SaleDate",
                HeaderText = "Date & Time",
                DataPropertyName = "FormattedDate",
                Width = 135
            });

            dgvBills.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TableNumber",
                HeaderText = "Table / Token",
                DataPropertyName = "TableDisplay",
                Width = 115
            });

            dgvBills.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "OrderType",
                HeaderText = "Type",
                DataPropertyName = "OrderType",
                Width = 90
            });

            dgvBills.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "GrandTotal",
                HeaderText = "Total (₹)",
                DataPropertyName = "FormattedTotal",
                Width = 105,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
                }
            });

            dgvBills.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PaymentMethod",
                HeaderText = "Payment",
                DataPropertyName = "PaymentMethod",
                Width = 115
            });

            dgvBills.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "StewardName",
                HeaderText = "Steward",
                DataPropertyName = "StewardName",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dgvBills.DoubleClick += (s, e) => ExecuteReprint(false);

            this.Controls.Add(dgvBills);

            // Precise Z-Order: header at top, filter directly below it
            filterPanel.SendToBack();
            headerPanel.SendToBack();
            actionPanel.SendToBack();
            dgvBills.BringToFront();
        }

        private void LoadBills()
        {
            try
            {
                string search = (txtSearch?.Text ?? "").Trim();
                int scopeIndex = comboScope?.SelectedIndex ?? 0;
                if (scopeIndex < 0) scopeIndex = 0;

                DataTable dt = new DataTable();
                dt.Columns.Add("Id", typeof(int));
                dt.Columns.Add("InvoiceNumber", typeof(string));
                dt.Columns.Add("FormattedDate", typeof(string));
                dt.Columns.Add("TableDisplay", typeof(string));
                dt.Columns.Add("OrderType", typeof(string));
                dt.Columns.Add("FormattedTotal", typeof(string));
                dt.Columns.Add("PaymentMethod", typeof(string));
                dt.Columns.Add("StewardName", typeof(string));
                dt.Columns.Add("RawTotal", typeof(decimal));

                decimal totalSum = 0;
                int count = 0;

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    int topLimit = 100;
                    string whereClause = "WHERE 1=1 ";
                    DateTime todayStart = DateTime.Today;
                    DateTime tomorrowStart = todayStart.AddDays(1);
                    DateTime yesterdayStart = todayStart.AddDays(-1);

                    if (scopeIndex == 0) // Today's Bills Only (Default)
                    {
                        topLimit = 100;
                        whereClause += "AND SaleDate >= @startDate AND SaleDate < @endDate ";
                    }
                    else if (scopeIndex == 1) // Last 50 Bills
                    {
                        topLimit = 50;
                    }
                    else if (scopeIndex == 2) // Last 100 Bills
                    {
                        topLimit = 100;
                    }
                    else if (scopeIndex == 3) // Yesterday & Today
                    {
                        topLimit = 100;
                        whereClause += "AND SaleDate >= @startDate AND SaleDate < @endDate ";
                    }

                    if (!string.IsNullOrEmpty(search))
                    {
                        whereClause += "AND (InvoiceNumber LIKE @search OR TableNumber LIKE @search OR PaymentMethod LIKE @search OR StewardName LIKE @search) ";
                    }

                    string query = $@"
                        SELECT TOP ({topLimit}) Id, InvoiceNumber, SaleDate, TableNumber, OrderType, GrandTotal, PaymentMethod, StewardName
                        FROM Sales
                        {whereClause}
                        ORDER BY Id DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        if (scopeIndex == 0)
                        {
                            cmd.Parameters.AddWithValue("@startDate", todayStart);
                            cmd.Parameters.AddWithValue("@endDate", tomorrowStart);
                        }
                        else if (scopeIndex == 3)
                        {
                            cmd.Parameters.AddWithValue("@startDate", yesterdayStart);
                            cmd.Parameters.AddWithValue("@endDate", tomorrowStart);
                        }

                        if (!string.IsNullOrEmpty(search))
                        {
                            cmd.Parameters.AddWithValue("@search", "%" + search + "%");
                        }

                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                int id = Convert.ToInt32(r["Id"]);
                                string inv = r["InvoiceNumber"]?.ToString() ?? "";
                                DateTime dtVal = r["SaleDate"] != DBNull.Value ? Convert.ToDateTime(r["SaleDate"]) : DateTime.Now;
                                string tbl = r["TableNumber"]?.ToString() ?? "-";
                                string type = r["OrderType"]?.ToString() ?? "DINING";
                                decimal grand = Convert.ToDecimal(r["GrandTotal"]);
                                string pay = r["PaymentMethod"]?.ToString() ?? "Cash";
                                string stwd = r["StewardName"]?.ToString() ?? "";

                                string tableDisp = tbl;
                                if (type == "TAKEAWAY" && !tableDisp.StartsWith("Token", StringComparison.OrdinalIgnoreCase))
                                    tableDisp = "Token " + tbl;
                                else if (type == "DELIVERY" && !tableDisp.StartsWith("Delivery", StringComparison.OrdinalIgnoreCase))
                                    tableDisp = "Deliv " + tbl;
                                else if (type == "DINING" && !tableDisp.StartsWith("Table", StringComparison.OrdinalIgnoreCase) && !tableDisp.StartsWith("Waiting", StringComparison.OrdinalIgnoreCase))
                                    tableDisp = "Table " + tbl;

                                dt.Rows.Add(
                                    id,
                                    inv,
                                    dtVal.ToString("dd MMM, hh:mm tt"),
                                    tableDisp,
                                    type,
                                    $"₹ {grand:N0}",
                                    pay,
                                    string.IsNullOrEmpty(stwd) ? "Direct Counter" : stwd,
                                    grand
                                );

                                totalSum += grand;
                                count++;
                            }
                        }
                    }
                }

                dgvBills.DataSource = dt;

                if (lblStats != null)
                {
                    string scopeDesc = (scopeIndex == 0) ? "Today" :
                                       (scopeIndex == 1) ? "Last 50" :
                                       (scopeIndex == 2) ? "Last 100" : "Yesterday & Today";
                    lblStats.Text = $"Showing {count} bill{(count == 1 ? "" : "s")} ({scopeDesc}) • Total: ₹{totalSum:N0}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settled bills: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExecuteReprint(bool previewOnly)
        {
            if (dgvBills.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a settled bill from the list to reprint.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var row = dgvBills.SelectedRows[0];
                int saleId = Convert.ToInt32(row.Cells["Id"].Value);
                string invNum = row.Cells["InvoiceNumber"].Value?.ToString() ?? "";
                string tbl = row.Cells["TableNumber"].Value?.ToString() ?? "";
                string total = row.Cells["GrandTotal"].Value?.ToString() ?? "";

                if (previewOnly)
                {
                    ThermalReceiptPrinter.ShowPreview(saleId);
                }
                else
                {
                    DialogResult res = MessageBox.Show(
                        $"Reprint thermal receipt for {invNum}?\n\n• Table / Order: {tbl}\n• Total Amount: {total}",
                        "Confirm Thermal Reprint",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (res == DialogResult.Yes)
                    {
                        ThermalReceiptPrinter.Print(saleId);
                        MessageBox.Show($"Receipt for {invNum} sent to thermal printer.", "Reprint Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reprinting receipt: {ex.Message}", "Reprint Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
