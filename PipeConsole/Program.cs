using System.IO;
using System.IO.Pipes;
using System.Numerics;
using System.Security.Principal;
using System.Text;

if (args.Length != 2)
{
    Console.WriteLine("usage: pconsole [in/out] [pipe name]"); //this in/out is from server's perspective.
    System.Environment.Exit(0);
}

if (args[0] == "in")
{
    var pipestream = new NamedPipeClientStream(".", args[1],
                        PipeDirection.Out, PipeOptions.None,
                        TokenImpersonationLevel.Impersonation);

    Console.WriteLine("Connecting...");
    pipestream.Connect();
    Console.WriteLine("Connected");

    string? cin;
    while ((cin = Console.ReadLine()) != "exit" && cin != null)
    {
        var content = Encoding.UTF8.GetBytes(cin);

        byte[] length_bits = new byte[2];
        ushort number = (ushort)content.Length;
        //assume little-endian
        length_bits[0] = (byte)(number & 255);
        length_bits[1] = (byte)(number >> 8);

        pipestream.Write(length_bits);
        pipestream.Write(content);
        pipestream.Flush();
#pragma warning disable CA1416 // 플랫폼 호환성 유효성 검사
        pipestream.WaitForPipeDrain();
#pragma warning restore CA1416 // 플랫폼 호환성 유효성 검사
    }

    pipestream.Close();
    // Give the client process some time to display results before exiting.
    Thread.Sleep(3000);
}
else if (args[0] == "out")
{
    var pipeClient = new NamedPipeClientStream(".", args[1],
                        PipeDirection.In, PipeOptions.None,
                        TokenImpersonationLevel.Impersonation);

    Console.WriteLine("Connecting...");
    pipeClient.Connect();
    Console.WriteLine("Connected");

    byte[] length_bits = new byte[2];
    while(true)
    {
        pipeClient.Read(length_bits, 0, length_bits.Length);
        //assume little-endian
        ushort number = (ushort)((ushort)(length_bits[0]) | (ushort)(length_bits[1]) << 8);

        byte[] body = new byte[number];
        pipeClient.ReadExactly(body, 0, body.Length);
        Console.WriteLine(Encoding.UTF8.GetString(body));
    }
}
else
{
    Console.WriteLine("usage: pconsole [in/out] [pipe name]");
    System.Environment.Exit(0);
}