// ================================================================
// File: Form1.cs
// Project: AutoDecoder.Gui
// Course: MS539 Programming Concepts (Graduate)
// Assignment: 5.1 (OOP: classes/objects/inheritance/encapsulation + GUI)
// Author: Harold Watkins
//
// HOW TO READ THIS FILE (5th-grade friendly):
// - Think of this app like a “log detective notebook”.
// - A “log file” is a long list of sentences a car/computer wrote down.
// - This window lets you:
//   1) load those sentences,
//   2) put each sentence into the right “bucket” (type),
//   3) show them in a table,
//   4) let you search/filter,
//   5) show a simple summary of what was found.
//
// HOW THIS MATCHES ASSIGNMENT 5.1:
// - GUI: Buttons, list boxes, tabs, grid, text boxes ✅
// - Objects/Classes: LogSession, LogLine, IsoTpPdu, UdsTransaction are objects ✅
// - Inheritance: LogLine has “child classes” like UnknownLine (and others in Models) ✅
// - Encapsulation: Data is stored inside classes (LogSession holds lists; LogLine holds info) ✅
// - Reusability/Scaling: Multiple sessions up to MaxSessions ✅
// ================================================================

#nullable enable // Helps the compiler warn us if something might be null (missing).

// ----------------------------
// Project references (DLLs)
// ----------------------------

// Think of these like “toolboxes” we imported.
// Each toolbox contains classes (building blocks) we can use.
using AutoDecoder.Models;                 // LogLine, UnknownLine, LogSession (our main “data objects”)
using AutoDecoder.Protocols.Classifiers;  // LineClassifier decides what type a line is
using AutoDecoder.Protocols.Utilities;    // FindingsAggregator, UdsTables (lookup tables, summaries)
using AutoDecoder.Protocols.Conversations;// Builds multi-line conversations (ISO-TP + UDS pairing)

// ----------------------------
// Standard .NET namespaces
// ----------------------------
// These are built-in .NET toolboxes.
using System;                    // Basic stuff (DateTime, Exception, etc.)
using System.Collections.Generic; // Lists and dictionaries (collections)
using System.ComponentModel;      // BindingList (auto-updates UI when list changes)
using System.Drawing;             // Fonts and colors
using System.IO;                  // Reading files
using System.Linq;                // Easy counting/filtering tools (Where, Count, Any, All)
using System.Windows.Forms;       // Windows UI controls (buttons, grids, tabs)

namespace AutoDecoder.Gui
{
    /// <summary>
    /// Form1 = the main window.
    ///
  
    /// This is the “main screen” of the app. Everything you see is built here.
    ///
    /// What it does:
    /// - Holds sessions (each session is one loaded log set)
    /// - Loads data (file/sample/paste)
    /// - Turns raw text into LogLine objects (OOP)
    /// - Filters/searches
    /// - Shows summaries and “conversations”
    /// </summary>
    public class Form1 : Form
    {
        // ================================================================
        // PROTOCOL-AWARE DERIVED DATA (extra “smart” data we build)
        // ================================================================

        // ISO-TP PDUs are “combined messages”.
        // Some car messages arrive in multiple chunks. This list stores the rebuilt full messages.
        private List<IsoTpPdu> _pdus = new();

        // UdsTransaction is like a “question and answer” pair:
        // - Request: “Car, tell me X”
        // - Response: “Here is X”
        // This list stores those paired conversations.
        private List<UdsTransaction> _transactions = new();

        // ================================================================
        // SPLIT CONTAINERS FOR LAYOUT (they split the screen like a divider)
        // ================================================================

        // Big split for the Decoded tab: top controls vs bottom results.
        private SplitContainer decodedRootSplit = null!;

        // Split results area: table on top vs detail boxes on bottom.
        private SplitContainer decodedBottomSplit = null!;

        // Split detail area: left raw text vs right decoded explanation.
        private SplitContainer rawDecodedSplit = null!;

        // ================================================================
        // SESSIONS (MULTI-LOG SUPPORT)
        // ================================================================

        // We only allow up to 5 sessions so the app doesn’t get slow.
        private const int MaxSessions = 5;

        // BindingList is a “smart list”:
        // when we add/remove items, the UI updates automatically.
        private readonly BindingList<LogSession> _sessions = new();

        // The session currently selected in the left list.
        private LogSession? _activeSession;

