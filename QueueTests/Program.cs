using MikoGPT;
using VkNet.Enums.SafetyEnums;

var qm = new SyncQueueManager();
Console.WriteLine("Add tasks");
Task.Run(() =>
{
    Task.Delay(100).Wait();
    qm.Add(new PrintTask("1"));
});
Task.Run(() =>
{
    Task.Delay(99).Wait();
    qm.Add(new PrintTask("2"));
});
Task.Run(() =>
{
    Task.Delay(99).Wait();
    qm.Add(new PrintTask("3"));
});
Console.WriteLine("fin");
Task.Delay(5000).Wait();

public class PrintTask : QueueTask
{
    public string value;
    public PrintTask(string val)
    {
        value = val;
    }
    public override void OnComplete()
    {
        Console.WriteLine($"{value} started");
        Task.Delay(1000).Wait();
        Console.WriteLine($"{value} ended");
    }
}

