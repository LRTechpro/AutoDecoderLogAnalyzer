// ================================================================
// File: Form1.cs
// Project: AutoDecoder.Gui
// Course: MS539 Programming Concepts (Graduate)
// Assignment: 5.1 (OOP: classes/objects/inheritance/encapsulation + GUI)
// Author: Harold Watkins
//
// PURPOSE (plain language, but professional):
// - This app loads diagnostic/log text.
// - Each raw line is converted into an object (LogLine).
// - Some LogLine objects are actually derived types (inheritance).
// - Each object decodes itself (encapsulation).
// - The GUI displays, filters, and summarizes the object collection.
//
// ASSIGNMENT 5.1 CONCEPT EMPHASIS:
// - Classes: LogSession, LogLine, UnknownLine, IsoTpPdu, UdsTransaction, FindingsSummary
// - Objects: 10+ instances created when you load 10+ lines (one object per line)
// - Inheritance: LineClassifier returns LogLine, but runtime instances can be derived line types
// - Encapsulation: ParseAndDecode() keeps decoding behavior inside the object
// - GUI: multiple WinForms controls used together for an intuitive UX
// - Scaling: multiple sessions supported (bounded by MaxSessions)
//
// TEACHING NOTE (how to present this file):
// 1) Build UI (controls + layout)
// 2) Wire events (user actions -> handler methods)
// 3) Load raw lines -> create objects (OOP)
// 4) Decode inside objects (encapsulation)
// 5) Filter list -> bind to grid (GUI)
// 6) Summarize using DLL utilities (separation of concerns)
// ================================================================

#nullable enable // Enable nullable reference analysis for safer code.

using AutoDecoder.Models;                  // Domain models (LogLine, LogSession, UnknownLine).
using AutoDecoder.Protocols.Classifiers;   // Classifier that chooses which derived LogLine type to create.
using AutoDecoder.Protocols.Utilities;     // Summaries + lookup tables (FindingsAggregator, UdsTables).
using AutoDecoder.Protocols.Conversations; // ISO-TP reassembly + UDS conversation builder.

using System;                              // Basic .NET types (DateTime, Exception).
using System.Collections.Generic;          // List<T>, Dictionary<K,V>.
using System.ComponentModel;               // BindingList<T> for data binding to UI.
using System.Drawing;                      // Font, Color.
using System.IO;                           // File, Path.
using System.Linq;                         // LINQ operators (Count, Where, Any, All).
using System.Windows.Forms;                // WinForms UI types.

namespace AutoDecoder.Gui
{
    /// <summary>
    /// Form1 is the main WinForms window.
    ///
    /// High-level responsibility:
    /// - Orchestrate sessions, data loading, filtering, UI binding, and summaries.
    ///
    /// Important boundary:
    /// - Form1 coordinates UI + data flow.
    /// - Actual protocol decoding and lookup knowledge lives in the DLL projects.
    /// </summary>
    public class Form1 : Form
    {
        // ----------------------------
        // Derived protocol artifacts
        // ----------------------------

        // ISO-TP PDUs = multi-frame messages rebuilt from multiple log lines.
        // This is "derived data" because it is built AFTER we decode each LogLine.
        private List<IsoTpPdu> _pdus = new();

        // UDS Transactions = request/response pairs built from the PDUs.
        // This is also "derived data" computed AFTER loading/decoding lines.
        private List<UdsTransaction> _transactions = new();

        // ----------------------------
        // Layout splitters
        // ----------------------------

        // Decoded tab: top controls vs bottom content.
        private SplitContainer decodedRootSplit = null!;

        // Bottom: grid (top) vs details (bottom).
        private SplitContainer decodedBottomSplit = null!;

        // Details: raw text (left) vs decoded explanation (right).
        private SplitContainer rawDecodedSplit = null!;

        // ----------------------------
        // Session model (scaling)
        // ----------------------------

        // Guardrail: do not allow unlimited sessions; prevents performance issues.
        private const int MaxSessions = 5;

        // BindingList auto-notifies the UI when items are added/removed.
        private readonly BindingList<LogSession> _sessions = new();

        // The session currently selected by the user.
        private LogSession? _activeSession;

        // ----------------------------
        // Data collections (active session)
        // ----------------------------

        // All decoded LogLine objects for the active session.
        private BindingList<LogLine> _allLogLines = new();

