using AutoDecoder.Models;
using AutoDecoder.Decoders;
using System.ComponentModel;

namespace AutoDecoder.Gui
{
    // Main form for the AutoDecoder application with filtering support
    public partial class Form1 : Form
    {
        // Master list holding all decoded log lines (never filtered)
        private BindingList<LogLine> _allLogLines;
        // Filtered list bound to the DataGridView
        private BindingList<LogLine> _filteredLogLines;

        // Constructor initializes the form and binding lists
        public Form1()
        {
            // Initialize form components
            InitializeComponent();
            // Create new binding list for all log lines (master list)
            _allLogLines = new BindingList<LogLine>();
            // Create new binding list for filtered log lines (display list)
            _filteredLogLines = new BindingList<LogLine>();
            // Bind the data grid to the filtered log lines list
            dgvLines.DataSource = _filteredLogLines;

            // Configure DataGridView column sizing after data binding
            ConfigureDataGridColumns();
            // Wire up row highlighting event handler
            dgvLines.RowPrePaint += DgvLines_RowPrePaint;
        }

        // Configure DataGridView columns for proper sizing and user control
        private void ConfigureDataGridColumns()
        {
            // Disable automatic column sizing (allows manual control)
            dgvLines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            // Allow users to resize columns manually
            dgvLines.AllowUserToResizeColumns = true;
            // Allow users to reorder columns by dragging headers
            dgvLines.AllowUserToOrderColumns = true;

            // Wait for columns to be auto-generated from data binding
            if (dgvLines.Columns.Count == 0)
            {
                // Columns not yet created, will configure on first data load
                dgvLines.DataBindingComplete += (s, e) =>
                {
                    // Configure columns once after first data binding
                    if (dgvLines.Columns.Count > 0)
                    {
                        // Apply column sizing rules
                        ApplyColumnSizing();
                    }
                };
            }
            else
            {
                // Columns already exist, configure now
                ApplyColumnSizing();
            }
        }

        // Apply specific sizing rules to DataGridView columns
        private void ApplyColumnSizing()
        {
            // Step 1: Auto-size all columns initially to get good default widths
            dgvLines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;

            // Step 2: Override specific columns with custom widths
            var summaryCol = dgvLines.Columns["Summary"];
            if (summaryCol != null)
            {
                // Rename column header to "Report Summary"
                summaryCol.HeaderText = "Report Summary";
                // Set fixed width for report summary
                summaryCol.Width = 400;
            }

            var detailsCol = dgvLines.Columns["Details"];
            if (detailsCol != null)
            {
                // Rename column header to "Technical Breakdown"
                detailsCol.HeaderText = "Technical Breakdown";
                // Set wider default width for technical breakdown
                detailsCol.Width = 500;
            }

            var rawCol = dgvLines.Columns["Raw"];
            if (rawCol != null)
            {
                // Set fixed width for raw data
                rawCol.Width = 300;
            }

            // Step 3: Switch ALL columns to None to enable manual resizing
            dgvLines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            foreach (DataGridViewColumn column in dgvLines.Columns)
            {
                // Set each column to None (allows manual resize)
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                // Explicitly enable resizing for each column
                column.Resizable = DataGridViewTriState.True;
            }
        }

