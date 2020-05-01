using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.Runtime
{
    // String 
    public class QsStringObject : QsObject
    {
        public string Data { get; } // 내부 구조는 string

        public QsStringObject(string data)
        {
            Data = data;
        }
    }
}
