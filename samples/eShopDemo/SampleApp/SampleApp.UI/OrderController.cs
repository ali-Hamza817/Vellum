using SampleApp.Data;
using SampleApp.Service;

namespace SampleApp.UI;

public class OrderController
{
    private readonly OrderService _service;
    private readonly OrderRepository _repository; // This is a violation (UI -> Data)

    public OrderController(OrderService service, OrderRepository repository)
    {
        _service = service;
        _repository = repository;
    }

    public void Post(object order)
    {
        // Correct path
        _service.PlaceOrder(order);

        // VIOLATION: Bypassing Service layer
        _repository.Save(order); 
    }
}
