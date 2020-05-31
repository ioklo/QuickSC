// 1

class C
{
    public static int x = 0;
}

C.x = 1;
@echo ${C.x}

var c = new C();
@echo ${c.x}