        // ================================================================
        // DATA (LINES + FILTERED VIEW)
        // ================================================================

        // All log lines (the full set) for the active session.
        private BindingList<LogLine> _allLogLines = new();

        // Filtered lines (only the ones that match search/type/UDS filter).
        // This is what the table/grid shows.
        private BindingList<LogLine> _filteredLogLines = new();

        // ================================================================
        // UI CONTROLS (CODE-ONLY UI)
        // ================================================================

        // Main split: left side = sessions, right side = tabs.
        private SplitContainer splitMain = null!;

        // Left list showing sessions.
        private ListBox lstSessions = null!;

        // Buttons for sessions.
        private Button btnAddSession = null!;
        private Button btnCloseSession = null!;

        // Tabs on right side.
        private TabControl tabControl = null!;
        private TabPage tabDecoded = null!;
        private TabPage tabSummary = null!;

        // The table showing lines.
        private DataGridView dgvLines = null!;

        // Bottom text areas showing:
        // - raw text (exact line)
        // - decoded details (explanation)
        private RichTextBox rtbRaw = null!;
        private RichTextBox rtbDecoded = null!;

        // Search and filter tools.
        private TextBox txtSearch = null!;
        private ComboBox cboTypeFilter = null!;
        private CheckBox chkUdsOnly = null!;
        private CheckBox chkMatchAllTerms = null!;

        // Buttons for loading/clearing.
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

        // Summary lists:
        // - NRCs are “error codes”
        // - DIDs are “data IDs” (like information IDs)
        private ListView lvNrc = null!;
        private ListView lvDid = null!;

        // Safety flag so we don’t attach the same grid event multiple times.
        private bool _gridBindingHooked;

        // ================================================================
        // CONSTRUCTOR (runs when the window is created)
        // ================================================================

        public Form1()
        {
            // BuildUi = create the screen controls (buttons, tabs, grid, etc.)
            BuildUi();

            // WireEvents = connect button clicks and changes to code methods.
            WireEvents();

            // Start with one fresh session so the user can load logs immediately.
            CreateNewSession(makeActive: true);
        }

        // ================================================================
        // UI BUILD (creating the screen layout)
        // ================================================================

        private void BuildUi()
        {
            // Title text at the top of the window.
            Text = "AutoDecoder Workbench (Code-Only)";

            // Size of the window when it opens.
            Width = 1400;
            Height = 850;

            // Put the window in the middle of the screen.
            StartPosition = FormStartPosition.CenterScreen;

            // Create the main “left vs right” divider.
            splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,              // Fill the entire form
                Orientation = Orientation.Vertical  // Vertical means left/right split
            };

            // Add it to the window.
            Controls.Add(splitMain);

            // ----------------------------
            // Left panel: sessions + buttons
            // ----------------------------

            // A small top area that holds the session buttons.
            var leftTop = new Panel { Dock = DockStyle.Top, Height = 78 };

            // Button: create a new session (new “workspace”)
            btnAddSession = new Button { Text = "New Session", Dock = DockStyle.Top, Height = 36 };

            // Button: close current session
            btnCloseSession = new Button { Text = "Close Session", Dock = DockStyle.Top, Height = 36 };

            // Add buttons into the panel.
            // Note: DockStyle.Top stacks controls; last added appears on top.
            leftTop.Controls.Add(btnCloseSession);
            leftTop.Controls.Add(btnAddSession);

            // Session list box (shows names of sessions)
            lstSessions = new ListBox
            {
                Dock = DockStyle.Top,
                IntegralHeight = true
            };

            // We don’t want the session list to get too tall.
            // MaxSessions lines visible, plus a little padding.
            lstSessions.Height = (lstSessions.ItemHeight * MaxSessions) + 6;

            // DisplayMember tells the ListBox what property to show as text.
            // Each LogSession has a Name property.
            lstSessions.DisplayMember = "Name";

            // Fill panel (extra space for future left-side controls)
            var leftFill = new Panel { Dock = DockStyle.Fill };

            // IMPORTANT: Add Fill first, then Top-docked panels.
            splitMain.Panel1.Controls.Add(leftFill);
            splitMain.Panel1.Controls.Add(lstSessions);
            splitMain.Panel1.Controls.Add(leftTop);

            // ----------------------------
            // Right panel: tabs (Decoded + Summary)
            // ----------------------------

