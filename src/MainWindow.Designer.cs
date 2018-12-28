namespace GarboDev.WinForms
{
    partial class MainWindow
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
            this.MainMenu = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FileOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.sep1ToolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.FilePause = new System.Windows.Forms.ToolStripMenuItem();
            this.FileReset = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.loadStateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripMenuItem();
            this.saveStateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem8 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem9 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem10 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem11 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem12 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem13 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.FileExit = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OptionsUseBios = new System.Windows.Forms.ToolStripMenuItem();
            this.OptionsSkipBios = new System.Windows.Forms.ToolStripMenuItem();
            this.OptionsBiosFile = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.OptionsSize = new System.Windows.Forms.ToolStripMenuItem();
            this.OptionsSizex1 = new System.Windows.Forms.ToolStripMenuItem();
            this.OptionsSizex2 = new System.Windows.Forms.ToolStripMenuItem();
            this.OptionsSizex3 = new System.Windows.Forms.ToolStripMenuItem();
            this.renderersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OptionsRenderersD3D = new System.Windows.Forms.ToolStripMenuItem();
            this.OptionsRenderersShader = new System.Windows.Forms.ToolStripMenuItem();
            this.OptionsRenderersGDI = new System.Windows.Forms.ToolStripMenuItem();
            this.OptionsLimitFps = new System.Windows.Forms.ToolStripMenuItem();
            this.OptionsVsync = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.EnableSound = new System.Windows.Forms.ToolStripMenuItem();
            this.debugToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.DebugDissassembly = new System.Windows.Forms.ToolStripMenuItem();
            this.DebugPalette = new System.Windows.Forms.ToolStripMenuItem();
            this.spritesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dumpOutputToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.StatusStrip = new System.Windows.Forms.StatusStrip();
            this.FPSPanel = new System.Windows.Forms.ToolStripStatusLabel();
            this.MainMenu.SuspendLayout();
            this.StatusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainMenu
            // 
            this.MainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.debugToolStripMenuItem});
            this.MainMenu.Location = new System.Drawing.Point(0, 0);
            this.MainMenu.Name = "MainMenu";
            this.MainMenu.Size = new System.Drawing.Size(292, 24);
            this.MainMenu.TabIndex = 0;
            this.MainMenu.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileOpen,
            this.sep1ToolStripSeparator,
            this.FilePause,
            this.FileReset,
            this.toolStripSeparator1,
            this.loadStateToolStripMenuItem,
            this.saveStateToolStripMenuItem,
            this.toolStripSeparator3,
            this.FileExit});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(40, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // FileOpen
            // 
            this.FileOpen.Name = "FileOpen";
            this.FileOpen.Size = new System.Drawing.Size(138, 22);
            this.FileOpen.Text = "&Open";
            this.FileOpen.Click += new System.EventHandler(this.FileOpen_Click);
            // 
            // sep1ToolStripSeparator
            // 
            this.sep1ToolStripSeparator.Name = "sep1ToolStripSeparator";
            this.sep1ToolStripSeparator.Size = new System.Drawing.Size(135, 6);
            // 
            // FilePause
            // 
            this.FilePause.CheckOnClick = true;
            this.FilePause.Name = "FilePause";
            this.FilePause.Size = new System.Drawing.Size(138, 22);
            this.FilePause.Text = "&Pause";
            this.FilePause.Click += new System.EventHandler(this.FilePause_Click);
            // 
            // FileReset
            // 
            this.FileReset.Name = "FileReset";
            this.FileReset.Size = new System.Drawing.Size(138, 22);
            this.FileReset.Text = "&Reset";
            this.FileReset.Click += new System.EventHandler(this.FileReset_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(135, 6);
            // 
            // loadStateToolStripMenuItem
            // 
            this.loadStateToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem2,
            this.toolStripMenuItem3,
            this.toolStripMenuItem4,
            this.toolStripMenuItem5,
            this.toolStripMenuItem6,
            this.toolStripMenuItem7});
            this.loadStateToolStripMenuItem.Enabled = false;
            this.loadStateToolStripMenuItem.Name = "loadStateToolStripMenuItem";
            this.loadStateToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.loadStateToolStripMenuItem.Text = "&Load State";
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(83, 22);
            this.toolStripMenuItem2.Text = "&0";
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(83, 22);
            this.toolStripMenuItem3.Text = "&1";
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(83, 22);
            this.toolStripMenuItem4.Text = "&2";
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            this.toolStripMenuItem5.Size = new System.Drawing.Size(83, 22);
            this.toolStripMenuItem5.Text = "&3";
            // 
            // toolStripMenuItem6
            // 
            this.toolStripMenuItem6.Name = "toolStripMenuItem6";
            this.toolStripMenuItem6.Size = new System.Drawing.Size(83, 22);
            this.toolStripMenuItem6.Text = "&4";
            // 
            // toolStripMenuItem7
            // 
            this.toolStripMenuItem7.Name = "toolStripMenuItem7";
            this.toolStripMenuItem7.Size = new System.Drawing.Size(83, 22);
            this.toolStripMenuItem7.Text = "&5";
            // 
            // saveStateToolStripMenuItem
            // 
            this.saveStateToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem8,
            this.toolStripMenuItem9,
            this.toolStripMenuItem10,
            this.toolStripMenuItem11,
            this.toolStripMenuItem12,
            this.toolStripMenuItem13});
            this.saveStateToolStripMenuItem.Enabled = false;
            this.saveStateToolStripMenuItem.Name = "saveStateToolStripMenuItem";
            this.saveStateToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.saveStateToolStripMenuItem.Text = "&Save State";
            // 
            // toolStripMenuItem8
            // 
            this.toolStripMenuItem8.Name = "toolStripMenuItem8";
            this.toolStripMenuItem8.Size = new System.Drawing.Size(83, 22);
            this.toolStripMenuItem8.Text = "&0";
            // 
            // toolStripMenuItem9
            // 
            this.toolStripMenuItem9.Name = "toolStripMenuItem9";
            this.toolStripMenuItem9.Size = new System.Drawing.Size(83, 22);
            this.toolStripMenuItem9.Text = "&1";
            // 
            // toolStripMenuItem10
            // 
            this.toolStripMenuItem10.Name = "toolStripMenuItem10";
            this.toolStripMenuItem10.Size = new System.Drawing.Size(83, 22);
            this.toolStripMenuItem10.Text = "&2";
            // 
            // toolStripMenuItem11
            // 
            this.toolStripMenuItem11.Name = "toolStripMenuItem11";
            this.toolStripMenuItem11.Size = new System.Drawing.Size(83, 22);
            this.toolStripMenuItem11.Text = "&3";
            // 
            // toolStripMenuItem12
            // 
            this.toolStripMenuItem12.Name = "toolStripMenuItem12";
            this.toolStripMenuItem12.Size = new System.Drawing.Size(83, 22);
            this.toolStripMenuItem12.Text = "&4";
            // 
            // toolStripMenuItem13
            // 
            this.toolStripMenuItem13.Name = "toolStripMenuItem13";
            this.toolStripMenuItem13.Size = new System.Drawing.Size(83, 22);
            this.toolStripMenuItem13.Text = "&5";
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(135, 6);
            // 
            // FileExit
            // 
            this.FileExit.Name = "FileExit";
            this.FileExit.Size = new System.Drawing.Size(138, 22);
            this.FileExit.Text = "E&xit";
            this.FileExit.Click += new System.EventHandler(this.FileExit_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OptionsUseBios,
            this.OptionsSkipBios,
            this.OptionsBiosFile,
            this.toolStripSeparator2,
            this.OptionsSize,
            this.renderersToolStripMenuItem,
            this.OptionsLimitFps,
            this.OptionsVsync,
            this.toolStripSeparator4,
            this.EnableSound});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(63, 20);
            this.optionsToolStripMenuItem.Text = "&Options";
            // 
            // OptionsUseBios
            // 
            this.OptionsUseBios.Checked = true;
            this.OptionsUseBios.CheckOnClick = true;
            this.OptionsUseBios.CheckState = System.Windows.Forms.CheckState.Checked;
            this.OptionsUseBios.Name = "OptionsUseBios";
            this.OptionsUseBios.Size = new System.Drawing.Size(154, 22);
            this.OptionsUseBios.Text = "Use &Bios";
            // 
            // OptionsSkipBios
            // 
            this.OptionsSkipBios.Checked = true;
            this.OptionsSkipBios.CheckOnClick = true;
            this.OptionsSkipBios.CheckState = System.Windows.Forms.CheckState.Checked;
            this.OptionsSkipBios.Name = "OptionsSkipBios";
            this.OptionsSkipBios.Size = new System.Drawing.Size(154, 22);
            this.OptionsSkipBios.Text = "&Skip Bios";
            this.OptionsSkipBios.Click += new System.EventHandler(this.OptionsSkipBios_Click);
            // 
            // OptionsBiosFile
            // 
            this.OptionsBiosFile.Name = "OptionsBiosFile";
            this.OptionsBiosFile.Size = new System.Drawing.Size(154, 22);
            this.OptionsBiosFile.Text = "Bios &File";
            this.OptionsBiosFile.Click += new System.EventHandler(this.OptionsBiosFile_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(151, 6);
            // 
            // OptionsSize
            // 
            this.OptionsSize.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OptionsSizex1,
            this.OptionsSizex2,
            this.OptionsSizex3});
            this.OptionsSize.Name = "OptionsSize";
            this.OptionsSize.Size = new System.Drawing.Size(154, 22);
            this.OptionsSize.Text = "&Size";
            // 
            // OptionsSizex1
            // 
            this.OptionsSizex1.CheckOnClick = true;
            this.OptionsSizex1.Name = "OptionsSizex1";
            this.OptionsSizex1.Size = new System.Drawing.Size(89, 22);
            this.OptionsSizex1.Text = "x&1";
            this.OptionsSizex1.Click += new System.EventHandler(this.OptionsSizex1_Click);
            // 
            // OptionsSizex2
            // 
            this.OptionsSizex2.Checked = true;
            this.OptionsSizex2.CheckOnClick = true;
            this.OptionsSizex2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.OptionsSizex2.Name = "OptionsSizex2";
            this.OptionsSizex2.Size = new System.Drawing.Size(89, 22);
            this.OptionsSizex2.Text = "x&2";
            this.OptionsSizex2.Click += new System.EventHandler(this.OptionsSizex2_Click);
            // 
            // OptionsSizex3
            // 
            this.OptionsSizex3.CheckOnClick = true;
            this.OptionsSizex3.Name = "OptionsSizex3";
            this.OptionsSizex3.Size = new System.Drawing.Size(89, 22);
            this.OptionsSizex3.Text = "x&3";
            this.OptionsSizex3.Click += new System.EventHandler(this.OptionsSizex3_Click);
            // 
            // renderersToolStripMenuItem
            // 
            this.renderersToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OptionsRenderersD3D,
            this.OptionsRenderersShader,
            this.OptionsRenderersGDI});
            this.renderersToolStripMenuItem.Name = "renderersToolStripMenuItem";
            this.renderersToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.renderersToolStripMenuItem.Text = "&Renderers";
            // 
            // OptionsRenderersD3D
            // 
            this.OptionsRenderersD3D.Checked = true;
            this.OptionsRenderersD3D.CheckOnClick = true;
            this.OptionsRenderersD3D.CheckState = System.Windows.Forms.CheckState.Checked;
            this.OptionsRenderersD3D.Name = "OptionsRenderersD3D";
            this.OptionsRenderersD3D.Size = new System.Drawing.Size(117, 22);
            this.OptionsRenderersD3D.Text = "D3D";
            this.OptionsRenderersD3D.Click += new System.EventHandler(this.OptionsRenderersD3D_Click);
            // 
            // OptionsRenderersShader
            // 
            this.OptionsRenderersShader.CheckOnClick = true;
            this.OptionsRenderersShader.Name = "OptionsRenderersShader";
            this.OptionsRenderersShader.Size = new System.Drawing.Size(117, 22);
            this.OptionsRenderersShader.Text = "Shader";
            this.OptionsRenderersShader.Click += new System.EventHandler(this.OptionsRenderersShader_Click);
            // 
            // OptionsRenderersGDI
            // 
            this.OptionsRenderersGDI.CheckOnClick = true;
            this.OptionsRenderersGDI.Enabled = false;
            this.OptionsRenderersGDI.Name = "OptionsRenderersGDI";
            this.OptionsRenderersGDI.Size = new System.Drawing.Size(117, 22);
            this.OptionsRenderersGDI.Text = "GDI";
            this.OptionsRenderersGDI.Click += new System.EventHandler(this.OptionsRenderersGDI_Click);
            // 
            // OptionsLimitFps
            // 
            this.OptionsLimitFps.Checked = true;
            this.OptionsLimitFps.CheckOnClick = true;
            this.OptionsLimitFps.CheckState = System.Windows.Forms.CheckState.Checked;
            this.OptionsLimitFps.Name = "OptionsLimitFps";
            this.OptionsLimitFps.Size = new System.Drawing.Size(154, 22);
            this.OptionsLimitFps.Text = "&Limit FPS";
            this.OptionsLimitFps.Click += new System.EventHandler(this.OptionsLimitFps_Click);
            // 
            // OptionsVsync
            // 
            this.OptionsVsync.CheckOnClick = true;
            this.OptionsVsync.Name = "OptionsVsync";
            this.OptionsVsync.Size = new System.Drawing.Size(154, 22);
            this.OptionsVsync.Text = "&Vsync";
            this.OptionsVsync.Click += new System.EventHandler(this.OptionsVsync_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(151, 6);
            // 
            // EnableSound
            // 
            this.EnableSound.Checked = true;
            this.EnableSound.CheckState = System.Windows.Forms.CheckState.Checked;
            this.EnableSound.Name = "EnableSound";
            this.EnableSound.Size = new System.Drawing.Size(154, 22);
            this.EnableSound.Text = "Enable Sound";
            this.EnableSound.Click += new System.EventHandler(this.enableSoundToolStripMenuItem_Click);
            // 
            // debugToolStripMenuItem
            // 
            this.debugToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DebugDissassembly,
            this.DebugPalette,
            this.spritesToolStripMenuItem,
            this.dumpOutputToolStripMenuItem});
            this.debugToolStripMenuItem.Name = "debugToolStripMenuItem";
            this.debugToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
            this.debugToolStripMenuItem.Text = "&Debug";
            // 
            // DebugDissassembly
            // 
            this.DebugDissassembly.Name = "DebugDissassembly";
            this.DebugDissassembly.Size = new System.Drawing.Size(152, 22);
            this.DebugDissassembly.Text = "&Dissassembly";
            this.DebugDissassembly.Click += new System.EventHandler(this.DebugDissassembly_Click);
            // 
            // DebugPalette
            // 
            this.DebugPalette.Name = "DebugPalette";
            this.DebugPalette.Size = new System.Drawing.Size(152, 22);
            this.DebugPalette.Text = "&Palette";
            this.DebugPalette.Click += new System.EventHandler(this.DebugPalette_Click);
            // 
            // spritesToolStripMenuItem
            // 
            this.spritesToolStripMenuItem.Name = "spritesToolStripMenuItem";
            this.spritesToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.spritesToolStripMenuItem.Text = "&Sprites";
            this.spritesToolStripMenuItem.Click += new System.EventHandler(this.DebugSprites_Click);
            // 
            // dumpOutputToolStripMenuItem
            // 
            this.dumpOutputToolStripMenuItem.Name = "dumpOutputToolStripMenuItem";
            this.dumpOutputToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.dumpOutputToolStripMenuItem.Text = "Dump &Output";
            // 
            // StatusStrip
            // 
            this.StatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FPSPanel});
            this.StatusStrip.Location = new System.Drawing.Point(0, 248);
            this.StatusStrip.Name = "StatusStrip";
            this.StatusStrip.Size = new System.Drawing.Size(292, 22);
            this.StatusStrip.TabIndex = 0;
            this.StatusStrip.Text = "statusStrip1";
            // 
            // FPSPanel
            // 
            this.FPSPanel.BackColor = System.Drawing.Color.Transparent;
            this.FPSPanel.Name = "FPSPanel";
            this.FPSPanel.Size = new System.Drawing.Size(35, 17);
            this.FPSPanel.Text = "FPS:";
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(292, 270);
            this.Controls.Add(this.StatusStrip);
            this.Controls.Add(this.MainMenu);
            this.MainMenuStrip = this.MainMenu;
            this.Name = "MainWindow";
            this.Text = "GarboDev";
            this.MainMenu.ResumeLayout(false);
            this.MainMenu.PerformLayout();
            this.StatusStrip.ResumeLayout(false);
            this.StatusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip MainMenu;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FileOpen;
        private System.Windows.Forms.ToolStripMenuItem FileExit;
        private System.Windows.Forms.ToolStripMenuItem debugToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem DebugDissassembly;
        private System.Windows.Forms.ToolStripMenuItem DebugPalette;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FileReset;
        private System.Windows.Forms.ToolStripMenuItem FilePause;
        private System.Windows.Forms.ToolStripSeparator sep1ToolStripSeparator;
        private System.Windows.Forms.StatusStrip StatusStrip;
        private System.Windows.Forms.ToolStripStatusLabel FPSPanel;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem spritesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OptionsUseBios;
        private System.Windows.Forms.ToolStripMenuItem OptionsBiosFile;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem OptionsLimitFps;
        private System.Windows.Forms.ToolStripMenuItem OptionsSkipBios;
        private System.Windows.Forms.ToolStripMenuItem OptionsVsync;
        private System.Windows.Forms.ToolStripMenuItem renderersToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OptionsRenderersD3D;
        private System.Windows.Forms.ToolStripMenuItem OptionsRenderersShader;
        private System.Windows.Forms.ToolStripMenuItem OptionsRenderersGDI;
        private System.Windows.Forms.ToolStripMenuItem OptionsSize;
        private System.Windows.Forms.ToolStripMenuItem OptionsSizex1;
        private System.Windows.Forms.ToolStripMenuItem OptionsSizex2;
        private System.Windows.Forms.ToolStripMenuItem OptionsSizex3;
        private System.Windows.Forms.ToolStripMenuItem loadStateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem5;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem6;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem7;
        private System.Windows.Forms.ToolStripMenuItem saveStateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem8;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem9;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem10;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem11;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem12;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem13;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem dumpOutputToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem EnableSound;
    }
}

