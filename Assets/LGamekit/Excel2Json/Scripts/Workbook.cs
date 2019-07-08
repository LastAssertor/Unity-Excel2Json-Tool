using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;

namespace LGamekit.Excel2Json {

    [Serializable]
    public class ColumnCollection {
        public List<string> Data = new List<string>();
    }

    [Serializable]
    public class Sheet {

        public string Name;
        public List<ColumnCollection> ColumnCollections = new List<ColumnCollection>();

        public int GetRowSize() {
            return ColumnCollections.Count;
        }

        public int GetColSize() {
            if (ColumnCollections.Count == 0) {
                return 0;
            }
            return ColumnCollections[0].Data.Count;
        }

        public string GetValue(int row, int col) {
            if (row < 0 || row >= GetRowSize())
                return string.Empty;
            if (col < 0 || col >= GetColSize())
                return string.Empty;
            return ColumnCollections[row].Data[col];
        }

        public bool TryGetValue(int row, int col, out string val) {
            val = GetValue(row, col);
            return !string.IsNullOrEmpty(val);
        }

        public string GetColRange(int row, int col, int size, string separator) {
            var sb = new StringBuilder();

            var str = GetValue(row, col);

            if(string.IsNullOrEmpty(str)) {
                return string.Empty;
            }

            sb.Append(str);

            var i = col + 1;
            var len = col + size;
            for (; i < len; i++) {
                if(!TryGetValue(row, i, out str)) {
                    break;
                }
                sb.Append(separator).Append(str);
            }

            return sb.ToString();
        }

    }

    [Serializable]
    public class Workbook {

        public string Name;
        public List<Sheet> Sheets = new List<Sheet>();

        public Sheet GetSheet(string name) {
            foreach (var sheet in Sheets) {
                if (sheet.Name == name) {
                    return sheet;
                }
            }
            return null;
        }

        public bool TryGetSheet(string name, out Sheet sheet) {
            sheet = GetSheet(name);
            return sheet != null;
        }

    }

}



