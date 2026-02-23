// ================================================================
// File: Form1.cs
// Project: AutoDecoder.Gui
// Course: MS539 Programming Concepts (Graduate)
// Assignment: OOP classes/objects/inheritance/encapsulation + GUI
// Author: Harold Watkins
//
// PURPOSE (plain English):
// - Provide a WinForms “workbench” that can load automotive logs (file/paste/sample),
//   classify each line into a strongly-typed object (inheritance),
//   decode protocol details (UDS/ISO-TP),
//   filter/search lines,
//   and summarize findings (NRC/DID counts + reconstructed conversations).
//
// NOTE ABOUT “OVER-COMMENTING”:
// - This version is intentionally verbose to satisfy grading rubrics.
// - Many comments restate what the code does in “human” language.
// ================================================================

#nullable enable // Enable nullable reference type analysis (helps prevent null bugs).

// ----------------------------
// Project references (DLLs)
// ----------------------------

// AutoDecoder.Models: Contains LogLine base class + derived line types + LogSession etc.
using AutoDecoder.Models;

// AutoDecoder.Protocols.Classifiers: Logic that decides what type a line is (ISO/UDS/XML/ASCII/Unknown).
using AutoDecoder.Protocols.Classifiers;

// AutoDecoder.Protocols.Utilities: Aggregation + lookup tables (NRC meanings, DID descriptions, etc.).
using AutoDecoder.Protocols.Utilities;

// AutoDecoder.Protocols.Conversations: ISO-TP reassembly and pairing into request/response UDS transactions.
using AutoDecoder.Protocols.Conversations;

// ----------------------------
// Standard .NET namespaces
// ----------------------------
using System;                    // Core types like DateTime, Action, Exception, etc.
using System.Collections.Generic; // List<>, Dictionary<>, etc.
using System.ComponentModel;      // BindingList<T> for data binding to WinForms controls.
using System.Drawing;             // Fonts + colors for UI.
using System.IO;                  // File.ReadAllLines, Path.GetFileName, etc.
using System.Linq;                // LINQ helpers (Count, Where, Any, All, etc).
using System.Windows.Forms;       // WinForms controls.

namespace AutoDecoder.Gui
{
    /// <summary>
    /// Main application window.
    /// This form owns:
    /// - Session management (up to MaxSessions)
    /// - Loading lines (file/sample/paste)
    /// - Classification + decode
    /// - Filtering/search
    /// - Summaries (NRC/DID counts + conversation count)
    /// </summary>
    public class Form1 : Form
    {
        // ================================================================
        // PROTOCOL-AWARE DERIVED DATA
        // ================================================================

        // List of reconstructed ISO-TP PDUs (multi-frame messages).
        // This gets rebuilt after loading lines.
        private List<IsoTpPdu> _pdus = new();

        // List of reconstructed UDS request/response transactions.
        // This gets rebuilt after loading lines.
        private List<UdsTransaction> _transactions = new();

        // ================================================================
        // SPLIT CONTAINERS FOR LAYOUT
        // ================================================================

        // Top-level split for decoded tab layout.
        private SplitContainer decodedRootSplit = null!;

        // Split between grid and bottom detail panes.
        private SplitContainer decodedBottomSplit = null!;

        // Split between raw text and decoded text panes.
        private SplitContainer rawDecodedSplit = null!;

        // ================================================================
        // SESSIONS (MULTI-LOG SUPPORT)
        // ================================================================

        // Hard cap to prevent slow UI and too many loaded logs.
        private const int MaxSessions = 5;

        // BindingList supports WinForms data binding and UI refresh.
        private readonly BindingList<LogSession> _sessions = new();

        // Tracks the currently selected session (the “active” log dataset).
        private LogSession? _activeSession;

        // ================================================================
        // DATA (LINES + FILTERED VIEW)
        // ================================================================

        // All lines for the active session (full dataset).
        private BindingList<LogLine> _allLogLines = new();

        // Filtered view of lines (what the grid shows).
        private BindingList<LogLine> _filteredLogLines = new();

        // ================================================================
        // UI CONTROLS (CODE-ONLY UI)
        // ================================================================

        // Main split: left navigation (sessions) vs right main content (tabs).
        private SplitContainer splitMain = null!;

        // Session list (left nav).
        private ListBox lstSessions = null!;

        // Buttons for session lifecycle.
        private Button btnAddSession = null!;
        private Button btnCloseSession = null!;

        // Tab control for main views.
        private TabControl tabControl = null!;

        // Tabs: decoded line-by-line view and summary view.
        private TabPage tabDecoded = null!;
        private TabPage tabSummary = null!;

        // Grid showing filtered lines.
        private DataGridView dgvLines = null!;

        // Bottom panes: raw selected line and decoded details.
        private RichTextBox rtbRaw = null!;
        private RichTextBox rtbDecoded = null!;

        // Filter/search controls.
        private TextBox txtSearch = null!;
        private ComboBox cboTypeFilter = null!;
        private CheckBox chkUdsOnly = null!;
        private CheckBox chkMatchAllTerms = null!;

        // Load/clear controls.
        private Button btnLoadFile = null!;
        private Button btnLoadSample = null!;
        private Button btnPaste = null!;
        private Button btnClear = null!;

        // Status labels (counts).
        private Label lblStatusTotal = null!;
        private Label lblStatusIso = null!;
        private Label lblStatusXml = null!;
        private Label lblStatusUnknown = null!;

        // Summary labels.
        private Label lblSummaryIso = null!;
        private Label lblSummaryUds = null!;
        private Label lblSummaryUnknown = null!;

