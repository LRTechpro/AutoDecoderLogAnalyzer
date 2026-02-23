// ================================================================
// File: Form1.cs
// Project: AutoDecoder.Gui
// Course: MS539 Programming Concepts (Graduate)
// Assignment: OOP classes/objects/inheritance/encapsulation + GUI
// Author: Harold Watkins
// Purpose:
//   This WinForms UI (code-only) loads automotive log text, classifies each
//   line into a LogLine-derived type (inheritance), decodes details, supports
//   filtering/searching, and shows a summary of UDS findings (NRCs/DIDs) and
//   multi-line UDS conversations (ISO-TP reassembly + request/response pairing).
// ================================================================

#nullable enable

using AutoDecoder.Models;                    // LogLine + LogSession + derived line types (Models DLL).
using AutoDecoder.Protocols.Classifiers;     // LineClassifier logic (Protocols DLL).
using AutoDecoder.Protocols.Utilities;       // FindingsAggregator + UdsTables (Protocols DLL).
using AutoDecoder.Protocols.Conversations;   // ISO-TP / UDS conversation builders (Protocols DLL).
using System;                                // Base .NET types.
using System.Collections.Generic;            // List<T>, Dictionary<TKey,TValue>.
using System.ComponentModel;                 // BindingList<T> for UI binding.
using System.Drawing;                        // Color, Font.
using System.IO;                             // File.ReadAllLines, Path.
using System.Linq;                           // LINQ.
using System.Windows.Forms;                  // WinForms UI components.

namespace AutoDecoder.Gui
{
    /// <summary>
    /// Main application window.
    /// </summary>
    public class Form1 : Form
    {
        // ---------------------------
        // Protocol-aware derived data
        // ---------------------------
        private List<IsoTpPdu> _pdus = new();
        private List<UdsTransaction> _transactions = new();

        // ---------------------------
        // Split containers for layout
        // ---------------------------
        private SplitContainer decodedRootSplit = null!;
        private SplitContainer decodedBottomSplit = null!;
        private SplitContainer rawDecodedSplit = null!;

        // ---------------------------
        // Sessions (multi-log support)
        // ---------------------------
        private const int MaxSessions = 5;
        private readonly BindingList<LogSession> _sessions = new();
        private LogSession? _activeSession;

        // ---------------------------
        // Data (lines + filtered view)
        // ---------------------------
        private BindingList<LogLine> _allLogLines = new();
        private BindingList<LogLine> _filteredLogLines = new();

        // ---------------------------
        // UI controls (code-only UI)
        // ---------------------------
        private SplitContainer splitMain = null!;
        private ListBox lstSessions = null!;
        private Button btnAddSession = null!;
        private Button btnCloseSession = null!;

        private TabControl tabControl = null!;
        private TabPage tabDecoded = null!;
        private TabPage tabSummary = null!;

        private DataGridView dgvLines = null!;
        private RichTextBox rtbRaw = null!;
        private RichTextBox rtbDecoded = null!;

        private TextBox txtSearch = null!;
        private ComboBox cboTypeFilter = null!;
        private CheckBox chkUdsOnly = null!;
        private CheckBox chkMatchAllTerms = null!;

        private Button btnLoadFile = null!;
        private Button btnLoadSample = null!;
        private Button btnPaste = null!;
        private Button btnClear = null!;

        private Label lblStatusTotal = null!;
        private Label lblStatusIso = null!;
        private Label lblStatusXml = null!;
        private Label lblStatusUnknown = null!;

        private Label lblSummaryIso = null!;
        private Label lblSummaryUds = null!;
        private Label lblSummaryUnknown = null!;

        private ListView lvNrc = null!;
        private ListView lvDid = null!;

        // Ensures we only wire DataBindingComplete once
        private bool _gridBindingHooked;

        public Form1()
        {
            BuildUi();
            WireEvents();
            CreateNewSession(makeActive: true);
        }

        // ================================================================
        // UI BUILD
        // ================================================================

        private void BuildUi()
        {
            Text = "AutoDecoder Workbench (Code-Only)";
            Width = 1400;
            Height = 850;
            StartPosition = FormStartPosition.CenterScreen;

            splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };
            Controls.Add(splitMain);

