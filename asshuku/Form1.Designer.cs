namespace asshuku {
    partial class Form1 {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.logs = new System.Windows.Forms.ListBox();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButton3 = new System.Windows.Forms.RadioButton();
            this.radioButton4 = new System.Windows.Forms.RadioButton();
            this.radioButton5 = new System.Windows.Forms.RadioButton();
            this.radioButton6 = new System.Windows.Forms.RadioButton();
            this.OptimizeTheImages = new System.Windows.Forms.RadioButton();
            this.DoNotOptimizeTheImages = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.WeakMode = new System.Windows.Forms.RadioButton();
            this.StrongMode = new System.Windows.Forms.RadioButton();
            this.MidMode = new System.Windows.Forms.RadioButton();
            this.PNGout = new System.Windows.Forms.CheckBox();
            this.BrowserButton = new System.Windows.Forms.Button();
            this.WhetherToRename = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // logs
            // 
            this.logs.BackColor = System.Drawing.SystemColors.Window;
            this.logs.Font = new System.Drawing.Font("Times New Roman", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.logs.FormattingEnabled = true;
            this.logs.ItemHeight = 9;
            this.logs.Location = new System.Drawing.Point(1, 108);
            this.logs.Name = "logs";
            this.logs.Size = new System.Drawing.Size(501, 751);
            this.logs.TabIndex = 3;
            // 
            // richTextBox1
            // 
            this.richTextBox1.Font = new System.Drawing.Font("ＭＳ 明朝", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.richTextBox1.Location = new System.Drawing.Point(1, 1);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(275, 101);
            this.richTextBox1.TabIndex = 5;
            this.richTextBox1.Text = "";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(508, 204);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(107, 106);
            this.button1.TabIndex = 6;
            this.button1.Text = "1.Rename           \r\n2.Margin remove  \r\n3.PNG optimize    \r\n4.zip rar 7z         " +
    "        \r\n画像入りフォルダをコピーしてここを押す.\r\n複数フォルダも可能.";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.Button1_Click);
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Location = new System.Drawing.Point(6, 18);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(90, 16);
            this.radioButton1.TabIndex = 7;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "zip (7z 19.00)";
            this.radioButton1.UseVisualStyleBackColor = true;
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(6, 46);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(87, 16);
            this.radioButton2.TabIndex = 8;
            this.radioButton2.Text = "7z (7z 19.00)";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // radioButton3
            // 
            this.radioButton3.AutoSize = true;
            this.radioButton3.Checked = true;
            this.radioButton3.Location = new System.Drawing.Point(6, 74);
            this.radioButton3.Name = "radioButton3";
            this.radioButton3.Size = new System.Drawing.Size(93, 16);
            this.radioButton3.TabIndex = 9;
            this.radioButton3.TabStop = true;
            this.radioButton3.Text = "rar (Rar 5.3.1)";
            this.radioButton3.UseVisualStyleBackColor = true;
            // 
            // radioButton4
            // 
            this.radioButton4.AutoSize = true;
            this.radioButton4.Location = new System.Drawing.Point(6, 31);
            this.radioButton4.Name = "radioButton4";
            this.radioButton4.Size = new System.Drawing.Size(44, 16);
            this.radioButton4.TabIndex = 10;
            this.radioButton4.Text = "Max";
            this.radioButton4.UseVisualStyleBackColor = true;
            // 
            // radioButton5
            // 
            this.radioButton5.AutoSize = true;
            this.radioButton5.Checked = true;
            this.radioButton5.Location = new System.Drawing.Point(6, 54);
            this.radioButton5.Name = "radioButton5";
            this.radioButton5.Size = new System.Drawing.Size(60, 16);
            this.radioButton5.TabIndex = 11;
            this.radioButton5.TabStop = true;
            this.radioButton5.Text = "Default";
            this.radioButton5.UseVisualStyleBackColor = true;
            // 
            // radioButton6
            // 
            this.radioButton6.AutoSize = true;
            this.radioButton6.Location = new System.Drawing.Point(6, 77);
            this.radioButton6.Name = "radioButton6";
            this.radioButton6.Size = new System.Drawing.Size(49, 16);
            this.radioButton6.TabIndex = 12;
            this.radioButton6.Text = "None";
            this.radioButton6.UseVisualStyleBackColor = true;
            // 
            // OptimizeTheImages
            // 
            this.OptimizeTheImages.AutoSize = true;
            this.OptimizeTheImages.Checked = true;
            this.OptimizeTheImages.Location = new System.Drawing.Point(6, 14);
            this.OptimizeTheImages.Name = "OptimizeTheImages";
            this.OptimizeTheImages.Size = new System.Drawing.Size(64, 16);
            this.OptimizeTheImages.TabIndex = 0;
            this.OptimizeTheImages.TabStop = true;
            this.OptimizeTheImages.Text = "Execute";
            this.OptimizeTheImages.UseVisualStyleBackColor = true;
            // 
            // DoNotOptimizeTheImages
            // 
            this.DoNotOptimizeTheImages.AutoSize = true;
            this.DoNotOptimizeTheImages.Location = new System.Drawing.Point(79, 14);
            this.DoNotOptimizeTheImages.Name = "DoNotOptimizeTheImages";
            this.DoNotOptimizeTheImages.Size = new System.Drawing.Size(49, 16);
            this.DoNotOptimizeTheImages.TabIndex = 1;
            this.DoNotOptimizeTheImages.TabStop = true;
            this.DoNotOptimizeTheImages.Text = "Don\'t";
            this.DoNotOptimizeTheImages.UseVisualStyleBackColor = true;
            this.DoNotOptimizeTheImages.CheckedChanged += new System.EventHandler(this.DoNotOptimizeTheImages_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioButton1);
            this.groupBox1.Controls.Add(this.radioButton2);
            this.groupBox1.Controls.Add(this.radioButton3);
            this.groupBox1.Location = new System.Drawing.Point(508, 102);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(107, 96);
            this.groupBox1.TabIndex = 11;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Compress format";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.radioButton6);
            this.groupBox2.Controls.Add(this.radioButton5);
            this.groupBox2.Controls.Add(this.radioButton4);
            this.groupBox2.Location = new System.Drawing.Point(543, 1);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(71, 100);
            this.groupBox2.TabIndex = 12;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Compress Level";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.groupBox4);
            this.groupBox3.Controls.Add(this.PNGout);
            this.groupBox3.Controls.Add(this.DoNotOptimizeTheImages);
            this.groupBox3.Controls.Add(this.OptimizeTheImages);
            this.groupBox3.Location = new System.Drawing.Point(332, 16);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(208, 86);
            this.groupBox3.TabIndex = 14;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "MarginRemove + PNGOptimize";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.WeakMode);
            this.groupBox4.Controls.Add(this.StrongMode);
            this.groupBox4.Controls.Add(this.MidMode);
            this.groupBox4.Location = new System.Drawing.Point(1, 30);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(207, 36);
            this.groupBox4.TabIndex = 21;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Shaving";
            // 
            // WeakMode
            // 
            this.WeakMode.AutoSize = true;
            this.WeakMode.Location = new System.Drawing.Point(3, 14);
            this.WeakMode.Name = "WeakMode";
            this.WeakMode.Size = new System.Drawing.Size(83, 16);
            this.WeakMode.TabIndex = 18;
            this.WeakMode.Text = "Weak(Novel";
            this.WeakMode.UseVisualStyleBackColor = true;
            // 
            // StrongMode
            // 
            this.StrongMode.AutoSize = true;
            this.StrongMode.Location = new System.Drawing.Point(122, 14);
            this.StrongMode.Name = "StrongMode";
            this.StrongMode.Size = new System.Drawing.Size(85, 16);
            this.StrongMode.TabIndex = 20;
            this.StrongMode.Text = "Strong(Dirty";
            this.StrongMode.UseVisualStyleBackColor = true;
            // 
            // MidMode
            // 
            this.MidMode.AutoSize = true;
            this.MidMode.Checked = true;
            this.MidMode.Location = new System.Drawing.Point(86, 14);
            this.MidMode.Name = "MidMode";
            this.MidMode.Size = new System.Drawing.Size(41, 16);
            this.MidMode.TabIndex = 19;
            this.MidMode.TabStop = true;
            this.MidMode.Text = "Mid";
            this.MidMode.UseVisualStyleBackColor = true;
            // 
            // PNGout
            // 
            this.PNGout.AutoSize = true;
            this.PNGout.Checked = true;
            this.PNGout.CheckState = System.Windows.Forms.CheckState.Checked;
            this.PNGout.Location = new System.Drawing.Point(4, 66);
            this.PNGout.Name = "PNGout";
            this.PNGout.Size = new System.Drawing.Size(63, 16);
            this.PNGout.TabIndex = 17;
            this.PNGout.Text = "PNGout";
            this.PNGout.UseVisualStyleBackColor = true;
            // 
            // BrowserButton
            // 
            this.BrowserButton.Location = new System.Drawing.Point(276, 1);
            this.BrowserButton.Name = "BrowserButton";
            this.BrowserButton.Size = new System.Drawing.Size(55, 101);
            this.BrowserButton.TabIndex = 13;
            this.BrowserButton.Text = "Folder\r\nBrowser\r\nDialog";
            this.BrowserButton.UseVisualStyleBackColor = true;
            this.BrowserButton.Click += new System.EventHandler(this.BrowserButtonClick);
            // 
            // WhetherToRename
            // 
            this.WhetherToRename.AutoSize = true;
            this.WhetherToRename.Checked = true;
            this.WhetherToRename.CheckState = System.Windows.Forms.CheckState.Checked;
            this.WhetherToRename.Location = new System.Drawing.Point(337, 1);
            this.WhetherToRename.Name = "WhetherToRename";
            this.WhetherToRename.Size = new System.Drawing.Size(125, 16);
            this.WhetherToRename.TabIndex = 15;
            this.WhetherToRename.Text = "ExecutFilesRename";
            this.WhetherToRename.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(615, 314);
            this.Controls.Add(this.WhetherToRename);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.BrowserButton);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.logs);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "Rename->PNGOptimize->Zip";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox logs;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButton3;
        private System.Windows.Forms.RadioButton radioButton4;
        private System.Windows.Forms.RadioButton radioButton5;
        private System.Windows.Forms.RadioButton radioButton6;
        private System.Windows.Forms.RadioButton OptimizeTheImages;
        private System.Windows.Forms.RadioButton DoNotOptimizeTheImages;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button BrowserButton;
        private System.Windows.Forms.CheckBox WhetherToRename;
        private System.Windows.Forms.CheckBox PNGout;
        private System.Windows.Forms.RadioButton StrongMode;
        private System.Windows.Forms.RadioButton MidMode;
        private System.Windows.Forms.RadioButton WeakMode;
        private System.Windows.Forms.GroupBox groupBox4;
    }
}
