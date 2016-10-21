using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace Common
{
    public partial class ChooseItem : Form
    {
        public class SearchTerm : IComparable
        {
            public readonly string value;
            public readonly int priority;

            public SearchTerm(string value, int priority)
            {
                this.value = value;
                this.priority = priority;
            }

            static int? Match(string value, string template)
            {
                if (value == template)
                {
                    return 4;
                }
                if (value.StartsWith(template))
                {
                    return 3;
                }
                if (value.Contains(template))
                {
                    return 2;
                }
                if (template.Contains(' '))
                {
                    var regexTemplate = Regex.Escape(template).Replace("\\ ", ".*");
                    if (Regex.IsMatch(value, regexTemplate))
                    {
                        return 1;
                    }
                }
                return null;
            }

            public int? Match(string template)
            {
                int? match = Match(value, template);
                if (match.HasValue)
                {
                    return priority * 100 + match.Value * 2 + 1;
                }
                match = Match(value.ToLower(), template.ToLower());
                if (match.HasValue)
                {
                    return priority * 100 + match.Value * 2;
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

            public Item(string value, string type, SearchTerm[] searchTerms = null)
            {
                this.value = value;
                this.type = type;
                if (searchTerms != null)
                {
                    this.searchTerms = searchTerms;
                }
                else
                {
                    this.searchTerms = new SearchTerm[] {
                        new SearchTerm(value, 2),
                        new SearchTerm(type, 1),
                    };
                }
                Array.Sort(this.searchTerms);
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
                        float matchRate = (float)(Math.Max(totalMatchedLength, 1)) / (totalTermLength + 1);
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
            
            if (title != null)
            {
                Text = title;
            }
            else
            {
                this.ControlBox = false;
                this.Text = String.Empty;

                list.Width = this.Width;
                list.Top = 0;
                list.Left = 0;
                list.Height = this.Height - text.Height;

                text.Width = this.Width;
                text.Left = 0;
                text.Top = list.Height;
            }
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
                    var remaining = filteredItems.Count - list.Items.Count;
                    list.Items.Add(new ListViewItem("< too many items, " + remaining + " items truncated >"));
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

        int CurrentIdx()
        {
            return list.SelectedIndices.Count > 0 ? list.SelectedIndices[0] : -1;
        }

        void SelectIdx(int idx)
        {
            if (list.Items.Count == 0)
            {
                return;
            }
            idx = Math.Max(idx, 0);
            idx = Math.Min(idx, list.Items.Count - 1);
            list.Items[idx].Selected = true;
            list.EnsureVisible(idx);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            e.Handled = true;
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    Finish();
                    break;
                case Keys.Escape:
                    DialogResult = DialogResult.Cancel;
                    break;
                case Keys.Up:
                    SelectIdx(CurrentIdx() - 1);
                    break;
                case Keys.Down:
                    SelectIdx(CurrentIdx() + 1);
                    break;
                case Keys.PageUp:
                    SelectIdx(CurrentIdx() - 10);
                    break;
                case Keys.PageDown:
                    SelectIdx(CurrentIdx() + 10);
                    break;
                case Keys.Home:
                    if (e.Control)
                    {
                        SelectIdx(0);
                        break;
                    }
                    goto default;
                case Keys.End:
                    if (e.Control)
                    {
                        SelectIdx(list.Items.Count);
                        break;
                    }
                    goto default;
                default:
                    e.Handled = false;
                    text.Focus();
                    break;
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
