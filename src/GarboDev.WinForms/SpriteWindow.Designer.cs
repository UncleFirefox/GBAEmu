namespace GarboDev.WinForms
{
    partial class SpriteWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
            this.spriteSelector = new System.Windows.Forms.TrackBar();
            this.spriteNumber = new System.Windows.Forms.TextBox();
            this.spriteDisplay = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.spriteLeft = new System.Windows.Forms.Label();
            this.spriteTop = new System.Windows.Forms.Label();
            this.spriteWidth = new System.Windows.Forms.Label();
            this.spriteHeight = new System.Windows.Forms.Label();
            this.spriteBase = new System.Windows.Forms.Label();
            this.spritePri = new System.Windows.Forms.Label();
            this.spritePal = new System.Windows.Forms.Label();
            this.spriteMode = new System.Windows.Forms.Label();
            this.spriteBlend = new System.Windows.Forms.Label();
            this.spriteWind = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.spriteSelector)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.spriteDisplay)).BeginInit();
            this.SuspendLayout();
            // 
            // spriteSelector
            // 
            this.spriteSelector.Location = new System.Drawing.Point(12, 39);
            this.spriteSelector.Maximum = 127;
            this.spriteSelector.Name = "spriteSelector";
            this.spriteSelector.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.spriteSelector.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.spriteSelector.Size = new System.Drawing.Size(48, 158);
            this.spriteSelector.TabIndex = 0;
            this.spriteSelector.TickStyle = System.Windows.Forms.TickStyle.TopLeft;
            this.spriteSelector.Scroll += new System.EventHandler(this.SpriteSelector_Scroll);
            // 
            // spriteNumber
            // 
            this.spriteNumber.Location = new System.Drawing.Point(13, 13);
            this.spriteNumber.Name = "spriteNumber";
            this.spriteNumber.Size = new System.Drawing.Size(47, 20);
            this.spriteNumber.TabIndex = 1;
            // 
            // spriteDisplay
            // 
            this.spriteDisplay.Location = new System.Drawing.Point(182, 12);
            this.spriteDisplay.Name = "spriteDisplay";
            this.spriteDisplay.Size = new System.Drawing.Size(128, 128);
            this.spriteDisplay.TabIndex = 2;
            this.spriteDisplay.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(79, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(26, 14);
            this.label1.TabIndex = 3;
            this.label1.Text = "Left:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(78, 32);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(27, 14);
            this.label2.TabIndex = 4;
            this.label2.Text = "Top:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(69, 52);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(36, 14);
            this.label3.TabIndex = 5;
            this.label3.Text = "Width:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(65, 72);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(40, 14);
            this.label4.TabIndex = 6;
            this.label4.Text = "Height:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(72, 92);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(33, 14);
            this.label5.TabIndex = 7;
            this.label5.Text = "Base:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(84, 112);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(21, 14);
            this.label6.TabIndex = 8;
            this.label6.Text = "Pri:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(62, 132);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(43, 14);
            this.label7.TabIndex = 9;
            this.label7.Text = "Palette:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(69, 152);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(36, 14);
            this.label8.TabIndex = 10;
            this.label8.Text = "Mode:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(69, 172);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(36, 14);
            this.label9.TabIndex = 11;
            this.label9.Text = "Blend:";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(57, 192);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(48, 14);
            this.label10.TabIndex = 12;
            this.label10.Text = "Window:";
            // 
            // spriteLeft
            // 
            this.spriteLeft.AutoSize = true;
            this.spriteLeft.Location = new System.Drawing.Point(112, 13);
            this.spriteLeft.Name = "spriteLeft";
            this.spriteLeft.Size = new System.Drawing.Size(41, 14);
            this.spriteLeft.TabIndex = 13;
            this.spriteLeft.Text = "label11";
            // 
            // spriteTop
            // 
            this.spriteTop.AutoSize = true;
            this.spriteTop.Location = new System.Drawing.Point(112, 33);
            this.spriteTop.Name = "spriteTop";
            this.spriteTop.Size = new System.Drawing.Size(41, 14);
            this.spriteTop.TabIndex = 14;
            this.spriteTop.Text = "label11";
            // 
            // spriteWidth
            // 
            this.spriteWidth.AutoSize = true;
            this.spriteWidth.Location = new System.Drawing.Point(112, 53);
            this.spriteWidth.Name = "spriteWidth";
            this.spriteWidth.Size = new System.Drawing.Size(41, 14);
            this.spriteWidth.TabIndex = 15;
            this.spriteWidth.Text = "label11";
            // 
            // spriteHeight
            // 
            this.spriteHeight.AutoSize = true;
            this.spriteHeight.Location = new System.Drawing.Point(112, 73);
            this.spriteHeight.Name = "spriteHeight";
            this.spriteHeight.Size = new System.Drawing.Size(41, 14);
            this.spriteHeight.TabIndex = 16;
            this.spriteHeight.Text = "label11";
            // 
            // spriteBase
            // 
            this.spriteBase.AutoSize = true;
            this.spriteBase.Location = new System.Drawing.Point(112, 93);
            this.spriteBase.Name = "spriteBase";
            this.spriteBase.Size = new System.Drawing.Size(41, 14);
            this.spriteBase.TabIndex = 17;
            this.spriteBase.Text = "label11";
            // 
            // spritePri
            // 
            this.spritePri.AutoSize = true;
            this.spritePri.Location = new System.Drawing.Point(112, 113);
            this.spritePri.Name = "spritePri";
            this.spritePri.Size = new System.Drawing.Size(41, 14);
            this.spritePri.TabIndex = 18;
            this.spritePri.Text = "label11";
            // 
            // spritePal
            // 
            this.spritePal.AutoSize = true;
            this.spritePal.Location = new System.Drawing.Point(112, 133);
            this.spritePal.Name = "spritePal";
            this.spritePal.Size = new System.Drawing.Size(41, 14);
            this.spritePal.TabIndex = 19;
            this.spritePal.Text = "label11";
            // 
            // spriteMode
            // 
            this.spriteMode.AutoSize = true;
            this.spriteMode.Location = new System.Drawing.Point(112, 153);
            this.spriteMode.Name = "spriteMode";
            this.spriteMode.Size = new System.Drawing.Size(41, 14);
            this.spriteMode.TabIndex = 20;
            this.spriteMode.Text = "label11";
            // 
            // spriteBlend
            // 
            this.spriteBlend.AutoSize = true;
            this.spriteBlend.Location = new System.Drawing.Point(112, 173);
            this.spriteBlend.Name = "spriteBlend";
            this.spriteBlend.Size = new System.Drawing.Size(41, 14);
            this.spriteBlend.TabIndex = 21;
            this.spriteBlend.Text = "label11";
            // 
            // spriteWind
            // 
            this.spriteWind.AutoSize = true;
            this.spriteWind.Location = new System.Drawing.Point(112, 193);
            this.spriteWind.Name = "spriteWind";
            this.spriteWind.Size = new System.Drawing.Size(41, 14);
            this.spriteWind.TabIndex = 22;
            this.spriteWind.Text = "label11";
            // 
            // SpriteWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(320, 218);
            this.Controls.Add(this.spriteWind);
            this.Controls.Add(this.spriteBlend);
            this.Controls.Add(this.spriteMode);
            this.Controls.Add(this.spritePal);
            this.Controls.Add(this.spritePri);
            this.Controls.Add(this.spriteBase);
            this.Controls.Add(this.spriteHeight);
            this.Controls.Add(this.spriteWidth);
            this.Controls.Add(this.spriteTop);
            this.Controls.Add(this.spriteLeft);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.spriteDisplay);
            this.Controls.Add(this.spriteNumber);
            this.Controls.Add(this.spriteSelector);
            this.Name = "SpriteWindow";
            this.Text = "SpriteWindow";
            ((System.ComponentModel.ISupportInitialize)(this.spriteSelector)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.spriteDisplay)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TrackBar spriteSelector;
        private System.Windows.Forms.TextBox spriteNumber;
        private System.Windows.Forms.PictureBox spriteDisplay;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label spriteLeft;
        private System.Windows.Forms.Label spriteTop;
        private System.Windows.Forms.Label spriteWidth;
        private System.Windows.Forms.Label spriteHeight;
        private System.Windows.Forms.Label spriteBase;
        private System.Windows.Forms.Label spritePri;
        private System.Windows.Forms.Label spritePal;
        private System.Windows.Forms.Label spriteMode;
        private System.Windows.Forms.Label spriteBlend;
        private System.Windows.Forms.Label spriteWind;

    }
}