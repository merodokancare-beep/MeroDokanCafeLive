using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;

namespace MeroDokan
{
    /* =========================================================================
       TVS RP 3220 / POS-80 / 80MM THERMAL RECEIPT & KOT PRINTING ENGINE
       Exact replica of "The Local Cafe" Dine-In, Takeaway, KOT & Void Tickets
       ========================================================================= */
    internal static class ThermalReceiptPrinter
    {
        private const int PaperWidth = 284; // 80mm printable width = 72mm = 284 GDI units
        private const int MarginLeft = 6;
        private const int MarginRight = 6;
        private const int UsableWidth = PaperWidth - MarginLeft - MarginRight; // 272 units
        private const string PaperName = "Thermal80mm";

        #region Public API
        public static void ShowPreview(int saleId)
        {
            try
            {
                PrintDocument doc = BuildCustomerBillDocument(saleId);
                PrintPreviewDialog dlg = new PrintPreviewDialog();
                dlg.Document = doc;
                dlg.Size = new Size(360, 720);
                try { ((Form)dlg).Text = "Receipt Preview - 80mm Thermal"; } catch { }
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating preview: {ex.Message}", "Print Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static bool IsVirtualOrPdfPrinter(string printerName)
        {
            if (string.IsNullOrEmpty(printerName)) return true;
            string lower = printerName.ToLowerInvariant();
            return lower.Contains("pdf") || lower.Contains("xps") || lower.Contains("onenote") || 
                   lower.Contains("fax") || lower.Contains("document writer") || lower.Contains("virtual");
        }

        public static void Print(int saleId)
        {
            try
            {
                CafeBillData d = LoadCustomerBillData(saleId);
                string printer = FindThermalPrinter(d.BillingPrinter);
                if (string.IsNullOrEmpty(printer) || IsVirtualOrPdfPrinter(printer))
                {
                    // Open clean 80mm preview instead of popup asking to save PDF file to system drive or failing on office printer
                    ShowPreview(saleId);
                    return;
                }
                PrintDocument doc = BuildCustomerBillDocument(saleId);
                doc.PrinterSettings.PrinterName = printer;
                doc.PrintController = new StandardPrintController(); // Silent printing without "Printing Page 1..." popup
                doc.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing receipt: {ex.Message}\nPlease check your printer connection.", "Printer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static bool ReprintLastSettledBill(IWin32Window parent = null)
        {
            try
            {
                int saleId = 0;
                string invNum = "";
                decimal total = 0;
                string tbl = "";
                string payMethod = "";
                DateTime saleDate = DateTime.Now;

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string sql = "SELECT TOP 1 Id, InvoiceNumber, GrandTotal, TableNumber, PaymentMethod, SaleDate FROM Sales ORDER BY Id DESC";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            saleId = Convert.ToInt32(r["Id"]);
                            invNum = r["InvoiceNumber"]?.ToString() ?? "";
                            total = Convert.ToDecimal(r["GrandTotal"]);
                            tbl = r["TableNumber"]?.ToString() ?? "Counter";
                            payMethod = r["PaymentMethod"]?.ToString() ?? "Cash";
                            if (r["SaleDate"] != DBNull.Value) saleDate = Convert.ToDateTime(r["SaleDate"]);
                        }
                    }
                }

                if (saleId <= 0)
                {
                    MessageBox.Show(parent, "No settled bills found to reprint.", "Reprint Bill", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }

                string msg = $"Reprint last settled bill?\n\n• Invoice: {invNum}\n• Table / Order: {tbl}\n• Amount: ₹{total:N0} ({payMethod})\n• Time: {saleDate:hh:mm tt}";
                DialogResult dr = MessageBox.Show(parent, msg, "Confirm Last Bill Reprint", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr == DialogResult.Yes)
                {
                    Print(saleId);
                    MessageBox.Show(parent, $"Receipt for {invNum} sent to thermal printer.", "Reprint Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(parent, $"Error fetching last bill: {ex.Message}", "Reprint Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static bool ReprintLastSettledBillForTable(string tableNum, IWin32Window parent = null)
        {
            try
            {
                if (string.IsNullOrEmpty(tableNum)) return false;

                int saleId = 0;
                string invNum = "";
                decimal total = 0;
                string payMethod = "";
                DateTime saleDate = DateTime.Now;

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string sql = "SELECT TOP 1 Id, InvoiceNumber, GrandTotal, PaymentMethod, SaleDate FROM Sales WHERE TableNumber = @tNum ORDER BY Id DESC";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@tNum", tableNum);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                saleId = Convert.ToInt32(r["Id"]);
                                invNum = r["InvoiceNumber"]?.ToString() ?? "";
                                total = Convert.ToDecimal(r["GrandTotal"]);
                                payMethod = r["PaymentMethod"]?.ToString() ?? "Cash";
                                if (r["SaleDate"] != DBNull.Value) saleDate = Convert.ToDateTime(r["SaleDate"]);
                            }
                        }
                    }
                }

                if (saleId <= 0)
                {
                    MessageBox.Show(parent, $"No past settled bills found for Table '{tableNum}'.", "Reprint Bill", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }

                string msg = $"Reprint last settled bill for Table {tableNum}?\n\n• Invoice: {invNum}\n• Amount: ₹{total:N0} ({payMethod})\n• Time: {saleDate:hh:mm tt}";
                DialogResult dr = MessageBox.Show(parent, msg, "Confirm Table Bill Reprint", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr == DialogResult.Yes)
                {
                    Print(saleId);
                    MessageBox.Show(parent, $"Receipt for {invNum} sent to thermal printer.", "Reprint Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(parent, $"Error fetching table bill: {ex.Message}", "Reprint Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static bool PrintKOT(int kotId, out string printerUsed, out string errorMessage)
        {
            printerUsed = "";
            errorMessage = "";
            try
            {
                PrintDocument doc = BuildKotDocument(kotId);
                printerUsed = doc.PrinterSettings.PrinterName;
                // If no physical thermal printer is configured or default is PDF,
                // do NOT prompt to save PDF in system drive; show clean preview instead.
                if (IsVirtualOrPdfPrinter(printerUsed))
                {
                    ShowKOTPreview(kotId);
                    return true;
                }
                doc.PrintController = new StandardPrintController(); // Silent printing without "Printing Page 1..." popup
                doc.Print();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                try { ShowKOTPreview(kotId); } catch { }
                return false;
            }
        }

        public static void PrintKOT(int kotId)
        {
            PrintKOT(kotId, out _, out _);
        }

        public static void ShowKOTPreview(int kotId)
        {
            try
            {
                PrintDocument doc = BuildKotDocument(kotId);
                PrintPreviewDialog dlg = new PrintPreviewDialog();
                dlg.Document = doc;
                dlg.Size = new Size(360, 600);
                try { ((Form)dlg).Text = "KOT Kitchen Ticket Preview (80mm)"; } catch { }
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating KOT preview: {ex.Message}", "KOT Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static bool PrintVoidKOT(int kotId, string reason, out string printerUsed, out string errorMessage)
        {
            printerUsed = "";
            errorMessage = "";
            try
            {
                PrintDocument doc = BuildVoidKotDocument(kotId, reason);
                printerUsed = doc.PrinterSettings.PrinterName;
                if (IsVirtualOrPdfPrinter(printerUsed))
                {
                    return true;
                }
                doc.PrintController = new StandardPrintController();
                doc.Print();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public static void PrintVoidKOT(int kotId, string reason)
        {
            PrintVoidKOT(kotId, reason, out _, out _);
        }

        public static void ShowSettlementPreview(SettlementPrintData d)
        {
            try
            {
                PrintDocument doc = BuildSettlementDocument(d);
                PrintPreviewDialog dlg = new PrintPreviewDialog();
                dlg.Document = doc;
                dlg.WindowState = FormWindowState.Maximized;
                dlg.StartPosition = FormStartPosition.CenterScreen;
                if (dlg.PrintPreviewControl != null)
                {
                    dlg.PrintPreviewControl.Zoom = 1.0;
                    dlg.PrintPreviewControl.AutoZoom = false;
                }
                try { ((Form)dlg).Text = $"Daily Settlement Slip (80mm) - {d.SettlementDate:yyyy-MM-dd}"; } catch { }
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating settlement preview: {ex.Message}", "Print Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void PrintSettlement(SettlementPrintData d)
        {
            try
            {
                string printer = FindThermalPrinter(d.BillingPrinter);
                if (string.IsNullOrEmpty(printer) || IsVirtualOrPdfPrinter(printer))
                {
                    ShowSettlementPreview(d);
                    return;
                }
                PrintDocument doc = BuildSettlementDocument(d);
                doc.PrinterSettings.PrinterName = printer;
                doc.PrintController = new StandardPrintController();
                doc.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing settlement slip: {ex.Message}\nPlease check your printer connection.", "Printer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void ShowSettlementPreview(DateTime date, decimal? actualCashOverride = null, string remarksOverride = null)
        {
            var data = LoadSettlementData(date, actualCashOverride, remarksOverride);
            ShowSettlementPreview(data);
        }

        public static void PrintSettlement(DateTime date, decimal? actualCashOverride = null, string remarksOverride = null)
        {
            var data = LoadSettlementData(date, actualCashOverride, remarksOverride);
            PrintSettlement(data);
        }

        public static void ShowA4SettlementPreview(SettlementPrintData d)
        {
            try
            {
                PrintDocument doc = BuildA4SettlementDocument(d);
                PrintPreviewDialog dlg = new PrintPreviewDialog();
                dlg.Document = doc;
                dlg.WindowState = FormWindowState.Maximized;
                dlg.StartPosition = FormStartPosition.CenterScreen;
                if (dlg.PrintPreviewControl != null)
                {
                    dlg.PrintPreviewControl.Zoom = 1.0;
                    dlg.PrintPreviewControl.AutoZoom = false;
                }
                try { ((Form)dlg).Text = $"Daily Settlement & Item Sales Report (A4) - {d.SettlementDate:yyyy-MM-dd}"; } catch { }
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating A4 settlement preview: {ex.Message}", "Print Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void PrintA4Settlement(SettlementPrintData d)
        {
            try
            {
                PrintDocument doc = BuildA4SettlementDocument(d);
                string printer = doc.PrinterSettings.PrinterName;
                if (IsVirtualOrPdfPrinter(printer))
                {
                    ShowA4SettlementPreview(d);
                    return;
                }
                doc.PrintController = new StandardPrintController();
                doc.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing A4 settlement report: {ex.Message}\nPlease check your printer connection.", "Printer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void ShowA4SettlementPreview(DateTime date, decimal? actualCashOverride = null, string remarksOverride = null)
        {
            var data = LoadSettlementData(date, actualCashOverride, remarksOverride);
            ShowA4SettlementPreview(data);
        }

        public static void PrintA4Settlement(DateTime date, decimal? actualCashOverride = null, string remarksOverride = null)
        {
            var data = LoadSettlementData(date, actualCashOverride, remarksOverride);
            PrintA4Settlement(data);
        }
        #endregion

        #region Printer Setup & Discovery
        public static string FindThermalPrinter(string configuredName = null)
        {
            try
            {
                // 1. Explicitly configured printer (if not placeholder/auto-detect)
                if (!string.IsNullOrEmpty(configuredName) && !configuredName.StartsWith("(", StringComparison.Ordinal))
                {
                    foreach (string p in PrinterSettings.InstalledPrinters)
                    {
                        if (p.Equals(configuredName, StringComparison.OrdinalIgnoreCase))
                            return p;
                    }
                }

                // Exclude known non-thermal office inkjet / laser printers (e.g. Epson EcoTank/L-series, HP DeskJet/LaserJet)
                bool IsNonThermalOfficePrinter(string name)
                {
                    string l = name.ToLowerInvariant();
                    return l.Contains("ecotank") || l.Contains("inkjet") || l.Contains("deskjet") || 
                           l.Contains("laserjet") || l.Contains("l3250") || l.Contains("l3150") || 
                           l.Contains("l3110") || l.Contains("l3210") || l.Contains("l805") ||
                           l.Contains("l-series") || l.Contains("ink tank") || l.Contains("smart tank");
                }

                // 2. Comprehensive auto-detection for thermal / POS / receipt / kitchen printers
                string[] thermalKeywords = new string[]
                {
                    "posiflex", "pp8800", "pp8000", "pp-8", "pp7", "pp9", "pp6", "aura",
                    "thermal", "pos-", "pos ", "receipt", "kot", "kitchen", "80mm", "58mm",
                    "tvs", "rp 3220", "rp3220", "rp-3220", "rp3200", "rp",
                    "tm-t", "tm-m", "tm-u", "tm-p", "tm-l", "tm-ba", "epson tm",
                    "xprinter", "xp-", "rongta", "everycom", "retsol",
                    "hoin", "bixolon", "citizen", "star", "snbc",
                    "gprinter", "black copper", "dokan", "esc/pos", "esc-pos"
                };

                foreach (string p in PrinterSettings.InstalledPrinters)
                {
                    if (IsVirtualOrPdfPrinter(p)) continue;
                    if (IsNonThermalOfficePrinter(p)) continue;
                    string lower = p.ToLowerInvariant();
                    foreach (var kw in thermalKeywords)
                    {
                        if (lower.Contains(kw))
                            return p;
                    }
                }

                // 3. Check Windows Default Printer if it is a physical printer (and not an office inkjet)
                try
                {
                    PrinterSettings defaultSettings = new PrinterSettings();
                    if (!string.IsNullOrEmpty(defaultSettings.PrinterName) && 
                        !IsVirtualOrPdfPrinter(defaultSettings.PrinterName) && 
                        !IsNonThermalOfficePrinter(defaultSettings.PrinterName))
                    {
                        return defaultSettings.PrinterName;
                    }
                }
                catch { }

                // 4. Fallback: Any installed non-virtual physical printer (excluding office inkjets)
                foreach (string p in PrinterSettings.InstalledPrinters)
                {
                    if (!IsVirtualOrPdfPrinter(p) && !IsNonThermalOfficePrinter(p))
                    {
                        return p;
                    }
                }
            }
            catch { }
            return null; // Fallback to Windows default printer
        }

        public static string GetConnectedPrinterDisplayName(string configuredName = null)
        {
            string p = FindThermalPrinter(configuredName);
            if (!string.IsNullOrEmpty(p) && !IsVirtualOrPdfPrinter(p))
            {
                return p;
            }
            return "Virtual / PDF (Preview)";
        }

        public static List<string> GetInstalledPrinterNames()
        {
            var list = new List<string>();
            try
            {
                foreach (string p in PrinterSettings.InstalledPrinters)
                {
                    list.Add(p);
                }
            }
            catch { }
            return list;
        }

        public static bool PrintTestKOT(string targetPrinter, out string message)
        {
            try
            {
                KotData d = new KotData
                {
                    KotNumber = 999,
                    TableNumber = "TEST-1",
                    OrderType = "DINING",
                    BillNumber = "TEST-BILL",
                    Steward = "System Admin",
                    DateStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    KotComment = "Hardware Thermal Test Print - MeroDokan Cafe POS",
                    Items = new List<CafeBillItem>
                    {
                        new CafeBillItem { Name = "Test Coffee (Hot)", Qty = 1, Rate = 120, Amt = 120 },
                        new CafeBillItem { Name = "Test Sandwich [Extra Cheese]", Qty = 2, Rate = 150, Amt = 300 }
                    }
                };

                PrintDocument doc = new PrintDocument();
                doc.DocumentName = "TestKOT_999";
                string p = FindThermalPrinter(targetPrinter);
                if (!string.IsNullOrEmpty(p))
                {
                    doc.PrinterSettings.PrinterName = p;
                }

                int pageHeight = EstimateKotHeight(d);
                PaperSize paperSize = new PaperSize(PaperName, PaperWidth, pageHeight);
                paperSize.RawKind = (int)PaperKind.Custom;
                doc.DefaultPageSettings.PaperSize = paperSize;
                doc.DefaultPageSettings.Margins = new Margins(MarginLeft, MarginRight, 6, 6);
                doc.PrinterSettings.DefaultPageSettings.PaperSize = paperSize;
                doc.PrinterSettings.DefaultPageSettings.Margins = new Margins(MarginLeft, MarginRight, 6, 6);

                doc.PrintPage += delegate(object s, PrintPageEventArgs e)
                {
                    DrawKotSlip(e.Graphics, d);
                    e.HasMorePages = false;
                };

                if (IsVirtualOrPdfPrinter(doc.PrinterSettings.PrinterName))
                {
                    PrintPreviewDialog dlg = new PrintPreviewDialog();
                    dlg.Document = doc;
                    dlg.Size = new Size(360, 600);
                    try { ((Form)dlg).Text = "Test KOT Preview (Virtual Printer)"; } catch { }
                    dlg.ShowDialog();
                    message = "Preview displayed (virtual / PDF printer selected).";
                    return true;
                }

                doc.PrintController = new StandardPrintController();
                doc.Print();
                message = $"Test KOT successfully sent to printer: {doc.PrinterSettings.PrinterName}";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Test print failed: {ex.Message}";
                return false;
            }
        }
        #endregion

        #region Data Models
        private class CafeBillItem
        {
            public string Name;
            public int Qty;
            public decimal Rate;
            public decimal Amt;
        }

        private class CafeBillData
        {
            public string ShopName = "The Local Cafe";
            public string LogoPath = "";
            public string GSTIN = "11BIDPB3498K1ZD";
            public string Address = "vajra world Mall Balwa khani,\nGangtok Sikkim 737101";
            public string ContactNo = "9971592652";
            public string OrderType = "DINING";
            public string TableNumber = "5";
            public string BillNo = "6";
            public string DateStr = "";
            public string Kots = "";
            public List<CafeBillItem> Items = new List<CafeBillItem>();
            public int TotalQty = 0;
            public decimal SubTotal = 0;
            public decimal Discount = 0;
            public decimal GstPercent = 5.0m;
            public decimal GstAmount = 0;
            public decimal CgstAmount = 0;
            public decimal SgstAmount = 0;
            public decimal RoundOff = 0;
            public decimal TotalInvoiceValue = 0;
            public string PaymentMethod = "Cash";
            public decimal CashAmount = 0;
            public decimal OnlineAmount = 0;
            public string FooterGreeting = "Tashi Delek! Thukje Che!";
            public string Branding = "Powered by - MeroDokan";
            public string BillingPrinter = null;
        }

        private class KotData
        {
            public int KotNumber = 0;
            public string TableNumber = "";
            public string OrderType = "DINING";
            public string BillNumber = "";
            public string Steward = "";
            public string DateStr = "";
            public string KotComment = "";
            public List<CafeBillItem> Items = new List<CafeBillItem>();
            public bool IsVoid = false;
            public string VoidReason = "";
            public string KitchenPrinter = null;
        }

        public class SettlementPrintData
        {
            public string ShopName = "The Local Cafe";
            public string LogoPath = "";
            public string GSTIN = "";
            public string Address = "";
            public string ContactNo = "";
            public string BillingPrinter = null;

            public DateTime SettlementDate = DateTime.Today;
            public string SettledBy = "System Administrator";
            public DateTime PrintTime = DateTime.Now;

            // Sales overview
            public int TotalBillsCount = 0;
            public decimal GrossSales = 0;
            public decimal TotalDiscounts = 0;
            public decimal TotalTax = 0;
            public decimal ReturnsAndRefunds = 0;
            public decimal NetSales = 0;

            // Voids
            public int VoidCount = 0;
            public decimal VoidAmount = 0;

            // Tender breakdown
            public decimal CashSales = 0;
            public decimal CardSales = 0;
            public decimal QRSales = 0;
            public decimal DuesCreated = 0;
            public decimal DueCollectionsPrev = 0;
            public decimal DueCollectionsToday = 0;
            public decimal DueCollectionsTotal = 0;
            public decimal TotalCollections = 0;

            // Cash Drawer Audit
            public decimal OpeningCash = 0;
            public decimal CashRefunds = 0;
            public decimal ExpectedCash = 0;
            public decimal ActualCash = 0;
            public decimal Variance = 0;
            public string Remarks = "";

            // Item-wise sold breakdown
            public List<SettlementSoldItem> SoldItems = new List<SettlementSoldItem>();
        }

        public class SettlementSoldItem
        {
            public int SerialNo;
            public string ItemName;
            public string Category;
            public decimal Rate;
            public int Quantity;
            public decimal TotalAmount;
        }
        #endregion

        #region Document Builders
        private static PrintDocument BuildCustomerBillDocument(int saleId)
        {
            CafeBillData d = LoadCustomerBillData(saleId);
            int pageHeight = EstimateCustomerBillHeight(d);

            PrintDocument doc = new PrintDocument();
            doc.DocumentName = "CafeBill_" + d.BillNo;

            string printer = FindThermalPrinter(d.BillingPrinter);
            if (!string.IsNullOrEmpty(printer))
            {
                doc.PrinterSettings.PrinterName = printer;
            }

            PaperSize ps = new PaperSize(PaperName, PaperWidth, pageHeight);
            ps.RawKind = (int)PaperKind.Custom;

            doc.DefaultPageSettings.PaperSize = ps;
            doc.DefaultPageSettings.Margins = new Margins(MarginLeft, MarginRight, 6, 6);
            doc.PrinterSettings.DefaultPageSettings.PaperSize = ps;
            doc.PrinterSettings.DefaultPageSettings.Margins = new Margins(MarginLeft, MarginRight, 6, 6);

            doc.PrintPage += delegate(object s, PrintPageEventArgs e)
            {
                DrawCustomerBill(e.Graphics, d);
                e.HasMorePages = false;
            };

            return doc;
        }

        private static PrintDocument BuildKotDocument(int kotId)
        {
            KotData d = LoadKotData(kotId, false, null);
            int pageHeight = EstimateKotHeight(d);

            PrintDocument doc = new PrintDocument();
            doc.DocumentName = "KOT_" + d.KotNumber;

            string printer = FindThermalPrinter(d.KitchenPrinter);
            if (!string.IsNullOrEmpty(printer))
            {
                doc.PrinterSettings.PrinterName = printer;
            }

            PaperSize ps = new PaperSize(PaperName, PaperWidth, pageHeight);
            ps.RawKind = (int)PaperKind.Custom;

            doc.DefaultPageSettings.PaperSize = ps;
            doc.DefaultPageSettings.Margins = new Margins(MarginLeft, MarginRight, 6, 6);
            doc.PrinterSettings.DefaultPageSettings.PaperSize = ps;
            doc.PrinterSettings.DefaultPageSettings.Margins = new Margins(MarginLeft, MarginRight, 6, 6);

            doc.PrintPage += delegate(object s, PrintPageEventArgs e)
            {
                DrawKotSlip(e.Graphics, d);
                e.HasMorePages = false;
            };

            return doc;
        }

        private static PrintDocument BuildVoidKotDocument(int kotId, string reason)
        {
            KotData d = LoadKotData(kotId, true, reason);
            int pageHeight = EstimateKotHeight(d);

            PrintDocument doc = new PrintDocument();
            doc.DocumentName = "VoidKOT_" + d.KotNumber;

            string printer = FindThermalPrinter(d.KitchenPrinter);
            if (!string.IsNullOrEmpty(printer))
            {
                doc.PrinterSettings.PrinterName = printer;
            }

            PaperSize ps = new PaperSize(PaperName, PaperWidth, pageHeight);
            ps.RawKind = (int)PaperKind.Custom;

            doc.DefaultPageSettings.PaperSize = ps;
            doc.DefaultPageSettings.Margins = new Margins(MarginLeft, MarginRight, 6, 6);
            doc.PrinterSettings.DefaultPageSettings.PaperSize = ps;
            doc.PrinterSettings.DefaultPageSettings.Margins = new Margins(MarginLeft, MarginRight, 6, 6);

            doc.PrintPage += delegate(object s, PrintPageEventArgs e)
            {
                DrawKotSlip(e.Graphics, d);
                e.HasMorePages = false;
            };

            return doc;
        }

        public static PrintDocument BuildSettlementDocument(SettlementPrintData d)
        {
            int pageHeight = EstimateSettlementHeight(d);

            PrintDocument doc = new PrintDocument();
            doc.DocumentName = "DaySettlement_" + d.SettlementDate.ToString("yyyyMMdd");

            string printer = FindThermalPrinter(d.BillingPrinter);
            if (!string.IsNullOrEmpty(printer))
            {
                doc.PrinterSettings.PrinterName = printer;
            }

            PaperSize ps = new PaperSize(PaperName, PaperWidth, pageHeight);
            ps.RawKind = (int)PaperKind.Custom;

            doc.DefaultPageSettings.PaperSize = ps;
            doc.DefaultPageSettings.Margins = new Margins(MarginLeft, MarginRight, 6, 6);
            doc.PrinterSettings.DefaultPageSettings.PaperSize = ps;
            doc.PrinterSettings.DefaultPageSettings.Margins = new Margins(MarginLeft, MarginRight, 6, 6);

            doc.PrintPage += delegate(object s, PrintPageEventArgs e)
            {
                DrawSettlementSlip(e.Graphics, d);
                e.HasMorePages = false;
            };

            return doc;
        }

        public static PrintDocument BuildA4SettlementDocument(SettlementPrintData d)
        {
            PrintDocument doc = new PrintDocument();
            doc.DocumentName = "DailySettlementReport_" + d.SettlementDate.ToString("yyyyMMdd");

            PaperSize a4Size = null;
            foreach (PaperSize ps in doc.PrinterSettings.PaperSizes)
            {
                if (ps.Kind == PaperKind.A4)
                {
                    a4Size = ps;
                    break;
                }
            }
            if (a4Size == null)
            {
                a4Size = new PaperSize("A4", 827, 1169);
            }
            doc.DefaultPageSettings.PaperSize = a4Size;
            doc.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);
            doc.DefaultPageSettings.Landscape = false;

            int currentItemIndex = 0;
            int pageNum = 1;

            doc.BeginPrint += (s, e) =>
            {
                currentItemIndex = 0;
                pageNum = 1;
            };

            doc.PrintPage += (s, e) =>
            {
                DrawA4SettlementPage(e.Graphics, d, e.MarginBounds, ref currentItemIndex, ref pageNum, out bool hasMore);
                e.HasMorePages = hasMore;
            };

            return doc;
        }
        #endregion

        #region Data Loaders
        private static CafeBillData LoadCustomerBillData(int saleId)
        {
            CafeBillData d = new CafeBillData();

            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();

                // 1. App Profile
                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 ShopName, GSTIN, Address, Phone, ISNULL(ReceiptFooterText, 'Tashi Delek! Thukje Che!') AS ReceiptFooterText, BillingPrinterName, LogoPath FROM AppProfile", conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        d.ShopName = r["ShopName"]?.ToString() ?? "The Local Cafe";
                        d.GSTIN = r["GSTIN"]?.ToString() ?? "";
                        d.Address = r["Address"]?.ToString() ?? "";
                        d.ContactNo = r["Phone"]?.ToString() ?? "";
                        d.FooterGreeting = r["ReceiptFooterText"]?.ToString() ?? "Tashi Delek! Thukje Che!";
                        d.BillingPrinter = r["BillingPrinterName"]?.ToString();
                        d.LogoPath = r["LogoPath"]?.ToString();
                    }
                }

                // Fallback resolution for logo
                if (!string.IsNullOrEmpty(d.LogoPath) && !File.Exists(d.LogoPath))
                {
                    string candidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, d.LogoPath);
                    if (File.Exists(candidate)) d.LogoPath = candidate;
                }
                if (string.IsNullOrEmpty(d.LogoPath) || !File.Exists(d.LogoPath))
                {
                    string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_transparent.png");
                    string p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.jpg");
                    string p3 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logo.jpg");
                    if (File.Exists(p1)) d.LogoPath = p1;
                    else if (File.Exists(p2)) d.LogoPath = p2;
                    else if (File.Exists(p3)) d.LogoPath = p3;
                }

                // 2. Sales Record
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT s.InvoiceNumber, s.SaleDate, s.SubTotal, s.Discount, s.Tax, s.GrandTotal, 
                           ISNULL(s.OrderType, 'DINING') AS OrderType, ISNULL(s.TableNumber, '') AS TableNumber, 
                           ISNULL(s.KotNumbers, '') AS KotNumbers, ISNULL(s.PackingCharges, 0) AS PackingCharges,
                           ISNULL(s.CGSTAmount, 0) AS CGSTAmount, ISNULL(s.SGSTAmount, 0) AS SGSTAmount,
                           ISNULL(s.RoundOff, 0) AS RoundOff, ISNULL(s.TaxableAmount, s.SubTotal) AS TaxableAmount,
                           ISNULL(s.PaymentMethod, 'Cash') AS PaymentMethod,
                           ISNULL(s.CashAmount, 0) AS CashAmount,
                           ISNULL(s.OnlineAmount, 0) AS OnlineAmount
                    FROM Sales s
                    WHERE s.Id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", saleId);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            d.BillNo = r["InvoiceNumber"]?.ToString() ?? "";
                            d.DateStr = Convert.ToDateTime(r["SaleDate"]).ToString("yyyy-MM-dd HH:mm:ss");
                            d.OrderType = r["OrderType"].ToString();
                            d.TableNumber = r["TableNumber"].ToString();
                            d.Kots = r["KotNumbers"].ToString();
                            d.TotalInvoiceValue = Convert.ToDecimal(r["GrandTotal"]);
                            d.SubTotal = Convert.ToDecimal(r["TaxableAmount"]);
                            d.Discount = r["Discount"] != DBNull.Value ? Convert.ToDecimal(r["Discount"]) : 0;
                            d.GstAmount = Convert.ToDecimal(r["Tax"]);
                            d.CgstAmount = Convert.ToDecimal(r["CGSTAmount"]);
                            d.SgstAmount = Convert.ToDecimal(r["SGSTAmount"]);
                            d.RoundOff = Convert.ToDecimal(r["RoundOff"]);
                            d.PaymentMethod = r["PaymentMethod"]?.ToString() ?? "Cash";
                            d.CashAmount = r["CashAmount"] != DBNull.Value ? Convert.ToDecimal(r["CashAmount"]) : 0m;
                            d.OnlineAmount = r["OnlineAmount"] != DBNull.Value ? Convert.ToDecimal(r["OnlineAmount"]) : 0m;

                            // If tax split was not stored separately, split 50-50
                            if (d.CgstAmount == 0 && d.SgstAmount == 0 && d.GstAmount > 0)
                            {
                                d.CgstAmount = Math.Round(d.GstAmount / 2.0m, 2);
                                d.SgstAmount = d.GstAmount - d.CgstAmount;
                            }
                        }
                    }
                }

                // 3. Sale Items
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT 
                        CASE WHEN sd.ItemType = 'Packaging' THEN 'Packaging Charges'
                             WHEN sd.ItemType = 'Service' THEN s.Name 
                             ELSE ISNULL(p.Name, 'Packaging Charges') END AS ItemName,
                        sd.Quantity, sd.UnitPrice, sd.Total,
                        ISNULL(sd.TaxableAmount, sd.Total) AS TaxableAmount,
                        ISNULL(sd.Instructions, '') AS Instructions
                    FROM SaleDetails sd
                    LEFT JOIN Products p ON sd.ProductId = p.Id
                    LEFT JOIN Services s ON sd.ServiceId = s.Id
                    WHERE sd.SaleId = @id
                    ORDER BY sd.Id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", saleId);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var it = new CafeBillItem();
                            it.Name = r["ItemName"]?.ToString() ?? "Item";
                            string inst = r["Instructions"]?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(inst))
                            {
                                it.Name += $" [{inst}]";
                            }
                            it.Qty = Convert.ToInt32(r["Quantity"]);
                            it.Rate = Convert.ToDecimal(r["UnitPrice"]);
                            it.Amt = Convert.ToDecimal(r["TaxableAmount"]);
                            d.Items.Add(it);
                            d.TotalQty += it.Qty;
                        }
                    }
                }
            }

            return d;
        }

        private static KotData LoadKotData(int kotId, bool isVoid, string voidReason)
        {
            KotData d = new KotData();
            d.IsVoid = isVoid;
            d.VoidReason = voidReason;

            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 KitchenPrinterName FROM AppProfile", conn))
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        d.KitchenPrinter = r["KitchenPrinterName"]?.ToString();
                    }
                }

                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT k.KOTNumber, k.TableNumber, k.OrderType, ISNULL(k.Steward, '') AS Steward,
                           k.CreatedAt, ISNULL(k.KotComment, '') AS KotComment, ISNULL(s.InvoiceNumber, '') AS InvoiceNumber
                    FROM KOTMaster k
                    LEFT JOIN Sales s ON k.SaleId = s.Id
                    WHERE k.Id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", kotId);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            d.KotNumber = Convert.ToInt32(r["KOTNumber"]);
                            d.TableNumber = r["TableNumber"].ToString();
                            d.OrderType = r["OrderType"].ToString();
                            d.Steward = r["Steward"].ToString();
                            d.DateStr = Convert.ToDateTime(r["CreatedAt"]).ToString("yyyy-MM-dd HH:mm:ss");
                            d.KotComment = r["KotComment"].ToString();
                            d.BillNumber = r["InvoiceNumber"]?.ToString() ?? "";
                        }
                    }
                }

                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT ItemName, Quantity, Rate, Amount, ISNULL(Instructions, '') AS Instructions, IsVoided, ISNULL(VoidReason, '') AS VoidReason
                    FROM KOTDetails
                    WHERE KOTId = @id
                    ORDER BY Id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", kotId);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            bool itemVoided = Convert.ToBoolean(r["IsVoided"]);
                            if (isVoid && !itemVoided) continue; // If void slip, only show voided items

                            var it = new CafeBillItem();
                            it.Name = r["ItemName"].ToString();
                            string inst = r["Instructions"]?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(inst))
                            {
                                it.Name += $" [{inst}]";
                            }
                            it.Qty = Convert.ToInt32(r["Quantity"]);
                            d.Items.Add(it);
                        }
                    }
                }
            }

            return d;
        }

        public static SettlementPrintData LoadSettlementData(DateTime date, decimal? actualCashOverride = null, string remarksOverride = null)
        {
            SettlementPrintData d = new SettlementPrintData();
            d.SettlementDate = date.Date;
            d.PrintTime = DateTime.Now;

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    // 1. App Profile
                    using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 ShopName, GSTIN, Address, Phone, BillingPrinterName, LogoPath FROM AppProfile", conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            d.ShopName = r["ShopName"]?.ToString() ?? "The Local Cafe";
                            d.GSTIN = r["GSTIN"]?.ToString() ?? "";
                            d.Address = r["Address"]?.ToString() ?? "";
                            d.ContactNo = r["Phone"]?.ToString() ?? "";
                            d.BillingPrinter = r["BillingPrinterName"]?.ToString();
                            d.LogoPath = r["LogoPath"]?.ToString();
                        }
                    }

                    // Logo resolution
                    if (!string.IsNullOrEmpty(d.LogoPath) && !File.Exists(d.LogoPath))
                    {
                        string candidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, d.LogoPath);
                        if (File.Exists(candidate)) d.LogoPath = candidate;
                    }
                    if (string.IsNullOrEmpty(d.LogoPath) || !File.Exists(d.LogoPath))
                    {
                        string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_transparent.png");
                        string p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.jpg");
                        string p3 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logo.jpg");
                        if (File.Exists(p1)) d.LogoPath = p1;
                        else if (File.Exists(p2)) d.LogoPath = p2;
                        else if (File.Exists(p3)) d.LogoPath = p3;
                    }

                    // 2. Sales Summary (Bill Count, Gross Sales, Discounts, Tax)
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT 
                            COUNT(*) AS BillCount,
                            ISNULL(SUM(GrandTotal), 0) AS GrossSales,
                            ISNULL(SUM(Discount), 0) AS TotalDiscount,
                            ISNULL(SUM(Tax), 0) AS TotalTax
                        FROM Sales 
                        WHERE CAST(SaleDate as DATE) = @date", conn))
                    {
                        cmd.Parameters.AddWithValue("@date", date.Date);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                d.TotalBillsCount = Convert.ToInt32(r["BillCount"]);
                                d.GrossSales = Convert.ToDecimal(r["GrossSales"]);
                                d.TotalDiscounts = Convert.ToDecimal(r["TotalDiscount"]);
                                d.TotalTax = Convert.ToDecimal(r["TotalTax"]);
                            }
                        }
                    }

                    // 3. Returns and Refunds
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT 
                            ISNULL(SUM(TotalRefund), 0) AS TotalRefunds,
                            ISNULL(SUM(CashRefund), 0) AS CashRefunds
                        FROM SalesReturns 
                        WHERE CAST(ReturnDate as DATE) = @date", conn))
                    {
                        cmd.Parameters.AddWithValue("@date", date.Date);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                d.ReturnsAndRefunds = Convert.ToDecimal(r["TotalRefunds"]);
                                d.CashRefunds = Convert.ToDecimal(r["CashRefunds"]);
                            }
                        }
                    }
                    d.NetSales = d.GrossSales - d.ReturnsAndRefunds;

                    // 4. Void KOTs
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT 
                            COUNT(kd.Id) AS VoidCount,
                            ISNULL(SUM(kd.Amount), 0) AS VoidAmount
                        FROM KOTDetails kd
                        INNER JOIN KOTMaster k ON kd.KOTId = k.Id
                        WHERE (kd.IsVoided = 1 OR k.Status = 'Voided' OR k.IsVoided = 1)
                          AND CAST(ISNULL(kd.VoidedAt, ISNULL(k.VoidedAt, k.CreatedAt)) AS DATE) = @date", conn))
                    {
                        cmd.Parameters.AddWithValue("@date", date.Date);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                d.VoidCount = Convert.ToInt32(r["VoidCount"]);
                                d.VoidAmount = Convert.ToDecimal(r["VoidAmount"]);
                            }
                        }
                    }

                    // 5. Check if record exists in DailySettlements
                    bool isSaved = false;
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT TOP 1 
                            s.OpeningCash, s.CashSales, s.DueCollections, s.CardSales, s.QRSales, s.CardQRSales,
                            s.DuesCreated, s.ExpectedCash, s.ActualCash, s.Variance, s.Remarks, s.Refunds, s.VoidAmount,
                            ISNULL(u.FullName, 'Administrator') AS SettledByName
                        FROM DailySettlements s
                        LEFT JOIN Users u ON s.SettlementBy = u.Id
                        WHERE CAST(s.SettlementDate as DATE) = @date
                        ORDER BY s.Id DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@date", date.Date);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                isSaved = true;
                                d.OpeningCash = Convert.ToDecimal(r["OpeningCash"]);
                                d.CashSales = Convert.ToDecimal(r["CashSales"]);
                                d.DueCollectionsTotal = Convert.ToDecimal(r["DueCollections"]);
                                d.CardSales = Convert.ToDecimal(r["CardSales"]);
                                decimal cardQRSales = Convert.ToDecimal(r["CardQRSales"]);
                                d.QRSales = r["QRSales"] != DBNull.Value ? Convert.ToDecimal(r["QRSales"]) : (cardQRSales - d.CardSales);
                                d.DuesCreated = Convert.ToDecimal(r["DuesCreated"]);
                                d.ExpectedCash = Convert.ToDecimal(r["ExpectedCash"]);
                                d.ActualCash = actualCashOverride.HasValue ? actualCashOverride.Value : Convert.ToDecimal(r["ActualCash"]);
                                d.Variance = actualCashOverride.HasValue ? (d.ActualCash - d.ExpectedCash) : Convert.ToDecimal(r["Variance"]);
                                d.Remarks = !string.IsNullOrEmpty(remarksOverride) ? remarksOverride : (r["Remarks"]?.ToString() ?? "");
                                d.SettledBy = r["SettledByName"]?.ToString() ?? "Administrator";
                                if (d.VoidAmount == 0) d.VoidAmount = Convert.ToDecimal(r["VoidAmount"]);
                            }
                        }
                    }

                    // If not saved in DailySettlements yet, calculate live metrics
                    if (!isSaved)
                    {
                        d.SettledBy = Session.FullName ?? "System Administrator";
                        d.Remarks = !string.IsNullOrEmpty(remarksOverride) ? remarksOverride : "Daily settlement reconciliation completed.";

                        // Opening Cash
                        using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 ActualCash FROM DailySettlements WHERE CAST(SettlementDate as DATE) < @date ORDER BY SettlementDate DESC, Id DESC", conn))
                        {
                            cmd.Parameters.AddWithValue("@date", date.Date);
                            object obj = cmd.ExecuteScalar();
                            if (obj != null && obj != DBNull.Value) d.OpeningCash = Convert.ToDecimal(obj);
                        }

                        // Cash Sales
                        using (SqlCommand cmd = new SqlCommand(@"
                            SELECT ISNULL(SUM(
                                CASE 
                                    WHEN PaymentMethod = 'Cash' THEN (CASE WHEN AmountPaid > GrandTotal THEN GrandTotal ELSE AmountPaid END)
                                    WHEN PaymentMethod = 'Split' THEN (CASE WHEN ISNULL(CashAmount, 0) > GrandTotal THEN GrandTotal ELSE ISNULL(CashAmount, 0) END)
                                    ELSE 0 
                                END), 0) 
                            FROM Sales 
                            WHERE CAST(SaleDate as DATE) = @date", conn))
                        {
                            cmd.Parameters.AddWithValue("@date", date.Date);
                            d.CashSales = Convert.ToDecimal(cmd.ExecuteScalar());
                        }

                        // Card Sales
                        using (SqlCommand cmd = new SqlCommand(@"
                            SELECT ISNULL(SUM(
                                CASE 
                                    WHEN PaymentMethod = 'Card' THEN (CASE WHEN AmountPaid > GrandTotal THEN GrandTotal ELSE AmountPaid END)
                                    ELSE 0 
                                END), 0) 
                            FROM Sales 
                            WHERE CAST(SaleDate as DATE) = @date", conn))
                        {
                            cmd.Parameters.AddWithValue("@date", date.Date);
                            d.CardSales = Convert.ToDecimal(cmd.ExecuteScalar());
                        }

                        // QR / UPI Sales
                        using (SqlCommand cmd = new SqlCommand(@"
                            SELECT ISNULL(SUM(
                                CASE 
                                    WHEN PaymentMethod NOT IN ('Cash', 'Card', 'Split') THEN (CASE WHEN AmountPaid > GrandTotal THEN GrandTotal ELSE AmountPaid END)
                                    WHEN PaymentMethod = 'Split' THEN (CASE WHEN ISNULL(OnlineAmount, 0) > GrandTotal THEN GrandTotal ELSE ISNULL(OnlineAmount, 0) END)
                                    ELSE 0 
                                END), 0) 
                            FROM Sales 
                            WHERE CAST(SaleDate as DATE) = @date", conn))
                        {
                            cmd.Parameters.AddWithValue("@date", date.Date);
                            d.QRSales = Convert.ToDecimal(cmd.ExecuteScalar());
                        }

                        // Dues Created
                        using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(DueAmount), 0) FROM Sales WHERE CAST(SaleDate as DATE) = @date", conn))
                        {
                            cmd.Parameters.AddWithValue("@date", date.Date);
                            d.DuesCreated = Convert.ToDecimal(cmd.ExecuteScalar());
                        }

                        // Dues Collections
                        decimal prevDues = 0;
                        using (SqlCommand cmd = new SqlCommand(@"
                            SELECT ISNULL(SUM(cp.Amount), 0)
                            FROM CustomerPayments cp
                            LEFT JOIN Sales s ON cp.SaleId = s.Id
                            WHERE CAST(cp.PaymentDate as DATE) = @date
                              AND cp.PaymentMethod = 'Cash'
                              AND (CAST(s.SaleDate as DATE) < @date OR cp.SaleId IS NULL)", conn))
                        {
                            cmd.Parameters.AddWithValue("@date", date.Date);
                            prevDues = Convert.ToDecimal(cmd.ExecuteScalar());
                        }

                        decimal todayRepayments = 0;
                        using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(Amount), 0) FROM CustomerPayments WHERE CAST(PaymentDate as DATE) = @date AND PaymentMethod = 'Cash'", conn))
                        {
                            cmd.Parameters.AddWithValue("@date", date.Date);
                            decimal tot = Convert.ToDecimal(cmd.ExecuteScalar());
                            todayRepayments = tot - prevDues;
                            if (todayRepayments < 0) todayRepayments = 0;
                        }

                        d.DueCollectionsPrev = prevDues;
                        d.DueCollectionsToday = todayRepayments;
                        d.DueCollectionsTotal = prevDues + todayRepayments;

                        // Card & QR dues
                        using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(Amount), 0) FROM CustomerPayments WHERE CAST(PaymentDate as DATE) = @date AND PaymentMethod = 'Card'", conn))
                        {
                            cmd.Parameters.AddWithValue("@date", date.Date);
                            d.CardSales += Convert.ToDecimal(cmd.ExecuteScalar());
                        }
                        using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(Amount), 0) FROM CustomerPayments WHERE CAST(PaymentDate as DATE) = @date AND PaymentMethod NOT IN ('Cash', 'Card')", conn))
                        {
                            cmd.Parameters.AddWithValue("@date", date.Date);
                            d.QRSales += Convert.ToDecimal(cmd.ExecuteScalar());
                        }

                        d.ExpectedCash = d.CashSales + d.DueCollectionsTotal - d.CashRefunds;
                        d.ActualCash = actualCashOverride.HasValue ? actualCashOverride.Value : d.ExpectedCash;
                        d.Variance = d.ActualCash - d.ExpectedCash;
                    }

                    d.TotalCollections = d.CashSales + d.CardSales + d.QRSales + d.DueCollectionsTotal;

                    // 6. Item-wise sales breakdown for that date
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
                            SELECT 
                                CASE 
                                    WHEN sd.ItemType = 'Packaging' THEN 'Packaging Charges'
                                    WHEN sd.ItemType = 'Service' THEN ISNULL(s.Name, 'Service Item')
                                    ELSE ISNULL(p.Name, 'Menu Item') 
                                END AS ItemName,
                                CASE 
                                    WHEN sd.ItemType = 'Packaging' THEN 'Packaging'
                                    WHEN sd.ItemType = 'Service' THEN ISNULL(s.Category, 'Service')
                                    ELSE ISNULL(p.Category, 'Food & Beverage') 
                                END AS CategoryName,
                                AVG(sd.UnitPrice) AS AvgRate,
                                SUM(sd.Quantity) AS TotalQty,
                                SUM(sd.Total) AS TotalAmount
                            FROM SaleDetails sd
                            INNER JOIN Sales sa ON sd.SaleId = sa.Id
                            LEFT JOIN Products p ON sd.ProductId = p.Id
                            LEFT JOIN Services s ON sd.ServiceId = s.Id
                            WHERE CAST(sa.SaleDate AS DATE) = @date
                            GROUP BY 
                                CASE 
                                    WHEN sd.ItemType = 'Packaging' THEN 'Packaging Charges'
                                    WHEN sd.ItemType = 'Service' THEN ISNULL(s.Name, 'Service Item')
                                    ELSE ISNULL(p.Name, 'Menu Item') 
                                END,
                                CASE 
                                    WHEN sd.ItemType = 'Packaging' THEN 'Packaging'
                                    WHEN sd.ItemType = 'Service' THEN ISNULL(s.Category, 'Service')
                                    ELSE ISNULL(p.Category, 'Food & Beverage') 
                                END
                            ORDER BY TotalAmount DESC, TotalQty DESC", conn))
                        {
                            cmd.Parameters.AddWithValue("@date", date.Date);
                            using (SqlDataReader r = cmd.ExecuteReader())
                            {
                                int sn = 1;
                                while (r.Read())
                                {
                                    var item = new SettlementSoldItem
                                    {
                                        SerialNo = sn++,
                                        ItemName = r["ItemName"]?.ToString() ?? "",
                                        Category = r["CategoryName"]?.ToString() ?? "General",
                                        Rate = r["AvgRate"] != DBNull.Value ? Convert.ToDecimal(r["AvgRate"]) : 0m,
                                        Quantity = r["TotalQty"] != DBNull.Value ? Convert.ToInt32(r["TotalQty"]) : 0,
                                        TotalAmount = r["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(r["TotalAmount"]) : 0m
                                    };
                                    d.SoldItems.Add(item);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return d;
        }
        #endregion

        #region Layout & Drawing Logic (The Local Cafe Format)
        private static Font FontRegular(float size) { return new Font("Consolas", size, FontStyle.Regular); }
        private static Font FontBold(float size) { return new Font("Consolas", size, FontStyle.Bold); }

        private static int EstimateCustomerBillHeight(CafeBillData d)
        {
            int h = 220; // Header and initial meta
            if (!string.IsNullOrEmpty(d.LogoPath) && File.Exists(d.LogoPath))
            {
                h += 75;
            }
            if (!string.IsNullOrEmpty(d.Address))
            {
                h += Math.Max(20, (d.Address.Length / 25 + 1) * 18);
            }
            h += d.Items.Count * 28;
            if (d.Discount > 0) h += 25;
            h += 205; // Subtotals, Taxes, Round Off, Total, Payment Method
            h += 90; // Footer greetings & feed
            return Math.Max(h, 520);
        }

        private static int EstimateKotHeight(KotData d)
        {
            int h = 170;
            h += d.Items.Count * 28;
            h += 90;
            return Math.Max(h, 320);
        }

        private static void DrawDashedLine(Graphics g, float y)
        {
            using (Pen p = new Pen(Color.Black, 1))
            {
                p.DashPattern = new float[] { 3, 2 };
                g.DrawLine(p, MarginLeft, y, MarginLeft + UsableWidth, y);
            }
        }

        private static void DrawCustomerBill(Graphics g, CafeBillData d)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

            Brush br = Brushes.Black;
            StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.Word };
            StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Near };
            StringFormat sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.Word };

            using (Font fHead = FontBold(9.5f))
            using (Font fBody = FontRegular(8.5f))
            using (Font fBold = FontBold(8.5f))
            using (Font fFoot = FontRegular(8f))
            {
                float y = 10;

                // 0. Cafe Logo (Centered)
                if (!string.IsNullOrEmpty(d.LogoPath) && File.Exists(d.LogoPath))
                {
                    try
                    {
                        using (Image img = Image.FromFile(d.LogoPath))
                        {
                            float logoSize = 65f;
                            float logoX = MarginLeft + (UsableWidth - logoSize) / 2f;
                            g.DrawImage(img, logoX, y, logoSize, logoSize);
                            y += logoSize + 6;
                        }
                    }
                    catch { }
                }

                // 1. Header (Centered)
                if (!string.IsNullOrEmpty(d.ShopName))
                {
                    SizeF shopSz = g.MeasureString(d.ShopName, fHead, (int)UsableWidth, sfCenter);
                    float h = Math.Max(18f, (float)Math.Ceiling(shopSz.Height));
                    g.DrawString(d.ShopName, fHead, br, new RectangleF(MarginLeft, y, UsableWidth, h), sfCenter);
                    y += h + 2;
                }

                if (!string.IsNullOrEmpty(d.GSTIN))
                {
                    SizeF gstSz = g.MeasureString("GST No: " + d.GSTIN, fBody, (int)UsableWidth, sfCenter);
                    float h = Math.Max(16f, (float)Math.Ceiling(gstSz.Height));
                    g.DrawString("GST No: " + d.GSTIN, fBody, br, new RectangleF(MarginLeft, y, UsableWidth, h), sfCenter);
                    y += h + 2;
                }

                if (!string.IsNullOrEmpty(d.Address))
                {
                    string[] addrLines = d.Address.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in addrLines)
                    {
                        SizeF sz = g.MeasureString(line, fBody, (int)UsableWidth, sfCenter);
                        float h = Math.Max(15f, (float)Math.Ceiling(sz.Height));
                        g.DrawString(line, fBody, br, new RectangleF(MarginLeft, y, UsableWidth, h), sfCenter);
                        y += h + 2;
                    }
                }

                if (!string.IsNullOrEmpty(d.ContactNo))
                {
                    SizeF phoneSz = g.MeasureString("Contact no: " + d.ContactNo, fBody, (int)UsableWidth, sfCenter);
                    float h = Math.Max(16f, (float)Math.Ceiling(phoneSz.Height));
                    g.DrawString("Contact no: " + d.ContactNo, fBody, br, new RectangleF(MarginLeft, y, UsableWidth, h), sfCenter);
                    y += h + 2;
                }

                DrawDashedLine(g, y);
                y += 6;

                // 2. Order Metadata
                string typeUpper = d.OrderType.ToUpperInvariant();
                g.DrawString("Type: " + (typeUpper.Contains("TAKE") ? "Take Away" : typeUpper), fBody, br, MarginLeft, y);
                y += 15;

                if (!string.IsNullOrEmpty(d.TableNumber) && !typeUpper.Contains("TAKE"))
                {
                    g.DrawString("Table Number: " + d.TableNumber, fBody, br, MarginLeft, y);
                    y += 15;
                }

                DrawDashedLine(g, y);
                y += 6;

                g.DrawString("Bill No: " + d.BillNo, fBody, br, MarginLeft, y);
                y += 15;
                g.DrawString("Date: " + d.DateStr, fBody, br, MarginLeft, y);
                y += 15;

                if (!string.IsNullOrEmpty(d.Kots))
                {
                    g.DrawString("Kots: " + d.Kots, fBody, br, MarginLeft, y);
                    y += 15;
                }

                DrawDashedLine(g, y);
                y += 6;

                // 3. Table Column Headers
                g.DrawString("Item", fBody, br, MarginLeft, y);
                g.DrawString("Qty", fBody, br, MarginLeft + 155, y);
                g.DrawString("Amt", fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                y += 16;

                DrawDashedLine(g, y);
                y += 6;

                // 4. Line Items (Wrapped nicely so long names are fully visible)
                foreach (var it in d.Items)
                {
                    string itemName = it.Name ?? "";
                    SizeF nameSz = g.MeasureString(itemName, fBody, 150, sfLeft);
                    float rowH = Math.Max(16f, (float)Math.Ceiling(nameSz.Height));

                    g.DrawString(itemName, fBody, br, new RectangleF(MarginLeft, y, 150, rowH), sfLeft);
                    g.DrawString(it.Qty.ToString(), fBody, br, MarginLeft + 160, y);
                    g.DrawString(it.Amt.ToString("0.00"), fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                    y += rowH + 2;
                }

                DrawDashedLine(g, y);
                y += 6;

                // 5. Total Qty & SubTotal
                g.DrawString("Total Qty:", fBody, br, MarginLeft, y);
                g.DrawString(d.TotalQty.ToString(), fBody, br, MarginLeft + 160, y);
                y += 15;

                g.DrawString("SubTotal:", fBody, br, MarginLeft, y);
                g.DrawString(d.SubTotal.ToString("0.00"), fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                y += 18;

                if (d.Discount > 0)
                {
                    g.DrawString("Offers / Disc:", fBody, br, MarginLeft, y);
                    g.DrawString("-" + d.Discount.ToString("0.00"), fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                    y += 18;
                }

                DrawDashedLine(g, y);
                y += 6;

                // 6. GST Breakdown (5% Split into CGST 2.5% & SGST 2.5%)
                g.DrawString("GST@5%:", fBody, br, MarginLeft, y);
                g.DrawString(d.GstAmount.ToString("0.00"), fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                y += 15;

                g.DrawString("  CGST @2.5%", fBody, br, MarginLeft, y);
                g.DrawString(d.CgstAmount.ToString("0.00"), fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                y += 15;

                g.DrawString("  SGST @2.5%", fBody, br, MarginLeft, y);
                g.DrawString(d.SgstAmount.ToString("0.00"), fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                y += 18;

                DrawDashedLine(g, y);
                y += 6;

                // 7. Round Off & Total Invoice Value
                g.DrawString("Round Off:", fBody, br, MarginLeft, y);
                g.DrawString(d.RoundOff.ToString("0.00"), fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                y += 16;

                g.DrawString("Total Invoice Value:", fBold, br, MarginLeft, y);
                g.DrawString(d.TotalInvoiceValue.ToString("0"), fBold, br, new RectangleF(MarginLeft, y, UsableWidth, 16), sfRight);
                y += 20;

                if (!string.IsNullOrEmpty(d.PaymentMethod))
                {
                    string payStr = d.PaymentMethod;
                    if (d.PaymentMethod == "Split")
                    {
                        payStr = $"Split (Cash ₹{d.CashAmount:0} + Online ₹{d.OnlineAmount:0})";
                    }
                    g.DrawString("Payment Mode:", fBody, br, MarginLeft, y);
                    g.DrawString(payStr, fBold, br, new RectangleF(MarginLeft, y, UsableWidth, 16), sfRight);
                    y += 18;
                }

                DrawDashedLine(g, y);
                y += 10;

                // 8. Custom Greeting & Branding (Centered)
                if (!string.IsNullOrEmpty(d.FooterGreeting))
                {
                    SizeF greetSz = g.MeasureString(d.FooterGreeting, fBody, (int)UsableWidth, sfCenter);
                    float greetH = Math.Max(16f, (float)Math.Ceiling(greetSz.Height));
                    g.DrawString(d.FooterGreeting, fBody, br, new RectangleF(MarginLeft, y, UsableWidth, greetH), sfCenter);
                    y += greetH + 4;
                }

                DrawDashedLine(g, y);
                y += 8;

                if (!string.IsNullOrEmpty(d.Branding))
                {
                    g.DrawString(d.Branding, fFoot, br, new RectangleF(MarginLeft, y, UsableWidth, 16), sfCenter);
                    y += 22;
                }
            }
        }

        private static void DrawKotSlip(Graphics g, KotData d)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

            Brush br = Brushes.Black;
            StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far };

            using (Font fHead = FontBold(9.5f))
            using (Font fBody = FontRegular(8.5f))
            using (Font fBold = FontBold(8.5f))
            {
                float y = 10;

                if (d.IsVoid)
                {
                    g.DrawString("Void KOT Item", fHead, br, MarginLeft, y);
                    y += 18;
                }
                else
                {
                    g.DrawString("KITCHEN ORDER TICKET", fHead, br, MarginLeft, y);
                    y += 18;
                }

                DrawDashedLine(g, y);
                y += 6;

                g.DrawString("Type:" + d.OrderType, fBody, br, MarginLeft, y);
                y += 15;
                g.DrawString("T No:" + d.TableNumber, fBold, br, MarginLeft, y);
                y += 16;

                DrawDashedLine(g, y);
                y += 6;

                if (!string.IsNullOrEmpty(d.BillNumber))
                {
                    g.DrawString("Bill Number:" + d.BillNumber, fBody, br, MarginLeft, y);
                    y += 15;
                }
                g.DrawString("Steward:" + d.Steward, fBody, br, MarginLeft, y);
                y += 15;
                g.DrawString("Date:" + d.DateStr, fBody, br, MarginLeft, y);
                y += 15;
                g.DrawString("Kot Number:" + d.KotNumber, fBold, br, MarginLeft, y);
                y += 18;

                DrawDashedLine(g, y);
                y += 6;

                g.DrawString("Item", fBody, br, MarginLeft, y);
                g.DrawString("Qty", fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                y += 16;

                DrawDashedLine(g, y);
                y += 6;

                StringFormat sfItemLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.Word };
                int totalQty = 0;
                foreach (var it in d.Items)
                {
                    string prefix = d.IsVoid ? "* " + it.Name : "* " + it.Name;
                    string qtyStr = d.IsVoid ? "- " + it.Qty : it.Qty.ToString();
                    totalQty += it.Qty;

                    SizeF sz = g.MeasureString(prefix, fBold, 180, sfItemLeft);
                    float rowH = Math.Max(18f, (float)Math.Ceiling(sz.Height));

                    g.DrawString(prefix, fBold, br, new RectangleF(MarginLeft, y, 180, rowH), sfItemLeft);
                    g.DrawString(qtyStr, fBold, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                    y += rowH + 2;
                }

                DrawDashedLine(g, y);
                y += 6;

                string totalQtyStr = d.IsVoid ? "- " + totalQty : totalQty.ToString();
                g.DrawString("Total Qty:", fBold, br, MarginLeft, y);
                g.DrawString(totalQtyStr, fBold, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                y += 18;

                DrawDashedLine(g, y);
                y += 8;

                if (d.IsVoid && !string.IsNullOrEmpty(d.VoidReason))
                {
                    string note = "Void Item Comment: " + d.VoidReason;
                    SizeF nSz = g.MeasureString(note, fBody, (int)UsableWidth, sfItemLeft);
                    float nH = Math.Max(18f, (float)Math.Ceiling(nSz.Height));
                    g.DrawString(note, fBody, br, new RectangleF(MarginLeft, y, UsableWidth, nH), sfItemLeft);
                    y += nH + 4;
                }
                else if (!string.IsNullOrEmpty(d.KotComment))
                {
                    string note = "Notes: " + d.KotComment;
                    SizeF nSz = g.MeasureString(note, fBody, (int)UsableWidth, sfItemLeft);
                    float nH = Math.Max(18f, (float)Math.Ceiling(nSz.Height));
                    g.DrawString(note, fBody, br, new RectangleF(MarginLeft, y, UsableWidth, nH), sfItemLeft);
                    y += nH + 4;
                }
            }
        }

        private static int EstimateSettlementHeight(SettlementPrintData d)
        {
            int h = 450;
            if (!string.IsNullOrEmpty(d.LogoPath) && File.Exists(d.LogoPath))
            {
                h += 75;
            }
            if (!string.IsNullOrEmpty(d.Address))
            {
                h += Math.Max(20, (d.Address.Length / 25 + 1) * 16);
            }
            if (!string.IsNullOrEmpty(d.Remarks))
            {
                h += Math.Max(25, (d.Remarks.Length / 30 + 1) * 16);
            }
            if (d.SoldItems != null && d.SoldItems.Count > 0)
            {
                h += 70 + d.SoldItems.Count * 25;
            }
            h += 350;
            return Math.Max(h, 880);
        }

        private static void DrawSolidLine(Graphics g, float y)
        {
            using (Pen p = new Pen(Color.Black, 1.2f))
            {
                g.DrawLine(p, MarginLeft, y, MarginLeft + UsableWidth, y);
            }
        }

        private static void DrawDottedLine(Graphics g, float y)
        {
            using (Pen p = new Pen(Color.Black, 1))
            {
                p.DashPattern = new float[] { 1, 2 };
                g.DrawLine(p, MarginLeft, y, MarginLeft + UsableWidth, y);
            }
        }

        private static void DrawRowKeyValue(Graphics g, Font fKey, Font fVal, Brush br, string key, string val, ref float y, float rowHeight, StringFormat sfRight)
        {
            g.DrawString(key, fKey, br, MarginLeft, y);
            g.DrawString(val, fVal, br, new RectangleF(MarginLeft, y, UsableWidth, rowHeight), sfRight);
            y += rowHeight;
        }

        private static void DrawSettlementSlip(Graphics g, SettlementPrintData d)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Brush br = Brushes.Black;
            StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.Word };
            StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Near };
            StringFormat sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.Word };

            using (Font fTitle = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (Font fHead = new Font("Segoe UI", 10f, FontStyle.Bold))
            using (Font fBody = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (Font fBold = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (Font fSection = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (Font fFoot = new Font("Segoe UI", 8f, FontStyle.Regular))
            using (Font fSmall = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            using (Font fSmallBold = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            {
                float y = 10;

                // 0. Logo
                if (!string.IsNullOrEmpty(d.LogoPath) && File.Exists(d.LogoPath))
                {
                    try
                    {
                        using (Image img = Image.FromFile(d.LogoPath))
                        {
                            float logoSize = 65f;
                            float logoX = MarginLeft + (UsableWidth - logoSize) / 2f;
                            g.DrawImage(img, logoX, y, logoSize, logoSize);
                            y += logoSize + 6;
                        }
                    }
                    catch { }
                }

                // 1. Cafe Name & Contact
                if (!string.IsNullOrEmpty(d.ShopName))
                {
                    SizeF shopSz = g.MeasureString(d.ShopName, fHead, (int)UsableWidth, sfCenter);
                    float h = Math.Max(18f, (float)Math.Ceiling(shopSz.Height));
                    g.DrawString(d.ShopName, fHead, br, new RectangleF(MarginLeft, y, UsableWidth, h), sfCenter);
                    y += h + 2;
                }

                if (!string.IsNullOrEmpty(d.GSTIN))
                {
                    g.DrawString("GSTIN: " + d.GSTIN, fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 16), sfCenter);
                    y += 17;
                }

                if (!string.IsNullOrEmpty(d.Address))
                {
                    string[] addrLines = d.Address.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in addrLines)
                    {
                        SizeF sz = g.MeasureString(line, fBody, (int)UsableWidth, sfCenter);
                        float h = Math.Max(15f, (float)Math.Ceiling(sz.Height));
                        g.DrawString(line, fBody, br, new RectangleF(MarginLeft, y, UsableWidth, h), sfCenter);
                        y += h + 2;
                    }
                }

                if (!string.IsNullOrEmpty(d.ContactNo))
                {
                    g.DrawString("Ph: " + d.ContactNo, fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 16), sfCenter);
                    y += 17;
                }

                DrawSolidLine(g, y);
                y += 6;

                // 2. Report Title
                g.DrawString("DAILY SETTLEMENT REPORT", fTitle, br, new RectangleF(MarginLeft, y, UsableWidth, 18), sfCenter);
                y += 18;
                g.DrawString("(DAY CLOSE / Z-REPORT)", fSmallBold, br, new RectangleF(MarginLeft, y, UsableWidth, 14), sfCenter);
                y += 16;

                DrawSolidLine(g, y);
                y += 6;

                // 3. Metadata
                DrawRowKeyValue(g, fBody, fBold, br, "Settlement Date:", d.SettlementDate.ToString("dd-MMM-yyyy"), ref y, 16, sfRight);
                DrawRowKeyValue(g, fBody, fBody, br, "Printed At:", d.PrintTime.ToString("dd-MMM-yyyy hh:mm tt"), ref y, 16, sfRight);
                DrawRowKeyValue(g, fBody, fBody, br, "Settled By:", d.SettledBy, ref y, 16, sfRight);

                string auditStatus;
                if (Math.Abs(d.Variance) < 0.01m)
                    auditStatus = "BALANCED ✓";
                else if (d.Variance < 0)
                    auditStatus = $"SHORT (-Rs. {Math.Abs(d.Variance):0.00})";
                else
                    auditStatus = $"SURPLUS (+Rs. {d.Variance:0.00})";

                DrawRowKeyValue(g, fBody, fBold, br, "Audit Result:", auditStatus, ref y, 16, sfRight);

                // 4. Section 1: Sales Summary
                y += 4;
                DrawDashedLine(g, y);
                y += 5;
                g.DrawString("--- SALES & BILLING SUMMARY ---", fSection, br, new RectangleF(MarginLeft, y, UsableWidth, 16), sfCenter);
                y += 16;
                DrawDashedLine(g, y);
                y += 6;

                string billCountStr = d.TotalBillsCount == 1 ? "1 Bill" : $"{d.TotalBillsCount} Bills";
                DrawRowKeyValue(g, fBody, fBody, br, $"Gross Sales ({billCountStr}):", $"Rs. {d.GrossSales:N2}", ref y, 16, sfRight);

                if (d.TotalDiscounts > 0)
                {
                    DrawRowKeyValue(g, fBody, fBody, br, "Discounts Given:", $"-Rs. {d.TotalDiscounts:N2}", ref y, 16, sfRight);
                }

                if (d.ReturnsAndRefunds > 0)
                {
                    DrawRowKeyValue(g, fBody, fBody, br, "Sales Returns/Refunds:", $"-Rs. {d.ReturnsAndRefunds:N2}", ref y, 16, sfRight);
                }

                DrawDottedLine(g, y);
                y += 5;
                DrawRowKeyValue(g, fBold, fBold, br, "NET SALES TODAY:", $"Rs. {d.NetSales:N2}", ref y, 17, sfRight);

                if (d.VoidAmount > 0 || d.VoidCount > 0)
                {
                    string voidItemStr = d.VoidCount > 0 ? $" ({d.VoidCount} Items)" : "";
                    DrawRowKeyValue(g, fBody, fBody, br, $"Void/Cancelled{voidItemStr}:", $"Rs. {d.VoidAmount:N2}", ref y, 16, sfRight);
                }

                // 5. Section 2: Payment / Tender Breakdown
                y += 4;
                DrawDashedLine(g, y);
                y += 5;
                g.DrawString("--- PAYMENT / TENDER BREAKDOWN ---", fSection, br, new RectangleF(MarginLeft, y, UsableWidth, 16), sfCenter);
                y += 16;
                DrawDashedLine(g, y);
                y += 6;

                DrawRowKeyValue(g, fBody, fBody, br, "Cash Sales:", $"Rs. {d.CashSales:N2}", ref y, 16, sfRight);
                DrawRowKeyValue(g, fBody, fBody, br, "Card Payments:", $"Rs. {d.CardSales:N2}", ref y, 16, sfRight);
                DrawRowKeyValue(g, fBody, fBody, br, "QR / Online / UPI:", $"Rs. {d.QRSales:N2}", ref y, 16, sfRight);

                if (d.DuesCreated > 0)
                {
                    DrawRowKeyValue(g, fBody, fBody, br, "Credit / Dues Created:", $"Rs. {d.DuesCreated:N2}", ref y, 16, sfRight);
                }

                if (d.DueCollectionsTotal > 0)
                {
                    DrawRowKeyValue(g, fBody, fBody, br, "Prev Dues Recovered:", $"Rs. {d.DueCollectionsTotal:N2}", ref y, 16, sfRight);
                }

                DrawDottedLine(g, y);
                y += 5;
                DrawRowKeyValue(g, fBold, fBold, br, "TOTAL COLLECTIONS:", $"Rs. {d.TotalCollections:N2}", ref y, 17, sfRight);

                // 6. Section 3: Cash Drawer Audit
                y += 4;
                DrawDashedLine(g, y);
                y += 5;
                g.DrawString("--- CASH DRAWER RECONCILIATION ---", fSection, br, new RectangleF(MarginLeft, y, UsableWidth, 16), sfCenter);
                y += 16;
                DrawDashedLine(g, y);
                y += 6;

                DrawRowKeyValue(g, fBody, fBody, br, "Opening Cash In Hand:", $"Rs. {d.OpeningCash:N2}", ref y, 16, sfRight);
                DrawRowKeyValue(g, fBody, fBody, br, "Add: Cash Sales Today:", $"+Rs. {d.CashSales:N2}", ref y, 16, sfRight);

                if (d.DueCollectionsTotal > 0)
                {
                    DrawRowKeyValue(g, fBody, fBody, br, "Add: Dues Recoveries:", $"+Rs. {d.DueCollectionsTotal:N2}", ref y, 16, sfRight);
                }

                if (d.CashRefunds > 0)
                {
                    DrawRowKeyValue(g, fBody, fBody, br, "Less: Cash Refunds Paid:", $"-Rs. {d.CashRefunds:N2}", ref y, 16, sfRight);
                }

                DrawDottedLine(g, y);
                y += 5;
                DrawRowKeyValue(g, fBold, fBold, br, "EXPECTED CASH IN DRAWER:", $"Rs. {d.ExpectedCash:N2}", ref y, 17, sfRight);
                DrawRowKeyValue(g, fBold, fBold, br, "ACTUAL CASH COUNTED:", $"Rs. {d.ActualCash:N2}", ref y, 17, sfRight);

                DrawSolidLine(g, y);
                y += 5;

                string varianceText;
                if (Math.Abs(d.Variance) < 0.01m)
                    varianceText = "Rs. 0.00 (MATCHED ✓)";
                else if (d.Variance < 0)
                    varianceText = $"-Rs. {Math.Abs(d.Variance):N2} (SHORTAGE)";
                else
                    varianceText = $"+Rs. {d.Variance:N2} (SURPLUS)";

                DrawRowKeyValue(g, fBold, fBold, br, "CASH VARIANCE:", varianceText, ref y, 18, sfRight);

                // 7. Section: Item-wise Sold List (if items exist)
                if (d.SoldItems != null && d.SoldItems.Count > 0)
                {
                    y += 4;
                    DrawDashedLine(g, y);
                    y += 5;
                    int totalSoldItemsQty = 0;
                    foreach (var sit in d.SoldItems) totalSoldItemsQty += sit.Quantity;
                    g.DrawString($"--- ITEMS SOLD TODAY ({totalSoldItemsQty} Units) ---", fSection, br, new RectangleF(MarginLeft, y, UsableWidth, 16), sfCenter);
                    y += 16;
                    DrawDashedLine(g, y);
                    y += 6;

                    g.DrawString("Item", fBold, br, MarginLeft, y);
                    g.DrawString("Qty", fBold, br, MarginLeft + 160, y);
                    g.DrawString("Amt", fBold, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                    y += 16;
                    DrawDottedLine(g, y);
                    y += 5;

                    foreach (var itm in d.SoldItems)
                    {
                        SizeF nSz = g.MeasureString(itm.ItemName, fBody, 150, sfLeft);
                        float rH = Math.Max(16f, (float)Math.Ceiling(nSz.Height));
                        g.DrawString(itm.ItemName, fBody, br, new RectangleF(MarginLeft, y, 150, rH), sfLeft);
                        g.DrawString(itm.Quantity.ToString(), fBody, br, MarginLeft + 160, y);
                        g.DrawString(itm.TotalAmount.ToString("0.00"), fBody, br, new RectangleF(MarginLeft, y, UsableWidth, 15), sfRight);
                        y += rH + 2;
                    }
                    DrawDottedLine(g, y);
                    y += 5;
                    decimal totalSoldAmt = 0;
                    foreach (var sit in d.SoldItems) totalSoldAmt += sit.TotalAmount;
                    DrawRowKeyValue(g, fBold, fBold, br, $"Total Sold ({totalSoldItemsQty} itm):", $"Rs. {totalSoldAmt:N2}", ref y, 17, sfRight);
                }

                // 8. Remarks
                if (!string.IsNullOrEmpty(d.Remarks))
                {
                    DrawDashedLine(g, y);
                    y += 5;
                    g.DrawString("Remarks / Notes:", fBold, br, MarginLeft, y);
                    y += 15;
                    SizeF remSz = g.MeasureString(d.Remarks, fBody, (int)UsableWidth, sfLeft);
                    float rH = Math.Max(16f, (float)Math.Ceiling(remSz.Height));
                    g.DrawString(d.Remarks, fBody, br, new RectangleF(MarginLeft, y, UsableWidth, rH), sfLeft);
                    y += rH + 4;
                }

                // 9. Signatures
                y += 8;
                DrawDashedLine(g, y);
                y += 16;

                float halfWidth = UsableWidth / 2f;
                g.DrawString("Cashier Sign:", fSmallBold, br, MarginLeft, y);
                g.DrawString("Manager Sign:", fSmallBold, br, MarginLeft + halfWidth, y);
                y += 24;
                g.DrawString("_______________", fBody, br, MarginLeft, y);
                g.DrawString("_______________", fBody, br, MarginLeft + halfWidth, y);
                y += 20;

                DrawSolidLine(g, y);
                y += 6;

                // 10. Footer
                g.DrawString("*** END OF DAILY SETTLEMENT ***", fSmallBold, br, new RectangleF(MarginLeft, y, UsableWidth, 14), sfCenter);
                y += 15;
                g.DrawString("Powered by - MeroDokan", fFoot, br, new RectangleF(MarginLeft, y, UsableWidth, 14), sfCenter);
                y += 25;
            }
        }

        private static void DrawA4SettlementPage(Graphics g, SettlementPrintData d, Rectangle bounds, ref int currentItemIndex, ref int pageNum, out bool hasMore)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
            StringFormat sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            StringFormat sfTopLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.Word };
            StringFormat sfTopRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Near };

            using (Font fShop = new Font("Segoe UI", 16f, FontStyle.Bold))
            using (Font fTitle = new Font("Segoe UI", 13f, FontStyle.Bold))
            using (Font fSection = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (Font fBody = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (Font fBold = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (Font fRow = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (Font fRowBold = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (Font fSmall = new Font("Segoe UI", 8f, FontStyle.Regular))
            using (Font fSmallBold = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (Font fTableHead = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                float left = bounds.Left;
                float width = bounds.Width;
                float bottom = bounds.Bottom;
                float y = bounds.Top;

                // 1. Header (Page 1 = Full Header, Page 2+ = Compact Header)
                if (pageNum == 1)
                {
                    float logoW = 0;
                    if (!string.IsNullOrEmpty(d.LogoPath) && File.Exists(d.LogoPath))
                    {
                        try
                        {
                            using (Image img = Image.FromFile(d.LogoPath))
                            {
                                logoW = 55f;
                                g.DrawImage(img, left, y, logoW, logoW);
                            }
                        }
                        catch { }
                    }

                    float textLeft = left + (logoW > 0 ? logoW + 12 : 0);
                    g.DrawString(d.ShopName, fShop, Brushes.Black, textLeft, y - 2);

                    string contactLine = "";
                    if (!string.IsNullOrEmpty(d.Address)) contactLine += d.Address.Replace("\r\n", ", ").Replace("\n", ", ") + " ";
                    if (!string.IsNullOrEmpty(d.GSTIN)) contactLine += "• GSTIN: " + d.GSTIN + " ";
                    if (!string.IsNullOrEmpty(d.ContactNo)) contactLine += "• Ph: " + d.ContactNo;

                    // Document Title Block (Right aligned)
                    float rightBlockW = 275;
                    float rightX = left + width - rightBlockW;
                    g.DrawString(contactLine, fSmall, Brushes.DimGray, new RectangleF(textLeft, y + 24, width - (logoW > 0 ? logoW + 12 : 0) - (rightBlockW + 10), 32), sfTopLeft);

                    g.DrawString("DAILY SETTLEMENT REPORT", fTitle, Brushes.Black, new RectangleF(rightX, y, rightBlockW, 20), sfTopRight);
                    g.DrawString("DAY CLOSE & ITEM-WISE SALES AUDIT", fSmallBold, Brushes.DarkSlateGray, new RectangleF(rightX, y + 22, rightBlockW, 16), sfTopRight);
                    g.DrawString($"Date: {d.SettlementDate:dd-MMM-yyyy}  |  By: {d.SettledBy}", fSmall, Brushes.DimGray, new RectangleF(rightX, y + 38, rightBlockW, 16), sfTopRight);

                    y += Math.Max(logoW, 58f) + 6;

                    // Accent Line
                    using (Pen accentPen = new Pen(Color.FromArgb(16, 185, 129), 2f))
                    {
                        g.DrawLine(accentPen, left, y, left + width, y);
                    }
                    y += 8;

                    // 2. Summary Boxes (Two Cards side-by-side)
                    float colW = (width - 14) / 2f;
                    float cardH = 175f;
                    float col1X = left;
                    float col2X = left + colW + 14;

                    using (SolidBrush cardBg = new SolidBrush(Color.FromArgb(249, 250, 251)))
                    {
                        g.FillRectangle(cardBg, col1X, y, colW, cardH);
                        g.FillRectangle(cardBg, col2X, y, colW, cardH);
                    }
                    using (Pen borderPen = new Pen(Color.FromArgb(229, 231, 235), 1f))
                    {
                        g.DrawRectangle(borderPen, col1X, y, colW, cardH);
                        g.DrawRectangle(borderPen, col2X, y, colW, cardH);
                    }

                    using (SolidBrush headBg = new SolidBrush(Color.FromArgb(243, 244, 246)))
                    {
                        g.FillRectangle(headBg, col1X, y, colW, 22);
                        g.FillRectangle(headBg, col2X, y, colW, 22);
                    }
                    g.DrawString("1. SALES & PAYMENT SUMMARY", fSection, Brushes.Black, col1X + 8, y + 3);
                    g.DrawString("2. CASH DRAWER AUDIT", fSection, Brushes.Black, col2X + 8, y + 3);

                    // Card 1 Rows
                    float rY = y + 26;
                    float rH = 16f;
                    string billStr = d.TotalBillsCount == 1 ? "1 Bill" : $"{d.TotalBillsCount} Bills";
                    DrawCardRow(g, fBody, fBody, $"Gross Sales ({billStr}):", $"Rs. {d.GrossSales:N2}", col1X + 8, rY, colW - 16, sfRight); rY += rH;
                    if (d.TotalDiscounts > 0) { DrawCardRow(g, fBody, fBody, "Discounts Given:", $"-Rs. {d.TotalDiscounts:N2}", col1X + 8, rY, colW - 16, sfRight); rY += rH; }
                    if (d.ReturnsAndRefunds > 0) { DrawCardRow(g, fBody, fBody, "Returns & Refunds:", $"-Rs. {d.ReturnsAndRefunds:N2}", col1X + 8, rY, colW - 16, sfRight); rY += rH; }
                    DrawCardRow(g, fBold, fBold, "Net Sales Today:", $"Rs. {d.NetSales:N2}", col1X + 8, rY, colW - 16, sfRight); rY += rH;
                    g.DrawLine(Pens.LightGray, col1X + 8, rY, col1X + colW - 8, rY); rY += 3;
                    DrawCardRow(g, fBody, fBody, "Cash Sales:", $"Rs. {d.CashSales:N2}", col1X + 8, rY, colW - 16, sfRight); rY += rH;
                    DrawCardRow(g, fBody, fBody, "Card Payments:", $"Rs. {d.CardSales:N2}", col1X + 8, rY, colW - 16, sfRight); rY += rH;
                    DrawCardRow(g, fBody, fBody, "QR / UPI Online:", $"Rs. {d.QRSales:N2}", col1X + 8, rY, colW - 16, sfRight); rY += rH;
                    if (d.DuesCreated > 0) { DrawCardRow(g, fBody, fBody, "Credit / Dues Created:", $"Rs. {d.DuesCreated:N2}", col1X + 8, rY, colW - 16, sfRight); rY += rH; }
                    g.DrawLine(Pens.LightGray, col1X + 8, rY, col1X + colW - 8, rY); rY += 3;
                    DrawCardRow(g, fBold, fBold, "Total Collections:", $"Rs. {d.TotalCollections:N2}", col1X + 8, rY, colW - 16, sfRight);

                    // Card 2 Rows
                    rY = y + 26;
                    DrawCardRow(g, fBody, fBody, "Opening Cash In Hand:", $"Rs. {d.OpeningCash:N2}", col2X + 8, rY, colW - 16, sfRight); rY += rH;
                    DrawCardRow(g, fBody, fBody, "Add: Cash Sales Today:", $"+Rs. {d.CashSales:N2}", col2X + 8, rY, colW - 16, sfRight); rY += rH;
                    if (d.DueCollectionsTotal > 0) { DrawCardRow(g, fBody, fBody, "Add: Dues Recovered:", $"+Rs. {d.DueCollectionsTotal:N2}", col2X + 8, rY, colW - 16, sfRight); rY += rH; }
                    if (d.CashRefunds > 0) { DrawCardRow(g, fBody, fBody, "Less: Cash Refunds:", $"-Rs. {d.CashRefunds:N2}", col2X + 8, rY, colW - 16, sfRight); rY += rH; }
                    g.DrawLine(Pens.LightGray, col2X + 8, rY, col2X + colW - 8, rY); rY += 3;
                    DrawCardRow(g, fBold, fBold, "Expected Cash in Drawer:", $"Rs. {d.ExpectedCash:N2}", col2X + 8, rY, colW - 16, sfRight); rY += rH;
                    DrawCardRow(g, fBold, fBold, "Actual Counted Cash:", $"Rs. {d.ActualCash:N2}", col2X + 8, rY, colW - 16, sfRight); rY += rH;
                    g.DrawLine(Pens.Black, col2X + 8, rY, col2X + colW - 8, rY); rY += 3;
                    string varText = Math.Abs(d.Variance) < 0.01m ? "Rs. 0.00 (MATCHED ✓)" : (d.Variance < 0 ? $"-Rs. {Math.Abs(d.Variance):N2} (SHORT)" : $"+Rs. {d.Variance:N2} (SURPLUS)");
                    DrawCardRow(g, fBold, fBold, "Cash Variance:", varText, col2X + 8, rY, colW - 16, sfRight); rY += rH;
                    if (!string.IsNullOrEmpty(d.Remarks))
                    {
                        g.DrawString("Remarks: " + d.Remarks, fSmall, Brushes.DimGray, new RectangleF(col2X + 8, rY + 2, colW - 16, 28), sfTopLeft);
                    }

                    y += cardH + 12;
                }
                else
                {
                    g.DrawString($"{d.ShopName} — DAILY SETTLEMENT REPORT — Date: {d.SettlementDate:dd-MMM-yyyy} (Page {pageNum})", fSmallBold, Brushes.DimGray, left, y);
                    y += 16;
                    g.DrawLine(Pens.LightGray, left, y, left + width, y);
                    y += 8;
                }

                // 3. Item-wise Sold Table Header
                g.DrawString("3. ITEM-WISE SALES BREAKDOWN (COMPLETE MENU & PRODUCT AUDIT)", fSection, Brushes.Black, left, y);
                y += 18;

                float colSN = 35;
                float colItem = 290;
                float colCat = 140;
                float colRate = 85;
                float colQty = 75;
                float colAmt = width - (colSN + colItem + colCat + colRate + colQty);

                float xSN = left;
                float xItem = xSN + colSN;
                float xCat = xItem + colItem;
                float xRate = xCat + colCat;
                float xQty = xRate + colRate;
                float xAmt = xQty + colQty;

                using (SolidBrush thBg = new SolidBrush(Color.FromArgb(229, 231, 235)))
                {
                    g.FillRectangle(thBg, left, y, width, 22);
                }
                g.DrawRectangle(Pens.DarkGray, left, y, width, 22);

                g.DrawString("S.N.", fTableHead, Brushes.Black, new RectangleF(xSN, y + 3, colSN, 16), sfCenter);
                g.DrawString("Item Name / Description", fTableHead, Brushes.Black, new RectangleF(xItem + 4, y + 3, colItem - 6, 16), sfLeft);
                g.DrawString("Category", fTableHead, Brushes.Black, new RectangleF(xCat + 4, y + 3, colCat - 6, 16), sfLeft);
                g.DrawString("Avg Rate", fTableHead, Brushes.Black, new RectangleF(xRate, y + 3, colRate - 4, 16), sfRight);
                g.DrawString("Qty Sold", fTableHead, Brushes.Black, new RectangleF(xQty, y + 3, colQty, 16), sfCenter);
                g.DrawString("Total Amount", fTableHead, Brushes.Black, new RectangleF(xAmt, y + 3, colAmt - 6, 16), sfRight);

                y += 22;

                // 4. Render Sold Items
                float rowH = 20f;
                while (currentItemIndex < d.SoldItems.Count)
                {
                    if (y + rowH > bottom - 50)
                    {
                        g.DrawLine(Pens.Gray, left, y, left + width, y);
                        g.DrawString($"MeroDokan Cafe System  •  Daily Settlement Report ({d.SettlementDate:yyyy-MM-dd})  •  Page {pageNum}", fSmall, Brushes.Gray, left, bottom - 18);
                        hasMore = true;
                        pageNum++;
                        return;
                    }

                    var itm = d.SoldItems[currentItemIndex];
                    if (currentItemIndex % 2 == 1)
                    {
                        using (SolidBrush rowBg = new SolidBrush(Color.FromArgb(249, 250, 251)))
                        {
                            g.FillRectangle(rowBg, left, y, width, rowH);
                        }
                    }

                    g.DrawString(itm.SerialNo.ToString(), fRow, Brushes.Black, new RectangleF(xSN, y + 2, colSN, 16), sfCenter);
                    g.DrawString(itm.ItemName, fRowBold, Brushes.Black, new RectangleF(xItem + 4, y + 2, colItem - 6, 16), sfLeft);
                    g.DrawString(itm.Category, fRow, Brushes.DimGray, new RectangleF(xCat + 4, y + 2, colCat - 6, 16), sfLeft);
                    g.DrawString(itm.Rate.ToString("N2"), fRow, Brushes.Black, new RectangleF(xRate, y + 2, colRate - 4, 16), sfRight);
                    g.DrawString(itm.Quantity.ToString(), fRowBold, Brushes.Black, new RectangleF(xQty, y + 2, colQty, 16), sfCenter);
                    g.DrawString(itm.TotalAmount.ToString("N2"), fRowBold, Brushes.Black, new RectangleF(xAmt, y + 2, colAmt - 6, 16), sfRight);

                    g.DrawLine(Pens.LightGray, left, y + rowH, left + width, y + rowH);
                    y += rowH;
                    currentItemIndex++;
                }

                // Table Total Row
                int totalQty = 0;
                decimal totalAmt = 0;
                foreach (var itm in d.SoldItems)
                {
                    totalQty += itm.Quantity;
                    totalAmt += itm.TotalAmount;
                }

                using (SolidBrush totBg = new SolidBrush(Color.FromArgb(243, 244, 246)))
                {
                    g.FillRectangle(totBg, left, y, width, 22);
                }
                g.DrawRectangle(Pens.DarkGray, left, y, width, 22);

                g.DrawString($"TOTAL ITEMS SOLD ({totalQty} Total Units):", fSection, Brushes.Black, new RectangleF(xItem + 4, y + 3, colItem + colCat, 16), sfLeft);
                g.DrawString(totalQty.ToString(), fSection, Brushes.Black, new RectangleF(xQty, y + 3, colQty, 16), sfCenter);
                g.DrawString($"Rs. {totalAmt:N2}", fSection, Brushes.Black, new RectangleF(xAmt, y + 3, colAmt - 6, 16), sfRight);
                y += 32;

                // Signatures
                if (y + 75 > bottom - 30)
                {
                    g.DrawString($"MeroDokan Cafe System  •  Daily Settlement Report ({d.SettlementDate:yyyy-MM-dd})  •  Page {pageNum}", fSmall, Brushes.Gray, left, bottom - 18);
                    hasMore = true;
                    pageNum++;
                    return;
                }

                float sigW = 220;
                float sig1X = left + 30;
                float sig2X = left + width - sigW - 30;

                g.DrawLine(Pens.Black, sig1X, y + 35, sig1X + sigW, y + 35);
                g.DrawString("Cashier / Prepared By Signature", fSmallBold, Brushes.Black, new RectangleF(sig1X, y + 40, sigW, 16), sfCenter);

                g.DrawLine(Pens.Black, sig2X, y + 35, sig2X + sigW, y + 35);
                g.DrawString("Manager / Audit Verified Signature", fSmallBold, Brushes.Black, new RectangleF(sig2X, y + 40, sigW, 16), sfCenter);

                g.DrawString($"MeroDokan Cafe System  •  Daily Settlement Report ({d.SettlementDate:yyyy-MM-dd})  •  Page {pageNum}", fSmall, Brushes.Gray, left, bottom - 18);
                hasMore = false;
            }
        }

        private static void DrawCardRow(Graphics g, Font fKey, Font fVal, string key, string val, float x, float y, float w, StringFormat sfRight)
        {
            g.DrawString(key, fKey, Brushes.Black, x, y);
            g.DrawString(val, fVal, Brushes.Black, new RectangleF(x, y, w, 16), sfRight);
        }
        #endregion
    }
}