        // Summary list views for NRC and DID counts.
        private ListView lvNrc = null!;
        private ListView lvDid = null!;

        // Guard flag to avoid wiring events multiple times.
        private bool _gridBindingHooked;

        // ================================================================
        // CONSTRUCTOR
        // ================================================================

        public Form1()
        {
            // Build all UI controls (no designer file).
            BuildUi();

            // Hook events (click, selection changed, filters, etc.).
            WireEvents();

            // Create an initial session so the app starts “ready”.
            CreateNewSession(makeActive: true);
        }

        // ================================================================
        // UI BUILD
        // ================================================================

        private void BuildUi()
        {
            // Window title.
            Text = "AutoDecoder Workbench (Code-Only)";

            // Initial window size.
            Width = 1400;
            Height = 850;

            // Center the window on launch.
            StartPosition = FormStartPosition.CenterScreen;

            // Create main split container: left nav vs right content.
            splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,                     // Fill the form.
                Orientation = Orientation.Vertical         // Vertical split: left/right.
            };

            // Add split container to the form.
            Controls.Add(splitMain);

            // ----------------------------
            // Left panel (sessions + buttons)
            // ----------------------------

            // Panel for session buttons, pinned to top.
            var leftTop = new Panel { Dock = DockStyle.Top, Height = 78 };

            // Button: create new session.
            btnAddSession = new Button { Text = "New Session", Dock = DockStyle.Top, Height = 36 };

            // Button: close active session.
            btnCloseSession = new Button { Text = "Close Session", Dock = DockStyle.Top, Height = 36 };

            // Add buttons to top panel (order matters: last added appears on top for DockStyle.Top).
            leftTop.Controls.Add(btnCloseSession);
            leftTop.Controls.Add(btnAddSession);

            // ListBox that shows sessions.
            lstSessions = new ListBox
            {
                Dock = DockStyle.Top,      // Dock to top so it sits below the buttons.
                IntegralHeight = true      // Snap height to whole items.
            };

            // Limit the visible height so the list doesn’t dominate the UI.
            lstSessions.Height = (lstSessions.ItemHeight * MaxSessions) + 6;

            // Use Name property for session display.
            lstSessions.DisplayMember = "Name";

            // Fill panel (unused for now, but gives clean layout and room for future left-side controls).
            var leftFill = new Panel { Dock = DockStyle.Fill };

            // IMPORTANT: Dock order: add Fill first, then top-docked controls.
            splitMain.Panel1.Controls.Add(leftFill);
            splitMain.Panel1.Controls.Add(lstSessions);
            splitMain.Panel1.Controls.Add(leftTop);

            // ----------------------------
            // Right panel (tabs)
            // ----------------------------

            // Create tab control for main views.
            tabControl = new TabControl { Dock = DockStyle.Fill };

            // Create tabs.
            tabDecoded = new TabPage("Decoded");
            tabSummary = new TabPage("Summary");

            // Add tabs to the tab control.
            tabControl.TabPages.Add(tabDecoded);
            tabControl.TabPages.Add(tabSummary);

            // Add tab control to the right panel.
            splitMain.Panel2.Controls.Add(tabControl);

            // Build contents of each tab.
            BuildDecodedTab();
            BuildSummaryTab();
        }

        private void BuildDecodedTab()
        {
            // Root split: top filter panel vs bottom content.
            decodedRootSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,                 // Fill the decoded tab.
                Orientation = Orientation.Horizontal,   // Horizontal split: top/bottom.
                FixedPanel = FixedPanel.Panel1         // Keep top panel stable.
            };

            // Add root split to decoded tab.
            tabDecoded.Controls.Add(decodedRootSplit);

            // Top layout panel that holds buttons + filters + status.
            var top = new TableLayoutPanel
            {
                Dock = DockStyle.Top,      // Pin to top.
                AutoSize = true,           // Grow to fit contents.
                ColumnCount = 10,          // 10 columns for controls.
                RowCount = 2,              // 2 rows: controls + status.
                Padding = new Padding(4),  // internal spacing.
                Margin = new Padding(0)    // no extra outside margin.
            };

