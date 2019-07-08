﻿using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LGamekit.Excel2Json
{


    public static class StringBuilderExtensions
    {

        public static StringBuilder AppendTab(this StringBuilder stringBuilder, int times = 1)
        {
            var count = 0;
            while (count < times)
            {
                stringBuilder.Append("\t");
                count++;
            }
            return stringBuilder;
        }

    }

}

