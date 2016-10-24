namespace Joiner
{
    partial class Configuration
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.rulesText = new System.Windows.Forms.TextBox();
            this.rulesLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // rulesText
            // 
            this.rulesText.AcceptsReturn = true;
            this.rulesText.AcceptsTab = true;
            this.rulesText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rulesText.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::Joiner.Properties.Settings.Default, "CustomRules", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.rulesText.Location = new System.Drawing.Point(3, 26);
            this.rulesText.Multiline = true;
            this.rulesText.Name = "rulesText";
            this.rulesText.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.rulesText.Size = new System.Drawing.Size(605, 398);
            this.rulesText.TabIndex = 0;
            this.rulesText.Text = global::Joiner.Properties.Settings.Default.CustomRules;
            // 
            // rulesLabel
            // 
            this.rulesLabel.AutoSize = true;
            this.rulesLabel.Location = new System.Drawing.Point(3, 6);
            this.rulesLabel.Name = "rulesLabel";
            this.rulesLabel.Size = new System.Drawing.Size(94, 17);
            this.rulesLabel.TabIndex = 1;
            this.rulesLabel.Text = "Custom rules:";
            // 
            // Configuration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.rulesLabel);
            this.Controls.Add(this.rulesText);
            this.Name = "Configuration";
            this.Size = new System.Drawing.Size(611, 427);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox rulesText;
        private System.Windows.Forms.Label rulesLabel;
    }
}
