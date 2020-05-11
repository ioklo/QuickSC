enum X
{
    First,
    Second (int i)
}

var x = X.First;
x = X.Second (2);

if (x == X.First)
    @echo hi