using System.IO;
using System.IO.Pipes;
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

    using (StreamWriter sw = new(pipestream, Encoding.UTF8))
    {
        sw.AutoFlush = true;
        string? cin;
        while ((cin = Console.ReadLine()) != "exit")
        {
            sw.WriteLine(cin);
            pipestream.WaitForPipeDrain();
        }
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

    using (StreamReader sr = new(pipeClient, Encoding.UTF8))
    {
        // Display the read text to the console
        string? temp;
        while ((temp = sr.ReadLine()) != null)
        {
            Console.WriteLine(temp);
        }
    }
    
    pipeClient.Close();
    // Give the client process some time to display results before exiting.
    Thread.Sleep(3000);
}
else
{
    Console.WriteLine("usage: pconsole [in/out] [pipe name]");
    System.Environment.Exit(0);
}