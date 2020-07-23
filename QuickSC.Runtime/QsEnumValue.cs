using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace QuickSC
{
    public class QsEnumValue : QsValue
    {
        private Dictionary<QsName, QsValue> valueLocations;

        public QsTypeInst TypeInst { get; }

        // memberValues will be copied
        public QsEnumValue(QsTypeInst typeInst, IEnumerable<(QsName Name, QsValue Value)> memberValues)
        {
            TypeInst = typeInst;

            valueLocations = new Dictionary<QsName, QsValue>();
            foreach (var memberValue in memberValues)
                valueLocations.Add(memberValue.Name, memberValue.Value.MakeCopy());
        }        
        
        public override QsValue MakeCopy()
        {
            var copiedMembers = valueLocations.Select(e => (e.Key, e.Value));
            return new QsEnumValue(TypeInst, copiedMembers);
        }

        public override void SetValue(QsValue fromValue)
        {
            this.valueLocations = ((QsEnumValue)fromValue).valueLocations;
        }

        public QsTypeInst GetTypeInst()
        {
            return TypeInst;
        }
    }
    
}

