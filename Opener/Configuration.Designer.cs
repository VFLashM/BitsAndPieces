namespace Opener
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
            this.rootTextBox = new System.Windows.Forms.TextBox();
            this.databasesTextBox = new System.Windows.Forms.TextBox();
            this.rootLabel = new System.Windows.Forms.Label();
            this.databasesLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // rootTextBox
            // 
            this.rootTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rootTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::Opener.Properties.Settings.Default, "ProjectRoot", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.rootTextBox.Location = new System.Drawing.Point(220, 3);
            this.rootTextBox.Name = "rootTextBox";
            this.rootTextBox.Size = new System.Drawing.Size(262, 22);
            this.rootTextBox.TabIndex = 0;
            this.rootTextBox.Text = global::Opener.Properties.Settings.Default.ProjectRoot;
            // 
            // databasesTextBox
            // 
            this.databasesTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.databasesTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::Opener.Properties.Settings.Default, "Databases", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.databasesTextBox.Location = new System.Drawing.Point(220, 31);
            this.databasesTextBox.Name = "databasesTextBox";
            this.databasesTextBox.Size = new System.Drawing.Size(262, 22);
            this.databasesTextBox.TabIndex = 1;
            this.databasesTextBox.Text = global::Opener.Properties.Settings.Default.Databases;
            // 
            // rootLabel
            // 
            this.rootLabel.AutoSize = true;
            this.rootLabel.Location = new System.Drawing.Point(3, 6);
            this.rootLabel.Name = "rootLabel";
            this.rootLabel.Size = new System.Drawing.Size(85, 17);
            this.rootLabel.TabIndex = 2;
            this.rootLabel.Text = "Project root:";
            // 
            // databasesLabel
            // 
            this.databasesLabel.AutoSize = true;
            this.databasesLabel.Location = new System.Drawing.Point(3, 34);
            this.databasesLabel.Name = "databasesLabel";
            this.databasesLabel.Size = new System.Drawing.Size(211, 17);
            this.databasesLabel.TabIndex = 3;
            this.databasesLabel.Text = "Comma separated database list:";
            // 
            // Configuration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.databasesLabel);
            this.Controls.Add(this.rootLabel);
            this.Controls.Add(this.databasesTextBox);
            this.Controls.Add(this.rootTextBox);
            this.Name = "Configuration";
            this.Size = new System.Drawing.Size(485, 138);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox rootTextBox;
        private System.Windows.Forms.TextBox databasesTextBox;
        private System.Windows.Forms.Label rootLabel;
        private System.Windows.Forms.Label databasesLabel;
    }
}
