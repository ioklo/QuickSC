// hi hello world world true true false false onetwo true false true true false false true false true true

string s = "hi";
string t = s;

s = "hello"; // 

string p = "$t $s ${s = "world"} $s";
@echo $p

string t2 = "${"h"}${"i"}";
p = " ${t == t2} ${s == "world"} ${t != t2} ${s != "world"}";
@echo $p

p = " ${"one" + "two"}";
@echo $p

p = " ${"s1" < "s1abcd"} ${"s1abcd" < "s1"} ${"s1" <= "s1abcd"} ${"s1" <= "s1"} ${"s1abcd" <= "s1"}";
@echo $p

p = " ${"s1" > "s1abcd"} ${"s1abcd" > "s1"} ${"s1" >= "s1abcd"} ${"s1" >= "s1"} ${"s1abcd" >= "s1"}";
@echo $p
