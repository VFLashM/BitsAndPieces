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
        private bool _truncated = false;

        public ChooseItem(string[] values)
        {
            InitializeComponent();
            KeyPreview = true;

            _values = values;
            UpdateList();

            list.DoubleClick += new EventHandler(list_DoubleClick);
            text.TextChanged += new EventHandler(text_TextChanged);
        }

        void UpdateList()
        {
            list.Items.Clear();
            _truncated = false;
            foreach (var value in _values)
            {
                if (value.ToLower().Contains(text.Text.ToLower()))
                {
                    list.Items.Add(value);
                    if (list.Items.Count > 100)
                    {
                        list.Items.Add("< too many items, list truncated >");
                        _truncated = true;
                        break;
                    }
                }
            }
            if (list.Items.Count > 0)
            {
                list.SelectedIndex = 0;
            }
        }

        void text_TextChanged(object sender, EventArgs e)
        {
            UpdateList();
        }

        void list_DoubleClick(object sender, EventArgs e)
        {
            Finish();
        }

        void Finish()
        {
            int itemCount = _truncated ? list.Items.Count - 1 : list.Items.Count;
            if (list.SelectedIndex >= 0 && list.SelectedIndex < itemCount)
            {
                DialogResult = DialogResult.OK;
            }
            else
            {
                DialogResult = DialogResult.Cancel;
            }
        }

        public string Result()
        {
            return list.SelectedItem.ToString();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            e.Handled = true;
            if (e.KeyCode == Keys.Enter)
            {
                Finish();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
            }
            else if (e.KeyCode == Keys.Up)
            {
                if (list.SelectedIndex > 0)
                {
                    list.SelectedIndex -= 1;
                }
            }
            else if (e.KeyCode == Keys.Down)
            {
                if (list.SelectedIndex < list.Items.Count - 1)
                {
                    list.SelectedIndex += 1;
                }
            } 
            else 
            {
                e.Handled = false;
                text.Focus();
            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            text.Focus();
            base.OnKeyPress(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            text.Focus();
            base.OnKeyUp(e);
        }
    }
}
