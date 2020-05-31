// -3 4 4 false true true -4 -6 -26 2 3 true false true true false false true false true true

int i;

i = -3; // assignment

string p = "$i ${i = 4} $i";
@echo $p

p = " ${3 == 4} ${3 == 3} ${-3 == -3}";
@echo $p

p = " ${-3 - 3 + 2} ${-3 - 3} ${2 + 4 * -7} ${3 - 3 / 2} ${2 + 7 % 3}";
@echo $p

p = " ${2 < 4} ${4 < 2} ${2 <= 2} ${1 <= 2} ${3 <= 2} ${-10 > 20} ${20 > -10} ${-10 >= 20} ${20 >= -10} ${28 >= 28}";
@echo $p
