using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace LGamekit.Excel2Json
{


    public static class StringExtensions
    {

        public static bool HasChinese(this string str)
        {
            return Regex.IsMatch(str + string.Empty, @"[\u4e00-\u9fa5]{1,}[\u4e00-\u9fa5.·]{0,15}[\u4e00-\u9fa5]{1,}");
        }

        public static void CreateDirectory(this string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
            Directory.CreateDirectory(directory);
        }

    }

}


