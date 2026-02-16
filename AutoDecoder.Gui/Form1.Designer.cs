namespace AutoDecoder.Gui
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // Create the top button panel
            panelTop = new Panel();
            // Create button for loading files
            btnLoadFile = new Button();
            // Create button for loading sample data
            btnLoadSample = new Button();
            // Create button for pasting from clipboard
            btnPaste = new Button();
            // Create button for clearing data
            btnClear = new Button();

            // Create filter panel controls
            panelFilter = new Panel();
            // Create label for search textbox
            lblSearch = new Label();
            // Create search textbox for filtering
            txtSearch = new TextBox();
            // Create checkbox for match all terms (AND vs OR search)
            chkMatchAllTerms = new CheckBox();
            // Create label for type filter
            lblTypeFilter = new Label();
            // Create combo box for type filtering
            cboTypeFilter = new ComboBox();
            // Create checkbox for UDS findings filter
            chkUdsOnly = new CheckBox();

            // Create the main vertical split container (top/bottom)
            splitMain = new SplitContainer();
            // Create the DataGridView for displaying log lines
            dgvLines = new DataGridView();

            // Create the bottom horizontal split container (left/right for raw/decoded)
            splitBottom = new SplitContainer();
            // Create RichTextBox for raw line display
            rtbRaw = new RichTextBox();
            // Create TabControl for decoded/summary display
            tabControl = new TabControl();
            // Create "Decoded" tab page
            tabDecoded = new TabPage();
            // Create RichTextBox for decoded details display
            rtbDecoded = new RichTextBox();
            // Create "Summary" tab page
            tabSummary = new TabPage();
            // Create ListView for NRC findings
            lvNrc = new ListView();
            // Create ListView for DID findings
            lvDid = new ListView();
            // Create panel for summary totals
            panelSummaryTotals = new Panel();
            // Create labels for summary statistics
            lblSummaryIso = new Label();
            lblSummaryUds = new Label();
            lblSummaryUnknown = new Label();

            // Create "Legend" tab page for visual guide
            tabLegend = new TabPage();
            // Create legend GroupBox container for visual guide
            grpLegend = new GroupBox();
            // Create label header for row color legend section
            lblLegendRowColors = new Label();
            // Create color swatch panel for positive response (green)
            pnlColorPositive = new Panel();
            // Create label describing positive response color
            lblColorPositive = new Label();
            // Create color swatch panel for negative response (orange/salmon)
            pnlColorNegative = new Panel();
            // Create label describing negative response color
            lblColorNegative = new Label();
            // Create color swatch panel for request (blue)
            pnlColorRequest = new Panel();
            // Create label describing request color
            lblColorRequest = new Label();
            // Create color swatch panel for partial/incomplete (gray)
            pnlColorPartial = new Panel();
            // Create label describing partial decode color
            lblColorPartial = new Label();
            // Create label header for confidence score legend section
            lblLegendConfidence = new Label();
            // Create label for 1.0 confidence explanation
            lblConfidence10 = new Label();
            // Create label for 0.9 confidence explanation
            lblConfidence09 = new Label();
            // Create label for 0.6 confidence explanation
            lblConfidence06 = new Label();
            // Create label for 0.5 confidence explanation
            lblConfidence05 = new Label();

            // Create status strip for statistics
            statusStrip = new StatusStrip();
            // Create status label for total lines
            lblStatusTotal = new ToolStripStatusLabel();
            // Create status label for ISO lines
            lblStatusIso = new ToolStripStatusLabel();
            // Create status label for XML lines
            lblStatusXml = new ToolStripStatusLabel();
            // Create status label for unknown lines
            lblStatusUnknown = new ToolStripStatusLabel();

            // Suspend layout during initialization
            panelTop.SuspendLayout();
            // Suspend filter panel layout
            panelFilter.SuspendLayout();
            // Suspend main split container layout
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            // Suspend Panel1 layout
            splitMain.Panel1.SuspendLayout();
            // Suspend Panel2 layout
            splitMain.Panel2.SuspendLayout();
            // Suspend main split container
            splitMain.SuspendLayout();
            // Suspend data grid layout
            ((System.ComponentModel.ISupportInitialize)dgvLines).BeginInit();
            // Suspend bottom split container layout
            ((System.ComponentModel.ISupportInitialize)splitBottom).BeginInit();
            // Suspend Panel1 layout
            splitBottom.Panel1.SuspendLayout();
            // Suspend Panel2 layout
            splitBottom.Panel2.SuspendLayout();
            // Suspend bottom split container
            splitBottom.SuspendLayout();
            // Suspend tab control layout
            tabControl.SuspendLayout();
            // Suspend decoded tab layout
            tabDecoded.SuspendLayout();
            // Suspend summary tab layout
            tabSummary.SuspendLayout();
            // Suspend legend tab layout
            tabLegend.SuspendLayout();
            // Suspend summary totals panel layout
            panelSummaryTotals.SuspendLayout();
            // Suspend status strip layout
            statusStrip.SuspendLayout();
            // Suspend form layout
            SuspendLayout();

            // Configure panelTop (button panel)
            panelTop.Dock = DockStyle.Top;
            // Set panel height
            panelTop.Height = 50;
            // Set background color
            panelTop.BackColor = System.Drawing.SystemColors.Control;
            // Add Load File button to panel
            panelTop.Controls.Add(btnLoadFile);
            // Add Load Sample button to panel
            panelTop.Controls.Add(btnLoadSample);
            // Add Paste button to panel
            panelTop.Controls.Add(btnPaste);
            // Add Clear button to panel
            panelTop.Controls.Add(btnClear);

            // Configure btnLoadFile
            btnLoadFile.Location = new Point(10, 10);
            // Set button size
            btnLoadFile.Size = new Size(100, 30);
            // Set button text
            btnLoadFile.Text = "Load File";
            // Wire up click event
            btnLoadFile.Click += BtnLoadFile_Click;

            // Configure btnLoadSample
            btnLoadSample.Location = new Point(120, 10);
            // Set button size
            btnLoadSample.Size = new Size(100, 30);
            // Set button text
            btnLoadSample.Text = "Load Sample";
            // Wire up click event
            btnLoadSample.Click += BtnLoadSample_Click;

            // Configure btnPaste
            btnPaste.Location = new Point(230, 10);
            // Set button size
            btnPaste.Size = new Size(100, 30);
            // Set button text
            btnPaste.Text = "Paste Log";
            // Wire up click event
            btnPaste.Click += BtnPaste_Click;

            // Configure btnClear
            btnClear.Location = new Point(340, 10);
            // Set button size
            btnClear.Size = new Size(100, 30);
            // Set button text
            btnClear.Text = "Clear";
            // Wire up click event
            btnClear.Click += BtnClear_Click;

            // Configure panelFilter (filter controls)
            panelFilter.Dock = DockStyle.Top;
            // Set panel height
            panelFilter.Height = 50;
            // Set background color
            panelFilter.BackColor = System.Drawing.SystemColors.Control;
            // Add search label
            panelFilter.Controls.Add(lblSearch);
            // Add search textbox
            panelFilter.Controls.Add(txtSearch);
            // Add match all terms checkbox
            panelFilter.Controls.Add(chkMatchAllTerms);
            // Add type filter label
            panelFilter.Controls.Add(lblTypeFilter);
            // Add type filter combo
            panelFilter.Controls.Add(cboTypeFilter);
            // Add UDS checkbox
            panelFilter.Controls.Add(chkUdsOnly);

            // Configure lblSearch
            lblSearch.Location = new Point(10, 15);
            // Set label size
            lblSearch.Size = new Size(50, 20);
            // Set label text
            lblSearch.Text = "Search:";
            // Center text vertically
            lblSearch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Configure txtSearch
            txtSearch.Location = new Point(65, 13);
            // Set textbox size
            txtSearch.Size = new Size(200, 23);
            // Wire up text changed event for live filtering
            txtSearch.TextChanged += FilterControls_Changed;

            // Configure chkMatchAllTerms
            chkMatchAllTerms.Location = new Point(275, 13);
            // Set checkbox size
            chkMatchAllTerms.Size = new Size(130, 23);
            // Set checkbox text
            chkMatchAllTerms.Text = "Match all terms";
            // Set default state (checked = AND search)
            chkMatchAllTerms.Checked = true;
            // Wire up checked changed event
            chkMatchAllTerms.CheckedChanged += FilterControls_Changed;

            // Configure lblTypeFilter
            lblTypeFilter.Location = new Point(415, 15);
            // Set label size
            lblTypeFilter.Size = new Size(40, 20);
            // Set label text
            lblTypeFilter.Text = "Type:";
            // Center text vertically
            lblTypeFilter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Configure cboTypeFilter
            cboTypeFilter.Location = new Point(460, 13);
            // Set combo size
            cboTypeFilter.Size = new Size(120, 23);
            // Set dropdown style
            cboTypeFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            // Add filter options
            cboTypeFilter.Items.AddRange(new object[] { "All", "Iso15765", "Xml", "Hex", "Ascii", "Unknown" });
            // Set default selection
            cboTypeFilter.SelectedIndex = 0;
            // Wire up selection changed event
            cboTypeFilter.SelectedIndexChanged += FilterControls_Changed;

            // Configure chkUdsOnly
            chkUdsOnly.Location = new Point(590, 13);
            // Set checkbox size
            chkUdsOnly.Size = new Size(150, 23);
            // Set checkbox text
            chkUdsOnly.Text = "Only UDS Findings";
            // Wire up checked changed event
            chkUdsOnly.CheckedChanged += FilterControls_Changed;
            // Configure splitMain (main vertical split)
            splitMain.Dock = DockStyle.Fill;
            // Set orientation to horizontal (top/bottom split)
            splitMain.Orientation = Orientation.Horizontal;
            // Set splitter distance (400 pixels for top)
            splitMain.SplitterDistance = 400;
            // Add data grid to top panel
            splitMain.Panel1.Controls.Add(dgvLines);
            // Add bottom split container to bottom panel
            splitMain.Panel2.Controls.Add(splitBottom);

            // Configure dgvLines
            dgvLines.Dock = DockStyle.Fill;
            // Allow full row selection
            dgvLines.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            // Disable multi-select
            dgvLines.MultiSelect = false;
            // Make read-only
            dgvLines.ReadOnly = true;
            // Enable column reordering
            dgvLines.AllowUserToOrderColumns = true;
            // Auto size columns
            dgvLines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            // Wire up selection changed event
            dgvLines.SelectionChanged += DgvLines_SelectionChanged;

            // Configure splitBottom (horizontal split for raw/decoded)
            splitBottom.Dock = DockStyle.Fill;
            // Set orientation to vertical (left/right split)
            splitBottom.Orientation = Orientation.Vertical;
            // Set splitter distance (50% split)
            splitBottom.SplitterDistance = 400;
            // Add raw textbox to left panel
            splitBottom.Panel1.Controls.Add(rtbRaw);
            // Add tab control to right panel
            splitBottom.Panel2.Controls.Add(tabControl);

            // Configure rtbRaw in left panel
            rtbRaw.Dock = DockStyle.Fill;
            // Make read-only
            rtbRaw.ReadOnly = true;
            // Use monospace font for better readability
            rtbRaw.Font = new Font("Consolas", 9);
            // Set background color
            rtbRaw.BackColor = System.Drawing.Color.WhiteSmoke;

            // Configure tabControl in right panel
            tabControl.Dock = DockStyle.Fill;
            // Add decoded tab
            tabControl.TabPages.Add(tabDecoded);
            // Add summary tab
            tabControl.TabPages.Add(tabSummary);
            // Add legend tab
            tabControl.TabPages.Add(tabLegend);

            // Configure tabDecoded (Decoded tab)
            tabDecoded.Text = "Decoded";
            // Set tab name
            tabDecoded.Name = "tabDecoded";
            // Add decoded textbox to tab
            tabDecoded.Controls.Add(rtbDecoded);

            // Configure rtbDecoded in decoded tab
            rtbDecoded.Dock = DockStyle.Fill;
            // Make read-only
            rtbDecoded.ReadOnly = true;
            // Use monospace font for better readability
            rtbDecoded.Font = new Font("Consolas", 9);
            // Set background color
            rtbDecoded.BackColor = System.Drawing.Color.Honeydew;

            // Configure tabSummary (Summary tab)
            tabSummary.Text = "Summary";
            // Set tab name
            tabSummary.Name = "tabSummary";
            // Add panel for totals at top
            tabSummary.Controls.Add(panelSummaryTotals);
            // Add NRC ListView
            tabSummary.Controls.Add(lvNrc);
            // Add DID ListView
            tabSummary.Controls.Add(lvDid);

            // Configure tabLegend (Legend tab)
            tabLegend.Text = "Legend";
            // Set tab name
            tabLegend.Name = "tabLegend";
            // Add legend GroupBox to Legend tab
            tabLegend.Controls.Add(grpLegend);

            // Configure panelSummaryTotals (totals at top of Summary tab)
            panelSummaryTotals.Dock = DockStyle.Top;
            // Set panel height
            panelSummaryTotals.Height = 40;
            // Set background color
            panelSummaryTotals.BackColor = System.Drawing.SystemColors.Info;
            // Add ISO label
            panelSummaryTotals.Controls.Add(lblSummaryIso);
            // Add UDS label
            panelSummaryTotals.Controls.Add(lblSummaryUds);
            // Add Unknown label
            panelSummaryTotals.Controls.Add(lblSummaryUnknown);

            // Configure summary labels
            lblSummaryIso.Location = new Point(10, 10);
            // Set label size
            lblSummaryIso.Size = new Size(150, 20);
            // Set label text
            lblSummaryIso.Text = "ISO Lines: 0";
            // Center text vertically
            lblSummaryIso.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Configure UDS findings label
            lblSummaryUds.Location = new Point(170, 10);
            // Set label size
            lblSummaryUds.Size = new Size(150, 20);
            // Set label text
            lblSummaryUds.Text = "UDS Findings: 0";
            // Center text vertically
            lblSummaryUds.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Configure Unknown lines label
            lblSummaryUnknown.Location = new Point(330, 10);
            // Set label size
            lblSummaryUnknown.Size = new Size(150, 20);
            // Set label text
            lblSummaryUnknown.Text = "Unknown: 0";
            // Center text vertically
            lblSummaryUnknown.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Configure lvNrc (NRC findings ListView)
            lvNrc.Dock = DockStyle.Top;
            // Set list view height (half of remaining space)
            lvNrc.Height = 200;
            // Set view to Details
            lvNrc.View = View.Details;
            // Enable full row select
            lvNrc.FullRowSelect = true;
            // Enable grid lines
            lvNrc.GridLines = true;
            // Add columns for NRC ListView
            lvNrc.Columns.Add("NRC", 80);
            // Add Meaning column
            lvNrc.Columns.Add("Meaning", 200);
            // Add Count column
            lvNrc.Columns.Add("Count", 80);
            // Wire up click event for filtering
            lvNrc.ItemActivate += LvNrc_ItemActivate;

            // Configure lvDid (DID findings ListView)
            lvDid.Dock = DockStyle.Fill;
            // Set view to Details
            lvDid.View = View.Details;
            // Enable full row select
            lvDid.FullRowSelect = true;
            // Enable grid lines
            lvDid.GridLines = true;
            // Add columns for DID ListView
            lvDid.Columns.Add("DID", 80);
            // Add Name column
            lvDid.Columns.Add("Name", 200);
            // Add Count column
            lvDid.Columns.Add("Count", 80);
            // Wire up click event for filtering
            lvDid.ItemActivate += LvDid_ItemActivate;

            // ============================================================
            // Configure Legend GroupBox (visual guide for colors and confidence)
            // ============================================================
            grpLegend.Text = "Legend";
            // Dock to fill entire Legend tab
            grpLegend.Dock = DockStyle.Fill;
            // Set padding for internal controls
            grpLegend.Padding = new Padding(20);
            // Add row colors header label
            grpLegend.Controls.Add(lblLegendRowColors);
            // Add positive response color swatch
            grpLegend.Controls.Add(pnlColorPositive);
            // Add positive response label
            grpLegend.Controls.Add(lblColorPositive);
            // Add negative response color swatch
            grpLegend.Controls.Add(pnlColorNegative);
            // Add negative response label
            grpLegend.Controls.Add(lblColorNegative);
            // Add request color swatch
            grpLegend.Controls.Add(pnlColorRequest);
            // Add request label
            grpLegend.Controls.Add(lblColorRequest);
            // Add partial decode color swatch
            grpLegend.Controls.Add(pnlColorPartial);
            // Add partial decode label
            grpLegend.Controls.Add(lblColorPartial);
            // Add confidence scores header label
            grpLegend.Controls.Add(lblLegendConfidence);
            // Add 1.0 confidence explanation
            grpLegend.Controls.Add(lblConfidence10);
            // Add 0.9 confidence explanation
            grpLegend.Controls.Add(lblConfidence09);
            // Add 0.6 confidence explanation
            grpLegend.Controls.Add(lblConfidence06);
            // Add 0.5 confidence explanation
            grpLegend.Controls.Add(lblConfidence05);

            // Configure "Row Colors" header label
            lblLegendRowColors.Text = "Row Colors:";
            // Position at top-left of legend group
            lblLegendRowColors.Location = new Point(20, 30);
            // Set size for header
            lblLegendRowColors.Size = new Size(150, 20);
            // Make text bold
            lblLegendRowColors.Font = new Font(lblLegendRowColors.Font, FontStyle.Bold);

            // Configure positive response color swatch (green 18x18 panel)
            pnlColorPositive.BackColor = Color.LightGreen;
            // Position below header
            pnlColorPositive.Location = new Point(30, 60);
            // Set swatch size to 18x18 pixels
            pnlColorPositive.Size = new Size(18, 18);
            // Add border for visibility
            pnlColorPositive.BorderStyle = BorderStyle.FixedSingle;

            // Configure positive response description label
            lblColorPositive.Text = "UDS Positive Response (0x62)";
            // Position next to color swatch
            lblColorPositive.Location = new Point(55, 60);
            // Set label size
            lblColorPositive.Size = new Size(250, 18);
            // Center text vertically
            lblColorPositive.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Configure negative response color swatch (orange/salmon 18x18 panel)
            pnlColorNegative.BackColor = Color.LightSalmon;
            // Position below positive response
            pnlColorNegative.Location = new Point(30, 85);
            // Set swatch size to 18x18 pixels
            pnlColorNegative.Size = new Size(18, 18);
            // Add border for visibility
            pnlColorNegative.BorderStyle = BorderStyle.FixedSingle;

            // Configure negative response description label
            lblColorNegative.Text = "UDS Negative Response (0x7F)";
            // Position next to color swatch
            lblColorNegative.Location = new Point(55, 85);
            // Set label size
            lblColorNegative.Size = new Size(250, 18);
            // Center text vertically
            lblColorNegative.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Configure request color swatch (blue 18x18 panel)
            pnlColorRequest.BackColor = Color.LightSkyBlue;
            // Position below negative response
            pnlColorRequest.Location = new Point(30, 110);
            // Set swatch size to 18x18 pixels
            pnlColorRequest.Size = new Size(18, 18);
            // Add border for visibility
            pnlColorRequest.BorderStyle = BorderStyle.FixedSingle;

            // Configure request description label
            lblColorRequest.Text = "UDS Request (0x22)";
            // Position next to color swatch
            lblColorRequest.Location = new Point(55, 110);
            // Set label size
            lblColorRequest.Size = new Size(250, 18);
            // Center text vertically
            lblColorRequest.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Configure partial decode color swatch (light gray 18x18 panel)
            pnlColorPartial.BackColor = Color.LightGray;
            // Position below request
            pnlColorPartial.Location = new Point(30, 135);
            // Set swatch size to 18x18 pixels
            pnlColorPartial.Size = new Size(18, 18);
            // Add border for visibility
            pnlColorPartial.BorderStyle = BorderStyle.FixedSingle;

            // Configure partial decode description label
            lblColorPartial.Text = "Partial/No UDS payload";
            // Position next to color swatch
            lblColorPartial.Location = new Point(55, 135);
            // Set label size
            lblColorPartial.Size = new Size(250, 18);
            // Center text vertically
            lblColorPartial.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Configure "Confidence Scores" header label
            lblLegendConfidence.Text = "Confidence Scores:";
            // Position in right column of legend group
            lblLegendConfidence.Location = new Point(330, 30);
            // Set size for header
            lblLegendConfidence.Size = new Size(200, 20);
            // Make text bold
            lblLegendConfidence.Font = new Font(lblLegendConfidence.Font, FontStyle.Bold);

            // Configure 1.0 confidence explanation label
            lblConfidence10.Text = "1.0 → Exact match";
            // Position below confidence header
            lblConfidence10.Location = new Point(340, 60);
            // Set label size
            lblConfidence10.Size = new Size(220, 18);
            // Center text vertically
            lblConfidence10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Configure 0.9 confidence explanation label
            lblConfidence09.Text = "0.9 → Strong match";
            // Position below 1.0 explanation
            lblConfidence09.Location = new Point(340, 85);
            // Set label size
            lblConfidence09.Size = new Size(220, 18);
            // Center text vertically
            lblConfidence09.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Configure 0.6 confidence explanation label
            lblConfidence06.Text = "0.6 → Partial decode";
            // Position below 0.9 explanation
            lblConfidence06.Location = new Point(340, 110);
            // Set label size
            lblConfidence06.Size = new Size(220, 18);
            // Center text vertically
            lblConfidence06.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Configure 0.5 confidence explanation label
            lblConfidence05.Text = "0.5 → Incomplete frame";
            // Position below 0.6 explanation
            lblConfidence05.Location = new Point(340, 135);
            // Set label size
            lblConfidence05.Size = new Size(220, 18);
            // Center text vertically
            lblConfidence05.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // ============================================================
            // End of Legend configuration
            // ============================================================

            // Configure statusStrip
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatusTotal, lblStatusIso, lblStatusXml, lblStatusUnknown });
            // Dock to bottom
            statusStrip.Location = new Point(0, 700);
            // Set status strip size
            statusStrip.Size = new Size(1000, 22);

            // Configure status labels
            lblStatusTotal.Text = "Total: 0";
            // Set spring to push other labels to the right
            lblStatusTotal.Spring = true;
            // Align left
            lblStatusTotal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // Configure ISO status label
            lblStatusIso.Text = "ISO: 0";
            // Set border
            lblStatusIso.BorderSides = ToolStripStatusLabelBorderSides.Left;

            // Configure XML status label
            lblStatusXml.Text = "XML: 0";
            // Set border
            lblStatusXml.BorderSides = ToolStripStatusLabelBorderSides.Left;

            // Configure Unknown status label
            lblStatusUnknown.Text = "Unknown: 0";
            // Set border
            lblStatusUnknown.BorderSides = ToolStripStatusLabelBorderSides.Left;

            // Configure Form1
            AutoScaleDimensions = new SizeF(7F, 15F);
            // Set auto scale mode
            AutoScaleMode = AutoScaleMode.Font;
            // Set form size
            ClientSize = new Size(1000, 722);
            // Add status strip to form
            Controls.Add(statusStrip);
            // Add main split container to form
            Controls.Add(splitMain);
            // Add filter panel to form
            Controls.Add(panelFilter);
            // Add top panel to form
            Controls.Add(panelTop);
            // Set form title
            Text = "AutoDecoder - Log Line Decoder";
            // Set minimum size
            MinimumSize = new Size(800, 600);

            // Resume layout
            panelTop.ResumeLayout(false);
            // Resume filter panel layout
            panelFilter.ResumeLayout(false);
            // Resume filter panel layout
            panelFilter.PerformLayout();
            // Resume Panel1 layout
            splitMain.Panel1.ResumeLayout(false);
            // Resume Panel2 layout
            splitMain.Panel2.ResumeLayout(false);
            // Resume main split container
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            // Resume main split container layout
            splitMain.ResumeLayout(false);
            // Resume data grid layout
            ((System.ComponentModel.ISupportInitialize)dgvLines).EndInit();
            // Resume Panel1 layout
            splitBottom.Panel1.ResumeLayout(false);
            // Resume Panel2 layout
            splitBottom.Panel2.ResumeLayout(false);
            // Resume bottom split container
            ((System.ComponentModel.ISupportInitialize)splitBottom).EndInit();
            // Resume bottom split container layout
            splitBottom.ResumeLayout(false);
            // Resume tab control layout
            tabControl.ResumeLayout(false);
            // Resume decoded tab layout
            tabDecoded.ResumeLayout(false);
            // Resume summary tab layout
            tabSummary.ResumeLayout(false);
            // Resume legend tab layout
            tabLegend.ResumeLayout(false);
            // Resume summary totals panel layout
            panelSummaryTotals.ResumeLayout(false);
            // Resume status strip layout
            statusStrip.ResumeLayout(false);
            // Resume status strip layout
            statusStrip.PerformLayout();
            // Resume form layout
            ResumeLayout(false);
            // Perform layout
            PerformLayout();
        }

        #endregion

        // Top button panel
        private Panel panelTop = null!;
        // Button for loading files
        private Button btnLoadFile = null!;
        // Button for loading sample data
        private Button btnLoadSample = null!;
        // Button for pasting from clipboard
        private Button btnPaste = null!;
        // Button for clearing data
        private Button btnClear = null!;

        // Filter panel
        private Panel panelFilter = null!;
        // Search label
        private Label lblSearch = null!;
        // Search textbox
        private TextBox txtSearch = null!;
        // Match all terms checkbox (AND vs OR search)
        private CheckBox chkMatchAllTerms = null!;
        // Type filter label
        private Label lblTypeFilter = null!;
        // Type filter combo box
        private ComboBox cboTypeFilter = null!;
        // UDS only checkbox
        private CheckBox chkUdsOnly = null!;

        // Main vertical split container
        private SplitContainer splitMain = null!;
        // DataGridView for displaying log lines
        private DataGridView dgvLines = null!;

        // Bottom horizontal split container
        private SplitContainer splitBottom = null!;
        // RichTextBox for displaying raw line text
        private RichTextBox rtbRaw = null!;
        // TabControl for decoded/summary display
        private TabControl tabControl = null!;
        // Decoded tab page
        private TabPage tabDecoded = null!;
        // RichTextBox for displaying decoded details
        private RichTextBox rtbDecoded = null!;
        // Summary tab page
        private TabPage tabSummary = null!;
        // ListView for NRC findings
        private ListView lvNrc = null!;
        // ListView for DID findings
        private ListView lvDid = null!;
        // Panel for summary totals
        private Panel panelSummaryTotals = null!;
        // Label for ISO lines count
        private Label lblSummaryIso = null!;
        // Label for UDS findings count
        private Label lblSummaryUds = null!;
        // Label for unknown lines count
        private Label lblSummaryUnknown = null!;

        // Legend tab page
        private TabPage tabLegend = null!;

        // Status strip for statistics
        private StatusStrip statusStrip = null!;
        // Status label for total lines
        private ToolStripStatusLabel lblStatusTotal = null!;
        // Status label for ISO lines
        private ToolStripStatusLabel lblStatusIso = null!;
        // Status label for XML lines
        private ToolStripStatusLabel lblStatusXml = null!;
        // Status label for unknown lines
        private ToolStripStatusLabel lblStatusUnknown = null!;

        // Legend GroupBox for visual guide
        private GroupBox grpLegend = null!;
        // Row colors header label
        private Label lblLegendRowColors = null!;
        // Color swatch for positive response (green)
        private Panel pnlColorPositive = null!;
        // Label describing positive response color
        private Label lblColorPositive = null!;
        // Color swatch for negative response (orange/salmon)
        private Panel pnlColorNegative = null!;
        // Label describing negative response color
        private Label lblColorNegative = null!;
        // Color swatch for request (blue)
        private Panel pnlColorRequest = null!;
        // Label describing request color
        private Label lblColorRequest = null!;
        // Color swatch for partial decode (gray)
        private Panel pnlColorPartial = null!;
        // Label describing partial decode color
        private Label lblColorPartial = null!;
        // Confidence scores header label
        private Label lblLegendConfidence = null!;
        // Label for 1.0 confidence explanation
        private Label lblConfidence10 = null!;
        // Label for 0.9 confidence explanation
        private Label lblConfidence09 = null!;
        // Label for 0.6 confidence explanation
        private Label lblConfidence06 = null!;
        // Label for 0.5 confidence explanation
        private Label lblConfidence05 = null!;
    }
}
