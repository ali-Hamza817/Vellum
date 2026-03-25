namespace SampleApp.Data;

public class OrderRepository
{
    public void Save(object order)
    {
        Console.WriteLine("Order saved to database.");
    }
}
