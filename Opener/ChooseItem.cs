using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Opener
{
    public partial class ChooseItem : Form
    {
        private string[] _values;

        public ChooseItem(string[] values)
        {
            InitializeComponent();
            _values = values;
        }

        public string Result()
        {
            return _values[0];
        }
    }
}
