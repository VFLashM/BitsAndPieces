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

        public ChooseItem(string[] values, string title)
        {
            InitializeComponent();
            KeyPreview = true;

            _values = values;
            UpdateList();

            list.DoubleClick += new EventHandler(list_DoubleClick);
            list.Resize += new EventHandler(list_Resize);
            text.TextChanged += new EventHandler(text_TextChanged);
            Text = title;
        }

        void list_Resize(object sender, EventArgs e)
        {
            list.Columns[0].Width = list.Width - list.Columns[1].Width - SystemInformation.VerticalScrollBarWidth - 4;
        }

        void UpdateList()
        {
            list.Items.Clear();
            _truncated = false;
            foreach (var value in _values)
            {
                if (value.ToLower().Contains(text.Text.ToLower()))
                {
                    list.Items.Add(new ListViewItem(new string[]{value, "type"}));
                    if (list.Items.Count > 100)
                    {
                        list.Items.Add(new ListViewItem("< too many items, list truncated >"));
                        _truncated = true;
                        break;
                    }
                }
            }
            if (list.Items.Count > 0)
            {
                list.Items[0].Selected = true;
            }
            list.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent);
            list_Resize(null, null);
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
            if (list.SelectedIndices.Count > 0 && list.SelectedIndices[0] < itemCount)
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
            if (list.SelectedItems.Count > 0)
            {
                return list.SelectedItems[0].SubItems[0].Text;
            }
            return null;
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
                if (list.SelectedIndices.Count > 0 && list.SelectedIndices[0] > 0)
                {
                    list.Items[list.SelectedIndices[0]-1].Selected = true;
                }
            }
            else if (e.KeyCode == Keys.Down)
            {
                if (list.SelectedIndices.Count > 0 && list.SelectedIndices[0] < list.Items.Count - 1)
                {
                    list.Items[list.SelectedIndices[0]+1].Selected = true;
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