            // Define fixed/percent widths for each column.
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));   // LoadFile
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));   // LoadSample
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));   // Paste
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));   // Clear
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));   // Search label
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));   // Search textbox (expand)
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));   // Type label
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));  // Type dropdown
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));   // UDS only
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));  // Match all

            // Clear and define row heights.
            top.RowStyles.Clear();
            top.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // row 0: controls
            top.RowStyles.Add(new RowStyle(SizeType.Absolute, 20)); // row 1: status

            // Create action buttons.
            btnLoadFile = new Button { Text = "Load File", Dock = DockStyle.Fill, Margin = new Padding(2) };
            btnLoadSample = new Button { Text = "Load Sample", Dock = DockStyle.Fill, Margin = new Padding(2) };
            btnPaste = new Button { Text = "Paste", Dock = DockStyle.Fill, Margin = new Padding(2) };
            btnClear = new Button { Text = "Clear", Dock = DockStyle.Fill, Margin = new Padding(2) };

            // Create “Search:” label.
            var lblSearch = new Label
            {
                Text = "Search:",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 0, 2, 0)
            };

            // Create search textbox.
            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 5, 2, 2)
            };

            // Create “Type:” label.
            var lblType = new Label
            {
                Text = "Type:",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 0, 2, 0)
            };

            // Create type filter dropdown.
            cboTypeFilter = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList, // prevent arbitrary typing.
                Margin = new Padding(2, 5, 2, 2)
            };

            // Checkbox: restrict to UDS lines.
            chkUdsOnly = new CheckBox { Text = "UDS only", Dock = DockStyle.Fill, Margin = new Padding(8, 6, 2, 2) };

            // Checkbox: require all tokens to match.
            chkMatchAllTerms = new CheckBox { Text = "Match all terms", Dock = DockStyle.Fill, Margin = new Padding(8, 6, 2, 2) };

            // Populate type filter options.
            cboTypeFilter.Items.Add("All"); // Always include “All” for no filtering.
            foreach (var v in Enum.GetValues(typeof(LineType)).Cast<LineType>())
                cboTypeFilter.Items.Add(v.ToString());
            cboTypeFilter.SelectedItem = "All"; // Default selection.

            // Place controls into the TableLayoutPanel.
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

            // Status bar row container.
            var statusPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(2, 0, 2, 0)
            };

            // Create status labels.
            lblStatusTotal = new Label { AutoSize = true, Text = "Total: 0", Padding = new Padding(0, 4, 15, 0) };
            lblStatusIso = new Label { AutoSize = true, Text = "ISO: 0", Padding = new Padding(0, 4, 15, 0) };
            lblStatusXml = new Label { AutoSize = true, Text = "XML: 0", Padding = new Padding(0, 4, 15, 0) };
            lblStatusUnknown = new Label { AutoSize = true, Text = "Unknown: 0", Padding = new Padding(0, 4, 15, 0) };

            // Add labels to status panel.
            statusPanel.Controls.Add(lblStatusTotal);
            statusPanel.Controls.Add(lblStatusIso);
            statusPanel.Controls.Add(lblStatusXml);
            statusPanel.Controls.Add(lblStatusUnknown);

            // Add status panel to row 1 and span all columns.
            top.Controls.Add(statusPanel, 0, 1);
            top.SetColumnSpan(statusPanel, 10);

            // Put top panel into the top split panel.
            decodedRootSplit.Panel1.Controls.Add(top);

            // Bottom split: grid on top, detail panes on bottom.
            decodedBottomSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal
            };

            // Add bottom split to the lower area.
            decodedRootSplit.Panel2.Controls.Add(decodedBottomSplit);

            // Create DataGridView for log lines.
            dgvLines = new DataGridView
            {
                Dock = DockStyle.Fill,                          // fill container
                ReadOnly = true,                                // prevent editing
                AllowUserToAddRows = false,                      // no extra blank row
                AllowUserToDeleteRows = false,                   // prevent deletes
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, // select whole rows
                MultiSelect = false,                             // single row at a time
                AutoGenerateColumns = true                       // auto columns from LogLine properties
            };

            // Hook binding behavior once (removes unwanted columns, sizes, ordering safely).
            HookGridBindingOnce();

            // Add grid to upper panel of bottom split.
            decodedBottomSplit.Panel1.Controls.Add(dgvLines);

            // Create split between raw and decoded.
            rawDecodedSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };

            // Add raw/decoded split to the bottom panel.
            decodedBottomSplit.Panel2.Controls.Add(rawDecodedSplit);

            // Raw line textbox (monospace font).
            rtbRaw = new RichTextBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 10) };

            // Decoded detail textbox (monospace font).
            rtbDecoded = new RichTextBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 10) };

            // Add text boxes to split panels.
            rawDecodedSplit.Panel1.Controls.Add(rtbRaw);
            rawDecodedSplit.Panel2.Controls.Add(rtbDecoded);

            // Bind grid to filtered list so filters update the displayed rows.
            dgvLines.DataSource = _filteredLogLines;

            // Configure grid behavior (resizing, column order).
            ConfigureDataGridColumns();
        }

        private void HookGridBindingOnce()
        {
            // If already hooked, do nothing (prevents duplicate event wiring).
            if (_gridBindingHooked) return;

            // Mark as hooked.
            _gridBindingHooked = true;

            // When binding completes (columns auto-generated), adjust the grid.
            dgvLines.DataBindingComplete += (s, e) =>
            {
                // Remove columns we do not want shown.
                RemoveColumnIfExists(dgvLines, "Confidence"); // internal classifier confidence
                RemoveColumnIfExists(dgvLines, "CanId");      // internal CAN ID, not part of your displayed design

                // Remove timestamp-related columns (per your design choice).
                RemoveColumnIfExists(dgvLines, "Timestamp");
                RemoveColumnIfExists(dgvLines, "TimestampText");

                // Ensure CanNode is visible if it exists.
                if (dgvLines.Columns.Contains("CanNode"))
                {
                    var col = dgvLines.Columns["CanNode"];
                    if (col != null) col.Visible = true;
                }

                // Apply consistent widths/headers (no ordering here).
                ApplyColumnSizing();

                // Apply safe ordering after sizing so DisplayIndex never crashes.
                ApplySafeDisplayOrder();
            };
        }

        private void ApplySafeDisplayOrder()
        {
            // Defensive checks (avoid null/disposed).
            if (dgvLines == null || dgvLines.IsDisposed) return;
            if (dgvLines.Columns == null || dgvLines.Columns.Count == 0) return;

            // Desired visible column order (left to right).
            string[] desired =
            {
                "LineNumber",
                "Raw",
                "Type",
                "Summary",
                "Details",
                "CanNode"
            };

            // Select columns that exist right now (since auto columns may vary).
            var cols = desired
                .Where(n => dgvLines.Columns.Contains(n))
                .Select(n => dgvLines.Columns[n])
                .Where(c => c != null)
                .ToList();

            // Assign DisplayIndex sequentially (0..N-1) safely.
            for (int i = 0; i < cols.Count; i++)
                cols[i]!.DisplayIndex = i;
        }

        private static void RemoveColumnIfExists(DataGridView grid, string columnName)
        {
            // If grid or Columns is not available, exit.
            if (grid?.Columns == null) return;

            // If column exists, remove it.
            if (grid.Columns.Contains(columnName))
                grid.Columns.Remove(columnName);
        }

        private void BuildSummaryTab()
        {
            // Main layout: two columns (NRC left, DID right) and top row for summary labels.
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(10)
            };

            // Each column gets half the width.
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // Row 0 is fixed for labels; row 1 takes remaining space.
            layout.RowStyles.Clear();
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Clear previous controls and add fresh layout.
            tabSummary.Controls.Clear();
            tabSummary.Controls.Add(layout);

            // Top label bar.
            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            // Summary labels.
            lblSummaryIso = new Label { AutoSize = true, Text = "ISO Lines: 0", Padding = new Padding(0, 10, 20, 0) };
            lblSummaryUds = new Label { AutoSize = true, Text = "UDS Findings: 0", Padding = new Padding(0, 10, 20, 0) };
            lblSummaryUnknown = new Label { AutoSize = true, Text = "Unknown: 0", Padding = new Padding(0, 10, 20, 0) };

            // Add summary labels.
            top.Controls.Add(lblSummaryIso);
            top.Controls.Add(lblSummaryUds);
            top.Controls.Add(lblSummaryUnknown);

            // Add top bar spanning both columns.
            layout.Controls.Add(top, 0, 0);
            layout.SetColumnSpan(top, 2);

            // NRC list view definition.
            lvNrc = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };

            // NRC columns.
            lvNrc.Columns.Add("NRC", 90);
            lvNrc.Columns.Add("Meaning", 260);
            lvNrc.Columns.Add("Count", 80);

            // DID list view definition.
            lvDid = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };

            // DID columns.
            lvDid.Columns.Add("DID", 90);
            lvDid.Columns.Add("Name", 260);
            lvDid.Columns.Add("Count", 80);

            // Group box around NRC list.
            var gbNrc = new GroupBox { Text = "NRCs", Dock = DockStyle.Fill };
            gbNrc.Controls.Add(lvNrc);

            // Group box around DID list.
            var gbDid = new GroupBox { Text = "DIDs", Dock = DockStyle.Fill };
            gbDid.Controls.Add(lvDid);

            // Place group boxes in layout.
            layout.Controls.Add(gbNrc, 0, 1);
            layout.Controls.Add(gbDid, 1, 1);
        }

        private void WireEvents()
        {
            // Hook button clicks.
            btnLoadFile.Click += BtnLoadFile_Click;
            btnLoadSample.Click += BtnLoadSample_Click;
            btnPaste.Click += BtnPaste_Click;
            btnClear.Click += BtnClear_Click;

            // Hook grid events.
            dgvLines.RowPrePaint += DgvLines_RowPrePaint;                 // color coding by content
            dgvLines.SelectionChanged += DgvLines_SelectionChanged;       // update raw/decoded panes

            // Hook filter events so UI updates instantly as user types/changes.
            txtSearch.TextChanged += FilterControls_Changed;
            cboTypeFilter.SelectedIndexChanged += FilterControls_Changed;
            chkUdsOnly.CheckedChanged += FilterControls_Changed;
            chkMatchAllTerms.CheckedChanged += FilterControls_Changed;

            // Hook summary interactions (click NRC/DID to jump to filtered view).
            lvNrc.ItemActivate += LvNrc_ItemActivate;
            lvDid.ItemActivate += LvDid_ItemActivate;

            // Hook session management.
            btnAddSession.Click += BtnAddSession_Click;
            btnCloseSession.Click += BtnCloseSession_Click;
            lstSessions.SelectedIndexChanged += LstSessions_SelectedIndexChanged;
        }

        // ================================================================
        // SESSIONS
        // ================================================================

        private void CreateNewSession(bool makeActive)
        {
            // Enforce max session count.
            if (_sessions.Count >= MaxSessions)
            {
                MessageBox.Show($"Max sessions reached ({MaxSessions}).", "Sessions",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Create a new session object (instance).
            var s = new LogSession
            {
                // Give the session a default timestamped name.
                Name = $"Session {_sessions.Count + 1} - {DateTime.Now:HH:mm:ss}"
            };

            // Add session to the binding list.
            _sessions.Add(s);

            // Bind session list if not already bound.
            if (lstSessions.DataSource == null)
                lstSessions.DataSource = _sessions;

            // Optionally make it the active session.
            if (makeActive)
            {
                // Select it visually.
                lstSessions.SelectedItem = s;

                // Switch active session backing data.
                SetActiveSession(s);
            }
        }

        private void SetActiveSession(LogSession? session)
        {
            // Store the new active session.
            _activeSession = session;

            // If no session is active, clear data safely.
            if (_activeSession == null)
            {
                _allLogLines = new BindingList<LogLine>();
                _filteredLogLines = new BindingList<LogLine>();
                dgvLines.DataSource = _filteredLogLines;
                UpdateStatusBar();
                UpdateFindingsSummary();
                return;
            }

            // Point our form-level bindings at the session’s lists.
            _allLogLines = _activeSession.AllLines;
            _filteredLogLines = _activeSession.FilteredLines;

            // Rebind grid to the new filtered list.
            dgvLines.DataSource = _filteredLogLines;

            // Reapply filters for this session.
            ApplyFilters();

            // Update the UI counts.
            UpdateStatusBar();

            // Update summary tab counts.
            UpdateFindingsSummary();
        }

        private void BtnAddSession_Click(object? sender, EventArgs e)
            => CreateNewSession(makeActive: true); // Create session and immediately activate it.

        private void BtnCloseSession_Click(object? sender, EventArgs e)
        {
            // If no session is active, nothing to close.
            if (_activeSession == null) return;

            // Remember index so we can select a reasonable next session.
            int idx = lstSessions.SelectedIndex;

            // Store reference to close.
            var toClose = _activeSession;

            // Remove from session list.
            _sessions.Remove(toClose);

            // If nothing remains, create a fresh session.
            if (_sessions.Count == 0)
            {
                CreateNewSession(makeActive: true);
                return;
            }

            // Choose next index (same position if possible, otherwise last).
            int nextIdx = Math.Min(idx, _sessions.Count - 1);

            // Select next session.
            lstSessions.SelectedIndex = nextIdx;

            // Activate the selected session.
            SetActiveSession(lstSessions.SelectedItem as LogSession);
        }

        private void LstSessions_SelectedIndexChanged(object? sender, EventArgs e)
            => SetActiveSession(lstSessions.SelectedItem as LogSession); // Activate whichever session user clicks.

        // ================================================================
        // GRID / FILTERS / DECODING
        // ================================================================

        private void ConfigureDataGridColumns()
        {
            // Disable autosizing so our widths are consistent/predictable.
            dgvLines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // Let user resize columns.
            dgvLines.AllowUserToResizeColumns = true;

            // Let user reorder columns (even though we set a default order).
            dgvLines.AllowUserToOrderColumns = true;
        }

        /// <summary>
        /// Applies consistent column widths and readable headers after binding.
        /// IMPORTANT: Does NOT set DisplayIndex (ordering is handled elsewhere).
        /// </summary>
        private void ApplyColumnSizing()
        {
            // Defensive checks.
            if (dgvLines == null || dgvLines.IsDisposed) return;
            if (dgvLines.Columns == null || dgvLines.Columns.Count == 0) return;

            // Ensure we do not autosize unpredictably.
            dgvLines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // Helper that sets width + header if the column exists.
            void SetCol(string name, int width, string? header = null)
            {
                // If the named column isn’t present, skip.
                if (!dgvLines.Columns.Contains(name)) return;

                // Get the column object.
                var col = dgvLines.Columns[name];

                // If the column object is missing, skip.
                if (col == null) return;

                // Ensure column is visible.
                col.Visible = true;

                // Set fixed width.
                col.Width = width;

                // Ensure fixed mode.
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

                // Allow resizing.
                col.Resizable = DataGridViewTriState.True;

                // Apply friendly header text if provided.
                if (!string.IsNullOrWhiteSpace(header))
                    col.HeaderText = header;
            }

            // Size and label the columns you care about.
            SetCol("LineNumber", 80, "Line");
            SetCol("Raw", 360, "Raw");
            SetCol("Type", 95, "Type");
            SetCol("Summary", 320, "Report Summary");
            SetCol("Details", 520, "Technical Breakdown");
            SetCol("CanNode", 170, "Node");

            // Prevent wrapping in Details column to keep rows compact.
            if (dgvLines.Columns.Contains("Details"))
            {
                var c = dgvLines.Columns["Details"];
                if (c != null) c.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            }
        }

        private void DgvLines_RowPrePaint(object? sender, DataGridViewRowPrePaintEventArgs e)
        {
            // Ignore invalid row index.
            if (e.RowIndex < 0 || e.RowIndex >= dgvLines.Rows.Count) return;

            // Get the row.
            var row = dgvLines.Rows[e.RowIndex];

            // Attempt to get the bound LogLine object.
            var logLine = row.DataBoundItem as LogLine;

            // If something is wrong, default to white.
            if (logLine == null)
            {
                row.DefaultCellStyle.BackColor = Color.White;
                return;
            }

            // Only color-code ISO15765 lines (UDS over CAN / ISO-TP).
            if (logLine.Type == LineType.Iso15765)
            {
                // Negative response highlighting.
                if (logLine.Details?.Contains("Negative Response", StringComparison.OrdinalIgnoreCase) == true ||
                    logLine.Details?.Contains("0x7F", StringComparison.OrdinalIgnoreCase) == true)
                {
                    row.DefaultCellStyle.BackColor = Color.LightSalmon;
                    return;
                }

                // Request highlighting.
                if (logLine.Details?.Contains("UDS Request", StringComparison.OrdinalIgnoreCase) == true)
                {
                    row.DefaultCellStyle.BackColor = Color.LightSkyBlue;
                    return;
                }

                // Positive response highlighting.
                if (logLine.Details?.Contains("UDS Positive Response", StringComparison.OrdinalIgnoreCase) == true ||
                    logLine.Details?.Contains("(0x62)", StringComparison.OrdinalIgnoreCase) == true)
                {
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                    return;
                }
            }

            // Default background.
            row.DefaultCellStyle.BackColor = Color.White;
        }

        private void BtnLoadFile_Click(object? sender, EventArgs e)
        {
            // Configure open file dialog.
            using var ofd = new OpenFileDialog
            {
                Title = "Select Log File",
                Filter = "Text Files (*.txt)|*.txt|Log Files (*.log)|*.log|All Files (*.*)|*.*"
            };

            // If user cancels, exit.
            if (ofd.ShowDialog() != DialogResult.OK) return;

            try
            {
                // Read all lines from selected file.
                var lines = File.ReadAllLines(ofd.FileName);

                // Use filename as session name for clarity.
                string fileName = Path.GetFileName(ofd.FileName);

                // Load and process these lines.
                LoadLines(lines, sessionName: fileName);
            }
            catch (Exception ex)
            {
                // Show user-friendly error.
                MessageBox.Show($"Error loading file: {ex.Message}", "Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnLoadSample_Click(object? sender, EventArgs e)
        {
            // Hard-coded sample lines that exercise ISO/UDS, ASCII, and XML.
            string[] sampleLines =
            {
                "2025-10-21T10:23:45.123 ISO15765 RX <- [00,00,07,D8,7F,22,78]",
                "2025-10-21T10:23:45.200 ISO15765 TX -> [00,00,07,D0,62,80,6A,41,42,43,44]",
                "2025-10-21T10:23:47.000 ISO15765 TX -> [00,00,07,D0,22,F1,88]",
                "2025-10-21T10:23:47.100 ISO15765 RX <- [00,00,07,D8,62,F1,88,56,45,52,53,49,4F,4E,31]",
                "DEBUG: Starting diagnostic session",
                "<ns3:didValue didValue=\"F188\" type=\"Strategy\"><ns3:Response>4D59535452415445475931</ns3:Response></ns3:didValue>",
            };

            // Load sample lines into active session.
            LoadLines(sampleLines, sessionName: "Sample");
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            // Clear active full list.
            _allLogLines.Clear();

            // Clear active filtered list.
            _filteredLogLines.Clear();

            // Clear raw text pane.
            rtbRaw.Clear();

            // Clear decoded text pane.
            rtbDecoded.Clear();

            // Refresh counts.
            UpdateStatusBar();

            // Refresh summary tab.
            UpdateFindingsSummary();
        }

        private void BtnPaste_Click(object? sender, EventArgs e)
        {
            // If clipboard has no text, warn the user.
            if (!Clipboard.ContainsText())
            {
                MessageBox.Show("Clipboard does not contain text.", "Paste",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Get clipboard text.
            var text = Clipboard.GetText();

            // If clipboard text is blank, warn user.
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("Clipboard text is empty.", "Paste",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Split clipboard into lines using common newline formats.
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Load the pasted lines.
            LoadLines(lines, sessionName: "Pasted");
        }

        private void DgvLines_SelectionChanged(object? sender, EventArgs e)
        {
            // If no row is selected, do nothing.
            if (dgvLines.SelectedRows.Count <= 0) return;

            // Get selected row.
            var row = dgvLines.SelectedRows[0];

            // Ensure the bound item is a LogLine.
            if (row.DataBoundItem is not LogLine logLine) return;

            // Display raw text in raw pane (defensive null handling).
            rtbRaw.Text = logLine.Raw ?? string.Empty;

            // Display decoded details in decoded pane (defensive null handling).
            rtbDecoded.Text = logLine.Details ?? string.Empty;
        }

        private void FilterControls_Changed(object? sender, EventArgs e)
            => ApplyFilters(); // Whenever a filter changes, recalculate filtered list.

        private static List<string> TokenizeSearch(string input)
        {
            // Tokens list we will return.
            var tokens = new List<string>();

            // If input is empty, return no tokens.
            if (string.IsNullOrWhiteSpace(input)) return tokens;

            // Track whether we are inside quotes.
            bool inQuotes = false;

            // Build current token progressively.
            var current = new System.Text.StringBuilder();

            // Walk every character.
            foreach (char c in input)
            {
                // Toggle quoting state on double-quote.
                if (c == '"') { inQuotes = !inQuotes; continue; }

                // If we see a space outside quotes, finalize the current token.
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
                    // Otherwise add character to current token.
                    current.Append(c);
                }
            }

            // Add last token if any.
            if (current.Length > 0) tokens.Add(current.ToString().Trim());

            // Remove blanks and return clean token list.
            return tokens.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        }

        private void LoadLines(string[] lines, string? sessionName = null)
        {
            // If no active session, create one so data has a home.
            if (_activeSession == null)
                CreateNewSession(makeActive: true);

            // If sessionName is provided, rename the session for clarity.
            if (_activeSession != null && !string.IsNullOrWhiteSpace(sessionName))
            {
                _activeSession.Name = sessionName;
                RefreshSessionListUi();
            }

            try
            {
                // Clear existing data for current session.
                _allLogLines.Clear();
                _filteredLogLines.Clear();

                // Loop through each raw input line.
                for (int i = 0; i < lines.Length; i++)
                {
                    // Pull the raw line string.
                    string rawLine = lines[i];

                    // Convert to 1-based line numbering for display.
                    int lineNumber = i + 1;

                    try
                    {
                        // Classify line into correct derived class (inheritance).
                        var logLine = LineClassifier.Classify(lineNumber, rawLine);

                        // Decode/parse the line (fills Summary/Details, etc.).
                        logLine.ParseAndDecode();

                        // Add to all-lines collection.
                        _allLogLines.Add(logLine);
                    }
                    catch (Exception ex)
                    {
                        // If classification/decode fails, store as UnknownLine with error reason.
                        var errorLine = new UnknownLine(lineNumber, rawLine, $"Error: {ex.Message}");

                        // Still call ParseAndDecode so details are consistent.
                        errorLine.ParseAndDecode();

                        // Add error line as part of dataset (keeps indexing consistent).
                        _allLogLines.Add(errorLine);
                    }
                }

                // Apply current UI filters to produce the filtered list.
                ApplyFilters();

                // Build ISO-TP PDUs from all lines (multi-frame reassembly).
                _pdus = IsoTpReassembler.Build(_allLogLines);

                // Build UDS transactions from PDUs (request/response pairing).
                _transactions = UdsConversationBuilder.Build(_pdus);

                // Update status counts.
                UpdateStatusBar();

                // Update summary tab.
                UpdateFindingsSummary();
            }
            catch (Exception ex)
            {
                // If anything fails at a higher level, show error dialog.
                MessageBox.Show($"Error loading lines: {ex.Message}", "Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyFilters()
        {
            // Get search string from UI (defensive null handling).
            string searchText = (txtSearch.Text ?? string.Empty).Trim();

            // Tokenize (supports quoted phrases).
            var tokens = TokenizeSearch(searchText);

            // Determine whether we require ALL tokens or just ANY token.
            bool matchAll = chkMatchAllTerms.Checked;

            // Get type filter selection (or “All”).
            string typeFilter = cboTypeFilter.SelectedItem?.ToString() ?? "All";

            // Determine whether to enforce UDS-only filtering.
            bool udsOnly = chkUdsOnly.Checked;

            // Clear current filtered results.
            _filteredLogLines.Clear();

            // Walk through all lines and decide whether to include each one.
            foreach (var logLine in _allLogLines)
            {
                // Create a combined searchable field from multiple properties.
                string combined =
                    (logLine.Raw ?? "") + " " +
                    (logLine.Summary ?? "") + " " +
                    (logLine.Details ?? "");

                // Normalize searchable field to reduce punctuation mismatch.
                string field = NormalizeForSearch(combined);

                // Default: line matches search if no tokens are provided.
                bool matchesSearch = true;

                // If there are tokens, enforce match logic.
                if (tokens.Count > 0)
                {
                    matchesSearch = matchAll
                        ? tokens.All(t => field.Contains(NormalizeForSearch(t)))
                        : tokens.Any(t => field.Contains(NormalizeForSearch(t)));
                }

                // Type filter: pass if “All”, otherwise exact type string match.
                bool matchesType = typeFilter == "All" || logLine.Type.ToString() == typeFilter;

                // UDS-only: pass if checkbox not checked, otherwise require “UDS” in details.
                bool matchesUds =
                    !udsOnly ||
                    (logLine.Details?.Contains("UDS", StringComparison.OrdinalIgnoreCase) == true);

                // If all criteria pass, include the line in the filtered list.
                if (matchesSearch && matchesType && matchesUds)
                    _filteredLogLines.Add(logLine);
            }

            // Update summary after filter changes.
            UpdateFindingsSummary();
        }

        private void UpdateStatusBar()
        {
            // Total line count.
            int total = _allLogLines.Count;

            // Count ISO15765 lines.
            int iso = _allLogLines.Count(l => l.Type == LineType.Iso15765);

            // Count XML lines.
            int xml = _allLogLines.Count(l => l.Type == LineType.Xml);

            // Count Unknown lines.
            int unk = _allLogLines.Count(l => l.Type == LineType.Unknown);

            // Update label text.
            lblStatusTotal.Text = $"Total: {total}";
            lblStatusIso.Text = $"ISO: {iso}";
            lblStatusXml.Text = $"XML: {xml}";
            lblStatusUnknown.Text = $"Unknown: {unk}";
        }

        private void UpdateFindingsSummary()
        {
            // Build a summary object from the currently filtered lines.
            var summary = FindingsAggregator.Build(_filteredLogLines);

            // Update summary label values.
            lblSummaryIso.Text = $"ISO Lines: {summary.IsoLines}";
            lblSummaryUds.Text = $"UDS Findings: {summary.UdsFindingLines}";
            lblSummaryUnknown.Text = $"Unknown: {summary.UnknownLines}";

            // Populate NRC list view.
            PopulateNrcListView(summary.NrcCounts);

            // Populate DID list view.
            PopulateDidListView(summary.DidCounts);

            // Append reconstructed conversation count for extra insight.
            lblSummaryUds.Text += $" | Conversations: {_transactions?.Count ?? 0}";
        }

        private void PopulateNrcListView(Dictionary<byte, int> nrcCounts)
        {
            // Prevent flicker while updating.
            lvNrc.BeginUpdate();

            // Clear existing items.
            lvNrc.Items.Clear();

            // Sort NRCs by count desc, then by code asc.
            foreach (var kvp in nrcCounts.OrderByDescending(k => k.Value).ThenBy(k => k.Key))
            {
                // NRC code.
                byte nrc = kvp.Key;

                // Occurrence count.
                int count = kvp.Value;

                // Lookup NRC meaning or fallback.
                string meaning =
                    UdsTables.NrcMeaning.TryGetValue(nrc, out var m)
                        ? m
                        : "UnknownNRC";

                // Create list item with formatted NRC hex.
                var item = new ListViewItem($"0x{nrc:X2}");

                // Add meaning column.
                item.SubItems.Add(meaning);

                // Add count column.
                item.SubItems.Add(count.ToString());

                // Store NRC in Tag for click behavior.
                item.Tag = nrc;

                // Add item to list view.
                lvNrc.Items.Add(item);
            }

            // Resume drawing.
            lvNrc.EndUpdate();
        }

        private void PopulateDidListView(Dictionary<ushort, int> didCounts)
        {
            // Prevent flicker while updating.
            lvDid.BeginUpdate();

            // Clear existing items.
            lvDid.Items.Clear();

            // Sort DIDs by count desc, then by DID asc.
            foreach (var kvp in didCounts.OrderByDescending(k => k.Value).ThenBy(k => k.Key))
            {
                // DID code.
                ushort did = kvp.Key;

                // Occurrence count.
                int count = kvp.Value;

                // Resolve DID name/description.
                string name = UdsTables.DescribeDid(did);

                // Create list item with formatted DID hex.
                var item = new ListViewItem($"0x{did:X4}");

                // Add DID name column.
                item.SubItems.Add(name);

                // Add count column.
                item.SubItems.Add(count.ToString());

                // Store DID in Tag for click behavior.
                item.Tag = did;

                // Add item to list.
                lvDid.Items.Add(item);
            }

            // Resume drawing.
            lvDid.EndUpdate();
        }

        private void LvNrc_ItemActivate(object? sender, EventArgs e)
        {
            // If nothing selected, do nothing.
            if (lvNrc.SelectedItems.Count <= 0) return;

            // Pull NRC code from Tag.
            byte nrc = (byte)(lvNrc.SelectedItems[0].Tag ?? (byte)0);

            // Set search box to NRC string (triggers filtering through TextChanged handler).
            txtSearch.Text = $"0x{nrc:X2}";

            // Bring user back to decoded tab to see matching lines.
            tabControl.SelectedTab = tabDecoded;
        }

        private void LvDid_ItemActivate(object? sender, EventArgs e)
        {
            // If nothing selected, do nothing.
            if (lvDid.SelectedItems.Count <= 0) return;

            // Pull DID code from Tag.
            ushort did = (ushort)(lvDid.SelectedItems[0].Tag ?? (ushort)0);

            // Set search box to DID string.
            txtSearch.Text = $"0x{did:X4}";

            // Switch to decoded tab.
            tabControl.SelectedTab = tabDecoded;
        }

        // ================================================================
        // SPLITTER SAFETY (PREVENT “JACKED UP” UI / CRASHES)
        // ================================================================

        private static bool TryClampSplitter(SplitContainer s)
        {
            // If the splitter is invalid, abort safely.
            if (s == null || s.IsDisposed) return false;

            // Determine the usable size depending on orientation.
            int size = (s.Orientation == Orientation.Vertical) ? s.Width : s.Height;

            // If control has no size yet, skip.
            if (size <= 0) return false;

            // Minimum size for panel1.
            int min = s.Panel1MinSize;

            // Maximum allowable splitter distance based on panel2 minimum and splitter width.
            int max = size - s.SplitterWidth - s.Panel2MinSize;

            // If max is less than min, we can’t clamp safely.
            if (max < min) return false;

            // The requested splitter distance.
            int desired = s.SplitterDistance;

            // Clamp desired into [min, max].
            int clamped = Math.Max(min, Math.Min(desired, max));

            // If clamped differs, apply it.
            if (clamped != desired)
                s.SplitterDistance = clamped;

            // Success.
            return true;
        }

        protected override void OnShown(EventArgs e)
        {
            // Call base behavior.
            base.OnShown(e);

            // Delay layout tuning until after the form is fully shown.
            BeginInvoke(new Action(() =>
            {
                // Minimum widths for main split.
                splitMain.Panel1MinSize = 120;
                splitMain.Panel2MinSize = 500;

                // Minimum heights for decoded root split.
                decodedRootSplit.Panel1MinSize = 70;
                decodedRootSplit.Panel2MinSize = 500;

                // Minimum heights for grid/details split.
                decodedBottomSplit.Panel1MinSize = 250;
                decodedBottomSplit.Panel2MinSize = 200;

                // Minimum widths for raw/decoded split.
                rawDecodedSplit.Panel1MinSize = 200;
                rawDecodedSplit.Panel2MinSize = 200;

                // Set initial splitter distances.
                splitMain.SplitterDistance = 140;
                decodedRootSplit.SplitterDistance = 60;
                decodedBottomSplit.SplitterDistance = 360;
                rawDecodedSplit.SplitterDistance = Math.Max(200, rawDecodedSplit.Width / 2);

                // Keep left panel fixed so it doesn’t stretch awkwardly.
                splitMain.FixedPanel = FixedPanel.Panel1;

                // Clamp everything to prevent invalid distances.
                TryClampSplitter(splitMain);
                TryClampSplitter(decodedRootSplit);
                TryClampSplitter(decodedBottomSplit);
                TryClampSplitter(rawDecodedSplit);
            }));
        }

        protected override void OnResize(EventArgs e)
        {
            // Call base resize logic.
            base.OnResize(e);

            // Clamp splitter distances during resize to prevent out-of-range exceptions.
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
            // If string is empty, return empty.
            if (string.IsNullOrEmpty(s)) return string.Empty;

            // Lowercase to make search case-insensitive.
            var chars = s.ToLowerInvariant().ToCharArray();

            // Replace punctuation separators with spaces to reduce mismatch.
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (c is ',' or '[' or ']' or '(' or ')' or '{' or '}' or ':' or ';' or '\t')
                    chars[i] = ' ';
            }

            // Collapse repeated spaces into single spaces.
            return string.Join(" ", new string(chars)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private void RefreshSessionListUi()
        {
            // If listbox is not ready, exit.
            if (lstSessions == null) return;

            // If listbox has no data source, nothing to refresh.
            if (lstSessions.DataSource == null) return;

            // Refresh binding context so list updates text (like session rename).
            if (BindingContext != null)
            {
                if (BindingContext[lstSessions.DataSource] is CurrencyManager cm)
                    cm.Refresh();
            }

            // Force redraw.
            lstSessions.Invalidate();
            lstSessions.Update();
        }
    }
}