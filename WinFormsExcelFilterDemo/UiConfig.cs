using System.Collections.Generic;

namespace WinFormsExcelFilterDemo
{
    public class UiConfig
    {
        public FeaturesConfig Features { get; set; } = new FeaturesConfig();

        public List<DgvConfig> DataGridViews { get; set; } = new List<DgvConfig>();
        public List<ButtonConfig> Buttons { get; set; } = new List<ButtonConfig>();
        public List<ComboConfig> ComboBoxes { get; set; } = new List<ComboConfig>();
        public List<TextConfig> TextBoxes { get; set; } = new List<TextConfig>();
        public List<PanelConfig> Panels { get; set; } = new List<PanelConfig>();
    }

    public class FeaturesConfig
    {
        public bool EnableHeaderSelectAll { get; set; } = true;
        public string SelectAllMode { get; set; } = "Logical"; // Logical | Physical (por si luego cambias)
        public bool EnableFilters { get; set; } = true;
        public bool EnablePartialSearch { get; set; } = true;
        public bool EnableSorting { get; set; } = true;
        public bool EnableMassEdit { get; set; } = true;
        public bool EnableSave { get; set; } = true;
        public bool EnableReloadFromDisk { get; set; } = true;
        public bool ReadOnly { get; set; } = false;
        public bool ColorizeSelected { get; set; } = true;
    }

    public class DgvConfig
    {
        public string Name { get; set; }
        public bool Enabled { get; set; } = true;
        public bool Visible { get; set; } = true;
        public DgvProps Props { get; set; } = new DgvProps();
    }

    public class DgvProps
    {
        public string Dock { get; set; } = "Fill"; // None|Top|Bottom|Left|Right|Fill
        public bool? ReadOnly { get; set; }
        public bool? AllowUserToAddRows { get; set; }
        public string AutoSizeColumnsMode { get; set; } = "DisplayedCells";
        public string SelectionMode { get; set; } = "FullRowSelect";
        public bool? MultiSelect { get; set; }
        public string SortMode { get; set; } = "Programmatic"; // NotSortable | Automatic | Programmatic
        public List<DgvColumnConfig> Columns { get; set; } = new List<DgvColumnConfig>();
    }

    public class DgvColumnConfig
    {
        public string Name { get; set; }
        public bool? Visible { get; set; }
        public int? Width { get; set; }
        public bool? ReadOnly { get; set; }
        public string Format { get; set; } // "N2", "yyyy-MM-dd", etc.
    }

    public class ButtonConfig
    {
        public string Name { get; set; }
        public bool Enabled { get; set; } = true;
        public bool Visible { get; set; } = true;
        public ButtonProps Props { get; set; } = new ButtonProps();
    }

    public class ButtonProps
    {
        public string Text { get; set; }
        public string ToolTip { get; set; }
        public string BackColor { get; set; } // "#RRGGBB"
        public string ForeColor { get; set; } // "#RRGGBB"
        public string HotKey { get; set; }    // "Ctrl+S", "F5", etc.
        public string Confirm { get; set; }   // mensaje confirmación
    }

    public class ComboConfig
    {
        public string Name { get; set; }
        public bool Enabled { get; set; } = true;
        public bool Visible { get; set; } = true;
        public ComboProps Props { get; set; } = new ComboProps();
    }

    public class ComboProps
    {
        public string DropDownStyle { get; set; } = "DropDown"; // DropDown | DropDownList | Simple
    }

    public class TextConfig
    {
        public string Name { get; set; }
        public bool Enabled { get; set; } = true;
        public bool Visible { get; set; } = true;
    }

    public class PanelConfig
    {
        public string Name { get; set; }
        public bool Enabled { get; set; } = true;
        public bool Visible { get; set; } = true;
    }
}
