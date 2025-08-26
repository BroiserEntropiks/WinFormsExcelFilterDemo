using System.Windows.Forms;

namespace WinFormsExcelFilterDemo
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private Panel panelTop;
        private Button btnActualizarJson;
        private Button btnGuardarJson;
        private Button btnGuardarComo;
        private Button btnClearFilters;
        private ComboBox cboColMass;
        private TextBox txtMassValue;
        private Button btnMassApply;
        private DataGridView dataGridView1;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.panelTop = new System.Windows.Forms.Panel();
            this.btnActualizarJson = new System.Windows.Forms.Button();
            this.btnGuardarJson = new System.Windows.Forms.Button();
            this.btnGuardarComo = new System.Windows.Forms.Button();
            this.btnClearFilters = new System.Windows.Forms.Button();
            this.cboColMass = new System.Windows.Forms.ComboBox();
            this.txtMassValue = new System.Windows.Forms.TextBox();
            this.btnMassApply = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.btnActualizarJson);
            this.panelTop.Controls.Add(this.btnGuardarJson);
            this.panelTop.Controls.Add(this.btnGuardarComo);
            this.panelTop.Controls.Add(this.btnClearFilters);
            this.panelTop.Controls.Add(this.cboColMass);
            this.panelTop.Controls.Add(this.txtMassValue);
            this.panelTop.Controls.Add(this.btnMassApply);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1000, 48);
            this.panelTop.TabIndex = 0;
            // 
            // btnActualizarJson
            // 
            this.btnActualizarJson.Location = new System.Drawing.Point(12, 12);
            this.btnActualizarJson.Name = "btnActualizarJson";
            this.btnActualizarJson.Size = new System.Drawing.Size(108, 23);
            this.btnActualizarJson.TabIndex = 0;
            this.btnActualizarJson.Text = "Actualizar JSON";
            this.btnActualizarJson.UseVisualStyleBackColor = true;
            // 
            // btnGuardarJson
            // 
            this.btnGuardarJson.Location = new System.Drawing.Point(126, 12);
            this.btnGuardarJson.Name = "btnGuardarJson";
            this.btnGuardarJson.Size = new System.Drawing.Size(75, 23);
            this.btnGuardarJson.TabIndex = 1;
            this.btnGuardarJson.Text = "Guardar";
            this.btnGuardarJson.UseVisualStyleBackColor = true;
            // 
            // btnGuardarComo
            // 
            this.btnGuardarComo.Location = new System.Drawing.Point(207, 12);
            this.btnGuardarComo.Name = "btnGuardarComo";
            this.btnGuardarComo.Size = new System.Drawing.Size(102, 23);
            this.btnGuardarComo.TabIndex = 2;
            this.btnGuardarComo.Text = "Guardar como…";
            this.btnGuardarComo.UseVisualStyleBackColor = true;
            // 
            // btnClearFilters
            // 
            this.btnClearFilters.Location = new System.Drawing.Point(315, 12);
            this.btnClearFilters.Name = "btnClearFilters";
            this.btnClearFilters.Size = new System.Drawing.Size(92, 23);
            this.btnClearFilters.TabIndex = 3;
            this.btnClearFilters.Text = "Limpiar filtros";
            this.btnClearFilters.UseVisualStyleBackColor = true;
            // 
            // cboColMass
            // 
            this.cboColMass.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboColMass.FormattingEnabled = true;
            this.cboColMass.Location = new System.Drawing.Point(413, 12);
            this.cboColMass.Name = "cboColMass";
            this.cboColMass.Size = new System.Drawing.Size(180, 21);
            this.cboColMass.TabIndex = 4;
            // 
            // txtMassValue
            // 
            this.txtMassValue.Location = new System.Drawing.Point(599, 13);
            this.txtMassValue.Name = "txtMassValue";
            this.txtMassValue.Size = new System.Drawing.Size(200, 20);
            this.txtMassValue.TabIndex = 5;
            // 
            // btnMassApply
            // 
            this.btnMassApply.Location = new System.Drawing.Point(805, 12);
            this.btnMassApply.Name = "btnMassApply";
            this.btnMassApply.Size = new System.Drawing.Size(119, 23);
            this.btnMassApply.TabIndex = 6;
            this.btnMassApply.Text = "Aplicar a seleccionadas";
            this.btnMassApply.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(0, 48);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(1000, 602);
            this.dataGridView1.TabIndex = 1;
            this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.MultiSelect = true;
            this.dataGridView1.AllowUserToAddRows = false;
            // 
            // Form1
            // 
            this.KeyPreview = true;
            this.ClientSize = new System.Drawing.Size(1000, 650);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.panelTop);
            this.Name = "Form1";
            this.Text = "JSON Editor con filtros, selección lógica y configuración por JSON";
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
