using System.Collections.Generic;
using System.Windows.Controls;

namespace AzureExplorer.ToolWindows
{
    /// <summary>
    /// A read-only text control for displaying streaming log content with filtering support.
    /// </summary>
    public partial class LogDocumentControl : UserControl
    {
        private readonly string _streamKey;
        private readonly List<string> _allLines = new();
        private string _currentFilter;
        private TextBox _logTextBox;

        public LogDocumentControl(string streamKey, Action disconnectCallback)
        {
            InitializeComponent();

            _streamKey = streamKey;

            // Get reference to the TextBox control
            _logTextBox = (TextBox)FindName("LogTextBox");
        }

        /// <summary>
        /// Gets the stream key associated with this control.
        /// </summary>
        public string StreamKey => _streamKey;

        /// <summary>
        /// Appends a line of text and scrolls to the bottom.
        /// </summary>
        public void AppendLine(string text)
        {
            _allLines.Add(text);

            // If filtering, only show matching lines
            if (string.IsNullOrEmpty(_currentFilter) || MatchesFilter(text, _currentFilter))
            {
                _logTextBox?.AppendText(text + Environment.NewLine);
                _logTextBox?.ScrollToEnd();
            }
        }

        /// <summary>
        /// Clears all log content.
        /// </summary>
        public void Clear()
        {
            _allLines.Clear();
            _logTextBox?.Clear();
        }

        /// <summary>
        /// Sets the streaming state (no-op now that toolbar is in VSCT).
        /// </summary>
        public void SetStreamingState(bool isStreaming)
        {
            // Streaming state is now managed by the VSCT toolbar command
        }

        /// <summary>
        /// Applies a filter to show only matching log lines.
        /// </summary>
        public void ApplyFilter(string filter)
        {
            _currentFilter = filter;
            RefreshFilteredView();
        }

        private void RefreshFilteredView()
        {
            _logTextBox?.Clear();

            foreach (string line in _allLines)
            {
                if (string.IsNullOrEmpty(_currentFilter) || MatchesFilter(line, _currentFilter))
                {
                    _logTextBox?.AppendText(line + Environment.NewLine);
                }
            }

            _logTextBox?.ScrollToEnd();
        }

        private static bool MatchesFilter(string line, string filter)
        {
            return line.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
