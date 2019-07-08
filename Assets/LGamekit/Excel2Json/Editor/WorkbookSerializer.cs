using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;


namespace LGamekit.Excel2Json {

    public static class WorkbookSerializer {

        static Sheet ParseSheet(Codaxy.Xlio.Sheet sheet) {
            var result = new Sheet();
            result.Name = sheet.SheetName;
            for (int row = 0, rowLen = sheet.Data.LastRow + 1; row < rowLen; row++) {
                var cCollection = new ColumnCollection();
                for (int col = 0, colLen = sheet.Data.LastCol + 1; col < colLen; col++) {
                    var cellData = sheet[row, col].Value;
                    cCollection.Data.Add(cellData == null ? string.Empty : cellData.ToString());
                }
                result.ColumnCollections.Add(cCollection);
            }
            return result;
        }

        static Workbook ParseWorkbook(Codaxy.Xlio.Workbook workbook, string name) {
            var result = new Workbook();
            result.Name = name;
            foreach (var sheet in workbook.Sheets) {
                result.Sheets.Add(ParseSheet(sheet));
            }
            return result;
        }

        public static Workbook LoadWorkbook(string xlsxFile) {
            var fileName = Path.GetFileNameWithoutExtension(xlsxFile);
            return ParseWorkbook(Codaxy.Xlio.Workbook.Load(xlsxFile), fileName);
        }

        public static Dictionary<string, Workbook> LoadWorkbooks(string inputDir) {
            var workbooks = new Dictionary<string, Workbook>();
            var files = Directory.GetFiles(inputDir, "*.json", SearchOption.TopDirectoryOnly);

            foreach (var path in files) {
                var workbookJson = File.ReadAllText(path, Encoding.UTF8);
                var workbook = UnityEngine.JsonUtility.FromJson<Workbook>(workbookJson);
                workbooks.Add(workbook.Name, workbook);
            }

            return workbooks;
        }

    }

}