            // Left panel
            var leftTop = new Panel { Dock = DockStyle.Top, Height = 78 };
            btnAddSession = new Button { Text = "New Session", Dock = DockStyle.Top, Height = 36 };
            btnCloseSession = new Button { Text = "Close Session", Dock = DockStyle.Top, Height = 36 };
            leftTop.Controls.Add(btnCloseSession);
            leftTop.Controls.Add(btnAddSession);

            lstSessions = new ListBox
            {
                Dock = DockStyle.Top,
                IntegralHeight = true
            };

            // Only show enough height for MaxSessions rows
            lstSessions.Height = (lstSessions.ItemHeight * MaxSessions) + 6;
            lstSessions.DisplayMember = "Name";

            var leftFill = new Panel { Dock = DockStyle.Fill };

            // Dock order: fill first, then top-docked controls
            splitMain.Panel1.Controls.Add(leftFill);
            splitMain.Panel1.Controls.Add(lstSessions);
            splitMain.Panel1.Controls.Add(leftTop);

            // Right panel: tabs
            tabControl = new TabControl { Dock = DockStyle.Fill };
            tabDecoded = new TabPage("Decoded");
            tabSummary = new TabPage("Summary");
            tabControl.TabPages.Add(tabDecoded);
            tabControl.TabPages.Add(tabSummary);
            splitMain.Panel2.Controls.Add(tabControl);

            BuildDecodedTab();
            BuildSummaryTab();
        }

        private void BuildDecodedTab()
        {
            decodedRootSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                FixedPanel = FixedPanel.Panel1
            };
            tabDecoded.Controls.Add(decodedRootSplit);

