// output: 21
int x;

x = 1;

{
    int x;
    x = 2; // write
    @echo $x
}

// read
@echo $x