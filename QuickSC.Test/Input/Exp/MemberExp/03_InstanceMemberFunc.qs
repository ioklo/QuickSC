// 2

class C
{
    public int x;

    public void F()
    {
        @echo $x
    }

    public C(int x)
    {
        this.x = x;
    }
}

var c = new C(2);

var f = c.F;

f();