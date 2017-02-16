namespace Precomplete
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
            this.databasesLabel = new System.Windows.Forms.Label();
            this.databasesTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // databasesLabel
            // 
            this.databasesLabel.AutoSize = true;
            this.databasesLabel.Location = new System.Drawing.Point(3, 9);
            this.databasesLabel.Name = "databasesLabel";
            this.databasesLabel.Size = new System.Drawing.Size(211, 17);
            this.databasesLabel.TabIndex = 0;
            this.databasesLabel.Text = "Comma separated database list:";
            // 
            // databasesTextBox
            // 
            this.databasesTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.databasesTextBox.Location = new System.Drawing.Point(6, 29);
            this.databasesTextBox.Name = "databasesTextBox";
            this.databasesTextBox.Size = new System.Drawing.Size(415, 22);
            this.databasesTextBox.TabIndex = 1;
            // 
            // Configuration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.databasesTextBox);
            this.Controls.Add(this.databasesLabel);
            this.Name = "Configuration";
            this.Size = new System.Drawing.Size(424, 239);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label databasesLabel;
        private System.Windows.Forms.TextBox databasesTextBox;
    }
}
