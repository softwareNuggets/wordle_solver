using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;

namespace wordleSolver
{
    public partial class MainWindow : Window
    {
        private int[] _cellColor        = new int[30];
        private List<string> __DARKGREY = new List<string>();
        private List<string> __GREEN    = new List<string>();
        private List<string> __GOLD     = new List<string>();
        private int _currentRow         = 0;

        List<FiveLetterWords> _all5LetterWords;
        
        public MainWindow()
        {
            InitializeComponent();
            
            InitializeApp();

            AttachAllWordsToLBoxAllWords();
        }

        private void AttachAllWordsToLBoxAllWords()
        {
            if (_all5LetterWords != null)
            {
                this.LBoxAllWords.ItemsSource = _all5LetterWords;
                BtnSearch.Content = "Search - (" + _all5LetterWords.Count().ToString() + ")";
            }
        }

        private void InitializeApp()
        {
            
            LoadTextBlock();

            InitializeCellColors();

            LoadAll5LetterWords();

            UnlockRow(0);

        }

        private void LoadTextBlock()
        {
            int[] offset = { 0, 2, 4, 6, 8, 10 };
            int tag_offset = 0;

            for (int r = 0; r < 6; r++)
            {
                for (int c = 0; c < 5; c++, tag_offset++)
                {
                    var tb = new TextBlock();
                    tb.Uid = string.Format($"r{r}c{c}");
                    tb.Name = string.Format($"r{r}c{c}");
                    tb.Text = String.Empty;
                    tb.TextAlignment = TextAlignment.Center;
                    tb.Tag = tag_offset.ToString();
                    tb.FontFamily = new FontFamily("fourier");
                    tb.FontSize = 50;
                    tb.TextWrapping = TextWrapping.NoWrap;

                    tb.Width = tb.Height = 60.0;

                    if (r > 0)
                        tb.IsEnabled = false;

                    tb.PreviewMouseLeftButtonUp += Tb_PreviewMouseLeftButtonUp;

                    TheBoard.Children.Add(tb);

                    Grid.SetRow(tb, offset[r]);
                    Grid.SetColumn(tb, offset[c]);
                }
            }
        }

        private void InitializeCellColors()
        {
            for (int i = 0; i < 30; i++)
                _cellColor[i] = (int)CellColorType.DarkGray;

            var ctrls = TheBoard.Children.OfType<TextBlock>();
            foreach (TextBlock ctrl in ctrls)
            {
                ctrl.Background = new SolidColorBrush(Colors.GhostWhite);
            }
        }

        private void LoadAll5LetterWords()
        {
            string filename = @"AppData\words.json";
            if (System.IO.File.Exists(filename) == true)
            {
                string jsonText = System.IO.File.ReadAllText(filename);
                if (jsonText != null)
                {
                    _all5LetterWords = JsonConvert.DeserializeObject<List<FiveLetterWords>>(jsonText);
                }
            }
        }

