using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace WinFormsExcelFilterDemo
{
    public static class UiConfigLoader
    {
        public static UiConfig Load(string path)
        {
            if (!System.IO.File.Exists(path))
                throw new InvalidOperationException($"No se encontró config: {path}");
            var json = System.IO.File.ReadAllText(path);
            return JsonConvert.DeserializeObject<UiConfig>(json) ?? new UiConfig();
        }

        public static void Apply(Form form, UiConfig cfg, ToolTip sharedToolTip = null)
        {
            if (sharedToolTip == null) sharedToolTip = new ToolTip();

            // === FEATURES (conectan con lógica del Form1) ===
            if (form is Form1 f1)
            {
                f1.SetFeaturesFromConfig(cfg.Features);
            }

            // === DATAGRIDVIEWS ===
            foreach (var dv in cfg.DataGridViews)
            {
                var ctrl = FindControl(form, dv.Name) as DataGridView;
                if (ctrl == null) continue;

                ctrl.Enabled = dv.Enabled;
                ctrl.Visible = dv.Visible;

                if (dv.Props.ReadOnly.HasValue) ctrl.ReadOnly = dv.Props.ReadOnly.Value;
                if (dv.Props.AllowUserToAddRows.HasValue) ctrl.AllowUserToAddRows = dv.Props.AllowUserToAddRows.Value;

                if (Enum.TryParse(dv.Props.Dock ?? "Fill", out DockStyle dock))
                    ctrl.Dock = dock;

                if (Enum.TryParse(dv.Props.AutoSizeColumnsMode ?? "DisplayedCells", out DataGridViewAutoSizeColumnsMode mode))
                    ctrl.AutoSizeColumnsMode = mode;

                if (Enum.TryParse(dv.Props.SelectionMode ?? "FullRowSelect", out DataGridViewSelectionMode sm))
                    ctrl.SelectionMode = sm;

                if (dv.Props.MultiSelect.HasValue) ctrl.MultiSelect = dv.Props.MultiSelect.Value;

                if (Enum.TryParse(dv.Props.SortMode ?? "Programmatic", out DataGridViewColumnSortMode sortMode))
                {
                    foreach (DataGridViewColumn c in ctrl.Columns)
                        c.SortMode = sortMode;
                }

                if (dv.Props.Columns != null)
                {
                    foreach (var c in dv.Props.Columns)
                    {
                        if (string.IsNullOrWhiteSpace(c.Name)) continue;
                        if (!ctrl.Columns.Contains(c.Name)) continue;

                        var col = ctrl.Columns[c.Name];
                        if (c.Visible.HasValue) col.Visible = c.Visible.Value;
                        if (c.Width.HasValue) col.Width = c.Width.Value;
                        if (c.ReadOnly.HasValue) col.ReadOnly = c.ReadOnly.Value;
                        if (!string.IsNullOrWhiteSpace(c.Format))
                            col.DefaultCellStyle.Format = c.Format;
                    }
                }
            }

            // === BUTTONS ===
            foreach (var b in cfg.Buttons)
            {
                var ctrl = FindControl(form, b.Name) as Button;
                if (ctrl == null) continue;

                ctrl.Enabled = b.Enabled;
                ctrl.Visible = b.Visible;

                if (b.Props != null)
                {
                    if (!string.IsNullOrEmpty(b.Props.Text)) ctrl.Text = b.Props.Text;
                    if (!string.IsNullOrEmpty(b.Props.ToolTip)) sharedToolTip.SetToolTip(ctrl, b.Props.ToolTip);
                    if (!string.IsNullOrEmpty(b.Props.BackColor)) ctrl.BackColor = ParseColor(b.Props.BackColor);
                    if (!string.IsNullOrEmpty(b.Props.ForeColor)) ctrl.ForeColor = ParseColor(b.Props.ForeColor);

                    // hotkeys + confirm
                    if (!string.IsNullOrWhiteSpace(b.Props.HotKey) && form is Form1 f1a)
                        f1a.RegisterHotKeyForButton(ctrl, b.Props.HotKey, b.Props.Confirm);
                    else if (!string.IsNullOrWhiteSpace(b.Props.Confirm) && form is Form1 f1b)
                        f1b.RegisterConfirmForButton(ctrl, b.Props.Confirm);
                }
            }

            // === COMBOBOXES ===
            foreach (var c in cfg.ComboBoxes)
            {
                var ctrl = FindControl(form, c.Name) as ComboBox;
                if (ctrl == null) continue;
                ctrl.Enabled = c.Enabled;
                ctrl.Visible = c.Visible;
                if (Enum.TryParse(c.Props?.DropDownStyle ?? "DropDown", out ComboBoxStyle style))
                    ctrl.DropDownStyle = style;
            }

            // === TEXTBOXES ===
            foreach (var t in cfg.TextBoxes)
            {
                var ctrl = FindControl(form, t.Name) as TextBoxBase;
                if (ctrl == null) continue;
                ctrl.Enabled = t.Enabled;
                ctrl.Visible = t.Visible;
            }

            // === PANELS ===
            foreach (var p in cfg.Panels)
            {
                var ctrl = FindControl(form, p.Name) as Panel;
                if (ctrl == null) continue;
                ctrl.Enabled = p.Enabled;
                ctrl.Visible = p.Visible;
            }
        }

        private static Control FindControl(Control root, string name)
        {
            if (root == null || string.IsNullOrWhiteSpace(name)) return null;
            if (root.Name == name) return root;
            foreach (Control c in root.Controls)
            {
                var r = FindControl(c, name);
                if (r != null) return r;
            }
            return null;
        }

        private static Color ParseColor(string hex)
        {
            try
            {
                if (hex.StartsWith("#")) hex = hex.Substring(1);
                if (hex.Length == 6)
                {
                    int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                    int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                    int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                    return Color.FromArgb(r, g, b);
                }
            }
            catch { }
            return SystemColors.Control;
        }
    }
}
