// -3 3 -3 -2 -2 -3

int i = -3;

string p = "$i ${-i}";
@echo $p

p = " ${i++} ${i--} ${++i} ${--i}";
@echo $p