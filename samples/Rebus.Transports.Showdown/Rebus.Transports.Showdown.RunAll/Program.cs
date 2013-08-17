namespace Rebus.Transports.Showdown.RunAll
{
    class Program
    {
        static void Main()
        {
            SqlServer.Program.Main();
            Showndown.Msmq.Program.Main();
            Showndown.RabbitMq.Program.Main();
        }
    }
}
