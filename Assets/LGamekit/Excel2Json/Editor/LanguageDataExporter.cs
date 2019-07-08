using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;


namespace LGamekit.Excel2Json {



    public static class LanguageDataExporter {

        public const string Tag = "keys";
        public const string Extend = "json";

        [@MenuItem("Assets/Excel2Json/Export Languages", false, 5001)]
        public static void Export() {
            var input = EditorUtility.OpenFilePanel("Export Languages", "", Extend);
            if (string.IsNullOrEmpty(input)) {
                return;
            }

            var workbookJson = File.ReadAllText(input, Encoding.UTF8);
            var workbook = UnityEngine.JsonUtility.FromJson<Workbook>(workbookJson);
            var languageDatas = new Dictionary<string, LanguageData>();

            foreach (var sheet in workbook.Sheets) {
                if (sheet.GetValue(0, 0).Trim().ToLower() != Tag) {
                    continue;
                }
                var dict = LanguageDataSerializer.Parse(sheet);
                foreach (var kvp in dict) {
                    languageDatas.Add(kvp.Key, kvp.Value);
                }
            }

            if (languageDatas.Count == 0) {
                return;
            }

            var outputDir = Path.GetDirectoryName(input);
            outputDir = Path.Combine(outputDir, "Languages");
            outputDir.CreateDirectory();

            foreach (var kvp in languageDatas) {
                var fileName = kvp.Key + "." + Extend;
                var languagePath = Path.Combine(outputDir, fileName);
                var languageJson = UnityEngine.JsonUtility.ToJson(kvp.Value, true);
                File.WriteAllText(languagePath, languageJson, Encoding.UTF8);
            }

            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(outputDir);
        }


    }
}