// 4

// global은 복사 캡쳐하지 않는다
int x = 3;

var f = () => { @{$x} };

x = 4;

f(); 