            var top = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 10,
                RowCount = 2,
                Padding = new Padding(4),
                Margin = new Padding(0)
            };

            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));   // LoadFile
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));   // LoadSample
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));   // Paste
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));   // Clear
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));   // Search label
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));   // Search textbox
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));   // Type label
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));  // Type dropdown
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));   // UDS only
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));  // Match all

            top.RowStyles.Clear();
            top.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            top.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));

            btnLoadFile = new Button { Text = "Load File", Dock = DockStyle.Fill, Margin = new Padding(2) };
            btnLoadSample = new Button { Text = "Load Sample", Dock = DockStyle.Fill, Margin = new Padding(2) };
            btnPaste = new Button { Text = "Paste", Dock = DockStyle.Fill, Margin = new Padding(2) };
            btnClear = new Button { Text = "Clear", Dock = DockStyle.Fill, Margin = new Padding(2) };

            var lblSearch = new Label
            {
                Text = "Search:",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 0, 2, 0)
            };

            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 5, 2, 2)
            };

            var lblType = new Label
            {
                Text = "Type:",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 0, 2, 0)
            };

            cboTypeFilter = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(2, 5, 2, 2)
            };

            chkUdsOnly = new CheckBox { Text = "UDS only", Dock = DockStyle.Fill, Margin = new Padding(8, 6, 2, 2) };
            chkMatchAllTerms = new CheckBox { Text = "Match all terms", Dock = DockStyle.Fill, Margin = new Padding(8, 6, 2, 2) };

            cboTypeFilter.Items.Add("All");
            foreach (var v in Enum.GetValues(typeof(LineType)).Cast<LineType>())
                cboTypeFilter.Items.Add(v.ToString());
            cboTypeFilter.SelectedItem = "All";

            top.Controls.Add(btnLoadFile, 0, 0);
            top.Controls.Add(btnLoadSample, 1, 0);
            top.Controls.Add(btnPaste, 2, 0);
            top.Controls.Add(btnClear, 3, 0);
            top.Controls.Add(lblSearch, 4, 0);
            top.Controls.Add(txtSearch, 5, 0);
            top.Controls.Add(lblType, 6, 0);
            top.Controls.Add(cboTypeFilter, 7, 0);
            top.Controls.Add(chkUdsOnly, 8, 0);
            top.Controls.Add(chkMatchAllTerms, 9, 0);

            var statusPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(2, 0, 2, 0)
            };

            lblStatusTotal = new Label { AutoSize = true, Text = "Total: 0", Padding = new Padding(0, 4, 15, 0) };
            lblStatusIso = new Label { AutoSize = true, Text = "ISO: 0", Padding = new Padding(0, 4, 15, 0) };
            lblStatusXml = new Label { AutoSize = true, Text = "XML: 0", Padding = new Padding(0, 4, 15, 0) };
            lblStatusUnknown = new Label { AutoSize = true, Text = "Unknown: 0", Padding = new Padding(0, 4, 15, 0) };

            statusPanel.Controls.Add(lblStatusTotal);
            statusPanel.Controls.Add(lblStatusIso);
            statusPanel.Controls.Add(lblStatusXml);
            statusPanel.Controls.Add(lblStatusUnknown);

            top.Controls.Add(statusPanel, 0, 1);
            top.SetColumnSpan(statusPanel, 10);

            decodedRootSplit.Panel1.Controls.Add(top);

            decodedBottomSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal
            };
            decodedRootSplit.Panel2.Controls.Add(decodedBottomSplit);

            dgvLines = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = true
            };

            HookGridBindingOnce();

            decodedBottomSplit.Panel1.Controls.Add(dgvLines);

            rawDecodedSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };
            decodedBottomSplit.Panel2.Controls.Add(rawDecodedSplit);

            rtbRaw = new RichTextBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 10) };
            rtbDecoded = new RichTextBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 10) };
            rawDecodedSplit.Panel1.Controls.Add(rtbRaw);
            rawDecodedSplit.Panel2.Controls.Add(rtbDecoded);

            dgvLines.DataSource = _filteredLogLines;

            ConfigureDataGridColumns();
        }

        private void HookGridBindingOnce()
        {
            if (_gridBindingHooked) return;
            _gridBindingHooked = true;

            dgvLines.DataBindingComplete += (s, e) =>
            {
                // Always remove these auto-generated columns after any bind/regenerate
                RemoveColumnIfExists(dgvLines, "Confidence");
                RemoveColumnIfExists(dgvLines, "CanId");

                // Remove both timestamp columns completely
                RemoveColumnIfExists(dgvLines, "Timestamp");
                RemoveColumnIfExists(dgvLines, "TimestampText");

                // Keep CanNode visible
                if (dgvLines.Columns.Contains("CanNode"))
                {
                    var col = dgvLines.Columns["CanNode"];
                    if (col != null) col.Visible = true;
                }

                // Widths/headers only (NO DisplayIndex here)
                ApplyColumnSizing();

                // Safe ordering (sets DisplayIndex sequentially 0..N-1)
                ApplySafeDisplayOrder();
            };
        }

        private void ApplySafeDisplayOrder()
        {
            if (dgvLines == null || dgvLines.IsDisposed) return;
            if (dgvLines.Columns == null || dgvLines.Columns.Count == 0) return;

            // Only reorder columns that actually exist right now
            string[] desired =
            {
                "LineNumber",
                "Raw",
                "Type",
                "Summary",
                "Details",
                "CanNode"
            };

            var cols = desired
                .Where(n => dgvLines.Columns.Contains(n))
                .Select(n => dgvLines.Columns[n])
                .Where(c => c != null)
                .ToList();

            // Assign DisplayIndex sequentially (0..N-1) (never out of range)
            for (int i = 0; i < cols.Count; i++)
                cols[i]!.DisplayIndex = i;
        }

        private static void RemoveColumnIfExists(DataGridView grid, string columnName)
        {
            if (grid?.Columns == null) return;
            if (grid.Columns.Contains(columnName))
                grid.Columns.Remove(columnName);
        }

        private void BuildSummaryTab()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(10)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            layout.RowStyles.Clear();
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            tabSummary.Controls.Clear();
            tabSummary.Controls.Add(layout);

            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            lblSummaryIso = new Label { AutoSize = true, Text = "ISO Lines: 0", Padding = new Padding(0, 10, 20, 0) };
            lblSummaryUds = new Label { AutoSize = true, Text = "UDS Findings: 0", Padding = new Padding(0, 10, 20, 0) };
            lblSummaryUnknown = new Label { AutoSize = true, Text = "Unknown: 0", Padding = new Padding(0, 10, 20, 0) };

            top.Controls.Add(lblSummaryIso);
            top.Controls.Add(lblSummaryUds);
            top.Controls.Add(lblSummaryUnknown);

            layout.Controls.Add(top, 0, 0);
            layout.SetColumnSpan(top, 2);

            lvNrc = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            lvNrc.Columns.Add("NRC", 90);
            lvNrc.Columns.Add("Meaning", 260);
            lvNrc.Columns.Add("Count", 80);

            lvDid = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            lvDid.Columns.Add("DID", 90);
            lvDid.Columns.Add("Name", 260);
            lvDid.Columns.Add("Count", 80);

            var gbNrc = new GroupBox { Text = "NRCs", Dock = DockStyle.Fill };
            gbNrc.Controls.Add(lvNrc);

            var gbDid = new GroupBox { Text = "DIDs", Dock = DockStyle.Fill };
            gbDid.Controls.Add(lvDid);

            layout.Controls.Add(gbNrc, 0, 1);
            layout.Controls.Add(gbDid, 1, 1);
        }

        private void WireEvents()
        {
            btnLoadFile.Click += BtnLoadFile_Click;
            btnLoadSample.Click += BtnLoadSample_Click;
            btnPaste.Click += BtnPaste_Click;
            btnClear.Click += BtnClear_Click;

            dgvLines.RowPrePaint += DgvLines_RowPrePaint;
            dgvLines.SelectionChanged += DgvLines_SelectionChanged;

            txtSearch.TextChanged += FilterControls_Changed;
            cboTypeFilter.SelectedIndexChanged += FilterControls_Changed;
            chkUdsOnly.CheckedChanged += FilterControls_Changed;
            chkMatchAllTerms.CheckedChanged += FilterControls_Changed;

            lvNrc.ItemActivate += LvNrc_ItemActivate;
            lvDid.ItemActivate += LvDid_ItemActivate;

            btnAddSession.Click += BtnAddSession_Click;
            btnCloseSession.Click += BtnCloseSession_Click;
            lstSessions.SelectedIndexChanged += LstSessions_SelectedIndexChanged;
        }

        // ================================================================
        // SESSIONS
        // ================================================================

        private void CreateNewSession(bool makeActive)
        {
            if (_sessions.Count >= MaxSessions)
            {
                MessageBox.Show($"Max sessions reached ({MaxSessions}).", "Sessions",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var s = new LogSession
            {
                Name = $"Session {_sessions.Count + 1} - {DateTime.Now:HH:mm:ss}"
            };

            _sessions.Add(s);

            if (lstSessions.DataSource == null)
                lstSessions.DataSource = _sessions;

            if (makeActive)
            {
                lstSessions.SelectedItem = s;
                SetActiveSession(s);
            }
        }

        private void SetActiveSession(LogSession? session)
        {
            _activeSession = session;

            if (_activeSession == null)
            {
                _allLogLines = new BindingList<LogLine>();
                _filteredLogLines = new BindingList<LogLine>();
                dgvLines.DataSource = _filteredLogLines;
                UpdateStatusBar();
                UpdateFindingsSummary();
                return;
            }

            _allLogLines = _activeSession.AllLines;
            _filteredLogLines = _activeSession.FilteredLines;

            dgvLines.DataSource = _filteredLogLines;

            ApplyFilters();
            UpdateStatusBar();
            UpdateFindingsSummary();
        }

        private void BtnAddSession_Click(object? sender, EventArgs e)
            => CreateNewSession(makeActive: true);

        private void BtnCloseSession_Click(object? sender, EventArgs e)
        {
            if (_activeSession == null) return;

            int idx = lstSessions.SelectedIndex;
            var toClose = _activeSession;

            _sessions.Remove(toClose);

            if (_sessions.Count == 0)
            {
                CreateNewSession(makeActive: true);
                return;
            }

            int nextIdx = Math.Min(idx, _sessions.Count - 1);
            lstSessions.SelectedIndex = nextIdx;
            SetActiveSession(lstSessions.SelectedItem as LogSession);
        }

        private void LstSessions_SelectedIndexChanged(object? sender, EventArgs e)
            => SetActiveSession(lstSessions.SelectedItem as LogSession);

        // ================================================================
        // GRID / FILTERS / DECODING
        // ================================================================

        private void ConfigureDataGridColumns()
        {
            dgvLines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvLines.AllowUserToResizeColumns = true;
            dgvLines.AllowUserToOrderColumns = true;
        }

        /// <summary>
        /// Applies consistent column widths and readable headers after binding.
        /// IMPORTANT: Does NOT set DisplayIndex (ordering is handled elsewhere).
        /// </summary>
        private void ApplyColumnSizing()
        {
            if (dgvLines == null || dgvLines.IsDisposed) return;
            if (dgvLines.Columns == null || dgvLines.Columns.Count == 0) return;

            dgvLines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            void SetCol(string name, int width, string? header = null)
            {
                if (!dgvLines.Columns.Contains(name)) return;

                var col = dgvLines.Columns[name];
                if (col == null) return;

                col.Visible = true;
                col.Width = width;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                col.Resizable = DataGridViewTriState.True;

                if (!string.IsNullOrWhiteSpace(header))
                    col.HeaderText = header;
            }

            // We removed Timestamp/TimestampText columns earlier; do not size them here.
            SetCol("LineNumber", 80, "Line");
            SetCol("Raw", 360, "Raw");
            SetCol("Type", 95, "Type");
            SetCol("Summary", 320, "Report Summary");
            SetCol("Details", 520, "Technical Breakdown");
            SetCol("CanNode", 170, "Node");

            // Optional: keep Details from wrapping weirdly
            if (dgvLines.Columns.Contains("Details"))
            {
                var c = dgvLines.Columns["Details"];
                if (c != null) c.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            }
        }

        private void DgvLines_RowPrePaint(object? sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvLines.Rows.Count) return;

            var row = dgvLines.Rows[e.RowIndex];
            var logLine = row.DataBoundItem as LogLine;

            if (logLine == null)
            {
                row.DefaultCellStyle.BackColor = Color.White;
                return;
            }

            if (logLine.Type == LineType.Iso15765)
            {
                if (logLine.Details?.Contains("Negative Response", StringComparison.OrdinalIgnoreCase) == true ||
                    logLine.Details?.Contains("0x7F", StringComparison.OrdinalIgnoreCase) == true)
                {
                    row.DefaultCellStyle.BackColor = Color.LightSalmon;
                    return;
                }

                if (logLine.Details?.Contains("UDS Request", StringComparison.OrdinalIgnoreCase) == true)
                {
                    row.DefaultCellStyle.BackColor = Color.LightSkyBlue;
                    return;
                }

                if (logLine.Details?.Contains("UDS Positive Response", StringComparison.OrdinalIgnoreCase) == true ||
                    logLine.Details?.Contains("(0x62)", StringComparison.OrdinalIgnoreCase) == true)
                {
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                    return;
                }
            }

            row.DefaultCellStyle.BackColor = Color.White;
        }

        private void BtnLoadFile_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Select Log File",
                Filter = "Text Files (*.txt)|*.txt|Log Files (*.log)|*.log|All Files (*.*)|*.*"
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var lines = File.ReadAllLines(ofd.FileName);
                string fileName = Path.GetFileName(ofd.FileName);
                LoadLines(lines, sessionName: fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading file: {ex.Message}", "Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnLoadSample_Click(object? sender, EventArgs e)
        {
            string[] sampleLines =
            {
                "2025-10-21T10:23:45.123 ISO15765 RX <- [00,00,07,D8,7F,22,78]",
                "2025-10-21T10:23:45.200 ISO15765 TX -> [00,00,07,D0,62,80,6A,41,42,43,44]",
                "2025-10-21T10:23:47.000 ISO15765 TX -> [00,00,07,D0,22,F1,88]",
                "2025-10-21T10:23:47.100 ISO15765 RX <- [00,00,07,D8,62,F1,88,56,45,52,53,49,4F,4E,31]",
                "DEBUG: Starting diagnostic session",
                "<ns3:didValue didValue=\"F188\" type=\"Strategy\"><ns3:Response>4D59535452415445475931</ns3:Response></ns3:didValue>",
            };

            LoadLines(sampleLines, sessionName: "Sample");
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            _allLogLines.Clear();
            _filteredLogLines.Clear();
            rtbRaw.Clear();
            rtbDecoded.Clear();
            UpdateStatusBar();
            UpdateFindingsSummary();
        }

        private void BtnPaste_Click(object? sender, EventArgs e)
        {
            if (!Clipboard.ContainsText())
            {
                MessageBox.Show("Clipboard does not contain text.", "Paste",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var text = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("Clipboard text is empty.", "Paste",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            LoadLines(lines, sessionName: "Pasted");
        }

        private void DgvLines_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvLines.SelectedRows.Count <= 0) return;

            var row = dgvLines.SelectedRows[0];
            if (row.DataBoundItem is not LogLine logLine) return;

            rtbRaw.Text = logLine.Raw ?? string.Empty;
            rtbDecoded.Text = logLine.Details ?? string.Empty;
        }

        private void FilterControls_Changed(object? sender, EventArgs e) => ApplyFilters();

        private static List<string> TokenizeSearch(string input)
        {
            var tokens = new List<string>();
            if (string.IsNullOrWhiteSpace(input)) return tokens;

            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            foreach (char c in input)
            {
                if (c == '"') { inQuotes = !inQuotes; continue; }

                if (c == ' ' && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString().Trim());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0) tokens.Add(current.ToString().Trim());
            return tokens.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        }

        private void LoadLines(string[] lines, string? sessionName = null)
        {
            if (_activeSession == null)
                CreateNewSession(makeActive: true);

            if (_activeSession != null && !string.IsNullOrWhiteSpace(sessionName))
            {
                _activeSession.Name = sessionName;
                RefreshSessionListUi();
            }

            try
            {
                _allLogLines.Clear();
                _filteredLogLines.Clear();

                for (int i = 0; i < lines.Length; i++)
                {
                    string rawLine = lines[i];
                    int lineNumber = i + 1;

                    try
                    {
                        var logLine = LineClassifier.Classify(lineNumber, rawLine);
                        logLine.ParseAndDecode();
                        _allLogLines.Add(logLine);
                    }
                    catch (Exception ex)
                    {
                        var errorLine = new UnknownLine(lineNumber, rawLine, $"Error: {ex.Message}");
                        errorLine.ParseAndDecode();
                        _allLogLines.Add(errorLine);
                    }
                }

                ApplyFilters();

                _pdus = IsoTpReassembler.Build(_allLogLines);
                _transactions = UdsConversationBuilder.Build(_pdus);

                UpdateStatusBar();
                UpdateFindingsSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading lines: {ex.Message}", "Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyFilters()
        {
            string searchText = (txtSearch.Text ?? string.Empty).Trim();
            var tokens = TokenizeSearch(searchText);
            bool matchAll = chkMatchAllTerms.Checked;

            string typeFilter = cboTypeFilter.SelectedItem?.ToString() ?? "All";
            bool udsOnly = chkUdsOnly.Checked;

            _filteredLogLines.Clear();

            foreach (var logLine in _allLogLines)
            {
                string combined =
                    (logLine.Raw ?? "") + " " +
                    (logLine.Summary ?? "") + " " +
                    (logLine.Details ?? "");

                string field = NormalizeForSearch(combined);

                bool matchesSearch = true;
                if (tokens.Count > 0)
                {
                    matchesSearch = matchAll
                        ? tokens.All(t => field.Contains(NormalizeForSearch(t)))
                        : tokens.Any(t => field.Contains(NormalizeForSearch(t)));
                }

                bool matchesType = typeFilter == "All" || logLine.Type.ToString() == typeFilter;

                bool matchesUds =
                    !udsOnly ||
                    (logLine.Details?.Contains("UDS", StringComparison.OrdinalIgnoreCase) == true);

                if (matchesSearch && matchesType && matchesUds)
                    _filteredLogLines.Add(logLine);
            }

            UpdateFindingsSummary();
        }

        private void UpdateStatusBar()
        {
            int total = _allLogLines.Count;
            int iso = _allLogLines.Count(l => l.Type == LineType.Iso15765);
            int xml = _allLogLines.Count(l => l.Type == LineType.Xml);
            int unk = _allLogLines.Count(l => l.Type == LineType.Unknown);

            lblStatusTotal.Text = $"Total: {total}";
            lblStatusIso.Text = $"ISO: {iso}";
            lblStatusXml.Text = $"XML: {xml}";
            lblStatusUnknown.Text = $"Unknown: {unk}";
        }

        private void UpdateFindingsSummary()
        {
            var summary = FindingsAggregator.Build(_filteredLogLines);

            lblSummaryIso.Text = $"ISO Lines: {summary.IsoLines}";
            lblSummaryUds.Text = $"UDS Findings: {summary.UdsFindingLines}";
            lblSummaryUnknown.Text = $"Unknown: {summary.UnknownLines}";

            PopulateNrcListView(summary.NrcCounts);
            PopulateDidListView(summary.DidCounts);

            lblSummaryUds.Text += $" | Conversations: {_transactions?.Count ?? 0}";
        }

        private void PopulateNrcListView(Dictionary<byte, int> nrcCounts)
        {
            lvNrc.BeginUpdate();
            lvNrc.Items.Clear();

            foreach (var kvp in nrcCounts.OrderByDescending(k => k.Value).ThenBy(k => k.Key))
            {
                byte nrc = kvp.Key;
                int count = kvp.Value;

                string meaning =
                    UdsTables.NrcMeaning.TryGetValue(nrc, out var m)
                        ? m
                        : "UnknownNRC";

                var item = new ListViewItem($"0x{nrc:X2}");
                item.SubItems.Add(meaning);
                item.SubItems.Add(count.ToString());
                item.Tag = nrc;
                lvNrc.Items.Add(item);
            }

            lvNrc.EndUpdate();
        }

        private void PopulateDidListView(Dictionary<ushort, int> didCounts)
        {
            lvDid.BeginUpdate();
            lvDid.Items.Clear();

            foreach (var kvp in didCounts.OrderByDescending(k => k.Value).ThenBy(k => k.Key))
            {
                ushort did = kvp.Key;
                int count = kvp.Value;

                string name = UdsTables.DescribeDid(did);

                var item = new ListViewItem($"0x{did:X4}");
                item.SubItems.Add(name);
                item.SubItems.Add(count.ToString());
                item.Tag = did;

                lvDid.Items.Add(item);
            }

            lvDid.EndUpdate();
        }

        private void LvNrc_ItemActivate(object? sender, EventArgs e)
        {
            if (lvNrc.SelectedItems.Count <= 0) return;

            byte nrc = (byte)(lvNrc.SelectedItems[0].Tag ?? (byte)0);
            txtSearch.Text = $"0x{nrc:X2}";
            tabControl.SelectedTab = tabDecoded;
        }

        private void LvDid_ItemActivate(object? sender, EventArgs e)
        {
            if (lvDid.SelectedItems.Count <= 0) return;

            ushort did = (ushort)(lvDid.SelectedItems[0].Tag ?? (ushort)0);
            txtSearch.Text = $"0x{did:X4}";
            tabControl.SelectedTab = tabDecoded;
        }

        // ================================================================
        // SPLITTER SAFETY
        // ================================================================

        private static bool TryClampSplitter(SplitContainer s)
        {
            if (s == null || s.IsDisposed) return false;

            int size = (s.Orientation == Orientation.Vertical) ? s.Width : s.Height;
            if (size <= 0) return false;

            int min = s.Panel1MinSize;
            int max = size - s.SplitterWidth - s.Panel2MinSize;
            if (max < min) return false;

            int desired = s.SplitterDistance;
            int clamped = Math.Max(min, Math.Min(desired, max));

            if (clamped != desired)
                s.SplitterDistance = clamped;

            return true;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            BeginInvoke(new Action(() =>
            {
                splitMain.Panel1MinSize = 120;
                splitMain.Panel2MinSize = 500;

                decodedRootSplit.Panel1MinSize = 70;
                decodedRootSplit.Panel2MinSize = 500;

                decodedBottomSplit.Panel1MinSize = 250;
                decodedBottomSplit.Panel2MinSize = 200;

                rawDecodedSplit.Panel1MinSize = 200;
                rawDecodedSplit.Panel2MinSize = 200;

                splitMain.SplitterDistance = 140;
                decodedRootSplit.SplitterDistance = 60;
                decodedBottomSplit.SplitterDistance = 360;
                rawDecodedSplit.SplitterDistance = Math.Max(200, rawDecodedSplit.Width / 2);

                splitMain.FixedPanel = FixedPanel.Panel1;

                TryClampSplitter(splitMain);
                TryClampSplitter(decodedRootSplit);
                TryClampSplitter(decodedBottomSplit);
                TryClampSplitter(rawDecodedSplit);
            }));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (splitMain != null) TryClampSplitter(splitMain);
            if (decodedRootSplit != null) TryClampSplitter(decodedRootSplit);
            if (decodedBottomSplit != null) TryClampSplitter(decodedBottomSplit);
            if (rawDecodedSplit != null) TryClampSplitter(rawDecodedSplit);
        }

        // ================================================================
        // SEARCH NORMALIZATION + SESSION UI REFRESH
        // ================================================================

        private static string NormalizeForSearch(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            var chars = s.ToLowerInvariant().ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (c is ',' or '[' or ']' or '(' or ')' or '{' or '}' or ':' or ';' or '\t')
                    chars[i] = ' ';
            }

            return string.Join(" ", new string(chars)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private void RefreshSessionListUi()
        {
            if (lstSessions == null) return;
            if (lstSessions.DataSource == null) return;

            if (BindingContext != null)
            {
                if (BindingContext[lstSessions.DataSource] is CurrencyManager cm)
                    cm.Refresh();
            }

            lstSessions.Invalidate();
            lstSessions.Update();
        }
    }
}