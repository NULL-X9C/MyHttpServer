using MyHttpServer;
// идея юрать списрк конфигов и запустить сразу несколько страниц (украл у Артёма)

Console.WriteLine("Hello, World!");
var runner = new Welcome();
await runner.RunUserOrder();
