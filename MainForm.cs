using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SimpleNotepad
{
    public class MainForm : Form
    {
        // ===== Core UI =====
        private TextBox textArea;
        private FontDialog fontDialog;
        private Panel contentPanel;
        private Panel lineNumberPanel;
        private ToolStrip mainToolStrip;

        private string currentFilePath = string.Empty;

        // Undo / Redo
        private readonly Stack<string> undoStack = new Stack<string>();
        private readonly Stack<string> redoStack = new Stack<string>();
        private bool isInternalChange = false;
        private string snapshotText = "";
        private DateTime lastSnapshotTime;

        private bool isDirty = false;

        // Find / Replace
        private string lastSearchText = string.Empty;
        private FindReplaceForm findForm;

        // ===== Themes =====
        private enum ThemeMode
        {
            Light,
            Dark,
            Hacker,
            Cyberpunk,
            Synthwave,
            SkyBlue,
            SoftYellow
        }

        private ThemeMode currentTheme = ThemeMode.Light;

        private ToolStripMenuItem themeLightMenuItem;
        private ToolStripMenuItem themeDarkMenuItem;
        private ToolStripMenuItem themeHackerMenuItem;
        private ToolStripMenuItem themeCyberpunkMenuItem;
        private ToolStripMenuItem themeSynthwaveMenuItem;
        private ToolStripMenuItem themeSkyBlueMenuItem;
        private ToolStripMenuItem themeSoftYellowMenuItem;

        // ===== Status Bar / Counters =====
        private StatusStrip statusStrip;
        private ToolStripStatusLabel charCountLabel;
        private ToolStripStatusLabel wordCountLabel;
        private ToolStripStatusLabel lineCountLabel;
        private bool showLineCount = true;
        private ToolStripMenuItem showLineCountMenuItem;

        // Word wrap
        private bool wordWrap = false;
        private ToolStripMenuItem wordWrapMenuItem;

        // Line numbers
        private bool showLineNumbers = false;
        private ToolStripMenuItem showLineNumbersMenuItem;
        private ToolStripButton wordWrapButton;
        private ToolStripButton lineNumbersButton;

        public MainForm()
        {
            this.Text = "Simple Notepad";
            this.Width = 900;
            this.Height = 600;

            BuildCoreUi();
            BuildStatusBar();

            MenuStrip menu = BuildMenu();
            this.MainMenuStrip = menu;

            ToolStrip toolbar = BuildToolbar();

            this.Controls.Add(contentPanel);
            this.Controls.Add(statusStrip);
            this.Controls.Add(toolbar);
            this.Controls.Add(menu);

            InitializeDocument("");
            ApplyWordWrap();
            ApplyTheme();
            UpdateStatusBar();
        }

        // ================= CORE UI =================

        private void BuildCoreUi()
        {
            textArea = new TextBox();
            textArea.Multiline = true;
            textArea.Dock = DockStyle.Fill;
            textArea.ScrollBars = ScrollBars.Both;
            textArea.AcceptsTab = true;
            textArea.WordWrap = false;
            textArea.BackColor = Color.White;
            textArea.ForeColor = Color.Black;

            textArea.TextChanged += TextArea_TextChanged;
            textArea.Resize += TextArea_LayoutChanged;
            textArea.FontChanged += TextArea_LayoutChanged;
            textArea.KeyUp += TextArea_LayoutChanged;
            textArea.MouseUp += TextArea_LayoutChanged;
            textArea.MouseWheel += TextArea_LayoutChanged;

            fontDialog = new FontDialog();
            fontDialog.Font = new Font("Consolas", 11);
            fontDialog.ShowEffects = true;
            textArea.Font = fontDialog.Font;

            lineNumberPanel = new Panel();
            lineNumberPanel.Dock = DockStyle.Left;
            lineNumberPanel.Width = 50;
            lineNumberPanel.BackColor = Color.Gainsboro;
            lineNumberPanel.Visible = showLineNumbers;
            lineNumberPanel.Paint += LineNumberPanel_Paint;

            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.BackColor = Color.White;
            contentPanel.Controls.Add(textArea);
            contentPanel.Controls.Add(lineNumberPanel);

            this.BackColor = Color.White;
        }

        private void TextArea_LayoutChanged(object sender, EventArgs e)
        {
            if (showLineNumbers)
                lineNumberPanel.Invalidate();
        }

        private MenuStrip BuildMenu()
        {
            MenuStrip menu = new MenuStrip();

            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
            ToolStripMenuItem editMenu = new ToolStripMenuItem("Edit");
            ToolStripMenuItem formatMenu = new ToolStripMenuItem("Format");
            ToolStripMenuItem viewMenu = new ToolStripMenuItem("View");
            ToolStripMenuItem helpMenu = new ToolStripMenuItem("Help");

            // FILE
            ToolStripMenuItem newItem = new ToolStripMenuItem("New", null, OnNewClicked);
            newItem.ShortcutKeys = Keys.Control | Keys.N;
            ToolStripMenuItem openItem = new ToolStripMenuItem("Open...", null, OnOpenClicked);
            openItem.ShortcutKeys = Keys.Control | Keys.O;
            ToolStripMenuItem saveItem = new ToolStripMenuItem("Save", null, OnSaveClicked);
            saveItem.ShortcutKeys = Keys.Control | Keys.S;
            ToolStripMenuItem saveAsItem = new ToolStripMenuItem("Save As...", null, OnSaveAsClicked);
            saveAsItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit", null, OnExitClicked);

            fileMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                newItem, openItem, saveItem, saveAsItem,
                new ToolStripSeparator(), exitItem
            });

            // EDIT
            ToolStripMenuItem undoItem = new ToolStripMenuItem("Undo", null, delegate { Undo(); });
            undoItem.ShortcutKeys = Keys.Control | Keys.Z;
            ToolStripMenuItem redoItem = new ToolStripMenuItem("Redo", null, delegate { Redo(); });
            redoItem.ShortcutKeys = Keys.Control | Keys.Y;

            ToolStripMenuItem cutItem = new ToolStripMenuItem("Cut", null, delegate { textArea.Cut(); });
            ToolStripMenuItem copyItem = new ToolStripMenuItem("Copy", null, delegate { textArea.Copy(); });
            ToolStripMenuItem pasteItem = new ToolStripMenuItem("Paste", null, delegate { textArea.Paste(); });
            ToolStripMenuItem selectAllItem = new ToolStripMenuItem("Select All", null, delegate { textArea.SelectAll(); });
            selectAllItem.ShortcutKeys = Keys.Control | Keys.A;

            ToolStripMenuItem findItem = new ToolStripMenuItem("Find...", null, delegate { ShowFindReplace(true); });
            findItem.ShortcutKeys = Keys.Control | Keys.F;
            ToolStripMenuItem replaceItem = new ToolStripMenuItem("Replace...", null, delegate { ShowFindReplace(false); });
            replaceItem.ShortcutKeys = Keys.Control | Keys.H;

            editMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                undoItem, redoItem,
                new ToolStripSeparator(),
                cutItem, copyItem, pasteItem,
                new ToolStripSeparator(),
                findItem, replaceItem,
                new ToolStripSeparator(),
                selectAllItem
            });

            // FORMAT
            ToolStripMenuItem fontItem = new ToolStripMenuItem("Font...", null, OnFontClicked);

            ToolStripMenuItem themeMenu = new ToolStripMenuItem("Theme");
            themeLightMenuItem = new ToolStripMenuItem("Light", null, delegate { SetTheme(ThemeMode.Light); });
            themeDarkMenuItem = new ToolStripMenuItem("Dark", null, delegate { SetTheme(ThemeMode.Dark); });
            themeHackerMenuItem = new ToolStripMenuItem("Hacker (Green on Black)", null, delegate { SetTheme(ThemeMode.Hacker); });
            themeCyberpunkMenuItem = new ToolStripMenuItem("Cyberpunk", null, delegate { SetTheme(ThemeMode.Cyberpunk); });
            themeSynthwaveMenuItem = new ToolStripMenuItem("Synthwave", null, delegate { SetTheme(ThemeMode.Synthwave); });
            themeSkyBlueMenuItem = new ToolStripMenuItem("Sky Blue", null, delegate { SetTheme(ThemeMode.SkyBlue); });
            themeSoftYellowMenuItem = new ToolStripMenuItem("Soft Yellow", null, delegate { SetTheme(ThemeMode.SoftYellow); });

            themeLightMenuItem.Checked = true;

            themeMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                themeLightMenuItem,
                themeDarkMenuItem,
                themeHackerMenuItem,
                new ToolStripSeparator(),
                themeCyberpunkMenuItem,
                themeSynthwaveMenuItem,
                new ToolStripSeparator(),
                themeSkyBlueMenuItem,
                themeSoftYellowMenuItem
            });

            formatMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                fontItem,
                new ToolStripSeparator(),
                themeMenu
            });

            // VIEW
            wordWrapMenuItem = new ToolStripMenuItem("Word Wrap", null, OnToggleWordWrap);
            wordWrapMenuItem.CheckOnClick = true;
            wordWrapMenuItem.Checked = wordWrap;

            showLineCountMenuItem = new ToolStripMenuItem("Show Line Count", null, OnToggleLineCount);
            showLineCountMenuItem.CheckOnClick = true;
            showLineCountMenuItem.Checked = showLineCount;

            showLineNumbersMenuItem = new ToolStripMenuItem("Show Line Numbers", null, OnToggleLineNumbers);
            showLineNumbersMenuItem.CheckOnClick = true;
            showLineNumbersMenuItem.Checked = showLineNumbers;

            viewMenu.DropDownItems.Add(wordWrapMenuItem);
            viewMenu.DropDownItems.Add(showLineCountMenuItem);
            viewMenu.DropDownItems.Add(showLineNumbersMenuItem);

            // HELP
            ToolStripMenuItem aboutItem = new ToolStripMenuItem("About", null, OnAboutClicked);
            helpMenu.DropDownItems.Add(aboutItem);

            menu.Items.Add(fileMenu);
            menu.Items.Add(editMenu);
            menu.Items.Add(formatMenu);
            menu.Items.Add(viewMenu);
            menu.Items.Add(helpMenu);

            return menu;
        }

        private ToolStrip BuildToolbar()
        {
            mainToolStrip = new ToolStrip();
            mainToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            mainToolStrip.RenderMode = ToolStripRenderMode.System;
            mainToolStrip.Dock = DockStyle.Top;

            ToolStripButton btnNew = new ToolStripButton("New");
            btnNew.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnNew.Click += OnNewClicked;

            ToolStripButton btnOpen = new ToolStripButton("Open");
            btnOpen.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnOpen.Click += OnOpenClicked;

            ToolStripButton btnSave = new ToolStripButton("Save");
            btnSave.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnSave.Click += OnSaveClicked;

            ToolStripSeparator sep1 = new ToolStripSeparator();

            ToolStripButton btnFind = new ToolStripButton("Find");
            btnFind.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnFind.Click += delegate { ShowFindReplace(true); };

            ToolStripButton btnReplace = new ToolStripButton("Replace");
            btnReplace.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnReplace.Click += delegate { ShowFindReplace(false); };

            ToolStripSeparator sep2 = new ToolStripSeparator();

            wordWrapButton = new ToolStripButton("Word Wrap");
            wordWrapButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            wordWrapButton.CheckOnClick = true;
            wordWrapButton.Checked = wordWrap;
            wordWrapButton.Click += OnToggleWordWrap;

            lineNumbersButton = new ToolStripButton("Line Numbers");
            lineNumbersButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            lineNumbersButton.CheckOnClick = true;
            lineNumbersButton.Checked = showLineNumbers;
            lineNumbersButton.Click += OnToggleLineNumbers;

            ToolStripSeparator sep3 = new ToolStripSeparator();

            ToolStripDropDownButton themeDropDown = new ToolStripDropDownButton("Theme");
            themeDropDown.DropDownItems.Add("Light", null, (s, e) => SetTheme(ThemeMode.Light));
            themeDropDown.DropDownItems.Add("Dark", null, (s, e) => SetTheme(ThemeMode.Dark));
            themeDropDown.DropDownItems.Add("Hacker", null, (s, e) => SetTheme(ThemeMode.Hacker));
            themeDropDown.DropDownItems.Add(new ToolStripSeparator());
            themeDropDown.DropDownItems.Add("Cyberpunk", null, (s, e) => SetTheme(ThemeMode.Cyberpunk));
            themeDropDown.DropDownItems.Add("Synthwave", null, (s, e) => SetTheme(ThemeMode.Synthwave));
            themeDropDown.DropDownItems.Add(new ToolStripSeparator());
            themeDropDown.DropDownItems.Add("Sky Blue", null, (s, e) => SetTheme(ThemeMode.SkyBlue));
            themeDropDown.DropDownItems.Add("Soft Yellow", null, (s, e) => SetTheme(ThemeMode.SoftYellow));

            ToolStripSeparator sep4 = new ToolStripSeparator();

            ToolStripButton aboutButton = new ToolStripButton("About");
            aboutButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            aboutButton.Click += OnAboutClicked;

            mainToolStrip.Items.Add(btnNew);
            mainToolStrip.Items.Add(btnOpen);
            mainToolStrip.Items.Add(btnSave);
            mainToolStrip.Items.Add(sep1);
            mainToolStrip.Items.Add(btnFind);
            mainToolStrip.Items.Add(btnReplace);
            mainToolStrip.Items.Add(sep2);
            mainToolStrip.Items.Add(wordWrapButton);
            mainToolStrip.Items.Add(lineNumbersButton);
            mainToolStrip.Items.Add(sep3);
            mainToolStrip.Items.Add(themeDropDown);
            mainToolStrip.Items.Add(sep4);
            mainToolStrip.Items.Add(aboutButton);

            return mainToolStrip;
        }

        private void BuildStatusBar()
        {
            statusStrip = new StatusStrip();
            statusStrip.SizingGrip = false;

            charCountLabel = new ToolStripStatusLabel();
            wordCountLabel = new ToolStripStatusLabel();
            lineCountLabel = new ToolStripStatusLabel();

            statusStrip.Items.Add(charCountLabel);
            statusStrip.Items.Add(wordCountLabel);
            statusStrip.Items.Add(lineCountLabel);
        }

        private void InitializeDocument(string text)
        {
            isInternalChange = true;
            textArea.Text = text;
            isInternalChange = false;

            snapshotText = text;
            undoStack.Clear();
            redoStack.Clear();
            isDirty = false;
            lastSnapshotTime = DateTime.Now;

            if (showLineNumbers)
                lineNumberPanel.Invalidate();
        }

        // ================= UNDO / REDO =================

        private void TextArea_TextChanged(object sender, EventArgs e)
        {
            if (isInternalChange) return;

            string current = textArea.Text;
            DateTime now = DateTime.Now;

            bool bigChange = Math.Abs(current.Length - snapshotText.Length) >= 3;
            bool timeGap = (now - lastSnapshotTime).TotalMilliseconds >= 600;
            bool boundary = current.Length > 0 && IsBoundaryChar(current[current.Length - 1]);

            if (bigChange || timeGap || boundary)
            {
                undoStack.Push(snapshotText);
                snapshotText = current;
                redoStack.Clear();
                lastSnapshotTime = now;
            }

            isDirty = true;
            UpdateStatusBar();

            if (showLineNumbers)
                lineNumberPanel.Invalidate();
        }

        private static bool IsBoundaryChar(char c)
        {
            if (char.IsWhiteSpace(c)) return true;
            return ".!?,;:-\"'()\n\r\t".IndexOf(c) >= 0;
        }

        private void Undo()
        {
            if (undoStack.Count == 0) return;

            redoStack.Push(textArea.Text);

            isInternalChange = true;
            string previous = undoStack.Pop();
            textArea.Text = previous;
            snapshotText = previous;
            isInternalChange = false;

            isDirty = true;
            UpdateStatusBar();

            if (showLineNumbers)
                lineNumberPanel.Invalidate();
        }

        private void Redo()
        {
            if (redoStack.Count == 0) return;

            undoStack.Push(textArea.Text);

            isInternalChange = true;
            string next = redoStack.Pop();
            textArea.Text = next;
            snapshotText = next;
            isInternalChange = false;

            isDirty = true;
            UpdateStatusBar();

            if (showLineNumbers)
                lineNumberPanel.Invalidate();
        }

        // ================= STATUS BAR =================

        private void UpdateStatusBar()
        {
            string text = textArea.Text;
            int charCount = text.Length;
            int lineCount = textArea.Lines.Length;

            int wordCount = 0;
            if (!string.IsNullOrWhiteSpace(text))
            {
                string[] parts = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                wordCount = parts.Length;
            }

            charCountLabel.Text = "Chars: " + charCount;
            wordCountLabel.Text = "Words: " + wordCount;
            lineCountLabel.Text = "Lines: " + lineCount;
            lineCountLabel.Visible = showLineCount;
        }

        private void OnToggleLineCount(object sender, EventArgs e)
        {
            showLineCount = showLineCountMenuItem.Checked;
            UpdateStatusBar();
        }

        // ================= WORD WRAP =================

        private void OnToggleWordWrap(object sender, EventArgs e)
        {
            bool newValue;

            if (sender == wordWrapMenuItem)
                newValue = wordWrapMenuItem.Checked;
            else if (sender == wordWrapButton)
                newValue = !wordWrap;
            else
                newValue = !wordWrap;

            wordWrap = newValue;

            if (wordWrapMenuItem != null)
                wordWrapMenuItem.Checked = wordWrap;
            if (wordWrapButton != null)
                wordWrapButton.Checked = wordWrap;

            ApplyWordWrap();
        }

        private void ApplyWordWrap()
        {
            textArea.WordWrap = wordWrap;
            textArea.ScrollBars = wordWrap ? ScrollBars.Vertical : ScrollBars.Both;
        }

        // ================= LINE NUMBERS =================

        private void OnToggleLineNumbers(object sender, EventArgs e)
        {
            bool newValue;

            if (sender == showLineNumbersMenuItem)
                newValue = showLineNumbersMenuItem.Checked;
            else if (sender == lineNumbersButton)
                newValue = !showLineNumbers;
            else
                newValue = !showLineNumbers;

            showLineNumbers = newValue;

            if (showLineNumbersMenuItem != null)
                showLineNumbersMenuItem.Checked = showLineNumbers;
            if (lineNumbersButton != null)
                lineNumbersButton.Checked = showLineNumbers;

            lineNumberPanel.Visible = showLineNumbers;
            lineNumberPanel.Invalidate();
        }

        private void LineNumberPanel_Paint(object sender, PaintEventArgs e)
        {
            if (!showLineNumbers) return;

            e.Graphics.Clear(lineNumberPanel.BackColor);

            if (textArea.Lines.Length == 0)
                return;

            int firstIndex = textArea.GetCharIndexFromPosition(new Point(0, 0));
            int firstLine = textArea.GetLineFromCharIndex(firstIndex);

            int lastIndex = textArea.GetCharIndexFromPosition(
                new Point(0, textArea.ClientSize.Height - 1));
            int lastLine = textArea.GetLineFromCharIndex(lastIndex);

            using (Brush brush = new SolidBrush(textArea.ForeColor))
            {
                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Far;
                format.LineAlignment = StringAlignment.Near;

                for (int line = firstLine; line <= lastLine && line < textArea.Lines.Length; line++)
                {
                    int charIndex = textArea.GetFirstCharIndexFromLine(line);
                    if (charIndex < 0) continue;

                    Point pos = textArea.GetPositionFromCharIndex(charIndex);
                    float y = pos.Y;

                    string lineText = (line + 1).ToString();

                    RectangleF rect = new RectangleF(
                        0,
                        y,
                        lineNumberPanel.Width - 4,
                        textArea.Font.Height);

                    e.Graphics.DrawString(lineText, textArea.Font, brush, rect, format);
                }
            }
        }

        // ================= FILE MENU =================

        private bool PromptToSaveIfDirty()
        {
            if (!isDirty) return true;

            DialogResult result = MessageBox.Show(
                "Do you want to save changes?",
                "Simple Notepad",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Cancel) return false;
            if (result == DialogResult.No) return true;

            OnSaveClicked(this, EventArgs.Empty);
            return !isDirty;
        }

        private void OnNewClicked(object sender, EventArgs e)
        {
            if (!PromptToSaveIfDirty()) return;

            currentFilePath = "";
            this.Text = "Simple Notepad";
            InitializeDocument("");
            UpdateStatusBar();
        }

        private void OnOpenClicked(object sender, EventArgs e)
        {
            if (!PromptToSaveIfDirty()) return;

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                currentFilePath = dlg.FileName;
                InitializeDocument(File.ReadAllText(currentFilePath));
                this.Text = "Simple Notepad - " + Path.GetFileName(currentFilePath);
                ApplyTheme();
                UpdateStatusBar();
            }
        }

        private void OnSaveClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                OnSaveAsClicked(sender, e);
                return;
            }

            File.WriteAllText(currentFilePath, textArea.Text);
            isDirty = false;
        }

        private void OnSaveAsClicked(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                currentFilePath = dlg.FileName;
                File.WriteAllText(currentFilePath, textArea.Text);
                this.Text = "Simple Notepad - " + Path.GetFileName(currentFilePath);
                isDirty = false;
            }
        }

        private void OnExitClicked(object sender, EventArgs e)
        {
            if (!PromptToSaveIfDirty()) return;
            this.Close();
        }

        // ================= FORMAT / THEMES =================

        private void OnFontClicked(object sender, EventArgs e)
        {
            fontDialog.Font = textArea.Font;
            fontDialog.Color = textArea.ForeColor;

            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                textArea.Font = fontDialog.Font;
                if (showLineNumbers)
                    lineNumberPanel.Invalidate();
            }
        }

        private void SetTheme(ThemeMode theme)
        {
            currentTheme = theme;

            themeLightMenuItem.Checked = (theme == ThemeMode.Light);
            themeDarkMenuItem.Checked = (theme == ThemeMode.Dark);
            themeHackerMenuItem.Checked = (theme == ThemeMode.Hacker);
            themeCyberpunkMenuItem.Checked = (theme == ThemeMode.Cyberpunk);
            themeSynthwaveMenuItem.Checked = (theme == ThemeMode.Synthwave);
            themeSkyBlueMenuItem.Checked = (theme == ThemeMode.SkyBlue);
            themeSoftYellowMenuItem.Checked = (theme == ThemeMode.SoftYellow);

            ApplyTheme();
        }

        private void ApplyTheme()
        {
            Color back;
            Color text;
            Color menuBack;
            Color accent;

            switch (currentTheme)
            {
                case ThemeMode.Light:
                    back = Color.White;
                    text = Color.Black;
                    menuBack = SystemColors.Control;
                    accent = Color.FromArgb(255, 230, 160);
                    break;

                case ThemeMode.Hacker:
                    back = Color.Black;
                    text = Color.Lime;
                    menuBack = Color.FromArgb(20, 20, 20);
                    accent = Color.FromArgb(0, 80, 0);
                    break;

                case ThemeMode.Cyberpunk:
                    back = Color.FromArgb(10, 10, 30);
                    text = Color.FromArgb(0, 255, 200);
                    menuBack = Color.FromArgb(40, 0, 70);
                    accent = Color.FromArgb(80, 0, 120);
                    break;

                case ThemeMode.Synthwave:
                    back = Color.FromArgb(25, 5, 45);
                    text = Color.FromArgb(255, 220, 255);
                    menuBack = Color.FromArgb(60, 10, 90);
                    accent = Color.FromArgb(110, 40, 150);
                    break;

                case ThemeMode.SkyBlue:
                    back = Color.FromArgb(235, 245, 255);
                    text = Color.FromArgb(15, 30, 60);
                    menuBack = Color.FromArgb(210, 230, 250);
                    accent = Color.FromArgb(180, 210, 250);
                    break;

                case ThemeMode.SoftYellow:
                    back = Color.FromArgb(255, 252, 230);
                    text = Color.FromArgb(70, 60, 20);
                    menuBack = Color.FromArgb(250, 240, 200);
                    accent = Color.FromArgb(240, 220, 150);
                    break;

                default: // Dark
                    back = Color.FromArgb(30, 30, 30);
                    text = Color.Gainsboro;
                    menuBack = Color.FromArgb(45, 45, 48);
                    accent = Color.FromArgb(70, 70, 80);
                    break;
            }

            this.BackColor = back;
            contentPanel.BackColor = back;
            textArea.BackColor = back;
            textArea.ForeColor = text;
            textArea.BorderStyle = BorderStyle.FixedSingle;

            if (lineNumberPanel != null)
            {
                lineNumberPanel.BackColor = menuBack;
                if (showLineNumbers)
                    lineNumberPanel.Invalidate();
            }

            if (MainMenuStrip != null)
            {
                MainMenuStrip.BackColor = menuBack;
                MainMenuStrip.ForeColor = text;
                ApplyMenuTheme(MainMenuStrip.Items, menuBack, text);
            }

            if (mainToolStrip != null)
            {
                mainToolStrip.BackColor = menuBack;
                mainToolStrip.ForeColor = text;
                foreach (ToolStripItem item in mainToolStrip.Items)
                {
                    item.BackColor = menuBack;
                    item.ForeColor = text;
                }
            }

            statusStrip.BackColor = menuBack;
            statusStrip.ForeColor = text;

            // Themed hover / pressed highlight
            ThemedColorTable colorTable = new ThemedColorTable(menuBack, text, accent);
            ToolStripProfessionalRenderer renderer = new ToolStripProfessionalRenderer(colorTable);

            if (MainMenuStrip != null)
                MainMenuStrip.Renderer = renderer;
            if (mainToolStrip != null)
                mainToolStrip.Renderer = renderer;
        }

        private void ApplyMenuTheme(ToolStripItemCollection items, Color back, Color fore)
        {
            foreach (ToolStripItem item in items)
            {
                if (item is ToolStripMenuItem mi)
                {
                    mi.BackColor = back;
                    mi.ForeColor = fore;
                }
            }
        }

        // ================= HELP / ABOUT =================

        private void OnAboutClicked(object sender, EventArgs e)
        {
            string message =
                "Simple Notepad\r\n" +
                "----------------------\r\n" +
                "Line numbers, themes, toolbar, find/replace,\r\n" +
                "word/char/line counter and more.\r\n\r\n" +
                "Built by IMITATOR, with a little AI friend. 😉";

            MessageBox.Show(message, "About Simple Notepad",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ================= FIND / REPLACE =================

        private void ShowFindReplace(bool findMode)
        {
            if (findForm == null || findForm.IsDisposed)
                findForm = new FindReplaceForm(this);

            if (!string.IsNullOrEmpty(textArea.SelectedText))
                lastSearchText = textArea.SelectedText;

            findForm.StartPosition = FormStartPosition.CenterParent;
            findForm.SetFindText(lastSearchText);
            findForm.SetModeReplace(!findMode);
            findForm.Show(this);
            findForm.Focus();
        }

        internal void FindNext(string term, bool matchCase)
        {
            if (string.IsNullOrEmpty(term)) return;

            lastSearchText = term;
            string text = textArea.Text;
            if (string.IsNullOrEmpty(text)) return;

            StringComparison cmp = matchCase
                ? StringComparison.CurrentCulture
                : StringComparison.CurrentCultureIgnoreCase;

            int startIndex = textArea.SelectionStart + textArea.SelectionLength;
            if (startIndex > text.Length) startIndex = 0;

            int index = text.IndexOf(term, startIndex, cmp);
            if (index == -1 && startIndex > 0)
                index = text.IndexOf(term, 0, cmp);

            if (index != -1)
            {
                textArea.Focus();
                textArea.SelectionStart = index;
                textArea.SelectionLength = term.Length;
            }
            else
            {
                MessageBox.Show($"Cannot find \"{term}\".", "Find",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        internal void ReplaceCurrent(string term, string replacement, bool matchCase)
        {
            if (string.IsNullOrEmpty(term)) return;

            if (textArea.SelectionLength > 0)
            {
                bool equals = matchCase
                    ? textArea.SelectedText == term
                    : string.Equals(textArea.SelectedText, term,
                        StringComparison.CurrentCultureIgnoreCase);

                if (equals)
                    textArea.SelectedText = replacement;
            }

            FindNext(term, matchCase);
        }

        internal void ReplaceAll(string term, string replacement, bool matchCase)
        {
            if (string.IsNullOrEmpty(term)) return;

            string text = textArea.Text;
            if (string.IsNullOrEmpty(text)) return;

            StringComparison cmp = matchCase
                ? StringComparison.CurrentCulture
                : StringComparison.CurrentCultureIgnoreCase;

            int index = 0;
            bool changed = false;
            StringBuilder result = new StringBuilder();

            while (index < text.Length)
            {
                int found = text.IndexOf(term, index, cmp);
                if (found < 0)
                {
                    result.Append(text, index, text.Length - index);
                    break;
                }

                changed = true;
                result.Append(text, index, found - index);
                result.Append(replacement);
                index = found + term.Length;
            }

            if (changed)
            {
                undoStack.Push(snapshotText);
                redoStack.Clear();

                isInternalChange = true;
                textArea.Text = result.ToString();
                isInternalChange = false;

                snapshotText = textArea.Text;
                isDirty = true;
                UpdateStatusBar();

                if (showLineNumbers)
                    lineNumberPanel.Invalidate();
            }
            else
            {
                MessageBox.Show($"No occurrences of \"{term}\" found.", "Replace All",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    // ================= Find / Replace Form =================

    internal class FindReplaceForm : Form
    {
        private readonly MainForm owner;
        private TextBox txtFind;
        private TextBox txtReplace;
        private CheckBox chkMatchCase;

        public FindReplaceForm(MainForm owner)
        {
            this.owner = owner;

            this.Text = "Find / Replace";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Width = 420;
            this.Height = 200;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            InitializeControls();
        }

        private void InitializeControls()
        {
            Label lblFind = new Label();
            lblFind.Left = 10;
            lblFind.Top = 10;
            lblFind.Width = 80;
            lblFind.Text = "Find what:";

            txtFind = new TextBox();
            txtFind.Left = 100;
            txtFind.Top = 8;
            txtFind.Width = 290;

            Label lblReplace = new Label();
            lblReplace.Left = 10;
            lblReplace.Top = 40;
            lblReplace.Width = 80;
            lblReplace.Text = "Replace with:";

            txtReplace = new TextBox();
            txtReplace.Left = 100;
            txtReplace.Top = 38;
            txtReplace.Width = 290;

            chkMatchCase = new CheckBox();
            chkMatchCase.Left = 10;
            chkMatchCase.Top = 70;
            chkMatchCase.Width = 120;
            chkMatchCase.Text = "Match case";

            Button btnFindNext = new Button();
            btnFindNext.Left = 10;
            btnFindNext.Top = 110;
            btnFindNext.Width = 90;
            btnFindNext.Text = "Find Next";

            Button btnReplace = new Button();
            btnReplace.Left = 110;
            btnReplace.Top = 110;
            btnReplace.Width = 90;
            btnReplace.Text = "Replace";

            Button btnReplaceAll = new Button();
            btnReplaceAll.Left = 210;
            btnReplaceAll.Top = 110;
            btnReplaceAll.Width = 90;
            btnReplaceAll.Text = "Replace All";

            Button btnClose = new Button();
            btnClose.Left = 310;
            btnClose.Top = 110;
            btnClose.Width = 80;
            btnClose.Text = "Close";

            btnFindNext.Click += (s, e) => owner.FindNext(txtFind.Text, chkMatchCase.Checked);
            btnReplace.Click += (s, e) => owner.ReplaceCurrent(txtFind.Text, txtReplace.Text, chkMatchCase.Checked);
            btnReplaceAll.Click += (s, e) => owner.ReplaceAll(txtFind.Text, txtReplace.Text, chkMatchCase.Checked);
            btnClose.Click += (s, e) => this.Hide();

            this.Controls.Add(lblFind);
            this.Controls.Add(txtFind);
            this.Controls.Add(lblReplace);
            this.Controls.Add(txtReplace);
            this.Controls.Add(chkMatchCase);
            this.Controls.Add(btnFindNext);
            this.Controls.Add(btnReplace);
            this.Controls.Add(btnReplaceAll);
            this.Controls.Add(btnClose);

            this.AcceptButton = btnFindNext;
        }

        public void SetFindText(string text)
        {
            if (!string.IsNullOrEmpty(text))
                txtFind.Text = text;

            txtFind.SelectAll();
            txtFind.Focus();
        }

        public void SetModeReplace(bool replaceMode)
        {
            txtReplace.Enabled = replaceMode;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                base.OnFormClosing(e);
            }
        }
    }

    // ================= Themed Color Table =================

    internal class ThemedColorTable : ProfessionalColorTable
    {
        private readonly Color _back;
        private readonly Color _fore;
        private readonly Color _accent;

        public ThemedColorTable(Color back, Color fore, Color accent)
        {
            _back = back;
            _fore = fore;
            _accent = accent;
        }

        public override Color ToolStripGradientBegin => _back;
        public override Color ToolStripGradientMiddle => _back;
        public override Color ToolStripGradientEnd => _back;

        public override Color MenuStripGradientBegin => _back;
        public override Color MenuStripGradientEnd => _back;

        public override Color MenuItemSelected => _accent;
        public override Color MenuItemBorder => _accent;

        public override Color ButtonSelectedHighlight => _accent;
        public override Color ButtonSelectedBorder => _accent;

        public override Color MenuItemPressedGradientBegin => _accent;
        public override Color MenuItemPressedGradientEnd => _accent;

        public override Color ImageMarginGradientBegin => _back;
        public override Color ImageMarginGradientMiddle => _back;
        public override Color ImageMarginGradientEnd => _back;
    }
}
