using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System;
using System.IO;
using System.Text;


namespace LGamekit.Excel2Json {



    public static class LanguageDataSerializer {

        public static Dictionary<string, LanguageData> Parse(Sheet sheet) {
            var dict = new Dictionary<string, LanguageData>();
            var keys = new List<string>();

            var rowLen = sheet.GetRowSize();
            var colLen = sheet.GetColSize();
            var row = 1;
            var col = 1;

            for (; row < rowLen; row++) {
                keys.Add(sheet.GetValue(row, 0));
            }

            while (col < colLen) {

                var languageData = new LanguageData();
                languageData.Language = sheet.GetValue(0, col);

                if (string.IsNullOrEmpty(languageData.Language)) {
                    continue;
                }

                languageData.Data.AddRange(keys);

                row = 1;

                while (row < rowLen) {
                    languageData.Data.Add(sheet.GetValue(row, col));
                    row++;
                }

                dict.Add(languageData.Language, languageData);

                col++;
            }

            return dict;
        }

    }




}