            tabControl = new TabControl { Dock = DockStyle.Fill };

            // Create the two tabs.
            tabDecoded = new TabPage("Decoded");
            tabSummary = new TabPage("Summary");

            // Add them into the tab control.
            tabControl.TabPages.Add(tabDecoded);
            tabControl.TabPages.Add(tabSummary);

            // Put the tabs on the right side of the main split.
            splitMain.Panel2.Controls.Add(tabControl);

            // Build the contents inside each tab.
            BuildDecodedTab();
            BuildSummaryTab();
        }

        private void BuildDecodedTab()
        {
            // This split makes:
            // - Top area: buttons + search/filter controls
            // - Bottom area: results table + details
            decodedRootSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal, // Top/bottom split
                FixedPanel = FixedPanel.Panel1       // Keep top fixed height
            };

            tabDecoded.Controls.Add(decodedRootSplit);

            // Top panel using a grid layout (table layout).
            // This helps align buttons and text boxes nicely.
            var top = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 10,
                RowCount = 2,
                Padding = new Padding(4),
                Margin = new Padding(0)
            };

            // Column widths:
            // Some are fixed pixels, one is “Percent 100” (stretchy search box).
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));   // LoadFile
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));   // LoadSample
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));   // Paste
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));   // Clear
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));   // Search label
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));   // Search textbox (grows)
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));   // Type label
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));  // Type dropdown
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));   // UDS only
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));  // Match all

            // Row heights:
            // Row 0: buttons/filters
            // Row 1: status info
            top.RowStyles.Clear();
            top.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            top.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));

            // Buttons that load/clear data.
            btnLoadFile = new Button { Text = "Load File", Dock = DockStyle.Fill, Margin = new Padding(2) };
            btnLoadSample = new Button { Text = "Load Sample", Dock = DockStyle.Fill, Margin = new Padding(2) };
            btnPaste = new Button { Text = "Paste", Dock = DockStyle.Fill, Margin = new Padding(2) };
            btnClear = new Button { Text = "Clear", Dock = DockStyle.Fill, Margin = new Padding(2) };

            // Labels for the filters.
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

            // Dropdown: user can pick which “kind of line” they want to see.
            cboTypeFilter = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList, // user can’t type random values
                Margin = new Padding(2, 5, 2, 2)
            };

            // Checkbox: show only UDS-related lines (a special diagnostic protocol).
            chkUdsOnly = new CheckBox { Text = "UDS only", Dock = DockStyle.Fill, Margin = new Padding(8, 6, 2, 2) };

            // Checkbox: “Match all terms” means:
            // - ON: every search word must appear
            // - OFF: at least one search word can appear
            chkMatchAllTerms = new CheckBox { Text = "Match all terms", Dock = DockStyle.Fill, Margin = new Padding(8, 6, 2, 2) };

            // Add dropdown options.
            cboTypeFilter.Items.Add("All");
            foreach (var v in Enum.GetValues(typeof(LineType)).Cast<LineType>())
                cboTypeFilter.Items.Add(v.ToString());
            cboTypeFilter.SelectedItem = "All";

            // Put controls into row 0.
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

            // Status row: simple counts shown in a line.
            var statusPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(2, 0, 2, 0)
            };

            // Labels show “how many lines of each type”.
            lblStatusTotal = new Label { AutoSize = true, Text = "Total: 0", Padding = new Padding(0, 4, 15, 0) };
            lblStatusIso = new Label { AutoSize = true, Text = "ISO: 0", Padding = new Padding(0, 4, 15, 0) };
            lblStatusXml = new Label { AutoSize = true, Text = "XML: 0", Padding = new Padding(0, 4, 15, 0) };
            lblStatusUnknown = new Label { AutoSize = true, Text = "Unknown: 0", Padding = new Padding(0, 4, 15, 0) };

            statusPanel.Controls.Add(lblStatusTotal);
            statusPanel.Controls.Add(lblStatusIso);
            statusPanel.Controls.Add(lblStatusXml);
            statusPanel.Controls.Add(lblStatusUnknown);

            // Put the status bar in row 1 spanning all columns.
            top.Controls.Add(statusPanel, 0, 1);
            top.SetColumnSpan(statusPanel, 10);

            // Put the top table into the top split panel.
            decodedRootSplit.Panel1.Controls.Add(top);

            // Bottom area: split between the grid and the raw/decoded panes.
            decodedBottomSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal // top = grid, bottom = details
            };
            decodedRootSplit.Panel2.Controls.Add(decodedBottomSplit);

            // Grid: shows each line as a row.
            dgvLines = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true, // user can’t edit the log data
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, // pick one row at a time
                AutoGenerateColumns = true // make columns based on LogLine properties
            };

            // Important: apply “after binding” tweaks only once.
            HookGridBindingOnce();

            decodedBottomSplit.Panel1.Controls.Add(dgvLines);

            // Bottom detail split: left raw text, right decoded explanation.
            rawDecodedSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical // left/right
            };
            decodedBottomSplit.Panel2.Controls.Add(rawDecodedSplit);

            // Raw line view uses a monospaced font so logs align nicely.
            rtbRaw = new RichTextBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 10) };

            // Decoded view also monospaced (hex and structured text looks better).
            rtbDecoded = new RichTextBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 10) };

            rawDecodedSplit.Panel1.Controls.Add(rtbRaw);
            rawDecodedSplit.Panel2.Controls.Add(rtbDecoded);

            // Bind grid to the filtered list.
            // This means: the grid only shows what passes the filters.
            dgvLines.DataSource = _filteredLogLines;

            // Set grid behaviors and consistent columns.
            ConfigureDataGridColumns();
        }

        private void HookGridBindingOnce()
        {
            // This method’s job:
            // Only attach the DataBindingComplete event one time.
            // If we attached it multiple times, it would run multiple times and cause bugs.

            if (_gridBindingHooked) return; // already done? stop.
            _gridBindingHooked = true;

            // DataBindingComplete happens after the grid auto-creates its columns.
            dgvLines.DataBindingComplete += (s, e) =>
            {
                // Remove columns we don’t want the user to see.
                // “Confidence” = internal score from classifier (not needed for your UI).
                // “CanId” = you chose to hide it (design decision).
                RemoveColumnIfExists(dgvLines, "Confidence");
                RemoveColumnIfExists(dgvLines, "CanId");

                // Hide timestamp columns (your UI choice).
                RemoveColumnIfExists(dgvLines, "Timestamp");
                RemoveColumnIfExists(dgvLines, "TimestampText");

                // Make sure “CanNode” is visible (node name is more helpful than raw CAN ID).
                if (dgvLines.Columns.Contains("CanNode"))
                {
                    var col = dgvLines.Columns["CanNode"];
                    if (col != null) col.Visible = true;
                }

                // Set widths and friendly headers.
                ApplyColumnSizing();

                // Set left-to-right order safely.
                ApplySafeDisplayOrder();
            };
        }

        private void ApplySafeDisplayOrder()
        {
            // Defensive checks (avoid crashes if grid isn’t ready).
            if (dgvLines == null || dgvLines.IsDisposed) return;
            if (dgvLines.Columns == null || dgvLines.Columns.Count == 0) return;

            // Desired order for columns (like arranging books on a shelf).
            string[] desired =
            {
                "LineNumber",
                "Raw",
                "Type",
                "Summary",
                "Details",
                "CanNode"
            };

            // Only keep the columns that actually exist.
            // (Sometimes different line types produce different auto-columns.)
            var cols = desired
                .Where(n => dgvLines.Columns.Contains(n))
                .Select(n => dgvLines.Columns[n])
                .Where(c => c != null)
                .ToList();

            // Set DisplayIndex = column position in the grid.
            for (int i = 0; i < cols.Count; i++)
                cols[i]!.DisplayIndex = i;
        }

        private static void RemoveColumnIfExists(DataGridView grid, string columnName)
        {
            // If the grid is missing or has no columns, do nothing.
            if (grid?.Columns == null) return;

            // If the named column exists, remove it.
            if (grid.Columns.Contains(columnName))
                grid.Columns.Remove(columnName);
        }

        private void BuildSummaryTab()
        {
            // This tab is like a “report card”:
            // It shows totals and two tables:
            // - NRC counts
            // - DID counts

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(10)
            };

            // Two equal columns.
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // Top row is fixed height, bottom row fills rest.
            layout.RowStyles.Clear();
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            tabSummary.Controls.Clear();
            tabSummary.Controls.Add(layout);

            // Top labels bar.
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

            top.Controls.Add(lblSummaryIso);
            top.Controls.Add(lblSummaryUds);
            top.Controls.Add(lblSummaryUnknown);

            layout.Controls.Add(top, 0, 0);
            layout.SetColumnSpan(top, 2);

            // NRC list view (table).
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

            // DID list view (table).
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

            // Put list views inside labeled boxes.
            var gbNrc = new GroupBox { Text = "NRCs", Dock = DockStyle.Fill };
            gbNrc.Controls.Add(lvNrc);

            var gbDid = new GroupBox { Text = "DIDs", Dock = DockStyle.Fill };
            gbDid.Controls.Add(lvDid);

            layout.Controls.Add(gbNrc, 0, 1);
            layout.Controls.Add(gbDid, 1, 1);
        }

        private void WireEvents()
        {
            // “Events” are like “when this happens, do that”.
            // Example: “When you click Load File, run BtnLoadFile_Click”.

            btnLoadFile.Click += BtnLoadFile_Click;
            btnLoadSample.Click += BtnLoadSample_Click;
            btnPaste.Click += BtnPaste_Click;
            btnClear.Click += BtnClear_Click;

            // Color rows based on what kind of UDS message it looks like.
            dgvLines.RowPrePaint += DgvLines_RowPrePaint;

            // When user clicks a row, show details in bottom text boxes.
            dgvLines.SelectionChanged += DgvLines_SelectionChanged;

            // Filters update live.
            txtSearch.TextChanged += FilterControls_Changed;
            cboTypeFilter.SelectedIndexChanged += FilterControls_Changed;
            chkUdsOnly.CheckedChanged += FilterControls_Changed;
            chkMatchAllTerms.CheckedChanged += FilterControls_Changed;

            // Clicking summary items jumps you back to matching lines.
            lvNrc.ItemActivate += LvNrc_ItemActivate;
            lvDid.ItemActivate += LvDid_ItemActivate;

            // Session controls.
            btnAddSession.Click += BtnAddSession_Click;
            btnCloseSession.Click += BtnCloseSession_Click;
            lstSessions.SelectedIndexChanged += LstSessions_SelectedIndexChanged;
        }

        // ================================================================
        // SESSIONS (like having multiple notebooks)
        // ================================================================

        private void CreateNewSession(bool makeActive)
        {
            // If we already have 5 sessions, do not allow more.
            if (_sessions.Count >= MaxSessions)
            {
                MessageBox.Show($"Max sessions reached ({MaxSessions}).", "Sessions",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Create a new LogSession object.
            // This is an “object” (a real instance) of the LogSession class.
            var s = new LogSession
            {
                // Give it a name including time, so each is unique.
                Name = $"Session {_sessions.Count + 1} - {DateTime.Now:HH:mm:ss}"
            };

            // Add it to our session list (BindingList updates UI automatically).
            _sessions.Add(s);

            // Connect listbox to sessions if it isn’t connected yet.
            if (lstSessions.DataSource == null)
                lstSessions.DataSource = _sessions;

            // If we want this to become active right away:
            if (makeActive)
            {
                // Highlight the session in the list.
                lstSessions.SelectedItem = s;

                // Actually switch the app’s data to this session.
                SetActiveSession(s);
            }
        }

        private void SetActiveSession(LogSession? session)
        {
            // Save which session is active.
            _activeSession = session;

            // If no session exists, clear things safely.
            if (_activeSession == null)
            {
                _allLogLines = new BindingList<LogLine>();
                _filteredLogLines = new BindingList<LogLine>();
                dgvLines.DataSource = _filteredLogLines;
                UpdateStatusBar();
                UpdateFindingsSummary();
                return;
            }

            // Here is encapsulation in action:
            // LogSession *owns* the lists internally.
            // We just point our UI to those lists.
            _allLogLines = _activeSession.AllLines;
            _filteredLogLines = _activeSession.FilteredLines;

            // Rebind grid to this session’s filtered list.
            dgvLines.DataSource = _filteredLogLines;

            // Apply whatever filters are currently selected.
            ApplyFilters();

            // Update counts and summary.
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

            // If no sessions left, make a new one automatically.
            if (_sessions.Count == 0)
            {
                CreateNewSession(makeActive: true);
                return;
            }

            // Select another session: same index if possible, else last.
            int nextIdx = Math.Min(idx, _sessions.Count - 1);
            lstSessions.SelectedIndex = nextIdx;

            SetActiveSession(lstSessions.SelectedItem as LogSession);
        }

        private void LstSessions_SelectedIndexChanged(object? sender, EventArgs e)
            => SetActiveSession(lstSessions.SelectedItem as LogSession);

        // ================================================================
        // GRID SETTINGS
        // ================================================================

        private void ConfigureDataGridColumns()
        {
            // Turn off auto-sizing so columns don’t jump around.
            dgvLines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // Let user resize and reorder columns if they want.
            dgvLines.AllowUserToResizeColumns = true;
            dgvLines.AllowUserToOrderColumns = true;
        }

        private void ApplyColumnSizing()
        {
            // Safety checks.
            if (dgvLines == null || dgvLines.IsDisposed) return;
            if (dgvLines.Columns == null || dgvLines.Columns.Count == 0) return;

            dgvLines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // Helper method: “If this column exists, set it up”.
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

            // Choose the columns that matter for your UI.
            // (Think of these as the “main story” columns.)
            SetCol("LineNumber", 80, "Line");
            SetCol("Raw", 360, "Raw");
            SetCol("Type", 95, "Type");
            SetCol("Summary", 320, "Report Summary");
            SetCol("Details", 520, "Technical Breakdown");
            SetCol("CanNode", 170, "Node");

            // Keep Details from wrapping (wrapping makes rows huge).
            if (dgvLines.Columns.Contains("Details"))
            {
                var c = dgvLines.Columns["Details"];
                if (c != null) c.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            }
        }

        private void DgvLines_RowPrePaint(object? sender, DataGridViewRowPrePaintEventArgs e)
        {
            // RowPrePaint runs before a row is drawn.
            // We use it to color rows like “traffic lights”:
            // - red-ish = negative response (problem)
            // - blue = request (question)
            // - green = positive response (answer)

            if (e.RowIndex < 0 || e.RowIndex >= dgvLines.Rows.Count) return;

            var row = dgvLines.Rows[e.RowIndex];
            var logLine = row.DataBoundItem as LogLine;

            if (logLine == null)
            {
                row.DefaultCellStyle.BackColor = Color.White;
                return;
            }

            // Only color ISO15765 lines because that’s the UDS-over-CAN style data.
            if (logLine.Type == LineType.Iso15765)
            {
                // Negative Response = usually contains “7F” in UDS.
                if (logLine.Details?.Contains("Negative Response", StringComparison.OrdinalIgnoreCase) == true ||
                    logLine.Details?.Contains("0x7F", StringComparison.OrdinalIgnoreCase) == true)
                {
                    row.DefaultCellStyle.BackColor = Color.LightSalmon;
                    return;
                }

                // Request lines.
                if (logLine.Details?.Contains("UDS Request", StringComparison.OrdinalIgnoreCase) == true)
                {
                    row.DefaultCellStyle.BackColor = Color.LightSkyBlue;
                    return;
                }

                // Positive Response lines (example: 0x62 is positive response to 0x22).
                if (logLine.Details?.Contains("UDS Positive Response", StringComparison.OrdinalIgnoreCase) == true ||
                    logLine.Details?.Contains("(0x62)", StringComparison.OrdinalIgnoreCase) == true)
                {
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                    return;
                }
            }

            row.DefaultCellStyle.BackColor = Color.White;
        }

        // ================================================================
        // LOADING DATA (file / sample / paste)
        // ================================================================

        private void BtnLoadFile_Click(object? sender, EventArgs e)
        {
            // OpenFileDialog = Windows file picker popup.
            using var ofd = new OpenFileDialog
            {
                Title = "Select Log File",
                Filter = "Text Files (*.txt)|*.txt|Log Files (*.log)|*.log|All Files (*.*)|*.*"
            };

            // If user cancels, stop.
            if (ofd.ShowDialog() != DialogResult.OK) return;

            try
            {
                // Read all lines from the file into a string array.
                var lines = File.ReadAllLines(ofd.FileName);

                // Use the file name as the session name.
                string fileName = Path.GetFileName(ofd.FileName);

                // Load and process these lines into the app.
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
            // These sample lines are like a “practice worksheet”
            // to prove the tool can decode different kinds of content.
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
            // Clear the lists and UI text boxes (like erasing the whiteboard).
            _allLogLines.Clear();
            _filteredLogLines.Clear();
            rtbRaw.Clear();
            rtbDecoded.Clear();

            UpdateStatusBar();
            UpdateFindingsSummary();
        }

        private void BtnPaste_Click(object? sender, EventArgs e)
        {
            // Clipboard = what you copied (Ctrl+C).
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

            // Split the text into individual lines.
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            LoadLines(lines, sessionName: "Pasted");
        }

        // ================================================================
        // WHEN USER SELECTS A ROW
        // ================================================================

        private void DgvLines_SelectionChanged(object? sender, EventArgs e)
        {
            // If nothing selected, do nothing.
            if (dgvLines.SelectedRows.Count <= 0) return;

            var row = dgvLines.SelectedRows[0];

            // Each row represents a LogLine object.
            if (row.DataBoundItem is not LogLine logLine) return;

            // Show the exact original text of that line.
            rtbRaw.Text = logLine.Raw ?? string.Empty;

            // Show the decoded “explanation” of that line.
            rtbDecoded.Text = logLine.Details ?? string.Empty;
        }

        // ================================================================
        // FILTERING (search + type + UDS-only)
        // ================================================================

        private void FilterControls_Changed(object? sender, EventArgs e)
            => ApplyFilters();

        private static List<string> TokenizeSearch(string input)
        {
            // This turns search text into “tokens” (little search pieces).
            // Example:
            //   hello world   -> ["hello", "world"]
            //   "hello world" -> ["hello world"] (quotes keep it together)

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

        // ================================================================
        // THE BIG WORK: Load lines -> classify -> decode -> show
        // ================================================================

        private void LoadLines(string[] lines, string? sessionName = null)
        {
            // If we somehow have no active session, create one.
            if (_activeSession == null)
                CreateNewSession(makeActive: true);

            // Rename session if a name was provided (like file name or “Sample”).
            if (_activeSession != null && !string.IsNullOrWhiteSpace(sessionName))
            {
                _activeSession.Name = sessionName;
                RefreshSessionListUi();
            }

            try
            {
                // Start fresh for this session’s data.
                _allLogLines.Clear();
                _filteredLogLines.Clear();

                // Loop through each line in the input.
                for (int i = 0; i < lines.Length; i++)
                {
                    string rawLine = lines[i];
                    int lineNumber = i + 1; // humans count from 1, not 0

                    try
                    {
                        // OOP + Inheritance moment:
                        // Classify returns a LogLine, but it might actually be a “child class”
                        // like Iso15765Line, XmlLine, AsciiLine, UnknownLine, etc.
                        var logLine = LineClassifier.Classify(lineNumber, rawLine);

                        // Each LogLine knows how to decode itself.
                        // This is encapsulation: the logic stays inside the object.
                        logLine.ParseAndDecode();

                        // Add to the master list.
                        _allLogLines.Add(logLine);
                    }
                    catch (Exception ex)
                    {
                        // If decoding fails, we still keep the line.
                        // We store it as UnknownLine with an error message.
                        var errorLine = new UnknownLine(lineNumber, rawLine, $"Error: {ex.Message}");
                        errorLine.ParseAndDecode();
                        _allLogLines.Add(errorLine);
                    }
                }

                // Now produce the filtered list based on current UI filters.
                ApplyFilters();

                // Build multi-frame messages (ISO-TP reassembly).
                _pdus = IsoTpReassembler.Build(_allLogLines);

                // Build request/response conversations (UDS transactions).
                _transactions = UdsConversationBuilder.Build(_pdus);

                // Update counts + summary view.
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
            // Read filter settings from UI.
            string searchText = (txtSearch.Text ?? string.Empty).Trim();
            var tokens = TokenizeSearch(searchText);

            bool matchAll = chkMatchAllTerms.Checked;
            string typeFilter = cboTypeFilter.SelectedItem?.ToString() ?? "All";
            bool udsOnly = chkUdsOnly.Checked;

            // Clear filtered results so we can rebuild it.
            _filteredLogLines.Clear();

            // Check every line to see if it should be kept.
            foreach (var logLine in _allLogLines)
            {
                // Combine fields into one searchable text.
                string combined =
                    (logLine.Raw ?? "") + " " +
                    (logLine.Summary ?? "") + " " +
                    (logLine.Details ?? "");

                // Normalize to make searching easier.
                string field = NormalizeForSearch(combined);

                // If no tokens, it matches by default.
                bool matchesSearch = true;

                // If tokens exist, enforce match logic.
                if (tokens.Count > 0)
                {
                    matchesSearch = matchAll
                        ? tokens.All(t => field.Contains(NormalizeForSearch(t)))
                        : tokens.Any(t => field.Contains(NormalizeForSearch(t)));
                }

                // Type filter:
                // - If “All”, everything passes
                // - Otherwise, only exact type matches
                bool matchesType = typeFilter == "All" || logLine.Type.ToString() == typeFilter;

                // UDS-only filter:
                // If checked, require “UDS” to show up in decoded details.
                bool matchesUds =
                    !udsOnly ||
                    (logLine.Details?.Contains("UDS", StringComparison.OrdinalIgnoreCase) == true);

                // If all conditions are true, keep it in filtered list.
                if (matchesSearch && matchesType && matchesUds)
                    _filteredLogLines.Add(logLine);
            }

            // Update summary based on what’s currently visible.
            UpdateFindingsSummary();
        }

        // ================================================================
        // STATUS + SUMMARY UPDATES
        // ================================================================

        private void UpdateStatusBar()
        {
            // Count totals in the FULL dataset (not filtered).
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
            // Build a summary from the FILTERED lines (what the user is looking at).
            var summary = FindingsAggregator.Build(_filteredLogLines);

            lblSummaryIso.Text = $"ISO Lines: {summary.IsoLines}";
            lblSummaryUds.Text = $"UDS Findings: {summary.UdsFindingLines}";
            lblSummaryUnknown.Text = $"Unknown: {summary.UnknownLines}";

            // Fill list views with counts.
            PopulateNrcListView(summary.NrcCounts);
            PopulateDidListView(summary.DidCounts);

            // Add how many conversations we reconstructed.
            lblSummaryUds.Text += $" | Conversations: {_transactions?.Count ?? 0}";
        }

        private void PopulateNrcListView(Dictionary<byte, int> nrcCounts)
        {
            // BeginUpdate prevents flicker (screen flashing) during changes.
            lvNrc.BeginUpdate();
            lvNrc.Items.Clear();

            foreach (var kvp in nrcCounts.OrderByDescending(k => k.Value).ThenBy(k => k.Key))
            {
                byte nrc = kvp.Key;
                int count = kvp.Value;

                // Lookup meaning for NRC code.
                string meaning =
                    UdsTables.NrcMeaning.TryGetValue(nrc, out var m)
                        ? m
                        : "UnknownNRC";

                var item = new ListViewItem($"0x{nrc:X2}");
                item.SubItems.Add(meaning);
                item.SubItems.Add(count.ToString());

                // Tag stores the code so clicking the row knows which NRC it represents.
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

        // Clicking an NRC row sets the search box to that NRC and jumps to Decoded tab.
        private void LvNrc_ItemActivate(object? sender, EventArgs e)
        {
            if (lvNrc.SelectedItems.Count <= 0) return;

            byte nrc = (byte)(lvNrc.SelectedItems[0].Tag ?? (byte)0);

            // Setting txtSearch triggers ApplyFilters automatically (TextChanged event).
            txtSearch.Text = $"0x{nrc:X2}";

            tabControl.SelectedTab = tabDecoded;
        }

        // Clicking a DID row does the same for DIDs.
        private void LvDid_ItemActivate(object? sender, EventArgs e)
        {
            if (lvDid.SelectedItems.Count <= 0) return;

            ushort did = (ushort)(lvDid.SelectedItems[0].Tag ?? (ushort)0);

            txtSearch.Text = $"0x{did:X4}";
            tabControl.SelectedTab = tabDecoded;
        }

        // ================================================================
        // SPLITTER SAFETY (prevents UI from breaking during resizing)
        // ================================================================

        private static bool TryClampSplitter(SplitContainer s)
        {
            // Clamping means: “keep the splitter inside safe limits”
            // so Windows doesn’t throw an error.

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

            // BeginInvoke delays until after the form is visible.
            // This avoids “size is 0” problems.
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

                // Set reasonable starter positions.
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

            // Every time window changes size, clamp splitters to stay safe.
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
            // This “cleans up” text so search matches better.
            // Example:
            // - “0x7F,[22]” becomes “0x7f  22”
            // This helps users find things even if punctuation is different.

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
            // If the session name changes, we want the ListBox to refresh text.
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