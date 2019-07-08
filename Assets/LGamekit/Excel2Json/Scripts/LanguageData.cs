using System.Collections;
using System.Collections.Generic;
using System;

namespace LGamekit.Excel2Json
{


    [Serializable]
    public class LanguageData
    {
        public string Language;
        public List<string> Data = new List<string>();
    }
}