        // Filtered list shown in the grid (the "view model" list).
        private BindingList<LogLine> _filteredLogLines = new();

        // ----------------------------
        // UI Controls
        // ----------------------------

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

        // Defensive: prevent attaching the same grid event multiple times.
        private bool _gridBindingHooked;

        // ================================================================
        // Constructor
        // ================================================================

        // GOAL: Create the main window in a valid ready-to-use state.
        // READS: (none) - this is object construction time.
        // CHANGES: Builds UI controls, wires events, creates the first LogSession.
        // OOP PROOF:
        // - Objects: Form1 itself is an object; UI controls are objects created inside it.
        // - Encapsulation: construction delegates work to private methods (BuildUi/WireEvents).
        // STEPS:
        // 1) Build the UI layout and controls.
        // 2) Wire user actions (events) to handler methods.
        // 3) Create the first session so the app can load data immediately.
        public Form1()
        {
            BuildUi();
            WireEvents();
            CreateNewSession(makeActive: true);

            try
            {
                var csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NodeAddress.csv");
                LoadNodeAddressCsv(csvPath);
               
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load NodeAddress.csv:\n" + ex.Message);
            }

        }
        private void LoadNodeAddressCsv(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("NodeAddress.csv not found.", path);

            var lines = File.ReadAllLines(path);

            foreach (var line in lines.Skip(1)) // skip header row
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',');

                if (parts.Length < 3)
                    continue;

                var canIdHex = parts[0].Trim();
                var abbrev = parts[1].Trim();
                var name = parts[2].Trim();

