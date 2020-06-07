// 1234
seq string F()
{
    yield "Hello";
    yield "World";
}

foreach(var e in F())
    @$e