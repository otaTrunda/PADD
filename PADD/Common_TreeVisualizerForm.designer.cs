namespace PADD
{
    partial class TreeVisualizerForm
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
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.Generate_button = new System.Windows.Forms.Button();
            this.peaks_trackBar = new System.Windows.Forms.TrackBar();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.smooth_trackBar = new System.Windows.Forms.TrackBar();
            this.Save_button = new System.Windows.Forms.Button();
            this.Load_button = new System.Windows.Forms.Button();
            this.saveFunc_Dialog = new System.Windows.Forms.SaveFileDialog();
            this.loadFunc_Dialog = new System.Windows.Forms.OpenFileDialog();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.GoSteps_button = new System.Windows.Forms.Button();
            this.steps_trackBar = new System.Windows.Forms.TrackBar();
            this.label3 = new System.Windows.Forms.Label();
            this.Visited_label = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.peaks_trackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.smooth_trackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.steps_trackBar)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(12, 393);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(1152, 321);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // Generate_button
            // 
            this.Generate_button.Location = new System.Drawing.Point(13, 728);
            this.Generate_button.Name = "Generate_button";
            this.Generate_button.Size = new System.Drawing.Size(123, 23);
            this.Generate_button.TabIndex = 1;
            this.Generate_button.Text = "generateNew";
            this.Generate_button.UseVisualStyleBackColor = true;
            this.Generate_button.Click += new System.EventHandler(this.Generate_button_Click);
            // 
            // peaks_trackBar
            // 
            this.peaks_trackBar.Location = new System.Drawing.Point(207, 720);
            this.peaks_trackBar.Name = "peaks_trackBar";
            this.peaks_trackBar.Size = new System.Drawing.Size(125, 45);
            this.peaks_trackBar.TabIndex = 2;
            this.peaks_trackBar.Value = 7;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(142, 733);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "oscilations:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(338, 733);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(66, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "smoothness:";
            // 
            // smooth_trackBar
            // 
            this.smooth_trackBar.Location = new System.Drawing.Point(403, 720);
            this.smooth_trackBar.Name = "smooth_trackBar";
            this.smooth_trackBar.Size = new System.Drawing.Size(124, 45);
            this.smooth_trackBar.TabIndex = 4;
            this.smooth_trackBar.Value = 8;
            // 
            // Save_button
            // 
            this.Save_button.Location = new System.Drawing.Point(533, 724);
            this.Save_button.Name = "Save_button";
            this.Save_button.Size = new System.Drawing.Size(73, 31);
            this.Save_button.TabIndex = 6;
            this.Save_button.Text = "Save";
            this.Save_button.UseVisualStyleBackColor = true;
            this.Save_button.Click += new System.EventHandler(this.Save_button_Click);
            // 
            // Load_button
            // 
            this.Load_button.Location = new System.Drawing.Point(612, 724);
            this.Load_button.Name = "Load_button";
            this.Load_button.Size = new System.Drawing.Size(73, 31);
            this.Load_button.TabIndex = 7;
            this.Load_button.Text = "Load";
            this.Load_button.UseVisualStyleBackColor = true;
            this.Load_button.Click += new System.EventHandler(this.Load_button_Click);
            // 
            // saveFunc_Dialog
            // 
            this.saveFunc_Dialog.DefaultExt = "bin";
            this.saveFunc_Dialog.FileName = "func";
            this.saveFunc_Dialog.Filter = "Binary files|*.bin";
            this.saveFunc_Dialog.FileOk += new System.ComponentModel.CancelEventHandler(this.saveFunc_Dialog_FileOk);
            // 
            // loadFunc_Dialog
            // 
            this.loadFunc_Dialog.DefaultExt = "bin";
            this.loadFunc_Dialog.Filter = "Binary files|*.bin";
            this.loadFunc_Dialog.FileOk += new System.ComponentModel.CancelEventHandler(this.loadFunc_Dialog_FileOk);
            // 
            // pictureBox2
            // 
            this.pictureBox2.Location = new System.Drawing.Point(12, 12);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(1152, 375);
            this.pictureBox2.TabIndex = 8;
            this.pictureBox2.TabStop = false;
            this.pictureBox2.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox2_MouseMove);
            // 
            // GoSteps_button
            // 
            this.GoSteps_button.Location = new System.Drawing.Point(1027, 724);
            this.GoSteps_button.Name = "GoSteps_button";
            this.GoSteps_button.Size = new System.Drawing.Size(137, 31);
            this.GoSteps_button.TabIndex = 9;
            this.GoSteps_button.Text = "Perform steps and draw";
            this.GoSteps_button.UseVisualStyleBackColor = true;
            this.GoSteps_button.Click += new System.EventHandler(this.GoSteps_button_Click);
            // 
            // steps_trackBar
            // 
            this.steps_trackBar.Location = new System.Drawing.Point(897, 720);
            this.steps_trackBar.Name = "steps_trackBar";
            this.steps_trackBar.Size = new System.Drawing.Size(124, 45);
            this.steps_trackBar.TabIndex = 10;
            this.steps_trackBar.Value = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(856, 733);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 13);
            this.label3.TabIndex = 11;
            this.label3.Text = "steps:";
            // 
            // Visited_label
            // 
            this.Visited_label.AutoSize = true;
            this.Visited_label.Location = new System.Drawing.Point(725, 733);
            this.Visited_label.Name = "Visited_label";
            this.Visited_label.Size = new System.Drawing.Size(30, 13);
            this.Visited_label.TabIndex = 12;
            this.Visited_label.Text = "visits";
            // 
            // TreeVisualizerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1176, 763);
            this.Controls.Add(this.Visited_label);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.steps_trackBar);
            this.Controls.Add(this.GoSteps_button);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.Load_button);
            this.Controls.Add(this.Save_button);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.smooth_trackBar);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.peaks_trackBar);
            this.Controls.Add(this.Generate_button);
            this.Controls.Add(this.pictureBox1);
            this.Name = "TreeVisualizerForm";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.peaks_trackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.smooth_trackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.steps_trackBar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button Generate_button;
        private System.Windows.Forms.TrackBar peaks_trackBar;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TrackBar smooth_trackBar;
        private System.Windows.Forms.Button Save_button;
        private System.Windows.Forms.Button Load_button;
        private System.Windows.Forms.SaveFileDialog saveFunc_Dialog;
        private System.Windows.Forms.OpenFileDialog loadFunc_Dialog;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Button GoSteps_button;
        private System.Windows.Forms.TrackBar steps_trackBar;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label Visited_label;
    }
}

