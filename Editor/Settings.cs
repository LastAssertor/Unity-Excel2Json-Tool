using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;

namespace UnityExcel2JsonTool {

    [CreateAssetMenu(menuName = "Unity-Excel2Json-Tool/Settings", fileName = "Settings.asset")]
    public class Settings : ScriptableObject {

        [Header("文件夹")]
        public string excelFolder = "Assets/Unity-Excel2Json-Tool/Example/Excels";
        public string exportWorkbookFolder = "Assets/Unity-Excel2Json-Tool/Generated/Workbooks";
        public string exportScriptFolder = "Assets/Unity-Excel2Json-Tool/Generated/Scripts";
        public string exportScriptDataFolder = "Assets/Unity-Excel2Json-Tool/Generated/Data";
        public string exportLocalizationDataFolder = "Assets/Unity-Excel2Json-Tool/Generated/Resources/Languages";

        [Header("脚本模版")]

        public string enumSheetType = "enum";
        public string classSheetType = "class";

        public TextAsset enumTemplate;
        public TextAsset classTemplate;
        public TextAsset classDataTemplate;
        public TextAsset dataSerializerTemplate;
        public TextAsset dataExporterTemplate;

        [Header("脚本")]
        public string headersFileName = "Headers";

        public string enumTag = "{{.Enum}}";
        public string classTag = "{{.Class}}";
        public string fieldsTag = "{{.Fields}}";
        public string methodsTag = "{{.Methods}}";
        public string readFieldsTag = "{{.ReadFields}}";
        public string menuItemOrderTag = "{{.MenuItemOrder}}";
        public string writeJsonsTag = "{{.WriteJsons}}";

        public int menuItemOrder = 5002;

        [Header("多语言")]
        public string localizationWorkbookFileName = "Localization";
        public string localizationKeyTag = "keys";

    }

}

