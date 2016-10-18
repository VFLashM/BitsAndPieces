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
        class SearchTerm : IComparable
        {
            public readonly string value;
            public readonly int priority;

            public SearchTerm(string value, int priority)
            {
                this.value = value;
                this.priority = priority;
            }

            public int? Match(string template)
            {
                if (value == template)
                {
                    return this.priority * 6 + 5;
                }
                if (value.ToLower() == template.ToLower())
                {
                    return this.priority * 6 + 4;
                }
                if (value.StartsWith(template))
                {
                    return this.priority * 6 + 3;
                }
                if (value.ToLower().StartsWith(template.ToLower()))
                {
                    return this.priority * 6 + 2;
                }
                if (value.Contains(template))
                {
                    return this.priority * 6 + 1;
                }
                if (value.ToLower().Contains(template.ToLower()))
                {
                    return this.priority * 6;
                }
                return null;
            }

            public int CompareTo(object obj)
            {
                return -this.priority.CompareTo((obj as SearchTerm).priority);
            }
        }

        public class Item
        {
            public readonly string value;
            public readonly string type;
            readonly SearchTerm[] searchTerms;

            public Item(string value, string type)
            {
                var searchTerms = new List<SearchTerm>();
                searchTerms.Add(new SearchTerm(type, 0));
                searchTerms.Add(new SearchTerm(value, 1));
                int priority = 2;
                foreach (var part in value.Split('.'))
                {
                    foreach (var term in part.Split(':'))
                    {
                        searchTerms.Add(new SearchTerm(term, priority));
                    }
                    ++priority;
                }
                searchTerms.Sort();

                this.value = value;
                this.type = type; 
                this.searchTerms = searchTerms.ToArray();
            }

            public float? Match(string template)
            {
                foreach (var term in searchTerms)
                {
                    int? match = term.Match(template);
                    if (match.HasValue)
                    {
                        int totalTermLength = 0;
                        int totalMatchedLength = 0;
                        foreach (var otherTerm in searchTerms)
                        {
                            if (otherTerm.priority == term.priority)
                            {
                                totalTermLength += otherTerm.value.Length;
                                if (otherTerm.Match(template).HasValue)
                                {
                                    totalMatchedLength += template.Length;
                                }
                            }
                        }
                        float matchRate = (float)totalMatchedLength / (totalTermLength + 1);
                        return match.Value + matchRate;
                    }
                }
                return null;
            }
        }

        class FilteredItem : IComparable
        {
            public readonly string[] data;
            public readonly float priority;

            public FilteredItem(string[] data, float priority)
            {
                this.data = data;
                this.priority = priority;
            }

            public int CompareTo(object obj)
            {
                return -this.priority.CompareTo((obj as FilteredItem).priority);
            }
        }

        private Item[] _items;
        private bool _truncated = false;

        public ChooseItem(Item[] items, string title)
        {
            InitializeComponent();
            KeyPreview = true;

            _items = items;
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
            var filteredItems = new List<FilteredItem>();
            list.Items.Clear();
            _truncated = false;
            foreach (var item in _items)
            {
                float? priority = item.Match(text.Text);
                if (priority.HasValue)
                {
                    filteredItems.Add(new FilteredItem(new string[] { item.value, item.type }, priority.Value));
                }
            }
            filteredItems.Sort();
            foreach (var filteredItem in filteredItems)
            {
                list.Items.Add(new ListViewItem(filteredItem.data));
                if (list.Items.Count > 100)
                {
                    list.Items.Add(new ListViewItem("< too many items, list truncated >"));
                    _truncated = true;
                    break;
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
