using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Opener
{
    public partial class Configuration : UserControl
    {
        public Configuration()
        {
            InitializeComponent();

            this.VisibleChanged += new EventHandler(Configuration_VisibleChanged);
        }

        void Configuration_VisibleChanged(object sender, EventArgs ev)
        {
            try
            {
                Properties.Settings.ResolveProjectRoot();
                Properties.Settings.Default.Save();
            }
            catch (Error e)
            {
                e.Show();
            }
        }
    }
}
