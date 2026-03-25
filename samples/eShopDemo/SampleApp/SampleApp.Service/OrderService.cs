using SampleApp.Data;

namespace SampleApp.Service;

public class OrderService
{
    private readonly OrderRepository _repository;

    public OrderService(OrderRepository repository)
    {
        _repository = repository;
    }

    public void PlaceOrder(object order)
    {
        _repository.Save(order);
    }
}
