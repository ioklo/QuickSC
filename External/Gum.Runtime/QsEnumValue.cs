using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace QuickSC
{
    public class QsEnumValue : QsValue
    {
        public string ElemName { get; private set; }
        private Dictionary<string, QsValue> values;

        public QsEnumValue()
        {
            ElemName = string.Empty;
            values = new Dictionary<string, QsValue>();
        }
        
        public QsEnumValue(string elemName, IEnumerable<(string Name, QsValue Value)> memberValues)
        {
            ElemName = elemName;

            values = new Dictionary<string, QsValue>();
            foreach (var memberValue in memberValues)
                values.Add(memberValue.Name, memberValue.Value);
        }        
        
        public override QsValue MakeCopy()
        {
            var copiedMembers = values.Select(e => (e.Key, e.Value));
            return new QsEnumValue(ElemName, copiedMembers);
        }

        public override void SetValue(QsValue fromValue)
        {
            QsEnumValue enumValue = (QsEnumValue)fromValue;
            SetValue(enumValue.ElemName, enumValue.values.Select(tv => (tv.Key, tv.Value)));
        }

        public void SetValue(string elemName, IEnumerable<(string Name, QsValue Value)> memberValues)
        {
            if (ElemName != elemName)
            {
                ElemName = elemName;
                values.Clear();
                foreach (var memberValue in memberValues)
                    values[memberValue.Name] = memberValue.Value.MakeCopy();
            }
            else
            {
                foreach (var memberValue in memberValues)
                    values[memberValue.Name].SetValue(memberValue.Value);
            }
        }

        public QsValue GetValue(string memberName)
        {
            return values[memberName];
        }
    }
    
}

