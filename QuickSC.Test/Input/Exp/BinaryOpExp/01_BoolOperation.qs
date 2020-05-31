// false true true true false false true false true true false

bool b;

b = false; // assignment

string p = "$b ${b = true} $b";
@echo $p

p = " ${false == false} ${false == true} ${true == false} ${true == true}";
@echo $p

p = " ${false != false} ${false != true} ${true != false} ${true != true}";
@echo $p
