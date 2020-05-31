// 23

class C
{
    public static (int => void) F;
}

C.F = i => {
    @echo $i
});

C.F(2);

var c = new C();
c.F(3);