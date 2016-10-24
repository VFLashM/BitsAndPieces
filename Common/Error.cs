using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Common
{
    public class Error : Exception
    {
        private string _caption;
        public Error(string message, string caption = "Error")
            : base(message)
        {
            _caption = caption;
        }

        public void Show()
        {
            MessageBox.Show(Message, _caption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
    }
}
