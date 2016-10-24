using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Joiner
{
    public partial class Configuration : UserControl
    {
        public Configuration()
        {
            InitializeComponent();
            rulesText.KeyPress += new KeyPressEventHandler(rulesText_KeyPress);
        }

        void rulesText_KeyPress(object sender, KeyPressEventArgs e)
        {
            // AcceptsReturn does not work, so hacking here
            if (e.KeyChar == (char)13)
            {
                rulesText.AppendText(Environment.NewLine);
            }
        }
    }
}