        private void UnlockRow(int row)
        {
            var ctrls = TheBoard.Children.OfType<TextBlock>();

            foreach (var ctrl in ctrls)
            {
                if (ctrl.Name.Contains($"r{row}c"))
                {
                    ctrl.IsEnabled = true;
                    ctrl.Background = new SolidColorBrush(Colors.DarkGray);
                    string? tag = ctrl.Tag.ToString();
                    if (tag != null)
                    {
                        int offset = int.Parse(tag);
                        _cellColor[offset] = (int)CellColorType.DarkGray;
                    }

                }
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            var _passOneList = new List<FiveLetterWords>();
            var tempdb = GetAllWordsFromListBox();

            ProcessRow(_currentRow);

            foreach (var w in tempdb)
            {
                CharacterUsedStateType state = CharacterUsedStateType.Init;

                if (w.Word != null)
                {

                    state = SearchGreen(w.Word.ToString());

                    if (state != CharacterUsedStateType.Failed)
                    {
                        state = SearchGold(w.Word.ToString(), state);
                    }

                    if (state != CharacterUsedStateType.Failed)
                    {
                        state = SearchDarkGray(w.Word.ToString(), state);
                    }

                    if (state == CharacterUsedStateType.Success ||
                        state == CharacterUsedStateType.Init)
                    {
                        var f = new FiveLetterWords();
                        f = w;
                        _passOneList.Add(f);
                    }
                }
            }

            LBoxAllWords.ItemsSource = null;
            LBoxAllWords.ItemsSource = _passOneList;
            _currentRow = _currentRow + 1;

            BtnSearch.Content = "Search - (" + _passOneList.Count().ToString() + ")";

        }

        private List<FiveLetterWords> GetAllWordsFromListBox()
        {

            var data = (List<FiveLetterWords>)this.LBoxAllWords.ItemsSource;
            return (data);
        }

        private void ProcessRow(int row)
        {
            if (row == 0)
            {
                __DARKGREY.Clear();
                __GREEN.Clear();
                __GOLD.Clear();
            }
            LoadColorTypeBuckets(row);
        }

        private void LoadColorTypeBuckets(int row)
        {
            var ctrls = TheBoard.Children.OfType<TextBlock>();

            var rowTextBlocks = ctrls.Where(p => p.Name.Contains($"r{row}c")).ToList();


            foreach (var ctrl in rowTextBlocks)
            {
                int offset = GetOffsetValue(ctrl);  // sequence value placed in tag property
                int column = GetColumnValue(ctrl, row);  // get column from r0c1  = 1
                string value = GetTextValueFromCtrl(ctrl);

                switch (_cellColor[offset])
                {
                    case (int)CellColorType.DarkGray:
                        ProcessDark(ctrl, offset, column, value);
                        break;

                    case (int)CellColorType.Gold:
                        ProcessGold(ctrl, offset, column, value);
                        break;

                    case (int)CellColorType.Green:
                        ProcessGreen(ctrl, offset, column, value);
                        break;
                }

                ctrl.IsEnabled = false;
            }
        }
        
        private int GetOffsetValue(TextBlock ctrl)
        {
            if (ctrl == null) return -1;

            string? tag = ctrl.Tag.ToString();
            if (tag != null)
            {
                int offset = int.Parse(tag);
                return (offset);
            }

            return -1;
        }

        private int GetColumnValue(TextBlock ctrl, int row)
        {
            string? temp = ctrl.Name.Replace($"r{row}c", "");
            if (temp != null)
            {
                int column = int.Parse(temp);
                return (column);
            }

            return -1;
        }

        private string GetTextValueFromCtrl(TextBlock ctrl)
        {
            string value = ctrl.Text.ToString().ToLower();
            return value;
        }

        private void ProcessDark(TextBlock ctrl, int offset, int column, string value)
        {
            if (IsCharacterInCollection(CellColorType.Green, value) == true)
                return;

            if (IsCharacterInCollection(CellColorType.Gold, value) == true)
                return;

            if (__DARKGREY.Contains(value) == false)
                __DARKGREY.Add($"{value}");
        }

        private void ProcessGold(TextBlock ctrl, int offset, int column, string value)
        {
            if (IsCharacterInDarkGrayCollection(value) == true)
            {
                RemoveCharacterFromDarkCollection(value);
            }

            __GOLD.Add($"{column},{value}");
        }

        private void ProcessGreen(TextBlock ctrl, int offset, int column, string value)
        {
            if (IsCharacterInDarkGrayCollection(value) == true)
            {
                RemoveCharacterFromDarkCollection(value);
            }

            __GREEN.Add($"{column},{value}");
        }


        private bool IsCharacterInCollection(CellColorType colorType, string value)
        {
            bool found = false;
            char[] sep = { ',' };

            List<string> temp;

            if (colorType == CellColorType.Green)
                temp = __GREEN;
            else
                temp = __GOLD;

            if (colorType > 0)
            {
                foreach (var entry in temp)
                {
                    var key_value = entry.Split(sep);
                    int position = int.Parse(key_value[0]);

                    if (value == key_value[1])
                    {
                        found = true;
                        break;
                    }
                }
            }

            return (found);
        }

        private void RemoveCharacterFromDarkCollection(string value)
        {
            if (this.__DARKGREY.Count > 0)
            {
                foreach (var ch in this.__DARKGREY)
                {
                    if (ch == value)
                    {
                        this.__DARKGREY.Remove(ch);
                        break;
                    }
                }
            }
        }

        private bool IsCharacterInDarkGrayCollection(string value)
        {
            bool found = false;
            if (this.__DARKGREY.Count > 0)
            {
                char[] sep = { ',' };
                foreach (var ch in this.__DARKGREY)
                {
                    if (ch == value)
                    {
                        found = true;
                        break;
                    }
                }
            }
            return (found);
        }

        private CharacterUsedStateType SearchGreen(string word)
        {
            char[] sep = { ',' };
            CharacterUsedStateType state = CharacterUsedStateType.Init;

            if (this.__GREEN.Count > 0)
            {
                foreach (var entry in this.__GREEN)
                {
                    var key_value = entry.Split(sep);
                    int position = int.Parse(key_value[0]);

                    if (word.Substring(position, 1) == key_value[1])
                        state = CharacterUsedStateType.Success;
                    else
                    {
                        state = CharacterUsedStateType.Failed;
                        break;
                    }
                }
            }

            return (state);
        }

        private CharacterUsedStateType SearchGold(string word, CharacterUsedStateType state)
        {
            char[] sep = { ',' };
            // when it's GOLD
            // when a character can be used anywhere
            // it must be in the word
            // but, not in this current position

            if (this.__GOLD.Count > 0)
            {
                foreach (var entry in this.__GOLD)
                {
                    var key_value = entry.Split(sep);
                    int position = int.Parse(key_value[0]);

                    if (word.Contains(key_value[1]) == true)
                    {
                        if (word.Substring(position, 1) != key_value[1])
                        {
                            state = CharacterUsedStateType.Success;
                        }
                        else
                        {
                            state = CharacterUsedStateType.Failed;
                            break;
                        }
                    }
                    else
                    {
                        state = CharacterUsedStateType.Failed;
                        break;
                    }
                }
            }

            return (state);
        }

        private CharacterUsedStateType SearchDarkGray(string word, CharacterUsedStateType state)
        {
            if (this.__DARKGREY.Count > 0)
            {
                char[] sep = { ',' };
                foreach (var ch in this.__DARKGREY)
                {
                    bool found = false;
                    foreach (var green_ch in this.__GREEN)
                    {
                        var key_value = green_ch.Split(sep);
                        int position = int.Parse(key_value[0]);

                        if (ch == key_value[1])
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found == false)
                    {
                        if (word.Contains(ch) == true)
                        {
                            state = CharacterUsedStateType.Failed;
                            break;
                        }
                    }

                }
            }

            return (state);
        }

        private void Tb_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var ctrl = (TextBlock)sender;
            int offset = GetOffsetValue(ctrl);
            if (offset == -1) return;

            if (_cellColor[offset] == (int)CellColorType.DarkGray)
            {
                ctrl.Background = new SolidColorBrush(Colors.Gold);
                _cellColor[offset] = (int)CellColorType.Gold;

            }
            else if (_cellColor[offset] == (int)CellColorType.Gold)
            {
                ctrl.Background = new SolidColorBrush(Colors.Green);
                _cellColor[offset] = (int)CellColorType.Green;
            }
            else if (_cellColor[offset] == (int)CellColorType.Green)
            {
                ctrl.Background = new SolidColorBrush(Colors.DarkGray);
                _cellColor[offset] = (int)CellColorType.DarkGray;
            }
        }

        private void LBoxAllWords_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedWord = (FiveLetterWords)this.LBoxAllWords.SelectedItem;
            if (selectedWord != null)
            {
                if (selectedWord.Word != null && selectedWord.Word.Length == 5)
                {
                    LoadWordIntoTextBlock(selectedWord.Word.ToString(), _currentRow);
                    UnlockRow(_currentRow);
                }
            }
        }

        private void LoadWordIntoTextBlock(string selectedWord, int row)
        {
            var ctrls = TheBoard.Children.OfType<TextBlock>();
            int offset = 0;
            foreach (var ctrl in ctrls)
            {
                if (ctrl.Name.Contains($"r{row}c"))
                {
                    ctrl.Text = selectedWord[offset].ToString().ToLower();
                    offset++;
                }
            }
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            InitializeCellColors();
            LoadAll5LetterWords();
            AttachAllWordsToLBoxAllWords();
            _currentRow = 0;
            __DARKGREY.Clear();
            __GREEN.Clear();
            __GOLD.Clear();
            UnlockRow(0);

            
            var ctrls = TheBoard.Children.OfType<TextBlock>();
            

            foreach (var ctrl in ctrls)
            {
                ctrl.Text = String.Empty;
            }
        }
    }
}

