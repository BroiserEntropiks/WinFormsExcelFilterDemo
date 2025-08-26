using System;
using System.Collections.Generic;
using System.ComponentModel; // ListSortDirection
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WinFormsExcelFilterDemo
{
    public partial class Form1 : Form
    {
        // === Archivo de datos ===
        private string filePath = "BDTEST.json";

        private DataTable _table;

        // === Header checkbox (modo lógico D) ===
        private CheckBox _headerCheck;
        private bool _selectAllMode = false;                 // select-all global
        private readonly HashSet<int> _unselected = new HashSet<int>();   // excepciones por _RowIndex

        // === Config features (desde JSON) ===
        private UiConfig _uiConfig = new UiConfig();         // persistimos última config aplicada
        private FeaturesConfig _features { get { return _uiConfig.Features; } }

        // === Colores ===
        private readonly Color _rowCheckedBack = Color.FromArgb(220, 255, 220);
        private readonly Color _rowCheckedSelBack = Color.FromArgb(120, 200, 120);
        private readonly Color _rowUncheckedBack = SystemColors.Window;
        private readonly Color _rowUncheckedSelBack = SystemColors.Highlight;

        // === Orden ===
        private string _lastSortCol = null;
        private ListSortDirection _lastDir = ListSortDirection.Ascending;

        // === Filtros tipo Excel ===
        private ContextMenuStrip _filterMenu;
        private CheckedListBox _chkList;
        private ToolStripButton _btnOk, _btnCancel;
        private ToolStripLabel _lblSearch;
        private ToolStripTextBox _txtSearch;
        private string _filterColumn;

        // Checklist (valores exactos)
        private readonly Dictionary<string, HashSet<string>> _filtersExact =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        // Texto parcial (LIKE %texto%)
        private readonly Dictionary<string, string> _filtersText =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Form1()
        {
            InitializeComponent();
            this.KeyPreview = true;

            WireUpEvents();
            CargarDatos();
            InitFilterMenu();

            // === Cargar y aplicar configuración de UI ===
            TryLoadAndApplyConfig();

            ResetFilterDictionaries();
            RellenarComboColumnas();
        }

        private void TryLoadAndApplyConfig()
        {
            try
            {
                if (File.Exists("ui-config.json"))
                {
                    _uiConfig = UiConfigLoader.Load("ui-config.json");
                    UiConfigLoader.Apply(this, _uiConfig, new ToolTip());
                }
                else
                {
                    // crea default para que el usuario lo edite
                    File.WriteAllText("ui-config.json",
                        JsonConvert.SerializeObject(_uiConfig, Formatting.Indented), Encoding.UTF8);
                    UiConfigLoader.Apply(this, _uiConfig, new ToolTip());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Config UI: " + ex.Message);
                // Aplica defaults
                UiConfigLoader.Apply(this, _uiConfig, new ToolTip());
            }
        }

        // ================== Eventos y preparación ==================
        private void WireUpEvents()
        {
            // Botones
            btnClearFilters.Click += btnClearFilters_Click;
            btnMassApply.Click += btnMassApply_Click;
            btnGuardarJson.Click += btnGuardarJson_Click;
            btnGuardarComo.Click += btnGuardarComo_Click;
            btnActualizarJson.Click += btnActualizarJson_Click;

            // DGV
            dataGridView1.CellFormatting += dataGridView1_CellFormatting;
            dataGridView1.CurrentCellDirtyStateChanged += dataGridView1_CurrentCellDirtyStateChanged;
            dataGridView1.CellValueChanged += dataGridView1_CellValueChanged;
            dataGridView1.CellBeginEdit += dataGridView1_CellBeginEdit;
            dataGridView1.DataBindingComplete += (s, e) => { RellenarComboColumnas(); PositionHeaderCheck(); };

            dataGridView1.ColumnHeaderMouseClick += dataGridView1_ColumnHeaderMouseClick;

            dataGridView1.Scroll += (s, e) => PositionHeaderCheck();
            dataGridView1.ColumnWidthChanged += (s, e) => PositionHeaderCheck();
            dataGridView1.SizeChanged += (s, e) => PositionHeaderCheck();
            dataGridView1.ColumnDisplayIndexChanged += (s, e) => PositionHeaderCheck();

            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AutoGenerateColumns = true;
            EnableDoubleBuffer(dataGridView1);
        }

        // ================== Cargar JSON ==================
        private void CargarDatos()
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("No se encontró el archivo JSON.");
                return;
            }

            string json = File.ReadAllText(filePath, Encoding.UTF8);
            var data = JArray.Parse(json);
            if (data.Count == 0)
            {
                MessageBox.Show("El JSON está vacío.");
                return;
            }

            _table = new DataTable();

            _table.Columns.Add(new DataColumn("_RowIndex", typeof(int)));
            _table.Columns.Add(new DataColumn("Seleccionar", typeof(bool)));

            var first = (JObject)data[0];
            var colNames = first.Properties().Select(p => p.Name).ToList();
            foreach (var name in colNames)
                _table.Columns.Add(name, typeof(string));

            for (int i = 0; i < data.Count; i++)
            {
                var jo = (JObject)data[i];
                var row = _table.NewRow();
                row["_RowIndex"] = i;
                row["Seleccionar"] = false;
                foreach (var name in colNames)
                    row[name] = jo[name]?.ToString() ?? "";
                _table.Rows.Add(row);
            }

            dataGridView1.DataSource = _table;

            if (dataGridView1.Columns.Contains("_RowIndex"))
                dataGridView1.Columns["_RowIndex"].Visible = false;

            if (dataGridView1.Columns.Contains("Seleccionar"))
            {
                dataGridView1.Columns["Seleccionar"].DisplayIndex = 0;
                dataGridView1.Columns["Seleccionar"].Width = 42;
                dataGridView1.Columns["Seleccionar"].ReadOnly = false;
            }

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;

            // Reset select-all lógico
            _selectAllMode = false;
            _unselected.Clear();

            EnsureHeaderCheck();
            dataGridView1.Invalidate();
        }

        // ================== Header checkbox (modo lógico) ==================
        private void EnsureHeaderCheck()
        {
            if (!_features.EnableHeaderSelectAll)
            {
                if (_headerCheck != null)
                {
                    dataGridView1.Controls.Remove(_headerCheck);
                    _headerCheck.Dispose();
                    _headerCheck = null;
                }
                return;
            }

            if (_headerCheck != null)
            {
                dataGridView1.Controls.Remove(_headerCheck);
                _headerCheck.Dispose();
                _headerCheck = null;
            }
            if (!dataGridView1.Columns.Contains("Seleccionar")) return;

            _headerCheck = new CheckBox
            {
                Size = new Size(15, 15),
                BackColor = Color.Transparent,
                Checked = _selectAllMode
            };

            _headerCheck.MouseUp += (s, e) =>
            {
                if (!_features.EnableHeaderSelectAll) return;
                // Alternativa D (lógico)
                _selectAllMode = _headerCheck.Checked;
                _unselected.Clear();        // reinicia excepciones
                dataGridView1.Invalidate(); // repinta
            };

            dataGridView1.Controls.Add(_headerCheck);
            PositionHeaderCheck();
            _headerCheck.BringToFront();
        }

        private void PositionHeaderCheck()
        {
            if (_headerCheck == null) return;
            if (!dataGridView1.Columns.Contains("Seleccionar")) return;

            var col = dataGridView1.Columns["Seleccionar"];
            Rectangle rect = dataGridView1.GetCellDisplayRectangle(col.Index, -1, true);
            int x = rect.Left + (rect.Width - _headerCheck.Width) / 2;
            int y = rect.Top + (rect.Height - _headerCheck.Height) / 2;
            _headerCheck.Location = new Point(Math.Max(x, 0), Math.Max(y, 0));
            _headerCheck.BringToFront();
        }

        private bool IsRowSelected(DataGridViewRow row)
        {
            int ridx = Convert.ToInt32(row.Cells["_RowIndex"].Value);
            if (_selectAllMode) return !_unselected.Contains(ridx);
            return Convert.ToBoolean(row.Cells["Seleccionar"].Value ?? false);
        }

        // ================== Orden/Filtro en encabezado ==================
        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var col = dataGridView1.Columns[e.ColumnIndex];

            // Orden (izq)
            if (e.Button == MouseButtons.Left)
            {
                if (!_features.EnableSorting) return;
                if (col.Name == "Seleccionar" || col.Name == "_RowIndex") return;

                string colName = col.DataPropertyName ?? col.Name;
                if (_lastSortCol == colName)
                    _lastDir = (_lastDir == ListSortDirection.Ascending) ? ListSortDirection.Descending : ListSortDirection.Ascending;
                else { _lastSortCol = colName; _lastDir = ListSortDirection.Ascending; }

                _table.DefaultView.Sort = "[" + colName + "] " + (_lastDir == ListSortDirection.Ascending ? "ASC" : "DESC");

                foreach (DataGridViewColumn c in dataGridView1.Columns)
                    c.HeaderCell.SortGlyphDirection = SortOrder.None;
                col.HeaderCell.SortGlyphDirection = _lastDir == ListSortDirection.Ascending ? SortOrder.Ascending : SortOrder.Descending;

                return;
            }

            // Filtro (der)
            if (e.Button == MouseButtons.Right)
            {
                if (!_features.EnableFilters) return;

                _filterColumn = col.DataPropertyName ?? col.Name;
                if (_filterColumn == "Seleccionar" || _filterColumn == "_RowIndex") return;

                var vals = _table.DefaultView.ToTable(true, _filterColumn)
                               .AsEnumerable()
                               .Select(r => r[0] != null ? r[0].ToString() : "")
                               .Distinct(StringComparer.OrdinalIgnoreCase)
                               .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                               .ToList();

                _chkList.Items.Clear();
                _chkList.Items.Add("(Seleccionar todo)", true);

                bool hadBlank = vals.Any(v => string.IsNullOrEmpty(v));
                if (hadBlank)
                {
                    _chkList.Items.Add("(Blanks)", true);
                    vals = vals.Where(v => !string.IsNullOrEmpty(v)).ToList();
                }
                foreach (var v in vals)
                    _chkList.Items.Add(v, true);

                if (_filtersExact.ContainsKey(_filterColumn) && _filtersExact[_filterColumn] != null)
                {
                    HashSet<string> selectedSet = _filtersExact[_filterColumn];
                    for (int i = 0; i < _chkList.Items.Count; i++) _chkList.SetItemChecked(i, false);
                    for (int i = 0; i < _chkList.Items.Count; i++)
                    {
                        string item = _chkList.Items[i].ToString();
                        if (item == "(Seleccionar todo)") continue;
                        if (item == "(Blanks)")
                        {
                            if (selectedSet.Contains("")) _chkList.SetItemChecked(i, true);
                        }
                        else if (selectedSet.Contains(item)) _chkList.SetItemChecked(i, true);
                    }
                }

                if (_features.EnablePartialSearch && _txtSearch != null)
                {
                    string t;
                    _txtSearch.Text = _filtersText.TryGetValue(_filterColumn, out t) ? t : string.Empty;
                }

                _chkList.ItemCheck -= ChkList_ItemCheck;
                _chkList.ItemCheck += ChkList_ItemCheck;

                var rect = dataGridView1.GetCellDisplayRectangle(e.ColumnIndex, -1, true);
                var screenPoint = dataGridView1.PointToScreen(new Point(rect.Left, rect.Bottom));
                _filterMenu.Show(screenPoint);
            }
        }

        // ================== Menú de filtro (con búsqueda parcial) ==================
        private void InitFilterMenu()
        {
            _filterMenu = new ContextMenuStrip { AutoClose = false };

            if (_features.EnablePartialSearch)
            {
                _lblSearch = new ToolStripLabel("Buscar:");
                _txtSearch = new ToolStripTextBox { AutoSize = false, Width = 200 };
                _txtSearch.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        ApplySelectionFromMenu();
                        _filterMenu.Close();
                    }
                };
                _filterMenu.Items.Add(_lblSearch);
                _filterMenu.Items.Add(_txtSearch);
                _filterMenu.Items.Add(new ToolStripSeparator());
            }
            else
            {
                _lblSearch = null;
                _txtSearch = null;
            }

            _chkList = new CheckedListBox();
            _chkList.CheckOnClick = true;
            _chkList.BorderStyle = BorderStyle.None;
            _chkList.IntegralHeight = false;
            _chkList.Width = 260;
            _chkList.Height = 240;

            var host = new ToolStripControlHost(_chkList);
            host.AutoSize = false;
            host.Width = 260;
            host.Height = 240;
            host.Margin = new Padding(4);

            _btnOk = new ToolStripButton("Aceptar");
            _btnCancel = new ToolStripButton("Cancelar");
            _btnOk.Click += (s, e) => { ApplySelectionFromMenu(); _filterMenu.Close(); };
            _btnCancel.Click += (s, e) => _filterMenu.Close();

            _filterMenu.Items.Add(host);
            _filterMenu.Items.Add(new ToolStripSeparator());
            _filterMenu.Items.Add(_btnOk);
            _filterMenu.Items.Add(_btnCancel);
        }

        private void ChkList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.Index == 0) // “Seleccionar todo”
            {
                bool checkAll = e.NewValue == CheckState.Checked;
                BeginInvoke(new Action(delegate
                {
                    for (int i = 1; i < _chkList.Items.Count; i++)
                        _chkList.SetItemChecked(i, checkAll);
                }));
            }
        }

        private void ApplySelectionFromMenu()
        {
            if (string.IsNullOrEmpty(_filterColumn)) return;

            // 1) Búsqueda parcial
            string term = null;
            if (_features.EnablePartialSearch && _txtSearch != null)
            {
                term = (_txtSearch.Text ?? "").Trim();
            }
            _filtersText[_filterColumn] = string.IsNullOrEmpty(term) ? null : term;

            // 2) Checklist exacto
            HashSet<string> selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool allChecked = true;
            for (int i = 1; i < _chkList.Items.Count; i++)
            {
                if (_chkList.GetItemChecked(i))
                {
                    string txt = _chkList.Items[i].ToString();
                    if (txt == "(Blanks)") selected.Add("");
                    else selected.Add(txt);
                }
                else allChecked = false;
            }
            _filtersExact[_filterColumn] = allChecked ? null : selected;

            ApplyAllFilters();
        }

        private void ApplyAllFilters()
        {
            List<string> parts = new List<string>();

            foreach (DataColumn col in _table.Columns)
            {
                string name = col.ColumnName;
                if (name == "Seleccionar" || name == "_RowIndex") continue;

                List<string> andList = new List<string>();

                // LIKE '%texto%'
                string term;
                if (_filtersText.TryGetValue(name, out term) && !string.IsNullOrEmpty(term))
                {
                    string like = "%" + EscapeLike(term) + "%";
                    andList.Add("(CONVERT([" + EscapeCol(name) + "], 'System.String') LIKE '" + like + "')");
                }

                // checklist exacto
                HashSet<string> chosen;
                if (_filtersExact.TryGetValue(name, out chosen) && chosen != null)
                {
                    List<string> orList = new List<string>();
                    foreach (string v in chosen)
                    {
                        if (v == "")
                            orList.Add("([" + EscapeCol(name) + "] IS NULL OR [" + EscapeCol(name) + "] = '')");
                        else
                            orList.Add("([" + EscapeCol(name) + "] = '" + EscapeVal(v) + "')");
                    }
                    if (orList.Count > 0)
                        andList.Add("(" + string.Join(" OR ", orList.ToArray()) + ")");
                }

                if (andList.Count > 0)
                    parts.Add("(" + string.Join(" AND ", andList.ToArray()) + ")");
            }

            _table.DefaultView.RowFilter = string.Join(" AND ", parts.ToArray());
        }

        private static string EscapeCol(string col) { return col.Replace("]", "]]"); }
        private static string EscapeVal(string val) { return val.Replace("'", "''"); }
        private static string EscapeLike(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]").Replace("'", "''");
        }

        private void ResetFilterDictionaries()
        {
            _filtersExact.Clear();
            _filtersText.Clear();
            if (_table == null) return;
            foreach (DataColumn dc in _table.Columns)
            {
                if (dc.ColumnName == "Seleccionar" || dc.ColumnName == "_RowIndex") continue;
                _filtersExact[dc.ColumnName] = null;
                _filtersText[dc.ColumnName] = null;
            }
            _table.DefaultView.RowFilter = string.Empty;
        }

        // ================== Edición masiva ==================
        private void btnMassApply_Click(object sender, EventArgs e)
        {
            if (!_features.EnableMassEdit) return;

            if (cboColMass.SelectedItem == null)
            {
                MessageBox.Show("Selecciona una columna.");
                return;
            }
            string colName = cboColMass.SelectedItem.ToString();
            string newValue = txtMassValue.Text;

            int afectadas = 0;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                if (IsRowSelected(row))
                {
                    row.Cells[colName].Value = newValue;
                    afectadas++;
                }
            }
            dataGridView1.EndEdit();
            MessageBox.Show("Actualizadas " + afectadas + " filas seleccionadas.");
        }

        private void RellenarComboColumnas()
        {
            if (_table == null) return;
            cboColMass.Items.Clear();
            foreach (DataColumn dc in _table.Columns)
            {
                if (dc.ColumnName == "Seleccionar" || dc.ColumnName == "_RowIndex") continue;
                cboColMass.Items.Add(dc.ColumnName);
            }
            if (cboColMass.Items.Count > 0 && cboColMass.SelectedIndex < 0)
                cboColMass.SelectedIndex = 0;
        }

        // ================== Guardar / Guardar como / Actualizar ==================
        private void btnGuardarJson_Click(object sender, EventArgs e)
        {
            if (!_features.EnableSave) return;

            try
            {
                GuardarSoloCambiosSeleccionados(filePath);
                MessageBox.Show("Guardado OK (solo cambios de filas seleccionadas).");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }

        private void btnGuardarComo_Click(object sender, EventArgs e)
        {
            if (!_features.EnableSave) return;

            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "JSON|*.json", FileName = Path.GetFileName(filePath) })
            {
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        GuardarSoloCambiosSeleccionados(sfd.FileName);
                        MessageBox.Show("Guardado como: " + sfd.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al guardar: " + ex.Message);
                    }
                }
            }
        }

        private void btnActualizarJson_Click(object sender, EventArgs e)
        {
            if (!_features.EnableReloadFromDisk) return;
            CargarDatos();
            TryLoadAndApplyConfig(); // si cambiaste columnas en config, se re-aplica
            MessageBox.Show("Datos recargados desde el archivo.");
        }

        private void btnClearFilters_Click(object sender, EventArgs e)
        {
            ResetFilterDictionaries();
        }

        private void GuardarSoloCambiosSeleccionados(string destino)
        {
            dataGridView1.EndEdit();

            if (!File.Exists(filePath))
                throw new FileNotFoundException("No se encontró el archivo original.", filePath);

            string originalText = File.ReadAllText(filePath, Encoding.UTF8);
            JArray originalArr = JArray.Parse(originalText);

            string keyCol = DetectKeyColumn();

            Dictionary<string, JObject> dictByKey = null;
            if (!string.IsNullOrEmpty(keyCol) && _table.Columns.Contains(keyCol))
            {
                dictByKey = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
                foreach (JToken tok in originalArr)
                {
                    JObject jo = tok as JObject;
                    if (jo != null)
                    {
                        string k = jo[keyCol] != null ? jo[keyCol].ToString() : null;
                        if (!string.IsNullOrEmpty(k) && !dictByKey.ContainsKey(k))
                            dictByKey[k] = jo;
                    }
                }
            }

            foreach (DataGridViewRow gridRow in dataGridView1.Rows)
            {
                if (gridRow.IsNewRow) continue;
                if (!IsRowSelected(gridRow)) continue;

                Dictionary<string, string> pairs = new Dictionary<string, string>();
                foreach (DataGridViewColumn c in dataGridView1.Columns)
                {
                    string name = c.DataPropertyName ?? c.Name;
                    if (name == "Seleccionar" || name == "_RowIndex") continue;
                    string val = gridRow.Cells[c.Index].Value != null ? gridRow.Cells[c.Index].Value.ToString() : "";
                    pairs[name] = val;
                }

                JObject target = null;

                if (dictByKey != null)
                {
                    string keyVal = gridRow.Cells[keyCol] != null ? (gridRow.Cells[keyCol].Value != null ? gridRow.Cells[keyCol].Value.ToString() : null) : null;
                    if (!string.IsNullOrEmpty(keyVal))
                        dictByKey.TryGetValue(keyVal, out target);
                }

                if (target == null)
                {
                    int idx = Convert.ToInt32(gridRow.Cells["_RowIndex"].Value);
                    if (idx >= 0 && idx < originalArr.Count && originalArr[idx] is JObject)
                        target = (JObject)originalArr[idx];
                }

                if (target == null) continue;

                bool anyChange = false;
                foreach (KeyValuePair<string, string> kv in pairs)
                {
                    string oldVal = target[kv.Key] != null ? target[kv.Key].ToString() : "";
                    string newVal = kv.Value ?? "";
                    if (!string.Equals(oldVal, newVal, StringComparison.Ordinal))
                    { anyChange = true; break; }
                }
                if (!anyChange) continue;

                foreach (KeyValuePair<string, string> kv in pairs)
                    target[kv.Key] = kv.Value == null ? JValue.CreateNull() : JToken.FromObject(kv.Value);
            }

            string outText = JsonConvert.SerializeObject(originalArr, Formatting.Indented);
            File.WriteAllText(destino, outText, Encoding.UTF8);
        }

        private string DetectKeyColumn()
        {
            string[] candidates = new string[] { "Id", "ID", "id", "Key", "Clave", "Codigo", "Código" };
            foreach (string c in candidates)
                if (_table.Columns.Contains(c)) return c;
            return null;
        }

        // ================== Estilos / edición condicionada ==================
        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dataGridView1.Rows.Count) return;

            if (!_features.ColorizeSelected) return;

            DataGridViewRow row = dataGridView1.Rows[e.RowIndex];
            bool isSelected = IsRowSelected(row);

            row.DefaultCellStyle.BackColor = isSelected ? _rowCheckedBack : _rowUncheckedBack;
            row.DefaultCellStyle.ForeColor = SystemColors.WindowText;
            row.DefaultCellStyle.SelectionBackColor = isSelected ? _rowCheckedSelBack : _rowUncheckedSelBack;
            row.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.IsCurrentCellDirty &&
                dataGridView1.CurrentCell != null &&
                dataGridView1.CurrentCell.OwningColumn.Name == "Seleccionar")
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string colName = dataGridView1.Columns[e.ColumnIndex].Name;
            if (colName != "Seleccionar") return;

            // En select-all lógico, el checkbox de fila indica EXCEPCIÓN
            if (_selectAllMode)
            {
                int rowIndex = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["_RowIndex"].Value);
                bool cellVal = Convert.ToBoolean(dataGridView1.Rows[e.RowIndex].Cells["Seleccionar"].Value ?? false);

                if (!cellVal) _unselected.Add(rowIndex);
                else _unselected.Remove(rowIndex);

                dataGridView1.InvalidateRow(e.RowIndex);
            }
            else
            {
                dataGridView1.InvalidateRow(e.RowIndex);
            }
        }

        private void dataGridView1_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (dataGridView1.Columns[e.ColumnIndex].Name == "Seleccionar") return; // siempre editable

            if (_features.ReadOnly)
            {
                e.Cancel = true; return;
            }

            DataGridViewRow row = dataGridView1.Rows[e.RowIndex];
            bool allowed = IsRowSelected(row);
            if (!allowed) e.Cancel = true;
        }

        // ================== Helper de doble buffer ==================
        private void EnableDoubleBuffer(DataGridView dgv)
        {
            try
            {
                var pi = typeof(DataGridView).GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (pi != null) pi.SetValue(dgv, true, null);
            }
            catch { }
        }

        // ================== Integración con UiConfigLoader ==================
        public void SetFeaturesFromConfig(FeaturesConfig cfg)
        {
            _uiConfig.Features = cfg != null ? cfg : new FeaturesConfig();

            // Sorting
            foreach (DataGridViewColumn c in dataGridView1.Columns)
                c.SortMode = _features.EnableSorting ? DataGridViewColumnSortMode.Programmatic : DataGridViewColumnSortMode.NotSortable;

            // Select-all header
            EnsureHeaderCheck();

            // Color
            dataGridView1.Invalidate();
        }

        public void RegisterHotKeyForButton(Button btn, string hotKey, string confirmMessage = null)
        {
            if (string.IsNullOrWhiteSpace(hotKey)) return;
            this.KeyPreview = true;

            Keys parse(string s)
            {
                string[] parts = s.Split('+');
                Keys k = Keys.None;
                foreach (string pRaw in parts)
                {
                    string p = pRaw.Trim();
                    Keys partKey;
                    if (Enum.TryParse<Keys>(p, true, out partKey)) k |= partKey;
                }
                return k;
            }

            Keys target = parse(hotKey);

            this.KeyDown += (s, e) =>
            {
                if (e.KeyData == target && btn.Visible && btn.Enabled)
                {
                    e.SuppressKeyPress = true;
                    if (!string.IsNullOrEmpty(confirmMessage))
                    {
                        if (MessageBox.Show(confirmMessage, "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                            return;
                    }
                    btn.PerformClick();
                }
            };
        }

        public void RegisterConfirmForButton(Button btn, string confirmMessage)
        {
            if (string.IsNullOrWhiteSpace(confirmMessage)) return;

            EventHandler wrapper = null;
            wrapper = (sender, e) =>
            {
                if (MessageBox.Show(confirmMessage, "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    btn.Click -= wrapper;
                    try { btn.PerformClick(); }
                    finally { btn.Click += wrapper; }
                }
            };

            btn.Click -= wrapper;
            btn.Click += wrapper;
        }
    }
}
