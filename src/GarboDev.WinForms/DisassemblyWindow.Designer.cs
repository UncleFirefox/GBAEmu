namespace GarboDev.WinForms
{
    partial class DisassemblyWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.disassembly = new System.Windows.Forms.ListBox();
            this.RegistersGroup = new System.Windows.Forms.GroupBox();
            this.registerValues = new System.Windows.Forms.ListBox();
            this.registerNames = new System.Windows.Forms.ListBox();
            this.banks = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.step = new System.Windows.Forms.Button();
            this.go = new System.Windows.Forms.Button();
            this.ScanlineStep = new System.Windows.Forms.Button();
            this.DisAsmScrollBar = new System.Windows.Forms.VScrollBar();
            this.label2 = new System.Windows.Forms.Label();
            this.curLocation = new System.Windows.Forms.TextBox();
            this.gotoLocation = new System.Windows.Forms.Button();
            this.gotoPc = new System.Windows.Forms.Button();
            this.displayArmRadio = new System.Windows.Forms.RadioButton();
            this.displayThumbRadio = new System.Windows.Forms.RadioButton();
            this.displayAutoRadio = new System.Windows.Forms.RadioButton();
            this.RegistersGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // disassembly
            // 
            this.disassembly.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.disassembly.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.disassembly.FormattingEnabled = true;
            this.disassembly.ItemHeight = 14;
            this.disassembly.Location = new System.Drawing.Point(13, 41);
            this.disassembly.Name = "disassembly";
            this.disassembly.Size = new System.Drawing.Size(382, 480);
            this.disassembly.TabIndex = 0;
            // 
            // RegistersGroup
            // 
            this.RegistersGroup.Controls.Add(this.registerValues);
            this.RegistersGroup.Controls.Add(this.registerNames);
            this.RegistersGroup.Controls.Add(this.banks);
            this.RegistersGroup.Controls.Add(this.label1);
            this.RegistersGroup.Location = new System.Drawing.Point(415, 114);
            this.RegistersGroup.Name = "RegistersGroup";
            this.RegistersGroup.Size = new System.Drawing.Size(203, 407);
            this.RegistersGroup.TabIndex = 1;
            this.RegistersGroup.TabStop = false;
            this.RegistersGroup.Text = "Registers";
            // 
            // registerValues
            // 
            this.registerValues.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.registerValues.FormattingEnabled = true;
            this.registerValues.ItemHeight = 14;
            this.registerValues.Location = new System.Drawing.Point(96, 48);
            this.registerValues.Name = "registerValues";
            this.registerValues.Size = new System.Drawing.Size(97, 340);
            this.registerValues.TabIndex = 3;
            // 
            // registerNames
            // 
            this.registerNames.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.registerNames.FormattingEnabled = true;
            this.registerNames.ItemHeight = 14;
            this.registerNames.Location = new System.Drawing.Point(7, 48);
            this.registerNames.Name = "registerNames";
            this.registerNames.Size = new System.Drawing.Size(82, 340);
            this.registerNames.TabIndex = 2;
            // 
            // banks
            // 
            this.banks.FormattingEnabled = true;
            this.banks.Location = new System.Drawing.Point(44, 20);
            this.banks.Name = "banks";
            this.banks.Size = new System.Drawing.Size(117, 21);
            this.banks.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(30, 14);
            this.label1.TabIndex = 0;
            this.label1.Text = "Bank";
            // 
            // step
            // 
            this.step.Location = new System.Drawing.Point(446, 12);
            this.step.Name = "step";
            this.step.Size = new System.Drawing.Size(45, 23);
            this.step.TabIndex = 2;
            this.step.Text = "Step";
            this.step.Click += new System.EventHandler(this.step_Click);
            // 
            // go
            // 
            this.go.Location = new System.Drawing.Point(497, 12);
            this.go.Name = "go";
            this.go.Size = new System.Drawing.Size(39, 23);
            this.go.TabIndex = 3;
            this.go.Text = "Run";
            this.go.Click += new System.EventHandler(this.go_Click);
            // 
            // ScanlineStep
            // 
            this.ScanlineStep.Location = new System.Drawing.Point(542, 12);
            this.ScanlineStep.Name = "ScanlineStep";
            this.ScanlineStep.Size = new System.Drawing.Size(66, 23);
            this.ScanlineStep.TabIndex = 4;
            this.ScanlineStep.Text = "Scan Step";
            this.ScanlineStep.Click += new System.EventHandler(this.ScanlineStep_Click);
            // 
            // DisAsmScrollBar
            // 
            this.DisAsmScrollBar.Location = new System.Drawing.Point(391, 41);
            this.DisAsmScrollBar.Name = "DisAsmScrollBar";
            this.DisAsmScrollBar.Size = new System.Drawing.Size(18, 480);
            this.DisAsmScrollBar.TabIndex = 5;
            this.DisAsmScrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(this.DisAsmScrollBar_Scroll);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 17);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 14);
            this.label2.TabIndex = 6;
            this.label2.Text = "Location:";
            // 
            // curLocation
            // 
            this.curLocation.Location = new System.Drawing.Point(69, 14);
            this.curLocation.Name = "curLocation";
            this.curLocation.Size = new System.Drawing.Size(103, 20);
            this.curLocation.TabIndex = 7;
            // 
            // gotoLocation
            // 
            this.gotoLocation.Location = new System.Drawing.Point(178, 12);
            this.gotoLocation.Name = "gotoLocation";
            this.gotoLocation.Size = new System.Drawing.Size(28, 23);
            this.gotoLocation.TabIndex = 8;
            this.gotoLocation.Text = "Go";
            this.gotoLocation.Click += new System.EventHandler(this.gotoLocation_Click);
            // 
            // gotoPc
            // 
            this.gotoPc.Location = new System.Drawing.Point(213, 12);
            this.gotoPc.Name = "gotoPc";
            this.gotoPc.Size = new System.Drawing.Size(27, 23);
            this.gotoPc.TabIndex = 9;
            this.gotoPc.Text = "PC";
            this.gotoPc.Click += new System.EventHandler(this.gotoPc_Click);
            // 
            // displayArmRadio
            // 
            this.displayArmRadio.AutoSize = true;
            this.displayArmRadio.Location = new System.Drawing.Point(246, 15);
            this.displayArmRadio.Name = "displayArmRadio";
            this.displayArmRadio.Size = new System.Drawing.Size(39, 17);
            this.displayArmRadio.TabIndex = 10;
            this.displayArmRadio.TabStop = false;
            this.displayArmRadio.Text = "Arm";
            this.displayArmRadio.CheckedChanged += new System.EventHandler(this.displayArmRadio_CheckedChanged);
            // 
            // displayThumbRadio
            // 
            this.displayThumbRadio.AutoSize = true;
            this.displayThumbRadio.Location = new System.Drawing.Point(291, 15);
            this.displayThumbRadio.Name = "displayThumbRadio";
            this.displayThumbRadio.Size = new System.Drawing.Size(54, 17);
            this.displayThumbRadio.TabIndex = 11;
            this.displayThumbRadio.TabStop = false;
            this.displayThumbRadio.Text = "Thumb";
            this.displayThumbRadio.CheckedChanged += new System.EventHandler(this.displayThumbRadio_CheckedChanged);
            // 
            // displayAutoRadio
            // 
            this.displayAutoRadio.AutoSize = true;
            this.displayAutoRadio.Checked = true;
            this.displayAutoRadio.Location = new System.Drawing.Point(352, 15);
            this.displayAutoRadio.Name = "displayAutoRadio";
            this.displayAutoRadio.Size = new System.Drawing.Size(43, 17);
            this.displayAutoRadio.TabIndex = 12;
            this.displayAutoRadio.Text = "Auto";
            this.displayAutoRadio.CheckedChanged += new System.EventHandler(this.displayAutoRadio_CheckedChanged);
            // 
            // DisassemblyWindow
            // 
            this.ClientSize = new System.Drawing.Size(630, 533);
            this.Controls.Add(this.displayAutoRadio);
            this.Controls.Add(this.displayThumbRadio);
            this.Controls.Add(this.displayArmRadio);
            this.Controls.Add(this.gotoPc);
            this.Controls.Add(this.gotoLocation);
            this.Controls.Add(this.curLocation);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.DisAsmScrollBar);
            this.Controls.Add(this.ScanlineStep);
            this.Controls.Add(this.go);
            this.Controls.Add(this.step);
            this.Controls.Add(this.RegistersGroup);
            this.Controls.Add(this.disassembly);
            this.Name = "DisassemblyWindow";
            this.Text = "DisassemblyWindow";
            this.RegistersGroup.ResumeLayout(false);
            this.RegistersGroup.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox disassembly;
        private System.Windows.Forms.GroupBox RegistersGroup;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox banks;
        private System.Windows.Forms.ListBox registerNames;
        private System.Windows.Forms.ListBox registerValues;
        private System.Windows.Forms.Button step;
        private System.Windows.Forms.Button go;
        private System.Windows.Forms.Button ScanlineStep;
        private System.Windows.Forms.VScrollBar DisAsmScrollBar;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox curLocation;
        private System.Windows.Forms.Button gotoLocation;
        private System.Windows.Forms.Button gotoPc;
        private System.Windows.Forms.RadioButton displayArmRadio;
        private System.Windows.Forms.RadioButton displayThumbRadio;
        private System.Windows.Forms.RadioButton displayAutoRadio;
    }
}