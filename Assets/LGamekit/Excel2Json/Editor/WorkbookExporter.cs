using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;


namespace LGamekit.Excel2Json {



    public static class WorkbookExporter {

        [@MenuItem("Assets/Excel2Json/Export Workbook", false, 5000)]
        public static void Export() {
            var inputDir = EditorUtility.OpenFolderPanel("Export Workbook", "", "");
            if (string.IsNullOrEmpty(inputDir)) {
                return;
            }

            var files = Directory.GetFiles(inputDir, "*.xlsx", SearchOption.AllDirectories);
            if (files.Length == 0) {
                return;
            }

            var outputDir = Path.Combine(inputDir, "Workbooks");
            outputDir.CreateDirectory();

            foreach (var path in files) {
                var fileName = Path.GetFileNameWithoutExtension(path) + ".json";
                var workbookPath = Path.Combine(outputDir, fileName);
                var workbook = WorkbookSerializer.LoadWorkbook(path);
                var workbookJson = UnityEngine.JsonUtility.ToJson(workbook, true);
                File.WriteAllText(workbookPath, workbookJson, Encoding.UTF8);
            }

            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(outputDir);

        }

    }






}


