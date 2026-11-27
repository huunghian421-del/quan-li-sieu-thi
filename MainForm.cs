using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace NeoMartWinForms
{
    public class ProductItem
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class MainForm : Form
    {
        private static readonly Color BgColor = Color.FromArgb(246, 248, 251);
        private static readonly Color SurfaceColor = Color.White;
        private static readonly Color InkColor = Color.FromArgb(15, 23, 42);
        private static readonly Color MutedColor = Color.FromArgb(91, 107, 130);
        private static readonly Color LineColor = Color.FromArgb(203, 213, 225);
        private static readonly Color BrandColor = Color.FromArgb(15, 118, 110);
        private static readonly Color BlueColor = Color.FromArgb(37, 99, 235);

        private static readonly Color OkText = Color.FromArgb(21, 128, 61);
        private static readonly Color OkTint = Color.FromArgb(230, 245, 235);
        private static readonly Color OkBar = Color.FromArgb(22, 163, 74);

        private static readonly Color WarnText = Color.FromArgb(194, 65, 12);
        private static readonly Color WarnTint = Color.FromArgb(253, 235, 221);
        private static readonly Color WarnBar = Color.FromArgb(234, 88, 12);

        private static readonly Color BadText = Color.FromArgb(185, 28, 28);
        private static readonly Color BadTint = Color.FromArgb(252, 230, 230);
        private static readonly Color BadBar = Color.FromArgb(220, 38, 38);

        private readonly string _dataFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sieuthi.txt");
        private readonly CultureInfo _viCulture = new CultureInfo("vi-VN");

        private List<ProductItem> _items = new List<ProductItem>();
        private List<ProductItem> _viewItems = new List<ProductItem>();
        // Lưu lần xoá gần nhất để hỗ trợ hoàn tác (không giữ lịch sử không giới hạn).
        private List<ProductItem> _lastDeletedItems = new List<ProductItem>();
        private string _statusFilter = "all";
        private int _lowThreshold = 10;
        private string _sortColumn = "Code";
        private bool _sortAsc = true;
        private string _editingCode = null;

        private TextBox txtSearch;
        private Button btnToggleAdd;
        private Label lblSync;

        private Panel pnlForm;
        private Label lblFormTitle;
        private TextBox txtCode, txtName;
        private ComboBox cboCategory;
        private NumericUpDown numPrice, numQty;
        private Label lblError;

        private Label lblKpiValue, lblKpiValueSub;
        private Label lblKpiQty, lblKpiQtySub;
        private Label lblKpiMaxName, lblKpiMaxPrice, lblKpiMinName, lblKpiMinPrice;
        private Label lblKpiLow, lblKpiLowSub;
        private Panel cardLow;

        private ComboBox cboFilterCat;
        private Button btnFilterAll, btnFilterOk, btnFilterLow, btnFilterOut;
        private NumericUpDown numThreshold;
        private Label lblCount, lblSelected;
        private Button btnDeleteSelected, btnSelectAll;

        private DataGridView dgv;
        private LinkLabel btnUndoDelete;
        private Timer _searchTimer;

        public MainForm()
        {
            Text = "NEO-MART — Quản lý Siêu thị";
            Size = new Size(1220, 780);
            MinimumSize = new Size(980, 640);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BgColor;
            ForeColor = InkColor;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            EnableDoubleBuffer(this);
            BuildUI();
            LoadData();
            RefreshAll();
        }

        private static void EnableDoubleBuffer(Control ctrl)
        {
            typeof(Control).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, ctrl, new object[] { true });
        }

        private void BuildUI()
        {
            SuspendLayout();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 14, 18, 12),
                BackColor = BgColor,
                ColumnCount = 1,
                RowCount = 6
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 102));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

            // 0. HEADER
            var pnlTop = new Panel { Dock = DockStyle.Fill };

            var picLogo = new PictureBox { Size = new Size(36, 36), Location = new Point(0, 6) };
            picLogo.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var br = new SolidBrush(BrandColor))
                    e.Graphics.FillRectangle(br, 0, 0, 36, 36);
                using (var pen = new Pen(Color.White, 2f))
                {
                    int[] xs = { 7, 11, 16, 19, 24, 28 };
                    foreach (int x in xs) e.Graphics.DrawLine(pen, x, 9, x, 27);
                }
            };

            var lblTitle = new Label
            {
                Text = "NEO-MART",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                Location = new Point(46, 2),
                AutoSize = true
            };
            var lblSub = new Label
            {
                Text = "Kệ hàng tương lai · Quản lý tồn kho",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = MutedColor,
                Location = new Point(47, 26),
                AutoSize = true
            };

            txtSearch = new TextBox
            {
                Width = 320,
                Height = 32,
                Font = new Font("Segoe UI", 10f),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            SetPlaceholder(txtSearch, "Tìm theo mã hoặc tên sản phẩm...");
            _searchTimer = new Timer { Interval = 150 };
            _searchTimer.Tick += (s, e) => { _searchTimer.Stop(); ApplyFilterAndBind(); };
            txtSearch.TextChanged += (s, e) => { _searchTimer.Stop(); _searchTimer.Start(); };

            btnToggleAdd = CreateButton("+ Thêm mặt hàng", BrandColor, Color.White, 150, 34);
            btnToggleAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnToggleAdd.Click += (s, e) => OpenEditor(null);

            lblSync = new Label
            {
                Text = "● sieuthi.txt",
                Font = new Font("Consolas", 9f, FontStyle.Bold),
                ForeColor = OkText,
                BackColor = SurfaceColor,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(125, 34),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            pnlTop.Resize += (s, e) =>
            {
                int right = pnlTop.Width;
                lblSync.Location = new Point(right - lblSync.Width, 7);
                btnToggleAdd.Location = new Point(lblSync.Left - btnToggleAdd.Width - 10, 7);
                txtSearch.Location = new Point(btnToggleAdd.Left - txtSearch.Width - 12, 9);
            };
            pnlTop.Controls.AddRange(new Control[] { picLogo, lblTitle, lblSub, txtSearch, btnToggleAdd, lblSync });
            root.Controls.Add(pnlTop, 0, 0);

            // 1. FORM PANEL
            pnlForm = new Panel
            {
                Dock = DockStyle.Top,
                Height = 135,
                BackColor = SurfaceColor,
                Padding = new Padding(14),
                Visible = false
            };
            pnlForm.Paint += DrawCardBorder;

            lblFormTitle = new Label
            {
                Text = "Thêm mặt hàng mới",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Location = new Point(14, 10),
                AutoSize = true
            };

            var tblFields = new TableLayoutPanel
            {
                Location = new Point(14, 36),
                Size = new Size(1150, 52),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ColumnCount = 5,
                RowCount = 2
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14));

            tblFields.Controls.Add(MakeFieldLabel("Mã SP"), 0, 0);
            tblFields.Controls.Add(MakeFieldLabel("Tên sản phẩm"), 1, 0);
            tblFields.Controls.Add(MakeFieldLabel("Nhóm hàng"), 2, 0);
            tblFields.Controls.Add(MakeFieldLabel("Đơn giá (₫)"), 3, 0);
            tblFields.Controls.Add(MakeFieldLabel("Số lượng"), 4, 0);

            txtCode = new TextBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 10f), MaxLength = 12, CharacterCasing = CharacterCasing.Upper };
            txtName = new TextBox { Dock = DockStyle.Fill, MaxLength = 80 };
            cboCategory = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown };
            numPrice = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 1000000000, Increment = 500, ThousandsSeparator = true, Font = new Font("Consolas", 10f) };
            numQty = new NumericUpDown { Dock = DockStyle.Fill, Maximum = 1000000, Increment = 1, ThousandsSeparator = true, Font = new Font("Consolas", 10f) };

            tblFields.Controls.Add(txtCode, 0, 1);
            tblFields.Controls.Add(txtName, 1, 1);
            tblFields.Controls.Add(cboCategory, 2, 1);
            tblFields.Controls.Add(numPrice, 3, 1);
            tblFields.Controls.Add(numQty, 4, 1);

            var btnSave = CreateButton("Lưu dữ liệu", BrandColor, Color.White, 110, 30);
            btnSave.Location = new Point(14, 94);
            btnSave.Click += BtnSave_Click;

            var btnCancel = CreateButton("Huỷ", SurfaceColor, InkColor, 75, 30);
            btnCancel.Location = new Point(132, 94);
            btnCancel.Click += (s, e) => { pnlForm.Visible = false; _editingCode = null; };

            lblError = new Label
            {
                ForeColor = BadText,
                Location = new Point(220, 100),
                AutoSize = true
            };

            pnlForm.Controls.AddRange(new Control[] { lblFormTitle, tblFields, btnSave, btnCancel, lblError });
            root.Controls.Add(pnlForm, 0, 1);

            // 2. KPI STRIP
            var tblKpi = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 4, 0, 4)
            };
            for (int i = 0; i < 4; i++) tblKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            var c1 = CreateKpiCard("GIÁ TRỊ TỒN KHO", out lblKpiValue, out lblKpiValueSub);
            var c2 = CreateKpiCard("TỔNG SỐ LƯỢNG", out lblKpiQty, out lblKpiQtySub);

            var c3 = new Panel { Dock = DockStyle.Fill, BackColor = SurfaceColor, Margin = new Padding(4, 0, 4, 0), Padding = new Padding(12, 8, 12, 8) };
            c3.Paint += DrawCardBorder;
            var h3 = new Label { Text = "GIÁ CAO / THẤP NHẤT", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = MutedColor, Location = new Point(12, 8), AutoSize = true };
            lblKpiMaxName = new Label { Location = new Point(12, 32), Size = new Size(165, 20), AutoEllipsis = true };
            lblKpiMaxPrice = new Label { Location = new Point(180, 32), Size = new Size(95, 20), Font = new Font("Consolas", 9.5f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            lblKpiMinName = new Label { Location = new Point(12, 56), Size = new Size(165, 20), AutoEllipsis = true };
            lblKpiMinPrice = new Label { Location = new Point(180, 56), Size = new Size(95, 20), Font = new Font("Consolas", 9.5f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            c3.Resize += (s, e) =>
            {
                lblKpiMaxPrice.Left = c3.Width - lblKpiMaxPrice.Width - 12;
                lblKpiMinPrice.Left = c3.Width - lblKpiMinPrice.Width - 12;
                lblKpiMaxName.Width = lblKpiMaxPrice.Left - 16;
                lblKpiMinName.Width = lblKpiMinPrice.Left - 16;
            };
            c3.Controls.AddRange(new Control[] { h3, lblKpiMaxName, lblKpiMaxPrice, lblKpiMinName, lblKpiMinPrice });

            cardLow = CreateKpiCard("CẢNH BÁO SẮP HẾT HÀNG", out lblKpiLow, out lblKpiLowSub);

            c1.Margin = new Padding(0, 0, 4, 0);
            c2.Margin = new Padding(4, 0, 4, 0);
            cardLow.Margin = new Padding(4, 0, 0, 0);

            tblKpi.Controls.Add(c1, 0, 0);
            tblKpi.Controls.Add(c2, 1, 0);
            tblKpi.Controls.Add(c3, 2, 0);
            tblKpi.Controls.Add(cardLow, 3, 0);
            root.Controls.Add(tblKpi, 0, 2);

            // 3. FILTER TOOLBAR
            var pnlTools = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 4) };

            cboFilterCat = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 175,
                Location = new Point(0, 8)
            };
            cboFilterCat.SelectedIndexChanged += (s, e) => ApplyFilterAndBind();

            btnFilterAll = CreateFilterTab("Tất cả", "all", 185, true);
            btnFilterOk = CreateFilterTab("Còn hàng", "ok", 260, false);
            btnFilterLow = CreateFilterTab("Sắp hết", "low", 345, false);
            btnFilterOut = CreateFilterTab("Hết hàng", "out", 425, false);

            var lblTh = new Label
            {
                Text = "Ngưỡng sắp hết ≤",
                ForeColor = MutedColor,
                Location = new Point(520, 12),
                AutoSize = true
            };
            numThreshold = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 10000,
                Value = 10,
                Width = 62,
                Location = new Point(632, 9),
                Font = new Font("Consolas", 9.5f)
            };
            numThreshold.ValueChanged += (s, e) =>
            {
                _lowThreshold = (int)numThreshold.Value;
                UpdateKpis();
                ApplyFilterAndBind();
            };

            lblCount = new Label
            {
                ForeColor = MutedColor,
                TextAlign = ContentAlignment.MiddleRight,
                Size = new Size(160, 24),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlTools.Resize += (s, e) =>
            {
                bool hasRoom = pnlTools.Width >= 1180;
                lblCount.Visible = hasRoom;
                lblSelected.Visible = hasRoom;
                if (hasRoom)
                {
                    lblCount.Location = new Point(pnlTools.Width - lblCount.Width, 10);
                    lblSelected.Location = new Point(920, 12);
                }
            };

            btnSelectAll = CreateButton("Chọn tất cả", SurfaceColor, InkColor, 94, 28);
            btnSelectAll.Location = new Point(710, 7);
            btnSelectAll.Click += (s, e) => ToggleSelectAll();
            btnDeleteSelected = CreateButton("Xoá đã chọn", BadText, Color.White, 105, 28);
            btnDeleteSelected.Location = new Point(812, 7);
            btnDeleteSelected.Enabled = false;
            btnDeleteSelected.Click += (s, e) => DeleteSelected();
            lblSelected = new Label { Text = "Đã chọn: 0", ForeColor = MutedColor, AutoSize = true, Location = new Point(925, 12) };

            pnlTools.Controls.AddRange(new Control[] { cboFilterCat, btnFilterAll, btnFilterOk, btnFilterLow, btnFilterOut, lblTh, numThreshold, btnSelectAll, btnDeleteSelected, lblSelected, lblCount });
            root.Controls.Add(pnlTools, 0, 3);

            // 4. DATA GRID VIEW
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = SurfaceColor,
                BorderStyle = BorderStyle.FixedSingle,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(228, 234, 242),
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 38,
                RowTemplate = { Height = 42 }
            };
            EnableDoubleBuffer(dgv);

            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = BgColor,
                ForeColor = MutedColor,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Padding = new Padding(8, 0, 8, 0),
                SelectionBackColor = BgColor
            };
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = SurfaceColor,
                ForeColor = InkColor,
                SelectionBackColor = Color.FromArgb(227, 243, 241),
                SelectionForeColor = InkColor,
                Padding = new Padding(8, 0, 8, 0)
            };

            var colSelect = new DataGridViewCheckBoxColumn
            {
                Name = "Select", HeaderText = "✓", FillWeight = 4,
                ThreeState = false, FalseValue = false, TrueValue = true,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ReadOnly = false
            };
            dgv.Columns.Add(colSelect);
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Code", HeaderText = "MÃ SP", FillWeight = 12, ReadOnly = true, DefaultCellStyle = { Font = new Font("Consolas", 9.5f, FontStyle.Bold) } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "TÊN SẢN PHẨM", FillWeight = 30, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "NHÓM HÀNG", FillWeight = 16, ReadOnly = true, DefaultCellStyle = { ForeColor = MutedColor } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Price", HeaderText = "ĐƠN GIÁ", FillWeight = 14, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Consolas", 9.5f) } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "SỐ LƯỢNG", FillWeight = 10, ReadOnly = true, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Consolas", 9.5f) } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Stock", HeaderText = "TỒN KHO", FillWeight = 16, ReadOnly = true });

            var colEdit = new DataGridViewButtonColumn { Name = "Edit", HeaderText = "", Text = "Sửa", UseColumnTextForButtonValue = true, FillWeight = 6, FlatStyle = FlatStyle.Flat, ReadOnly = true };
            colEdit.DefaultCellStyle.ForeColor = BlueColor;
            var colDel = new DataGridViewButtonColumn { Name = "Delete", HeaderText = "", Text = "Xoá", UseColumnTextForButtonValue = true, FillWeight = 6, FlatStyle = FlatStyle.Flat, ReadOnly = true };
            colDel.DefaultCellStyle.ForeColor = BadText;

            dgv.Columns.Add(colEdit);
            dgv.Columns.Add(colDel);

            dgv.ColumnHeaderMouseClick += Dgv_ColumnHeaderMouseClick;
            dgv.CellPainting += Dgv_CellPainting;
            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgv.IsCurrentCellDirty) dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgv.CellValueChanged += (s, e) => { if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgv.Columns[e.ColumnIndex].Name == "Select") UpdateSelectionUi(); };
            dgv.KeyDown += Dgv_KeyDown;

            root.Controls.Add(dgv, 0, 4);

            // 5. FOOTER
            var pnlFoot = new Panel { Dock = DockStyle.Fill };
            var lblTagline = new Label
            {
                Text = "Chuẩn từng mã vạch — Mượt từng thao tác. Tự động đồng bộ sieuthi.txt",
                ForeColor = MutedColor,
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(0, 8),
                AutoSize = true
            };

            var btnReset = new LinkLabel
            {
                Text = "Khôi phục dữ liệu mẫu",
                LinkColor = BlueColor,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnReset.LinkClicked += (s, e) =>
            {
                if (MessageBox.Show("Khôi phục lại 30 mặt hàng mẫu ban đầu?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _items = GetSampleItems();
                    _lastDeletedItems.Clear();
                    UpdateUndoUi();
                    SaveData();
                    RefreshAll();
                }
            };

            btnUndoDelete = new LinkLabel
            {
                Text = "↶ Hoàn tác xoá",
                LinkColor = MutedColor,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                AutoSize = true,
                Enabled = false
            };
            btnUndoDelete.LinkClicked += (s, e) => UndoLastDelete();

            var btnExport = new LinkLabel
            {
                Text = "Mở thư mục chứa sieuthi.txt",
                LinkColor = BlueColor,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnExport.LinkClicked += (s, e) =>
            {
                SaveData();
                System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + _dataFile + "\"");
            };

            pnlFoot.Resize += (s, e) =>
            {
                btnReset.Location = new Point(pnlFoot.Width - btnReset.Width, 8);
                btnExport.Location = new Point(btnReset.Left - btnExport.Width - 16, 8);
                btnUndoDelete.Location = new Point(btnExport.Left - btnUndoDelete.Width - 16, 8);
            };
            pnlFoot.Controls.AddRange(new Control[] { lblTagline, btnUndoDelete, btnExport, btnReset });
            root.Controls.Add(pnlFoot, 0, 5);

            Controls.Add(root);
            ResumeLayout();
        }

        private void Dgv_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _viewItems.Count || e.ColumnIndex != dgv.Columns["Stock"].Index) return;

            e.PaintBackground(e.CellBounds, true);
            var item = _viewItems[e.RowIndex];
            string status = GetStatus(item.Quantity);

            Color pillBg = status == "out" ? BadTint : (status == "low" ? WarnTint : OkTint);
            Color pillFg = status == "out" ? BadText : (status == "low" ? WarnText : OkText);
            Color barFg = status == "out" ? BadBar : (status == "low" ? WarnBar : OkBar);
            string label = status == "out" ? "Hết hàng" : (status == "low" ? "Sắp hết" : "Còn hàng");

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var pillRect = new Rectangle(e.CellBounds.X + 8, e.CellBounds.Y + 6, 72, 18);
            using (var br = new SolidBrush(pillBg))
                g.FillRectangle(br, pillRect);
            TextRenderer.DrawText(g, label, new Font("Segoe UI", 8f, FontStyle.Bold), pillRect, pillFg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            int barWidth = e.CellBounds.Width - 18;
            var trackRect = new Rectangle(e.CellBounds.X + 8, e.CellBounds.Y + 29, barWidth, 4);
            using (var trackBr = new SolidBrush(Color.FromArgb(228, 234, 242)))
                g.FillRectangle(trackBr, trackRect);

            if (item.Quantity > 0)
            {
                double ratio = Math.Min(1.0, Math.Max(0.05, item.Quantity / (_lowThreshold * 4.0)));
                var fillRect = new Rectangle(trackRect.X, trackRect.Y, (int)(barWidth * ratio), 4);
                using (var barBr = new SolidBrush(barFg))
                    g.FillRectangle(barBr, fillRect);
            }

            e.Handled = true;
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _viewItems.Count) return;
            var item = _viewItems[e.RowIndex];

            if (e.ColumnIndex == dgv.Columns["Select"].Index)
            {
                UpdateSelectionUi();
                return;
            }
            if (e.ColumnIndex == dgv.Columns["Edit"].Index)
            {
                OpenEditor(item);
            }
            else if (e.ColumnIndex == dgv.Columns["Delete"].Index)
            {
                if (MessageBox.Show($"Xoá mặt hàng [{item.Code}] {item.Name}?", "Xác nhận xoá",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    DeleteItems(new List<string> { item.Code });
                }
            }
        }

        private List<string> GetSelectedCodes()
        {
            return dgv.Rows.Cast<DataGridViewRow>()
                .Where(r => r.Cells["Select"].Value is bool selected && selected)
                .Select(r => Convert.ToString(r.Tag))
                .Where(code => !string.IsNullOrEmpty(code)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private void UpdateSelectionUi()
        {
            if (dgv == null || lblSelected == null) return;
            int count = GetSelectedCodes().Count;
            lblSelected.Text = "Đã chọn: " + count;
            btnDeleteSelected.Enabled = count > 0;
            btnSelectAll.Text = count == dgv.Rows.Count && count > 0 ? "Bỏ chọn" : "Chọn tất cả";
        }

        private void ToggleSelectAll()
        {
            bool select = GetSelectedCodes().Count != dgv.Rows.Count || dgv.Rows.Count == 0;
            dgv.SuspendLayout();
            foreach (DataGridViewRow row in dgv.Rows) row.Cells["Select"].Value = select;
            dgv.ResumeLayout();
            UpdateSelectionUi();
        }

        private void DeleteSelected()
        {
            var codes = GetSelectedCodes();
            if (codes.Count == 0) return;
            if (MessageBox.Show($"Bạn chắc chắn muốn xoá {codes.Count} mặt hàng đã chọn? Bạn có thể hoàn tác ngay sau khi xoá.",
                "Xác nhận xoá hàng loạt", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            DeleteItems(codes);
        }

        private void DeleteItems(List<string> codes)
        {
            if (codes == null || codes.Count == 0) return;
            var codeSet = new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
            var deleted = _items
                .Where(x => codeSet.Contains(x.Code))
                .Select(x => new ProductItem
                {
                    Code = x.Code, Name = x.Name, Category = x.Category,
                    Price = x.Price, Quantity = x.Quantity
                }).ToList();

            if (deleted.Count == 0) return;
            _lastDeletedItems = deleted;
            _items.RemoveAll(x => codeSet.Contains(x.Code));
            SaveData();
            UpdateUndoUi();
            RefreshAll();
        }

        private void UndoLastDelete()
        {
            if (_lastDeletedItems.Count == 0) return;

            var existingCodes = new HashSet<string>(
                _items.Select(x => x.Code), StringComparer.OrdinalIgnoreCase);
            var restore = _lastDeletedItems
                .Where(x => !existingCodes.Contains(x.Code))
                .ToList();
            int skipped = _lastDeletedItems.Count - restore.Count;

            if (restore.Count == 0)
            {
                MessageBox.Show("Không thể khôi phục vì mã sản phẩm đã tồn tại.",
                    "Hoàn tác xoá", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _items.AddRange(restore);
            _lastDeletedItems.Clear();
            SaveData();
            UpdateUndoUi();
            RefreshAll();

            string message = $"Đã khôi phục {restore.Count} mặt hàng.";
            if (skipped > 0) message += $" Bỏ qua {skipped} mặt hàng do mã đã tồn tại.";
            MessageBox.Show(message, "Hoàn tác thành công",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateUndoUi()
        {
            if (btnUndoDelete == null) return;
            btnUndoDelete.Enabled = _lastDeletedItems.Count > 0;
            btnUndoDelete.LinkColor = btnUndoDelete.Enabled ? BlueColor : MutedColor;
            btnUndoDelete.Text = _lastDeletedItems.Count > 0
                ? $"↶ Hoàn tác xoá ({_lastDeletedItems.Count})"
                : "↶ Hoàn tác xoá";
        }

        private void Dgv_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z && _lastDeletedItems.Count > 0)
            {
                UndoLastDelete();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.A)
            {
                ToggleSelectAll();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Delete && GetSelectedCodes().Count > 0)
            {
                DeleteSelected();
                e.Handled = true;
            }
        }

        private void Dgv_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            string colName = dgv.Columns[e.ColumnIndex].Name;
            if (colName == "Edit" || colName == "Delete" || colName == "Select") return;

            if (_sortColumn == colName) _sortAsc = !_sortAsc;
            else { _sortColumn = colName; _sortAsc = true; }

            ApplyFilterAndBind();
        }

        private void OpenEditor(ProductItem item)
        {
            _editingCode = item?.Code;
            lblFormTitle.Text = item == null ? "Thêm mặt hàng mới" : $"Sửa mặt hàng: {item.Code}";
            txtCode.Text = item?.Code ?? "";
            txtCode.ReadOnly = item != null;
            txtCode.BackColor = item != null ? BgColor : SurfaceColor;
            txtName.Text = item?.Name ?? "";
            cboCategory.Text = item?.Category ?? "";
            numPrice.Value = item?.Price ?? 0;
            numQty.Value = item?.Quantity ?? 0;
            lblError.Text = "";
            pnlForm.Visible = true;
            if (item == null) txtCode.Focus(); else txtName.Focus();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string code = txtCode.Text.Trim().ToUpper();
            string name = txtName.Text.Trim();
            string cat = string.IsNullOrWhiteSpace(cboCategory.Text) ? "Khác" : cboCategory.Text.Trim();
            decimal price = numPrice.Value;
            int qty = (int)numQty.Value;

            if (string.IsNullOrEmpty(code)) { lblError.Text = "Lỗi: Không được để trống mã sản phẩm."; return; }
            if (code.Contains("|") || name.Contains("|") || cat.Contains("|")) { lblError.Text = "Lỗi: Không được dùng ký tự '|'."; return; }
            if (string.IsNullOrEmpty(name)) { lblError.Text = "Lỗi: Không được để trống tên sản phẩm."; return; }
            if (_editingCode == null && _items.Any(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            {
                lblError.Text = $"Lỗi: Mã sản phẩm '{code}' đã tồn tại trong kho.";
                return;
            }

            if (_editingCode != null)
            {
                var existing = _items.FirstOrDefault(x => x.Code == _editingCode);
                if (existing != null)
                {
                    existing.Name = name;
                    existing.Category = cat;
                    existing.Price = price;
                    existing.Quantity = qty;
                }
            }
            else
            {
                _items.Add(new ProductItem { Code = code, Name = name, Category = cat, Price = price, Quantity = qty });
            }

            SaveData();
            pnlForm.Visible = false;
            _editingCode = null;
            RefreshAll();
        }

        private void RefreshAll()
        {
            var cats = _items.Select(x => x.Category).Distinct().OrderBy(x => x).ToList();
            string currentFilter = cboFilterCat.SelectedItem?.ToString() ?? "Mọi nhóm hàng";

            cboFilterCat.BeginUpdate();
            cboFilterCat.Items.Clear();
            cboFilterCat.Items.Add("Mọi nhóm hàng");
            foreach (var c in cats) cboFilterCat.Items.Add(c);
            cboFilterCat.SelectedItem = cats.Contains(currentFilter) ? currentFilter : "Mọi nhóm hàng";
            cboFilterCat.EndUpdate();

            cboCategory.Items.Clear();
            foreach (var c in cats) cboCategory.Items.Add(c);

            UpdateKpis();
            ApplyFilterAndBind();
        }

        private void UpdateKpis()
        {
            decimal totalValue = _items.Sum(x => x.Price * x.Quantity);
            int totalQty = _items.Sum(x => x.Quantity);
            int lowCount = _items.Count(x => x.Quantity > 0 && x.Quantity <= _lowThreshold);
            int outCount = _items.Count(x => x.Quantity == 0);

            var maxItem = _items.OrderByDescending(x => x.Price).FirstOrDefault();
            var minItem = _items.OrderBy(x => x.Price).FirstOrDefault();

            lblKpiValue.Text = FormatMoney(totalValue);
            lblKpiValueSub.Text = $"{_items.Count} mặt hàng trong danh mục";

            lblKpiQty.Text = totalQty.ToString("N0", _viCulture);
            lblKpiQtySub.Text = "đơn vị hàng hoá";

            lblKpiMaxName.Text = maxItem != null ? $"Cao: {maxItem.Name}" : "—";
            lblKpiMaxPrice.Text = maxItem != null ? FormatMoney(maxItem.Price) : "";
            lblKpiMinName.Text = minItem != null ? $"Thấp: {minItem.Name}" : "—";
            lblKpiMinPrice.Text = minItem != null ? FormatMoney(minItem.Price) : "";

            lblKpiLow.Text = (lowCount + outCount).ToString();
            lblKpiLow.ForeColor = (lowCount + outCount) > 0 ? WarnText : OkText;
            lblKpiLowSub.Text = $"{outCount} hết hàng · {lowCount} sắp hết (≤ {_lowThreshold})";
        }

        private void ApplyFilterAndBind()
        {
            string q = txtSearch.Text.Trim();
            string cat = cboFilterCat.SelectedIndex > 0 ? cboFilterCat.SelectedItem.ToString() : "";

            IEnumerable<ProductItem> query = _items;

            if (!string.IsNullOrEmpty(cat))
                query = query.Where(x => x.Category == cat);

            if (_statusFilter != "all")
                query = query.Where(x => GetStatus(x.Quantity) == _statusFilter);

            if (!string.IsNullOrEmpty(q))
                query = query.Where(x => x.Code.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         x.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);

            switch (_sortColumn)
            {
                case "Name":
                    query = _sortAsc ? query.OrderBy(x => x.Name) : query.OrderByDescending(x => x.Name);
                    break;
                case "Category":
                    query = _sortAsc ? query.OrderBy(x => x.Category) : query.OrderByDescending(x => x.Category);
                    break;
                case "Price":
                    query = _sortAsc ? query.OrderBy(x => x.Price) : query.OrderByDescending(x => x.Price);
                    break;
                case "Quantity":
                case "Stock":
                    query = _sortAsc ? query.OrderBy(x => x.Quantity) : query.OrderByDescending(x => x.Quantity);
                    break;
                default:
                    query = _sortAsc ? query.OrderBy(x => x.Code) : query.OrderByDescending(x => x.Code);
                    break;
            }

            _viewItems = query.ToList();

            dgv.SuspendLayout();
            dgv.Rows.Clear();
            foreach (var it in _viewItems)
            {
                int rowIndex = dgv.Rows.Add(false, it.Code, it.Name, it.Category, FormatMoney(it.Price), it.Quantity.ToString("N0", _viCulture), "");
                dgv.Rows[rowIndex].Tag = it.Code;
            }
            dgv.ResumeLayout();

            lblCount.Text = $"Hiển thị {_viewItems.Count} / {_items.Count} mặt hàng";
            UpdateSelectionUi();
        }

        private string GetStatus(int qty)
        {
            if (qty == 0) return "out";
            if (qty <= _lowThreshold) return "low";
            return "ok";
        }

        private string FormatMoney(decimal amount) => amount.ToString("N0", _viCulture) + " ₫";

        private void LoadData()
        {
            if (!File.Exists(_dataFile))
            {
                _items = GetSampleItems();
                SaveData();
                return;
            }

            var list = new List<ProductItem>();
            foreach (var rawLine in File.ReadAllLines(_dataFile, Encoding.UTF8))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                var parts = line.Split('|');
                if (parts.Length < 5) continue;

                if (decimal.TryParse(parts[3], out decimal price) && int.TryParse(parts[4], out int qty))
                {
                    list.Add(new ProductItem
                    {
                        Code = parts[0].Trim(),
                        Name = parts[1].Trim(),
                        Category = parts[2].Trim(),
                        Price = price,
                        Quantity = qty
                    });
                }
            }
            _items = list;
        }

        private void SaveData()
        {
            var sb = new StringBuilder();
            foreach (var it in _items)
            {
                sb.AppendLine($"{it.Code}|{it.Name}|{it.Category}|{it.Price}|{it.Quantity}");
            }
            string tempFile = _dataFile + ".tmp";
            File.WriteAllText(tempFile, sb.ToString(), new UTF8Encoding(true));
            if (File.Exists(_dataFile))
            {
                string backupFile = _dataFile + ".bak";
                try { File.Replace(tempFile, _dataFile, backupFile, true); }
                catch
                {
                    File.Copy(tempFile, _dataFile, true);
                    File.Delete(tempFile);
                }
            }
            else File.Move(tempFile, _dataFile);
        }

        private List<ProductItem> GetSampleItems()
        {
            return new List<ProductItem>
            {
                new ProductItem { Code = "SP001", Name = "Sữa tươi tiệt trùng 1L", Category = "Sữa & trứng", Price = 34500, Quantity = 48 },
                new ProductItem { Code = "SP002", Name = "Sữa chua có đường hộp 100g", Category = "Sữa & trứng", Price = 7500, Quantity = 120 },
                new ProductItem { Code = "SP003", Name = "Trứng gà hộp 10 quả", Category = "Sữa & trứng", Price = 38000, Quantity = 9 },
                new ProductItem { Code = "SP004", Name = "Gạo thơm ST25 túi 5kg", Category = "Lương thực", Price = 142000, Quantity = 36 },
                new ProductItem { Code = "SP005", Name = "Mì gói hảo hảo thùng 30 gói", Category = "Lương thực", Price = 118000, Quantity = 22 },
                new ProductItem { Code = "SP006", Name = "Bún khô gói 300g", Category = "Lương thực", Price = 16500, Quantity = 7 },
                new ProductItem { Code = "SP007", Name = "Dầu ăn đậu nành chai 1L", Category = "Gia vị", Price = 54000, Quantity = 40 },
                new ProductItem { Code = "SP008", Name = "Nước mắm cá cơm chai 500ml", Category = "Gia vị", Price = 46000, Quantity = 31 },
                new ProductItem { Code = "SP009", Name = "Đường tinh luyện gói 1kg", Category = "Gia vị", Price = 27000, Quantity = 5 },
                new ProductItem { Code = "SP010", Name = "Muối i-ốt gói 500g", Category = "Gia vị", Price = 6500, Quantity = 64 },
                new ProductItem { Code = "SP011", Name = "Nước suối chai 500ml lốc 12", Category = "Đồ uống", Price = 48000, Quantity = 52 },
                new ProductItem { Code = "SP012", Name = "Cà phê hoà tan hộp 20 gói", Category = "Đồ uống", Price = 69000, Quantity = 18 },
                new ProductItem { Code = "SP013", Name = "Trà xanh không độ chai 455ml", Category = "Đồ uống", Price = 10500, Quantity = 0 },
                new ProductItem { Code = "SP014", Name = "Nước ngọt lon 330ml lốc 6", Category = "Đồ uống", Price = 62000, Quantity = 26 },
                new ProductItem { Code = "SP015", Name = "Bia lon 330ml thùng 24", Category = "Đồ uống", Price = 395000, Quantity = 8 },
                new ProductItem { Code = "SP016", Name = "Thịt ba rọi khay 500g", Category = "Thịt & hải sản", Price = 89000, Quantity = 14 },
                new ProductItem { Code = "SP017", Name = "Cá basa phi lê gói 500g", Category = "Thịt & hải sản", Price = 72000, Quantity = 6 },
                new ProductItem { Code = "SP018", Name = "Tôm sú đông lạnh hộp 500g", Category = "Thịt & hải sản", Price = 165000, Quantity = 11 },
                new ProductItem { Code = "SP019", Name = "Cà chua túi 500g", Category = "Rau củ quả", Price = 19000, Quantity = 25 },
                new ProductItem { Code = "SP020", Name = "Chuối sứ bó 1kg", Category = "Rau củ quả", Price = 24000, Quantity = 17 },
                new ProductItem { Code = "SP021", Name = "Rau muống bó 300g", Category = "Rau củ quả", Price = 9000, Quantity = 3 },
                new ProductItem { Code = "SP022", Name = "Bánh quy bơ hộp 400g", Category = "Bánh kẹo", Price = 58000, Quantity = 29 },
                new ProductItem { Code = "SP023", Name = "Kẹo dẻo trái cây gói 150g", Category = "Bánh kẹo", Price = 21000, Quantity = 44 },
                new ProductItem { Code = "SP024", Name = "Snack khoai tây gói 52g", Category = "Bánh kẹo", Price = 12500, Quantity = 0 },
                new ProductItem { Code = "SP025", Name = "Nước rửa chén chai 750ml", Category = "Gia dụng", Price = 33500, Quantity = 38 },
                new ProductItem { Code = "SP026", Name = "Bột giặt túi 3kg", Category = "Gia dụng", Price = 139000, Quantity = 12 },
                new ProductItem { Code = "SP027", Name = "Giấy vệ sinh lốc 10 cuộn", Category = "Gia dụng", Price = 78000, Quantity = 41 },
                new ProductItem { Code = "SP028", Name = "Dầu gội chai 650g", Category = "Chăm sóc cá nhân", Price = 149000, Quantity = 10 },
                new ProductItem { Code = "SP029", Name = "Kem đánh răng tuýp 180g", Category = "Chăm sóc cá nhân", Price = 44500, Quantity = 57 },
                new ProductItem { Code = "SP030", Name = "Khẩu trang y tế hộp 50 cái", Category = "Chăm sóc cá nhân", Price = 32000, Quantity = 4 }
            };
        }

        private Panel CreateKpiCard(string title, out Label bigLabel, out Label subLabel)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                Padding = new Padding(14, 10, 14, 10)
            };
            card.Paint += DrawCardBorder;

            var lblH = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = MutedColor,
                Location = new Point(14, 10),
                AutoSize = true
            };
            bigLabel = new Label
            {
                Text = "0",
                Font = new Font("Consolas", 16f, FontStyle.Bold),
                Location = new Point(12, 32),
                AutoSize = true
            };
            subLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = MutedColor,
                Location = new Point(14, 64),
                AutoSize = true
            };
            card.Controls.AddRange(new Control[] { lblH, bigLabel, subLabel });
            return card;
        }

        private Button CreateFilterTab(string text, string statusKey, int x, bool active)
        {
            var btn = new Button
            {
                Text = text,
                Tag = statusKey,
                Location = new Point(x, 7),
                Size = new Size(76, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, active ? FontStyle.Bold : FontStyle.Regular),
                BackColor = active ? BrandColor : SurfaceColor,
                ForeColor = active ? Color.White : InkColor,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = active ? BrandColor : LineColor;
            btn.Click += (s, e) =>
            {
                _statusFilter = statusKey;
                foreach (Button b in new[] { btnFilterAll, btnFilterOk, btnFilterLow, btnFilterOut })
                {
                    bool isSel = (string)b.Tag == _statusFilter;
                    b.BackColor = isSel ? BrandColor : SurfaceColor;
                    b.ForeColor = isSel ? Color.White : InkColor;
                    b.Font = new Font("Segoe UI", 8.5f, isSel ? FontStyle.Bold : FontStyle.Regular);
                    b.FlatAppearance.BorderColor = isSel ? BrandColor : LineColor;
                }
                ApplyFilterAndBind();
            };
            return btn;
        }

        private static Button CreateButton(string text, Color back, Color fore, int w, int h)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(w, h),
                BackColor = back,
                ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = back == SurfaceColor ? LineColor : back;
            return btn;
        }

        private static Label MakeFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = MutedColor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };
        }

        private static void DrawCardBorder(object sender, PaintEventArgs e)
        {
            if (sender is Control c)
            {
                using (var pen = new Pen(LineColor, 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, c.Width - 1, c.Height - 1);
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, string lp);

        private static void SetPlaceholder(TextBox tb, string placeholder)
        {
            tb.HandleCreated += (s, e) => SendMessage(tb.Handle, 0x1501, (IntPtr)1, placeholder);
        }
    }
}