        // Event handler for row pre-paint to apply visual highlighting
        private void DgvLines_RowPrePaint(object? sender, DataGridViewRowPrePaintEventArgs e)
        {
            // Check if row index is valid
            if (e.RowIndex < 0 || e.RowIndex >= dgvLines.Rows.Count)
            {
                // Skip invalid row indices
                return;
            }

            // Get the row being painted
            DataGridViewRow row = dgvLines.Rows[e.RowIndex];
            // Get the LogLine object bound to this row
            LogLine? logLine = row.DataBoundItem as LogLine;

            // Check if we have a valid LogLine object
            if (logLine == null)
            {
                // No data bound, use default color
                row.DefaultCellStyle.BackColor = Color.White;
                // Exit early
                return;
            }

            // Apply color based on line type and content
            if (logLine.Type == LineType.Iso15765)
            {
                // Check for UDS Negative Response (0x7F)
                if (logLine.Details?.Contains("Negative Response", StringComparison.OrdinalIgnoreCase) == true ||
                    logLine.Details?.Contains("0x7F", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Highlight negative responses in light salmon
                    row.DefaultCellStyle.BackColor = Color.LightSalmon;
                    // Exit after setting color
                    return;
                }

                // Check for UDS Request
                if (logLine.Details?.Contains("UDS Request", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Highlight requests in light sky blue
                    row.DefaultCellStyle.BackColor = Color.LightSkyBlue;
                    // Exit after setting color
                    return;
                }

                // Check for UDS Positive Response (0x62)
                if (logLine.Details?.Contains("UDS Positive Response", StringComparison.OrdinalIgnoreCase) == true ||
                    logLine.Details?.Contains("(0x62)", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Highlight positive responses in light green
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                    // Exit after setting color
                    return;
                }
            }

            // Default color for all other rows
            row.DefaultCellStyle.BackColor = Color.White;
        }

        // Event handler for Load File button click
        private void BtnLoadFile_Click(object? sender, EventArgs e)
        {
            // Create OpenFileDialog for file selection
            using OpenFileDialog ofd = new OpenFileDialog();
            // Set dialog title
            ofd.Title = "Select Log File";
            // Set file filter for text and log files
            ofd.Filter = "Text Files (*.txt)|*.txt|Log Files (*.log)|*.log|All Files (*.*)|*.*";

            // Show dialog and check if user selected a file
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                // Try to load the selected file
                try
                {
                    // Read all lines from the selected file
                    string[] lines = File.ReadAllLines(ofd.FileName);
                    // Load the lines into the application
                    LoadLines(lines);
                }
                // Catch any exceptions during file loading
                catch (Exception ex)
                {
                    // Show error message to user without crashing
                    MessageBox.Show($"Error loading file: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Event handler for Load Sample button click (updated with timestamp prefixes)
        private void BtnLoadSample_Click(object? sender, EventArgs e)
        {
            // Create array of sample log lines demonstrating various formats including timestamp prefixes
            string[] sampleLines = new[]
            {
                // Sample ISO15765 line with timestamp prefix and UDS negative response (0x7F 0x22 0x78 = ResponsePending)
                "2025-10-21T10:23:45.123 ISO15765 RX <- [00,00,07,D8,7F,22,78]",
                // Sample ISO15765 line with UDS positive response (0x62 DID 0x806A)
                "2025-10-21T10:23:45.200 ISO15765 TX -> [00,00,07,D0,62,80,6A,41,42,43,44]",
                // Sample ISO15765 line with another negative response
                "2025-10-21T10:23:46.000 ISO15765 RX <- [00,00,07,D8,7F,10,11]",
                // Sample XML line with DID F188
                "<ns3:didValue didValue=\"F188\" type=\"Strategy\"><ns3:Response>4D59535452415445475931</ns3:Response></ns3:didValue>",
                // Sample XML line with DID F110
                "<didValue=\"F110\"><Response>50415254325350454331</Response></didValue>",
                // Sample XML line with DID DE00
                "<ns3:didValue didValue=\"DE00\" type=\"DirectConfig\"><ns3:Response>434F4E464947</ns3:Response></ns3:didValue>",
                // Sample hex line in bracket notation
                "[48,65,6C,6C,6F,20,57,6F,72,6C,64]",
                // Sample long hex string
                "48656C6C6F2C20746869732069732061206C6F6E672068657820737472696E67",
                // Sample ASCII text line
                "This is a plain ASCII text log entry with timestamp 2024-01-15",
                // Another ISO15765 line with different service and timestamp
                "2025-10-21T10:23:47.000 ISO15765 TX -> [00,00,07,D0,22,F1,88]",
                // ISO15765 positive response to previous request
                "2025-10-21T10:23:47.100 ISO15765 RX <- [00,00,07,D8,62,F1,88,56,45,52,53,49,4F,4E,31]",
                // Sample with TesterPresent service
                "2025-10-21T10:23:48.000 ISO15765 TX -> [00,00,07,D0,3E,00]",
                // Positive response to TesterPresent
                "2025-10-21T10:23:48.050 ISO15765 RX <- [00,00,07,D8,7E,00]",
                // Sample ASCII log entry
                "DEBUG: Starting diagnostic session",
                // Sample XML with calibration DID
                "<ns3:didValue didValue=\"F124\" type=\"Calibration\"><ns3:Response>43414C4942</ns3:Response></ns3:didValue>",
                // More ASCII entries to reach 50+ lines
                "INFO: Connecting to ECU",
                "INFO: Sending diagnostic request",
                "INFO: Waiting for response",
                "DEBUG: Response received successfully",
                "INFO: Processing response data",
                // More ISO15765 examples with timestamps
                "2025-10-21T10:24:00.000 ISO15765 TX -> [00,00,07,D0,10,01]",
                "2025-10-21T10:24:00.050 ISO15765 RX <- [00,00,07,D8,50,01]",
                "2025-10-21T10:24:01.000 ISO15765 TX -> [00,00,07,D0,27,01]",
                "2025-10-21T10:24:01.050 ISO15765 RX <- [00,00,07,D8,67,01,12,34,56,78]",
                // More text entries
                "INFO: Security access granted",
                "DEBUG: Writing configuration data",
                "INFO: Configuration write successful",
                "INFO: Verifying configuration",
                "DEBUG: Verification passed",
                // More sample lines to exceed 50 total
                "INFO: Operation completed successfully",
                "DEBUG: Disconnecting from ECU",
                "INFO: Session ended",
                "TRACE: Cleanup operations started",
                "TRACE: Resources released",
                "INFO: Application ready for next operation",
                // Additional hex samples
                "[01,02,03,04,05,06,07,08,09,0A,0B,0C,0D,0E,0F]",
                "DEADBEEFCAFEBABE0123456789ABCDEF",
                // Additional XML samples
                "<ns3:didValue didValue=\"F111\" type=\"CoreAssembly\"><ns3:Response>434F5245</ns3:Response></ns3:didValue>",
                "<ns3:didValue didValue=\"F113\" type=\"Assembly\"><ns3:Response>4153534D</ns3:Response></ns3:didValue>",
                // More text entries
                "INFO: Diagnostic scan complete",
                "INFO: No errors detected",
                "DEBUG: System status: OK",
                "INFO: Ready for next command",
                // More ISO15765 with different NRCs and timestamps
                "2025-10-21T10:25:00.000 ISO15765 RX <- [00,00,07,D8,7F,22,31]",
                "2025-10-21T10:25:01.000 ISO15765 RX <- [00,00,07,D8,7F,27,35]",
                "2025-10-21T10:25:02.000 ISO15765 RX <- [00,00,07,D8,7F,2E,22]",
                // More text to ensure 50+ lines
                "INFO: Test sequence initiated",
                "DEBUG: Parameter validation passed",
                "INFO: Executing test case 1",
                "INFO: Executing test case 2",
                "INFO: Executing test case 3",
                "DEBUG: All test cases passed",
                "INFO: Test sequence completed"
            };

            // Load the sample lines into the application
            LoadLines(sampleLines);
        }

        // Event handler for Clear button click
        private void BtnClear_Click(object? sender, EventArgs e)
        {
            // Clear the master log lines list
            _allLogLines.Clear();
            // Clear the filtered log lines list
            _filteredLogLines.Clear();
            // Clear raw text box
            rtbRaw.Clear();
            // Clear decoded text box
            rtbDecoded.Clear();
            // Update status bar counts
            UpdateStatusBar();
            // Clear findings summary
            UpdateFindingsSummary();
        }

        // Event handler for Paste button click
        private void BtnPaste_Click(object? sender, EventArgs e)
        {
            // Check if clipboard contains text
            if (!Clipboard.ContainsText())
            {
                // Show message if clipboard is empty or doesn't contain text
                MessageBox.Show("Clipboard does not contain text data.", "Paste Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Try to get text from clipboard
            try
            {
                // Get text from clipboard
                string clipboardText = Clipboard.GetText();

                // Check if text is empty
                if (string.IsNullOrWhiteSpace(clipboardText))
                {
                    // Show message if clipboard text is empty
                    MessageBox.Show("Clipboard text is empty.", "Paste Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Split clipboard text into lines (handle both Windows and Unix line endings)
                string[] lines = clipboardText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                // Load the lines into the application
                LoadLines(lines);
            }
            // Catch any exceptions during clipboard access
            catch (Exception ex)
            {
                // Show error message to user without crashing
                MessageBox.Show($"Error pasting from clipboard: {ex.Message}", "Paste Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Event handler for DataGridView selection change
        private void DgvLines_SelectionChanged(object? sender, EventArgs e)
        {
            // Check if any rows are selected
            if (dgvLines.SelectedRows.Count > 0)
            {
                // Get the first selected row
                DataGridViewRow selectedRow = dgvLines.SelectedRows[0];
                // Get the LogLine object from the row
                LogLine? logLine = selectedRow.DataBoundItem as LogLine;

                // Check if we got a valid LogLine object
                if (logLine != null)
                {
                    // Display raw line text in left textbox
                    rtbRaw.Text = logLine.Raw ?? string.Empty;
                    // Display decoded details in right textbox
                    rtbDecoded.Text = logLine.Details ?? string.Empty;
                }
            }
        }

        // Event handler for filter controls (search, type, UDS checkbox)
        private void FilterControls_Changed(object? sender, EventArgs e)
        {
            // Apply filters to update the displayed lines
            ApplyFilters();
        }

        // Helper method to tokenize search input, respecting quoted phrases
        private static List<string> TokenizeSearch(string input)
        {
            // List to hold parsed tokens
            List<string> tokens = new List<string>();

            // Return empty list if input is null or whitespace
            if (string.IsNullOrWhiteSpace(input))
            {
                return tokens;
            }

            // Track if we're inside quotes
            bool inQuotes = false;
            // Build current token
            System.Text.StringBuilder currentToken = new System.Text.StringBuilder();

            // Iterate through each character
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == '"')
                {
                    // Toggle quote state
                    inQuotes = !inQuotes;
                }
                else if (c == ' ' && !inQuotes)
                {
                    // Space outside quotes - end current token
                    if (currentToken.Length > 0)
                    {
                        // Add trimmed token to list
                        tokens.Add(currentToken.ToString().Trim());
                        // Reset for next token
                        currentToken.Clear();
                    }
                }
                else
                {
                    // Regular character - add to current token
                    currentToken.Append(c);
                }
            }

            // Add final token if any
            if (currentToken.Length > 0)
            {
                tokens.Add(currentToken.ToString().Trim());
            }

            // Remove empty tokens
            tokens = tokens.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();

            return tokens;
        }

        // Helper method to load lines into the application (improved with stats)
        private void LoadLines(string[] lines)
        {
            // Try to process all lines
            try
            {
                // Clear existing master log lines
                _allLogLines.Clear();
                // Clear filtered log lines
                _filteredLogLines.Clear();

                // Iterate through each line with index
                for (int i = 0; i < lines.Length; i++)
                {
                    // Get the current line
                    string rawLine = lines[i];
                    // Calculate line number (1-based)
                    int lineNumber = i + 1;

                    // Try to classify and decode the line
                    try
                    {
                        // Classify the raw line into appropriate LogLine type
                        LogLine logLine = LineClassifier.Classify(lineNumber, rawLine);
                        // Parse and decode the line
                        logLine.ParseAndDecode();
                        // Add the decoded line to the master list
                        _allLogLines.Add(logLine);
                    }
                    // Catch any exceptions during line processing
                    catch (Exception ex)
                    {
                        // Create UnknownLine for failed lines to avoid crashes
                        LogLine errorLine = new UnknownLine(lineNumber, rawLine, $"Error: {ex.Message}");
                        // Parse the error line
                        errorLine.ParseAndDecode();
                        // Add the error line to the master list
                        _allLogLines.Add(errorLine);
                    }
                }

                // Apply filters to populate the filtered list
                ApplyFilters();
                // Update status bar with statistics
                UpdateStatusBar();
                // Build and display findings summary
                UpdateFindingsSummary();

                // Show success message with line count
                MessageBox.Show($"Successfully loaded {_allLogLines.Count} lines.", "Load Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            // Catch any exceptions during overall loading
            catch (Exception ex)
            {
                // Show error message without crashing
                MessageBox.Show($"Error loading lines: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Helper method to apply filters and update the filtered list
        private void ApplyFilters()
        {
            // Get search text from textbox (trim)
            string searchText = (txtSearch.Text ?? string.Empty).Trim();
            // Tokenize search text into keywords and phrases
            List<string> searchTokens = TokenizeSearch(searchText);
            // Get match all terms checkbox state (AND vs OR)
            bool matchAllTerms = chkMatchAllTerms.Checked;
            // Get selected type filter from combo box
            string typeFilter = cboTypeFilter.SelectedItem?.ToString() ?? "All";
            // Get UDS only checkbox state
            bool udsOnly = chkUdsOnly.Checked;

            // Clear the filtered list (preparing to rebuild)
            _filteredLogLines.Clear();

            // Iterate through all master log lines
            foreach (LogLine logLine in _allLogLines)
            {
                // Build combined search field (Raw + Report Summary + Technical Breakdown)
                string combinedField = (logLine.Raw ?? string.Empty) + " " +
                                      (logLine.Summary ?? string.Empty) + " " +
                                      (logLine.Details ?? string.Empty);
                // Convert to lowercase for case-insensitive search
                string searchField = combinedField.ToLower();

                // Check search text filter with multi-keyword support
                bool matchesSearch = true;
                if (searchTokens.Count > 0)
                {
                    if (matchAllTerms)
                    {
                        // AND logic - all tokens must be present
                        matchesSearch = searchTokens.All(token => 
                            searchField.Contains(token.ToLower(), StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        // OR logic - any token present
                        matchesSearch = searchTokens.Any(token => 
                            searchField.Contains(token.ToLower(), StringComparison.OrdinalIgnoreCase));
                    }
                }

                // Check type filter
                bool matchesType = typeFilter == "All" || logLine.Type.ToString() == typeFilter;

                // Check UDS only filter (Details contains "UDS")
                bool matchesUds = !udsOnly || (logLine.Details?.Contains("UDS", StringComparison.OrdinalIgnoreCase) == true);

                // Add to filtered list if all filters match
                if (matchesSearch && matchesType && matchesUds)
                {
                    // Add line to filtered list
                    _filteredLogLines.Add(logLine);
                }
            }

            // Update findings summary based on filtered lines
            UpdateFindingsSummary();
        }

        // Helper method to update status bar with statistics
        private void UpdateStatusBar()
        {
            // Count total lines from master list
            int totalLines = _allLogLines.Count;
            // Count ISO15765 lines
            int isoLines = _allLogLines.Count(line => line.Type == LineType.Iso15765);
            // Count XML lines
            int xmlLines = _allLogLines.Count(line => line.Type == LineType.Xml);
            // Count Unknown lines
            int unknownLines = _allLogLines.Count(line => line.Type == LineType.Unknown);

            // Update status bar labels with counts
            lblStatusTotal.Text = $"Total: {totalLines}";
            // Update ISO count
            lblStatusIso.Text = $"ISO: {isoLines}";
            // Update XML count
            lblStatusXml.Text = $"XML: {xmlLines}";
            // Update Unknown count
            lblStatusUnknown.Text = $"Unknown: {unknownLines}";
        }

        // Build and display findings summary in the Summary tab
        private void UpdateFindingsSummary()
        {
            // Build aggregated findings from filtered lines
            FindingsSummary summary = FindingsAggregator.Build(_filteredLogLines);

            // Update summary totals labels
            lblSummaryIso.Text = $"ISO Lines: {summary.IsoLines}";
            // Update UDS findings count
            lblSummaryUds.Text = $"UDS Findings: {summary.UdsFindingLines}";
            // Update unknown lines count
            lblSummaryUnknown.Text = $"Unknown: {summary.UnknownLines}";

            // Populate NRC ListView
            PopulateNrcListView(summary.NrcCounts);

            // Populate DID ListView
            PopulateDidListView(summary.DidCounts);
        }

        // Populate the NRC ListView with aggregated NRC counts
        private void PopulateNrcListView(Dictionary<byte, int> nrcCounts)
        {
            // Clear existing items
            lvNrc.Items.Clear();

            // Sort NRC codes by count (descending) then by NRC value (ascending)
            var sortedNrcs = nrcCounts.OrderByDescending(kvp => kvp.Value).ThenBy(kvp => kvp.Key);

            // Add each NRC to the ListView
            foreach (var kvp in sortedNrcs)
            {
                // Get NRC code
                byte nrcCode = kvp.Key;
                // Get occurrence count
                int count = kvp.Value;

                // Look up NRC meaning from decode table (deterministic)
                string nrcMeaning = DecodeTables.UdsNrcNames.TryGetValue(nrcCode, out var meaning)
                    ? meaning
                    : "Unknown";

                // Create ListView item with NRC hex value
                ListViewItem item = new ListViewItem($"0x{nrcCode:X2}");
                // Add NRC meaning as subitem
                item.SubItems.Add(nrcMeaning);
                // Add count as subitem
                item.SubItems.Add(count.ToString());
                // Store NRC code in Tag for filtering
                item.Tag = nrcCode;
                // Add item to ListView
                lvNrc.Items.Add(item);
            }
        }

        // Populate the DID ListView with aggregated DID counts
        private void PopulateDidListView(Dictionary<ushort, int> didCounts)
        {
            // Clear existing items
            lvDid.Items.Clear();

            // Sort DIDs by count (descending) then by DID value (ascending)
            var sortedDids = didCounts.OrderByDescending(kvp => kvp.Value).ThenBy(kvp => kvp.Key);

            // Add each DID to the ListView
            foreach (var kvp in sortedDids)
            {
                // Get DID value
                ushort didValue = kvp.Key;
                // Get occurrence count
                int count = kvp.Value;

                // Look up DID name from decode table (deterministic)
                string didName = DecodeTables.KnownDids.TryGetValue(didValue, out var name)
                    ? name
                    : "Unknown";

                // Create ListView item with DID hex value
                ListViewItem item = new ListViewItem($"0x{didValue:X4}");
                // Add DID name as subitem
                item.SubItems.Add(didName);
                // Add count as subitem
                item.SubItems.Add(count.ToString());
                // Store DID value in Tag for filtering
                item.Tag = didValue;
                // Add item to ListView
                lvDid.Items.Add(item);
            }
        }

        // Event handler for clicking an NRC item (double-click or enter)
        private void LvNrc_ItemActivate(object? sender, EventArgs e)
        {
            // Check if any items are selected
            if (lvNrc.SelectedItems.Count > 0)
            {
                // Get the first selected item
                ListViewItem selectedItem = lvNrc.SelectedItems[0];
                // Get the NRC code from the Tag
                byte nrcCode = (byte)(selectedItem.Tag ?? (byte)0);

                // Set search filter to NRC hex value (e.g., "0x78")
                txtSearch.Text = $"0x{nrcCode:X2}";
                // Switch to Decoded tab to show filtered results
                tabControl.SelectedTab = tabDecoded;
            }
        }

        // Event handler for clicking a DID item (double-click or enter)
        private void LvDid_ItemActivate(object? sender, EventArgs e)
        {
            // Check if any items are selected
            if (lvDid.SelectedItems.Count > 0)
            {
                // Get the first selected item
                ListViewItem selectedItem = lvDid.SelectedItems[0];
                // Get the DID value from the Tag
                ushort didValue = (ushort)(selectedItem.Tag ?? (ushort)0);

                // Set search filter to DID hex value (e.g., "0xF188")
                txtSearch.Text = $"0x{didValue:X4}";
                // Switch to Decoded tab to show filtered results
                tabControl.SelectedTab = tabDecoded;
            }
        }
    }
}