                if (int.TryParse(
                        canIdHex.Replace("0x", ""),
                        System.Globalization.NumberStyles.HexNumber,
                        null,
                        out int canId))
                {
                    AutoDecoder.Protocols.Utilities.ModuleAddressBook.AddOrUpdate(canId, abbrev, name);
                }
            }
        }

        // ================================================================
        // UI Build
        // ================================================================

        // GOAL: Build the main window layout (sessions on left, tabs on right).
        // READS: MaxSessions (to size the session list).
        // CHANGES: Creates and assigns UI control fields (splitMain, buttons, listbox, tabs).
        // OOP PROOF:
        // - Encapsulation: UI creation is isolated here instead of scattered in event handlers.
        // STEPS:
        // 1) Set window title/size.
        // 2) Create the main left/right split.
        // 3) Build left panel (session controls).
        // 4) Build right panel (tab control).
        // 5) Build each tab’s internal UI.
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

            // Visible size is limited so the left UI stays compact and predictable.
            lstSessions.Height = (lstSessions.ItemHeight * MaxSessions) + 6;

            // Data binding: ListBox shows LogSession.Name for each session object.
            lstSessions.DisplayMember = "Name";

            var leftFill = new Panel { Dock = DockStyle.Fill };

            splitMain.Panel1.Controls.Add(leftFill);
            splitMain.Panel1.Controls.Add(lstSessions);
            splitMain.Panel1.Controls.Add(leftTop);

            tabControl = new TabControl { Dock = DockStyle.Fill };
            tabDecoded = new TabPage("Decoded");
            tabSummary = new TabPage("Summary");
            var tabReference = new TabPage("Reference"); // NEW

            tabControl.TabPages.Add(tabDecoded);
            tabControl.TabPages.Add(tabSummary);
            tabControl.TabPages.Add(tabReference); // NEW

            splitMain.Panel2.Controls.Add(tabControl);

            BuildDecodedTab();
            BuildSummaryTab();
            BuildReferenceTab(tabReference); // NEW
        }

        // GOAL: Build the "Decoded" tab UI (filters, grid, and detail panes).
        // READS: LineType enum (to populate type filter dropdown).
        // CHANGES: Creates and assigns decoded tab UI fields (splitters, buttons, grid, textboxes).
        // OOP PROOF:
        // - Objects: creates many UI objects (buttons, grid, splitters).
        // - Encapsulation: separates UI construction from data/logic methods.
        // STEPS:
        // 1) Build top filter/control bar.
        // 2) Build grid area bound to _filteredLogLines.
        // 3) Build detail area (raw + decoded).
        // 4) Configure grid behavior and post-binding cleanup.
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
                Padding = new Padding(8),
                Margin = new Padding(0)
            };

            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

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

            // Important binding concept:
            // - _filteredLogLines is the "view list"
            // - the grid always shows exactly what is in that list
            dgvLines.DataSource = _filteredLogLines;

            ConfigureDataGridColumns();
        }
        private void BuildReferenceTab(TabPage tabReference)
        {
            // Top-level split: UDS/NRC on top, DIDs on bottom (or whatever you prefer)
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 260
            };

            // UDS + NRC side-by-side
            var topSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = tabReference.Width / 2
            };

            var grdUds = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };

            var grdNrc = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };

            // Bind UDS + NRC tables
            grdUds.DataSource = AutoDecoder.Protocols.Reference.UdsServiceTable.RequestSidToName
                .Select(kvp => new { SID = $"0x{kvp.Key:X2}", Name = kvp.Value })
                .ToList();

            grdNrc.DataSource = AutoDecoder.Protocols.Reference.NrcTable.CodeToMeaning
                .Select(kvp => new { NRC = $"0x{kvp.Key:X2}", Meaning = kvp.Value })
                .ToList();

            topSplit.Panel1.Controls.Add(grdUds);
            topSplit.Panel2.Controls.Add(grdNrc);

            // Bottom: placeholder for DID list (CSV-backed)
            var grdDid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };

            // If you implement FordDidTable as CSV-loaded dictionary, bind it similarly:
            // grdDid.DataSource = FordDidTable.All().Select(...).ToList();

            split.Panel1.Controls.Add(topSplit);
            split.Panel2.Controls.Add(grdDid);

            tabReference.Controls.Add(split);
        }

        // GOAL: Attach grid post-binding cleanup exactly one time.
        // READS: _gridBindingHooked, dgvLines.
        // CHANGES: Adds a DataBindingComplete handler to dgvLines.
        // OOP PROOF: Encapsulation at the UI layer—grid-specific behavior is isolated here.
        // STEPS:
        // 1) If already hooked, exit.
        // 2) Mark as hooked.
        // 3) On DataBindingComplete: remove unneeded columns, apply sizing and display order.
        private void HookGridBindingOnce()
        {
            if (_gridBindingHooked) return;

            _gridBindingHooked = true;

            dgvLines.DataBindingComplete += (s, e) =>
            {
                RemoveColumnIfExists(dgvLines, "Confidence");
                RemoveColumnIfExists(dgvLines, "CanId");
                RemoveColumnIfExists(dgvLines, "Timestamp");
                RemoveColumnIfExists(dgvLines, "TimestampText");

                if (dgvLines.Columns.Contains("CanNode"))
                {
                    var col = dgvLines.Columns["CanNode"];
                    if (col != null) col.Visible = true;
                }

                ApplyColumnSizing();
                ApplySafeDisplayOrder();
            };
        }

        // GOAL: Apply a safe column order without crashing when some columns are missing.
        // READS: dgvLines.Columns.
        // CHANGES: Sets DisplayIndex for columns that exist.
        // OOP PROOF: Defensive programming for a dynamic object-driven grid.
        // STEPS:
        // 1) Validate grid state.
        // 2) Define desired order.
        // 3) Apply DisplayIndex only for columns that exist.
        private void ApplySafeDisplayOrder()
        {
            if (dgvLines == null) return;
            if (dgvLines.IsDisposed) return;
            if (dgvLines.Columns == null) return;
            if (dgvLines.Columns.Count == 0) return;

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

            for (int i = 0; i < cols.Count; i++)
                cols[i]!.DisplayIndex = i;
        }

        // GOAL: Remove a grid column by name if it exists.
        // READS: grid.Columns.
        // CHANGES: Removes the column from the grid.
        // OOP PROOF: Encapsulation—column removal logic is centralized in one helper.
        // STEPS:
        // 1) Validate grid/columns.
        // 2) If the column is present, remove it.
        private static void RemoveColumnIfExists(DataGridView grid, string columnName)
        {
            if (grid == null) return;
            if (grid.Columns == null) return;

            if (grid.Columns.Contains(columnName))
                grid.Columns.Remove(columnName);
        }

        // GOAL: Build the "Summary" tab (labels + NRC table + DID table).
        // READS: (none) at build time.
        // CHANGES: Creates and assigns summary UI fields (labels + listviews).
        // OOP PROOF: Separation of concerns—the tab shows results computed elsewhere.
        // STEPS:
        // 1) Create a 2-column layout.
        // 2) Add top summary labels.
        // 3) Add NRC and DID ListViews inside GroupBoxes.
        private void BuildSummaryTab()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(8)
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

        // GOAL: Wire user actions (events) to the handler methods.
        // READS: UI controls that raise events.
        // CHANGES: Adds event subscriptions so the app responds to user input.
        // OOP PROOF: Event-driven GUI design—controls are objects that publish events.
        // STEPS:
        // 1) Buttons -> load/paste/clear handlers.
        // 2) Grid -> row coloring + selection details.
        // 3) Filter controls -> re-run ApplyFilters automatically.
        // 4) Summary list activation -> jump back to matching lines.
        // 5) Session controls -> create/close/switch sessions.
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

        // GOAL: Create a new LogSession object and optionally switch the UI to it.
        // READS: _sessions.Count, MaxSessions.
        // CHANGES: Adds a new session object to _sessions; may set it as active.
        // OOP PROOF:
        // - Classes/Objects: creates a LogSession instance (object) from the LogSession class.
        // - Scaling: multiple sessions are supported, bounded for performance.
        // STEPS:
        // 1) Block if MaxSessions reached.
        // 2) Instantiate a new LogSession and name it.
        // 3) Add it to the BindingList (UI updates automatically).
        // 4) Optionally select and activate it.
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

        // GOAL: Switch which session’s data the UI is showing.
        // READS: session passed in + its internal lists (AllLines, FilteredLines).
        // CHANGES: _activeSession, _allLogLines, _filteredLogLines, dgvLines.DataSource, status + summary.
        // OOP PROOF (Encapsulation):
        // - LogSession owns its lists; the Form binds to them instead of duplicating data.
        // STEPS:
        // 1) Store active session.
        // 2) If null: reset lists and refresh UI.
        // 3) Bind UI lists to the session-owned lists.
        // 4) Apply filters and refresh counts/summary.
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

        // GOAL: Button handler that forwards to CreateNewSession.
        // READS: (none)
        // CHANGES: Creates a new session object and activates it.
        // OOP PROOF: Creates a new object instance through CreateNewSession.
        // STEPS: Call CreateNewSession(makeActive: true).
        private void BtnAddSession_Click(object? sender, EventArgs e)
            => CreateNewSession(makeActive: true);

        // GOAL: Close the currently active session and switch to another session.
        // READS: _activeSession, lstSessions.SelectedIndex, _sessions.
        // CHANGES: Removes a session from _sessions; updates active selection and UI binding.
        // OOP PROOF: Object lifecycle—removing an object from the session collection.
        // STEPS:
        // 1) If no active session, exit.
        // 2) Remove active session from the list.
        // 3) If none left, create a new one.
        // 4) Otherwise pick a nearby session and activate it.
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

        // GOAL: When the user selects a session in the list, activate that session.
        // READS: lstSessions.SelectedItem.
        // CHANGES: Active session and UI binding via SetActiveSession.
        // OOP PROOF: Switching which object (LogSession) the UI is bound to.
        // STEPS: Call SetActiveSession on the selected session.
        private void LstSessions_SelectedIndexChanged(object? sender, EventArgs e)
            => SetActiveSession(lstSessions.SelectedItem as LogSession);

        // GOAL: Configure baseline grid behavior (sizing + user interactions).
        // READS: (none)
        // CHANGES: DataGridView configuration properties.
        // OOP PROOF: UI component configuration is encapsulated here.
        // STEPS:
        // 1) Disable auto-sizing that causes layout jumping.
        // 2) Allow user resizing and reordering.
        private void ConfigureDataGridColumns()
        {
            dgvLines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvLines.AllowUserToResizeColumns = true;
            dgvLines.AllowUserToOrderColumns = true;
        }

        // GOAL: Apply consistent column widths and friendly headers.
        // READS: dgvLines.Columns.
        // CHANGES: Column visible state, widths, header text.
        // OOP PROOF: UI “presentation rules” are separated from data logic.
        // STEPS:
        // 1) Validate grid state.
        // 2) Apply width/header config for key columns if they exist.
        // 3) Disable wrapping on Details to keep row height stable.
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

            SetCol("LineNumber", 80, "Line");
            SetCol("Raw", 360, "Raw");
            SetCol("Type", 95, "Type");
            SetCol("Summary", 320, "Report Summary");
            SetCol("Details", 520, "Technical Breakdown");
            SetCol("CanNode", 170, "Node");

            if (dgvLines.Columns.Contains("Details"))
            {
                var c = dgvLines.Columns["Details"];
                if (c != null) c.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            }
        }

        // GOAL: Color ISO15765 rows to visually separate request/response/error patterns.
        // READS: dgvLines row data (LogLine.Details, LogLine.Type).
        // CHANGES: Row background color (UI styling only).
        // OOP PROOF: UI reads object properties (LogLine fields) and renders them.
        // STEPS:
        // 1) Identify the LogLine bound to the row.
        // 2) If ISO15765: check details for Negative/Request/Positive patterns.
        // 3) Apply a background color to help scanning.
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

        // GOAL: Load a text/log file and send its lines into the decoding pipeline.
        // READS: File path selected by user.
        // CHANGES: Calls LoadLines which rebuilds objects, filters, summaries.
        // OOP PROOF: Delegation—UI handler calls the core object-building method (LoadLines).
        // STEPS:
        // 1) Show file picker dialog.
        // 2) If user selects a file: read all lines.
        // 3) Call LoadLines(lines, sessionName).
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

        // GOAL: Load built-in sample lines to demonstrate decoding and OOP pipeline quickly.
        // READS: Hard-coded sample string array.
        // CHANGES: Calls LoadLines which rebuilds objects, filters, summaries.
        // OOP PROOF: Creates multiple objects (one per sample line) through the normal pipeline.
        // STEPS:
        // 1) Define sample lines with different types (ISO, DEBUG, XML).
        // 2) Call LoadLines(sampleLines, "Sample").
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

        // GOAL: Clear the current session’s data and reset the detail views.
        // READS: (none)
        // CHANGES: Clears _allLogLines/_filteredLogLines and clears detail text boxes; updates status/summary.
        // OOP PROOF: Operates on collections of objects by clearing the object lists.
        // STEPS:
        // 1) Clear object lists.
        // 2) Clear UI detail panes.
        // 3) Recompute status and summary for an empty dataset.
        private void BtnClear_Click(object? sender, EventArgs e)
        {
            _allLogLines.Clear();
            _filteredLogLines.Clear();
            rtbRaw.Clear();
            rtbDecoded.Clear();
            UpdateStatusBar();
            UpdateFindingsSummary();
        }

        // GOAL: Load text from clipboard, split into lines, and decode them.
        // READS: Clipboard text.
        // CHANGES: Calls LoadLines which rebuilds objects, filters, summaries.
        // OOP PROOF: Same pipeline as file/sample—creates objects per line.
        // STEPS:
        // 1) Validate clipboard contains text.
        // 2) Split into line array.
        // 3) Call LoadLines(lines, "Pasted").
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

        // GOAL: When the user selects a grid row, show raw and decoded details below.
        // READS: dgvLines.SelectedRows -> selected LogLine object.
        // CHANGES: rtbRaw.Text and rtbDecoded.Text.
        // OOP PROOF: UI reads properties from the selected object (LogLine).
        // STEPS:
        // 1) Get selected row.
        // 2) Cast DataBoundItem to LogLine.
        // 3) Display Raw and Details fields.
        private void DgvLines_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvLines.SelectedRows.Count <= 0) return;

            var row = dgvLines.SelectedRows[0];

            if (row.DataBoundItem is not LogLine logLine) return;

            rtbRaw.Text = logLine.Raw ?? string.Empty;
            rtbDecoded.Text = logLine.Details ?? string.Empty;
        }

        // GOAL: Any filter control change triggers a filter rebuild.
        // READS: (none) directly; ApplyFilters reads UI state.
        // CHANGES: Calls ApplyFilters which rebuilds _filteredLogLines and summary.
        // OOP PROOF: Event-driven UI updates the displayed object collection.
        // STEPS: Call ApplyFilters().
        private void FilterControls_Changed(object? sender, EventArgs e)
            => ApplyFilters();

        // GOAL: Convert a search string into tokens (words/phrases) for matching.
        // READS: input string.
        // CHANGES: Returns a List<string> of tokens.
        // OOP PROOF: Uses collection types and string processing.
        // STEPS:
        // 1) Handle empty input.
        // 2) Split by spaces unless inside quotes.
        // 3) Return normalized non-empty tokens.
        private static List<string> TokenizeSearch(string input)
        {
            var tokens = new List<string>();

            if (string.IsNullOrWhiteSpace(input)) return tokens;

            bool inQuotes = false;

            var current = new System.Text.StringBuilder();

            foreach (char c in input)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

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

            if (current.Length > 0)
                tokens.Add(current.ToString().Trim());

            return tokens.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        }

        // GOAL: Convert raw text lines into decoded LogLine objects, then rebuild the UI and summaries.
        // READS: input 'lines' + optional sessionName + current _activeSession.
        // CHANGES:
        // - _allLogLines: becomes a new set of LogLine objects (one per input line).
        // - _filteredLogLines: rebuilt by ApplyFilters().
        // - _pdus and _transactions: rebuilt derived protocol artifacts.
        // - UI: status labels and summary tables refreshed.
        // OOP PROOF (this is the Assignment 5.1 centerpiece):
        // - Objects: each raw line becomes a new object instance (LogLine).
        // - Inheritance: Classify returns LogLine base type; runtime object can be derived types.
        // - Encapsulation: ParseAndDecode() keeps decoding logic inside the object itself.
        // STEPS:
        // 1) Ensure a session exists (create one if needed).
        // 2) Optionally rename the session (for file/sample/paste clarity).
        // 3) Clear prior objects for this session.
        // 4) For each line: classify -> decode -> add to object list.
        // 5) Apply filters to build the "visible" list.
        // 6) Build ISO-TP PDUs and UDS conversations from decoded lines.
        // 7) Update counts and summary displays.
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
                        // Inheritance proof:
                        // - logLine variable is declared as base type LogLine
                        // - the classifier may return a derived type at runtime
                        var logLine = LineClassifier.Classify(lineNumber, rawLine);

                        // Encapsulation proof:
                        // - the object owns its own decoding behavior
                        logLine.ParseAndDecode();

                        _allLogLines.Add(logLine);
                    }
                    catch (Exception ex)
                    {
                        // Error handling strategy:
                        // - Never crash the app because one line is malformed.
                        // - Instead store the line as an UnknownLine object with an error message.
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

        // GOAL: Rebuild the filtered list based on search text + selected type + UDS-only toggle.
        // READS: txtSearch, cboTypeFilter, chkUdsOnly, chkMatchAllTerms, _allLogLines.
        // CHANGES: _filteredLogLines (which controls what the grid shows) and summary output.
        // OOP PROOF:
        // - Works over a collection of LogLine objects and selects which objects are visible.
        // - Demonstrates control flow + LINQ-style matching behavior.
        // STEPS:
        // 1) Read UI filter state.
        // 2) Clear current filtered list.
        // 3) For each LogLine object: compute matches for search/type/UDS.
        // 4) Add matching objects to _filteredLogLines.
        // 5) Update the summary based on the visible list.
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

        // GOAL: Update the status bar counts for the FULL dataset (not filtered).
        // READS: _allLogLines.
        // CHANGES: lblStatusTotal/lblStatusIso/lblStatusXml/lblStatusUnknown text.
        // OOP PROOF: Uses object properties (LogLine.Type) to compute summary metrics.
        // STEPS:
        // 1) Count total lines.
        // 2) Count by type.
        // 3) Update label text.
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

        // GOAL: Update the Summary tab using the currently visible (filtered) lines.
        // READS: _filteredLogLines, _transactions.
        // CHANGES: Summary labels and both ListViews (NRC + DID tables).
        // OOP PROOF:
        // - Encapsulation + DLL usage: FindingsAggregator and UdsTables live in DLL projects.
        // - Form1 does not re-implement those rules; it calls them.
        // STEPS:
        // 1) Build FindingsSummary from filtered lines.
        // 2) Update summary labels.
        // 3) Populate NRC and DID tables.
        // 4) Append conversation count.
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

        // GOAL: Populate the NRC ListView with NRC code, meaning, and count.
        // READS: nrcCounts + UdsTables.NrcMeaning.
        // CHANGES: lvNrc items.
        // OOP PROOF: Uses dictionary data + lookups from a DLL table.
        // STEPS:
        // 1) Freeze UI updates (BeginUpdate).
        // 2) Clear old items.
        // 3) For each NRC: look up meaning, add a row.
        // 4) Resume UI updates (EndUpdate).
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

        // GOAL: Populate the DID ListView with DID, name, and count.
        // READS: didCounts + UdsTables.DescribeDid().
        // CHANGES: lvDid items.
        // OOP PROOF: Uses DLL-based DID naming logic instead of UI guessing.
        // STEPS:
        // 1) Freeze UI updates.
        // 2) Clear old items.
        // 3) For each DID: name it using UdsTables, add a row.
        // 4) Resume UI updates.
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

        // GOAL: When user activates an NRC row, jump to Decoded tab and filter by that NRC.
        // READS: lvNrc.SelectedItems[0].Tag.
        // CHANGES: txtSearch.Text, selected tab (Decoded).
        // OOP PROOF: UI drives filtering logic through shared pipeline (ApplyFilters via TextChanged).
        // STEPS:
        // 1) Get NRC from the selected item.
        // 2) Put the NRC into the search box.
        // 3) Switch to Decoded tab.
        private void LvNrc_ItemActivate(object? sender, EventArgs e)
        {
            if (lvNrc.SelectedItems.Count <= 0) return;

            byte nrc = (byte)(lvNrc.SelectedItems[0].Tag ?? (byte)0);

            txtSearch.Text = $"0x{nrc:X2}";
            tabControl.SelectedTab = tabDecoded;
        }

        // GOAL: When user activates a DID row, jump to Decoded tab and filter by that DID.
        // READS: lvDid.SelectedItems[0].Tag.
        // CHANGES: txtSearch.Text, selected tab (Decoded).
        // OOP PROOF: Same pipeline reuse as NRC activation.
        // STEPS:
        // 1) Get DID from the selected item.
        // 2) Put the DID into the search box.
        // 3) Switch to Decoded tab.
        private void LvDid_ItemActivate(object? sender, EventArgs e)
        {
            if (lvDid.SelectedItems.Count <= 0) return;

            ushort did = (ushort)(lvDid.SelectedItems[0].Tag ?? (ushort)0);

            txtSearch.Text = $"0x{did:X4}";
            tabControl.SelectedTab = tabDecoded;
        }

        // GOAL: Keep a SplitContainer splitter distance within safe bounds to prevent UI exceptions.
        // READS: SplitContainer size + min sizes + current SplitterDistance.
        // CHANGES: Adjusts SplitterDistance if needed.
        // OOP PROOF: Encapsulation—splitter safety rules are centralized in one helper.
        // STEPS:
        // 1) Validate container state and size.
        // 2) Compute min and max allowed distances.
        // 3) Clamp current distance into [min, max].
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

        // GOAL: After the form first appears, set safe minimum sizes and initial splitter positions.
        // READS: Current form/control sizes.
        // CHANGES: Panel min sizes and splitter distances.
        // OOP PROOF: Defensive GUI setup—prevents size-0 timing issues.
        // STEPS:
        // 1) Defer until UI is visible (BeginInvoke).
        // 2) Set min sizes.
        // 3) Set initial splitter distances.
        // 4) Clamp splitters to safe bounds.
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

        // GOAL: On every resize, clamp splitters so they remain valid.
        // READS: Current splitter distances and container sizes.
        // CHANGES: Adjusts splitter distances if needed.
        // OOP PROOF: Defensive UI behavior that prevents runtime errors.
        // STEPS:
        // 1) Call TryClampSplitter for each SplitContainer that exists.
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (splitMain != null) TryClampSplitter(splitMain);
            if (decodedRootSplit != null) TryClampSplitter(decodedRootSplit);
            if (decodedBottomSplit != null) TryClampSplitter(decodedBottomSplit);
            if (rawDecodedSplit != null) TryClampSplitter(rawDecodedSplit);
        }

        // GOAL: Normalize text so searching is consistent despite punctuation and casing.
        // READS: input string s.
        // CHANGES: Returns a cleaned string.
        // OOP PROOF: Utility method used by filtering logic.
        // STEPS:
        // 1) Lowercase the string.
        // 2) Replace common punctuation with spaces.
        // 3) Collapse multiple spaces into single spaces.
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

        // GOAL: Refresh the session ListBox display after a session name changes.
        // READS: lstSessions.DataSource and BindingContext.
        // CHANGES: Forces ListBox to repaint and refresh binding.
        // OOP PROOF: Demonstrates data-binding refresh mechanics for UI.
        // STEPS:
        // 1) Validate ListBox is bound.
        // 2) Ask CurrencyManager to refresh.
        // 3) Invalidate/update the ListBox.
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