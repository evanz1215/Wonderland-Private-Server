using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DataFiles;
using Server;

namespace Wonderland_Private_Server
{
    /// <summary>
    /// Programmatically-built panel for managing the Item Mall.
    /// Added to the main TabControl at runtime.
    /// </summary>
    public class ImMallPanel
    {
        readonly ImMallManager _manager;
        readonly PhxItemDat _itemDat;

        DataGridView _grid;
        ComboBox _tabFilter;
        TextBox _txtItemID;
        TextBox _txtPrice;
        TextBox _txtAmount;
        TextBox _txtDiscount;
        ComboBox _cmbTab;
        Label _lblItemName;
        Label _lblStatus;

        public ImMallPanel(ImMallManager manager, PhxItemDat itemDat)
        {
            _manager = manager;
            _itemDat = itemDat;
        }

        /// <summary>
        /// Creates the TabPage with all controls and returns it.
        /// </summary>
        public TabPage CreateTabPage()
        {
            var page = new TabPage("\u5546\u57CE\u7BA1\u7406");
            page.UseVisualStyleBackColor = true;

            // ── Main split: left = grid, right = controls ──
            var splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Orientation = Orientation.Vertical;
            splitContainer.SplitterDistance = 520;
            splitContainer.FixedPanel = FixedPanel.Panel2;

            // ── Left panel: filter + grid ──
            var leftPanel = splitContainer.Panel1;

            var filterPanel = new Panel();
            filterPanel.Dock = DockStyle.Top;
            filterPanel.Height = 30;

            var lblFilter = new Label { Text = "\u7BE9\u9078\u5206\u9801\uFF1A", AutoSize = true, Location = new Point(5, 7) };
            _tabFilter = new ComboBox();
            _tabFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            _tabFilter.Location = new Point(75, 3);
            _tabFilter.Width = 130;
            _tabFilter.Items.AddRange(new object[] { "\u5168\u90E8", "1-\u6B66\u5668", "2-\u88DD\u5099", "3-\u71B1\u9580", "4-\u96DC\u8CA8", "5-\u5BB6\u5177" });
            _tabFilter.SelectedIndex = 0;
            _tabFilter.SelectedIndexChanged += TabFilter_Changed;

            _lblStatus = new Label { Text = "\u5546\u54C1\u6578\uFF1A0", AutoSize = true, Location = new Point(215, 7) };

            filterPanel.Controls.Add(lblFilter);
            filterPanel.Controls.Add(_tabFilter);
            filterPanel.Controls.Add(_lblStatus);

            _grid = new DataGridView();
            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.ReadOnly = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.DataSource = _manager.Items;
            _grid.CellEndEdit += Grid_CellEndEdit;

            leftPanel.Controls.Add(_grid);
            leftPanel.Controls.Add(filterPanel);

            // ── Right panel: add/remove controls ──
            var rightPanel = splitContainer.Panel2;
            int y = 10;

            var grpAdd = new GroupBox();
            grpAdd.Text = "\u4E0A\u67B6\u5546\u54C1";
            grpAdd.Dock = DockStyle.Top;
            grpAdd.Height = 250;

            y = 20;
            grpAdd.Controls.Add(MakeLabel("\u7269\u54C1 ID\uFF1A", 10, y));
            _txtItemID = new TextBox { Location = new Point(80, y - 3), Width = 80 };
            _txtItemID.TextChanged += TxtItemID_Changed;
            grpAdd.Controls.Add(_txtItemID);

            _lblItemName = new Label { Text = "(\u8ACB\u8F38\u5165 ID)", Location = new Point(10, y + 20), AutoSize = true, ForeColor = Color.Blue };
            grpAdd.Controls.Add(_lblItemName);
            y += 45;

            grpAdd.Controls.Add(MakeLabel("\u5206\u9801\uFF1A", 10, y));
            _cmbTab = new ComboBox { Location = new Point(80, y - 3), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbTab.Items.AddRange(new object[] { "1-\u6B66\u5668", "2-\u88DD\u5099", "3-\u71B1\u9580", "4-\u96DC\u8CA8", "5-\u5BB6\u5177" });
            _cmbTab.SelectedIndex = 0;
            grpAdd.Controls.Add(_cmbTab);
            y += 30;

            grpAdd.Controls.Add(MakeLabel("\u50F9\u683C (IM)\uFF1A", 10, y));
            _txtPrice = new TextBox { Location = new Point(80, y - 3), Width = 80, Text = "100" };
            grpAdd.Controls.Add(_txtPrice);
            y += 30;

            grpAdd.Controls.Add(MakeLabel("\u6578\u91CF\uFF1A", 10, y));
            _txtAmount = new TextBox { Location = new Point(80, y - 3), Width = 80, Text = "1" };
            grpAdd.Controls.Add(_txtAmount);
            y += 30;

            grpAdd.Controls.Add(MakeLabel("\u6298\u6263(%)\uFF1A", 10, y));
            _txtDiscount = new TextBox { Location = new Point(80, y - 3), Width = 80, Text = "100" };
            grpAdd.Controls.Add(_txtDiscount);
            y += 35;

            var btnAdd = new Button { Text = "\u4E0A\u67B6", Location = new Point(10, y), Width = 120, Height = 28 };
            btnAdd.Click += BtnAdd_Click;
            grpAdd.Controls.Add(btnAdd);

            // Action buttons
            var grpActions = new GroupBox();
            grpActions.Text = "\u64CD\u4F5C";
            grpActions.Dock = DockStyle.Top;
            grpActions.Height = 160;

            y = 22;
            var btnRemove = new Button { Text = "\u4E0B\u67B6\u9078\u53D6\u7684\u5546\u54C1", Location = new Point(10, y), Width = 150, Height = 28 };
            btnRemove.Click += BtnRemove_Click;
            grpActions.Controls.Add(btnRemove);
            y += 35;

            var btnSave = new Button { Text = "\u5132\u5B58\u8A2D\u5B9A", Location = new Point(10, y), Width = 150, Height = 28 };
            btnSave.Click += BtnSave_Click;
            grpActions.Controls.Add(btnSave);
            y += 35;

            var btnAutoPopulate = new Button { Text = "\u81EA\u52D5\u586B\u5145\u5546\u54C1", Location = new Point(10, y), Width = 150, Height = 28 };
            btnAutoPopulate.Click += BtnAutoPopulate_Click;
            grpActions.Controls.Add(btnAutoPopulate);
            y += 35;

            var btnClearAll = new Button { Text = "\u6E05\u7A7A\u5168\u90E8", Location = new Point(10, y), Width = 150, Height = 28, ForeColor = Color.Red };
            btnClearAll.Click += BtnClearAll_Click;
            grpActions.Controls.Add(btnClearAll);

            // Stack the groups in right panel (add Actions first so it's below Add)
            rightPanel.Controls.Add(grpActions);
            rightPanel.Controls.Add(grpAdd);

            page.Controls.Add(splitContainer);

            UpdateStatus();
            return page;
        }

        Label MakeLabel(string text, int x, int y)
        {
            return new Label { Text = text, AutoSize = true, Location = new Point(x, y) };
        }

        void TxtItemID_Changed(object sender, EventArgs e)
        {
            ushort id;
            if (ushort.TryParse(_txtItemID.Text, out id) && id > 0 && _itemDat != null)
            {
                try
                {
                    var info = _itemDat.GetItemByID(id);
                    if (info != null && info.ItemID > 0)
                    {
                        string name = Encoding.ASCII.GetString(info.ItemName).TrimEnd('\0');
                        _lblItemName.Text = name;
                        _lblItemName.ForeColor = Color.Blue;
                    }
                    else
                    {
                        _lblItemName.Text = "(\u627E\u4E0D\u5230\u6B64\u7269\u54C1)";
                        _lblItemName.ForeColor = Color.Red;
                    }
                }
                catch
                {
                    _lblItemName.Text = "(\u627E\u4E0D\u5230\u6B64\u7269\u54C1)";
                    _lblItemName.ForeColor = Color.Red;
                }
            }
            else
            {
                _lblItemName.Text = "(\u8ACB\u8F38\u5165 ID)";
                _lblItemName.ForeColor = Color.Gray;
            }
        }

        void BtnAdd_Click(object sender, EventArgs e)
        {
            ushort itemID;
            if (!ushort.TryParse(_txtItemID.Text, out itemID) || itemID == 0)
            {
                MessageBox.Show("\u7269\u54C1 ID \u7121\u6548\u3002", "\u932F\u8AA4", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ushort price;
            if (!ushort.TryParse(_txtPrice.Text, out price))
                price = 100;

            byte amount;
            if (!byte.TryParse(_txtAmount.Text, out amount) || amount == 0)
                amount = 1;

            byte discount;
            if (!byte.TryParse(_txtDiscount.Text, out discount))
                discount = 100;

            byte tab = (byte)(_cmbTab.SelectedIndex + 1);

            string name = _lblItemName.Text;
            if (name.StartsWith("(")) name = "Item " + itemID;

            var item = new ImMallItem(itemID, name, tab, price);
            item.Amount = amount;
            item.Discount = discount;
            _manager.AddItem(item);

            UpdateStatus();
        }

        void BtnRemove_Click(object sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0) return;
            int idx = _grid.SelectedRows[0].Index;
            _manager.RemoveAt(idx);
            UpdateStatus();
        }

        void BtnSave_Click(object sender, EventArgs e)
        {
            _manager.Save();
            MessageBox.Show("\u5546\u57CE\u8A2D\u5B9A\u5DF2\u5132\u5B58\u3002", "\u5132\u5B58\u6210\u529F", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        void BtnAutoPopulate_Click(object sender, EventArgs e)
        {
            if (_manager.Items.Count > 0)
            {
                var result = MessageBox.Show("\u9019\u6703\u6E05\u9664\u73FE\u6709\u5546\u54C1\u4E26\u5F9E Item.dat \u91CD\u65B0\u586B\u5145\uFF0C\u662F\u5426\u7E7C\u7E8C\uFF1F",
                    "\u81EA\u52D5\u586B\u5145", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes) return;

                _manager.Items.Clear();
            }
            _manager.AutoPopulate(_itemDat);
            _manager.Save();
            UpdateStatus();
        }

        void BtnClearAll_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("\u78BA\u5B9A\u8981\u6E05\u9664\u5168\u90E8\u5546\u57CE\u5546\u54C1\u55CE\uFF1F", "\u6E05\u7A7A\u5168\u90E8",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            _manager.Items.Clear();
            _manager.InvalidateCache();
            UpdateStatus();
        }

        void TabFilter_Changed(object sender, EventArgs e)
        {
            UpdateStatus();
        }

        void Grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            _manager.InvalidateCache();
        }

        void UpdateStatus()
        {
            if (_lblStatus != null)
                _lblStatus.Text = "\u5546\u54C1\u6578\uFF1A" + _manager.Items.Count;
        }
    }
}
